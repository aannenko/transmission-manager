namespace TransmissionManager.Api.Common.Constants;

internal static class RegexPatterns
{
    // language=regex
    public const string IsFindMagnet = @"^.*magnet:\\\?.+$";

    // language=regex
    public const string IsCron =
        @"^((\*(/\d{1,2})?|\d{1,2}(/\d{1,2})?|(\d{1,2}-\d{1,2})(/\d{1,2})?|((\d{1,2},)+\d{1,2}))\s){4}(\*(/\d{1,2})?|\d{1,2}(/\d{1,2})?|(\d{1,2}-\d{1,2})(/\d{1,2})?|((\d{1,2},)+\d{1,2}))$";
}
