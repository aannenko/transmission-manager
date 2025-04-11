using System.ComponentModel.DataAnnotations;
using System.Text;
using Direction = TransmissionManager.Api.Common.Dto.Torrents.FindPage.FindTorrentPageDirection;
using Order = TransmissionManager.Api.Common.Dto.Torrents.FindPage.FindTorrentPageOrder;

namespace TransmissionManager.Api.Common.Dto.Torrents.FindPage;

public readonly record struct FindTorrentPageParameters(
    [property: EnumDataType(typeof(Order))] Order OrderBy = Order.Id,
    long? AnchorId = null,
    string? AnchorValue = null,
    [property: Range(1, FindTorrentPageParameters._maxTake)] int Take = 20,
    [property: EnumDataType(typeof(Direction))] Direction Direction = Direction.Forward,
#pragma warning disable IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code - tested after trimming
    [property: MinLength(1)] string? PropertyStartsWith = null,
#pragma warning restore IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code
    bool? CronExists = null) : IValidatableObject
{
    private const int _maxTake = 1000;

    public static int MaxTake => _maxTake;

    private static readonly CompositeFormat _orderByAndAnchorValueErrorFormat = CompositeFormat.Parse(
        $"Incompatible arguments {nameof(OrderBy)} '{{0}}' and {nameof(AnchorValue)} '{{1}}' were provided.");

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (OrderBy is Order.Id or Order.IdDesc && AnchorValue is not null)
        {
            yield return new ValidationResult(
                string.Format(null, _orderByAndAnchorValueErrorFormat, OrderBy, AnchorValue),
                [nameof(AnchorValue)]);
        }
    }
}
