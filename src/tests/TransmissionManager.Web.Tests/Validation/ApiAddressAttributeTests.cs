using TransmissionManager.Web.Validation;

namespace TransmissionManager.Web.Tests.Validation;

[Parallelizable(ParallelScope.All)]
internal sealed class ApiAddressAttributeTests
{
    private static readonly ApiAddressAttribute _attribute = new();

    [Test]
    public void IsValid_WhenValueIsAnAddress_ReturnsTrue()
    {
        Assert.That(_attribute.IsValid("nas:9092"), Is.True);
    }

    [Test]
    public void IsValid_WhenValueIsNotAnAddress_ReturnsFalse()
    {
        Assert.That(_attribute.IsValid("ftp://nas"), Is.False);
    }

    /// <remarks>Absence is <c>[Required]</c>'s business.</remarks>
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
