using Microsoft.Extensions.Options;

namespace TransmissionManager.TorrentWebPages.Options.Validation;

[OptionsValidator]
public sealed partial class ValidateTorrentWebPageServiceOptions
    : IValidateOptions<TorrentWebPageServiceOptions>
{
}
