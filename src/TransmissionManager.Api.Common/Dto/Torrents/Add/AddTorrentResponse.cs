using TransmissionManager.Api.Common.Dto.Transmission;

namespace TransmissionManager.Api.Common.Dto.Torrents.Add;

public readonly record struct AddTorrentResponse(TransmissionAddResult TransmissionResult);
