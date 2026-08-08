using TransmissionManager.Database.Dto;
using TransmissionManager.Database.Models;
using TransmissionManager.Database.Tests.Helpers;

namespace TransmissionManager.Database.Tests;

[Parallelizable(ParallelScope.Self)]
internal sealed class TorrentServiceQueryTests : BaseTorrentServiceTests
{
    internal readonly record struct GetPageAsyncTestData<TAnchor>(
        TorrentPageDescriptor<TAnchor> Page,
        TorrentFilter Filter,
        Torrent[] ExpectedTorrents);

    [Test]
    public async Task FindOneByIdAsync_WhenIdExists_ReturnsTorrent()
    {
        using var context = CreateContext();
        var service = CreateService(context);

        var torrent = await service.FindOneByIdAsync(2).ConfigureAwait(false);

        TorrentAssertions.AssertEqual(torrent, InitialTorrents[1].Id, InitialTorrents[1]);
    }

    [Test]
    public async Task FindOneByIdAsync_WhenIdDoesNotExist_ReturnsNull()
    {
        using var context = CreateContext();
        var service = CreateService(context);

        var torrent = await service.FindOneByIdAsync(-1).ConfigureAwait(false);

        Assert.That(torrent, Is.Null);
    }

    private static IEnumerable<TestCaseData<TorrentFilter, long>> GetCountAsyncTestCases()
    {
        yield return new(default, 3) { TestName = "GetCountAsync_NoFilter" };
        yield return new(new(CronExists: true), 2) { TestName = "GetCountAsync_CronExists" };
        yield return new(new(CronExists: false), 1) { TestName = "GetCountAsync_CronMissing" };
        yield return new(new(PropertyStartsWith: "/tv"), 1) { TestName = "GetCountAsync_DownloadDirPrefix" };
        yield return new(new(PropertyStartsWith: "M"), 2) { TestName = "GetCountAsync_NamePrefix" };
        yield return new(new(PropertyStartsWith: "Mu", CronExists: true), 1) { TestName = "GetCountAsync_BothFilters" };
        yield return new(new(PropertyStartsWith: "no-such-prefix"), 0) { TestName = "GetCountAsync_NoMatch" };
    }

    [TestCaseSource(nameof(GetCountAsyncTestCases))]
    public async Task GetCountAsync_WhenCalledWithFilter_ReturnsMatchingCount(TorrentFilter filter, long expected)
    {
        using var context = CreateContext();
        var service = CreateService(context);

        var count = await service.GetCountAsync(filter).ConfigureAwait(false);

        Assert.That(count, Is.EqualTo(expected));
    }

