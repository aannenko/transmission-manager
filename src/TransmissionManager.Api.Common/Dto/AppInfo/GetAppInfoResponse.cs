namespace TransmissionManager.Api.Common.Dto.AppInfo;

public sealed record GetAppInfoResponse(Version Version, DateTimeOffset LocalTime);
