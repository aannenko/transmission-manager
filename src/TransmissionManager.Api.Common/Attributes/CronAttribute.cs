using System.ComponentModel.DataAnnotations;

namespace TransmissionManager.Api.Common.Attributes;

public sealed class CronAttribute : RegularExpressionAttribute
{
    public CronAttribute()
        : base(@"^((\*(\d{1,2})?|\d{1,2}(\d{1,2})?|(\d{1,2}-\d{1,2})(\d{1,2})?|((\d{1,2},)+\d{1,2}))\s){4}(\*(\d{1,2})?|\d{1,2}(\d{1,2})?|(\d{1,2}-\d{1,2})(\d{1,2})?|((\d{1,2},)+\d{1,2}))$")
    {
        MatchTimeoutInMilliseconds = 50;
        ErrorMessage = "Invalid or unsupported cron expression.";
    }
}
