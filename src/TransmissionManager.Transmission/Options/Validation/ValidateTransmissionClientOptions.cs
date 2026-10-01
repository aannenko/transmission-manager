using Microsoft.Extensions.Options;

namespace TransmissionManager.Transmission.Options.Validation;

/// <summary>Validates <see cref="TransmissionClientOptions"/>.</summary>
[OptionsValidator]
public sealed partial class ValidateTransmissionClientOptions
    : IValidateOptions<TransmissionClientOptions>
{
}
