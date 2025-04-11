namespace TransmissionManager.Api.Shared.Dto.AppInfo.Get;

public readonly record struct GetAppInfoResponse(Version Version, DateTimeOffset LocalTime);
