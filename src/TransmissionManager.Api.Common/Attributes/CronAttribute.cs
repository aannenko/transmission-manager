using System.ComponentModel.DataAnnotations;

namespace TransmissionManager.Api.Common.Attributes;

public sealed class CronAttribute : RegularExpressionAttribute
{
    private const string _isCron =
        @"^((\*(\d{1,2})?|\d{1,2}(\d{1,2})?|(\d{1,2}-\d{1,2})(\d{1,2})?|((\d{1,2},)+\d{1,2}))\s){4}(\*(\d{1,2})?|\d{1,2}(\d{1,2})?|(\d{1,2}-\d{1,2})(\d{1,2})?|((\d{1,2},)+\d{1,2}))$";

    public CronAttribute() : base(_isCron)
    {
        MatchTimeoutInMilliseconds = 50;
        ErrorMessage = "Invalid or unsupported cron expression.";
    }
}
