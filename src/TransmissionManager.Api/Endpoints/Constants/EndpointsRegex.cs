namespace TransmissionManager.Api.Endpoints.Constants;

public static partial class EndpointsRegex
{
    // language=regex
    internal const string IsCron =
        @"^((\*(\/\d{1,2})?|\d{1,2}(\/\d{1,2})?|(\d{1,2}-\d{1,2})(\/\d{1,2})?|((\d{1,2},)+\d{1,2}))\s){4}(\*(\/\d{1,2})?|\d{1,2}(\/\d{1,2})?|(\d{1,2}-\d{1,2})(\/\d{1,2})?|((\d{1,2},)+\d{1,2}))$";
}
