﻿using System.Text.RegularExpressions;
using TransmissionManager.TorrentWebPages.Extensions;

namespace TransmissionManager.TorrentWebPages.Tests;

internal sealed partial class RegexExtensionsTests
{
    private const string _abcdefg = "abcdefg";

    [Test]
    public void TryGetFirstMatch_WhenGivenMatchingSpan_ReturnsTrueAndNonEmptyMatchRange()
    {
        var result = CdeRegex().TryGetFirstMatch(_abcdefg, out var range);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.True);
            Assert.That(range, Is.EqualTo(new Range(2, 5)));
        }
    }

    [Test]
    public void TryGetFirstMatch_WhenGivenNonMatchingSpan_ReturnsFalseAndEmptyMatchRange()
    {
        var result = CdfRegex().TryGetFirstMatch(_abcdefg, out var range);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.False);
            Assert.That(range, Is.EqualTo(default(Range)));
        }
    }

    [GeneratedRegex("cde")]
    private static partial Regex CdeRegex();

    [GeneratedRegex("cdf")]
    private static partial Regex CdfRegex();
}
