using Coravel;
using TransmissionManager.Api.Database.Extensions;
using TransmissionManager.Api.Database.Services;
using TransmissionManager.Api.Endpoints;
using TransmissionManager.Api.Endpoints.Serialization;
using TransmissionManager.Api.Endpoints.Services;
using TransmissionManager.TorrentTrackerClient.Extensions;
using TransmissionManager.TransmissionClient.Extensions;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.AddProblemDetails();

builder.Services.ConfigureHttpJsonOptions(
    static options => options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default));

builder.Services.AddAppDbContext();
builder.Services.AddTorrentWebPageService(builder.Configuration);
builder.Services.AddTransmissionService(builder.Configuration);

builder.Services.AddTransient<TorrentService>();
builder.Services.AddTransient<CompositeAddOrUpdateTorrentService>();
builder.Services.AddTransient<CompositeRefreshTorrentService>();

builder.Services.AddScheduler();
builder.Services.AddTransient<TorrentSchedulerService>();
builder.Services.AddTransient<StartupSchedulerService>();
builder.Services.AddTransient<SchedulableTorrentService>();
builder.Services.AddTransient<BackgroundTaskService>();

var app = builder.Build();

LogStartup(app);

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

static void LogStartup(WebApplication app)
{
    var logger = app.Logger;
    logger.LogInformation("Starting application {AssemblyFullName}", typeof(Program).Assembly.FullName);
    logger.LogInformation("Start time: {StartTime:o}", DateTime.Now);
}
