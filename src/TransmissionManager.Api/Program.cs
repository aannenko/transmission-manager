using Coravel;
using TransmissionManager.Api.Actions.AppVersion;
using TransmissionManager.Api.Actions.Torrents;
using TransmissionManager.Api.Common.Serialization;
using TransmissionManager.Api.Extensions;
using TransmissionManager.Api.Middleware;
using TransmissionManager.Api.Serialization;
using TransmissionManager.Api.Services.Background;
using TransmissionManager.Api.Services.Logging;
using TransmissionManager.Api.Services.Scheduling;
using TransmissionManager.Api.Services.TorrentWebPage;
using TransmissionManager.Api.Services.Transmission;
using TransmissionManager.Database.Services;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.ConfigureHttpJsonOptions(static options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, DtoJsonSerializerContext.Default);
    options.SerializerOptions.TypeInfoResolverChain.Insert(1, ApiJsonSerializerContext.Default);
});

builder.Services.AddSingleton(typeof(Log<>));
builder.Services.AddSingleton<CacheControlHeaderMiddleware>();
builder.Services.AddSingleton<XContentTypeOptionsHeaderMiddleware>();
builder.Services.AddSingleton<AllowPrivateNetworkHeaderMiddleware>();
builder.Services.AddCorsFromConfiguration(builder.Configuration);
builder.Services.AddProblemDetails();
builder.Services.AddValidation();

builder.Services.AddScheduler();

builder.Services.AddDatabaseServices();
builder.Services.AddTorrentWebPagesServices(builder.Configuration);
builder.Services.AddTransmissionServices(builder.Configuration);

builder.Services.AddSingleton<TorrentSchedulerService>();
builder.Services.AddTransient<StartupTorrentSchedulerService>();

builder.Services.AddTransient<TorrentWebPageClientWrapper>();
builder.Services.AddTransient<TransmissionClientWrapper>();
builder.Services.AddSingleton<BackgroundTorrentUpdateService>();

builder.Services.AddTorrentEndpointHandlers();

var app = builder.Build();

app.Services.GetRequiredService<Log<Program>>().LogStartup();

using (var scope = app.Services.CreateScope())
{
    var provider = scope.ServiceProvider;

    var lifetime = provider.GetRequiredService<IHostApplicationLifetime>();

    _ = await provider.GetRequiredService<AppDbContext>()
        .Database.EnsureCreatedAsync(lifetime.ApplicationStopping)
        .ConfigureAwait(false);

    await provider.GetRequiredService<StartupTorrentSchedulerService>()
        .ScheduleUpdatesForAllTorrentsAsync(lifetime.ApplicationStopping)
        .ConfigureAwait(false);
}

app.UseMiddleware<CacheControlHeaderMiddleware>();
app.UseMiddleware<XContentTypeOptionsHeaderMiddleware>();
app.UseMiddleware<AllowPrivateNetworkHeaderMiddleware>();
app.UseCors();

if (!app.Environment.IsDevelopment())
    _ = app.UseExceptionHandler();

app.UseStatusCodePages();

app.MapTorrentEndpoints();
app.MapAppVersionEndpoints();

await app.RunAsync().ConfigureAwait(false);
