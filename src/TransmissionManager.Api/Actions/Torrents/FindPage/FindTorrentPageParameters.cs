using System.ComponentModel.DataAnnotations;
using System.Text;
using TransmissionManager.Database.Dto;
using Direction = TransmissionManager.Api.Actions.Torrents.FindPage.FindTorrentPageDirection;

namespace TransmissionManager.Api.Actions.Torrents.FindPage;

internal readonly record struct FindTorrentPageParameters(
    [property: EnumDataType(typeof(TorrentOrder))] TorrentOrder OrderBy = TorrentOrder.Id,
    long? AnchorId = null,
    string? AnchorValue = null,
    [property: Range(1, 1000)] int Take = 20,
    [property: EnumDataType(typeof(Direction))] Direction Direction = Direction.Forward,
#pragma warning disable IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code - tested after trimming
    [property: MinLength(1)] string? PropertyStartsWith = null,
#pragma warning restore IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code
    bool? CronExists = null) : IValidatableObject
{
    private static readonly CompositeFormat _orderByAndAnchorValueErrorFormat = CompositeFormat.Parse(
        $"Incompatible arguments {nameof(OrderBy)} '{{0}}' and {nameof(AnchorValue)} '{{1}}' were provided.");

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (OrderBy is TorrentOrder.Id or TorrentOrder.IdDesc && AnchorValue is not null)
        {
            yield return new ValidationResult(
                string.Format(null, _orderByAndAnchorValueErrorFormat, OrderBy, AnchorValue),
                [nameof(AnchorValue)]);
        }
    }
}
