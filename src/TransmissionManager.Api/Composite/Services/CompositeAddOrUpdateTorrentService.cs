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
            await GetMagnetUriAsync(dto.WebPageUri, dto.MagnetRegexPattern, cancellationToken);

        if (string.IsNullOrEmpty(magnetUri))
            return new(AddOrUpdateResult.Error, -1, string.Format(error, dto.WebPageUri, trackerError));

        var (transmissionTorrent, transmissionError) =
            await SendMagnetToTransmissionAsync(magnetUri, dto.DownloadDir, cancellationToken);

        if (transmissionTorrent is null)
            return new(AddOrUpdateResult.Error, -1, string.Format(error, dto.WebPageUri, transmissionError));

        var torrentId = torrentService.FindPage(new(1, 0, WebPageUri: dto.WebPageUri)).FirstOrDefault()?.Id;
        AddOrUpdateResult resultType;
        TorrentUpdateDto? updateDto = null;
        if (torrentId is null)
        {
            resultType = AddOrUpdateResult.Add;
            torrentId = torrentService.AddOne(dto.ToTorrentAddDto(transmissionTorrent));
        }
        else
        {
            resultType = AddOrUpdateResult.Update;
            torrentService.TryUpdateOneById(torrentId.Value, updateDto = dto.ToTorrentUpdateDto(transmissionTorrent));
        }

        if (transmissionTorrent.HashString == transmissionTorrent.Name)
            _ = StartUpdateTorrentNameTask(torrentId.Value, updateDto ?? dto.ToTorrentUpdateDto(transmissionTorrent));

        return new(resultType, torrentId.Value, null);
    }
}
