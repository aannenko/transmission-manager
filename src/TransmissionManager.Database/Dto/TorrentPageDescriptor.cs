namespace TransmissionManager.Database.Dto;

public readonly record struct TorrentPageDescriptor<TAfter>(
    TorrentOrder OrderBy = TorrentOrder.Id,
    int Take = 20,
    long AfterId = 0,
    TAfter? After = default)
{
    public TorrentPageDescriptor() : this(TorrentOrder.Id)
    {
    }

    public TorrentOrder OrderBy { get; } =
        Enum.IsDefined(OrderBy)
            ? OrderBy
            : throw new ArgumentOutOfRangeException(nameof(OrderBy));

    public int Take { get; } =
        Take > 0
            ? Take
            : throw new ArgumentOutOfRangeException(nameof(Take));
}
