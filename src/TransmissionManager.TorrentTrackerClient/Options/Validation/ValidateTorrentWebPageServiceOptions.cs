using Microsoft.Extensions.Options;

namespace TransmissionManager.TorrentTrackerClient.Options.Validation;

[OptionsValidator]
public sealed partial class ValidateTorrentWebPageServiceOptions
    : IValidateOptions<TorrentWebPageServiceOptions>
{
}
