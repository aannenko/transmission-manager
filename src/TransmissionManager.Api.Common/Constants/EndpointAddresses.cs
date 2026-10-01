namespace TransmissionManager.Api.Common.Constants;

/// <summary>Provides relative API endpoint addresses.</summary>
public static class EndpointAddresses
{
    /// <summary>Gets the torrent collection endpoint.</summary>
    /// <returns>The relative torrent collection address.</returns>
    public static string Torrents => "/api/v1/torrents";

    /// <summary>Gets the application-version endpoint.</summary>
    /// <returns>The relative application-version address.</returns>
    public static string AppVersion => "/api/v1/appversion";
}
