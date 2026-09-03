using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http;
using TransmissionManager.BaseTests.HttpClient;
using TransmissionManager.Database.Models;
using TransmissionManager.Database.Services;
using TransmissionManager.TorrentSources.JsonPointer;
using TransmissionManager.TorrentSources.WebPage;
using TransmissionManager.Transmission.Services;

namespace TransmissionManager.Api.IntegrationTests.Helpers;

internal sealed class TestWebApplicationFactory<TProgram>(
    Torrent[] initialTorrents,
    IReadOnlyDictionary<TestRequest, TestResponse>? sourceRequestResponseMap,
    IReadOnlyDictionary<TestRequest, TestResponse>? transmissionRequestResponseMap,
    Func<TorrentService, CancellationToken, Task>? transmissionAddMutation = null)
    : WebApplicationFactory<TProgram> where TProgram : class
{
    private static readonly Dictionary<TestRequest, TestResponse> _emptyRequestResponseMap = [];

    private readonly IReadOnlyDictionary<TestRequest, TestResponse> _sourceRequestResponseMap =
        sourceRequestResponseMap ?? _emptyRequestResponseMap;

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
            _ = services.ConfigureDbContext<AppDbContext>(options => options.UseSqlite(_connection));

            // Keyed by the typed client's NAME, and PostConfigure against an unregistered name is
            // silently ignored - a no-op here means real outbound requests, not a failing test.
            // AddHttpClient<TConcrete>() names the client "TConcrete", but
            // AddHttpClient<IFoo, Foo>() names it "IFoo"; a client registered behind an interface
            // must therefore pin its name via AddHttpClient<IFoo, Foo>("Foo") to stay hooked here.
            _ = services.PostConfigure(nameof(TorrentWebPageClient), (HttpClientFactoryOptions options) =>
            {
                options.HttpMessageHandlerBuilderActions.Add(builder =>
                    builder.PrimaryHandler = new FakeHttpMessageHandler(_sourceRequestResponseMap));
            });

            _ = services.PostConfigure(nameof(TorrentJsonPointerClient), (HttpClientFactoryOptions options) =>
            {
                options.HttpMessageHandlerBuilderActions.Add(builder =>
                    builder.PrimaryHandler = new FakeHttpMessageHandler(_sourceRequestResponseMap));
            });

            _ = services.PostConfigure(nameof(TransmissionClient), (HttpClientFactoryOptions options) =>
            {
                options.HttpMessageHandlerBuilderActions.Add(builder =>
                    builder.PrimaryHandler = transmissionAddMutation is null
                        ? new FakeHttpMessageHandler(_transmissionRequestResponseMap)
                        : new DatabaseMutatingHttpMessageHandler(
                            _transmissionRequestResponseMap,
                            builder.Services,
                            transmissionAddMutation));
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

    /// <remarks>
    /// Mutates the database through a separate service scope while a successful
    /// <c>torrent-add</c> response is pending, before the API handler attempts its local update.
    /// </remarks>
    private sealed class DatabaseMutatingHttpMessageHandler(
        IReadOnlyDictionary<TestRequest, TestResponse> requestToResponseMap,
        IServiceProvider serviceProvider,
        Func<TorrentService, CancellationToken, Task> mutation)
        : FakeHttpMessageHandler(requestToResponseMap)
    {
        private int _mutationApplied;

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
            try
            {
                var content = request.Content is null
                    ? null
                    : await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

                if (response.IsSuccessStatusCode
                    && content?.Contains("\"method\":\"torrent-add\"", StringComparison.Ordinal) is true
                    && Interlocked.Exchange(ref _mutationApplied, 1) == 0)
                {
                    using var scope = serviceProvider.CreateScope();
                    var torrentService = scope.ServiceProvider.GetRequiredService<TorrentService>();
                    await mutation(torrentService, cancellationToken).ConfigureAwait(false);
                }

                return response;
            }
            catch
            {
                response.Dispose();
                throw;
            }
        }
    }
}
