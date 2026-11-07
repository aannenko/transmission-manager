using System.Text;
using TransmissionManager.Transmission.Services;

namespace TransmissionManager.Api.Services.Transmission;

internal sealed class TransmissionClientWrapper(TransmissionClient transmissionClient)
{
    private const string _addUnexpectedResponseError =
        "Could not add a torrent to Transmission: its response holds neither torrent-added nor torrent-duplicate.";

    private static readonly CompositeFormat _addRequestFailedError =
        CompositeFormat.Parse("Could not add a torrent to Transmission. {0}");

    private static readonly CompositeFormat _getNotFoundError =
        CompositeFormat.Parse("Could not get a torrent with hash '{0}' from Transmission.");

    private static readonly CompositeFormat _getRequestFailedError =
        CompositeFormat.Parse("Could not get a torrent with hash '{0}' from Transmission. {1}");

    private static readonly CompositeFormat _removeRequestFailedError =
        CompositeFormat.Parse("Could not remove a torrent with hash '{0}' from Transmission. {1}");

    public async Task<TransmissionAddTorrentOutcome> AddTorrentUsingMagnetAsync(
        Uri magnetUri,
        string downloadDir,
        CancellationToken cancellationToken)
    {
        try
        {
            var transmissionResponse = await transmissionClient
                .AddTorrentUsingMagnetUriAsync(magnetUri, downloadDir, cancellationToken)
                .ConfigureAwait(false);

            return transmissionResponse.Arguments switch
            {
                { TorrentAdded: not null } =>
                    new(TransmissionAddTorrentResult.Added, transmissionResponse.Arguments.TorrentAdded, null),
                { TorrentDuplicate: not null } =>
                    new(TransmissionAddTorrentResult.Duplicate, transmissionResponse.Arguments.TorrentDuplicate, null),
                _ => new(TransmissionAddTorrentResult.UnexpectedResponse, null, _addUnexpectedResponseError)
            };
        }
        catch (HttpRequestException e)
        {
            return new(
                TransmissionAddTorrentResult.RequestFailed,
                null,
                string.Format(null, _addRequestFailedError, e.Message));
        }
    }

    public async Task<TransmissionGetTorrentOutcome> GetTorrentAsync(
        string hashString,
        CancellationToken cancellationToken)
    {
        try
        {
            var transmissionResponse = await transmissionClient
                .GetTorrentsAsync([hashString], cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            var torrent = transmissionResponse?.Arguments?.Torrents?.SingleOrDefault();

            return torrent is not null
                ? new(TransmissionGetTorrentResult.Found, torrent, null)
                : new(TransmissionGetTorrentResult.NotFound, null, string.Format(null, _getNotFoundError, hashString));
        }
        catch (HttpRequestException e)
        {
            return new(
                TransmissionGetTorrentResult.RequestFailed,
                null,
                string.Format(null, _getRequestFailedError, hashString, e.Message));
        }
    }

    public async Task<TransmissionRemoveTorrentOutcome> RemoveTorrentAsync(
        string hashString,
        bool deleteData,
        CancellationToken cancellationToken)
    {
        try
        {
            _ = await transmissionClient
                .RemoveTorrentsAsync([hashString], deleteData, cancellationToken)
                .ConfigureAwait(false);

            return new(TransmissionRemoveTorrentResult.Removed, null);
        }
        catch (HttpRequestException e)
        {
            return new(
                TransmissionRemoveTorrentResult.RequestFailed,
                string.Format(null, _removeRequestFailedError, hashString, e.Message));
        }
    }
}
