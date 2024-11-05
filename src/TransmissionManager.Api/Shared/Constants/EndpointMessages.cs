using System.Text;

namespace TransmissionManager.Api.Shared.Constants;

internal static class EndpointMessages
{
    public const string IdNotFound = "Torrent with id {0} was not found.";

    public static readonly CompositeFormat IdNotFoundFormat = CompositeFormat.Parse(IdNotFound);
}
