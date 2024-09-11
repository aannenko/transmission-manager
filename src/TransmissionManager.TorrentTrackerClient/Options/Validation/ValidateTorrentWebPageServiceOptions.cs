using Microsoft.Extensions.Options;

namespace TransmissionManager.TorrentTrackerClient.Options.Validation;

[OptionsValidator]
public partial class ValidateTorrentWebPageServiceOptions : IValidateOptions<TorrentWebPageServiceOptions>
{
}
