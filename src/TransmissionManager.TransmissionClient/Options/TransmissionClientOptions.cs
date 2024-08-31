namespace TransmissionManager.TransmissionClient.Options;

public sealed class TransmissionClientOptions
{
    public required string BaseAddress { get; set; }

    public required string RpcEndpointAddressSuffix { get; set; }
}
