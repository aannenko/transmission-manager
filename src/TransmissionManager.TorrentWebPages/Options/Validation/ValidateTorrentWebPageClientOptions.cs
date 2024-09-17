using Microsoft.Extensions.Options;

namespace TransmissionManager.TorrentWebPages.Options.Validation;

[OptionsValidator]
public sealed partial class ValidateTorrentWebPageClientOptions
    : IValidateOptions<TorrentWebPageClientOptions>
{
}
