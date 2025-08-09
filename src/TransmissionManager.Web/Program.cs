using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using TransmissionManager.Api.Common.Serialization;
using TransmissionManager.Web;
using TransmissionManager.Web.Extensions;
using TransmissionManager.Web.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddSingleton(DtoJsonSerializerContext.Default);
builder.Services.AddSingleton(static _ => DateTimeExtensions.ServerTimeZoneService = new());
builder.Services.AddSingleton<ConnectionService>();
builder.Services.AddHttpClient<TransmissionManagerClient>(static (services, client) =>
    client.BaseAddress = services.GetRequiredService<ConnectionService>().BaseAddress);

var app = builder.Build();

await app.Services.GetRequiredService<ConnectionService>().InitializeAsync().ConfigureAwait(false);

await app.RunAsync().ConfigureAwait(false);
