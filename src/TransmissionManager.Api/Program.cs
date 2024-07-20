using Coravel;
using Microsoft.EntityFrameworkCore;
using TransmissionManager.Api.Composite.Services;
using TransmissionManager.Api.Database.Services;
using TransmissionManager.Api.Endpoints;
using TransmissionManager.Api.Scheduling.Services;
using TransmissionManager.Api.Serialization;
using TransmissionManager.Api.Trackers.Extensions;
using TransmissionManager.Api.Transmission.Extensions;

const string ConnectionStringConfigKey = "AppDb";

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default));

builder.Services.AddDbContext<AppDbContext>(static (services, options) =>
    options.UseSqlite(services.GetRequiredService<IConfiguration>().GetConnectionString(ConnectionStringConfigKey)));

builder.Services.AddMagnetUriRetriever(builder.Configuration);
builder.Services.AddTransmissionClient(builder.Configuration);
builder.Services.AddTransient<TorrentService>();
builder.Services.AddTransient(typeof(CompositeService<>));

builder.Services.AddScheduler();
builder.Services.AddTransient<TorrentSchedulerService>();
builder.Services.AddTransient<SchedulableTorrentService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();
    scope.ServiceProvider.GetRequiredService<TorrentSchedulerService>().ScheduleUpdatesForAllTorrents();
}

app.MapTorrentEndpoints();

app.Run();
