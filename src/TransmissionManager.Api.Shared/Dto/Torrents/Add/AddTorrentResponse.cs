using TransmissionManager.Api.Shared.Dto.Transmission;

namespace TransmissionManager.Api.Shared.Dto.Torrents.Add;

public readonly record struct AddTorrentResponse(TransmissionAddResult TransmissionResult);
