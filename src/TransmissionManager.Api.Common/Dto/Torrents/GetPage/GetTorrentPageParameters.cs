using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using Direction = TransmissionManager.Api.Common.Dto.Torrents.GetTorrentPageDirection;
using Order = TransmissionManager.Api.Common.Dto.Torrents.GetTorrentPageOrder;

namespace TransmissionManager.Api.Common.Dto.Torrents;

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
    private const int _maxTake = 1000;

    public static int MaxTake => _maxTake;
}
