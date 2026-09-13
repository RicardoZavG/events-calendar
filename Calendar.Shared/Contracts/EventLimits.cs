namespace Calendar.Shared.Contracts;

/// <summary>
/// The bounds an event has to respect.
/// </summary>
/// <remarks>
/// Part of the contract rather than a server detail: a client that knows them can say how long
/// a title may be before the request is sent, and can word a rejection with the actual number
/// instead of a vague "too long".
/// </remarks>
public static class EventLimits
{
    /// <summary>Longest title accepted.</summary>
    public const int TitleMaxLength = 200;

    /// <summary>Longest description accepted.</summary>
    public const int DescriptionMaxLength = 2000;
}
