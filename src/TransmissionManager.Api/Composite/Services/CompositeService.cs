using TransmissionManager.Api.Composite.Extensions;
using TransmissionManager.Api.Database.Abstractions;
using TransmissionManager.Api.Database.Dto;
using TransmissionManager.Api.Endpoints.Dto;
using TransmissionManager.Api.Endpoints.Extensions;
using TransmissionManager.Api.Trackers.Services;
using TransmissionManager.Api.Transmission.Models;
using TransmissionManager.Api.Transmission.Services;

namespace TransmissionManager.Api.Composite.Services;

public sealed class CompositeService<TTorrentService>(
    MagnetUriRetriever magnetRetriever,
    TransmissionClient transmissionClient,
    TTorrentService torrentService)
    where TTorrentService : ITorrentService
{
    private static readonly TorrentGetRequestFields[] _getNameOnlyFieldsArray = [TorrentGetRequestFields.Name];

    public async Task<bool> TryAddOrUpdateTorrentAsync(
        TorrentPostRequest dto,
        CancellationToken cancellationToken = default)
    {
        var magnetUri = await magnetRetriever
            .FindMagnetUri(dto.WebPageUri, dto.MagnetRegexPattern, cancellationToken)
            .ConfigureAwait(false);

        if (magnetUri is null)
            return false;

        var transmissionResponse = await transmissionClient
            .AddTorrentMagnetAsync(magnetUri, dto.DownloadDir, cancellationToken)
            .ConfigureAwait(false);

        var transmissionTorrent =
            transmissionResponse.Arguments?.TorrentAdded ?? transmissionResponse.Arguments?.TorrentDuplicate;

        if (transmissionTorrent is null)
            return false;

        var torrentId = torrentService.FindPage(new(1, 0, WebPageUri: dto.WebPageUri)).SingleOrDefault()?.Id;
        if (torrentId is null)
            torrentId = torrentService.AddOne(dto.ToTorrentAddDto(transmissionTorrent));
        else
            torrentService.UpdateOneById(torrentId.Value, dto.ToTorrentUpdateDto(transmissionTorrent));

        if (transmissionTorrent.HashString == transmissionTorrent.Name)
            _ = UpdateTorrentNameWithRetriesAsync(torrentId.Value, dto.ToTorrentUpdateDto(transmissionTorrent));

        return true;
    }

    public async Task<bool> TryRefreshTorrentAsync(
        long torrentId,
        CancellationToken cancellationToken = default)
    {
        var torrent = torrentService.FindOneById(torrentId);

        if (torrent is null)
            return false;

        var transmissionGetResponse = await transmissionClient
            .GetTorrentsAsync([torrent.TransmissionId], _getNameOnlyFieldsArray, cancellationToken)
            .ConfigureAwait(false);

        if (transmissionGetResponse.Arguments?.Torrents?.Length is null or 0)
            return false;

        var magnetUri = await magnetRetriever
            .FindMagnetUri(torrent.WebPageUri, torrent.MagnetRegexPattern, cancellationToken)
            .ConfigureAwait(false);

        if (magnetUri is null)
            return false;

        var transmissionResponse = await transmissionClient
            .AddTorrentMagnetAsync(magnetUri, torrent.DownloadDir, cancellationToken)
            .ConfigureAwait(false);

        var transmissionAddTorrent =
            transmissionResponse.Arguments?.TorrentAdded ?? transmissionResponse.Arguments?.TorrentDuplicate;

        if (transmissionAddTorrent is null)
            return false;

        var updateDto = torrent.ToTorrentUpdateDto(transmissionAddTorrent);
        torrentService.UpdateOneById(torrent.Id, updateDto);

        if (transmissionAddTorrent.HashString == transmissionAddTorrent.Name)
            _ = UpdateTorrentNameWithRetriesAsync(torrentId, updateDto);

        return true;
    }

    private async Task UpdateTorrentNameWithRetriesAsync(long id, TorrentUpdateDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto?.TransmissionId);

        long[] singleTransmissionIdArray = [dto.TransmissionId.Value];
        for (int i = 1; i <= 10; i++)
        {
            await Task.Delay(TimeSpan.FromSeconds(i * i)).ConfigureAwait(false);

            var transmissionResponse = await transmissionClient
                .GetTorrentsAsync(singleTransmissionIdArray, _getNameOnlyFieldsArray)
                .ConfigureAwait(false);

            var newName = transmissionResponse.Arguments?.Torrents?.SingleOrDefault()?.Name;
            if (string.IsNullOrEmpty(newName))
            {
                return;
            }
            else if (dto.Name != newName)
            {
                dto.Name = newName;
                torrentService.UpdateOneById(id, dto);
                return;
            }
        }
    }
}
