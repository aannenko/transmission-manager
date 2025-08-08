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
builder.Services.AddSingleton(static services =>
{
    var environment = services.GetRequiredService<IWebAssemblyHostEnvironment>();
    return DateTimeExtensions.InfoProvider = new TransmissionManagerInfoProvider(environment);
});

builder.Services.AddHttpClient<TransmissionManagerClient>(static (services, client) =>
{
    client.BaseAddress = services.GetRequiredService<TransmissionManagerInfoProvider>().BaseAddress;
});

await builder.Build().RunAsync().ConfigureAwait(false);
