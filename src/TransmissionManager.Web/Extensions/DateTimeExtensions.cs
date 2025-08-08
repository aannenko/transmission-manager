using TransmissionManager.Web.Services;

namespace TransmissionManager.Web.Extensions;

internal static class DateTimeExtensions
{
    public static TransmissionManagerInfoProvider? InfoProvider { get; set; }

    public static string ToLocalTimeString(this DateTime dateTime)
    {
        return new DateTimeOffset(dateTime)
            .ToOffset(InfoProvider?.Offset ?? TimeSpan.Zero)
            .ToLocalTimeString();
    }
}
