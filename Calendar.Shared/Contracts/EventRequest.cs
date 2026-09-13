namespace Calendar.Shared.Contracts;

/// <summary>
/// The payload sent when creating or updating an event.
/// </summary>
/// <remarks>
/// Creating and updating take the same fields, so they share one contract. It is deliberately
/// separate from the <see cref="Models.Event"/> model: the identifier and the creation instant
/// belong to the server and must never be settable by a client.
/// <para>
/// Every member is nullable so that a malformed or partial body deserializes instead of
/// throwing, and is then rejected by validation with a message the caller can act on.
/// </para>
/// </remarks>
public sealed record EventRequest
{
    /// <summary>Short title shown in the calendar grid.</summary>
    public string? Title { get; init; }

    /// <summary>Optional longer detail.</summary>
    public string? Description { get; init; }

    /// <summary>Moment the event starts.</summary>
    public DateTime? Start { get; init; }

    /// <summary>Moment the event ends. Must be later than <see cref="Start"/>.</summary>
    public DateTime? End { get; init; }
}
