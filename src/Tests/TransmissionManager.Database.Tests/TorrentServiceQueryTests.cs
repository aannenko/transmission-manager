using TransmissionManager.Database.Dto;
using TransmissionManager.Database.Models;
using TransmissionManager.Database.Services;

namespace TransmissionManager.Database.Tests;

[Parallelizable(ParallelScope.Self)]
internal sealed class TorrentServiceQueryTests : BaseTorrentServiceTests
{
    internal readonly record struct FindPageAsyncTestData(
        TorrentPageDescriptor<string> Page,
        TorrentFilter Filter,
        Torrent[] ExpectedTorrents);

    private static readonly TestCaseData<FindPageAsyncTestData>[] _findPageAsyncTestCases =
    [
        new(new(default, default, InitialTorrents))
        { TestName = "FindPageAsync_WhenDefaultPaginationValuesAreUsed_ReturnsArrayOfTorrents" },

        new(new(new(IsForwardPagination: false), default, InitialTorrents))
        { TestName = "FindPageAsync_WhenForwardPaginationIsFalse_ReturnsArrayOfTorrents" },

        new(new(new(Take: 2), default, InitialTorrents[..^1]))
        { TestName = "FindPageAsync_WhenTakeIsTwo_ReturnsArrayWithTwoTorrents" },

        new(new(new(IsForwardPagination: false, Take: 2), default, InitialTorrents[1..]))
        { TestName = "FindPageAsync_WhenTakeIsTwoAndIsForwardPaginationIsFalse_ReturnsArrayWithTwoTorrents" },

        new(new(new(AnchorId: 1), default, InitialTorrents[1..]))
        { TestName = "FindPageAsync_WhenAnchorIdIsOne_ReturnsArrayOfTorrentsWithIdGreaterThanOne" },

        new(new(new(AnchorId: 2, IsForwardPagination: false), default, InitialTorrents[..1]))
        { TestName = "FindPageAsync_WhenAnchorIdIsTwoAndIsForwardPaginationIsFalse_ReturnsArrayOfTorrentsWithIdLessThanTwo" },

        new(new(new(AnchorId: 3), default, []))
        { TestName = "FindPageAsync_WhenAnchorIdIsLargestExistingId_ReturnsEmptyArray" },

        new(new(new(AnchorId: 1, IsForwardPagination: false), default, []))
        { TestName = "FindPageAsync_WhenAnchorIdIsSmallestExistingIdAndIsForwardPaginationIsFalse_ReturnsEmptyArray" },

        new(new(new(AnchorId: long.MaxValue), default, []))
        { TestName = "FindPageAsync_WhenAnchorIdIsGreaterThanAnyTorrentId_ReturnsEmptyArray" },

        new(new(new(AnchorId: long.MinValue, IsForwardPagination: false), default, []))
        { TestName = "FindPageAsync_WhenAnchorIdIsSmallerThanAnyTorrentIdAndIsForwardPaginationIsFalse_ReturnsEmptyArray" },

        new(new(new(AnchorId: long.MinValue), default, InitialTorrents))
        { TestName = "FindPageAsync_WhenAnchorIdIsNegative_ReturnsArrayOfTorrents" },

        new(new(new(AnchorId: long.MaxValue, IsForwardPagination: false), default, InitialTorrents))
        { TestName = "FindPageAsync_WhenAnchorIdIsMaxPossibleValueAndIsForwardPaginationIsFalse_ReturnsArrayOfTorrents" },

        new(new(default, new(InitialTorrents[1].HashString), InitialTorrents[1..^1]))
        { TestName = "FindPageAsync_WhenPropertyStartsWithIsFullHashString_ReturnsFilteredArrayOfTorrents" },

        new(new(default, new(InitialTorrents[1].HashString.ToUpperInvariant()), InitialTorrents[1..^1]))
        { TestName = "FindPageAsync_WhenPropertyStartsWithIsFullUppercasedHashString_ReturnsFilteredArrayOfTorrents" },

        new(new(default, new(InitialTorrents[1].HashString[..20]), InitialTorrents[1..^1]))
        { TestName = "FindPageAsync_WhenPropertyStartsWithIsPartialHashString_ReturnsFilteredArrayOfTorrents" },

        new(new(new(IsForwardPagination: false), new(InitialTorrents[1].HashString[..20]), InitialTorrents[1..^1]))
        { TestName = "FindPageAsync_WhenPropertyStartsWithIsPartialHashStringAndIsForwardPaginationIsFalse_ReturnsFilteredArrayOfTorrents" },

        new(new(default, new(InitialTorrents[1].WebPageUri), InitialTorrents[1..^1]))
        { TestName = "FindPageAsync_WhenPropertyStartsWithIsFullWebPageUri_ReturnsFilteredArrayOfTorrents" },

        new(new(default, new(InitialTorrents[1].WebPageUri.ToUpperInvariant()), InitialTorrents[1..^1]))
        { TestName = "FindPageAsync_WhenPropertyStartsWithIsFullUppercasedWebPageUri_ReturnsFilteredArrayOfTorrents" },

        new(new(default, new(InitialTorrents[1].WebPageUri[..^1]), InitialTorrents))
        { TestName = "FindPageAsync_WhenPropertyStartsWithIsPartialWebPageUri_ReturnsFilteredArrayOfTorrents" },

        new(new(new(IsForwardPagination: false, Take: 2), new(InitialTorrents[1].WebPageUri[..^1]), InitialTorrents[1..]))
        { TestName = "FindPageAsync_WhenPropertyStartsWithIsPartialWebPageUriAndIsForwardPaginationIsFalse_ReturnsFilteredArrayOfTorrents" },

        new(new(default, new(InitialTorrents[1].Name), InitialTorrents[1..^1]))
        { TestName = "FindPageAsync_WhenPropertyStartsWithIsFullName_ReturnsFilteredArrayOfTorrents" },

        new(new(default, new(InitialTorrents[1].Name.ToUpperInvariant()), InitialTorrents[1..^1]))
        { TestName = "FindPageAsync_WhenPropertyStartsWithIsFullUppercasedName_ReturnsFilteredArrayOfTorrents" },

        new(new(default, new(InitialTorrents[1].Name[..^1]), InitialTorrents[1..^1]))
        { TestName = "FindPageAsync_WhenPropertyStartsWithIsPartialName_ReturnsFilteredArrayOfTorrents" },

        new(new(new(IsForwardPagination: false), new(InitialTorrents[1].Name[..^1]), InitialTorrents[1..^1]))
        { TestName = "FindPageAsync_WhenPropertyStartsWithIsPartialNameAndIsForwardPaginationIsFalse_ReturnsFilteredArrayOfTorrents" },

        new(new(default, new(CronExists: true), [InitialTorrents[0], InitialTorrents[2]]))
        { TestName = "FindPageAsync_WhenCronExistsIsTrue_ReturnsFilteredArrayOfTorrents" },

        new(new(new(IsForwardPagination: false, Take: 1), new(CronExists: true), InitialTorrents[2..]))
        { TestName = "FindPageAsync_WhenCronExistsIsTrueAndIsForwardPaginationIsFalse_ReturnsFilteredArrayOfTorrents" },

        new(new(default, new(InitialTorrents[2].Name[..1], true), InitialTorrents[2..]))
        { TestName = "FindPageAsync_WhenMultipleFiltersAreUsed_ReturnsFilteredArrayOfTorrents" },

        new(new(new(IsForwardPagination: false), new(InitialTorrents[2].Name[..1], true), InitialTorrents[2..]))
        { TestName = "FindPageAsync_WhenMultipleFiltersAreUsedAndIsForwardPaginationIsFalse_ReturnsFilteredArrayOfTorrents" },

        new(new(new(TorrentOrder.Id), default, InitialTorrents))
        { TestName = "FindPageAsync_WhenOrderByIsId_ReturnsSortedArrayOfTorrents" },

        new(new(new(TorrentOrder.Id, IsForwardPagination: false, Take: 2), default, InitialTorrents[1..]))
        { TestName = "FindPageAsync_WhenOrderByIsIdAndIsForwardPaginationIsFalse_ReturnsSortedArrayOfTorrents" },

        new(new(new(TorrentOrder.IdDesc), default, [.. InitialTorrents.Reverse()]))
        { TestName = "FindPageAsync_WhenOrderByIsIdDesc_ReturnsSortedArrayOfTorrents" },

        new(new(new(TorrentOrder.IdDesc, IsForwardPagination: false, Take: 2), default, [.. InitialTorrents[..^1].Reverse()]))
        { TestName = "FindPageAsync_WhenOrderByIsIdDescAndIsForwardPaginationIsFalse_ReturnsSortedArrayOfTorrents" },

        new(new(new(TorrentOrder.Name), default, [.. InitialTorrents.OrderBy(static torrent => torrent.Name)]))
        { TestName = "FindPageAsync_WhenOrderByIsName_ReturnsSortedArrayOfTorrents" },

        new(new(new(TorrentOrder.Name, IsForwardPagination: false, Take: 2), default, [.. InitialTorrents.OrderBy(static torrent => torrent.Name).Skip(1)]))
        { TestName = "FindPageAsync_WhenOrderByIsNameAndIsForwardPaginationIsFalse_ReturnsSortedArrayOfTorrents" },

        new(new(new(TorrentOrder.NameDesc), default, [.. InitialTorrents.OrderByDescending(static torrent => torrent.Name)]))
        { TestName = "FindPageAsync_WhenOrderByIsNameDesc_ReturnsSortedArrayOfTorrents" },

        new(new(new(TorrentOrder.NameDesc, IsForwardPagination: false, Take: 2), default, [.. InitialTorrents.OrderByDescending(static torrent => torrent.Name).Skip(1)]))
        { TestName = "FindPageAsync_WhenOrderByIsNameDescAndIsForwardPaginationIsFalse_ReturnsSortedArrayOfTorrents" },

        new(new(new(TorrentOrder.WebPage), default, InitialTorrents))
        { TestName = "FindPageAsync_WhenOrderByIsWebPage_ReturnsSortedArrayOfTorrents" },

        new(new(new(TorrentOrder.WebPage, IsForwardPagination: false, Take: 2), default, InitialTorrents[1..]))
        { TestName = "FindPageAsync_WhenOrderByIsWebPageAndIsForwardPaginationIsFalse_ReturnsSortedArrayOfTorrents" },

        new(new(new(TorrentOrder.WebPageDesc), default, [.. InitialTorrents.Reverse()]))
        { TestName = "FindPageAsync_WhenOrderByIsWebPageDesc_ReturnsSortedArrayOfTorrents" },

        new(new(new(TorrentOrder.WebPageDesc, IsForwardPagination: false, Take: 2), default, [.. InitialTorrents.Reverse().Skip(1)]))
        { TestName = "FindPageAsync_WhenOrderByIsWebPageDescAndIsForwardPaginationIsFalse_ReturnsSortedArrayOfTorrents" },

        new(new(new(TorrentOrder.DownloadDir), default, [.. InitialTorrents.OrderBy(static torrent => torrent.DownloadDir)]))
        { TestName = "FindPageAsync_WhenOrderByIsDownloadDir_ReturnsSortedArrayOfTorrents" },

        new(new(new(TorrentOrder.DownloadDir, IsForwardPagination: false, Take: 2), default, [.. InitialTorrents.OrderBy(static torrent => torrent.DownloadDir).Skip(1)]))
        { TestName = "FindPageAsync_WhenOrderByIsDownloadDirAndIsForwardPaginationIsFalse_ReturnsSortedArrayOfTorrents" },

        new(new(new(TorrentOrder.DownloadDirDesc), default, [.. InitialTorrents.OrderByDescending(static torrent => torrent.DownloadDir)]))
        { TestName = "FindPageAsync_WhenOrderByIsDownloadDirDesc_ReturnsSortedArrayOfTorrents" },

        new(new(new(TorrentOrder.DownloadDirDesc, IsForwardPagination: false, Take: 2), default, [.. InitialTorrents.OrderByDescending(static torrent => torrent.DownloadDir).Skip(1)]))
        { TestName = "FindPageAsync_WhenOrderByIsDownloadDirDescAndIsForwardPaginationIsFalse_ReturnsSortedArrayOfTorrents" },

        new(new(new(TorrentOrder.Name, AnchorValue: string.Empty), default, [.. InitialTorrents.OrderBy(static torrent => torrent.Name)]))
        { TestName = "FindPageAsync_WhenOrderByIsNameAndAnchorValueIsEmptyString_ReturnsPageOfTorrents" },

        new(new(new(TorrentOrder.Name, AnchorValue: string.Empty, IsForwardPagination: false), default, []))
        { TestName = "FindPageAsync_WhenOrderByIsNameAndAnchorValueIsEmptyStringAndIsForwardPaginationIsFalse_ReturnsEmptyPageOfTorrents" },

        new(new(new(TorrentOrder.Name, 2, InitialTorrents[1].Name), default, [.. InitialTorrents.OrderBy(static torrent => torrent.Name).Skip(1)]))
        { TestName = "FindPageAsync_WhenOrderByIsNameAndAnchorValueIsExistingName_ReturnsPageOfTorrents" },

        new(new(new(TorrentOrder.Name, 3, InitialTorrents[2].Name, IsForwardPagination: false), default, [.. InitialTorrents.OrderBy(static torrent => torrent.Name).Take(1)]))
        { TestName = "FindPageAsync_WhenOrderByIsNameAndAnchorValueIsExistingNameAndIsForwardPaginationIsFalse_ReturnsPageOfTorrents" },

        new(new(new(TorrentOrder.NameDesc, AnchorValue: string.Empty), default, []))
        { TestName = "FindPageAsync_WhenOrderByIsNameDescAndAnchorValueIsEmptyString_ReturnsEmptyPageOfTorrents" },

        new(new(new(TorrentOrder.NameDesc, AnchorValue: string.Empty, IsForwardPagination: false, Take: 2), default, [.. InitialTorrents.OrderByDescending(static torrent => torrent.Name).Skip(1)]))
        { TestName = "FindPageAsync_WhenOrderByIsNameDescAndAnchorValueIsEmptyStringAndIsForwardPaginationIsFalse_ReturnsPageOfTorrents" },

        new(new(new(TorrentOrder.NameDesc, 1, InitialTorrents[0].Name), default, [.. InitialTorrents.OrderByDescending(static torrent => torrent.Name).Skip(1)]))
        { TestName = "FindPageAsync_WhenOrderByIsNameDescAndAnchorValueIsExistingName_ReturnsPageOfTorrents" },

        new(new(new(TorrentOrder.NameDesc, 2, InitialTorrents[1].Name, IsForwardPagination: false), default, [.. InitialTorrents.OrderByDescending(static torrent => torrent.Name).Take(2)]))
        { TestName = "FindPageAsync_WhenOrderByIsNameDescAndAnchorValueIsExistingNameAndIsForwardPaginationIsFalse_ReturnsPageOfTorrents" },

        new(new(new(TorrentOrder.WebPage, AnchorValue: string.Empty), default, InitialTorrents))
        { TestName = "FindPageAsync_WhenOrderByIsWebPageAndAnchorValueIsEmptyString_ReturnsPageOfTorrents" },

        new(new(new(TorrentOrder.WebPage, AnchorValue: string.Empty, IsForwardPagination: false), default, []))
        { TestName = "FindPageAsync_WhenOrderByIsWebPageAndAnchorValueIsEmptyStringAndIsForwardPaginationIsFalse_ReturnsEmptyPageOfTorrents" },

        new(new(new(TorrentOrder.WebPage, 1, InitialTorrents[0].WebPageUri), default, [.. InitialTorrents.OrderBy(static torrent => torrent.WebPageUri).Skip(1)]))
        { TestName = "FindPageAsync_WhenOrderByIsWebPageAndAnchorValueIsExistingWebPage_ReturnsPageOfTorrents" },

        new(new(new(TorrentOrder.WebPage, 3, InitialTorrents[2].WebPageUri, IsForwardPagination: false), default, [.. InitialTorrents.OrderBy(static torrent => torrent.WebPageUri).Take(2)]))
        { TestName = "FindPageAsync_WhenOrderByIsWebPageAndAnchorValueIsExistingWebPageAndIsForwardPaginationIsFalse_ReturnsPageOfTorrents" },

        new(new(new(TorrentOrder.WebPageDesc, AnchorValue: string.Empty), default, []))
        { TestName = "FindPageAsync_WhenOrderByIsWebPageDescAndAnchorValueIsEmptyString_ReturnsEmptyPageOfTorrents" },

        new(new(new(TorrentOrder.WebPageDesc, AnchorValue: string.Empty, IsForwardPagination: false, Take: 2), default, [.. InitialTorrents.OrderByDescending(static torrent => torrent.WebPageUri).Skip(1)]))
        { TestName = "FindPageAsync_WhenOrderByIsWebPageDescAndAnchorValueIsEmptyStringAndIsForwardPaginationIsFalse_ReturnsPageOfTorrents" },

        new(new(new(TorrentOrder.WebPageDesc, 3, InitialTorrents[2].WebPageUri), default, [.. InitialTorrents.OrderByDescending(static torrent => torrent.WebPageUri).Skip(1)]))
        { TestName = "FindPageAsync_WhenOrderByIsWebPageDescAndAnchorValueIsExistingWebPage_ReturnsPageOfTorrents" },

        new(new(new(TorrentOrder.WebPageDesc, 1, InitialTorrents[0].WebPageUri, IsForwardPagination: false), default, [.. InitialTorrents.OrderByDescending(static torrent => torrent.WebPageUri).Take(2)]))
        { TestName = "FindPageAsync_WhenOrderByIsWebPageDescAndAnchorValueIsExistingWebPageAndIsForwardPaginationIsFalse_ReturnsPageOfTorrents" },

        new(new(new(TorrentOrder.DownloadDir, AnchorValue: string.Empty), default, [.. InitialTorrents.OrderBy(static torrent => torrent.DownloadDir)]))
        { TestName = "FindPageAsync_WhenOrderByIsDownloadDirAndAnchorValueIsEmptyString_ReturnsPageOfTorrents" },

        new(new(new(TorrentOrder.DownloadDir, AnchorValue: string.Empty, IsForwardPagination: false), default, []))
        { TestName = "FindPageAsync_WhenOrderByIsDownloadDirAndAnchorValueIsEmptyStringAndIsForwardPaginationIsFalse_ReturnsEmptyPageOfTorrents" },

        new(new(new(TorrentOrder.DownloadDir, 2, InitialTorrents[1].DownloadDir), default, [.. InitialTorrents.OrderBy(static torrent => torrent.DownloadDir).Skip(1)]))
        { TestName = "FindPageAsync_WhenOrderByIsDownloadDirAndAnchorValueIsExistingDownloadDir_ReturnsPageOfTorrents" },

        new(new(new(TorrentOrder.DownloadDir, 3, InitialTorrents[2].DownloadDir, IsForwardPagination: false), default, [.. InitialTorrents.OrderBy(static torrent => torrent.DownloadDir).Take(2)]))
        { TestName = "FindPageAsync_WhenOrderByIsDownloadDirAndAnchorValueIsExistingDownloadDirAndIsForwardPaginationIsFalse_ReturnsPageOfTorrents" },

        new(new(new(TorrentOrder.DownloadDirDesc, 3, InitialTorrents[2].DownloadDir), default, [.. InitialTorrents.OrderByDescending(static torrent => torrent.DownloadDir).Skip(1)]))
        { TestName = "FindPageAsync_WhenOrderByIsDownloadDirDescAndAnchorValueIsExistingDownloadDir_ReturnsPageOfTorrents" },

        new(new(new(TorrentOrder.DownloadDirDesc, 2, InitialTorrents[1].DownloadDir, IsForwardPagination: false), default, [.. InitialTorrents.OrderByDescending(static torrent => torrent.DownloadDir).Take(2)]))
        { TestName = "FindPageAsync_WhenOrderByIsDownloadDirDescAndAnchorValueIsExistingDownloadDirAndIsForwardPaginationIsFalse_ReturnsPageOfTorrents" },

        new(new(new(TorrentOrder.NameDesc, 1, InitialTorrents[0].Name), new("m", true), [InitialTorrents[2]]))
        { TestName = "FindPageAsync_WhenOrderByIsNameDescAndAnchorValueIsExistingValueAndPropertyStartsWithIsM_ReturnsPageOfTorrents" },
    ];

