using TransmissionManager.Api.Services.Transmission;

namespace TransmissionManager.Api.Actions.Torrents.Add;

internal readonly record struct AddTorrentResponse(TransmissionAddResult TransmissionResult);
