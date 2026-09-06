using System;
using System.Collections.Generic;
using System.Globalization;
using Calendar.Client.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Calendar.Client.ViewModels;

/// <summary>
/// Drives the month view: owns the month currently on screen, rebuilds its day cells whenever
/// that changes, and exposes the commands that move through the calendar.
/// </summary>
public partial class CalendarViewModel : ViewModelBase
{
    /// <summary>The day the week starts on, i.e. the leftmost column of the grid.</summary>
    private const DayOfWeek WeekStart = DayOfWeek.Sunday;

    /// <summary>Earliest year that can be displayed.</summary>
    public const int MinYear = 1900;

    /// <summary>Latest year that can be displayed.</summary>
    public const int MaxYear = 2100;

    private static readonly DateOnly MinMonth = new(MinYear, 1, 1);
    private static readonly DateOnly MaxMonth = new(MaxYear, 12, 1);

    private readonly DateOnly _today;

    /// <summary>
    /// First day of the month on screen. Every other piece of displayed state derives from it.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MonthTitle))]
    [NotifyCanExecuteChangedFor(
        nameof(PreviousMonthCommand),
        nameof(NextMonthCommand),
        nameof(PreviousYearCommand),
        nameof(NextYearCommand))]
    private DateOnly _displayedMonth;

    /// <summary>The cells rendered by the grid, always <see cref="MonthGrid.TotalCells"/>.</summary>
    [ObservableProperty]
    private IReadOnlyList<CalendarDay> _days = Array.Empty<CalendarDay>();

    /// <summary>
    /// Date chosen in the "jump to date" picker. Setting it navigates to that date's month; it
    /// is an input only, so moving with the arrows does not write back to it.
    /// </summary>
    [ObservableProperty]
    private DateTimeOffset? _selectedDate;

    /// <summary>Creates the view model positioned on the current month.</summary>
    public CalendarViewModel()
        : this(DateOnly.FromDateTime(DateTime.Today))
    {
    }

    /// <summary>
    /// Creates the view model positioned on the month containing <paramref name="today"/>.
    /// </summary>
    /// <param name="today">
    /// The date treated as "today". Injected rather than read from the clock so the view model
    /// can be tested against fixed dates.
    /// </param>
    public CalendarViewModel(DateOnly today)
    {
        _today = today;
        DisplayedMonth = FirstDayOf(today);
    }

    /// <summary>Abbreviated weekday names for the header row, ordered from <see cref="WeekStart"/>.</summary>
    public IReadOnlyList<string> WeekdayNames { get; } = BuildWeekdayNames();

    /// <summary>The displayed month and year, e.g. "September 2026", in the current culture.</summary>
    public string MonthTitle
    {
        get
        {
            var culture = CultureInfo.CurrentCulture;
            return culture.TextInfo.ToTitleCase(DisplayedMonth.ToString("MMMM yyyy", culture));
        }
    }

    /// <summary>Moves one month back.</summary>
    [RelayCommand(CanExecute = nameof(CanGoToPreviousMonth))]
    private void PreviousMonth() => MoveTo(DisplayedMonth.AddMonths(-1));

    /// <summary>Moves one month forward.</summary>
    [RelayCommand(CanExecute = nameof(CanGoToNextMonth))]
    private void NextMonth() => MoveTo(DisplayedMonth.AddMonths(1));

    /// <summary>Moves one year back, keeping the same month.</summary>
    [RelayCommand(CanExecute = nameof(CanGoToPreviousYear))]
    private void PreviousYear() => MoveTo(DisplayedMonth.AddYears(-1));

    /// <summary>Moves one year forward, keeping the same month.</summary>
    [RelayCommand(CanExecute = nameof(CanGoToNextYear))]
    private void NextYear() => MoveTo(DisplayedMonth.AddYears(1));

    /// <summary>Returns to the month containing today.</summary>
    [RelayCommand]
    private void GoToToday() => MoveTo(_today);

    private bool CanGoToPreviousMonth() => DisplayedMonth > MinMonth;

    private bool CanGoToNextMonth() => DisplayedMonth < MaxMonth;

    private bool CanGoToPreviousYear() => DisplayedMonth.Year > MinYear;

    private bool CanGoToNextYear() => DisplayedMonth.Year < MaxYear;

    /// <summary>Rebuilds the grid whenever the displayed month changes.</summary>
    partial void OnDisplayedMonthChanged(DateOnly value)
    {
        Days = MonthGrid.Build(value.Year, value.Month, _today, WeekStart);
    }

    /// <summary>Navigates to the month of the date picked in the "jump to date" control.</summary>
    partial void OnSelectedDateChanged(DateTimeOffset? value)
    {
        if (value is null)
        {
            return;
        }

        MoveTo(DateOnly.FromDateTime(value.Value.Date));
    }

    /// <summary>
    /// Displays the month containing <paramref name="date"/>, ignoring dates outside the
    /// supported range so navigation can never leave it.
    /// </summary>
    /// <param name="date">Any date within the target month.</param>
    private void MoveTo(DateOnly date)
    {
        var month = FirstDayOf(date);
        if (month < MinMonth || month > MaxMonth)
        {
            return;
        }

        DisplayedMonth = month;
    }

    /// <summary>Normalizes a date to the first day of its month.</summary>
    /// <param name="date">The date to normalize.</param>
    /// <returns>Day 1 of the same month and year.</returns>
    private static DateOnly FirstDayOf(DateOnly date) => new(date.Year, date.Month, 1);

    /// <summary>
    /// Builds the weekday header labels in display order, rotating the culture's names so the
    /// first one is <see cref="WeekStart"/>.
    /// </summary>
    /// <returns>Seven abbreviated day names, left column first.</returns>
    private static IReadOnlyList<string> BuildWeekdayNames()
    {
        var cultureNames = CultureInfo.CurrentCulture.DateTimeFormat.AbbreviatedDayNames;
        var ordered = new List<string>(MonthGrid.DaysPerWeek);

        for (var column = 0; column < MonthGrid.DaysPerWeek; column++)
        {
            var index = ((int)WeekStart + column) % MonthGrid.DaysPerWeek;
            ordered.Add(cultureNames[index]);
        }

        return ordered;
    }
}
