using TransmissionManager.Database.Models;
using TransmissionManager.Database.Services;

namespace TransmissionManager.Api.Services.Development;

internal static class DevDatabaseSeeder
{
    private static readonly string[][] _nameWords =
    [
        ["Crimson", "Silent", "Frozen", "Blazing", "Hidden", "Ancient", "Digital", "Phantom", "Eternal", "Savage"],
        ["Dragon", "Shadow", "Empire", "Horizon", "Thunder", "Crystal"],
        ["Chronicles", "Rising", "Legacy", "Protocol", "Odyssey"],
    ];

    public static int TorrentCount { get; } =
        _nameWords[0].Length * _nameWords[1].Length * _nameWords[2].Length;

    private static readonly string[] _downloadDirs =
    [
        "/downloads/anime",
        "/downloads/movies",
        "/downloads/tv-shows",
        "/downloads/software",
        "/downloads/music",
        "/downloads/books",
    ];

    // Impossible dates: these pass the CronAttribute regex but never fire
    private static readonly string[] _cronExpressions =
    [
        "0 0 30 2 *",
        "15 3 31 2 *",
        "30 6 31 4 *",
        "45 9 31 6 *",
        "0 12 31 9 *",
        "30 18 31 11 *",
    ];

    public static async Task SeedAsync(AppDbContext dbContext, CancellationToken cancellationToken = default)
    {
        var torrents = new Torrent[TorrentCount];
        var baseDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        for (var i = 0; i < TorrentCount; i++)
        {
            // Scatter names so consecutive IDs are visually distinct (137 is coprime with 300)
            var j = i * 137 % TorrentCount;
            var word1 = _nameWords[0][j / (_nameWords[1].Length * _nameWords[2].Length)];
            var word2 = _nameWords[1][j / _nameWords[2].Length % _nameWords[1].Length];
            var word3 = _nameWords[2][j % _nameWords[2].Length];

            torrents[i] = new Torrent
            {
                Id = 0,
                HashString = GenerateHashString(i),
                RefreshDate = baseDate.AddHours(i * 7.3),
                Name = $"{word1} {word2} {word3}",
                WebPageUri = $"https://torrents.example.com/view/{i + 1}",
                DownloadDir = _downloadDirs[i % _downloadDirs.Length],
                MagnetRegexPattern = i % 5 == 0 ? @"magnet:\?xt=urn:btih:[0-9a-fA-F]{40}" : null,
                Cron = i % 3 != 0 ? _cronExpressions[i % _cronExpressions.Length] : null,
            };
        }

        dbContext.Torrents.AddRange(torrents);
        _ = await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private static string GenerateHashString(int index)
    {
        return string.Create(40, index, static (span, seed) =>
        {
            // 5 bijective hashes (odd multipliers mod 2^32) × 8 hex chars = 40 chars
            WriteHexBytes(span, 0, unchecked((uint)seed * 2654435761U + 2246822519U));
            WriteHexBytes(span, 8, unchecked((uint)seed * 3266489917U + 668265263U));
            WriteHexBytes(span, 16, unchecked((uint)seed * 951274213U + 374761393U));
            WriteHexBytes(span, 24, unchecked((uint)seed * 2869860233U + 2654435761U));
            WriteHexBytes(span, 32, unchecked((uint)seed * 1103515245U + 12345U));

            static void WriteHexBytes(Span<char> span, int offset, uint value)
            {
                const string hexChars = "0123456789abcdef";
                for (var i = 0; i < 8; i++)
                {
                    span[offset + i] = hexChars[(int)(value >> 28)];
                    value <<= 4;
                }
            }
        });
    }
}
