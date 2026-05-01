namespace TransmissionManager.Web.Services;

internal enum MutationStatus
{
    // Default. CallService returns default(MutationOutcome) when an HttpRequestException is caught
    // (e.g. 4xx other than 404/409, 5xx, network failure) - so Failed is the safe baseline.
    Failed,
    Success,
    NotFound,
    Conflict,
}

// Result of a mutation (PATCH/DELETE) where 404 Not Found and 409 Conflict are normal outcomes
// the UI must recover from. Other non-success status codes still surface as HttpRequestException
// and are converted by CallService into a default outcome (Status = Failed).
internal readonly record struct MutationOutcome(MutationStatus Status, long? CurrentVersion)
{
    public static MutationOutcome Success { get; } = new(MutationStatus.Success, null);

    public static MutationOutcome NotFound { get; } = new(MutationStatus.NotFound, null);

    public static MutationOutcome Conflict(long? currentVersion) =>
        new(MutationStatus.Conflict, currentVersion);
}
