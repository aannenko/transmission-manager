using System.ComponentModel.DataAnnotations;

namespace TransmissionManager.Api.Common.Attributes;

/// <summary>
/// Specifies that a value must be a supported cron expression.
/// </summary>
/// <remarks>
/// Accepts five whitespace-separated fields, each a wildcard, a number, a range or a
/// comma-separated list. Step syntax such as <c>*/5</c> is rejected on purpose - the scheduler
/// this application uses does not support it, so accepting it would schedule a refresh that never
/// runs. Field values are not range-checked; this is a shape check, not a semantic one.
/// <see langword="null"/> and the empty string are valid; use <c>[Required]</c> to enforce
/// presence.
/// </remarks>
public sealed class CronAttribute : RegularExpressionAttribute
{
    public CronAttribute()
        : base(@"^((\*(\d{1,2})?|\d{1,2}(\d{1,2})?|(\d{1,2}-\d{1,2})(\d{1,2})?|((\d{1,2},)+\d{1,2}))\s){4}(\*(\d{1,2})?|\d{1,2}(\d{1,2})?|(\d{1,2}-\d{1,2})(\d{1,2})?|((\d{1,2},)+\d{1,2}))$")
    {
        MatchTimeoutInMilliseconds = 50;
        ErrorMessage = "Invalid or unsupported cron expression.";
    }
}
