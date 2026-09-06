using System;
using System.Collections.Generic;

namespace Calendar.Client.Models;

/// <summary>
/// Builds the day cells shown by the month view. Pure date arithmetic with no UI dependency,
/// which keeps it directly testable.
/// </summary>
public static class MonthGrid
{
    /// <summary>Columns in the grid, one per day of the week.</summary>
    public const int DaysPerWeek = 7;

    /// <summary>
    /// Rows in the grid. Fixed at six so the layout never jumps between months: six weeks is
    /// the maximum a month can span, which happens when a 31-day month starts on the last day
    /// of the week.
    /// </summary>
    public const int WeeksShown = 6;

    /// <summary>Total cells the grid always renders.</summary>
    public const int TotalCells = DaysPerWeek * WeeksShown;

    /// <summary>
    /// Builds the <see cref="TotalCells"/> cells of the grid for the given month. The grid
    /// starts on the first <paramref name="firstDayOfWeek"/> on or before day 1, so it is
    /// padded with the trailing days of the previous month and the leading days of the next
    /// one.
    /// </summary>
    /// <param name="year">Year of the month to render.</param>
    /// <param name="month">Month to render, 1 to 12.</param>
    /// <param name="today">
    /// The current date, used to flag which cell is today. Passed in rather than read from the
    /// clock so the result is deterministic.
    /// </param>
    /// <param name="firstDayOfWeek">The day the week starts on, i.e. the leftmost column.</param>
    /// <returns>
    /// Exactly <see cref="TotalCells"/> cells in calendar order: left to right, top to bottom.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// When <paramref name="year"/> or <paramref name="month"/> is not a valid date, or when
    /// the resulting grid would fall outside the range <see cref="DateOnly"/> can represent.
    /// </exception>
    public static IReadOnlyList<CalendarDay> Build(
        int year,
        int month,
        DateOnly today,
        DayOfWeek firstDayOfWeek)
    {
        var firstOfMonth = new DateOnly(year, month, 1);
        var gridStart = firstOfMonth.AddDays(-LeadingDayCount(firstOfMonth, firstDayOfWeek));

        var cells = new List<CalendarDay>(TotalCells);
        for (var offset = 0; offset < TotalCells; offset++)
        {
            var date = gridStart.AddDays(offset);
            cells.Add(new CalendarDay
            {
                Date = date,
                IsInCurrentMonth = date.Year == year && date.Month == month,
                IsToday = date == today,
            });
        }

        return cells;
    }

    /// <summary>
    /// How many days of the previous month are needed before day 1 so that the first row
    /// starts on <paramref name="firstDayOfWeek"/>.
    /// </summary>
    /// <param name="firstOfMonth">The first day of the month being rendered.</param>
    /// <param name="firstDayOfWeek">The day the week starts on.</param>
    /// <returns>A value from 0 to <see cref="DaysPerWeek"/> - 1.</returns>
    private static int LeadingDayCount(DateOnly firstOfMonth, DayOfWeek firstDayOfWeek)
    {
        return ((int)firstOfMonth.DayOfWeek - (int)firstDayOfWeek + DaysPerWeek) % DaysPerWeek;
    }
}
