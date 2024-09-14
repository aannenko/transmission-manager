using TransmissionManager.TorrentWebPages.Services;

namespace TransmissionManager.Api.Common.Services;

public sealed class TorrentWebPageService(TorrentWebPageClient torrentWebPageClient)
{
    public async Task<(string? Magnet, string? Error)> GetMagnetUriAsync(
        string webPageUri,
        string? magnetRegexPattern,
        CancellationToken cancellationToken)
    {
        string? magnetUri = null;
        var error = string.Empty;
        try
        {
            magnetUri = await torrentWebPageClient
                .FindMagnetUriAsync(webPageUri, magnetRegexPattern, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception e) when (e is HttpRequestException or ArgumentException or InvalidOperationException)
        {
            error = $": '{e.Message}'";
        }

        return magnetUri is null
            ? (null, $"Could not retrieve a magnet link from '{webPageUri}'{error}.")
            : (magnetUri, null);
    }
}
