using TransmissionManager.Api.Database.Dto;
using TransmissionManager.Api.Endpoints.Dto;
using TransmissionManager.Api.Endpoints.Extensions;
using TransmissionManager.Api.Trackers.Services;
using TransmissionManager.Api.Transmission.Services;
using Result = TransmissionManager.Api.Endpoints.Dto.AddOrUpdateTorrentResult.ResultType;

namespace TransmissionManager.Api.Endpoints.Services;

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
            return new(Result.Error, -1, string.Format(error, dto.WebPageUri, trackerError));

        var (transmissionTorrent, transmissionError) =
            await SendMagnetToTransmissionAsync(magnetUri, dto.DownloadDir, cancellationToken).ConfigureAwait(false);

        if (transmissionTorrent is null)
            return new(Result.Error, -1, string.Format(error, dto.WebPageUri, transmissionError));

        var torrents = await torrentService.FindPageAsync(new(1, 0), new(dto.WebPageUri), cancellationToken)
            .ConfigureAwait(false);

        var torrentId = torrents.FirstOrDefault()?.Id ?? -1;
        Result resultType;
        TorrentUpdateDto? updateDto = null;
        if (torrentId is -1)
        {
            resultType = Result.Add;
            var addDto = dto.ToTorrentAddDto(transmissionTorrent);
            torrentId = await torrentService.AddOneAsync(addDto, cancellationToken)
                .ConfigureAwait(false);
        }
        else
        {
            resultType = Result.Update;
            updateDto = dto.ToTorrentUpdateDto(transmissionTorrent);
            if (!await torrentService.TryUpdateOneByIdAsync(torrentId, updateDto, cancellationToken)
                .ConfigureAwait(false))
            {
                var formattedError = string.Format(
                    error,
                    dto.WebPageUri,
                    $"Torrent with id {torrentId} was removed before it could be updated.");

                return new(Result.Error, torrentId, formattedError);
            }
        }

        if (transmissionTorrent.HashString == transmissionTorrent.Name)
            _ = StartUpdateTorrentNameTask(torrentId, updateDto ?? dto.ToTorrentUpdateDto(transmissionTorrent));

        return new(resultType, torrentId, null);
    }
}
