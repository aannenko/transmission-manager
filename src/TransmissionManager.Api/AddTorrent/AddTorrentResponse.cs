using TransmissionManager.Api.Common.Transmission;

namespace TransmissionManager.Api.AddTorrent;

public readonly record struct AddTorrentResponse(TransmissionAddResult TransmissionResult);
