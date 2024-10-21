using Coravel;
using TransmissionManager.Api.AddTorrent;
using TransmissionManager.Api.Common.Serialization;
using TransmissionManager.Api.Common.Services;
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

LogStartup(app.Logger);

using (var scope = app.Services.CreateScope())
{
    var provider = scope.ServiceProvider;
    await provider.GetRequiredService<AppDbContext>().Database.EnsureCreatedAsync();
    await provider.GetRequiredService<StartupTorrentSchedulerService>().ScheduleUpdatesForAllTorrentsAsync();
}

if (!app.Environment.IsDevelopment())
    app.UseExceptionHandler();

app.UseStatusCodePages();

app.MapFindTorrentByIdEndpoint();
app.MapFindTorrentPageEndpoint();
app.MapAddTorrentEndpoint();
app.MapRefreshTorrentByIdEndpoint();
app.MapUpdateTorrentByIdEndpoint();
app.MapDeleteTorrentByIdEndpoint();

app.Run();

static void LogStartup(ILogger logger)
{
    logger.LogInformation("Starting application {AssemblyFullName}", typeof(Program).Assembly.FullName);
    logger.LogInformation("Start time: {StartTime:o}", DateTime.Now);
}

public sealed partial class Program;
