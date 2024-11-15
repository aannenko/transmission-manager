using System.Text;
using TransmissionManager.Transmission.Services;

namespace TransmissionManager.Api.Common.Transmission;

internal sealed class TransmissionClientWrapper(TransmissionClient transmissionClient)
{
    private static readonly CompositeFormat _addError =
        CompositeFormat.Parse("Could not get a torrent with hash '{0}' from Transmission{1}.");

    private static readonly CompositeFormat _getError =
        CompositeFormat.Parse("Could not add a torrent to Transmission{0}.");

    public async Task<TransmissionAddResponse> AddTorrentUsingMagnetAsync(
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
                    new(TransmissionAddResult.Added, transmissionResponse.Arguments.TorrentAdded, null),
                { TorrentDuplicate: not null } =>
                    new(TransmissionAddResult.Duplicate, transmissionResponse.Arguments.TorrentDuplicate, null),
                _ => new(null, null, string.Format(null, _getError, string.Empty))
            };
        }
        catch (HttpRequestException e)
        {
            return new(null, null, string.Format(null, _getError, $": {e.Message}"));
        }
    }

    public async Task<TransmissionGetResponse> GetTorrentAsync(
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
                ? new(torrent, null)
                : new(null, string.Format(null, _addError, hashString, string.Empty));
        }
        catch (HttpRequestException e)
        {
            return new(null, string.Format(null, _addError, hashString, $": '{e.Message}'"));
        }
    }
}
