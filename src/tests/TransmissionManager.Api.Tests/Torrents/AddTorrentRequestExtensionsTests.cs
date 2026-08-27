using System.ComponentModel.DataAnnotations;
using TransmissionManager.Api.Actions.Torrents.AddOne;
using TransmissionManager.Api.Common.Dto.Torrents;
using TransmissionManager.Transmission.Dto;

namespace TransmissionManager.Api.Tests.Torrents;

/// <remarks>
/// An add has nothing to replace, so an empty optional value carries no meaning a caller could have
/// intended beyond "absent" - unlike an update, where it clears a stored value. Both halves of that
/// need pinning together: validation has to let the empty value through, and the mapping has to turn
/// it into a null, because <c>TorrentAddDto</c> throws on a blank string and a request that reached
/// it would fault rather than fail validation - after Transmission has already taken the torrent.
/// </remarks>
[Parallelizable(ParallelScope.All)]
internal sealed class AddTorrentRequestExtensionsTests
{
    private static readonly TransmissionTorrentAddResponseItem _transmissionTorrent =
        new() { HashString = "3a8151e8fd4ff37cd2acbcfd6e5f7d1c1ba1e00c", Name = "Some Torrent Name" };

    private static AddTorrentRequest CreateRequest(string? optionalValue) => new()
    {
        SourceUri = new("https://torrenttracker.com/forum/viewtopic.php?t=1234567"),
        DownloadDir = "/tvshows",
        MagnetRegexPattern = optionalValue,
        JsonValueFormat = optionalValue,
        Cron = optionalValue,
    };

    [TestCase(null)]
    [TestCase("")]
    public void AddTorrentRequest_WhenOptionalValueIsAbsentOrEmpty_PassesValidation(string? optionalValue)
    {
        var request = CreateRequest(optionalValue);
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(request, new(request), results, validateAllProperties: true);

        Assert.That(isValid, Is.True, string.Join("; ", results.Select(static r => r.ErrorMessage)));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase(" ", TestName = "ToTorrentAddDto_WhenOptionalValueIsAbsentOrBlank_MapsItToNull(single space)")]
    [TestCase("\t ", TestName = "ToTorrentAddDto_WhenOptionalValueIsAbsentOrBlank_MapsItToNull(tab and space)")]
    public void ToTorrentAddDto_WhenOptionalValueIsAbsentOrBlank_MapsItToNull(string? optionalValue)
    {
        var dto = CreateRequest(optionalValue).ToTorrentAddDto(_transmissionTorrent, DateTime.UtcNow);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(dto.MagnetRegexPattern, Is.Null);
            Assert.That(dto.JsonValueFormat, Is.Null);
            Assert.That(dto.Cron, Is.Null);
        }
    }

    /// <remarks>
    /// The gap the mapping has to close. A blank pattern is a regular expression that compiles, and
    /// only a web page source additionally requires one to look for a magnet link - so under a JSON
    /// Pointer source nothing upstream of the mapping refuses it.
    /// </remarks>
    [Test]
    public void AddTorrentRequest_WhenPatternIsBlankUnderAJsonPointerSource_PassesValidation()
    {
        var request = new AddTorrentRequest
        {
            SourceUri = new("https://torrenttracker.com/v1/topics#/result/1/2"),
            SourceKind = TorrentSourceKind.JsonPointer,
            DownloadDir = "/tvshows",
            MagnetRegexPattern = " ",
        };

        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(request, new(request), results, validateAllProperties: true);

        Assert.That(isValid, Is.True, string.Join("; ", results.Select(static r => r.ErrorMessage)));
    }

    [Test]
    public void ToTorrentAddDto_WhenOptionalValueIsSupplied_PassesItThrough()
    {
        var request = new AddTorrentRequest
        {
            SourceUri = new("https://torrenttracker.com/forum/viewtopic.php?t=1234567"),
            DownloadDir = "/tvshows",
            MagnetRegexPattern = @"magnet:\?xt=urn:btih:[a-fA-F0-9]{40}",
            JsonValueFormat = "magnet:?xt=urn:btih:{0}",
            Cron = "0 9,17 * * *",
        };

        var dto = request.ToTorrentAddDto(_transmissionTorrent, DateTime.UtcNow);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(dto.MagnetRegexPattern, Is.EqualTo(request.MagnetRegexPattern));
            Assert.That(dto.JsonValueFormat, Is.EqualTo(request.JsonValueFormat));
            Assert.That(dto.Cron, Is.EqualTo(request.Cron));
        }
    }
}
