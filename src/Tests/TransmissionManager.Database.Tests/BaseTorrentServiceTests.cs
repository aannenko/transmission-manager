using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TransmissionManager.Database.Models;
using TransmissionManager.Database.Services;

namespace TransmissionManager.Database.Tests;

internal abstract class BaseTorrentServiceTests
{
    private SqliteConnection? _connection;
    private DbContextOptions<AppDbContext>? _contextOptions;

    private protected static Torrent[] InitialTorrents { get; } = CreateInitialTorrents();

    [OneTimeSetUp]
    public void Setup()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        _contextOptions = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;

        using var context = new AppDbContext(_contextOptions);

        context.Database.EnsureCreated();
        context.Torrents.AddRange(CreateInitialTorrents());
        context.SaveChanges();
    }

    [OneTimeTearDown]
    public void TearDown() => _connection?.Dispose();

    private protected AppDbContext CreateContext() =>
        _contextOptions is not null
            ? new(_contextOptions)
            : throw new InvalidOperationException("Context options are not initialized.");

    private static Torrent[] CreateInitialTorrents()
    {
        return [
            new()
            {
                Id = 1,
                HashString = "0bda511316a069e86dd8ee8a3610475d2013a7fa",
                RefreshDate = new(2024, 12, 3, 10, 20, 30, 400, DateTimeKind.Utc),
                Name = "TV show name",
                WebPageUri = new("https://torrentTracker.com/forum/viewtopic.php?t=1234567"),
                DownloadDir = "/tvshows",
                Cron = "0 9,17 * * *",
            },
            new()
            {
                Id = 2,
                HashString = "738c60cbd44f0e9457ba2afdad9e9231d76243fe",
                RefreshDate = new(2023, 11, 2, 11, 22, 33, 444, DateTimeKind.Utc),
                Name = "Movie name",
                WebPageUri = new("https://torrentTracker.com/forum/viewtopic.php?t=1234568"),
                DownloadDir = "/movies",
                MagnetRegexPattern = @"magnet:\?xt=urn:[^""]+",
            },
            new()
            {
                Id = 3,
                HashString = "5713cc1aeb2ec4a371c2412dc04e0a60d710862e",
                RefreshDate = new(2022, 10, 1, 12, 34, 56, 777, DateTimeKind.Utc),
                Name = "Music video name",
                WebPageUri = new("https://torrentTracker.com/forum/viewtopic.php?t=1234569"),
                DownloadDir = "/videos",
                Cron = "0 10,18 * * *",
                MagnetRegexPattern = @"magnet:\?xt[^""]+",
            }
        ];
    }
}
