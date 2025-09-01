using TransmissionManager.Api.Common.Dto.Transmission;

namespace TransmissionManager.Api.Common.Dto.Torrents;

public sealed record RefreshTorrentByIdResponse(
    TorrentDto TorrentDto,
    TransmissionAddResult TransmissionResult);
