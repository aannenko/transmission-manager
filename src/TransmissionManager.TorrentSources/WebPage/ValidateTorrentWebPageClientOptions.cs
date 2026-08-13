using Microsoft.Extensions.Options;

namespace TransmissionManager.TorrentSources.WebPage;

[OptionsValidator]
public sealed partial class ValidateTorrentWebPageClientOptions
    : IValidateOptions<TorrentWebPageClientOptions>
{
}
