using Microsoft.Extensions.Options;

namespace TransmissionManager.TorrentSources.JsonPointer;

[OptionsValidator]
public sealed partial class ValidateTorrentJsonPointerClientOptions
    : IValidateOptions<TorrentJsonPointerClientOptions>
{
}
