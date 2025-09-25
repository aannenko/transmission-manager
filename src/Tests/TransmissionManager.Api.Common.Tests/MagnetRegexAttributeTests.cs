using TransmissionManager.Api.Common.Attributes;

namespace TransmissionManager.Api.Common.Tests;

[Parallelizable(ParallelScope.All)]
internal sealed class MagnetRegexAttributeTests
{
    [TestCase("magnet:\\?xt=urn:btih:[^\"]+", true)] // stricter validation rule
    [TestCase("magnet:\\?xt[^\"]+", true)] // looser validation rule
    [TestCase("invalid magnet link", false)] // clearly invalid
    [TestCase("", true)] // empty string is valid (use [Required] to enforce presence)
    [TestCase(null, true)] // null is valid (use [Required] to enforce presence)
    public void IsValid_WithVariousInputs_ReturnsExpected(string? magnet, bool shouldBeValid)
    {
        var attribute = new MagnetRegexAttribute();

        var isValid = attribute.IsValid(magnet);

        Assert.That(isValid, Is.EqualTo(shouldBeValid));
    }
}
