using System.ComponentModel.DataAnnotations;

namespace TransmissionManager.Transmission.Options;

public sealed class TransmissionClientOptions
{
    [Required]
    [RegularExpression(@"^http(s?)://[\w-.]+:\d{1,5}$")]
    public required string BaseAddress { get; set; }

    public required string RpcEndpointAddressSuffix { get; set; } = "/transmission/rpc";
}
