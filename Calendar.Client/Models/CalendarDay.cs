using System;

namespace Calendar.Client.Models;

/// <summary>
/// A single cell of the month grid. Carries the date it represents plus the flags the view
/// needs to style it, so the view never has to compute dates itself.
/// </summary>
public sealed record CalendarDay
{
    /// <summary>The date this cell stands for.</summary>
    public required DateOnly Date { get; init; }

    /// <summary>
    /// <c>false</c> for the leading and trailing days borrowed from the adjacent months to
    /// fill the grid, which are rendered dimmed.
    /// </summary>
    public required bool IsInCurrentMonth { get; init; }

    /// <summary><c>true</c> when this cell is the current date.</summary>
    public required bool IsToday { get; init; }

    /// <summary>Day of the month, the number shown in the cell.</summary>
    public int DayNumber => Date.Day;
}
