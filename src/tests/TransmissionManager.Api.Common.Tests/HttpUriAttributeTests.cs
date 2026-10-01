using TransmissionManager.Api.Common.Attributes;

namespace TransmissionManager.Api.Common.Tests;

[Parallelizable(ParallelScope.All)]
internal sealed class HttpUriAttributeTests
{
    [TestCase("https://torrentTracker.com/forum/viewtopic.php?t=1", UriKind.Absolute, true)]
    [TestCase("http://torrentTracker.com/forum/viewtopic.php?t=1", UriKind.Absolute, true)]
    [TestCase("/forum/viewtopic.php?t=1", UriKind.Relative, false)]
    [TestCase("forum/viewtopic.php", UriKind.Relative, false)]
    [TestCase("ftp://torrentTracker.com/file", UriKind.Absolute, false)]
    [TestCase("file:///c:/torrents/page.html", UriKind.Absolute, false)]
    public void IsValid_WithVariousUris_ReturnsExpected(string address, UriKind uriKind, bool shouldBeValid)
    {
        var attribute = new HttpUriAttribute();

        var isValid = attribute.IsValid(new Uri(address, uriKind));

        Assert.That(isValid, Is.EqualTo(shouldBeValid));
    }

    [Test]
    public void IsValid_WithNull_ReturnsTrue()
    {
        var attribute = new HttpUriAttribute();

        var isValid = attribute.IsValid(null);

        Assert.That(isValid, Is.True); // null is valid, use [Required] to enforce presence
    }

    [TestCase("https://torrentTracker.com/forum/viewtopic.php?t=1", true)]
    [TestCase("http://torrentTracker.com/forum/viewtopic.php?t=1", true)]
    [TestCase(
        "\u00A0https://torrentTracker.com/forum/viewtopic.php?t=1\u00A0",
        true,
        TestName = "IsValid_WithVariousStrings_ReturnsExpected(non-breaking spaces around an http address)")]
    [TestCase("torrentTracker.com/forum/viewtopic.php", false)]
    [TestCase("ftp://torrentTracker.com/file", false)]
    public void IsValid_WithVariousStrings_ReturnsExpected(string address, bool shouldBeValid)
    {
        var attribute = new HttpUriAttribute();

        var isValid = attribute.IsValid(address);

        Assert.That(isValid, Is.EqualTo(shouldBeValid));
    }

    [Test]
    public void IsValid_WithAValueOfAnotherType_ReturnsFalse()
    {
        var attribute = new HttpUriAttribute();

        var isValid = attribute.IsValid(42);

        Assert.That(isValid, Is.False);
    }
}
