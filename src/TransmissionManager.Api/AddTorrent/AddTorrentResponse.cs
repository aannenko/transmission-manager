using TransmissionManager.Api.Shared.Transmission;

namespace TransmissionManager.Api.AddTorrent;

public readonly record struct AddTorrentResponse(TransmissionAddResult TransmissionResult);
