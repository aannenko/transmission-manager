namespace TransmissionManager.Api.Actions.AppInfo.Get;

internal readonly record struct GetAppInfoResponse(Version Version, DateTimeOffset LocalTime);
