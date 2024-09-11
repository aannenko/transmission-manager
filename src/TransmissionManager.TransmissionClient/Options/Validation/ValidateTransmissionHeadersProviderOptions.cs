using Microsoft.Extensions.Options;

namespace TransmissionManager.TransmissionClient.Options.Validation;

[OptionsValidator]
public sealed partial class ValidateTransmissionHeadersProviderOptions
    : IValidateOptions<TransmissionHeadersProviderOptions>
{
}
