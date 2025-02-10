using TransmissionManager.Database.Dto;
using TransmissionManager.Database.Models;
using TransmissionManager.Database.Services;

namespace TransmissionManager.Database.Tests;

[Parallelizable(ParallelScope.Self)]
internal sealed class TorrentServiceQueryTests : BaseTorrentServiceTests
{
    [Test]
    public async Task FindOneByIdAsync_WhenGivenExistingTorrentId_ReturnsTorrent()
    {
        using var context = CreateContext();
        var service = new TorrentService(context);

        var torrent = await service.FindOneByIdAsync(2).ConfigureAwait(false);

        AssertTorrent(torrent, _initialTorrents[1], 2);
    }

    [Test]
    public async Task FindOneByIdAsync_WhenGivenNonExistentTorrentId_ReturnsNull()
    {
        using var context = CreateContext();
        var service = new TorrentService(context);

        var torrent = await service.FindOneByIdAsync(-1).ConfigureAwait(false);

        Assert.That(torrent, Is.Null);
    }

    [Test]
    public async Task FindPageAsync_WhenGivenDefaultParameters_ReturnsArrayOfTorrents()
    {
        using var context = CreateContext();
        var service = new TorrentService(context);

        var torrents = await service.FindPageAsync<long>(new(TorrentOrder.Id, 5, 0)).ConfigureAwait(false);

        AssertMultipleTorrents(torrents, _initialTorrents, [1, 2, 3]);
    }

    [Test]
    public async Task FindPageAsync_WhenGivenParameterTakeEqualToTwo_ReturnsArrayWithTwoTorrents()
    {
        using var context = CreateContext();
        var service = new TorrentService(context);

        var torrents = await service.FindPageAsync<long>(new(TorrentOrder.Id, 2, 0)).ConfigureAwait(false);

        AssertMultipleTorrents(torrents, _initialTorrents[..^1], [1, 2]);
    }

    [Test]
    public async Task FindPageAsync_WhenGivenParameterAfterIdEqualToOne_ReturnsArrayOfTorrentsWithIdGreaterThanOne()
    {
        using var context = CreateContext();
        var service = new TorrentService(context);

        var torrents = await service.FindPageAsync<long>(new(TorrentOrder.Id, 5, 1)).ConfigureAwait(false);

        AssertMultipleTorrents(torrents, _initialTorrents[1..], [2, 3]);
    }

    [Test]
    public async Task FindPageAsync_WhenGivenParameterAfterIdGreaterThanAnyTorrentId_ReturnsEmptyArray()
    {
        using var context = CreateContext();
        var service = new TorrentService(context);

        var torrents = await service.FindPageAsync<long>(new(TorrentOrder.Id, 5, 10)).ConfigureAwait(false);

        AssertMultipleTorrents(torrents, [], []);
    }

    [Test]
    public async Task FindPageAsync_WhenGivenParameterAfterIdEqualToNegativeValue_ReturnsArrayOfTorrents()
    {
        using var context = CreateContext();
        var service = new TorrentService(context);

        var torrents = await service.FindPageAsync<long>(new(TorrentOrder.Id, 5, long.MinValue)).ConfigureAwait(false);

        AssertMultipleTorrents(torrents, _initialTorrents, [1, 2, 3]);
    }

    [Test]
    public async Task FindPageAsync_WhenGivenFullHashStringFilter_ReturnsFilteredArrayOfTorrents()
    {
        using var context = CreateContext();
        var service = new TorrentService(context);

        var torrents = await service.FindPageAsync<long>(new(TorrentOrder.Id, 2, 0), new(_initialTorrents[1].HashString))
            .ConfigureAwait(false);

        AssertMultipleTorrents(torrents, _initialTorrents[1..^1], [2]);
    }

    [Test]
    public async Task FindPageAsync_WhenGivenFullUppercasedHashStringFilter_ReturnsFilteredArrayOfTorrents()
    {
        using var context = CreateContext();
        var service = new TorrentService(context);

        var torrents = await service
            .FindPageAsync<long>(new(TorrentOrder.Id, 2, 0), new(_initialTorrents[1].HashString.ToUpperInvariant()))
            .ConfigureAwait(false);

        AssertMultipleTorrents(torrents, _initialTorrents[1..^1], [2]);
    }

