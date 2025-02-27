using System.Net;
using System.Text;
using TransmissionManager.BaseTests.HttpClient;
using TransmissionManager.Database.Models;

namespace TransmissionManager.Api.IntegrationTests.Helpers;

internal static class TestData
{
    internal static class Database
    {
        public const string FirstTorrentHashString = "0bda511316a069e86dd8ee8a3610475d2013a7fa";
        public const string FirstTorrentName = "TV Show 1";
        public const string FirstTorrentWebPageAddress = "https://torrentTracker.com/forum/viewtopic.php?t=1234567";
        public const string FirstTorrentDownloadDir = "/tvshows";
        public const string FirstTorrentCron = "0 11,17 * * *";

        public const string SecondTorrentHashString = "98ad2e3a694dfc69571c25241bd4042b94a55cf5";
        public const string SecondTorrentName = "TV Show 2";
        public const string SecondTorrentWebPageAddress = "https://torrentTracker.com/forum/viewtopic.php?t=1234568";
        public const string SecondTorrentDownloadDir = "/tvshows";
        public const string SecondTorrentMagnetRegexPattern = @"magnet:\?xt=[^""]+";

        public const string ThirdTorrentHashString = "10824f01cccd5d4088d8fa04f3d46b7d319744b2";
        public const string ThirdTorrentName = "TV Show 3";
        public const string ThirdTorrentWebPageAddress = "https://torrentTracker.com/forum/viewtopic.php?t=1234569";
        public const string ThirdTorrentDownloadDir = "/tvshows";
        public const string ThirdTorrentMagnetRegexPattern = @"magnet:\?xt=urn:[^""]+";
        public const string ThirdTorrentCron = "30 9,15 * * *";

        public static Torrent[] CreateInitialTorrents() =>
            [
                new()
                {
                    Id = default,
                    HashString = FirstTorrentHashString,
                    Name = FirstTorrentName,
                    WebPageUri = FirstTorrentWebPageAddress,
                    DownloadDir = FirstTorrentDownloadDir,
                    Cron = FirstTorrentCron,
                },
                new()
                {
                    Id = default,
                    HashString = SecondTorrentHashString,
                    Name = SecondTorrentName,
                    WebPageUri = SecondTorrentWebPageAddress,
                    DownloadDir = SecondTorrentDownloadDir,
                    MagnetRegexPattern = SecondTorrentMagnetRegexPattern,
                },
                new()
                {
                    Id = default,
                    HashString = ThirdTorrentHashString,
                    Name = ThirdTorrentName,
                    WebPageUri = ThirdTorrentWebPageAddress,
                    DownloadDir = ThirdTorrentDownloadDir,
                    MagnetRegexPattern = ThirdTorrentMagnetRegexPattern,
                    Cron = ThirdTorrentCron,
                },
            ];
    }

    internal static class WebPages
    {
        public const string WebPageHtml = """
            <!DOCTYPE html>
            <html lang="en">
            <head>
                <meta charset="UTF-8">
                <meta name="viewport" content="width=device-width, initial-scale=1.0">
                <title>Magnet Link Example</title>
            </head>
            <body>
                <h1>Magnet Link Example</h1>
                <p>Click the link below to open the Magnet URI:</p>
                <a href="{0}">Download via Magnet</a>
            </body>
            </html>
            """;

        public const string FirstPageMagnetExisting =
            "magnet:?xt=urn:btih:0BDA511316A069E86DD8EE8A3610475D2013A7FA&dn=TV+Show+1";

        public const string SecondPageMagnetUpdated =
            "magnet:?xt=urn:btih:9EBC251E08FB1AECB7B24D26385341875473919A&dn=TV+Show+2";

        public const string ThirdPageMagnetRemovedFromTransmission =
            "magnet:?xt=urn:btih:10824F01CCCD5D4088D8FA04F3D46B7D319744B2&dn=TV+Show+3";

        public const string FourthPageMagnetNew =
            "magnet:?xt=urn:btih:3A81AAA70E75439D332C146ABDE899E546356BE2&dn=TV+Show+4";

        public static readonly CompositeFormat WebPageHtmlFormat = CompositeFormat.Parse(WebPageHtml);

        public static readonly Dictionary<TestRequest, TestResponse> RequestResponseMap = new()
        {
            [new(HttpMethod.Get, new(Database.FirstTorrentWebPageAddress))] =
                new(HttpStatusCode.OK, Content: string.Format(null, WebPageHtmlFormat, FirstPageMagnetExisting)),

            [new(HttpMethod.Get, new(Database.SecondTorrentWebPageAddress))] =
                new(HttpStatusCode.OK, Content: string.Format(null, WebPageHtmlFormat, SecondPageMagnetUpdated)),

            [new(HttpMethod.Get, new(Database.ThirdTorrentWebPageAddress))] =
                new(HttpStatusCode.OK, Content: string.Format(null, WebPageHtmlFormat, ThirdPageMagnetRemovedFromTransmission)),

            [new(HttpMethod.Get, new("https://torrentTracker.com/forum/viewtopic.php?t=1234570"))] =
                new(HttpStatusCode.OK, Content: string.Format(null, WebPageHtmlFormat, FourthPageMagnetNew)),
        };
    }

