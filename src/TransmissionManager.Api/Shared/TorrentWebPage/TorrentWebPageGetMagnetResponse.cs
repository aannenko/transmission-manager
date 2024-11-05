namespace TransmissionManager.Api.Shared.TorrentWebPage;

public readonly record struct TorrentWebPageGetMagnetResponse(Uri? MagnetUri, string? Error);
