using Microsoft.Extensions.Options;

namespace TransmissionManager.Transmission.Options.Validation;

[OptionsValidator]
public sealed partial class ValidateSessionHeaderProviderOptions
    : IValidateOptions<SessionHeaderProviderOptions>
{
}
