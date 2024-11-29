using TransmissionManager.Database.Dto;
using TransmissionManager.Database.Models;
using TransmissionManager.Database.Services;

namespace TransmissionManager.Database.Tests;

[Parallelizable(ParallelScope.Self)]
internal sealed class TorrentServiceQueryTests : BaseTorrentServiceTests
{
    [Test]
    public async Task FindOneByIdAsync_ReturnsTorrent_WhenTorrentWithSuchIdExists()
    {
        using var context = CreateContext();
        var service = new TorrentService(context);

        var torrent = await service.FindOneByIdAsync(2).ConfigureAwait(false);

        AssertTorrent(torrent, _initialTorrents[1], 2);
    }

    [Test]
    public async Task FindOneByIdAsync_ReturnsNull_WhenTorrentWithSuchIdDoesNotExist()
    {
        using var context = CreateContext();
        var service = new TorrentService(context);

        var torrent = await service.FindOneByIdAsync(-1).ConfigureAwait(false);

        Assert.That(torrent, Is.Null);
    }

    [Test]
    public async Task FindPageAsync_ReturnsArrayOfTorrents_WhenDefaultParametersUsed()
    {
        using var context = CreateContext();
        var service = new TorrentService(context);

        var torrents = await service.FindPageAsync<long>(new(TorrentOrder.Id, 5, 0)).ConfigureAwait(false);

        AssertMultipleTorrents(torrents, _initialTorrents, [1, 2, 3]);
    }

    [Test]
    public async Task FindPageAsync_ReturnsArrayWithTwoTorrents_WhenTakeParameterEqualsTwo()
    {
        using var context = CreateContext();
        var service = new TorrentService(context);

        var torrents = await service.FindPageAsync<long>(new(TorrentOrder.Id, 2, 0)).ConfigureAwait(false);

        AssertMultipleTorrents(torrents, _initialTorrents[..^1], [1, 2]);
    }

    [Test]
    public async Task FindPageAsync_ReturnsArrayOfTorrentsWithIdGreaterThanOne_WhenAfterIdParameterEqualsOne()
    {
        using var context = CreateContext();
        var service = new TorrentService(context);

        var torrents = await service.FindPageAsync<long>(new(TorrentOrder.Id, 5, 1)).ConfigureAwait(false);

        AssertMultipleTorrents(torrents, _initialTorrents[1..], [2, 3]);
    }

    [Test]
    public async Task FindPageAsync_ReturnsEmptyArray_WhenAfterIdParameterIsGreaterThanAnyTorrentId()
    {
        using var context = CreateContext();
        var service = new TorrentService(context);

        var torrents = await service.FindPageAsync<long>(new(TorrentOrder.Id, 5, 10)).ConfigureAwait(false);

        AssertMultipleTorrents(torrents, [], []);
    }

    [Test]
    public async Task FindPageAsync_ReturnsArrayOfTorrents_WhenAfterIdParameterIsNegative()
    {
        using var context = CreateContext();
        var service = new TorrentService(context);

        var torrents = await service.FindPageAsync<long>(new(TorrentOrder.Id, 5, long.MinValue)).ConfigureAwait(false);

        AssertMultipleTorrents(torrents, _initialTorrents, [1, 2, 3]);
    }

    [Test]
    public async Task FindPageAsync_ReturnsFilteredArrayOfTorrents_WhenFullHashStringFilterIsUsed()
    {
        using var context = CreateContext();
        var service = new TorrentService(context);

        var torrents = await service.FindPageAsync<long>(new(TorrentOrder.Id, 2, 0), new(_initialTorrents[1].HashString))
            .ConfigureAwait(false);

        AssertMultipleTorrents(torrents, _initialTorrents[1..^1], [2]);
    }

    [Test]
    public async Task FindPageAsync_ReturnsFilteredArrayOfTorrents_WhenFullUppercasedHashStringFilterIsUsed()
    {
        using var context = CreateContext();
        var service = new TorrentService(context);

        var torrents = await service
            .FindPageAsync<long>(new(TorrentOrder.Id, 2, 0), new(_initialTorrents[1].HashString.ToUpperInvariant()))
            .ConfigureAwait(false);

        AssertMultipleTorrents(torrents, _initialTorrents[1..^1], [2]);
    }

    [Test]
    public async Task FindPageAsync_ReturnsFilteredArrayOfTorrents_WhenPartialHashStringFilterIsUsed()
    {
        using var context = CreateContext();
        var service = new TorrentService(context);

        var torrents = await service
            .FindPageAsync<long>(new(TorrentOrder.Id, 2, 0), new(_initialTorrents[1].HashString[..20]))
            .ConfigureAwait(false);

        AssertMultipleTorrents(torrents, _initialTorrents[1..^1], [2]);
    }

    [Test]
    public async Task FindPageAsync_ReturnsFilteredArrayOfTorrents_WhenFullWebPageUriFilterIsUsed()
    {
        using var context = CreateContext();
        var service = new TorrentService(context);

        var torrents = await service
            .FindPageAsync<long>(new(TorrentOrder.Id, 2, 0), new(new(_initialTorrents[1].WebPageUri)))
            .ConfigureAwait(false);

        AssertMultipleTorrents(torrents, _initialTorrents[1..^1], [2]);
    }

