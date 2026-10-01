using TransmissionManager.Web.Validation;

namespace TransmissionManager.Web.Tests.Validation;

[Parallelizable(ParallelScope.All)]
internal sealed class ApiAddressUtilsTests
{
    private const string _nasAddress = "http://nas:9092/";

    [TestCase("nas:9092", "http://nas:9092/")]
    [TestCase("nas", "http://nas/")]
    [TestCase("192.168.1.10:9092", "http://192.168.1.10:9092/")]
    [TestCase("[::1]:9092", "http://[::1]:9092/")]
    public void TryCreate_WhenValueHasNoScheme_AssumesHttp(string value, string expected)
    {
        Assert.That(TryCreateAbsoluteUri(value), Is.EqualTo(expected));
    }

    [TestCase("https://nas", "https://nas/")]
    [TestCase("HTTP://NAS:9092", "http://nas:9092/")]
    public void TryCreate_WhenValueHasAnHttpScheme_KeepsIt(string value, string expected)
    {
        Assert.That(TryCreateAbsoluteUri(value), Is.EqualTo(expected));
    }

    [TestCase("ftp://nas")]
    [TestCase("ws://nas:9092")]
    public void TryCreate_WhenValueHasAnotherScheme_Fails(string value)
    {
        Assert.That(ApiAddressUtils.TryCreate(value, out _), Is.False);
    }

    [TestCase("nas name")]
    [TestCase("nas:99999")]
    [TestCase("nas:port")]
    [TestCase("::1")]
    public void TryCreate_WhenValueIsNotAnAddress_Fails(string value)
    {
        Assert.That(ApiAddressUtils.TryCreate(value, out _), Is.False);
    }

    [TestCase("  nas:9092  ")]
    [TestCase("\u00A0http://nas:9092\u00A0", TestName = "TryCreate_WhenValueIsSurroundedByWhitespace_IgnoresIt(non-breaking spaces)")]
    public void TryCreate_WhenValueIsSurroundedByWhitespace_IgnoresIt(string value)
    {
        Assert.That(TryCreateAbsoluteUri(value), Is.EqualTo(_nasAddress));
    }

    [TestCase("http://nas:9092/tm", "http://nas:9092/tm/")]
    [TestCase("nas:9092/tm/", "http://nas:9092/tm/")]
    [TestCase("https://home.example/apps/tm", "https://home.example/apps/tm/")]
    public void TryCreate_WhenValueHasAPath_KeepsItEndingInASlash(string value, string expected)
    {
        Assert.That(TryCreateAbsoluteUri(value), Is.EqualTo(expected));
    }

    [TestCase("http://nas:9092/tm/?take=5#top")]
    [TestCase("http://user:pass@nas:9092/tm")]
    public void TryCreate_WhenValueHasCredentialsQueryOrFragment_DropsThem(string value)
    {
        Assert.That(TryCreateAbsoluteUri(value), Is.EqualTo("http://nas:9092/tm/"));
    }

    private static string? TryCreateAbsoluteUri(string value) =>
        ApiAddressUtils.TryCreate(value, out var address) ? address.AbsoluteUri : null;
}
