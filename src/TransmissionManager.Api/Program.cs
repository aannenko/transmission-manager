using Coravel;
using TransmissionManager.Api;
using TransmissionManager.Api.Serialization;
using TransmissionManager.Api.Services;
using TransmissionManager.Database.Extensions;
using TransmissionManager.Database.Services;
using TransmissionManager.TorrentTrackerClient.Extensions;
using TransmissionManager.TransmissionClient.Extensions;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.AddProblemDetails();

builder.Services.ConfigureHttpJsonOptions(
    static options => options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default));

builder.Services.AddDatabaseServices();
builder.Services.AddTorrentTrackerClientServices(builder.Configuration);
builder.Services.AddTransmissionClientServices(builder.Configuration);

builder.Services.AddScheduler();
builder.Services.AddTransient<BackgroundTaskService>();
builder.Services.AddTransient<TorrentSchedulerService>();
builder.Services.AddTransient<StartupSchedulerService>();
builder.Services.AddTransient<SchedulableTorrentCommandService>();
builder.Services.AddTransient<CompositeAddOrUpdateTorrentService>();
builder.Services.AddTransient<CompositeRefreshTorrentService>();

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

app.MapTorrentEndpoints();

app.Run();

static void LogStartup(ILogger logger)
{
    logger.LogInformation("Starting application {AssemblyFullName}", typeof(Program).Assembly.FullName);
    logger.LogInformation("Start time: {StartTime:o}", DateTime.Now);
}
