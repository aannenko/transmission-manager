using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace TransmissionManager.TorrentSources.Options;

/// <summary>
/// Settings shared by every kind of torrent source.
/// </summary>
/// <remarks>
/// Inherited rather than bound on its own, so each source's options carry these alongside their
/// own. They all bind the same configuration section, so a setting declared here is one key, read
/// once and set once, no matter how many sources read it.
/// </remarks>
public abstract class TorrentSourcesOptions
{
    /// <summary>
    /// Bounds reading and scanning a source's response body, starting once its headers have arrived.
    /// </summary>
    /// <remarks>
    /// Additive to the resilience pipeline rather than inclusive of it: the pipeline's timeouts end
    /// at the response headers, so this one is armed only from that point and the two never overlap.
    /// A source's worst case is therefore the pipeline's TotalRequestTimeout plus this value, and no
    /// value here can truncate the pipeline's retries.
    /// </remarks>
    [Required]
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "Tested after trimming")]
    [Range(typeof(TimeSpan), "00:00:01", "00:10:00")]
    public required TimeSpan ResponseReadTimeout { get; set; }
}
