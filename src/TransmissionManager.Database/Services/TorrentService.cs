using Microsoft.EntityFrameworkCore;
using TransmissionManager.Database.Dto;
using TransmissionManager.Database.Extensions;
using TransmissionManager.Database.Models;

namespace TransmissionManager.Database.Services;

/// <remarks>
/// <para>
/// The OCC mutation paths (<see cref="UpdateOneAsync"/> and
/// <see cref="DeleteOneAsync"/>) use <c>ExecuteUpdateAsync</c> /
/// <c>ExecuteDeleteAsync</c> with an explicit
/// <c>Id == id &amp;&amp; Version == version</c> predicate. They bypass the EF
/// change tracker and therefore do <b>not</b> throw
/// <see cref="DbUpdateConcurrencyException"/>; the affected-row count plus a
/// single disambiguating SELECT (<see cref="ResolveLostRaceAsync"/>) decides
/// between Success / NotFound / Conflict. This relies on the underlying
/// provider not reusing primary-key values for deleted rows
/// (SQLite <c>AUTOINCREMENT</c>, PostgreSQL sequences, SQL Server
/// <c>IDENTITY</c>, MySQL <c>AUTO_INCREMENT</c>).
/// </para>
/// <para>
/// The disambiguation result is best-effort: it reflects the row state at the
/// follow-up SELECT, which can legitimately differ from the state at the
/// failed UPDATE/DELETE under continued concurrent churn (e.g. another writer
/// deletes the row between the failed mutation and the SELECT, turning what
/// was a Conflict into a NotFound). Do not try to harden this with a
/// transaction — that would re-introduce the second round-trip on the hot path
/// that this design was built to eliminate.
/// </para>
/// <para>
/// <b>Future-maintainer warning:</b> if you add a new method that mutates a
/// <c>Torrent</c> via the EF change tracker (<c>Add</c> / <c>Update</c> /
/// <c>Remove</c> followed by <c>SaveChangesAsync</c>), the
/// <c>[ConcurrencyCheck]</c> attribute on <c>Torrent.Version</c> will arm a
/// <c>WHERE Version=@orig</c> filter on the generated SQL and
/// <see cref="DbUpdateConcurrencyException"/> can fire. You must catch it,
/// map to <c>Conflict</c> / <c>NotFound</c> via
/// <see cref="ResolveLostRaceAsync"/>, and call
/// <c>dbContext.ChangeTracker.Clear()</c> in the catch — otherwise the
/// failed entity stays tracked with stale <c>OriginalValues</c> and the next
/// <c>SaveChangesAsync</c> on the same scoped <c>DbContext</c> would
/// silently retry the lost write.
/// </para>
/// </remarks>
public sealed class TorrentService(AppDbContext dbContext)
{
    public async Task<Torrent?> FindOneByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        return await dbContext.Torrents.AsNoTracking()
            .FirstOrDefaultAsync(torrent => torrent.Id == id, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Returns a keyset-paginated page of torrents matching <paramref name="filter"/>, anchored
    /// by <paramref name="page"/>.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="page"/>.<see cref="TorrentPageDescriptor{TAnchor}.Take"/>
    /// equals <see cref="int.MaxValue"/>: the internal probe computes <c>Take + 1</c>, which
    /// overflows to <see cref="int.MinValue"/> and is rejected by
    /// <see cref="TorrentPageDescriptor{TAnchor}"/>'s validator.
    /// </exception>
    public async Task<TorrentPage> GetPageAsync<T>(
        TorrentPageDescriptor<T> page = default,
        TorrentFilter filter = default,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Torrents.AsNoTracking();

        // Filter
        if (!string.IsNullOrEmpty(filter.PropertyStartsWith))
        {
            query = query.Where(torrent =>
                torrent.HashString.StartsWith(filter.PropertyStartsWith) ||
                torrent.Name.StartsWith(filter.PropertyStartsWith) ||
                torrent.WebPageUri.StartsWith(filter.PropertyStartsWith) ||
                torrent.DownloadDir.StartsWith(filter.PropertyStartsWith));
        }

        if (filter.CronExists is not null)
            query = query.Where(torrent => filter.CronExists.Value ? torrent.Cron != null : torrent.Cron == null);

        // Paginate
        if (page == default)
            page = new();

        var pageToFetch = new TorrentPageDescriptor<T>(
            AnchorId: page.AnchorId,
            OrderBy: page.OrderBy,
            AnchorValue: page.AnchorValue,
            Direction: page.Direction,
            Take: page.Take + 1);

        var fetched = await query.WhereOrderByTake(pageToFetch)
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);

        if (fetched.Length <= page.Take)
            return new TorrentPage(fetched, HasMore: false);

        var sourceStart = page.Direction is PaginationDirection.Forward ? 0 : fetched.Length - page.Take;
        var torrents = fetched.AsSpan(sourceStart, page.Take).ToArray();
        return new TorrentPage(torrents, HasMore: true);
    }

    public async Task<Torrent> AddOneAsync(TorrentAddDto dto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var torrent = dto.ToTorrent();
        _ = dbContext.Torrents.Add(torrent);
        _ = await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return torrent;
    }

    public async Task<TorrentMutationOutcome> UpdateOneAsync(
        long id,
        long version,
        TorrentUpdateDto dto,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var affected = await dbContext.Torrents
            .Where(torrent => torrent.Id == id && torrent.Version == version)
            .ExecuteUpdateAsync(
                properties => properties
                    .SetProperty(
                        static torrent => torrent.HashString,
                        torrent => dto.HashString ?? torrent.HashString)
                    .SetProperty(
                        static torrent => torrent.RefreshDate,
                        torrent => dto.RefreshDate ?? torrent.RefreshDate)
                    .SetProperty(
                        static torrent => torrent.Name,
                        torrent => dto.Name ?? torrent.Name)
                    .SetProperty(
                        static torrent => torrent.DownloadDir,
                        torrent => dto.DownloadDir ?? torrent.DownloadDir)
                    .SetProperty(
                        static torrent => torrent.MagnetRegexPattern,
                        torrent => dto.MagnetRegexPattern != null && dto.MagnetRegexPattern.Length == 0
                            ? null
                            : dto.MagnetRegexPattern ?? torrent.MagnetRegexPattern)
                    .SetProperty(
                        static torrent => torrent.Cron,
                        torrent => dto.Cron != null && dto.Cron.Length == 0
                            ? null
                            : dto.Cron ?? torrent.Cron)
                    .SetProperty(
                        static torrent => torrent.Version,
                        static torrent => torrent.Version + 1),
                cancellationToken)
            .ConfigureAwait(false);

        return affected is 1
            ? new(TorrentMutationResult.Success, version + 1)
            : await ResolveLostRaceAsync(id, cancellationToken).ConfigureAwait(false);
    }

    public async Task<TorrentMutationOutcome> DeleteOneAsync(
        long id,
        long version,
        CancellationToken cancellationToken = default)
    {
        var affected = await dbContext.Torrents
            .Where(torrent => torrent.Id == id && torrent.Version == version)
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);

        return affected is 1
            ? new(TorrentMutationResult.Success, version)
            : await ResolveLostRaceAsync(id, cancellationToken).ConfigureAwait(false);
    }

    private async Task<TorrentMutationOutcome> ResolveLostRaceAsync(
        long id,
        CancellationToken cancellationToken)
    {
        var current = await dbContext.Torrents
            .AsNoTracking()
            .Where(t => t.Id == id)
            .Select(static t => (long?)t.Version)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return current is null
            ? new(TorrentMutationResult.NotFound, null)
            : new(TorrentMutationResult.Conflict, current);
    }
}
