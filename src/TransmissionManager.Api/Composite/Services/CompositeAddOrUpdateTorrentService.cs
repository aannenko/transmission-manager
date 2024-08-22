using TransmissionManager.Api.Composite.Dto;
using TransmissionManager.Api.Composite.Extensions;
using TransmissionManager.Api.Database.Dto;
using TransmissionManager.Api.Endpoints.Dto;
using TransmissionManager.Api.Trackers.Services;
using TransmissionManager.Api.Transmission.Services;
using AddOrUpdateResult = TransmissionManager.Api.Composite.Dto.AddOrUpdateTorrentResult.ResultType;

namespace TransmissionManager.Api.Composite.Services;

public sealed class CompositeAddOrUpdateTorrentService(
    MagnetUriRetriever magnetRetriever,
    TransmissionClient transmissionClient,
    SchedulableTorrentService torrentService,
    BackgroundTaskService backgroundTaskService)
    : BaseCompositeTorrentService(magnetRetriever, transmissionClient, backgroundTaskService)
{
    public async Task<AddOrUpdateTorrentResult> AddOrUpdateTorrentAsync(
        TorrentPostRequest dto,
        CancellationToken cancellationToken = default)
    {
        const string error = "Addition or update of a torrent from the web page '{0}' has failed: {1}.";
        var (magnetUri, trackerError) =
            await GetMagnetUriAsync(dto.WebPageUri, dto.MagnetRegexPattern, cancellationToken).ConfigureAwait(false);

        if (string.IsNullOrEmpty(magnetUri))
            return new(AddOrUpdateResult.Error, -1, string.Format(error, dto.WebPageUri, trackerError));

        var (transmissionTorrent, transmissionError) =
            await SendMagnetToTransmissionAsync(magnetUri, dto.DownloadDir, cancellationToken).ConfigureAwait(false);

        if (transmissionTorrent is null)
            return new(AddOrUpdateResult.Error, -1, string.Format(error, dto.WebPageUri, transmissionError));

        var torrents = await torrentService.FindPageAsync(new(1, 0, WebPageUri: dto.WebPageUri)).ConfigureAwait(false);
        var torrentId = torrents.FirstOrDefault()?.Id;
        AddOrUpdateResult resultType;
        TorrentUpdateDto? updateDto = null;
        if (torrentId is null)
        {
            resultType = AddOrUpdateResult.Add;
            var addDto = dto.ToTorrentAddDto(transmissionTorrent);
            torrentId = await torrentService.AddOneAsync(addDto).ConfigureAwait(false);
        }
        else
        {
            resultType = AddOrUpdateResult.Update;
            updateDto = dto.ToTorrentUpdateDto(transmissionTorrent);
            await torrentService.TryUpdateOneByIdAsync(torrentId.Value, updateDto).ConfigureAwait(false);
        }

        if (transmissionTorrent.HashString == transmissionTorrent.Name)
            _ = StartUpdateTorrentNameTask(torrentId.Value, updateDto ?? dto.ToTorrentUpdateDto(transmissionTorrent));

        return new(resultType, torrentId.Value, null);
    }
}
