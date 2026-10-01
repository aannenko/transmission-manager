using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using Direction = TransmissionManager.Api.Common.Dto.Torrents.GetTorrentPageDirection;
using Order = TransmissionManager.Api.Common.Dto.Torrents.GetTorrentPageOrder;

namespace TransmissionManager.Api.Common.Dto.Torrents;

/// <summary>Describes a filtered keyset-paginated torrent page.</summary>
/// <param name="OrderBy">The sort order.</param>
/// <param name="AnchorId">The anchor torrent ID, or <see langword="null"/> for the first page.</param>
/// <param name="AnchorValue">The formatted anchor value for a non-ID sort.</param>
/// <param name="Take">The maximum number of torrents to return.</param>
/// <param name="Direction">The direction from the anchor.</param>
/// <param name="PropertyStartsWith">The optional case-insensitive property prefix.</param>
/// <param name="CronExists">Whether to require or exclude torrents with a refresh schedule.</param>
[UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "Tested after trimming")]
public readonly record struct GetTorrentPageParameters(
    [property: EnumDataType(typeof(Order))] Order OrderBy = Order.Id,
    long? AnchorId = null,
    string? AnchorValue = null,
    [property: Range(1, GetTorrentPageParameters._maxTake)] int Take = 20,
    [property: EnumDataType(typeof(Direction))] Direction Direction = Direction.Forward,
    [property: MinLength(1)] string? PropertyStartsWith = null,
    bool? CronExists = null)
{
    private const int _maxTake = 10000;

    /// <summary>Gets the largest accepted page size.</summary>
    /// <returns>The maximum number of torrents in one page.</returns>
    public static int MaxTake => _maxTake;

    /// <summary>Gets the round-trip format used for date cursor values.</summary>
    /// <returns>The cursor date format.</returns>
    public static string DateFormat => "yyyyMMddHHmmssfffffffZ";
}
