using System.Text;

namespace TransmissionManager.Database.Dto;

public readonly record struct TorrentPageDescriptor<TAnchor>(
    TorrentOrder OrderBy = TorrentOrder.Id,
    long? AnchorId = null,
    TAnchor? AnchorValue = default,
    bool IsForwardPagination = true,
    int Take = 20)
{
    public TorrentPageDescriptor() : this(OrderBy: TorrentOrder.Id)
    {
    }

    public TorrentOrder OrderBy { get; } = Enum.IsDefined(OrderBy)
        ? OrderBy
        : throw new ArgumentOutOfRangeException(nameof(OrderBy));

    public TAnchor? AnchorValue { get; } = OrderBy.IsCompatibleWith(AnchorValue)
        ? AnchorValue
        : throw new ArgumentException(
            string.Format(null, OrderByAndAnchorValueErrorFormat, OrderBy, AnchorValue),
            nameof(AnchorValue));

    public int Take { get; } = Take > 0
        ? Take
        : throw new ArgumentOutOfRangeException(nameof(Take));

    internal static CompositeFormat OrderByAndAnchorValueErrorFormat { get; } = CompositeFormat.Parse(
        $"Incompatible arguments {nameof(OrderBy)} '{{0}}' and {nameof(AnchorValue)} '{{1}}' were provided.");
}