    [Test]
    public async Task FindPageAsync_ReturnsFilteredArrayOfTorrents_WhenFullUppercasedWebPageUriFilterIsUsed()
    {
        using var context = CreateContext();
        var service = new TorrentService(context);

        var torrents = await service
            .FindPageAsync<long>(new(TorrentOrder.Id, 2, 0), new(_initialTorrents[1].WebPageUri.ToUpperInvariant()))
            .ConfigureAwait(false);

        AssertMultipleTorrents(torrents, _initialTorrents[1..^1], [2]);
    }

    [Test]
    public async Task FindPageAsync_ReturnsFilteredArrayOfTorrents_WhenPartialWebPageUriFilterIsUsed()
    {
        using var context = CreateContext();
        var service = new TorrentService(context);

        var torrents = await service
            .FindPageAsync<long>(new(TorrentOrder.Id, 5, 0), new(_initialTorrents[1].WebPageUri[..^1]))
            .ConfigureAwait(false);

        AssertMultipleTorrents(torrents, _initialTorrents, [1, 2, 3]);
    }

    [Test]
    public async Task FindPageAsync_ReturnsFilteredArrayOfTorrents_WhenFullNameFilterIsUsed()
    {
        using var context = CreateContext();
        var service = new TorrentService(context);

        var torrents = await service
            .FindPageAsync<long>(new(TorrentOrder.Id, 5, 0), new(_initialTorrents[1].Name))
            .ConfigureAwait(false);

        AssertMultipleTorrents(torrents, _initialTorrents[1..^1], [2]);
    }

    [Test]
    public async Task FindPageAsync_ReturnsFilteredArrayOfTorrents_WhenFullUppercasedNameFilterIsUsed()
    {
        using var context = CreateContext();
        var service = new TorrentService(context);

        var torrents = await service
            .FindPageAsync<long>(new(TorrentOrder.Id, 5, 0), new(_initialTorrents[1].Name.ToUpperInvariant()))
            .ConfigureAwait(false);

        AssertMultipleTorrents(torrents, _initialTorrents[1..^1], [2]);
    }

    [Test]
    public async Task FindPageAsync_ReturnsFilteredArrayOfTorrents_WhenPartialNameFilterIsUsed()
    {
        using var context = CreateContext();
        var service = new TorrentService(context);

        var torrents = await service
            .FindPageAsync<long>(new(TorrentOrder.Id, 5, 0), new(_initialTorrents[1].Name[..^1]))
            .ConfigureAwait(false);

        AssertMultipleTorrents(torrents, _initialTorrents[1..^1], [2]);
    }

    [Test]
    public async Task FindPageAsync_ReturnsFilteredArrayOfTorrents_WhenCronFilterIsUsed()
    {
        using var context = CreateContext();
        var service = new TorrentService(context);

        var torrents = await service.FindPageAsync<long>(new(TorrentOrder.Id, 5, 0), new(CronExists: true))
            .ConfigureAwait(false);

        AssertMultipleTorrents(torrents, [_initialTorrents[0], _initialTorrents[2]], [1, 3]);
    }

    [Test]
    public async Task FindPageAsync_ReturnsFilteredArrayOfTorrents_WhenMultipleFiltersAreUsed()
    {
        using var context = CreateContext();
        var service = new TorrentService(context);

        var expected = _initialTorrents[2];
        var torrents = await service.FindPageAsync<long>(new(TorrentOrder.Id, 5, 0), new(expected.Name[..1], true))
            .ConfigureAwait(false);

        AssertMultipleTorrents(torrents, [expected], [3]);
    }

    [Test]
    public async Task FindPageAsync_ReturnsSortedArrayOfTorrents_WhenOrderByIdIsUsed()
    {
        using var context = CreateContext();
        var service = new TorrentService(context);

        var torrents = await service.FindPageAsync<long>(new(TorrentOrder.Id, 5, 0))
            .ConfigureAwait(false);

        AssertMultipleTorrents(torrents, _initialTorrents, [1, 2, 3]);
    }

    [Test]
    public async Task FindPageAsync_ReturnsSortedArrayOfTorrents_WhenOrderByNameIsUsed()
    {
        using var context = CreateContext();
        var service = new TorrentService(context);

        var torrents = await service.FindPageAsync<string>(new(TorrentOrder.Name, 5, 0))
            .ConfigureAwait(false);

        AssertMultipleTorrents(torrents, [.. _initialTorrents.OrderBy(torrent => torrent.Name)], [2, 3, 1]);
    }

    [Test]
    public async Task FindPageAsync_ReturnsSortedArrayOfTorrents_WhenOrderByNameDescIsUsed()
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
    public async Task FindPageAsync_ReturnsSortedArrayOfTorrents_WhenOrderByWebPageIsUsed()
    {
        using var context = CreateContext();
        var service = new TorrentService(context);

        var torrents = await service.FindPageAsync<string>(new(TorrentOrder.WebPage, 5, 0))
            .ConfigureAwait(false);

        AssertMultipleTorrents(torrents, _initialTorrents, [1, 2, 3]);
    }

