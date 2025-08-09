namespace TransmissionManager.Web.Services;

internal sealed class ServerTimeZoneService
{
    public TimeSpan Offset { get; set; } = TimeSpan.Zero;
}
