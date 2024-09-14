using TransmissionManager.Transmission.Dto;
using TransmissionManager.Transmission.Services;

namespace TransmissionManager.Api.Common.Services;

public sealed class TransmissionService(TransmissionClient transmissionClient)
{
    public async Task<(TransmissionTorrentAddResponseItem? Torrent, string? Error)> SendMagnetToTransmissionAsync(
        string magnetUri,
        string downloadDir,
        CancellationToken cancellationToken)
    {
        TransmissionTorrentAddResponse? transmissionResponse = null;
        var error = string.Empty;
        try
        {
            transmissionResponse = await transmissionClient
                .AddTorrentUsingMagnetUriAsync(magnetUri, downloadDir, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (HttpRequestException e)
        {
            error = $": '{e.Message}'";
        }

        var transmissionTorrent =
            transmissionResponse?.Arguments?.TorrentAdded ?? transmissionResponse?.Arguments?.TorrentDuplicate;

        if (transmissionTorrent is null)
            error = $"Could not add a torrent to Transmission{error}.";

        return (transmissionTorrent, error);
    }

    public async Task<(TransmissionTorrentGetResponseItem? Torrent, string? Error)> GetTorrentFromTransmissionAsync(
        string hashString,
        CancellationToken cancellationToken)
    {
        TransmissionTorrentGetResponse? transmissionResponse = null;
        var error = string.Empty;
        try
        {
            transmissionResponse = await transmissionClient
                .GetTorrentsAsync([hashString], cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }
        catch (HttpRequestException e)
        {
            error = $": '{e.Message}'";
        }

        TransmissionTorrentGetResponseItem? transmissionTorrent = null;
        if (transmissionResponse?.Arguments?.Torrents?.Length is 1)
            transmissionTorrent = transmissionResponse.Arguments.Torrents[0];
        else
            error = $"Could not get a torrent with hash '{hashString}' from Transmission{error}.";

        return (transmissionTorrent, error);
    }
}
