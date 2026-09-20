using Calendar.Client.Resources;
using Calendar.Shared.Contracts;

namespace Calendar.Client.Services;

/// <summary>
/// Turns an error code into something a person can read.
/// </summary>
/// <remarks>
/// This is the one place the server's vocabulary meets the user's language. The server answers
/// <c>event.not_found</c> and knows nothing about who is reading; the wording lives here,
/// beside every other string the application shows.
/// </remarks>
public static class ErrorMessages
{
    /// <summary>Looks up the message for an error code.</summary>
    /// <param name="code">
    /// An <see cref="ApiErrorCodes"/> entry the server sent, or a <see cref="ClientErrorCodes"/>
    /// entry for a call that never reached it. May be <c>null</c>.
    /// </param>
    /// <returns>
    /// The message to show. A code this version does not know still produces a sentence rather
    /// than a blank space or a raw identifier: a newer server must not leave the user staring at
    /// nothing.
    /// </returns>
    public static string For(string? code) => code switch
    {
        ClientErrorCodes.ServerUnreachable => Strings.ErrorServerUnreachable,
        ClientErrorCodes.RequestTimedOut => Strings.ErrorRequestTimedOut,
        ClientErrorCodes.ResponseUnreadable => Strings.ErrorResponseUnreadable,

        ApiErrorCodes.EventNotFound => Strings.ErrorEventNotFound,
        ApiErrorCodes.EventTitleRequired => Strings.ErrorEventTitleRequired,
        ApiErrorCodes.EventTitleTooLong => Strings.ErrorEventTitleTooLong,
        ApiErrorCodes.EventDescriptionTooLong => Strings.ErrorEventDescriptionTooLong,
        ApiErrorCodes.EventDatesRequired => Strings.ErrorEventDatesRequired,
        ApiErrorCodes.EventEndNotAfterStart => Strings.ErrorEventEndNotAfterStart,
        ApiErrorCodes.RequestBodyMissing => Strings.ErrorRequestInvalid,
        ApiErrorCodes.RangeRequired => Strings.ErrorRequestInvalid,
        ApiErrorCodes.RangeEndNotAfterStart => Strings.ErrorRequestInvalid,

        _ => Strings.ErrorUnexpected,
    };
}
