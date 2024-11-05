namespace TransmissionManager.Api.Shared.Constants;

internal static class EndpointRegex
{
    // language=regex
    public const string IsCron =
        @"^((\*(/\d{1,2})?|\d{1,2}(/\d{1,2})?|(\d{1,2}-\d{1,2})(/\d{1,2})?|((\d{1,2},)+\d{1,2}))\s){4}(\*(/\d{1,2})?|\d{1,2}(/\d{1,2})?|(\d{1,2}-\d{1,2})(/\d{1,2})?|((\d{1,2},)+\d{1,2}))$";
}
