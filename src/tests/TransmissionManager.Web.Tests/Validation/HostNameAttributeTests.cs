using TransmissionManager.Web.Validation;

namespace TransmissionManager.Web.Tests.Validation;

[Parallelizable(ParallelScope.All)]
internal sealed class HostNameAttributeTests
{
    private static readonly HostNameAttribute _attribute = new();

    [TestCase("transmission")]
    [TestCase("transmission.local")]
    [TestCase("192.168.1.10")]
    [TestCase("::1")]
    public void IsValid_WhenValueIsAHostName_ReturnsTrue(string value)
    {
        Assert.That(_attribute.IsValid(value), Is.True);
    }

    [TestCase("", TestName = "IsValid_WhenValueIsNotAHostName_ReturnsFalse(empty string)")]
    [TestCase(" ", TestName = "IsValid_WhenValueIsNotAHostName_ReturnsFalse(whitespace)")]
    [TestCase("transmission local")]
    [TestCase("http://transmission")]
    [TestCase("transmission:9091")]
    public void IsValid_WhenValueIsNotAHostName_ReturnsFalse(string value)
    {
        Assert.That(_attribute.IsValid(value), Is.False);
    }

    /// <remarks>
    /// Absence is <c>[Required]</c>'s business, and the two sit together on the field they guard.
    /// </remarks>
    [Test]
    public void IsValid_WhenValueIsNull_ReturnsTrue()
    {
        Assert.That(_attribute.IsValid(null), Is.True);
    }

    [Test]
    public void IsValid_WhenValueIsNotAString_ReturnsFalse()
    {
        Assert.That(_attribute.IsValid(42), Is.False);
    }
}
