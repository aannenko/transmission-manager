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
    /// The resilience pipeline's timeouts end once the response headers arrive; this one bounds the
    /// whole search, so it must exceed the pipeline's 15-second TotalRequestTimeout in
    /// ConfigureResilience. Nothing validates that - the range below permits far less.
    /// </summary>
    [Required]
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "Tested after trimming")]
    [Range(typeof(TimeSpan), "00:00:01", "00:10:00")]
    public required TimeSpan MagnetSearchTimeout { get; set; }
}
