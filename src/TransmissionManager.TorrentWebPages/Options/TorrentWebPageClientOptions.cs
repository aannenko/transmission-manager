using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using TransmissionManager.TorrentWebPages.Constants;
using TransmissionManager.TorrentWebPages.Utils;

namespace TransmissionManager.TorrentWebPages.Options;

public sealed class TorrentWebPageClientOptions
{
    public TorrentWebPageClientOptions()
    {
        DefaultMagnetRegex = new Lazy<Regex>(
            () => RegexUtils.CreateRegex(
                DefaultMagnetRegexPattern!,
                TimeSpan.FromMilliseconds(RegexMatchTimeoutMilliseconds)));
    }

    [Required]
    [RegularExpression(TorrentRegex.IsFindMagnet)]
    public required string DefaultMagnetRegexPattern { get; set; }

    [Required]
    [Range(10, 500)]
    public required int RegexMatchTimeoutMilliseconds { get; set; }

    public Lazy<Regex> DefaultMagnetRegex { get; }
}