    [Test]
    public async Task FindOneByIdAsync_WhenIdExists_ReturnsTorrent()
    {
        using var context = CreateContext();
        var service = new TorrentService(context);

        var torrent = await service.FindOneByIdAsync(2).ConfigureAwait(false);

        AssertTorrent(torrent, InitialTorrents[1]);
    }

    [Test]
    public async Task FindOneByIdAsync_WhenIdDoesNotExist_ReturnsNull()
    {
        using var context = CreateContext();
        var service = new TorrentService(context);

        var torrent = await service.FindOneByIdAsync(-1).ConfigureAwait(false);

        Assert.That(torrent, Is.Null);
    }

    [TestCaseSource(nameof(_findPageAsyncTestCases))]
    public async Task FindPageAsync_WhenCalledWithParameters_ReturnsExpectedTorrents(FindPageAsyncTestData data)
    {
        var (page, filter, expectedTorrents) = data;

        using var context = CreateContext();
        var service = new TorrentService(context);

        var torrents = await service.FindPageAsync(page, filter).ConfigureAwait(false);

        Assert.That(torrents, Is.Not.Null);
        Assert.That(torrents, Has.Length.EqualTo(expectedTorrents.Length));
        for (var i = 0; i < torrents!.Length; i++)
            AssertTorrent(torrents[i], expectedTorrents[i]);
    }

    private static void AssertTorrent(Torrent? actual, Torrent expected)
    {
        Assert.That(actual, Is.Not.Null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(actual!.Id, Is.EqualTo(expected.Id));
            Assert.That(actual.HashString, Is.EqualTo(expected.HashString));
            Assert.That(actual.Name, Is.EqualTo(expected.Name));
            Assert.That(actual.WebPageUri, Is.EqualTo(expected.WebPageUri));
            Assert.That(actual.DownloadDir, Is.EqualTo(expected.DownloadDir));
            Assert.That(actual.MagnetRegexPattern, Is.EqualTo(expected.MagnetRegexPattern));
            Assert.That(actual.Cron, Is.EqualTo(expected.Cron));
        }
    }
}
