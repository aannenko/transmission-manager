using Microsoft.EntityFrameworkCore;
using System.Diagnostics.CodeAnalysis;
using TransmissionManager.Database.Models;

namespace TransmissionManager.Database.Services;

[SuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "The app is thoroughly tested after trimming, that includes the used EF Core functionality")]
public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Torrent> Torrents { get; set; }
}
