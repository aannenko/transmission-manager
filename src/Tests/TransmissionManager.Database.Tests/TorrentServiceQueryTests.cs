using TransmissionManager.Database.Dto;
using TransmissionManager.Database.Models;
using TransmissionManager.Database.Services;
using TransmissionManager.Database.Tests.Helpers;

namespace TransmissionManager.Database.Tests;

[Parallelizable(ParallelScope.Self)]
internal sealed class TorrentServiceQueryTests : BaseTorrentServiceTests
{
    internal readonly record struct FindPageAsyncTestData<TAnchor>(
        TorrentPageDescriptor<TAnchor> Page,
        TorrentFilter Filter,
        Torrent[] ExpectedTorrents);

    [Test]
    public async Task FindOneByIdAsync_WhenIdExists_ReturnsTorrent()
    {
        using var context = CreateContext();
        var service = new TorrentService(context);

        var torrent = await service.FindOneByIdAsync(2).ConfigureAwait(false);

        TorrentAssertions.AssertEqual(torrent, InitialTorrents[1].Id, InitialTorrents[1]);
    }

    [Test]
    public async Task FindOneByIdAsync_WhenIdDoesNotExist_ReturnsNull()
    {
        using var context = CreateContext();
        var service = new TorrentService(context);

        var torrent = await service.FindOneByIdAsync(-1).ConfigureAwait(false);

        Assert.That(torrent, Is.Null);
    }

    [TestCaseSource(nameof(GetFindPageAsyncStringTestCases))]
    [TestCaseSource(nameof(GetFindPageAsyncDateTimeTestCases))]
    public async Task FindPageAsync_WhenCalledWithParameters_ReturnsExpectedTorrents<TAnchor>(
        FindPageAsyncTestData<TAnchor> data)
    {
        var (page, filter, expectedTorrents) = data;

        using var context = CreateContext();
        var service = new TorrentService(context);

        var torrents = await service.FindPageAsync(page, filter).ConfigureAwait(false);

        Assert.That(torrents, Is.Not.Null);
        Assert.That(torrents, Has.Length.EqualTo(expectedTorrents.Length));
        for (var i = 0; i < torrents.Length; i++)
            TorrentAssertions.AssertEqual(torrents[i], expectedTorrents[i].Id, expectedTorrents[i]);
    }

