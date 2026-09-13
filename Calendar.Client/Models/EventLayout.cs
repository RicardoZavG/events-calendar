using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Calendar.Shared.Models;

namespace Calendar.Client.Models;

/// <summary>
/// Places events onto the days of the grid.
/// </summary>
/// <remarks>
/// Pure: it converts instants to a time zone handed in and produces new cells, touching no UI
/// and no clock. That is what makes an event spanning a month boundary — the case most likely
/// to be got wrong — something a test can pin down.
/// </remarks>
public static class EventLayout
{
    /// <summary>Attaches each event to every day cell it covers.</summary>
    /// <param name="cells">The grid, as produced by <see cref="MonthGrid"/>.</param>
    /// <param name="events">The events returned for the window the grid spans.</param>
    /// <param name="timeZone">
    /// The zone the days are read in. Events are stored as instants in UTC, and which day an
    /// instant falls on is a question only a time zone can answer.
    /// </param>
    /// <returns>
    /// New cells, in the same order, each carrying the events covering it ordered by start.
    /// Cells with nothing on them carry an empty list, never <c>null</c>.
    /// </returns>
    public static IReadOnlyList<CalendarDay> Distribute(
        IReadOnlyList<CalendarDay> cells,
        IReadOnlyList<Event> events,
        TimeZoneInfo timeZone)
    {
        var byDay = new Dictionary<DateOnly, List<EventChip>>();

        foreach (var item in events.OrderBy(item => item.Start))
        {
            var start = ToZone(item.Start, timeZone);
            var end = ToZone(item.End, timeZone);

            var firstDay = DateOnly.FromDateTime(start);
            var lastDay = LastCoveredDay(firstDay, end);

            for (var day = firstDay; day <= lastDay; day = day.AddDays(1))
            {
                var isContinuation = day > firstDay;

                if (!byDay.TryGetValue(day, out var chips))
                {
                    chips = [];
                    byDay[day] = chips;
                }

                chips.Add(new EventChip
                {
                    EventId = item.Id,
                    Title = item.Title,
                    TimeLabel = isContinuation ? null : FormatTime(start),
                    IsContinuation = isContinuation,
                });
            }
        }

        return cells
            .Select(cell => cell with
            {
                Events = byDay.TryGetValue(cell.Date, out var chips)
                    ? chips
                    : [],
            })
            .ToList();
    }

    /// <summary>Converts an instant to the zone the calendar is read in.</summary>
    /// <param name="value">The instant, stored in UTC.</param>
    /// <param name="timeZone">The zone to read it in.</param>
    /// <returns>The same moment as a local time in that zone.</returns>
    private static DateTime ToZone(DateTime value, TimeZoneInfo timeZone) =>
        TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.SpecifyKind(value.ToUniversalTime(), DateTimeKind.Utc),
            timeZone);

    /// <summary>The last day an event actually occupies.</summary>
    /// <param name="firstDay">The day it starts on.</param>
    /// <param name="end">Its end, already in the display zone.</param>
    /// <returns>
    /// The day of <paramref name="end"/>, except when the event ends exactly at midnight: it
    /// then finishes as that day begins and does not belong on it.
    /// </returns>
    private static DateOnly LastCoveredDay(DateOnly firstDay, DateTime end)
    {
        var lastDay = DateOnly.FromDateTime(end);

        if (lastDay > firstDay && end.TimeOfDay == TimeSpan.Zero)
        {
            return lastDay.AddDays(-1);
        }

        return lastDay;
    }

    /// <summary>Renders the start time the way the machine's culture writes it.</summary>
    /// <param name="start">The start, already in the display zone.</param>
    /// <returns>A short time, e.g. "15:00".</returns>
    private static string FormatTime(DateTime start) =>
        start.ToString("t", CultureInfo.CurrentCulture);
}
