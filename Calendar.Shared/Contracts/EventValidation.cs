using System;

namespace Calendar.Shared.Contracts;

/// <summary>
/// The rules an event has to satisfy, shared by both sides.
/// </summary>
/// <remarks>
/// Defined once and called twice. The client checks before sending, because a round trip to be
/// told the title is empty is wasted; the server checks again because it is the boundary and
/// this client is not the only thing that can reach it. Two copies of these rules would drift,
/// and the drift would show up as a request the client swore was fine being refused.
/// </remarks>
public static class EventValidation
{
    /// <summary>Validates a create or update payload.</summary>
    /// <param name="request">The payload; may be null or missing fields entirely.</param>
    /// <returns>
    /// <c>null</c> when the payload is usable, otherwise the <see cref="ApiErrorCodes"/> entry
    /// naming why it was rejected. A code rather than a sentence: the wording belongs to
    /// whoever displays it.
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