    [Test]
    public async Task FindPageAsync_WhenGivenPartialHashStringFilter_ReturnsFilteredArrayOfTorrents()
    {
        using var context = CreateContext();
        var service = new TorrentService(context);

        var torrents = await service
            .FindPageAsync<long>(new(TorrentOrder.Id, 2, 0), new(_initialTorrents[1].HashString[..20]))
            .ConfigureAwait(false);

        AssertMultipleTorrents(torrents, _initialTorrents[1..^1], [2]);
    }

    [Test]
    public async Task FindPageAsync_WhenGivenFullWebPageUriFilter_ReturnsFilteredArrayOfTorrents()
    {
        using var context = CreateContext();
        var service = new TorrentService(context);

        var torrents = await service
            .FindPageAsync<long>(new(TorrentOrder.Id, 2, 0), new(new(_initialTorrents[1].WebPageUri)))
            .ConfigureAwait(false);

        AssertMultipleTorrents(torrents, _initialTorrents[1..^1], [2]);
    }

    [Test]
    public async Task FindPageAsync_WhenGivenFullUppercasedWebPageUriFilter_ReturnsFilteredArrayOfTorrents()
    {
        using var context = CreateContext();
        var service = new TorrentService(context);

        var torrents = await service
            .FindPageAsync<long>(new(TorrentOrder.Id, 2, 0), new(_initialTorrents[1].WebPageUri.ToUpperInvariant()))
            .ConfigureAwait(false);

        AssertMultipleTorrents(torrents, _initialTorrents[1..^1], [2]);
    }

    [Test]
    public async Task FindPageAsync_WhenGivenPartialWebPageUriFilter_ReturnsFilteredArrayOfTorrents()
    {
        using var context = CreateContext();
        var service = new TorrentService(context);

        var torrents = await service
            .FindPageAsync<long>(new(TorrentOrder.Id, 5, 0), new(_initialTorrents[1].WebPageUri[..^1]))
            .ConfigureAwait(false);

        AssertMultipleTorrents(torrents, _initialTorrents, [1, 2, 3]);
    }

    [Test]
    public async Task FindPageAsync_WhenGivenFullNameFilter_ReturnsFilteredArrayOfTorrents()
    {
        using var context = CreateContext();
        var service = new TorrentService(context);

        var torrents = await service
            .FindPageAsync<long>(new(TorrentOrder.Id, 5, 0), new(_initialTorrents[1].Name))
            .ConfigureAwait(false);

        AssertMultipleTorrents(torrents, _initialTorrents[1..^1], [2]);
    }

    [Test]
    public async Task FindPageAsync_WhenGivenFullUppercasedNameFilter_ReturnsFilteredArrayOfTorrents()
    {
        using var context = CreateContext();
        var service = new TorrentService(context);

        var torrents = await service
            .FindPageAsync<long>(new(TorrentOrder.Id, 5, 0), new(_initialTorrents[1].Name.ToUpperInvariant()))
            .ConfigureAwait(false);

        AssertMultipleTorrents(torrents, _initialTorrents[1..^1], [2]);
    }

    [Test]
    public async Task FindPageAsync_WhenGivenPartialNameFilter_ReturnsFilteredArrayOfTorrents()
    {
        using var context = CreateContext();
        var service = new TorrentService(context);

        var torrents = await service
            .FindPageAsync<long>(new(TorrentOrder.Id, 5, 0), new(_initialTorrents[1].Name[..^1]))
            .ConfigureAwait(false);

        AssertMultipleTorrents(torrents, _initialTorrents[1..^1], [2]);
    }

    [Test]
    public async Task FindPageAsync_WhenGivenCronFilter_ReturnsFilteredArrayOfTorrents()
    {
        using var context = CreateContext();
        var service = new TorrentService(context);

        var torrents = await service.FindPageAsync<long>(new(TorrentOrder.Id, 5, 0), new(CronExists: true))
            .ConfigureAwait(false);

        AssertMultipleTorrents(torrents, [_initialTorrents[0], _initialTorrents[2]], [1, 3]);
    }

    [Test]
    public async Task FindPageAsync_WhenGivenMultipleFilters_ReturnsFilteredArrayOfTorrents()
    {
        using var context = CreateContext();
        var service = new TorrentService(context);

        var expected = _initialTorrents[2];
        var torrents = await service.FindPageAsync<long>(new(TorrentOrder.Id, 5, 0), new(expected.Name[..1], true))
            .ConfigureAwait(false);

        AssertMultipleTorrents(torrents, [expected], [3]);
    }

