using Microsoft.Extensions.Options;

namespace TransmissionManager.Api.Options.Validation;

[OptionsValidator]
internal sealed partial class ValidateCorsPolicyOptions : IValidateOptions<CorsPolicyOptions>
{
}
