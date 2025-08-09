namespace TransmissionManager.Web.Extensions;

internal static class DateTimeOffsetExtensions
{
    public static string ToServerTimeString(this DateTimeOffset dateTimeOffset) =>
        dateTimeOffset.ToString("yyyy-MM-dd HH:mm:ss (zzz)");
}