    [Test]
    public async Task FindPageAsync_WhenGivenParameterOrderById_ReturnsSortedArrayOfTorrents()
    {
        using var context = CreateContext();
        var service = new TorrentService(context);

        var torrents = await service.FindPageAsync<long>(new(TorrentOrder.Id, 5, 0))
            .ConfigureAwait(false);

        AssertMultipleTorrents(torrents, _initialTorrents, [1, 2, 3]);
    }

    [Test]
    public async Task FindPageAsync_WhenGivenParameterOrderByName_ReturnsSortedArrayOfTorrents()
    {
        using var context = CreateContext();
        var service = new TorrentService(context);

        var torrents = await service.FindPageAsync<string>(new(TorrentOrder.Name, 5, 0))
            .ConfigureAwait(false);

        AssertMultipleTorrents(torrents, [.. _initialTorrents.OrderBy(torrent => torrent.Name)], [2, 3, 1]);
    }

    [Test]
    public async Task FindPageAsync_WhenGivenParameterOrderByNameDesc_ReturnsSortedArrayOfTorrents()
    {
        using var context = CreateContext();
        var service = new TorrentService(context);

        var torrents = await service.FindPageAsync<string>(new(TorrentOrder.NameDesc, 5, 0))
            .ConfigureAwait(false);

        AssertMultipleTorrents(
            torrents,
            [.. _initialTorrents.OrderByDescending(torrent => torrent.Name)],
            [1, 3, 2]);
    }

    [Test]
    public async Task FindPageAsync_WhenGivenParameterOrderByWebPage_ReturnsSortedArrayOfTorrents()
    {
        using var context = CreateContext();
        var service = new TorrentService(context);

        var torrents = await service.FindPageAsync<string>(new(TorrentOrder.WebPage, 5, 0))
            .ConfigureAwait(false);

        AssertMultipleTorrents(torrents, _initialTorrents, [1, 2, 3]);
    }

    [Test]
    public async Task FindPageAsync_WhenGivenParameterOrderByWebPageDesc_ReturnsSortedArrayOfTorrents()
    {
        using var context = CreateContext();
        var service = new TorrentService(context);

        var torrents = await service.FindPageAsync<string>(new(TorrentOrder.WebPageDesc, 5, 0))
            .ConfigureAwait(false);

        AssertMultipleTorrents(
            torrents,
            [.. _initialTorrents.OrderByDescending(torrent => torrent.WebPageUri)],
            [3, 2, 1]);
    }

    [Test]
    public async Task FindPageAsync_WhenGivenParameterOrderByDownloadDir_ReturnsSortedArrayOfTorrents()
    {
        using var context = CreateContext();
        var service = new TorrentService(context);

        var torrents = await service.FindPageAsync<string>(new(TorrentOrder.DownloadDir, 5, 0))
            .ConfigureAwait(false);

        AssertMultipleTorrents(
            torrents,
            [.. _initialTorrents.OrderBy(torrent => torrent.DownloadDir)],
            [2, 1, 3]);
    }

    [Test]
    public async Task FindPageAsync_WhenGivenParameterOrderByDownloadDirDesc_ReturnsSortedArrayOfTorrents()
    {
        using var context = CreateContext();
        var service = new TorrentService(context);

        var torrents = await service.FindPageAsync<string>(new(TorrentOrder.DownloadDirDesc, 5, 0))
            .ConfigureAwait(false);

        AssertMultipleTorrents(
            torrents,
            [.. _initialTorrents.OrderByDescending(torrent => torrent.DownloadDir)],
            [3, 1, 2]);
    }

    [Test]
    public async Task FindPageAsync_WhenGivenParametersOrderByNameAndAfterNull_ReturnsPageOfTorrents()
    {
        using var context = CreateContext();
        var service = new TorrentService(context);

        var torrents = await service
            .FindPageAsync<string>(new(TorrentOrder.Name, 5, 0, null))
            .ConfigureAwait(false);

        AssertMultipleTorrents(
            torrents,
            [.. _initialTorrents.OrderBy(torrent => torrent.Name)],
            [2, 3, 1]);
    }

    [Test]
    public async Task FindPageAsync_WhenGivenParametersOrderByNameAndAfterEmptyString_ReturnsPageOfTorrents()
    {
        using var context = CreateContext();
        var service = new TorrentService(context);

        var torrents = await service
            .FindPageAsync<string>(new(TorrentOrder.Name, 5, 0, string.Empty))
            .ConfigureAwait(false);

        AssertMultipleTorrents(
            torrents,
            [.. _initialTorrents.OrderBy(torrent => torrent.Name)],
            [2, 3, 1]);
    }

