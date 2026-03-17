using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using TransmissionManager.Api.Common.Serialization;
using TransmissionManager.Web;
using TransmissionManager.Web.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddSingleton(DtoJsonSerializerContext.Default);
builder.Services.AddSingleton<LocalStorageService>();
builder.Services.AddSingleton<ConnectionService>();
builder.Services.AddSingleton<ThemeService>();
builder.Services.AddHttpClient<TransmissionManagerClient>(static (services, client) =>
    client.BaseAddress = services.GetRequiredService<ConnectionService>().BaseAddress);

var host = builder.Build();

await host.Services.GetRequiredService<ConnectionService>().LoadAsync().ConfigureAwait(false);

await host.RunAsync().ConfigureAwait(false);
