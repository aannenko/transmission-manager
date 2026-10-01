using System.ComponentModel.DataAnnotations;

namespace TransmissionManager.Web.Validation;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
internal sealed class ApiAddressAttribute : ValidationAttribute
{
    public ApiAddressAttribute()
        : base(static () => "The address is not valid.")
    {
    }

    public override bool IsValid(object? value) =>
        value is null || (value is string text && ApiAddressUtils.TryCreate(text, out _));
}
