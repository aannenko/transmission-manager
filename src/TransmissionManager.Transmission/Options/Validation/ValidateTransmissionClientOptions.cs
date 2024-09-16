using Microsoft.Extensions.Options;

namespace TransmissionManager.Transmission.Options.Validation;

[OptionsValidator]
public sealed partial class ValidateTransmissionClientOptions
    : IValidateOptions<TransmissionClientOptions>
{
}
