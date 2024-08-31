using Coravel;
using TransmissionManager.Api.Database.Extensions;
using TransmissionManager.Api.Database.Services;
using TransmissionManager.Api.Endpoints;
using TransmissionManager.Api.Endpoints.Services;
using TransmissionManager.Api.Serialization;
using TransmissionManager.TorrentTrackers.Extensions;
using TransmissionManager.TransmissionClient.Extensions;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.AddProblemDetails();

builder.Services.ConfigureHttpJsonOptions(
    static options => options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default));

builder.Services.AddAppDbContext();
builder.Services.AddMagnetUriRetriever(builder.Configuration);
builder.Services.AddTransmissionClient(builder.Configuration);

builder.Services.AddTransient<TorrentService>();
builder.Services.AddTransient<CompositeAddOrUpdateTorrentService>();
builder.Services.AddTransient<CompositeRefreshTorrentService>();

builder.Services.AddScheduler();
builder.Services.AddTransient<TorrentSchedulerService>();
builder.Services.AddTransient<StartupSchedulerService>();
builder.Services.AddTransient<SchedulableTorrentService>();
builder.Services.AddTransient<BackgroundTaskService>();

var app = builder.Build();

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
