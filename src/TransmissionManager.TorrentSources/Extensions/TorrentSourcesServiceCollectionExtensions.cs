using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using System.Net;
using TransmissionManager.TorrentSources.JsonPointer;
using TransmissionManager.TorrentSources.WebPage;

namespace Microsoft.Extensions.DependencyInjection;

public static class TorrentSourcesServiceCollectionExtensions
{
    private const string _torrentSourcesConfigKey = "TorrentSources";
    private const string _webPageConfigKey = "WebPage";
    private const string _jsonPointerConfigKey = "JsonPointer";

    public static IServiceCollection AddTorrentSourcesServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var torrentSourcesSection = configuration.GetRequiredSection(_torrentSourcesConfigKey);

        _ = services
            .AddSingleton<IValidateOptions<TorrentWebPageClientOptions>, ValidateTorrentWebPageClientOptions>()
            .AddOptions<TorrentWebPageClientOptions>()
            .Bind(torrentSourcesSection.GetRequiredSection(_webPageConfigKey))
            .ValidateOnStart();

        _ = services
            .AddSingleton<IValidateOptions<TorrentJsonPointerClientOptions>, ValidateTorrentJsonPointerClientOptions>()
            .AddOptions<TorrentJsonPointerClientOptions>()
            .Bind(torrentSourcesSection.GetRequiredSection(_jsonPointerConfigKey))
            .ValidateOnStart();

        _ = services
            .AddHttpClient<TorrentWebPageClient>()
            .ConfigurePrimaryHttpMessageHandler(
                static () => new SocketsHttpHandler { AutomaticDecompression = DecompressionMethods.All })
            .AddStandardResilienceHandler(ConfigureResilience);

        _ = services
            .AddHttpClient<TorrentJsonPointerClient>()
            .ConfigurePrimaryHttpMessageHandler(
                static () => new SocketsHttpHandler { AutomaticDecompression = DecompressionMethods.All })
            .AddStandardResilienceHandler(ConfigureResilience);

        return services;
    }

    /// <remarks>
    /// These bound getting a response's headers and nothing beyond them: every source client
    /// requests with <see cref="HttpCompletionOption.ResponseHeadersRead"/>, so reading the body
    /// happens after the pipeline has let go, under that source's own <c>ResponseReadTimeout</c>.
    /// The two are therefore additive - a source's worst case is this <c>TotalRequestTimeout</c>
    /// plus its own read timeout - and no configured value can cut the retries here short.
    /// </remarks>
    private static void ConfigureResilience(HttpStandardResilienceOptions options)
    {
        options.TotalRequestTimeout = new HttpTimeoutStrategyOptions
        {
            Name = "FifteenSeconds-TotalRequestTimeout",
            Timeout = TimeSpan.FromSeconds(15)
        };

        options.AttemptTimeout = new HttpTimeoutStrategyOptions
        {
            Name = "SevenSeconds-AttemptTimeout",
            Timeout = TimeSpan.FromSeconds(7)
        };
    }
}
