using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

namespace TransmissionManager.Web.Services;

#pragma warning disable CA1812 // Avoid uninstantiated internal classes - instantiated by the DI container.
internal sealed class TransmissionManagerInfoProvider(IWebAssemblyHostEnvironment hostEnvironment)
#pragma warning restore CA1812
{
    public Uri BaseAddress { get; set; } = new(hostEnvironment.BaseAddress);

    public TimeSpan Offset { get; set; } = TimeSpan.Zero;
}
