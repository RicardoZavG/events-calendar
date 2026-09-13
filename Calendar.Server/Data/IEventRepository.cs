using Calendar.Shared.Models;

namespace Calendar.Server.Data;

/// <summary>
/// Stores and retrieves events. The endpoints depend on this rather than on EF Core, so the
/// storage technology can change without touching them.
/// </summary>
public interface IEventRepository
{
    /// <summary>Finds every event overlapping a window of time.</summary>
    /// <param name="fromUtc">Start of the window, inclusive.</param>
    /// <param name="toUtc">End of the window, exclusive.</param>
    /// <param name="cancellationToken">Cancels the query if the caller goes away.</param>
    /// <returns>
    /// The matching events ordered by start. An event overlapping the window is included even
    /// when it begins before it or ends after it, so a month never drops a multi-day event
    /// crossing its edge.
    /// </returns>
    Task<IReadOnlyList<Event>> FindInRangeAsync(
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken);

    /// <summary>Finds one event.</summary>
    /// <param name="id">Identifier of the event.</param>
    /// <param name="cancellationToken">Cancels the query if the caller goes away.</param>
    /// <returns>The event, or <c>null</c> when no event carries that identifier.</returns>
    Task<Event?> FindByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Stores a new event.</summary>
    /// <param name="calendarEvent">The event to store, with its identifier already assigned.</param>
    /// <param name="cancellationToken">Cancels the write if the caller goes away.</param>
    /// <returns>The stored event.</returns>
    Task<Event> CreateAsync(Event calendarEvent, CancellationToken cancellationToken);

    /// <summary>Replaces a stored event with a new version of itself.</summary>
    /// <param name="calendarEvent">The event, carrying the identifier of the one to replace.</param>
    /// <param name="cancellationToken">Cancels the write if the caller goes away.</param>
    /// <returns><c>true</c> when it was stored; <c>false</c> when no such event exists.</returns>
    Task<bool> UpdateAsync(Event calendarEvent, CancellationToken cancellationToken);

    /// <summary>Removes an event.</summary>
    /// <param name="id">Identifier of the event.</param>
    /// <param name="cancellationToken">Cancels the write if the caller goes away.</param>
    /// <returns><c>true</c> when it was removed; <c>false</c> when no such event exists.</returns>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
}
