using System.ComponentModel.DataAnnotations;
using TransmissionManager.Api.Common.Dto.Torrents;
using TransmissionManager.Web.Forms;

namespace TransmissionManager.Web.Tests.Forms;

[Parallelizable(ParallelScope.Self)]
internal sealed class AddTorrentFormTests
{
    [Test]
    public void SwitchSourceKind_WhenSwitchingBetweenKinds_RestoresEachKindsDraft()
    {
        var form = new AddTorrentForm
        {
            SourceUri = "https://tracker.example/topic/1",
            MagnetRegexPattern = @"magnet:\?xt=urn:btih:[^""]+",
        };

        Assert.That(form.SwitchSourceKind(TorrentSourceKind.JsonPointer), Is.True);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(form.SourceUri, Is.Empty);
            Assert.That(form.MagnetRegexPattern, Is.Empty);
            Assert.That(form.JsonValueFormat, Is.Empty);
        }

        form.SourceUri = "https://api.example/topics#/result/1/hash";
        form.MagnetRegexPattern = "[a-fA-F0-9]{40}";
        form.JsonValueFormat = "magnet:?xt=urn:btih:{0}";

        _ = form.SwitchSourceKind(TorrentSourceKind.WebPage);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(form.SourceUri, Is.EqualTo("https://tracker.example/topic/1"));
            Assert.That(form.MagnetRegexPattern, Is.EqualTo(@"magnet:\?xt=urn:btih:[^""]+"));
            Assert.That(form.JsonValueFormat, Is.Empty);
        }

        _ = form.SwitchSourceKind(TorrentSourceKind.JsonPointer);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(form.SourceUri, Is.EqualTo("https://api.example/topics#/result/1/hash"));
            Assert.That(form.MagnetRegexPattern, Is.EqualTo("[a-fA-F0-9]{40}"));
            Assert.That(form.JsonValueFormat, Is.EqualTo("magnet:?xt=urn:btih:{0}"));
        }
    }

    [Test]
    public void CreateRequest_WhenOptionalValuesAreBlank_NormalizesThemToNull()
    {
        var form = new AddTorrentForm
        {
            SourceUri = "  https://api.example/topics#/result/1/hash  ",
            DownloadDir = "  /tvshows  ",
            MagnetRegexPattern = "  ",
            JsonValueFormat = "  ",
            Cron = "  ",
        };
        _ = form.SwitchSourceKind(TorrentSourceKind.JsonPointer);
        form.SourceUri = "  https://api.example/topics#/result/1/hash  ";
        form.MagnetRegexPattern = "  ";
        form.JsonValueFormat = "  ";

        form.Normalize();
        var request = form.CreateRequest();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(request.SourceUri.OriginalString, Is.EqualTo("https://api.example/topics#/result/1/hash"));
            Assert.That(request.SourceKind, Is.EqualTo(TorrentSourceKind.JsonPointer));
            Assert.That(request.DownloadDir, Is.EqualTo("/tvshows"));
            Assert.That(request.MagnetRegexPattern, Is.Null);
            Assert.That(request.JsonValueFormat, Is.Null);
            Assert.That(request.Cron, Is.Null);
        }
    }

    [Test]
    public void CreateRequest_WhenWebPageFormatHasAValue_DoesNotSendTheUnusedValue()
    {
        var form = new AddTorrentForm
        {
            SourceUri = "https://tracker.example/topic/1",
            JsonValueFormat = "magnet:?xt=urn:btih:{0}",
        };

        form.Normalize();
        var request = form.CreateRequest();

        Assert.That(request.JsonValueFormat, Is.Null);
    }

    [Test]
    public void Validate_WhenPatternExtractsAHash_AcceptsItOnlyForJsonPointer()
    {
        var form = new AddTorrentForm
        {
            SourceUri = "https://tracker.example/topic/1",
            MagnetRegexPattern = "[a-fA-F0-9]{40}",
        };

        var webPageResults = Validate(form);

        _ = form.SwitchSourceKind(TorrentSourceKind.JsonPointer);
        form.SourceUri = "https://api.example/topics#/result/1/hash";
        form.MagnetRegexPattern = "[a-fA-F0-9]{40}";
        var jsonPointerResults = Validate(form);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(
                webPageResults.SelectMany(static result => result.MemberNames),
                Does.Contain(nameof(AddTorrentForm.MagnetRegexPattern)));

            Assert.That(jsonPointerResults, Is.Empty);
        }
    }

    private static List<ValidationResult> Validate(AddTorrentForm form)
    {
        var results = new List<ValidationResult>();
        _ = Validator.TryValidateObject(form, new(form), results, validateAllProperties: true);
        return results;
    }
}
