using Microsoft.Extensions.Options;

namespace TransmissionManager.TorrentSources.Options.Validation;

[OptionsValidator]
public sealed partial class ValidateTorrentWebPageClientOptions
    : IValidateOptions<TorrentWebPageClientOptions>
{
}
