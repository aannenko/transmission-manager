using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TransmissionManager.Database.Models;
using TransmissionManager.Database.Services;

namespace TransmissionManager.Database.Tests;

public abstract class BaseTorrentServiceTests
{
    private protected static readonly Torrent[] _initialTorrents =
    [
        new Torrent
        {
            Id = default,
            HashString = "0bda511316a069e86dd8ee8a3610475d2013a7fa",
            Name = "TV show name",
            WebPageUri = "https://torrentTracker.com/forum/viewtopic.php?t=1234567",
            DownloadDir = "/tvshows",
            Cron = "0 9,17 * * *",
        },
        new Torrent
        {
            Id = default,
            HashString = "738c60cbd44f0e9457ba2afdad9e9231d76243fe",
            Name = "Movie name",
            WebPageUri = "https://torrentTracker.com/forum/viewtopic.php?t=1234568",
            DownloadDir = "/movies",
            MagnetRegexPattern = @"magnet:\?xt=urn:[^""]*",
        },
        new Torrent
        {
            Id = default,
            HashString = "5713cc1aeb2ec4a371c2412dc04e0a60d710862e",
            Name = "Music video name",
            WebPageUri = "https://torrentTracker.com/forum/viewtopic.php?t=1234569",
            DownloadDir = "/videos",
            Cron = "0 10,18 * * *",
            MagnetRegexPattern = @"magnet:\?xt[^""]*",
        }
    ];

    private SqliteConnection _connection;
    private DbContextOptions<AppDbContext> _contextOptions;

    [OneTimeSetUp]
    public void Setup()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();

        _contextOptions = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;

        using var context = new AppDbContext(_contextOptions);

        context.Database.EnsureCreated();
        context.Torrents.AddRange(_initialTorrents);
        context.SaveChanges();
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        _connection.Dispose();
    }

    private protected AppDbContext CreateContext() => new(_contextOptions);
}
