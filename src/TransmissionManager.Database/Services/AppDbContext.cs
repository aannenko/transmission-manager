using Microsoft.EntityFrameworkCore;
using System.Diagnostics.CodeAnalysis;
using TransmissionManager.Database.Models;

namespace TransmissionManager.Database.Services;

[SuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "Tested after trimming")]
public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Torrent> Torrents => Set<Torrent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var torrentEntity = modelBuilder.Entity<Torrent>();
        torrentEntity.Property(t => t.Name).UseCollation("NOCASE");
        torrentEntity.Property(t => t.WebPageUri).UseCollation("NOCASE");
        torrentEntity.Property(t => t.HashString).UseCollation("NOCASE");
    }
}
