﻿namespace TransmissionManager.Transmission.Dto;

public sealed class TransmissionTorrentGetResponse : ITransmissionResponse
{
    public required string Result { get; init; }

    public TransmissionTorrentGetResponseArguments? Arguments { get; init; }

    public int? Tag { get; init; }
}

public sealed class TransmissionTorrentGetResponseArguments
{
    public IReadOnlyList<TransmissionTorrentGetResponseItem>? Torrents { get; init; }
}

public sealed class TransmissionTorrentGetResponseItem
{
    public string? HashString { get; init; } // used instead of Id

    public string? Name { get; init; }

    public long? SizeWhenDone { get; init; }

    public double? PercentDone { get; init; }

    public string? DownloadDir { get; init; }
}
