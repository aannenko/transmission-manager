namespace TransmissionManager.Web.Dto;

#pragma warning disable CA1515 // Consider making public types internal - exposed via public Blazor component parameters

public enum MutationStatus
{
    Failed, // Default - if we get HttpRequestException (network/5xx/unhandled 4xx)
    Success,
    NotFound,
    Conflict,
}

public readonly record struct MutationOutcome(MutationStatus Status, long? CurrentVersion)
{
    public static MutationOutcome Success { get; } = new(MutationStatus.Success, null);

    public static MutationOutcome NotFound { get; } = new(MutationStatus.NotFound, null);

    public static MutationOutcome Conflict(long? currentVersion) =>
        new(MutationStatus.Conflict, currentVersion);
}
#pragma warning restore CA1515
