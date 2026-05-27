using TransmissionManager.Api.Common.Attributes;

namespace TransmissionManager.Api.Common.Tests;

[Parallelizable(ParallelScope.All)]
internal sealed class CronAttributeTests
{
    [TestCase("* * * * *", true)] // every minute
    [TestCase("0 1,2,3 * * *", true)] // at 1, 2, and 3 minutes past the hour
    [TestCase("0 1-5 * * *", true)] // at 1 through 5 minutes past the hour
    [TestCase("*/5 * * * *", false)] // step syntax not supported by Coravel
    [TestCase("0 0 * *", false)] // too few fields
    [TestCase("0 0 0 * * *", false)] // too many fields
    [TestCase("0 1,a * * *", false)] // non-numeric in list
    [TestCase("", true)] // empty string is valid (use [Required] to enforce presence)
    [TestCase(null, true)] // null is valid (use [Required] to enforce presence)
    public void IsValid_WithVariousInputs_ReturnsExpected(string? cron, bool shouldBeValid)
    {
        var attribute = new CronAttribute();

        var isValid = attribute.IsValid(cron);

        Assert.That(isValid, Is.EqualTo(shouldBeValid));
    }
}