    [Test]
    public async Task FindPageAsync_WhenGivenParametersOrderByNameAndAfterValidValue_ReturnsPageOfTorrents()
    {
        using var context = CreateContext();
        var service = new TorrentService(context);

        var torrents = await service
            .FindPageAsync<string>(new(TorrentOrder.Name, 5, 2, _initialTorrents[1].Name))
            .ConfigureAwait(false);

        AssertMultipleTorrents(
            torrents,
            [.. _initialTorrents.OrderBy(torrent => torrent.Name).Skip(1)],
            [3, 1]);
    }

    [Test]
    public async Task FindPageAsync_WhenGivenParametersOrderByNameDescAndAfterValidValue_ReturnsPageOfTorrents()
    {
        using var context = CreateContext();
        var service = new TorrentService(context);

        var torrents = await service
            .FindPageAsync<string>(new(TorrentOrder.NameDesc, 5, 1, _initialTorrents[0].Name))
            .ConfigureAwait(false);

        AssertMultipleTorrents(
            torrents,
            [.. _initialTorrents.OrderByDescending(torrent => torrent.Name).Skip(1)],
            [3, 2]);
    }

    [Test]
    public async Task FindPageAsync_WhenGivenParametersOrderByWebPageAndAfterNull_ReturnsPageOfTorrents()
    {
        using var context = CreateContext();
        var service = new TorrentService(context);

        var torrents = await service
            .FindPageAsync<string>(new(TorrentOrder.WebPage, 5, 0, null))
            .ConfigureAwait(false);

        AssertMultipleTorrents(torrents, _initialTorrents, [1, 2, 3]);
    }

    [Test]
    public async Task FindPageAsync_WhenGivenParametersOrderByWebPageAndAfterEmptyString_ReturnsPageOfTorrents()
    {
        using var context = CreateContext();
        var service = new TorrentService(context);

        var torrents = await service
            .FindPageAsync<string>(new(TorrentOrder.WebPage, 5, 0, string.Empty))
            .ConfigureAwait(false);

        AssertMultipleTorrents(torrents, _initialTorrents, [1, 2, 3]);
    }

    [Test]
    public async Task FindPageAsync_WhenGivenParametersOrderByWebPageAndAfterValidValue_ReturnsPageOfTorrents()
    {
        using var context = CreateContext();
        var service = new TorrentService(context);

        var torrents = await service
            .FindPageAsync<string>(new(TorrentOrder.WebPage, 5, 1, _initialTorrents[0].WebPageUri))
            .ConfigureAwait(false);

        AssertMultipleTorrents(
            torrents,
            [.. _initialTorrents.OrderBy(torrent => torrent.WebPageUri).Skip(1)],
            [2, 3]);
    }

    [Test]
    public async Task FindPageAsync_WhenGivenParametersOrderByWebPageDescAndAfterValidValue_ReturnsPageOfTorrents()
    {
        using var context = CreateContext();
        var service = new TorrentService(context);

        var torrents = await service
            .FindPageAsync<string>(new(TorrentOrder.WebPageDesc, 5, 3, _initialTorrents[2].WebPageUri))
            .ConfigureAwait(false);

        AssertMultipleTorrents(
            torrents,
            [.. _initialTorrents.OrderByDescending(torrent => torrent.WebPageUri).Skip(1)],
            [2, 1]);
    }

    [Test]
    public async Task FindPageAsync_WhenGivenParametersOrderByDownloadDirAndAfterNull_ReturnsPageOfTorrents()
    {
        using var context = CreateContext();
        var service = new TorrentService(context);

        var torrents = await service
            .FindPageAsync<string>(new(TorrentOrder.DownloadDir, 5, 0, null))
            .ConfigureAwait(false);

        AssertMultipleTorrents(
            torrents,
            [.. _initialTorrents.OrderBy(torrent => torrent.DownloadDir)],
            [2, 1, 3]);
    }

    [Test]
    public async Task FindPageAsync_WhenGivenParametersOrderByDownloadDirAndAfterEmptyString_ReturnsPageOfTorrents()
    {
        using var context = CreateContext();
        var service = new TorrentService(context);

        var torrents = await service
            .FindPageAsync<string>(new(TorrentOrder.DownloadDir, 5, 0, string.Empty))
            .ConfigureAwait(false);

        AssertMultipleTorrents(
            torrents,
            [.. _initialTorrents.OrderBy(torrent => torrent.DownloadDir)],
            [2, 1, 3]);
    }

