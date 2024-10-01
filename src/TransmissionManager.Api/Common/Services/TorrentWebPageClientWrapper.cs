using TransmissionManager.TorrentWebPages.Services;

namespace TransmissionManager.Api.Common.Services;

public sealed class TorrentWebPageClientWrapper(TorrentWebPageClient torrentWebPageClient)
{
    public readonly record struct TorrentWebPageGetMagnetResponse(string? MagnetUri, string? Error);

    public async Task<TorrentWebPageGetMagnetResponse> GetMagnetUriAsync(
        Uri webPageUri,
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
            ? new(null, $"Could not retrieve a magnet link from '{webPageUri}'{error}.")
            : new(magnetUri, null);
    }
}
