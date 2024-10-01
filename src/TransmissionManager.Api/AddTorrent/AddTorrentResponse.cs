using static TransmissionManager.Api.Common.Services.TransmissionClientWrapper;

namespace TransmissionManager.Api.AddTorrent;

public readonly record struct AddTorrentResponse(TransmissionAddResult TransmissionResult);
