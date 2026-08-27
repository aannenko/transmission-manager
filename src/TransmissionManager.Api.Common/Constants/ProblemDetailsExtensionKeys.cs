namespace TransmissionManager.Api.Common.Constants;

public static class ProblemDetailsExtensionKeys
{
    public static readonly string CurrentVersion = "currentVersion";

    /// <remarks>
    /// The key ASP.NET Core reports validation failures under. Fill it with a dictionary
    /// with keys containing field names, and values being arrays of error messages.
    /// </remarks>
    public static readonly string Errors = "errors";
}
