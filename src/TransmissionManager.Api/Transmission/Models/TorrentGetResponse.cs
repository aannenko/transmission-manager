namespace TransmissionManager.Api.Transmission.Models;

public sealed class TorrentGetResponse : ITransmissionResponse
{
    public required string Result { get; init; }

    public TorrentGetResponseArguments? Arguments { get; init; }

    public int? Tag { get; init; }
}

public sealed class TorrentGetResponseArguments
{
    public TorrentGetResponseItem[]? Torrents { get; init; }
}

public sealed class TorrentGetResponseItem
{
    public long? Id { get; init; }

    public string? Name { get; init; }

    public int? SizeWhenDone { get; init; }

    public double? PercentDone { get; init; }

    public string? DownloadDir { get; init; }
}
