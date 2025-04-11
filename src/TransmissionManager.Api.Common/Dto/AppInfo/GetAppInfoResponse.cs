namespace TransmissionManager.Api.Common.Dto.AppInfo;

public readonly record struct GetAppInfoResponse(Version Version, DateTimeOffset LocalTime);
