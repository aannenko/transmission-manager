using TransmissionManager.Api.Common.Transmission;

namespace TransmissionManager.Api.Actions.AddTorrent;

internal readonly record struct AddTorrentResponse(TransmissionAddResult TransmissionResult);
