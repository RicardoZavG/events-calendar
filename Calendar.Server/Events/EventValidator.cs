using Calendar.Server.Data;
using Calendar.Shared.Contracts;

namespace Calendar.Server.Events;

/// <summary>
/// Checks an incoming event payload before anything is stored.
/// </summary>
/// <remarks>
/// Data arriving over the network is untrusted, whatever sent it. Every field is checked here
/// so a bad request is answered with a message the caller can act on, rather than surfacing as
/// a database error later.
/// </remarks>
public static class EventValidator
{
    /// <summary>Validates a create or update payload.</summary>
    /// <param name="request">The body as received; may be missing fields entirely.</param>
    /// <returns>
    /// <c>null</c> when the payload is usable, otherwise the reason it was rejected, phrased
    /// for the person reading it and free of internal detail.
    /// </returns>
    public static string? Validate(EventRequest? request)
    {
        if (request is null)
        {
            return "El cuerpo de la petición está vacío.";
        }

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return "El título es obligatorio.";
        }

        if (request.Title.Length > CalendarDbContext.TitleMaxLength)
        {
            return $"El título no puede superar los {CalendarDbContext.TitleMaxLength} caracteres.";
        }

        if (request.Description is { Length: > CalendarDbContext.DescriptionMaxLength })
        {
            return $"La descripción no puede superar los {CalendarDbContext.DescriptionMaxLength} caracteres.";
        }

        if (request.Start is null || request.End is null)
        {
            return "La fecha de inicio y la de fin son obligatorias.";
        }

        if (request.End <= request.Start)
        {
            return "La fecha de fin debe ser posterior a la de inicio.";
        }

        return null;
    }

    /// <summary>Normalizes an instant to UTC.</summary>
    /// <param name="value">The instant as received.</param>
    /// <returns>
    /// The same moment expressed in UTC. A value that arrives without a kind is taken as
    /// already being UTC, which is what the API documents clients should send.
    /// </returns>
    public static DateTime ToUtc(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
        };
    }
}
