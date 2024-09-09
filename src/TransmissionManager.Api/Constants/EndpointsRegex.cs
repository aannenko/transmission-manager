namespace TransmissionManager.Api.Constants;

internal static class EndpointsRegex
{
    // language=regex
    public const string IsCron =
        @"^((\*(\/\d{1,2})?|\d{1,2}(\/\d{1,2})?|(\d{1,2}-\d{1,2})(\/\d{1,2})?|((\d{1,2},)+\d{1,2}))\s){4}(\*(\/\d{1,2})?|\d{1,2}(\/\d{1,2})?|(\d{1,2}-\d{1,2})(\/\d{1,2})?|((\d{1,2},)+\d{1,2}))$";
}