    [TestCaseSource(nameof(GetCountAsyncTestCases))]
    public async Task GetCountAsync_MatchesGetPageAsyncItemCount(TorrentFilter filter, long expected)
    {
        using var context = CreateContext();
        var service = CreateService(context);

        var count = await service.GetCountAsync(filter).ConfigureAwait(false);
        var page = await service
            .GetPageAsync(new TorrentPageDescriptor<string>(Take: 10000), filter)
            .ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(count, Is.EqualTo(expected));
            Assert.That(page.Torrents, Has.Count.EqualTo(expected));
        }
    }

    [Test]
    public async Task GetCountAsync_WhenCalledTwice_ServesCachedValueWithoutRequeryingDatabase()
    {
        var cache = CreateCache();
        using var context1 = CreateContext();
        var service1 = CreateService(context1, cache);

        var first = await service1.GetCountAsync().ConfigureAwait(false);

        // Mutate via a second service that does NOT share the cache, so no invalidation occurs.
        using var context2 = CreateContext();
        var service2 = CreateService(context2);
        _ = await service2.DeleteOneAsync(2, 1).ConfigureAwait(false);

        var second = await service1.GetCountAsync().ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(first, Is.EqualTo(3));
            Assert.That(second, Is.EqualTo(3));
        }
    }

    [TestCaseSource(nameof(GetGetPageAsyncStringTestCases))]
    [TestCaseSource(nameof(GetGetPageAsyncDateTimeTestCases))]
    public async Task GetPageAsync_WhenCalledWithParameters_ReturnsExpectedTorrents<TAnchor>(
        GetPageAsyncTestData<TAnchor> data)
    {
        var (page, filter, expectedTorrents) = data;

        using var context = CreateContext();
        var service = CreateService(context);

        var result = await service.GetPageAsync(page, filter).ConfigureAwait(false);
        var torrents = result.Torrents;

        Assert.That(torrents, Is.Not.Null);
        Assert.That(torrents, Has.Count.EqualTo(expectedTorrents.Length));
        for (var i = 0; i < torrents.Count; i++)
            TorrentAssertions.AssertEqual(torrents[i], expectedTorrents[i].Id, expectedTorrents[i]);
    }

    private static IEnumerable<TestCaseData<GetPageAsyncTestData<string>>> GetGetPageAsyncStringTestCases()
    {
        yield return new(new(default, default, InitialTorrents))
        {
            TypeArgs = [typeof(string)],
            TestName = "GetPageAsync_WhenDefaultPaginationValuesAreUsed_ReturnsArrayOfTorrents"
        };

        yield return new(new(new(Direction: PaginationDirection.Backward), default, InitialTorrents))
        {
            TypeArgs = [typeof(string)],
            TestName = "GetPageAsync_WhenDirectionIsBackward_ReturnsArrayOfTorrents"
        };

        yield return new(new(new(Take: 2), default, InitialTorrents[..^1]))
        {
            TypeArgs = [typeof(string)],
            TestName = "GetPageAsync_WhenTakeIsTwo_ReturnsArrayWithTwoTorrents"
        };

        yield return new(new(new(Direction: PaginationDirection.Backward, Take: 2), default, InitialTorrents[1..]))
        {
            TypeArgs = [typeof(string)],
            TestName = "GetPageAsync_WhenTakeIsTwoAndDirectionIsBackward_ReturnsArrayWithTwoTorrents"
        };

        yield return new(new(new(AnchorId: 1), default, InitialTorrents[1..]))
        {
            TypeArgs = [typeof(string)],
            TestName = "GetPageAsync_WhenAnchorIdIsOne_ReturnsArrayOfTorrentsWithIdGreaterThanOne"
        };

        yield return new(new(new(AnchorId: 2, Direction: PaginationDirection.Backward), default, InitialTorrents[..1]))
        {
            TypeArgs = [typeof(string)],
            TestName = "GetPageAsync_WhenAnchorIdIsTwoAndDirectionIsBackward_ReturnsArrayOfTorrentsWithIdLessThanTwo"
        };

        yield return new(new(new(AnchorId: 3), default, []))
        {
            TypeArgs = [typeof(string)],
            TestName = "GetPageAsync_WhenAnchorIdIsLargestExistingId_ReturnsEmptyArray"
        };

        yield return new(new(new(AnchorId: 1, Direction: PaginationDirection.Backward), default, []))
        {
            TypeArgs = [typeof(string)],
            TestName = "GetPageAsync_WhenAnchorIdIsSmallestExistingIdAndDirectionIsBackward_ReturnsEmptyArray"
        };

        yield return new(new(new(AnchorId: long.MaxValue), default, []))
        {
            TypeArgs = [typeof(string)],
            TestName = "GetPageAsync_WhenAnchorIdIsGreaterThanAnyTorrentId_ReturnsEmptyArray"
        };

        yield return new(new(new(AnchorId: long.MinValue, Direction: PaginationDirection.Backward), default, []))
        {
            TypeArgs = [typeof(string)],
            TestName = "GetPageAsync_WhenAnchorIdIsSmallerThanAnyTorrentIdAndDirectionIsBackward_ReturnsEmptyArray"
        };

        yield return new(new(new(AnchorId: long.MinValue), default, InitialTorrents))
        {
            TypeArgs = [typeof(string)],
            TestName = "GetPageAsync_WhenAnchorIdIsNegative_ReturnsArrayOfTorrents"
        };

        yield return new(new(new(AnchorId: long.MaxValue, Direction: PaginationDirection.Backward), default, InitialTorrents))
        {
            TypeArgs = [typeof(string)],
            TestName = "GetPageAsync_WhenAnchorIdIsMaxPossibleValueAndDirectionIsBackward_ReturnsArrayOfTorrents"
        };

        yield return new(new(default, new(InitialTorrents[1].HashString), InitialTorrents[1..^1]))
        {
            TypeArgs = [typeof(string)],
            TestName = "GetPageAsync_WhenPropertyStartsWithIsFullHashString_ReturnsFilteredArrayOfTorrents"
        };

        yield return new(new(default, new(InitialTorrents[1].HashString.ToUpperInvariant()), InitialTorrents[1..^1]))
        {
            TypeArgs = [typeof(string)],
            TestName = "GetPageAsync_WhenPropertyStartsWithIsFullUppercasedHashString_ReturnsFilteredArrayOfTorrents"
        };

        yield return new(new(default, new(InitialTorrents[1].HashString[..20]), InitialTorrents[1..^1]))
        {
            TypeArgs = [typeof(string)],
            TestName = "GetPageAsync_WhenPropertyStartsWithIsPartialHashString_ReturnsFilteredArrayOfTorrents"
        };

        yield return new(new(new(Direction: PaginationDirection.Backward), new(InitialTorrents[1].HashString[..20]), InitialTorrents[1..^1]))
        {
            TypeArgs = [typeof(string)],
            TestName = "GetPageAsync_WhenPropertyStartsWithIsPartialHashStringAndDirectionIsBackward_ReturnsFilteredArrayOfTorrents"
        };

        yield return new(new(default, new(InitialTorrents[1].SourceUri), InitialTorrents[1..^1]))
        {
            TypeArgs = [typeof(string)],
            TestName = "GetPageAsync_WhenPropertyStartsWithIsFullSourceUri_ReturnsFilteredArrayOfTorrents"
        };

        yield return new(new(default, new(InitialTorrents[1].SourceUri.ToUpperInvariant()), InitialTorrents[1..^1]))
        {
            TypeArgs = [typeof(string)],
            TestName = "GetPageAsync_WhenPropertyStartsWithIsFullUppercasedSourceUri_ReturnsFilteredArrayOfTorrents"
        };

        yield return new(new(default, new(InitialTorrents[1].SourceUri[..^1]), InitialTorrents))
        {
            TypeArgs = [typeof(string)],
            TestName = "GetPageAsync_WhenPropertyStartsWithIsPartialSourceUri_ReturnsFilteredArrayOfTorrents"
        };

        yield return new(new(new(Direction: PaginationDirection.Backward, Take: 2), new(InitialTorrents[1].SourceUri[..^1]), InitialTorrents[1..]))
        {
            TypeArgs = [typeof(string)],
            TestName = "GetPageAsync_WhenPropertyStartsWithIsPartialSourceUriAndDirectionIsBackward_ReturnsFilteredArrayOfTorrents"
        };

        yield return new(new(default, new(InitialTorrents[1].Name), InitialTorrents[1..^1]))
        {
            TypeArgs = [typeof(string)],
            TestName = "GetPageAsync_WhenPropertyStartsWithIsFullName_ReturnsFilteredArrayOfTorrents"
        };

        yield return new(new(default, new(InitialTorrents[1].Name.ToUpperInvariant()), InitialTorrents[1..^1]))
        {
            TypeArgs = [typeof(string)],
            TestName = "GetPageAsync_WhenPropertyStartsWithIsFullUppercasedName_ReturnsFilteredArrayOfTorrents"
        };

        yield return new(new(default, new(InitialTorrents[1].Name[..^1]), InitialTorrents[1..^1]))
        {
            TypeArgs = [typeof(string)],
            TestName = "GetPageAsync_WhenPropertyStartsWithIsPartialName_ReturnsFilteredArrayOfTorrents"
        };

        yield return new(new(new(Direction: PaginationDirection.Backward), new(InitialTorrents[1].Name[..^1]), InitialTorrents[1..^1]))
        {
            TypeArgs = [typeof(string)],
            TestName = "GetPageAsync_WhenPropertyStartsWithIsPartialNameAndDirectionIsBackward_ReturnsFilteredArrayOfTorrents"
        };

        yield return new(new(default, new(CronExists: true), [InitialTorrents[0], InitialTorrents[2]]))
        {
            TypeArgs = [typeof(string)],
            TestName = "GetPageAsync_WhenCronExistsIsTrue_ReturnsFilteredArrayOfTorrents"
        };

        yield return new(new(new(Direction: PaginationDirection.Backward, Take: 1), new(CronExists: true), InitialTorrents[2..]))
        {
            TypeArgs = [typeof(string)],
            TestName = "GetPageAsync_WhenCronExistsIsTrueAndDirectionIsBackward_ReturnsFilteredArrayOfTorrents"
        };

        yield return new(new(default, new(CronExists: false), [InitialTorrents[1]]))
        {
            TypeArgs = [typeof(string)],
            TestName = "GetPageAsync_WhenCronExistsIsFalse_ReturnsFilteredArrayOfTorrents"
        };

        yield return new(new(new(Direction: PaginationDirection.Backward), new(CronExists: false), [InitialTorrents[1]]))
        {
            TypeArgs = [typeof(string)],
            TestName = "GetPageAsync_WhenCronExistsIsFalseAndDirectionIsBackward_ReturnsFilteredArrayOfTorrents"
        };

        yield return new(new(default, new(InitialTorrents[2].Name[..1], true), InitialTorrents[2..]))
        {
            TypeArgs = [typeof(string)],
            TestName = "GetPageAsync_WhenMultipleFiltersAreUsed_ReturnsFilteredArrayOfTorrents"
        };

        yield return new(new(new(Direction: PaginationDirection.Backward), new(InitialTorrents[2].Name[..1], true), InitialTorrents[2..]))
        {
            TypeArgs = [typeof(string)],
            TestName = "GetPageAsync_WhenMultipleFiltersAreUsedAndDirectionIsBackward_ReturnsFilteredArrayOfTorrents"
        };

        yield return new(new(new(TorrentOrder.Id), default, InitialTorrents))
        {
            TypeArgs = [typeof(string)],
            TestName = "GetPageAsync_WhenOrderByIsId_ReturnsSortedArrayOfTorrents"
        };

        yield return new(new(new(TorrentOrder.Id, Direction: PaginationDirection.Backward, Take: 2), default, InitialTorrents[1..]))
        {
            TypeArgs = [typeof(string)],
            TestName = "GetPageAsync_WhenOrderByIsIdAndDirectionIsBackward_ReturnsSortedArrayOfTorrents"
        };

        yield return new(new(new(TorrentOrder.IdDesc), default, [.. InitialTorrents.Reverse()]))
        {
            TypeArgs = [typeof(string)],
            TestName = "GetPageAsync_WhenOrderByIsIdDesc_ReturnsSortedArrayOfTorrents"
        };

        yield return new(new(new(TorrentOrder.IdDesc, Direction: PaginationDirection.Backward, Take: 2), default, [.. InitialTorrents[..^1].Reverse()]))
        {
            TypeArgs = [typeof(string)],
            TestName = "GetPageAsync_WhenOrderByIsIdDescAndDirectionIsBackward_ReturnsSortedArrayOfTorrents"
        };

        yield return new(new(new(TorrentOrder.Name), default, [.. InitialTorrents.OrderBy(static torrent => torrent.Name)]))
        {
            TypeArgs = [typeof(string)],
            TestName = "GetPageAsync_WhenOrderByIsName_ReturnsSortedArrayOfTorrents"
        };

        yield return new(new(new(TorrentOrder.Name, Direction: PaginationDirection.Backward, Take: 2), default, [.. InitialTorrents.OrderBy(static torrent => torrent.Name).Skip(1)]))
        {
            TypeArgs = [typeof(string)],
            TestName = "GetPageAsync_WhenOrderByIsNameAndDirectionIsBackward_ReturnsSortedArrayOfTorrents"
        };

        yield return new(new(new(TorrentOrder.NameDesc), default, [.. InitialTorrents.OrderByDescending(static torrent => torrent.Name)]))
        {
            TypeArgs = [typeof(string)],
            TestName = "GetPageAsync_WhenOrderByIsNameDesc_ReturnsSortedArrayOfTorrents"
        };

        yield return new(new(new(TorrentOrder.NameDesc, Direction: PaginationDirection.Backward, Take: 2), default, [.. InitialTorrents.OrderByDescending(static torrent => torrent.Name).Skip(1)]))
        {
            TypeArgs = [typeof(string)],
            TestName = "GetPageAsync_WhenOrderByIsNameDescAndDirectionIsBackward_ReturnsSortedArrayOfTorrents"
        };

        yield return new(new(new(TorrentOrder.Uri), default, InitialTorrents))
        {
            TypeArgs = [typeof(string)],
            TestName = "GetPageAsync_WhenOrderByIsUri_ReturnsSortedArrayOfTorrents"
        };

        yield return new(new(new(TorrentOrder.Uri, Direction: PaginationDirection.Backward, Take: 2), default, InitialTorrents[1..]))
        {
            TypeArgs = [typeof(string)],
            TestName = "GetPageAsync_WhenOrderByIsUriAndDirectionIsBackward_ReturnsSortedArrayOfTorrents"
        };

        yield return new(new(new(TorrentOrder.UriDesc), default, [.. InitialTorrents.Reverse()]))
        {
            TypeArgs = [typeof(string)],
            TestName = "GetPageAsync_WhenOrderByIsUriDesc_ReturnsSortedArrayOfTorrents"
        };

        yield return new(new(new(TorrentOrder.UriDesc, Direction: PaginationDirection.Backward, Take: 2), default, [.. InitialTorrents.Reverse().Skip(1)]))
        {
            TypeArgs = [typeof(string)],
            TestName = "GetPageAsync_WhenOrderByIsUriDescAndDirectionIsBackward_ReturnsSortedArrayOfTorrents"
        };

        yield return new(new(new(TorrentOrder.DownloadDir), default, [.. InitialTorrents.OrderBy(static torrent => torrent.DownloadDir)]))
        {
            TypeArgs = [typeof(string)],
            TestName = "GetPageAsync_WhenOrderByIsDownloadDir_ReturnsSortedArrayOfTorrents"
        };

        yield return new(new(new(TorrentOrder.DownloadDir, Direction: PaginationDirection.Backward, Take: 2), default, [.. InitialTorrents.OrderBy(static torrent => torrent.DownloadDir).Skip(1)]))
        {
            TypeArgs = [typeof(string)],
            TestName = "GetPageAsync_WhenOrderByIsDownloadDirAndDirectionIsBackward_ReturnsSortedArrayOfTorrents"
        };

        yield return new(new(new(TorrentOrder.DownloadDirDesc), default, [.. InitialTorrents.OrderByDescending(static torrent => torrent.DownloadDir)]))
        {
            TypeArgs = [typeof(string)],
            TestName = "GetPageAsync_WhenOrderByIsDownloadDirDesc_ReturnsSortedArrayOfTorrents"
        };

        yield return new(new(new(TorrentOrder.DownloadDirDesc, Direction: PaginationDirection.Backward, Take: 2), default, [.. InitialTorrents.OrderByDescending(static torrent => torrent.DownloadDir).Skip(1)]))
        {
            TypeArgs = [typeof(string)],
            TestName = "GetPageAsync_WhenOrderByIsDownloadDirDescAndDirectionIsBackward_ReturnsSortedArrayOfTorrents"
        };

        yield return new(new(new(TorrentOrder.Name, AnchorValue: string.Empty), default, [.. InitialTorrents.OrderBy(static torrent => torrent.Name)]))
        {
            TypeArgs = [typeof(string)],
            TestName = "GetPageAsync_WhenOrderByIsNameAndAnchorValueIsEmptyString_ReturnsPageOfTorrents"
        };

        yield return new(new(new(TorrentOrder.Name, AnchorValue: string.Empty, Direction: PaginationDirection.Backward), default, []))
        {
            TypeArgs = [typeof(string)],
            TestName = "GetPageAsync_WhenOrderByIsNameAndAnchorValueIsEmptyStringAndDirectionIsBackward_ReturnsEmptyPageOfTorrents"
        };

        yield return new(new(new(TorrentOrder.Name, 2, InitialTorrents[1].Name), default, [.. InitialTorrents.OrderBy(static torrent => torrent.Name).Skip(1)]))
        {
            TypeArgs = [typeof(string)],
            TestName = "GetPageAsync_WhenOrderByIsNameAndAnchorValueIsExistingName_ReturnsPageOfTorrents"
        };

        yield return new(new(new(TorrentOrder.Name, 3, InitialTorrents[2].Name, Direction: PaginationDirection.Backward), default, [.. InitialTorrents.OrderBy(static torrent => torrent.Name).Take(1)]))
        {
            TypeArgs = [typeof(string)],
            TestName = "GetPageAsync_WhenOrderByIsNameAndAnchorValueIsExistingNameAndDirectionIsBackward_ReturnsPageOfTorrents"
        };

        yield return new(new(new(TorrentOrder.NameDesc, AnchorValue: string.Empty), default, []))
        {
            TypeArgs = [typeof(string)],
            TestName = "GetPageAsync_WhenOrderByIsNameDescAndAnchorValueIsEmptyString_ReturnsEmptyPageOfTorrents"
        };

        yield return new(new(new(TorrentOrder.NameDesc, AnchorValue: string.Empty, Direction: PaginationDirection.Backward, Take: 2), default, [.. InitialTorrents.OrderByDescending(static torrent => torrent.Name).Skip(1)]))
        {
            TypeArgs = [typeof(string)],
            TestName = "GetPageAsync_WhenOrderByIsNameDescAndAnchorValueIsEmptyStringAndDirectionIsBackward_ReturnsPageOfTorrents"
        };

        yield return new(new(new(TorrentOrder.NameDesc, 1, InitialTorrents[0].Name), default, [.. InitialTorrents.OrderByDescending(static torrent => torrent.Name).Skip(1)]))
        {
            TypeArgs = [typeof(string)],
            TestName = "GetPageAsync_WhenOrderByIsNameDescAndAnchorValueIsExistingName_ReturnsPageOfTorrents"
        };

        yield return new(new(new(TorrentOrder.NameDesc, 2, InitialTorrents[1].Name, Direction: PaginationDirection.Backward), default, [.. InitialTorrents.OrderByDescending(static torrent => torrent.Name).Take(2)]))
        {
            TypeArgs = [typeof(string)],
            TestName = "GetPageAsync_WhenOrderByIsNameDescAndAnchorValueIsExistingNameAndDirectionIsBackward_ReturnsPageOfTorrents"
        };

        yield return new(new(new(TorrentOrder.Uri, AnchorValue: string.Empty), default, InitialTorrents))
        {
            TypeArgs = [typeof(string)],
            TestName = "GetPageAsync_WhenOrderByIsUriAndAnchorValueIsEmptyString_ReturnsPageOfTorrents"
        };

        yield return new(new(new(TorrentOrder.Uri, AnchorValue: string.Empty, Direction: PaginationDirection.Backward), default, []))
        {
            TypeArgs = [typeof(string)],
            TestName = "GetPageAsync_WhenOrderByIsUriAndAnchorValueIsEmptyStringAndDirectionIsBackward_ReturnsEmptyPageOfTorrents"
        };

        yield return new(new(new(TorrentOrder.Uri, 1, InitialTorrents[0].SourceUri), default, [.. InitialTorrents.OrderBy(static torrent => torrent.SourceUri).Skip(1)]))
        {
            TypeArgs = [typeof(string)],
            TestName = "GetPageAsync_WhenOrderByIsUriAndAnchorValueIsExistingSourceUri_ReturnsPageOfTorrents"
        };

        yield return new(new(new(TorrentOrder.Uri, 3, InitialTorrents[2].SourceUri, Direction: PaginationDirection.Backward), default, [.. InitialTorrents.OrderBy(static torrent => torrent.SourceUri).Take(2)]))
        {
            TypeArgs = [typeof(string)],
            TestName = "GetPageAsync_WhenOrderByIsUriAndAnchorValueIsExistingSourceUriAndDirectionIsBackward_ReturnsPageOfTorrents"
        };

        yield return new(new(new(TorrentOrder.UriDesc, AnchorValue: string.Empty), default, []))
        {
            TypeArgs = [typeof(string)],
            TestName = "GetPageAsync_WhenOrderByIsUriDescAndAnchorValueIsEmptyString_ReturnsEmptyPageOfTorrents"
        };

        yield return new(new(new(TorrentOrder.UriDesc, AnchorValue: string.Empty, Direction: PaginationDirection.Backward, Take: 2), default, [.. InitialTorrents.OrderByDescending(static torrent => torrent.SourceUri).Skip(1)]))
        {
            TypeArgs = [typeof(string)],
            TestName = "GetPageAsync_WhenOrderByIsUriDescAndAnchorValueIsEmptyStringAndDirectionIsBackward_ReturnsPageOfTorrents"
        };

        yield return new(new(new(TorrentOrder.UriDesc, 3, InitialTorrents[2].SourceUri), default, [.. InitialTorrents.OrderByDescending(static torrent => torrent.SourceUri).Skip(1)]))
        {
            TypeArgs = [typeof(string)],
            TestName = "GetPageAsync_WhenOrderByIsUriDescAndAnchorValueIsExistingSourceUri_ReturnsPageOfTorrents"
        };

        yield return new(new(new(TorrentOrder.UriDesc, 1, InitialTorrents[0].SourceUri, Direction: PaginationDirection.Backward), default, [.. InitialTorrents.OrderByDescending(static torrent => torrent.SourceUri).Take(2)]))
        {
            TypeArgs = [typeof(string)],
            TestName = "GetPageAsync_WhenOrderByIsUriDescAndAnchorValueIsExistingSourceUriAndDirectionIsBackward_ReturnsPageOfTorrents"
        };

        yield return new(new(new(TorrentOrder.DownloadDir, AnchorValue: string.Empty), default, [.. InitialTorrents.OrderBy(static torrent => torrent.DownloadDir)]))
        {
            TypeArgs = [typeof(string)],
            TestName = "GetPageAsync_WhenOrderByIsDownloadDirAndAnchorValueIsEmptyString_ReturnsPageOfTorrents"
        };

        yield return new(new(new(TorrentOrder.DownloadDir, AnchorValue: string.Empty, Direction: PaginationDirection.Backward), default, []))
        {
            TypeArgs = [typeof(string)],
            TestName = "GetPageAsync_WhenOrderByIsDownloadDirAndAnchorValueIsEmptyStringAndDirectionIsBackward_ReturnsEmptyPageOfTorrents"
        };

        yield return new(new(new(TorrentOrder.DownloadDir, 2, InitialTorrents[1].DownloadDir), default, [.. InitialTorrents.OrderBy(static torrent => torrent.DownloadDir).Skip(1)]))
        {
            TypeArgs = [typeof(string)],
            TestName = "GetPageAsync_WhenOrderByIsDownloadDirAndAnchorValueIsExistingDownloadDir_ReturnsPageOfTorrents"
        };

        yield return new(new(new(TorrentOrder.DownloadDir, 3, InitialTorrents[2].DownloadDir, Direction: PaginationDirection.Backward), default, [.. InitialTorrents.OrderBy(static torrent => torrent.DownloadDir).Take(2)]))
        {
            TypeArgs = [typeof(string)],
            TestName = "GetPageAsync_WhenOrderByIsDownloadDirAndAnchorValueIsExistingDownloadDirAndDirectionIsBackward_ReturnsPageOfTorrents"
        };

        yield return new(new(new(TorrentOrder.DownloadDirDesc, 3, InitialTorrents[2].DownloadDir), default, [.. InitialTorrents.OrderByDescending(static torrent => torrent.DownloadDir).Skip(1)]))
        {
            TypeArgs = [typeof(string)],
            TestName = "GetPageAsync_WhenOrderByIsDownloadDirDescAndAnchorValueIsExistingDownloadDir_ReturnsPageOfTorrents"
        };

        yield return new(new(new(TorrentOrder.DownloadDirDesc, 2, InitialTorrents[1].DownloadDir, Direction: PaginationDirection.Backward), default, [.. InitialTorrents.OrderByDescending(static torrent => torrent.DownloadDir).Take(2)]))
        {
            TypeArgs = [typeof(string)],
            TestName = "GetPageAsync_WhenOrderByIsDownloadDirDescAndAnchorValueIsExistingDownloadDirAndDirectionIsBackward_ReturnsPageOfTorrents"
        };

        yield return new(new(new(TorrentOrder.NameDesc, 1, InitialTorrents[0].Name), new("m", true), [InitialTorrents[2]]))
        {
            TypeArgs = [typeof(string)],
            TestName = "GetPageAsync_WhenOrderByIsNameDescAndAnchorValueIsExistingValueAndPropertyStartsWithIsM_ReturnsPageOfTorrents"
        };
    }

    private static IEnumerable<TestCaseData<GetPageAsyncTestData<DateTime?>>> GetGetPageAsyncDateTimeTestCases()
    {
        yield return new(new(new(TorrentOrder.RefreshDate), default, [.. InitialTorrents.OrderBy(static torrent => torrent.RefreshDate)]))
        {
            TypeArgs = [typeof(DateTime?)],
            TestName = "GetPageAsync_WhenOrderByIsRefreshDate_ReturnsSortedArrayOfTorrents"
        };

        yield return new(new(new(TorrentOrder.RefreshDate, 3, InitialTorrents[2].RefreshDate), default, [.. InitialTorrents.OrderBy(static torrent => torrent.RefreshDate).Skip(1)]))
        {
            TypeArgs = [typeof(DateTime?)],
            TestName = "GetPageAsync_WhenOrderByIsRefreshDateAndAnchorValueIsExistingRefreshDate_ReturnsSortedArrayOfTorrents"
        };

        yield return new(new(new(TorrentOrder.RefreshDate, 3, InitialTorrents[2].RefreshDate, Take: 1), default, [.. InitialTorrents.OrderBy(static torrent => torrent.RefreshDate).Skip(1).Take(1)]))
        {
            TypeArgs = [typeof(DateTime?)],
            TestName = "GetPageAsync_WhenOrderByIsRefreshDateAndAnchorValueIsExistingRefreshDateAndTakeIsOne_ReturnsSortedArrayOfTorrents"
        };

        yield return new(new(new(TorrentOrder.RefreshDate, 1, InitialTorrents[0].RefreshDate, PaginationDirection.Backward), default, [.. InitialTorrents.OrderBy(static torrent => torrent.RefreshDate).Take(2)]))
        {
            TypeArgs = [typeof(DateTime?)],
            TestName = "GetPageAsync_WhenOrderByIsRefreshDateAndAnchorValueIsExistingRefreshDateAndDirectionIsBackward_ReturnsSortedArrayOfTorrents"
        };

        yield return new(new(new(TorrentOrder.RefreshDate, 1, InitialTorrents[0].RefreshDate, PaginationDirection.Backward, 1), default, [.. InitialTorrents.OrderBy(static torrent => torrent.RefreshDate).Skip(1).Take(1)]))
        {
            TypeArgs = [typeof(DateTime?)],
            TestName = "GetPageAsync_WhenOrderByIsRefreshDateAndAnchorValueIsExistingRefreshDateAndDirectionIsBackwardAndTakeIsOne_ReturnsSortedArrayOfTorrents"
        };

        yield return new(new(new(TorrentOrder.RefreshDateDesc), default, [.. InitialTorrents.OrderByDescending(static torrent => torrent.RefreshDate)]))
        {
            TypeArgs = [typeof(DateTime?)],
            TestName = "GetPageAsync_WhenOrderByIsRefreshDateDesc_ReturnsSortedArrayOfTorrents"
        };

        yield return new(new(new(TorrentOrder.RefreshDateDesc, 1, InitialTorrents[0].RefreshDate), default, [.. InitialTorrents.OrderByDescending(static torrent => torrent.RefreshDate).Skip(1)]))
        {
            TypeArgs = [typeof(DateTime?)],
            TestName = "GetPageAsync_WhenOrderByIsRefreshDateDescAndAnchorValueIsExistingRefreshDate_ReturnsSortedArrayOfTorrents"
        };

        yield return new(new(new(TorrentOrder.RefreshDateDesc, 1, InitialTorrents[0].RefreshDate, Take: 1), default, [.. InitialTorrents.OrderByDescending(static torrent => torrent.RefreshDate).Skip(1).Take(1)]))
        {
            TypeArgs = [typeof(DateTime?)],
            TestName = "GetPageAsync_WhenOrderByIsRefreshDateDescAndAnchorValueIsExistingRefreshDateAndTakeIsOne_ReturnsSortedArrayOfTorrents"
        };

        yield return new(new(new(TorrentOrder.RefreshDateDesc, 3, InitialTorrents[2].RefreshDate, PaginationDirection.Backward), default, [.. InitialTorrents.OrderByDescending(static torrent => torrent.RefreshDate).Take(2)]))
        {
            TypeArgs = [typeof(DateTime?)],
            TestName = "GetPageAsync_WhenOrderByIsRefreshDateDescAndAnchorValueIsExistingRefreshDateAndDirectionIsBackward_ReturnsSortedArrayOfTorrents"
        };

        yield return new(new(new(TorrentOrder.RefreshDateDesc, 3, InitialTorrents[2].RefreshDate, PaginationDirection.Backward, 1), default, [.. InitialTorrents.OrderByDescending(static torrent => torrent.RefreshDate).Skip(1).Take(1)]))
        {
            TypeArgs = [typeof(DateTime?)],
            TestName = "GetPageAsync_WhenOrderByIsRefreshDateDescAndAnchorValueIsExistingRefreshDateAndDirectionIsBackwardAndTakeIsOne_ReturnsSortedArrayOfTorrents"
        };
    }

    internal readonly record struct GetPageAsyncHasMoreTestData(
        TorrentPageDescriptor<string> Page,
        TorrentFilter Filter,
        Torrent[] ExpectedTorrents,
        bool ExpectedHasMore);

    [TestCaseSource(nameof(GetGetPageAsyncHasMoreTestCases))]
    public async Task GetPageAsync_HasMore_ReturnsExpectedHasMoreAndExpectedTorrents(
        GetPageAsyncHasMoreTestData data)
    {
        var (page, filter, expectedTorrents, expectedHasMore) = data;

        using var context = CreateContext();
        var service = CreateService(context);

        var result = await service.GetPageAsync(page, filter).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Torrents, Has.Count.EqualTo(expectedTorrents.Length));
            Assert.That(result.HasMore, Is.EqualTo(expectedHasMore));
        }

        for (var i = 0; i < result.Torrents.Count; i++)
            TorrentAssertions.AssertEqual(result.Torrents[i], expectedTorrents[i].Id, expectedTorrents[i]);
    }

    private static IEnumerable<TestCaseData<GetPageAsyncHasMoreTestData>> GetGetPageAsyncHasMoreTestCases()
    {
        yield return new(new(new(Take: 2), default, [InitialTorrents[0], InitialTorrents[1]], ExpectedHasMore: true))
        {
            TestName = "GetPageAsync_WhenMoreRowsExistThanRequested_ReturnsHasMoreTrueAndExpectedSlice"
        };

        yield return new(new(new(Direction: PaginationDirection.Backward, Take: 2), default, [InitialTorrents[1], InitialTorrents[2]], ExpectedHasMore: true))
        {
            TestName = "GetPageAsync_WhenMoreRowsExistThanRequestedAndDirectionIsBackward_ReturnsHasMoreTrueAndCorrectSlice"
        };

        yield return new(new(new(Take: 3), default, InitialTorrents, ExpectedHasMore: false))
        {
            TestName = "GetPageAsync_WhenExactlyTakeRowsExist_ReturnsHasMoreFalse"
        };

        yield return new(new(new(Take: 5), default, InitialTorrents, ExpectedHasMore: false))
        {
            TestName = "GetPageAsync_WhenFewerRowsExistThanRequested_ReturnsHasMoreFalse"
        };

        yield return new(new(new(AnchorId: long.MaxValue, Take: 5), default, [], ExpectedHasMore: false))
        {
            TestName = "GetPageAsync_WhenAnchorIdPointsPastEnd_ReturnsEmptyAndHasMoreFalse"
        };

        yield return new(new(new(Take: 1), new("TV"), [InitialTorrents[0]], ExpectedHasMore: false))
        {
            TestName = "GetPageAsync_WhenFilterMatchesExactlyTake_ReturnsHasMoreFalse"
        };

        yield return new(new(new(Take: 2), new("https://torrentTracker.com"), [InitialTorrents[0], InitialTorrents[1]], ExpectedHasMore: true))
        {
            TestName = "GetPageAsync_WhenFilterMatchesMoreThanTake_ReturnsHasMoreTrue"
        };
    }
}
