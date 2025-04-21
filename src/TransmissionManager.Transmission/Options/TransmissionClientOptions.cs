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
    [RegularExpression(@"^http(s?)://[a-zA-Z_0-9\-\.]+:\d{1,5}$", MatchTimeoutInMilliseconds = 50)]
    public required string BaseAddress { get; set; }

    [Required]
    public required string RpcEndpointAddressSuffix { get; set; }

    public Uri BaseAddressUri => _lazyBaseAddressUri.Value;

    public Uri RpcEndpointAddressSuffixUri => _lazyRpcEndpointAddressSuffixUri.Value;
}
