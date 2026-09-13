using Calendar.Shared.Contracts;

namespace Calendar.Server.Events;

/// <summary>
/// Checks an incoming event payload before anything is stored.
/// </summary>
/// <remarks>
/// Data arriving over the network is untrusted, whatever sent it. Every field is checked here
/// so a bad request is rejected with a reason the caller can act on, rather than surfacing as a
/// database error later.
/// </remarks>
public static class EventValidator
{
    /// <summary>Validates a create or update payload.</summary>
    /// <param name="request">The body as received; may be missing fields entirely.</param>
    /// <returns>
    /// <c>null</c> when the payload is usable, otherwise the <see cref="ApiErrorCodes"/> entry
    /// naming why it was rejected. A code rather than a sentence: the server does not know what
    /// language the person in front of the client reads.
    /// </returns>
    public static string? Validate(EventRequest? request)
    {
        if (request is null)
        {
            return ApiErrorCodes.RequestBodyMissing;
        }

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return ApiErrorCodes.EventTitleRequired;
        }

        if (request.Title.Length > EventLimits.TitleMaxLength)
        {
            return ApiErrorCodes.EventTitleTooLong;
        }

        if (request.Description is { Length: > EventLimits.DescriptionMaxLength })
        {
            return ApiErrorCodes.EventDescriptionTooLong;
        }

        if (request.Start is null || request.End is null)
        {
            return ApiErrorCodes.EventDatesRequired;
        }

        if (request.End <= request.Start)
        {
            return ApiErrorCodes.EventEndNotAfterStart;
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