    private static IEnumerable<TestCaseData<FindPageAsyncTestData<string>>> GetFindPageAsyncStringTestCases()
    {
        yield return new(new(default, default, InitialTorrents))
        {
            TypeArgs = [typeof(string)],
            TestName = "FindPageAsync_WhenDefaultPaginationValuesAreUsed_ReturnsArrayOfTorrents"
        };

        yield return new(new(new(IsForwardPagination: false), default, InitialTorrents))
        {
            TypeArgs = [typeof(string)],
            TestName = "FindPageAsync_WhenForwardPaginationIsFalse_ReturnsArrayOfTorrents"
        };

        yield return new(new(new(Take: 2), default, InitialTorrents[..^1]))
        {
            TypeArgs = [typeof(string)],
            TestName = "FindPageAsync_WhenTakeIsTwo_ReturnsArrayWithTwoTorrents"
        };

        yield return new(new(new(IsForwardPagination: false, Take: 2), default, InitialTorrents[1..]))
        {
            TypeArgs = [typeof(string)],
            TestName = "FindPageAsync_WhenTakeIsTwoAndIsForwardPaginationIsFalse_ReturnsArrayWithTwoTorrents"
        };

        yield return new(new(new(AnchorId: 1), default, InitialTorrents[1..]))
        {
            TypeArgs = [typeof(string)],
            TestName = "FindPageAsync_WhenAnchorIdIsOne_ReturnsArrayOfTorrentsWithIdGreaterThanOne"
        };

        yield return new(new(new(AnchorId: 2, IsForwardPagination: false), default, InitialTorrents[..1]))
        {
            TypeArgs = [typeof(string)],
            TestName = "FindPageAsync_WhenAnchorIdIsTwoAndIsForwardPaginationIsFalse_ReturnsArrayOfTorrentsWithIdLessThanTwo"
        };

        yield return new(new(new(AnchorId: 3), default, []))
        {
            TypeArgs = [typeof(string)],
            TestName = "FindPageAsync_WhenAnchorIdIsLargestExistingId_ReturnsEmptyArray"
        };

        yield return new(new(new(AnchorId: 1, IsForwardPagination: false), default, []))
        {
            TypeArgs = [typeof(string)],
            TestName = "FindPageAsync_WhenAnchorIdIsSmallestExistingIdAndIsForwardPaginationIsFalse_ReturnsEmptyArray"
        };

        yield return new(new(new(AnchorId: long.MaxValue), default, []))
        {
            TypeArgs = [typeof(string)],
            TestName = "FindPageAsync_WhenAnchorIdIsGreaterThanAnyTorrentId_ReturnsEmptyArray"
        };

        yield return new(new(new(AnchorId: long.MinValue, IsForwardPagination: false), default, []))
        {
            TypeArgs = [typeof(string)],
            TestName = "FindPageAsync_WhenAnchorIdIsSmallerThanAnyTorrentIdAndIsForwardPaginationIsFalse_ReturnsEmptyArray"
        };

        yield return new(new(new(AnchorId: long.MinValue), default, InitialTorrents))
        {
            TypeArgs = [typeof(string)],
            TestName = "FindPageAsync_WhenAnchorIdIsNegative_ReturnsArrayOfTorrents"
        };

        yield return new(new(new(AnchorId: long.MaxValue, IsForwardPagination: false), default, InitialTorrents))
        {
            TypeArgs = [typeof(string)],
            TestName = "FindPageAsync_WhenAnchorIdIsMaxPossibleValueAndIsForwardPaginationIsFalse_ReturnsArrayOfTorrents"
        };

        yield return new(new(default, new(InitialTorrents[1].HashString), InitialTorrents[1..^1]))
        {
            TypeArgs = [typeof(string)],
            TestName = "FindPageAsync_WhenPropertyStartsWithIsFullHashString_ReturnsFilteredArrayOfTorrents"
        };

        yield return new(new(default, new(InitialTorrents[1].HashString.ToUpperInvariant()), InitialTorrents[1..^1]))
        {
            TypeArgs = [typeof(string)],
            TestName = "FindPageAsync_WhenPropertyStartsWithIsFullUppercasedHashString_ReturnsFilteredArrayOfTorrents"
        };

        yield return new(new(default, new(InitialTorrents[1].HashString[..20]), InitialTorrents[1..^1]))
        {
            TypeArgs = [typeof(string)],
            TestName = "FindPageAsync_WhenPropertyStartsWithIsPartialHashString_ReturnsFilteredArrayOfTorrents"
        };

        yield return new(new(new(IsForwardPagination: false), new(InitialTorrents[1].HashString[..20]), InitialTorrents[1..^1]))
        {
            TypeArgs = [typeof(string)],
            TestName = "FindPageAsync_WhenPropertyStartsWithIsPartialHashStringAndIsForwardPaginationIsFalse_ReturnsFilteredArrayOfTorrents"
        };

        yield return new(new(default, new(InitialTorrents[1].WebPageUri), InitialTorrents[1..^1]))
        {
            TypeArgs = [typeof(string)],
            TestName = "FindPageAsync_WhenPropertyStartsWithIsFullWebPageUri_ReturnsFilteredArrayOfTorrents"
        };

        yield return new(new(default, new(InitialTorrents[1].WebPageUri.ToUpperInvariant()), InitialTorrents[1..^1]))
        {
            TypeArgs = [typeof(string)],
            TestName = "FindPageAsync_WhenPropertyStartsWithIsFullUppercasedWebPageUri_ReturnsFilteredArrayOfTorrents"
        };

        yield return new(new(default, new(InitialTorrents[1].WebPageUri[..^1]), InitialTorrents))
        {
            TypeArgs = [typeof(string)],
            TestName = "FindPageAsync_WhenPropertyStartsWithIsPartialWebPageUri_ReturnsFilteredArrayOfTorrents"
        };

        yield return new(new(new(IsForwardPagination: false, Take: 2), new(InitialTorrents[1].WebPageUri[..^1]), InitialTorrents[1..]))
        {
            TypeArgs = [typeof(string)],
            TestName = "FindPageAsync_WhenPropertyStartsWithIsPartialWebPageUriAndIsForwardPaginationIsFalse_ReturnsFilteredArrayOfTorrents"
        };

        yield return new(new(default, new(InitialTorrents[1].Name), InitialTorrents[1..^1]))
        {
            TypeArgs = [typeof(string)],
            TestName = "FindPageAsync_WhenPropertyStartsWithIsFullName_ReturnsFilteredArrayOfTorrents"
        };

        yield return new(new(default, new(InitialTorrents[1].Name.ToUpperInvariant()), InitialTorrents[1..^1]))
        {
            TypeArgs = [typeof(string)],
            TestName = "FindPageAsync_WhenPropertyStartsWithIsFullUppercasedName_ReturnsFilteredArrayOfTorrents"
        };

        yield return new(new(default, new(InitialTorrents[1].Name[..^1]), InitialTorrents[1..^1]))
        {
            TypeArgs = [typeof(string)],
            TestName = "FindPageAsync_WhenPropertyStartsWithIsPartialName_ReturnsFilteredArrayOfTorrents"
        };

        yield return new(new(new(IsForwardPagination: false), new(InitialTorrents[1].Name[..^1]), InitialTorrents[1..^1]))
        {
            TypeArgs = [typeof(string)],
            TestName = "FindPageAsync_WhenPropertyStartsWithIsPartialNameAndIsForwardPaginationIsFalse_ReturnsFilteredArrayOfTorrents"
        };

        yield return new(new(default, new(CronExists: true), [InitialTorrents[0], InitialTorrents[2]]))
        {
            TypeArgs = [typeof(string)],
            TestName = "FindPageAsync_WhenCronExistsIsTrue_ReturnsFilteredArrayOfTorrents"
        };

        yield return new(new(new(IsForwardPagination: false, Take: 1), new(CronExists: true), InitialTorrents[2..]))
        {
            TypeArgs = [typeof(string)],
            TestName = "FindPageAsync_WhenCronExistsIsTrueAndIsForwardPaginationIsFalse_ReturnsFilteredArrayOfTorrents"
        };

        yield return new(new(default, new(InitialTorrents[2].Name[..1], true), InitialTorrents[2..]))
        {
            TypeArgs = [typeof(string)],
            TestName = "FindPageAsync_WhenMultipleFiltersAreUsed_ReturnsFilteredArrayOfTorrents"
        };

        yield return new(new(new(IsForwardPagination: false), new(InitialTorrents[2].Name[..1], true), InitialTorrents[2..]))
        {
            TypeArgs = [typeof(string)],
            TestName = "FindPageAsync_WhenMultipleFiltersAreUsedAndIsForwardPaginationIsFalse_ReturnsFilteredArrayOfTorrents"
        };

        yield return new(new(new(TorrentOrder.Id), default, InitialTorrents))
        {
            TypeArgs = [typeof(string)],
            TestName = "FindPageAsync_WhenOrderByIsId_ReturnsSortedArrayOfTorrents"
        };

        yield return new(new(new(TorrentOrder.Id, IsForwardPagination: false, Take: 2), default, InitialTorrents[1..]))
        {
            TypeArgs = [typeof(string)],
            TestName = "FindPageAsync_WhenOrderByIsIdAndIsForwardPaginationIsFalse_ReturnsSortedArrayOfTorrents"
        };

        yield return new(new(new(TorrentOrder.IdDesc), default, [.. InitialTorrents.Reverse()]))
        {
            TypeArgs = [typeof(string)],
            TestName = "FindPageAsync_WhenOrderByIsIdDesc_ReturnsSortedArrayOfTorrents"
        };

        yield return new(new(new(TorrentOrder.IdDesc, IsForwardPagination: false, Take: 2), default, [.. InitialTorrents[..^1].Reverse()]))
        {
            TypeArgs = [typeof(string)],
            TestName = "FindPageAsync_WhenOrderByIsIdDescAndIsForwardPaginationIsFalse_ReturnsSortedArrayOfTorrents"
        };

        yield return new(new(new(TorrentOrder.Name), default, [.. InitialTorrents.OrderBy(static torrent => torrent.Name)]))
        {
            TypeArgs = [typeof(string)],
            TestName = "FindPageAsync_WhenOrderByIsName_ReturnsSortedArrayOfTorrents"
        };

        yield return new(new(new(TorrentOrder.Name, IsForwardPagination: false, Take: 2), default, [.. InitialTorrents.OrderBy(static torrent => torrent.Name).Skip(1)]))
        {
            TypeArgs = [typeof(string)],
            TestName = "FindPageAsync_WhenOrderByIsNameAndIsForwardPaginationIsFalse_ReturnsSortedArrayOfTorrents"
        };

        yield return new(new(new(TorrentOrder.NameDesc), default, [.. InitialTorrents.OrderByDescending(static torrent => torrent.Name)]))
        {
            TypeArgs = [typeof(string)],
            TestName = "FindPageAsync_WhenOrderByIsNameDesc_ReturnsSortedArrayOfTorrents"
        };

        yield return new(new(new(TorrentOrder.NameDesc, IsForwardPagination: false, Take: 2), default, [.. InitialTorrents.OrderByDescending(static torrent => torrent.Name).Skip(1)]))
        {
            TypeArgs = [typeof(string)],
            TestName = "FindPageAsync_WhenOrderByIsNameDescAndIsForwardPaginationIsFalse_ReturnsSortedArrayOfTorrents"
        };

        yield return new(new(new(TorrentOrder.WebPage), default, InitialTorrents))
        {
            TypeArgs = [typeof(string)],
            TestName = "FindPageAsync_WhenOrderByIsWebPage_ReturnsSortedArrayOfTorrents"
        };

        yield return new(new(new(TorrentOrder.WebPage, IsForwardPagination: false, Take: 2), default, InitialTorrents[1..]))
        {
            TypeArgs = [typeof(string)],
            TestName = "FindPageAsync_WhenOrderByIsWebPageAndIsForwardPaginationIsFalse_ReturnsSortedArrayOfTorrents"
        };

        yield return new(new(new(TorrentOrder.WebPageDesc), default, [.. InitialTorrents.Reverse()]))
        {
            TypeArgs = [typeof(string)],
            TestName = "FindPageAsync_WhenOrderByIsWebPageDesc_ReturnsSortedArrayOfTorrents"
        };

        yield return new(new(new(TorrentOrder.WebPageDesc, IsForwardPagination: false, Take: 2), default, [.. InitialTorrents.Reverse().Skip(1)]))
        {
            TypeArgs = [typeof(string)],
            TestName = "FindPageAsync_WhenOrderByIsWebPageDescAndIsForwardPaginationIsFalse_ReturnsSortedArrayOfTorrents"
        };

        yield return new(new(new(TorrentOrder.DownloadDir), default, [.. InitialTorrents.OrderBy(static torrent => torrent.DownloadDir)]))
        {
            TypeArgs = [typeof(string)],
            TestName = "FindPageAsync_WhenOrderByIsDownloadDir_ReturnsSortedArrayOfTorrents"
        };

        yield return new(new(new(TorrentOrder.DownloadDir, IsForwardPagination: false, Take: 2), default, [.. InitialTorrents.OrderBy(static torrent => torrent.DownloadDir).Skip(1)]))
        {
            TypeArgs = [typeof(string)],
            TestName = "FindPageAsync_WhenOrderByIsDownloadDirAndIsForwardPaginationIsFalse_ReturnsSortedArrayOfTorrents"
        };

        yield return new(new(new(TorrentOrder.DownloadDirDesc), default, [.. InitialTorrents.OrderByDescending(static torrent => torrent.DownloadDir)]))
        {
            TypeArgs = [typeof(string)],
            TestName = "FindPageAsync_WhenOrderByIsDownloadDirDesc_ReturnsSortedArrayOfTorrents"
        };

        yield return new(new(new(TorrentOrder.DownloadDirDesc, IsForwardPagination: false, Take: 2), default, [.. InitialTorrents.OrderByDescending(static torrent => torrent.DownloadDir).Skip(1)]))
        {
            TypeArgs = [typeof(string)],
            TestName = "FindPageAsync_WhenOrderByIsDownloadDirDescAndIsForwardPaginationIsFalse_ReturnsSortedArrayOfTorrents"
        };

        yield return new(new(new(TorrentOrder.Name, AnchorValue: string.Empty), default, [.. InitialTorrents.OrderBy(static torrent => torrent.Name)]))
        {
            TypeArgs = [typeof(string)],
            TestName = "FindPageAsync_WhenOrderByIsNameAndAnchorValueIsEmptyString_ReturnsPageOfTorrents"
        };

        yield return new(new(new(TorrentOrder.Name, AnchorValue: string.Empty, IsForwardPagination: false), default, []))
        {
            TypeArgs = [typeof(string)],
            TestName = "FindPageAsync_WhenOrderByIsNameAndAnchorValueIsEmptyStringAndIsForwardPaginationIsFalse_ReturnsEmptyPageOfTorrents"
        };

        yield return new(new(new(TorrentOrder.Name, 2, InitialTorrents[1].Name), default, [.. InitialTorrents.OrderBy(static torrent => torrent.Name).Skip(1)]))
        {
            TypeArgs = [typeof(string)],
            TestName = "FindPageAsync_WhenOrderByIsNameAndAnchorValueIsExistingName_ReturnsPageOfTorrents"
        };

        yield return new(new(new(TorrentOrder.Name, 3, InitialTorrents[2].Name, IsForwardPagination: false), default, [.. InitialTorrents.OrderBy(static torrent => torrent.Name).Take(1)]))
        {
            TypeArgs = [typeof(string)],
            TestName = "FindPageAsync_WhenOrderByIsNameAndAnchorValueIsExistingNameAndIsForwardPaginationIsFalse_ReturnsPageOfTorrents"
        };

        yield return new(new(new(TorrentOrder.NameDesc, AnchorValue: string.Empty), default, []))
        {
            TypeArgs = [typeof(string)],
            TestName = "FindPageAsync_WhenOrderByIsNameDescAndAnchorValueIsEmptyString_ReturnsEmptyPageOfTorrents"
        };

        yield return new(new(new(TorrentOrder.NameDesc, AnchorValue: string.Empty, IsForwardPagination: false, Take: 2), default, [.. InitialTorrents.OrderByDescending(static torrent => torrent.Name).Skip(1)]))
        {
            TypeArgs = [typeof(string)],
            TestName = "FindPageAsync_WhenOrderByIsNameDescAndAnchorValueIsEmptyStringAndIsForwardPaginationIsFalse_ReturnsPageOfTorrents"
        };

        yield return new(new(new(TorrentOrder.NameDesc, 1, InitialTorrents[0].Name), default, [.. InitialTorrents.OrderByDescending(static torrent => torrent.Name).Skip(1)]))
        {
            TypeArgs = [typeof(string)],
            TestName = "FindPageAsync_WhenOrderByIsNameDescAndAnchorValueIsExistingName_ReturnsPageOfTorrents"
        };

        yield return new(new(new(TorrentOrder.NameDesc, 2, InitialTorrents[1].Name, IsForwardPagination: false), default, [.. InitialTorrents.OrderByDescending(static torrent => torrent.Name).Take(2)]))
        {
            TypeArgs = [typeof(string)],
            TestName = "FindPageAsync_WhenOrderByIsNameDescAndAnchorValueIsExistingNameAndIsForwardPaginationIsFalse_ReturnsPageOfTorrents"
        };

        yield return new(new(new(TorrentOrder.WebPage, AnchorValue: string.Empty), default, InitialTorrents))
        {
            TypeArgs = [typeof(string)],
            TestName = "FindPageAsync_WhenOrderByIsWebPageAndAnchorValueIsEmptyString_ReturnsPageOfTorrents"
        };

        yield return new(new(new(TorrentOrder.WebPage, AnchorValue: string.Empty, IsForwardPagination: false), default, []))
        {
            TypeArgs = [typeof(string)],
            TestName = "FindPageAsync_WhenOrderByIsWebPageAndAnchorValueIsEmptyStringAndIsForwardPaginationIsFalse_ReturnsEmptyPageOfTorrents"
        };

        yield return new(new(new(TorrentOrder.WebPage, 1, InitialTorrents[0].WebPageUri), default, [.. InitialTorrents.OrderBy(static torrent => torrent.WebPageUri).Skip(1)]))
        {
            TypeArgs = [typeof(string)],
            TestName = "FindPageAsync_WhenOrderByIsWebPageAndAnchorValueIsExistingWebPage_ReturnsPageOfTorrents"
        };

        yield return new(new(new(TorrentOrder.WebPage, 3, InitialTorrents[2].WebPageUri, IsForwardPagination: false), default, [.. InitialTorrents.OrderBy(static torrent => torrent.WebPageUri).Take(2)]))
        {
            TypeArgs = [typeof(string)],
            TestName = "FindPageAsync_WhenOrderByIsWebPageAndAnchorValueIsExistingWebPageAndIsForwardPaginationIsFalse_ReturnsPageOfTorrents"
        };

        yield return new(new(new(TorrentOrder.WebPageDesc, AnchorValue: string.Empty), default, []))
        {
            TypeArgs = [typeof(string)],
            TestName = "FindPageAsync_WhenOrderByIsWebPageDescAndAnchorValueIsEmptyString_ReturnsEmptyPageOfTorrents"
        };

        yield return new(new(new(TorrentOrder.WebPageDesc, AnchorValue: string.Empty, IsForwardPagination: false, Take: 2), default, [.. InitialTorrents.OrderByDescending(static torrent => torrent.WebPageUri).Skip(1)]))
        {
            TypeArgs = [typeof(string)],
            TestName = "FindPageAsync_WhenOrderByIsWebPageDescAndAnchorValueIsEmptyStringAndIsForwardPaginationIsFalse_ReturnsPageOfTorrents"
        };

        yield return new(new(new(TorrentOrder.WebPageDesc, 3, InitialTorrents[2].WebPageUri), default, [.. InitialTorrents.OrderByDescending(static torrent => torrent.WebPageUri).Skip(1)]))
        {
            TypeArgs = [typeof(string)],
            TestName = "FindPageAsync_WhenOrderByIsWebPageDescAndAnchorValueIsExistingWebPage_ReturnsPageOfTorrents"
        };

        yield return new(new(new(TorrentOrder.WebPageDesc, 1, InitialTorrents[0].WebPageUri, IsForwardPagination: false), default, [.. InitialTorrents.OrderByDescending(static torrent => torrent.WebPageUri).Take(2)]))
        {
            TypeArgs = [typeof(string)],
            TestName = "FindPageAsync_WhenOrderByIsWebPageDescAndAnchorValueIsExistingWebPageAndIsForwardPaginationIsFalse_ReturnsPageOfTorrents"
        };

        yield return new(new(new(TorrentOrder.DownloadDir, AnchorValue: string.Empty), default, [.. InitialTorrents.OrderBy(static torrent => torrent.DownloadDir)]))
        {
            TypeArgs = [typeof(string)],
            TestName = "FindPageAsync_WhenOrderByIsDownloadDirAndAnchorValueIsEmptyString_ReturnsPageOfTorrents"
        };

        yield return new(new(new(TorrentOrder.DownloadDir, AnchorValue: string.Empty, IsForwardPagination: false), default, []))
        {
            TypeArgs = [typeof(string)],
            TestName = "FindPageAsync_WhenOrderByIsDownloadDirAndAnchorValueIsEmptyStringAndIsForwardPaginationIsFalse_ReturnsEmptyPageOfTorrents"
        };

        yield return new(new(new(TorrentOrder.DownloadDir, 2, InitialTorrents[1].DownloadDir), default, [.. InitialTorrents.OrderBy(static torrent => torrent.DownloadDir).Skip(1)]))
        {
            TypeArgs = [typeof(string)],
            TestName = "FindPageAsync_WhenOrderByIsDownloadDirAndAnchorValueIsExistingDownloadDir_ReturnsPageOfTorrents"
        };

        yield return new(new(new(TorrentOrder.DownloadDir, 3, InitialTorrents[2].DownloadDir, IsForwardPagination: false), default, [.. InitialTorrents.OrderBy(static torrent => torrent.DownloadDir).Take(2)]))
        {
            TypeArgs = [typeof(string)],
            TestName = "FindPageAsync_WhenOrderByIsDownloadDirAndAnchorValueIsExistingDownloadDirAndIsForwardPaginationIsFalse_ReturnsPageOfTorrents"
        };

        yield return new(new(new(TorrentOrder.DownloadDirDesc, 3, InitialTorrents[2].DownloadDir), default, [.. InitialTorrents.OrderByDescending(static torrent => torrent.DownloadDir).Skip(1)]))
        {
            TypeArgs = [typeof(string)],
            TestName = "FindPageAsync_WhenOrderByIsDownloadDirDescAndAnchorValueIsExistingDownloadDir_ReturnsPageOfTorrents"
        };

        yield return new(new(new(TorrentOrder.DownloadDirDesc, 2, InitialTorrents[1].DownloadDir, IsForwardPagination: false), default, [.. InitialTorrents.OrderByDescending(static torrent => torrent.DownloadDir).Take(2)]))
        {
            TypeArgs = [typeof(string)],
            TestName = "FindPageAsync_WhenOrderByIsDownloadDirDescAndAnchorValueIsExistingDownloadDirAndIsForwardPaginationIsFalse_ReturnsPageOfTorrents"
        };

        yield return new(new(new(TorrentOrder.NameDesc, 1, InitialTorrents[0].Name), new("m", true), [InitialTorrents[2]]))
        {
            TypeArgs = [typeof(string)],
            TestName = "FindPageAsync_WhenOrderByIsNameDescAndAnchorValueIsExistingValueAndPropertyStartsWithIsM_ReturnsPageOfTorrents"
        };
    }

