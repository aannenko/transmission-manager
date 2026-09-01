namespace TransmissionManager.TorrentSources.Tests;

[Parallelizable(ParallelScope.All)]
internal sealed class RemoteTextUtilsTests
{
    [Test]
    public void Summarize_WhenTextIsPrintableAndWithinTheBudget_ReturnsItUnchanged() =>
        Assert.That(RemoteTextUtils.Summarize("just a moment..."), Is.EqualTo("just a moment..."));

    [Test]
    public void Summarize_WhenTextIsEmpty_ReturnsEmpty() =>
        Assert.That(RemoteTextUtils.Summarize(string.Empty), Is.Empty);

    [TestCase(
        "before\r\nafter",
        "before__after",
        TestName = "Summarize_WhenTextHoldsControlCharacters_ReplacesThem(CR LF)")]
    [TestCase(
        "red\u001b[31mtext",
        "red_[31mtext",
        TestName = "Summarize_WhenTextHoldsControlCharacters_ReplacesThem(ESC)")]
    [TestCase(
        "a\0b\tc",
        "a_b_c",
        TestName = "Summarize_WhenTextHoldsControlCharacters_ReplacesThem(NUL and TAB)")]
    public void Summarize_WhenTextHoldsControlCharacters_ReplacesThem(string value, string expected) =>
        Assert.That(RemoteTextUtils.Summarize(value), Is.EqualTo(expected));

    /// <remarks>
    /// The property the whole helper exists for: a source's bytes reach a log line, where a control
    /// character is structure rather than data - a newline forges a record, an escape sequence drives
    /// the operator's terminal. Nothing in the summary may be one, whatever was served.
    /// </remarks>
    [Test]
    public void Summarize_WhenTextIsNothingButControlCharacters_ReturnsNoneOfThem()
    {
        var served = string.Concat(Enumerable.Range(0, char.MaxValue + 1)
            .Select(static code => (char)code)
            .Where(char.IsControl));

        var summary = RemoteTextUtils.Summarize(served);

        Assert.That(summary, Is.EqualTo(new string('_', served.Length)));
    }

    /// <remarks>
    /// The same property where it is easiest to get wrong: control characters interleaved with text,
    /// so a summary that stopped sanitizing early would still look plausible.
    /// </remarks>
    [Test]
    public void Summarize_WhenControlCharactersAreInterleavedWithText_KeepsNoneOfThem()
    {
        var served = string.Concat(Enumerable.Repeat("a\r\n\u001b", 100));

        var summary = RemoteTextUtils.Summarize(served);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(summary.Any(char.IsControl), Is.False);
            Assert.That(
                summary,
                Is.EqualTo(string.Concat(Enumerable.Repeat("a___", RemoteTextUtils.DefaultSummaryLength / 4)) + "..."));
        }
    }

    [Test]
    public void Summarize_WhenTextExceedsTheBudget_ElidesTheRest()
    {
        var served = new string('x', RemoteTextUtils.DefaultSummaryLength + 1);

        var summary = RemoteTextUtils.Summarize(served);

        Assert.That(summary, Is.EqualTo(new string('x', RemoteTextUtils.DefaultSummaryLength) + "..."));
    }

    [Test]
    public void Summarize_WhenTextIsExactlyTheBudget_DoesNotElide()
    {
        var served = new string('x', RemoteTextUtils.DefaultSummaryLength);

        var summary = RemoteTextUtils.Summarize(served);

        Assert.That(summary, Is.EqualTo(served));
    }

    /// <remarks>
    /// The bound on <c>maxLength</c> is what keeps the stack allocation off a cliff: measured on a
    /// thread pool thread, which is where a request and a scheduled refresh both run, a length near
    /// a million overflows the stack - and a stack overflow can be neither caught nor logged, the
    /// process simply dies, which unattended on the cron path takes the container with it.
    /// </remarks>
    [TestCase(0)]
    [TestCase(-1)]
    [TestCase(101)]
    [TestCase(int.MaxValue)]
    public void Summarize_WhenMaxLengthIsOutOfRange_Throws(int maxLength) =>
        Assert.That(
            () => RemoteTextUtils.Summarize("served text", maxLength),
            Throws.TypeOf<ArgumentOutOfRangeException>());

    [Test]
    public void Summarize_WhenMaxLengthIsBelowTheDefault_NarrowsToIt() =>
        Assert.That(RemoteTextUtils.Summarize("abcdefghij", 4), Is.EqualTo("abcd..."));

    [Test]
    public void Summarize_WhenMaxLengthIsBelowTheDefaultAndTextFits_DoesNotElide() =>
        Assert.That(RemoteTextUtils.Summarize("abc", 4), Is.EqualTo("abc"));

    /// <remarks>
    /// The ends of the accepted range, which a guard is as easy to get wrong at as it is outside it.
    /// </remarks>
    [TestCase(1, "a...")]
    [TestCase(100, "abc")]
    public void Summarize_WhenMaxLengthIsAtAnAcceptedBoundary_Summarizes(int maxLength, string expected) =>
        Assert.That(RemoteTextUtils.Summarize("abc", maxLength), Is.EqualTo(expected));

    /// <remarks>
    /// Length is quoted alongside the summary by its callers, so a served value of any size costs a
    /// bounded amount of message - this pins that the bound holds for a value far past the buffer.
    /// </remarks>
    [Test]
    public void Summarize_WhenTextIsEnormous_StaysBounded()
    {
        var summary = RemoteTextUtils.Summarize(new string('x', 100_000));

        Assert.That(summary, Has.Length.EqualTo(RemoteTextUtils.DefaultSummaryLength + 3));
    }

    /// <remarks>
    /// The budget is a UTF-16 index, so it can fall between the halves of a surrogate pair. Keeping
    /// the high half alone would hand every consumer downstream an ill-formed string - measured
    /// harmless today, since System.Text.Json escapes it as U+FFFD rather than throwing, but only
    /// by luck.
    /// </remarks>
    [Test]
    public void Summarize_WhenTheBudgetSplitsASurrogatePair_DropsTheStrandedHalf()
    {
        var kept = new string('x', RemoteTextUtils.DefaultSummaryLength - 1);
        var served = $"{kept}\U0001F600tail";

        var summary = RemoteTextUtils.Summarize(served);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(summary, Is.EqualTo(kept + "..."));
            Assert.That(summary.Any(char.IsSurrogate), Is.False);
        }
    }

    /// <remarks>
    /// The other side of the same guard: a pair that fits must survive whole, or the trim would eat
    /// the last character of every summary ending in one.
    /// </remarks>
    [Test]
    public void Summarize_WhenASurrogatePairFitsWithinTheBudget_KeepsItWhole()
    {
        const string served = "no magnet here \U0001F600";

        Assert.That(RemoteTextUtils.Summarize(served), Is.EqualTo(served));
    }
}
