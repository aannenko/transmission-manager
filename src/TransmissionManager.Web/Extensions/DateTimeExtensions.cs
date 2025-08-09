using TransmissionManager.Web.Services;

namespace TransmissionManager.Web.Extensions;

internal static class DateTimeExtensions
{
    public static ServerTimeZoneService? ServerTimeZoneService { get; set; }

    public static string ToServerTimeString(this DateTime dateTime)
    {
        return new DateTimeOffset(dateTime)
            .ToOffset(ServerTimeZoneService?.Offset ?? TimeSpan.Zero)
            .ToServerTimeString();
    }
}
