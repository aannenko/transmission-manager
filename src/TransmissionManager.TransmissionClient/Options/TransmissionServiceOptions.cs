namespace TransmissionManager.TransmissionClient.Options;

public sealed class TransmissionServiceOptions
{
    public required string BaseAddress { get; set; }

    public required string RpcEndpointAddressSuffix { get; set; }
}
