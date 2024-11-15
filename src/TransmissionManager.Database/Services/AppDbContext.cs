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
#pragma warning disable IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code - tested after trimming
public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
#pragma warning restore IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code
{
    private const string _noCaseCollation = "NOCASE";

    public DbSet<Torrent> Torrents => Set<Torrent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        var torrentEntity = modelBuilder.Entity<Torrent>();

        torrentEntity.Property(torrent => torrent.Name)
            .UseCollation(_noCaseCollation);

        torrentEntity.Property(torrent => torrent.WebPageUri)
            .UseCollation(_noCaseCollation)
            .HasConversion(uri => uri.OriginalString, str => new Uri(str));

        torrentEntity.Property(torrent => torrent.HashString)
            .UseCollation(_noCaseCollation);
    }
}
