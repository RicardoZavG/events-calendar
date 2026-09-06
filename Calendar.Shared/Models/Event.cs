namespace Calendar.Shared.Models;

/// <summary>
/// A calendar event. Shared contract between the server and every LAN client.
/// </summary>
public sealed record Event
{
    /// <summary>Unique identifier of the event.</summary>
    public required Guid Id { get; init; }

    /// <summary>Short title shown in the calendar grid.</summary>
    public required string Title { get; init; }

    /// <summary>Optional longer detail.</summary>
    public string? Description { get; init; }

    /// <summary>Moment the event starts.</summary>
    public required DateTime Start { get; init; }

    /// <summary>Moment the event ends. Must not be earlier than <see cref="Start"/>.</summary>
    public required DateTime End { get; init; }

    /// <summary>Moment the event was first created, in UTC.</summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
