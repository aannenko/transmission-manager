using System.ComponentModel.DataAnnotations;

namespace TransmissionManager.TransmissionClient.Options;

public sealed class TransmissionServiceOptions
{
    [Required]
    [RegularExpression(@"^http(s?)://[\w-.]+:\d{1,5}$")]
    public required string BaseAddress { get; set; }

    public required string RpcEndpointAddressSuffix { get; set; } = "/transmission/rpc";
}
