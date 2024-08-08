namespace TransmissionManager.Api.Database.Dto;

public sealed class TorrentUpdateDto
{
    // null is ignored
    public long? TransmissionId { get; set; }

    // null is ignored
    public string? Name { get; set; }

    // null is ignored
    public string? DownloadDir { get; set; }

    // null is ignored, string.Empty sets the value in the DB to null
    public string? MagnetRegexPattern { get; set; }

    // null is ignored, string.Empty sets the value in the DB to null
    public string? Cron { get; set; }
}
