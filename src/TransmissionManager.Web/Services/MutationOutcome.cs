namespace TransmissionManager.Web.Services;

internal enum MutationStatus
{
    // Default — CallService returns this on HttpRequestException (network/5xx/unhandled 4xx).
    Failed,
    Success,
    NotFound,
    Conflict,
}

// Result of a mutation (PATCH/DELETE). 404 and 409 are normal outcomes the UI handles directly;
// other non-success codes surface as HttpRequestException and become Failed via CallService.
internal readonly record struct MutationOutcome(MutationStatus Status, long? CurrentVersion)
{
    public static MutationOutcome Success { get; } = new(MutationStatus.Success, null);

    public static MutationOutcome NotFound { get; } = new(MutationStatus.NotFound, null);

    public static MutationOutcome Conflict(long? currentVersion) =>
        new(MutationStatus.Conflict, currentVersion);
}
