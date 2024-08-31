namespace TransmissionManager.TransmissionClient.Dto;

public sealed class TransmissionTorrentGetResponse : ITransmissionResponse
{
    public required string Result { get; init; }

    public TransmissionTorrentGetResponseArguments? Arguments { get; init; }

    public int? Tag { get; init; }
}

public sealed class TransmissionTorrentGetResponseArguments
{
    public TransmissionTorrentGetResponseItem[]? Torrents { get; init; }
}

public sealed class TransmissionTorrentGetResponseItem
{
    public long? Id { get; init; }

    public string? Name { get; init; }

    public long? SizeWhenDone { get; init; }

    public double? PercentDone { get; init; }

    public string? DownloadDir { get; init; }
}
