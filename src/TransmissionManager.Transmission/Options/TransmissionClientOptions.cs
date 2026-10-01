using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace TransmissionManager.Transmission.Options;

/// <summary>Configures the Transmission RPC client.</summary>
public sealed class TransmissionClientOptions
{
    private readonly Lazy<Uri> _lazyBaseAddressUri;
    private readonly Lazy<Uri> _lazyRpcEndpointAddressSuffixUri;

    /// <summary>Initializes Transmission client options.</summary>
    public TransmissionClientOptions()
    {
        _lazyBaseAddressUri = new(() => new(BaseAddress!));
        _lazyRpcEndpointAddressSuffixUri = new(() => new(RpcEndpointAddressSuffix!, UriKind.Relative));
    }

    /// <summary>Gets or sets the Transmission server base address.</summary>
    /// <returns>The absolute base address.</returns>
    [StringSyntax(StringSyntaxAttribute.Uri)]
    [Required]
    [RegularExpression(@"^http(s?)://[a-zA-Z_0-9\-\.]+:\d{1,5}$", MatchTimeoutInMilliseconds = 50)]
    public required string BaseAddress { get; set; }

    /// <summary>Gets or sets the relative RPC endpoint.</summary>
    /// <returns>The RPC endpoint suffix.</returns>
    [Required]
    public required string RpcEndpointAddressSuffix { get; set; }

    /// <summary>Gets the parsed Transmission server base address.</summary>
    /// <returns>The absolute base-address URI.</returns>
    public Uri BaseAddressUri => _lazyBaseAddressUri.Value;

    /// <summary>Gets the parsed relative RPC endpoint.</summary>
    /// <returns>The RPC endpoint URI.</returns>
    public Uri RpcEndpointAddressSuffixUri => _lazyRpcEndpointAddressSuffixUri.Value;
}
