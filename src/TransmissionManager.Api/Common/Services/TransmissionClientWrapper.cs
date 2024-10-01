using System.Text.Json.Serialization;
using TransmissionManager.Transmission.Dto;
using TransmissionManager.Transmission.Services;

namespace TransmissionManager.Api.Common.Services;

public sealed class TransmissionClientWrapper(TransmissionClient transmissionClient)
{
    [JsonConverter(typeof(JsonStringEnumConverter<TransmissionAddResult>))]
    public enum TransmissionAddResult
    {
        Added,
        Duplicate
    }

    public readonly record struct TransmissionAddResponse(
        TransmissionAddResult? Result,
        TransmissionTorrentAddResponseItem? Torrent,
        string? Error);

    public readonly record struct TransmissionGetResponse(
        TransmissionTorrentGetResponseItem? Torrent,
        string? Error);

    public async Task<TransmissionAddResponse> AddTorrentUsingMagnetAsync(
        string magnetUri,
        string downloadDir,
        CancellationToken cancellationToken)
    {
        const string error = "Could not add a torrent to Transmission{0}.";
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
                _ => new(null, null, string.Format(error, string.Empty))
            };
        }
        catch (HttpRequestException e)
        {
            return new(null, null, string.Format(error, $": {e.Message}"));
        }
    }

    public async Task<TransmissionGetResponse> GetTorrentAsync(
        string hashString,
        CancellationToken cancellationToken)
    {
        const string error = "Could not get a torrent with hash '{0}' from Transmission{1}.";
        try
        {
            var transmissionResponse = await transmissionClient
                .GetTorrentsAsync([hashString], cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            var torrent = transmissionResponse?.Arguments?.Torrents?.SingleOrDefault();

            return torrent is not null
                ? new(torrent, null)
                : new(null, string.Format(error, hashString, string.Empty));
        }
        catch (HttpRequestException e)
        {
            return new(null, string.Format(error, hashString, $": '{e.Message}'"));
        }
    }
}