    [Test]
    public async Task FindPageAsync_WhenGivenParametersOrderByDownloadDirAndAfterValidValue_ReturnsPageOfTorrents()
    {
        using var context = CreateContext();
        var service = new TorrentService(context);

        var torrents = await service
            .FindPageAsync<string>(new(TorrentOrder.DownloadDir, 5, 2, _initialTorrents[1].DownloadDir))
            .ConfigureAwait(false);

        AssertMultipleTorrents(
            torrents,
            [.. _initialTorrents.OrderBy(torrent => torrent.DownloadDir).Skip(1)],
            [1, 3]);
    }

    [Test]
    public async Task FindPageAsync_WhenGivenParametersOrderByDownloadDirDescAndAfterValidValue_ReturnsPageOfTorrents()
    {
        using var context = CreateContext();
        var service = new TorrentService(context);

        var torrents = await service
            .FindPageAsync<string>(new(TorrentOrder.DownloadDirDesc, 5, 3, _initialTorrents[2].DownloadDir))
            .ConfigureAwait(false);

        AssertMultipleTorrents(
            torrents,
            [.. _initialTorrents.OrderByDescending(torrent => torrent.DownloadDir).Skip(1)],
            [1, 2]);
    }

    [Test]
    public async Task FindPageAsync_WhenGivenParametersOrderByNameDescAndAfterValidValueAndFilter_ReturnsPageOfTorrents()
    {
        using var context = CreateContext();
        var service = new TorrentService(context);

        var torrents = await service
            .FindPageAsync<string>(new(TorrentOrder.NameDesc, 5, 1, _initialTorrents[0].Name), new("m", true))
            .ConfigureAwait(false);

        AssertMultipleTorrents(torrents, [_initialTorrents[2]], [3]);
    }

    [Test]
    public void FindPageAsync_WhenGivenParameterTakeEqualToZero_ThrowsArgumentOutOfRangeException()
    {
        using var context = CreateContext();
        var service = new TorrentService(context);

        Assert.That(
            async () => await service.FindPageAsync<long>(new(TorrentOrder.Id, 0, 0)).ConfigureAwait(false),
            Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public void FindPageAsync_WhenGivenParameterTakeEqualToNegativeValue_ThrowsArgumentOutOfRangeException()
    {
        using var context = CreateContext();
        var service = new TorrentService(context);

        Assert.That(
            async () => await service.FindPageAsync<long>(new(TorrentOrder.Id, int.MinValue, 0)).ConfigureAwait(false),
            Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public void FindPageAsync_WhenGivenIncompatibleParameterOrderByAndTypeOfParameterAfter_ThrowsArgumentException()
    {
        using var context = CreateContext();
        var service = new TorrentService(context);

        var invalidPage = new TorrentPageDescriptor<long>(TorrentOrder.Name, 5, 0);

        var error = $"Incompatible arguments orderBy ({invalidPage.OrderBy}) " +
            $"and after ({invalidPage.After}) were provided. (Parameter 'after')";

        Assert.That(
            async () => await service.FindPageAsync(invalidPage).ConfigureAwait(false),
            Throws.TypeOf<ArgumentException>().With.Message.EqualTo(error));
    }

    private static void AssertMultipleTorrents(Torrent[]? actual, Torrent[] expected, long[] expectedIds)
    {
        Assert.That(actual, Is.Not.Null);
        Assert.That(actual, Has.Length.EqualTo(expected.Length));
        for (var i = 0; i < actual!.Length; i++)
            AssertTorrent(actual[i], expected[i], expectedIds[i]);
    }

    private static void AssertTorrent(Torrent? actual, Torrent expected, long expectedId)
    {
        Assert.That(actual, Is.Not.Null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(actual!.Id, Is.EqualTo(expectedId));
            Assert.That(actual.HashString, Is.EqualTo(expected.HashString));
            Assert.That(actual.Name, Is.EqualTo(expected.Name));
            Assert.That(actual.WebPageUri, Is.EqualTo(expected.WebPageUri));
            Assert.That(actual.DownloadDir, Is.EqualTo(expected.DownloadDir));
            Assert.That(actual.MagnetRegexPattern, Is.EqualTo(expected.MagnetRegexPattern));
            Assert.That(actual.Cron, Is.EqualTo(expected.Cron));
        }
    }
}
