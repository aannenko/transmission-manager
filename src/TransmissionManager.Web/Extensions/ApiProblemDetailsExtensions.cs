using TransmissionManager.Web.Dto;

namespace TransmissionManager.Web.Extensions;

internal static class ApiProblemDetailsExtensions
{
    /// <summary>
    /// Joins the problem details response's key-valued errors into one line.
    /// </summary>
    /// <param name="problemDetails">The response to read.</param>
    /// <returns>The joined messages, or an empty string if the response carries none.</returns>
    /// <remarks>
    /// The key is written as the API sent it, because several messages do not say what they are
    /// about on their own - a torrent source answering <c>502</c> reports only that "the server
    /// responded", and which server that was is what the key says.
    /// <para>
    /// A message the API keyed to more than one field is repeated under each of them. That is
    /// verbose but complete, and it keeps this a straight walk of what arrived.
    /// </para>
    /// </remarks>
    public static string JoinErrorMessages(this ApiProblemDetails problemDetails)
    {
        if (problemDetails.Errors is null)
            return string.Empty;

        var described = new List<string>();
        foreach (var (key, messages) in problemDetails.Errors)
        {
            if (messages is null)
                continue;

            foreach (var message in messages)
            {
                if (!string.IsNullOrEmpty(message))
                    described.Add($"{key}: {message}");
            }
        }

        return string.Join(" ", described);
    }
}