    private static IEnumerable<TestCaseData<FindPageAsyncTestData<DateTime?>>> GetFindPageAsyncDateTimeTestCases()
    {
        yield return new(new(new(TorrentOrder.RefreshDate), default, [.. InitialTorrents.OrderBy(static torrent => torrent.RefreshDate)]))
        {
            TypeArgs = [typeof(DateTime?)],
            TestName = "FindPageAsync_WhenOrderByIsRefreshDate_ReturnsSortedArrayOfTorrents"
        };

        yield return new(new(new(TorrentOrder.RefreshDate, 3, InitialTorrents[2].RefreshDate), default, [.. InitialTorrents.OrderBy(static torrent => torrent.RefreshDate).Skip(1)]))
        {
            TypeArgs = [typeof(DateTime?)],
            TestName = "FindPageAsync_WhenOrderByIsRefreshDateAndAnchorValueIsExistingRefreshDate_ReturnsSortedArrayOfTorrents"
        };

        yield return new(new(new(TorrentOrder.RefreshDate, 3, InitialTorrents[2].RefreshDate, Take: 1), default, [.. InitialTorrents.OrderBy(static torrent => torrent.RefreshDate).Skip(1).Take(1)]))
        {
            TypeArgs = [typeof(DateTime?)],
            TestName = "FindPageAsync_WhenOrderByIsRefreshDateAndAnchorValueIsExistingRefreshDateAndTakeIsOne_ReturnsSortedArrayOfTorrents"
        };

        yield return new(new(new(TorrentOrder.RefreshDate, 1, InitialTorrents[0].RefreshDate, false), default, [.. InitialTorrents.OrderBy(static torrent => torrent.RefreshDate).Take(2)]))
        {
            TypeArgs = [typeof(DateTime?)],
            TestName = "FindPageAsync_WhenOrderByIsRefreshDateAndAnchorValueIsExistingRefreshDateAndIsForwardPaginationIsFalse_ReturnsSortedArrayOfTorrents"
        };

        yield return new(new(new(TorrentOrder.RefreshDate, 1, InitialTorrents[0].RefreshDate, false, 1), default, [.. InitialTorrents.OrderBy(static torrent => torrent.RefreshDate).Skip(1).Take(1)]))
        {
            TypeArgs = [typeof(DateTime?)],
            TestName = "FindPageAsync_WhenOrderByIsRefreshDateAndAnchorValueIsExistingRefreshDateAndIsForwardPaginationIsFalseAndTakeIsOne_ReturnsSortedArrayOfTorrents"
        };

        yield return new(new(new(TorrentOrder.RefreshDateDesc), default, [.. InitialTorrents.OrderByDescending(static torrent => torrent.RefreshDate)]))
        {
            TypeArgs = [typeof(DateTime?)],
            TestName = "FindPageAsync_WhenOrderByIsRefreshDateDesc_ReturnsSortedArrayOfTorrents"
        };

        yield return new(new(new(TorrentOrder.RefreshDateDesc, 1, InitialTorrents[0].RefreshDate), default, [.. InitialTorrents.OrderByDescending(static torrent => torrent.RefreshDate).Skip(1)]))
        {
            TypeArgs = [typeof(DateTime?)],
            TestName = "FindPageAsync_WhenOrderByIsRefreshDateDescAndAnchorValueIsExistingRefreshDate_ReturnsSortedArrayOfTorrents"
        };

        yield return new(new(new(TorrentOrder.RefreshDateDesc, 1, InitialTorrents[0].RefreshDate, Take: 1), default, [.. InitialTorrents.OrderByDescending(static torrent => torrent.RefreshDate).Skip(1).Take(1)]))
        {
            TypeArgs = [typeof(DateTime?)],
            TestName = "FindPageAsync_WhenOrderByIsRefreshDateDescAndAnchorValueIsExistingRefreshDateAndTakeIsOne_ReturnsSortedArrayOfTorrents"
        };

        yield return new(new(new(TorrentOrder.RefreshDateDesc, 3, InitialTorrents[2].RefreshDate, false), default, [.. InitialTorrents.OrderByDescending(static torrent => torrent.RefreshDate).Take(2)]))
        {
            TypeArgs = [typeof(DateTime?)],
            TestName = "FindPageAsync_WhenOrderByIsRefreshDateDescAndAnchorValueIsExistingRefreshDateAndIsForwardPaginationIsFalse_ReturnsSortedArrayOfTorrents"
        };

        yield return new(new(new(TorrentOrder.RefreshDateDesc, 3, InitialTorrents[2].RefreshDate, false, 1), default, [.. InitialTorrents.OrderByDescending(static torrent => torrent.RefreshDate).Skip(1).Take(1)]))
        {
            TypeArgs = [typeof(DateTime?)],
            TestName = "FindPageAsync_WhenOrderByIsRefreshDateDescAndAnchorValueIsExistingRefreshDateAndIsForwardPaginationIsFalseAndTakeIsOne_ReturnsSortedArrayOfTorrents"
        };
    }
}
