using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TransmissionManager.Api.Services.Development;
using TransmissionManager.Database.Services;

namespace TransmissionManager.Api.IntegrationTests.Development;

[Parallelizable(ParallelScope.Self)]
internal sealed class DevDatabaseSeederTests
{
    [Test]
    public void TorrentCount_Is300()
    {
        Assert.That(DevDatabaseSeeder.TorrentCount, Is.EqualTo(300));
    }

    [Test]
    public async Task SeedAsync_InsertsExpectedNumberOfTorrents()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync().ConfigureAwait(false);

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        using var dbContext = new AppDbContext(options);
        var isCreated = await dbContext.Database.EnsureCreatedAsync().ConfigureAwait(false);

        Assert.That(isCreated, Is.True, "Database should be created successfully.");

        await DevDatabaseSeeder.SeedAsync(dbContext).ConfigureAwait(false);

        var torrents = await dbContext.Torrents.ToArrayAsync().ConfigureAwait(false);

        using var scope = Assert.EnterMultipleScope();

        Assert.That(torrents, Has.Length.EqualTo(DevDatabaseSeeder.TorrentCount));
        
        Assert.That(
            torrents.Select(static t => t.HashString).Distinct().Count(),
            Is.EqualTo(DevDatabaseSeeder.TorrentCount));
        
        Assert.That(
            torrents.Select(static t => t.Name).Distinct().Count(),
            Is.EqualTo(DevDatabaseSeeder.TorrentCount));
        
        Assert.That(
            torrents.Select(static t => t.WebPageUri).Distinct().Count(),
            Is.EqualTo(DevDatabaseSeeder.TorrentCount));
    }
}
