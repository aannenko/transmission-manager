using TransmissionManager.Api.Common.Dto.Transmission;

namespace TransmissionManager.Api.Common.Dto.Torrents;

public sealed record AddTorrentResponse(TorrentDto TorrentDto, TransmissionAddResult TransmissionResult);
