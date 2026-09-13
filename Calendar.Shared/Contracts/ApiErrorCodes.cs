namespace Calendar.Shared.Contracts;

/// <summary>
/// The reasons the API can reject a request, as stable identifiers.
/// </summary>
/// <remarks>
/// The API answers with a code, never with prose. Wording is a decision of whoever shows it:
/// the server has no idea what language the person in front of a client reads, and a sentence
/// baked into a response cannot be translated by the side that displays it.
/// <para>
/// They live in the shared library so a client maps them through a constant instead of a
/// string literal, and a code that disappears breaks the build rather than silently failing to
/// match at runtime.
/// </para>
/// </remarks>
public static class ApiErrorCodes
{
    /// <summary>The request carried no body at all.</summary>
    public const string RequestBodyMissing = "request.body_missing";

    /// <summary>The event has no title, or only whitespace.</summary>
    public const string EventTitleRequired = "event.title_required";

    /// <summary>The title is longer than <see cref="EventLimits.TitleMaxLength"/>.</summary>
    public const string EventTitleTooLong = "event.title_too_long";

    /// <summary>The description is longer than <see cref="EventLimits.DescriptionMaxLength"/>.</summary>
    public const string EventDescriptionTooLong = "event.description_too_long";

    /// <summary>The start, the end, or both are missing.</summary>
    public const string EventDatesRequired = "event.dates_required";

    /// <summary>The end is not later than the start.</summary>
    public const string EventEndNotAfterStart = "event.end_not_after_start";

    /// <summary>No event carries the requested identifier.</summary>
    public const string EventNotFound = "event.not_found";

    /// <summary>The query window is missing one of its two ends.</summary>
    public const string RangeRequired = "range.required";

    /// <summary>The query window ends before it starts.</summary>
    public const string RangeEndNotAfterStart = "range.end_not_after_start";

    /// <summary>Something failed on the server that the endpoints did not foresee.</summary>
    public const string ServerUnexpected = "server.unexpected";
}
