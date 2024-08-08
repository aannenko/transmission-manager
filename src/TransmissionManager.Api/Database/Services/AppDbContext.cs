using Microsoft.EntityFrameworkCore;
using TransmissionManager.Api.Database.Models;

namespace TransmissionManager.Api.Database.Services;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Torrent> Torrents { get; set; }
}
