using Microsoft.Extensions.Options;

namespace TransmissionManager.Transmission.Options.Validation;

/// <summary>Validates <see cref="SessionHeaderProviderOptions"/>.</summary>
[OptionsValidator]
public sealed partial class ValidateSessionHeaderProviderOptions
    : IValidateOptions<SessionHeaderProviderOptions>
{
}
