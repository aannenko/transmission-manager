using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http;
using TransmissionManager.BaseTests.HttpClient;
using TransmissionManager.Database.Models;
using TransmissionManager.Database.Services;
using TransmissionManager.TorrentWebPages.Services;
using TransmissionManager.Transmission.Services;

namespace TransmissionManager.Api.IntegrationTests.Helpers;

internal sealed class TestWebApplicationFactory<TProgram>(
    Torrent[] initialTorrents,
    IReadOnlyDictionary<TestRequest, TestResponse>? torrentPageRequestResponseMap,
    IReadOnlyDictionary<TestRequest, TestResponse>? transmissionRequestResponseMap)
    : WebApplicationFactory<TProgram> where TProgram : class
{
    private static readonly Dictionary<TestRequest, TestResponse> _emptyRequestResponseMap = [];

    private readonly IReadOnlyDictionary<TestRequest, TestResponse> _torrentPageRequestResponseMap =
        torrentPageRequestResponseMap ?? _emptyRequestResponseMap;

    private readonly IReadOnlyDictionary<TestRequest, TestResponse> _transmissionRequestResponseMap =
        transmissionRequestResponseMap ?? _emptyRequestResponseMap;

    private bool _disposed;

    private SqliteConnection? _connection;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(builder);

        base.ConfigureWebHost(builder);

        _ = builder.UseEnvironment("Testing");

        DisposeOfDbConnection();
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        _ = builder.ConfigureServices(services =>
        {
            _ = services
                .RemoveAll<DbContextOptions<AppDbContext>>()
                .AddDbContext<AppDbContext>(options => options.UseSqlite(_connection));

            _ = services.PostConfigure(nameof(TorrentWebPageClient), (HttpClientFactoryOptions options) =>
            {
                options.HttpMessageHandlerBuilderActions.Add(builder =>
                    builder.PrimaryHandler = new FakeHttpMessageHandler(_torrentPageRequestResponseMap));
            });

            _ = services.PostConfigure(nameof(TransmissionClient), (HttpClientFactoryOptions options) =>
            {
                options.HttpMessageHandlerBuilderActions.Add(builder =>
                    builder.PrimaryHandler = new FakeHttpMessageHandler(_transmissionRequestResponseMap));
            });
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        using var scope = host.Services.CreateScope();
        using var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        dbContext.Torrents.AddRange(initialTorrents);
        _ = dbContext.SaveChanges();

        return host;
    }

    public override async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;

        DisposeOfDbConnection();
        await base.DisposeAsync().ConfigureAwait(false);
    }

    private void DisposeOfDbConnection()
    {
        var connection = _connection;
        _connection = null;
        if (connection is not null)
        {
            connection.Close();
            connection.Dispose();
        }
    }
}
