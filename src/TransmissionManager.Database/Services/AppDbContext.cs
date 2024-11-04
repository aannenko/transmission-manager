using Microsoft.EntityFrameworkCore;
using TransmissionManager.Database.Models;

namespace TransmissionManager.Database.Services;

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
