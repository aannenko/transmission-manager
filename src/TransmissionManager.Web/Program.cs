using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using TransmissionManager.Api.Common.Serialization;
using TransmissionManager.Web;
using TransmissionManager.Web.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddSingleton(DtoJsonSerializerContext.Default);

builder.Services.AddSingleton<TransmissionManagerBaseAddressProvider>();
builder.Services.AddHttpClient<TransmissionManagerClient>(static (services, client) =>
{
    client.BaseAddress = services.GetRequiredService<TransmissionManagerBaseAddressProvider>().BaseAddress;
});

await builder.Build().RunAsync().ConfigureAwait(false);
