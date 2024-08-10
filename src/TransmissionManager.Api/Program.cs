using Coravel;
using TransmissionManager.Api.Composite.Services;
using TransmissionManager.Api.Database.Extensions;
using TransmissionManager.Api.Database.Services;
using TransmissionManager.Api.Endpoints;
using TransmissionManager.Api.Scheduling.Services;
using TransmissionManager.Api.Serialization;
using TransmissionManager.Api.Trackers.Extensions;
using TransmissionManager.Api.Transmission.Extensions;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.AddProblemDetails();

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default));

builder.Services.AddAppDbContext();
builder.Services.AddMagnetUriRetriever(builder.Configuration);
builder.Services.AddTransmissionClient(builder.Configuration);

builder.Services.AddTransient<TorrentService>();
builder.Services.AddTransient(typeof(CompositeTorrentService<>));

builder.Services.AddScheduler();
builder.Services.AddTransient<TorrentSchedulerService>();
builder.Services.AddTransient<StartupSchedulerService>();
builder.Services.AddTransient<SchedulableTorrentService>();
builder.Services.AddTransient<BackgroundTaskService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();
    scope.ServiceProvider.GetRequiredService<StartupSchedulerService>().ScheduleUpdatesForAllTorrents();
}

if (!app.Environment.IsDevelopment())
    app.UseExceptionHandler();

app.UseStatusCodePages();

app.MapTorrentEndpoints();

app.Run();
