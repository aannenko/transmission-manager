using System.Text;

namespace TransmissionManager.Api.Actions;

internal static class EndpointMessages
{
    public static readonly CompositeFormat IdNotFoundFormat =
        CompositeFormat.Parse("Torrent with id {0} was not found.");
}
