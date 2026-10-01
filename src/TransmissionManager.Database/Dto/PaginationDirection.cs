namespace TransmissionManager.Database.Dto;

/// <summary>Defines the direction of keyset pagination.</summary>
public enum PaginationDirection
{
    /// <summary>Moves after the supplied anchor.</summary>
    Forward,
    /// <summary>Moves before the supplied anchor.</summary>
    Backward,
}
