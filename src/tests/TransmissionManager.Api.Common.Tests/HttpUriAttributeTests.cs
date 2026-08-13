using TransmissionManager.Api.Common.Attributes;

namespace TransmissionManager.Api.Common.Tests;

[Parallelizable(ParallelScope.All)]
internal sealed class HttpUriAttributeTests
{
    [TestCase("https://torrentTracker.com/forum/viewtopic.php?t=1", UriKind.Absolute, true)]
    [TestCase("http://torrentTracker.com/forum/viewtopic.php?t=1", UriKind.Absolute, true)]
    [TestCase("/forum/viewtopic.php?t=1", UriKind.Relative, false)] // relative, unfetchable
    [TestCase("forum/viewtopic.php", UriKind.Relative, false)] // relative, unfetchable
    [TestCase("ftp://torrentTracker.com/file", UriKind.Absolute, false)] // absolute but not web
    [TestCase("file:///c:/torrents/page.html", UriKind.Absolute, false)] // absolute but not web
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

    [Test]
    public void IsValid_WithANonUriValue_ReturnsFalse()
    {
        var attribute = new HttpUriAttribute();

        var isValid = attribute.IsValid("https://torrentTracker.com");

        Assert.That(isValid, Is.False);
    }
}
