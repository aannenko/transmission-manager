using System.ComponentModel.DataAnnotations;

namespace TransmissionManager.Web.Validation;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
internal sealed class HostNameAttribute : ValidationAttribute
{
    public HostNameAttribute()
        : base(static () => "The host name is not valid.")
    {
    }

    public override bool IsValid(object? value)
    {
        if (value is null)
            return true;

        if (value is string hostNameString)
            return Uri.CheckHostName(hostNameString) is not UriHostNameType.Unknown;

        return false;
    }
}
