using System.Text;

namespace TransmissionManager.Api.Constants;

internal static class EndpointMessages
{
    public static readonly CompositeFormat IdNotFoundFormat =
        CompositeFormat.Parse("Torrent with id {0} was not found.");
}
