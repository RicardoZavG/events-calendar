using System;
using System.Globalization;
using Calendar.Shared.Models;

namespace Calendar.Client.Models;

/// <summary>
/// One event as it appears in the day panel: the record itself plus the times already written
/// out for reading.
/// </summary>
public sealed record DayEvent
{
    /// <summary>The event behind the row, needed to edit or delete it.</summary>
    public required Event Source { get; init; }

    /// <summary>When it runs, written for the panel.</summary>
    public required string TimeRange { get; init; }

    /// <summary>The event's title.</summary>
    public string Title => Source.Title;

    /// <summary>The event's description, if it has one.</summary>
    public string? Description => Source.Description;

    /// <summary>Whether there is a description worth a line of its own.</summary>
    public bool HasDescription => !string.IsNullOrWhiteSpace(Description);

    /// <summary>Builds the row for an event.</summary>
    /// <param name="source">The stored event, with its instants in UTC.</param>
    /// <param name="timeZone">The zone the times are read in.</param>
    /// <returns>The row, with its time range already formatted.</returns>
    public static DayEvent From(Event source, TimeZoneInfo timeZone)
    {
        var start = ToLocal(source.Start, timeZone);
        var end = ToLocal(source.End, timeZone);

        return new DayEvent
        {
            Source = source,
            TimeRange = FormatRange(start, end),
        };
    }

    /// <summary>Writes a range of time the shortest way that stays unambiguous.</summary>
    /// <param name="start">When it starts, in the display zone.</param>
    /// <param name="end">When it ends, in the display zone.</param>
    /// <returns>
    /// Two times when the event begins and ends on the same day, and dates alongside them when
    /// it does not — otherwise an event running into the next day would read as ending before
    /// it started.
    /// </returns>
    private static string FormatRange(DateTime start, DateTime end)
    {
        var culture = CultureInfo.CurrentCulture;

        if (start.Date == end.Date)
        {
            return $"{start.ToString("t", culture)} – {end.ToString("t", culture)}";
        }

        return $"{start.ToString("d MMM, t", culture)} – {end.ToString("d MMM, t", culture)}";
    }

    /// <summary>Converts a stored instant to the zone the panel displays.</summary>
    /// <param name="value">The instant, stored in UTC.</param>
    /// <param name="timeZone">The zone to read it in.</param>
    /// <returns>The same moment as a local time.</returns>
    private static DateTime ToLocal(DateTime value, TimeZoneInfo timeZone) =>
        TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.SpecifyKind(value.ToUniversalTime(), DateTimeKind.Utc),
            timeZone);
}
