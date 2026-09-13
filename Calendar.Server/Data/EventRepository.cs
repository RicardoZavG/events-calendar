using Calendar.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Calendar.Server.Data;

/// <summary>
/// EF Core implementation of <see cref="IEventRepository"/> over SQLite.
/// </summary>
public sealed class EventRepository(CalendarDbContext context) : IEventRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<Event>> FindInRangeAsync(
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken)
    {
        // Half-open overlap: an event counts when it starts before the window closes and ends
        // after it opens. Comparing only Start would drop anything already running.
        return await context.Events
            .AsNoTracking()
            .Where(item => item.Start < toUtc && item.End > fromUtc)
            .OrderBy(item => item.Start)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Event?> FindByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await context.Events
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Event> CreateAsync(Event calendarEvent, CancellationToken cancellationToken)
    {
        context.Events.Add(calendarEvent);
        await context.SaveChangesAsync(cancellationToken);
        return calendarEvent;
    }

    /// <inheritdoc />
    /// <remarks>
    /// The stored row is loaded and its values overwritten, rather than attaching the incoming
    /// instance. Attaching would fail whenever the context already holds an entity with the
    /// same identifier, which the repository has no way to know and no business assuming.
    /// </remarks>
    public async Task<bool> UpdateAsync(Event calendarEvent, CancellationToken cancellationToken)
    {
        var stored = await context.Events
            .FirstOrDefaultAsync(item => item.Id == calendarEvent.Id, cancellationToken);

        if (stored is null)
        {
            return false;
        }

        context.Entry(stored).CurrentValues.SetValues(calendarEvent);
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var stored = await context.Events
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (stored is null)
        {
            return false;
        }

        context.Events.Remove(stored);
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
