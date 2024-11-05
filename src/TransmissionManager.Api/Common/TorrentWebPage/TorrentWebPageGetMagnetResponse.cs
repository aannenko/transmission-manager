namespace TransmissionManager.Api.Common.TorrentWebPage;

public readonly record struct TorrentWebPageGetMagnetResponse(Uri? MagnetUri, string? Error);
