using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

namespace TransmissionManager.Web.Tests.Helpers;

internal sealed class FakeWebAssemblyHostEnvironment(string baseAddress) : IWebAssemblyHostEnvironment
{
    public string BaseAddress { get; } = baseAddress;

    public string Environment { get; } = "Development";
}
