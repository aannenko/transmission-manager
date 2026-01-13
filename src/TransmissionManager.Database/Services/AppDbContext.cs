using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using System.Diagnostics.CodeAnalysis;
using TransmissionManager.Database.Models;

namespace TransmissionManager.Database.Services;

[method: DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(EntryCurrentValueComparer<>))]
#pragma warning disable EF1001 // Internal EF Core API usage - required to prevent required class members from being trimmed
[method: DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(NullableClassCurrentProviderValueComparer<,>))]
#pragma warning restore EF1001 // Internal EF Core API usage.
[UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "Tested after trimming")]
public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    private const string _noCaseCollation = "NOCASE";

    public DbSet<Torrent> Torrents => Set<Torrent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        var torrentBuilder = modelBuilder.Entity<Torrent>();

        _ = torrentBuilder.HasIndex(static torrent => torrent.HashString).IsUnique(true);
        _ = torrentBuilder.Property(static torrent => torrent.HashString).UseCollation(_noCaseCollation);

        _ = torrentBuilder.HasIndex(static torrent => torrent.RefreshDate);
        _ = torrentBuilder.Property(static torrent => torrent.RefreshDate).HasConversion(
            static date => date.ToUniversalTime(),
            static date => DateTime.SpecifyKind(date, DateTimeKind.Utc));

        _ = torrentBuilder.HasIndex(static torrent => torrent.Name);
        _ = torrentBuilder.Property(static torrent => torrent.Name).UseCollation(_noCaseCollation);

        _ = torrentBuilder.HasIndex(static torrent => torrent.WebPageUri).IsUnique(true);
        _ = torrentBuilder.Property(static torrent => torrent.WebPageUri).UseCollation(_noCaseCollation);

        _ = torrentBuilder.HasIndex(static torrent => torrent.DownloadDir);
        _ = torrentBuilder.Property(static torrent => torrent.DownloadDir).UseCollation(_noCaseCollation);

        _ = torrentBuilder.HasIndex(static torrent => torrent.Cron).HasFilter($"{nameof(Torrent.Cron)} IS NOT NULL");
        _ = torrentBuilder.Property(static torrent => torrent.Cron).UseCollation(_noCaseCollation);
    }
}
