using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using System.Net;
using TransmissionManager.Api.Services.Background;
using TransmissionManager.Api.Services.Logging;
using TransmissionManager.BaseTests.Options;
using TransmissionManager.Database.Dto;
using TransmissionManager.Database.Models;
using TransmissionManager.Database.Services;
using TransmissionManager.Transmission.Options;
using TransmissionManager.Transmission.Services;

namespace TransmissionManager.Api.Tests.Services.Background;

[Parallelizable(ParallelScope.Self)]
internal sealed class BackgroundTorrentUpdateServiceTests
{
    private const string _hash = "abcdef0123456789abcdef0123456789abcdef01";
    private const string _resolvedName = "TV Show 1";

    private static readonly TimeSpan _testTimeout = TimeSpan.FromSeconds(5);

    private SqliteConnection _connection = null!;
    private ServiceProvider _services = null!;
    private FakeTimeProvider _time = null!;
    private RecordingLogger<BackgroundTorrentUpdateService> _logger = null!;
    private SequencedHttpMessageHandler _transmissionHandler = null!;

    [SetUp]
    public async Task Setup()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        await _connection.OpenAsync().ConfigureAwait(false);

        _time = new FakeTimeProvider();
        _logger = new RecordingLogger<BackgroundTorrentUpdateService>();
        _transmissionHandler = new SequencedHttpMessageHandler();

        var transmissionOptions = new FakeOptionsMonitor<TransmissionClientOptions>(new()
        {
            BaseAddress = "http://transmission:9091",
            RpcEndpointAddressSuffix = "/transmission/rpc",
        });

        var serviceCollection = new ServiceCollection();
        _ = serviceCollection.AddDbContext<AppDbContext>(options => options.UseSqlite(_connection));
        _ = serviceCollection.AddSingleton<TorrentCountCache>();
        _ = serviceCollection.AddScoped<TorrentService>();
        _ = serviceCollection.AddSingleton<TransmissionClient>(_ =>
            new TransmissionClient(transmissionOptions, new HttpClient(_transmissionHandler)
            {
                BaseAddress = new(transmissionOptions.CurrentValue.BaseAddress),
            }));

        _services = serviceCollection.BuildServiceProvider();

