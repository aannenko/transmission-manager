using Coravel;
using TransmissionManager.Api.AddTorrent;
using TransmissionManager.Api.Common.Constants;
using TransmissionManager.Api.Common.Scheduling;
using TransmissionManager.Api.Common.Serialization;
using TransmissionManager.Api.Common.Services;
using TransmissionManager.Api.Common.TorrentWebPage;
using TransmissionManager.Api.Common.Transmission;
using TransmissionManager.Api.DeleteTorrentById;
using TransmissionManager.Api.FindTorrentById;
using TransmissionManager.Api.FindTorrentPage;
using TransmissionManager.Api.RefreshTorrentById;
using TransmissionManager.Api.UpdateTorrentById;
using TransmissionManager.Database.Extensions;
using TransmissionManager.Database.Services;
using TransmissionManager.TorrentWebPages.Extensions;
using TransmissionManager.Transmission.Extensions;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.ConfigureHttpJsonOptions(
    static options => options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default));

builder.Services.AddProblemDetails();

builder.Services.AddDatabaseServices();
builder.Services.AddTorrentWebPagesServices(builder.Configuration);
builder.Services.AddTransmissionServices(builder.Configuration);

builder.Services.AddScheduler();
builder.Services.AddTransient<TorrentSchedulerService>();
builder.Services.AddTransient<StartupTorrentSchedulerService>();

builder.Services.AddTransient<BackgroundTaskService>();
builder.Services.AddTransient<TorrentWebPageClientWrapper>();
builder.Services.AddTransient<TransmissionClientWrapper>();
builder.Services.AddSingleton<TorrentNameUpdateService>();

builder.Services.AddTransient<AddTorrentHandler>();
builder.Services.AddTransient<RefreshTorrentByIdHandler>();
builder.Services.AddTransient<UpdateTorrentByIdHandler>();
builder.Services.AddTransient<DeleteTorrentByIdHandler>();

var app = builder.Build();

app.Logger.LogStartup();

using (var scope = app.Services.CreateScope())
{
    var provider = scope.ServiceProvider;

    await provider.GetRequiredService<AppDbContext>()
        .Database.EnsureCreatedAsync()
        .ConfigureAwait(false);

    await provider.GetRequiredService<StartupTorrentSchedulerService>()
        .ScheduleUpdatesForAllTorrentsAsync()
        .ConfigureAwait(false);
}

if (!app.Environment.IsDevelopment())
    app.UseExceptionHandler();

app.UseStatusCodePages();

app.MapGroup(EndpointAddresses.TorrentsApi)
    .MapFindTorrentByIdEndpoint()
    .MapFindTorrentPageEndpoint()
    .MapAddTorrentEndpoint()
    .MapRefreshTorrentByIdEndpoint()
    .MapUpdateTorrentByIdEndpoint()
    .MapDeleteTorrentByIdEndpoint();

await app.RunAsync().ConfigureAwait(false);

public sealed partial class Program;

static partial class LoggerStartupExtensions
{
    public static void LogStartup(this ILogger logger)
    {
        LogStartingApplication(logger, typeof(Program).Assembly.FullName);
        LogStartTime(logger, DateTime.Now.ToString());
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Starting application {AssemblyFullName}")]
    private static partial void LogStartingApplication(ILogger logger, string? assemblyFullName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Start time: {StartTime:o}")]
    private static partial void LogStartTime(ILogger logger, string startTime);
}
