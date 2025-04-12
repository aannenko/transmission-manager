using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace TransmissionManager.Transmission.Options;

public sealed class TransmissionClientOptions
{
    private readonly Lazy<Uri> _lazyBaseAddressUri;
    private readonly Lazy<Uri> _lazyRpcEndpointAddressSuffixUri;

    public TransmissionClientOptions()
    {
        _lazyBaseAddressUri = new(() => new(BaseAddress!));
        _lazyRpcEndpointAddressSuffixUri = new(() => new(RpcEndpointAddressSuffix!, UriKind.Relative));
    }

    [StringSyntax(StringSyntaxAttribute.Uri)]
    [Required]
    [RegularExpression(@"^http(s?)://[\w-.]+:\d{1,5}$")]
    public required string BaseAddress { get; set; }

    [Required]
    public required string RpcEndpointAddressSuffix { get; set; } = "/transmission/rpc";

    public Uri BaseAddressUri => _lazyBaseAddressUri.Value;

    public Uri RpcEndpointAddressSuffixUri => _lazyRpcEndpointAddressSuffixUri.Value;
}