        using var scope = _services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        _ = await dbContext.Database.EnsureCreatedAsync().ConfigureAwait(false);
    }

    [TearDown]
    public async Task TearDown()
    {
        await _services.DisposeAsync().ConfigureAwait(false);
        _transmissionHandler.Dispose();
        await _connection.DisposeAsync().ConfigureAwait(false);
    }

    [Test]
    public async Task UpdateTorrentNameAsync_WhenRealNameAvailableOnFirstAttempt_UpdatesTorrentName()
    {
        var seeded = await SeedTorrentAsync(name: _hash).ConfigureAwait(false);
        _transmissionHandler.Enqueue(_ => Json(BuildGetResponse(_resolvedName)));

        var service = CreateService();
        await DrainAsync(service.UpdateTorrentNameAsync(seeded.Id, _hash, _hash, seeded.Version))
            .ConfigureAwait(false);

        var actual = await ReadTorrentAsync(seeded.Id).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(actual!.Name, Is.EqualTo(_resolvedName));
            Assert.That(actual.Version, Is.EqualTo(seeded.Version + 1));
            Assert.That(_logger.Records, Is.Empty);
        }
    }

    [Test]
    public async Task UpdateTorrentNameAsync_WhenTransmissionReturnsHashString_KeepsPollingThenUpdatesWhenNameAvailable()
    {
        var seeded = await SeedTorrentAsync(name: _hash).ConfigureAwait(false);
        _transmissionHandler.Enqueue(_ => Json(BuildGetResponse(_hash)));
        _transmissionHandler.Enqueue(_ => Json(BuildGetResponse(_resolvedName)));

        var service = CreateService();
        await DrainAsync(service.UpdateTorrentNameAsync(seeded.Id, _hash, _hash, seeded.Version))
            .ConfigureAwait(false);

        var actual = await ReadTorrentAsync(seeded.Id).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(_transmissionHandler.CallCount, Is.EqualTo(2));
            Assert.That(actual!.Name, Is.EqualTo(_resolvedName));
            Assert.That(actual.Version, Is.EqualTo(seeded.Version + 1));
        }
    }

    [Test]
    public async Task UpdateTorrentNameAsync_WhenTransmissionReturnsCurrentName_DoesNotUpdate()
    {
        var seeded = await SeedTorrentAsync(name: "Existing Name").ConfigureAwait(false);
        _transmissionHandler.Enqueue(_ => Json(BuildGetResponse("Existing Name")));

        var service = CreateService();
        await DrainAsync(service.UpdateTorrentNameAsync(seeded.Id, _hash, "Existing Name", seeded.Version))
            .ConfigureAwait(false);

        var actual = await ReadTorrentAsync(seeded.Id).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(actual!.Name, Is.EqualTo("Existing Name"));
            Assert.That(actual.Version, Is.EqualTo(seeded.Version));
            Assert.That(_logger.Records, Is.Empty);
        }
    }

    [Test]
    public async Task UpdateTorrentNameAsync_WhenTransmissionReturnsEmptyName_DoesNotUpdate()
    {
        var seeded = await SeedTorrentAsync(name: _hash).ConfigureAwait(false);
        _transmissionHandler.Enqueue(_ => Json(BuildGetResponse("")));

        var service = CreateService();
        await DrainAsync(service.UpdateTorrentNameAsync(seeded.Id, _hash, _hash, seeded.Version))
            .ConfigureAwait(false);

        var actual = await ReadTorrentAsync(seeded.Id).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(actual!.Name, Is.EqualTo(_hash));
            Assert.That(actual.Version, Is.EqualTo(seeded.Version));
            Assert.That(_logger.Records, Is.Empty);
        }
    }

    [Test]
    public async Task UpdateTorrentNameAsync_WhenHttpRequestExceptionThrown_ContinuesPolling()
    {
        var seeded = await SeedTorrentAsync(name: _hash).ConfigureAwait(false);
        _transmissionHandler.Enqueue(_ => throw new HttpRequestException("transient"));
        _transmissionHandler.Enqueue(_ => Json(BuildGetResponse(_resolvedName)));

        var service = CreateService();
        await DrainAsync(service.UpdateTorrentNameAsync(seeded.Id, _hash, _hash, seeded.Version))
            .ConfigureAwait(false);

        var actual = await ReadTorrentAsync(seeded.Id).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(_transmissionHandler.CallCount, Is.EqualTo(2));
            Assert.That(actual!.Name, Is.EqualTo(_resolvedName));
            Assert.That(actual.Version, Is.EqualTo(seeded.Version + 1));
        }
    }

    [Test]
    public async Task UpdateTorrentNameAsync_WhenVersionConflictAndIdentityMatches_RetriesUntilSuccess()
    {
        var seeded = await SeedTorrentAsync(name: _hash).ConfigureAwait(false);
        var stalePriorVersion = seeded.Version - 1;
        _transmissionHandler.Enqueue(_ => Json(BuildGetResponse(_resolvedName)));

        var service = CreateService();
        await DrainAsync(service.UpdateTorrentNameAsync(seeded.Id, _hash, _hash, stalePriorVersion))
            .ConfigureAwait(false);

        var actual = await ReadTorrentAsync(seeded.Id).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(actual!.Name, Is.EqualTo(_resolvedName));
            Assert.That(actual.Version, Is.EqualTo(seeded.Version + 1));
            Assert.That(_logger.Records, Is.Empty);
        }
    }

    [Test]
    public async Task UpdateTorrentNameAsync_WhenVersionConflictAndNameChanged_DoesNotUpdateAndDoesNotLog()
    {
        var seeded = await SeedTorrentAsync(name: "User Renamed").ConfigureAwait(false);
        var stalePriorVersion = seeded.Version - 1;
        _transmissionHandler.Enqueue(_ => Json(BuildGetResponse(_resolvedName)));

        var service = CreateService();
        await DrainAsync(service.UpdateTorrentNameAsync(seeded.Id, _hash, _hash, stalePriorVersion))
            .ConfigureAwait(false);

        var actual = await ReadTorrentAsync(seeded.Id).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(actual!.Name, Is.EqualTo("User Renamed"));
            Assert.That(actual.Version, Is.EqualTo(seeded.Version));
            Assert.That(_logger.Records, Is.Empty);
        }
    }

    [Test]
    public async Task UpdateTorrentNameAsync_WhenTorrentDeleted_ReturnsSilently()
    {
        var seeded = await SeedTorrentAsync(name: _hash).ConfigureAwait(false);
        await DeleteTorrentAsync(seeded.Id).ConfigureAwait(false);
        _transmissionHandler.Enqueue(_ => Json(BuildGetResponse(_resolvedName)));

        var service = CreateService();
        await DrainAsync(service.UpdateTorrentNameAsync(seeded.Id, _hash, _hash, seeded.Version))
            .ConfigureAwait(false);

        Assert.That(_logger.Records, Is.Empty);
    }

    [Test]
    public async Task UpdateTorrentNameAsync_WhenUnexpectedExceptionThrown_LogsBackgroundNameUpdateFailed()
    {
        var seeded = await SeedTorrentAsync(name: _hash).ConfigureAwait(false);
        // Return a malformed payload so JSON deserialization throws (unexpected, non-HttpRequest).
        _transmissionHandler.Enqueue(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("not-json"),
        });

        var service = CreateService();
        await DrainAsync(service.UpdateTorrentNameAsync(seeded.Id, _hash, _hash, seeded.Version))
            .ConfigureAwait(false);

        var actual = await ReadTorrentAsync(seeded.Id).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(actual!.Version, Is.EqualTo(seeded.Version));
            Assert.That(
                _logger.Records,
                Has.Some.Matches<RecordedLog>(static r => r.Level == LogLevel.Warning && r.Exception is not null));
        }
    }

    private BackgroundTorrentUpdateService CreateService()
    {
        var scopeFactory = _services.GetRequiredService<IServiceScopeFactory>();
        return new BackgroundTorrentUpdateService(scopeFactory, new Log<BackgroundTorrentUpdateService>(_logger), _time);
    }

    private async Task<Torrent> SeedTorrentAsync(string name)
    {
        using var scope = _services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var torrent = new Torrent
        {
            Id = default,
            HashString = _hash,
            Name = name,
            SourceUri = "https://torrents.example.com/forum/viewtopic.php?t=1",
            SourceKind = TorrentSourceKind.WebPage,
            DownloadDir = "/tvshows",
            RefreshDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Version = 1,
        };

        _ = dbContext.Torrents.Add(torrent);
        _ = await dbContext.SaveChangesAsync().ConfigureAwait(false);

        // Bump version so the test exercises a non-trivial expected version.
        _ = await dbContext.Torrents.Where(t => t.Id == torrent.Id)
            .ExecuteUpdateAsync(setters => setters.SetProperty(t => t.Version, t => t.Version + 1))
            .ConfigureAwait(false);

        return await dbContext.Torrents.AsNoTracking().FirstAsync(t => t.Id == torrent.Id).ConfigureAwait(false);
    }

    private async Task<Torrent?> ReadTorrentAsync(long id)
    {
        using var scope = _services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await dbContext.Torrents.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id).ConfigureAwait(false);
    }

    private async Task DeleteTorrentAsync(long id)
    {
        using var scope = _services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        _ = await dbContext.Torrents.Where(t => t.Id == id).ExecuteDeleteAsync().ConfigureAwait(false);
    }

    private async Task DrainAsync(Task task)
    {
        var realDeadline = DateTime.UtcNow + _testTimeout;
        while (!task.IsCompleted)
        {
            // Let real async work (HTTP/EF) progress, then advance virtual time.
            var completedSignal = await Task.WhenAny(task, Task.Delay(50)).ConfigureAwait(false);
            if (completedSignal == task)
                break;

            _time.Advance(TimeSpan.FromMinutes(30));

            if (DateTime.UtcNow > realDeadline)
                throw new TimeoutException("Background update did not complete within the test timeout.");
        }

        await task.ConfigureAwait(false);
    }

    private static string BuildGetResponse(string name) =>
        $$"""{"arguments":{"torrents":[{"name":"{{name}}"}]},"result":"success"}""";

    private static HttpResponseMessage Json(string body) =>
        new(HttpStatusCode.OK) { Content = new StringContent(body) };

    private sealed record RecordedLog(LogLevel Level, EventId EventId, string Message, Exception? Exception);

    private sealed class RecordingLogger<T> : ILogger<T>
    {
        public List<RecordedLog> Records { get; } = [];

        IDisposable? ILogger.BeginScope<TState>(TState state) => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            ArgumentNullException.ThrowIfNull(formatter);
            Records.Add(new(logLevel, eventId, formatter(state, exception), exception));
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();
            public void Dispose() { }
        }
    }

    private sealed class SequencedHttpMessageHandler : HttpMessageHandler
    {
        private readonly Queue<Func<HttpRequestMessage, Task<HttpResponseMessage>>> _responses = new();
        private int _callCount;

        public int CallCount => _callCount;

        public void Enqueue(Func<HttpRequestMessage, HttpResponseMessage> response) =>
            _responses.Enqueue(request => Task.FromResult(response(request)));

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            _ = Interlocked.Increment(ref _callCount);

            if (_responses.Count == 0)
                throw new InvalidOperationException("No more queued responses.");

            var factory = _responses.Dequeue();
            return await factory(request).ConfigureAwait(false);
        }
    }
}
