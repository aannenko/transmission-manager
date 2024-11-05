using TransmissionManager.Api.Common.Transmission;

namespace TransmissionManager.Api.Actions.AddTorrent;

public readonly record struct AddTorrentResponse(TransmissionAddResult TransmissionResult);
