using Coravel;
using TransmissionManager.Api.Actions.AppInfo.Get;
using TransmissionManager.Api.Actions.Torrents.Add;
using TransmissionManager.Api.Actions.Torrents.DeleteById;
using TransmissionManager.Api.Actions.Torrents.FindById;
using TransmissionManager.Api.Actions.Torrents.FindPage;
using TransmissionManager.Api.Actions.Torrents.RefreshById;
using TransmissionManager.Api.Actions.Torrents.UpdateById;
using TransmissionManager.Api.Actions.TransmissionInfo.Get;
using TransmissionManager.Api.Common.Constants;
using TransmissionManager.Api.Common.Serialization;
using TransmissionManager.Api.Common.Services;
using TransmissionManager.Api.Common.TorrentWebPage;
using TransmissionManager.Api.Common.Transmission;
using TransmissionManager.Api.Scheduling;
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

    var lifetime = provider.GetRequiredService<IHostApplicationLifetime>();

    await provider.GetRequiredService<AppDbContext>()
        .Database.EnsureCreatedAsync(lifetime.ApplicationStopping)
        .ConfigureAwait(false);

    await provider.GetRequiredService<StartupTorrentSchedulerService>()
        .ScheduleUpdatesForAllTorrentsAsync(lifetime.ApplicationStopping)
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

app.MapGroup(EndpointAddresses.AppInfoApi)
    .MapGetAppInfoEndpoint();

app.MapGroup(EndpointAddresses.TransmissionInfoApi)
    .MapGetTransmissionInfoEndpoint();

await app.RunAsync().ConfigureAwait(false);

internal sealed partial class Program;

internal static partial class LoggerStartupExtensions
{
    public static void LogStartup(this ILogger logger)
    {
        LogStartingApplication(logger, typeof(Program).Assembly.FullName);
        LogStartTime(logger, DateTime.Now);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Starting application {AssemblyFullName}")]
    private static partial void LogStartingApplication(ILogger logger, string? assemblyFullName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Start time: {StartTime:o}")]
    private static partial void LogStartTime(ILogger logger, DateTime startTime);
}
