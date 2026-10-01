using System.Globalization;
using System.Text;

namespace TransmissionManager.Database.Dto;

/// <summary>Describes a keyset-paginated torrent page.</summary>
/// <param name="OrderBy">The sort order.</param>
/// <param name="AnchorId">The anchor torrent ID, or <see langword="null"/> for the first page.</param>
/// <param name="AnchorValue">The typed anchor value for a non-ID sort.</param>
/// <param name="Direction">The direction from the anchor.</param>
/// <param name="Take">The maximum number of torrents to return.</param>
/// <typeparam name="TAnchor">The anchor value type.</typeparam>
/// <exception cref="ArgumentOutOfRangeException">
/// <paramref name="OrderBy"/> or <paramref name="Direction"/> is undefined, or
/// <paramref name="Take"/> is not positive.
/// </exception>
/// <exception cref="ArgumentException">
/// <paramref name="AnchorValue"/> is incompatible with <paramref name="OrderBy"/>.
/// </exception>
public readonly record struct TorrentPageDescriptor<TAnchor>(
    TorrentOrder OrderBy = TorrentOrder.Id,
    long? AnchorId = null,
    TAnchor? AnchorValue = default,
    PaginationDirection Direction = PaginationDirection.Forward,
    int Take = 20)
{
    /// <summary>Initializes a descriptor with the default page settings.</summary>
    public TorrentPageDescriptor() : this(OrderBy: TorrentOrder.Id)
    {
    }

    /// <summary>Gets the validated sort order.</summary>
    /// <returns>The sort order.</returns>
    public TorrentOrder OrderBy { get; } = Enum.IsDefined(OrderBy)
        ? OrderBy
        : throw new ArgumentOutOfRangeException(nameof(OrderBy));

    /// <summary>Gets the validated anchor value.</summary>
    /// <returns>The anchor value for the selected sort order.</returns>
    public TAnchor? AnchorValue { get; } = OrderBy.IsCompatibleWith(AnchorValue)
        ? AnchorValue
        : throw new ArgumentException(
            string.Format(
                CultureInfo.InvariantCulture,
                OrderByAndAnchorValueErrorFormat,
                OrderBy,
                typeof(TAnchor),
                AnchorValue),
            nameof(AnchorValue));

    /// <summary>Gets the validated pagination direction.</summary>
    /// <returns>The pagination direction.</returns>
    public PaginationDirection Direction { get; } = Enum.IsDefined(Direction)
        ? Direction
        : throw new ArgumentOutOfRangeException(nameof(Direction));

    /// <summary>Gets the validated page size.</summary>
    /// <returns>The maximum number of torrents to return.</returns>
    public int Take { get; } = Take > 0
        ? Take
        : throw new ArgumentOutOfRangeException(nameof(Take));

    internal static CompositeFormat OrderByAndAnchorValueErrorFormat { get; } = CompositeFormat.Parse(
        $"Incompatible arguments {nameof(OrderBy)} '{{0}}' and {nameof(AnchorValue)} {{1}} '{{2}}' were provided.");
}