    internal static class Transmission
    {
        public const string SessionHeaderName = "X-Transmission-Session-Id";
        public const string SessionHeaderValue = "FctoNpkk6eYSSgmBV0B2DXI4SsLLSYc0lA5MdYkLpc9fDA59";

        public const string GetOneTorrentRequestBody =
            "{{\"method\":\"torrent-get\",\"arguments\":{{\"ids\":[\"{0}\"],\"fields\":[\"hashString\",\"name\",\"sizeWhenDone\",\"percentDone\",\"downloadDir\"]}}}}";

        public const string GetOneTorrentResponseBody =
            "{{\"arguments\":{{\"torrents\":[{{\"downloadDir\":\"{0}\",\"hashString\":\"{1}\",\"name\":\"{2}\",\"percentDone\":1,\"sizeWhenDone\":34008064679}}]}},\"result\":\"success\"}}";

        public const string GetOneTorrentNotFoundResponseBody =
            """{"arguments":{"torrents":[]},"result":"success"}""";

        public const string AddTorrentRequestBody =
            "{{\"method\":\"torrent-add\",\"arguments\":{{\"filename\":\"{0}\",\"download-dir\":\"{1}\"}}}}";

        public const string AddTorrentAddedResponseBody =
            "{{\"arguments\":{{\"torrent-added\":{{\"hashString\":\"{0}\",\"id\":{1},\"name\":\"{2}\"}}}},\"result\":\"success\"}}";

        public const string AddTorrentDuplicateResponseBody =
            "{{\"arguments\":{{\"torrent-duplicate\":{{\"hashString\":\"{0}\",\"id\":{1},\"name\":\"{2}\"}}}},\"result\":\"success\"}}";

        public const string DeleteTorrentRequestBody =
            "{{\"method\":\"torrent-remove\",\"arguments\":{{\"ids\":[\"{0}\"],\"delete-local-data\":{1}}}}}";

        public const string DeleteTorrentResponseBody =
            """{"arguments":{},"result":"success"}""";

        public const string ConflictResponseBody = $"""
            <h1>409: Conflict</h1><p>Your request had an invalid session-id header.</p><p>To fix this, follow these steps:<ol><li> When reading a response, get its X-Transmission-Session-Id header and remember it<li> Add the updated header to your outgoing requests<li> When you get this 409 error message, resend your request with the updated header</ol></p><p>This requirement has been added to help prevent <a href="https://en.wikipedia.org/wiki/Cross-site_request_forgery">CSRF</a> attacks.</p><p><code>{SessionHeaderName}: {SessionHeaderValue}</code></p>
            """;

        public static readonly CompositeFormat GetOneTorrentRequestBodyFormat =
            CompositeFormat.Parse(GetOneTorrentRequestBody);

        public static readonly CompositeFormat GetOneTorrentResponseBodyFormat =
            CompositeFormat.Parse(GetOneTorrentResponseBody);

        public static readonly CompositeFormat AddTorrentRequestBodyFormat =
            CompositeFormat.Parse(AddTorrentRequestBody);

        public static readonly CompositeFormat AddTorrentAddedResponseBodyFormat =
            CompositeFormat.Parse(AddTorrentAddedResponseBody);

        public static readonly CompositeFormat AddTorrentDuplicateResponseBodyFormat =
            CompositeFormat.Parse(AddTorrentDuplicateResponseBody);

        public static readonly CompositeFormat DeleteTorrentRequestBodyFormat =
            CompositeFormat.Parse(DeleteTorrentRequestBody);

        public static readonly Uri ApiUri = new("http://transmission:9091/transmission/rpc");

        public static readonly Dictionary<string, string> EmptyRequestHeaders = new()
        {
            [SessionHeaderName] = string.Empty
        };

        public static readonly Dictionary<string, string> FilledRequestHeaders = new()
        {
            [SessionHeaderName] = SessionHeaderValue
        };

        public static readonly Dictionary<string, string> DefaultResponseHeaders = new()
        {
            ["Server"] = "Transmission",
            ["Access-Control-Allow-Origin"] = "*",
            ["Date"] = "Sun, 13 Oct 2024 09:25:11 GMT"
        };

        public static readonly Dictionary<string, string> ConflictResponseHeaders = new()
        {
            ["Server"] = "Transmission",
            ["Access-Control-Allow-Origin"] = "*",
            [SessionHeaderName] = SessionHeaderValue,
            ["Access-Control-Expose-Headers"] = SessionHeaderName,
            ["Date"] = "Sun, 13 Oct 2024 09:25:11 GMT"
        };
    }

    internal static class Endpoints
    {
        public const string Torrents = "/api/v1/torrents";
        public const string AppVersion = "/api/v1/appversion";
        public const string LocalTime = "/api/v1/localtime";
        public const string TransmissionInfo = "/api/v1/transmissioninfo";
    }
}
