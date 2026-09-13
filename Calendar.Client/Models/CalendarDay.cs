using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Calendar.Client.Resources;

namespace Calendar.Client.Models;

/// <summary>
/// A single cell of the month grid. Carries the date it represents plus the flags the view
/// needs to style it, so the view never has to compute dates itself.
/// </summary>
public sealed record CalendarDay
{
    /// <summary>How many events fit in a cell before the rest are summarized.</summary>
    /// <remarks>
    /// Bounded by the space the cell reserves. Showing them all would make rows of different
    /// heights and a grid that jumps as the month changes.
    /// </remarks>
    public const int MaxVisibleEvents = 3;

    /// <summary>The date this cell stands for.</summary>
    public required DateOnly Date { get; init; }

    /// <summary>
    /// <c>false</c> for the leading and trailing days borrowed from the adjacent months to
    /// fill the grid, which are rendered dimmed.
    /// </summary>
    public required bool IsInCurrentMonth { get; init; }

    /// <summary><c>true</c> when this cell is the current date.</summary>
    public required bool IsToday { get; init; }

    /// <summary>
    /// The events covering this day, ordered by start. Empty rather than <c>null</c> when there
    /// are none, and empty as built: the grid is produced before the events are known, and
    /// filled in once they arrive.
    /// </summary>
    public IReadOnlyList<EventChip> Events { get; init; } = [];

    /// <summary>Day of the month, the number shown in the cell.</summary>
    public int DayNumber => Date.Day;

    /// <summary>The events the cell actually draws.</summary>
    public IReadOnlyList<EventChip> VisibleEvents =>
        Events.Count <= MaxVisibleEvents ? Events : Events.Take(MaxVisibleEvents).ToList();

    /// <summary>How many events did not fit.</summary>
    public int HiddenEventCount => Math.Max(0, Events.Count - MaxVisibleEvents);

    /// <summary>Whether anything was left out of the cell.</summary>
    public bool HasHiddenEvents => HiddenEventCount > 0;

    /// <summary>The "and N more" line shown when the cell could not draw everything.</summary>
    public string HiddenEventsLabel =>
        string.Format(CultureInfo.CurrentCulture, Strings.EventsMoreFormat, HiddenEventCount);
}
