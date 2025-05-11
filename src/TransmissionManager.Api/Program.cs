using Coravel;
using TransmissionManager.Api.Actions.AppInfo;
using TransmissionManager.Api.Actions.Torrents;
using TransmissionManager.Api.Common.Constants;
using TransmissionManager.Api.Extensions;
using TransmissionManager.Api.Serialization;
using TransmissionManager.Api.Services.Background;
using TransmissionManager.Api.Services.Scheduling;
using TransmissionManager.Api.Services.TorrentWebPage;
using TransmissionManager.Api.Services.Transmission;
using TransmissionManager.Database.Extensions;
using TransmissionManager.Database.Services;
using TransmissionManager.TorrentWebPages.Extensions;
using TransmissionManager.Transmission.Extensions;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.ConfigureHttpJsonOptions(
    static options => options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default));

builder.Services.AddCorsFromConfiguration(builder.Configuration);
builder.Services.AddProblemDetails();

builder.Services.AddDatabaseServices();
builder.Services.AddTorrentWebPagesServices(builder.Configuration);
builder.Services.AddTransmissionServices(builder.Configuration);

builder.Services.AddScheduler();
builder.Services.AddTransient<TorrentSchedulerService>();
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

app.UseCors();

if (!app.Environment.IsDevelopment())
    app.UseExceptionHandler();

app.UseStatusCodePages();

app.MapGroup(EndpointAddresses.Torrents)
    .MapFindTorrentByIdEndpoint()
    .MapFindTorrentPageEndpoint()
    .MapAddTorrentEndpoint()
    .MapRefreshTorrentByIdEndpoint()
    .MapUpdateTorrentByIdEndpoint()
    .MapDeleteTorrentByIdEndpoint();

app.MapGroup(EndpointAddresses.AppInfo)
    .MapGetAppInfoEndpoint();

await app.RunAsync().ConfigureAwait(false);

internal sealed partial class Program;
