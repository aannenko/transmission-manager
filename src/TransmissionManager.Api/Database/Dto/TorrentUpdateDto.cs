namespace TransmissionManager.Api.Database.Dto;

public sealed class TorrentUpdateDto
{
    public TorrentUpdateDto(
        long? transmissionId = null,
        string? name = null,
        string? downloadDir = null,
        string? magnetRegexPattern = null,
        string? cron = null)
    {
        if (transmissionId is not null)
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(transmissionId.Value, nameof(transmissionId));

        if (name is not null)
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (downloadDir is not null)
            ArgumentException.ThrowIfNullOrWhiteSpace(downloadDir);

        TransmissionId = transmissionId;
        Name = name;
        DownloadDir = downloadDir;
        MagnetRegexPattern = magnetRegexPattern;
        Cron = cron;
    }

    // null is ignored
    public long? TransmissionId { get; }

    // null is ignored
    public string? Name { get; }

    // null is ignored
    public string? DownloadDir { get; }

    // null is ignored, string.Empty sets the value in the DB to null
    public string? MagnetRegexPattern { get; }

    // null is ignored, string.Empty sets the value in the DB to null
    public string? Cron { get; }
}
