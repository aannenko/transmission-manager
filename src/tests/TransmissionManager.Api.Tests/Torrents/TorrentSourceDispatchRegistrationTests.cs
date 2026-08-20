using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TransmissionManager.Api.Actions.Torrents;
using TransmissionManager.Database.Dto;
using TransmissionManager.TorrentSources.Dto;

namespace TransmissionManager.Api.Tests.Torrents;

/// <remarks>
/// Guards the seam between the enum and the clients, which nothing else covers: dispatch resolves
/// its client at call time, so a kind whose client was never registered fails at the first search -
/// on the cron path, unattended - rather than at startup.
/// </remarks>
[Parallelizable(ParallelScope.Self)]
internal sealed class TorrentSourceDispatchRegistrationTests
{
    /// <remarks>
    /// Both clients reject a non-HTTP address before requesting anything, so this reaches the
    /// resolution it is testing and stops there. A missing registration surfaces as
    /// <see cref="InvalidOperationException"/> and a missing branch as
    /// <see cref="ArgumentOutOfRangeException"/>; neither can be mistaken for the expected outcome.
    /// </remarks>
    [Test]
    public async Task FindMagnetUriAsync_ForEveryDefinedSourceKind_ResolvesARegisteredClient()
    {
        using var provider = new ServiceCollection()
            .AddTorrentSourcesServices(CreateConfiguration())
            .BuildServiceProvider();

        foreach (var sourceKind in Enum.GetValues<TorrentSourceKind>())
        {
            var outcome = await provider
                .FindMagnetUriAsync(new("ftp://torrenttracker.com/x"), sourceKind, default)
                .ConfigureAwait(false);

            Assert.That(
                outcome.Result,
                Is.EqualTo(MagnetSearchResult.InvalidSource),
                $"{sourceKind} did not dispatch to a registered client.");
        }
    }

    private static IConfiguration CreateConfiguration() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["TorrentSources:WebPage:DefaultMagnetRegexPattern"] = @"magnet:\?xt=urn:btih:[^""]+",
                ["TorrentSources:WebPage:RegexMatchTimeout"] = "00:00:00.1",
                ["TorrentSources:WebPage:ResponseReadTimeout"] = "00:00:30",
                ["TorrentSources:JsonPointer:RegexMatchTimeout"] = "00:00:00.1",
                ["TorrentSources:JsonPointer:ResponseReadTimeout"] = "00:00:30",
                ["TorrentSources:JsonPointer:MaxJsonTokenBytes"] = "4096",
            })
            .Build();
}
