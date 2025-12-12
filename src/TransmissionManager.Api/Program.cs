using Coravel;
using TransmissionManager.Api.Actions.AppVersion;
using TransmissionManager.Api.Actions.Torrents;
using TransmissionManager.Api.Common.Constants;
using TransmissionManager.Api.Common.Serialization;
using TransmissionManager.Api.Middleware;
using TransmissionManager.Api.Serialization;
using TransmissionManager.Api.Services.Background;
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

builder.Services.AddSingleton<CacheControlHeaderMiddleware>();
builder.Services.AddSingleton<XContentTypeOptionsHeaderMiddleware>();
builder.Services.AddSingleton<AllowPrivateNetworkHeaderMiddleware>();
builder.Services.AddCorsFromConfiguration(builder.Configuration);
builder.Services.AddProblemDetails();
builder.Services.AddValidation();

builder.Services.AddDatabaseServices();
builder.Services.AddTorrentWebPagesServices(builder.Configuration);
builder.Services.AddTransmissionServices(builder.Configuration);

builder.Services.AddScheduler();
builder.Services.AddSingleton<TorrentSchedulerService>();
builder.Services.AddTransient<StartupTorrentSchedulerService>();

builder.Services.AddTransient<TorrentWebPageClientWrapper>();
builder.Services.AddTransient<TransmissionClientWrapper>();
builder.Services.AddSingleton<BackgroundTorrentUpdateService>();

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

app.UseMiddleware<CacheControlHeaderMiddleware>();
app.UseMiddleware<XContentTypeOptionsHeaderMiddleware>();
app.UseMiddleware<AllowPrivateNetworkHeaderMiddleware>();
app.UseCors();

if (!app.Environment.IsDevelopment())
    app.UseExceptionHandler();

app.UseStatusCodePages();

app.MapGroup(EndpointAddresses.Torrents)
    .MapGetTorrentByIdEndpoint()
    .MapGetTorrentPageEndpoint()
    .MapAddTorrentEndpoint()
    .MapRefreshTorrentByIdEndpoint()
    .MapUpdateTorrentByIdEndpoint()
    .MapDeleteTorrentByIdEndpoint();

app.MapGroup(EndpointAddresses.AppVersion)
    .MapGetAppVersionEndpoint();

await app.RunAsync().ConfigureAwait(false);

internal sealed partial class Program;
