using Coravel;
using TransmissionManager.Api.AddOrUpdateTorrent.Endpoints;
using TransmissionManager.Api.AddOrUpdateTorrent.Handlers;
using TransmissionManager.Api.Common.Serialization;
using TransmissionManager.Api.Common.Services;
using TransmissionManager.Api.DeleteTorrentById.Endpoints;
using TransmissionManager.Api.FindTorrentById.Endpoints;
using TransmissionManager.Api.FindTorrentPage.Endpoints;
using TransmissionManager.Api.RefreshTorrentById.Endpoints;
using TransmissionManager.Api.RefreshTorrentById.Handlers;
using TransmissionManager.Api.UpdateTorrentById.Endpoints;
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
builder.Services.AddTransient<StartupSchedulerService>();
builder.Services.AddTransient<SchedulableTorrentCommandService>();

builder.Services.AddTransient<BackgroundTaskService>();
builder.Services.AddTransient<TorrentWebPageService>();
builder.Services.AddTransient<TransmissionService>();
builder.Services.AddSingleton<TorrentNameUpdateService>();
builder.Services.AddTransient<AddOrUpdateTorrentHandler>();
builder.Services.AddTransient<RefreshTorrentByIdHandler>();

var app = builder.Build();

LogStartup(app.Logger);

using (var scope = app.Services.CreateScope())
{
    await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreatedAsync();
    await scope.ServiceProvider.GetRequiredService<StartupSchedulerService>().ScheduleUpdatesForAllTorrentsAsync();
}

if (!app.Environment.IsDevelopment())
    app.UseExceptionHandler();

app.UseStatusCodePages();

app.MapFindTorrentByIdEndpoint();
app.MapFindTorrentPageEndpoint();
app.MapAddOrUpdateTorrentEndpoint();
app.MapRefreshTorrentByIdEndpoint();
app.MapUpdateTorrentByIdEndpoint();
app.MapDeleteTorrentByIdEndpoint();

app.Run();

static void LogStartup(ILogger logger)
{
    logger.LogInformation("Starting application {AssemblyFullName}", typeof(Program).Assembly.FullName);
    logger.LogInformation("Start time: {StartTime:o}", DateTime.Now);
}
