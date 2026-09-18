using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

namespace TransmissionManager.Web.Tests.Helpers;

/// <summary>
/// Stands in for the address and environment name the browser served the application from.
/// </summary>
internal sealed class FakeWebAssemblyHostEnvironment(string baseAddress) : IWebAssemblyHostEnvironment
{
    public string BaseAddress { get; } = baseAddress;

    public string Environment { get; } = "Development";
}