    [Test]
    public async Task FindPageAsync_ReturnsSortedArrayOfTorrents_WhenOrderByWebPageDescIsUsed()
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
    public async Task FindPageAsync_ReturnsSortedArrayOfTorrents_WhenOrderByDownloadDirIsUsed()
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
    public async Task FindPageAsync_ReturnsSortedArrayOfTorrents_WhenOrderByDownloadDirDescIsUsed()
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
    public async Task FindPageAsync_ReturnsPageOfTorrents_WhenOrderByNameWithNullValueOfAfterIsUsed()
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
    public async Task FindPageAsync_ReturnsPageOfTorrents_WhenOrderByNameWithEmptyStringValueOfAfterIsUsed()
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
    public async Task FindPageAsync_ReturnsPageOfTorrents_WhenOrderByNameWithValidValueOfAfterIsUsed()
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
    public async Task FindPageAsync_ReturnsPageOfTorrents_WhenOrderByNameDescWithValidValueOfAfterIsUsed()
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
    public async Task FindPageAsync_ReturnsPageOfTorrents_WhenOrderByWebPageWithNullValueOfAfterIsUsed()
    {
        using var context = CreateContext();
        var service = new TorrentService(context);

        var torrents = await service
            .FindPageAsync<string>(new(TorrentOrder.WebPage, 5, 0, null))
            .ConfigureAwait(false);

        AssertMultipleTorrents(torrents, _initialTorrents, [1, 2, 3]);
    }

    [Test]
    public async Task FindPageAsync_ReturnsPageOfTorrents_WhenOrderByWebPageWithEmptyStringValueOfAfterIsUsed()
    {
        using var context = CreateContext();
        var service = new TorrentService(context);

        var torrents = await service
            .FindPageAsync<string>(new(TorrentOrder.WebPage, 5, 0, string.Empty))
            .ConfigureAwait(false);

        AssertMultipleTorrents(torrents, _initialTorrents, [1, 2, 3]);
    }

    [Test]
    public async Task FindPageAsync_ReturnsPageOfTorrents_WhenOrderByWebPageWithValidValueOfAfterIsUsed()
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
    public async Task FindPageAsync_ReturnsPageOfTorrents_WhenOrderByWebPageDescWithValidValueOfAfterIsUsed()
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
    public async Task FindPageAsync_ReturnsPageOfTorrents_WhenOrderByDownloadDirWithNullValueOfAfterIsUsed()
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
    public async Task FindPageAsync_ReturnsPageOfTorrents_WhenOrderByDownloadDirWithEmptyStringValueOfAfterIsUsed()
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
    public async Task FindPageAsync_ReturnsPageOfTorrents_WhenOrderByDownloadDirWithValidValueOfAfterIsUsed()
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
    public async Task FindPageAsync_ReturnsPageOfTorrents_WhenOrderByDownloadDirDescWithValidValueOfAfterIsUsed()
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
    public async Task FindPageAsync_ReturnsPageOfTorrents_WhenOrderByNameDescWithValidValueOfAfterAndFilterIsUsed()
    {
        using var context = CreateContext();
        var service = new TorrentService(context);

        var torrents = await service
            .FindPageAsync<string>(new(TorrentOrder.NameDesc, 5, 1, _initialTorrents[0].Name), new("m", true))
            .ConfigureAwait(false);

        AssertMultipleTorrents(torrents, [_initialTorrents[2]], [3]);
    }

    [Test]
    public void FindPageAsync_ThrowsArgumentOutOfRangeException_WhenTakeParameterIsZero()
    {
        using var context = CreateContext();
        var service = new TorrentService(context);

        Assert.That(
            async () => await service.FindPageAsync<long>(new(TorrentOrder.Id, 0, 0)).ConfigureAwait(false),
            Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public void FindPageAsync_ThrowsArgumentOutOfRangeException_WhenTakeParameterIsNegative()
    {
        using var context = CreateContext();
        var service = new TorrentService(context);

        Assert.That(
            async () => await service.FindPageAsync<long>(new(TorrentOrder.Id, int.MinValue, 0)).ConfigureAwait(false),
            Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public void FindPageAsync_ThrowsArgumentException_WhenIncompatibleValueOfOrderByAndTypeOfAfterIsUsed()
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
        Assert.Multiple(() =>
        {
            Assert.That(actual!.Id, Is.EqualTo(expectedId));
            Assert.That(actual.HashString, Is.EqualTo(expected.HashString));
            Assert.That(actual.Name, Is.EqualTo(expected.Name));
            Assert.That(actual.WebPageUri, Is.EqualTo(expected.WebPageUri));
            Assert.That(actual.DownloadDir, Is.EqualTo(expected.DownloadDir));
            Assert.That(actual.MagnetRegexPattern, Is.EqualTo(expected.MagnetRegexPattern));
            Assert.That(actual.Cron, Is.EqualTo(expected.Cron));
        });
    }
}
