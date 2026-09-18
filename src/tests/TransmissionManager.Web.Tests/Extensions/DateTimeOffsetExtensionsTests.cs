using TransmissionManager.Web.Extensions;

namespace TransmissionManager.Web.Tests.Extensions;

[Parallelizable(ParallelScope.Self)]
internal sealed class DateTimeOffsetExtensionsTests
{
    /// <remarks>
    /// The application runs under invariant globalization, which is what fixes the field order and
    /// the separators asserted here.
    /// </remarks>
    [TestCase(0, "2026-09-17 08:05:03 (+00:00)")]
    [TestCase(2, "2026-09-17 08:05:03 (+02:00)")]
    [TestCase(-5, "2026-09-17 08:05:03 (-05:00)")]
    [TestCase(5.5, "2026-09-17 08:05:03 (+05:30)")]
    public void ToServerTimeString_WhenTheValueCarriesAnOffset_ShowsItBesideTheUnshiftedTime(
        double offsetHours,
        string expected)
    {
        var dateTimeOffset = new DateTimeOffset(2026, 9, 17, 8, 5, 3, TimeSpan.FromHours(offsetHours));

        Assert.That(dateTimeOffset.ToServerTimeString(), Is.EqualTo(expected));
    }

    [Test]
    public void ToServerTimeString_WhenTheValueIsMidnight_KeepsTheHoursOnATwentyFourHourClock()
    {
        var dateTimeOffset = new DateTimeOffset(2026, 12, 31, 0, 0, 0, TimeSpan.Zero);

        Assert.That(dateTimeOffset.ToServerTimeString(), Is.EqualTo("2026-12-31 00:00:00 (+00:00)"));
    }
}
