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
    [NotifyCanExecuteChangedFor(nameof(PreviousMonthCommand), nameof(NextMonthCommand))]
    private DateOnly _displayedMonth;

    /// <summary>The cells rendered by the grid, always <see cref="MonthGrid.TotalCells"/>.</summary>
    [ObservableProperty]
    private IReadOnlyList<CalendarDay> _days = Array.Empty<CalendarDay>();

    /// <summary>
    /// The date shown in the picker. Setting it navigates to that date's month, and navigating
    /// updates it, so the two always agree.
    /// </summary>
    /// <remarks>
    /// The picker is the only place the year is displayed — the title names the month alone —
    /// so it has to follow navigation rather than only feed it. It is never left empty: an
    /// empty picker renders its own placeholders in the control's language, which would leak
    /// untranslated text into the UI.
    /// </remarks>
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
        SelectedDate = ToPickerDate(today);
    }

    /// <summary>Abbreviated weekday names for the header row, ordered from <see cref="WeekStart"/>.</summary>
    public IReadOnlyList<string> WeekdayNames { get; } = BuildWeekdayNames();

    /// <summary>
    /// The displayed month, e.g. "September", in the current culture. The year is deliberately
    /// left out: the picker beside it already shows the full date.
    /// </summary>
    public string MonthTitle
    {
        get
        {
            var culture = CultureInfo.CurrentCulture;
            return culture.TextInfo.ToTitleCase(DisplayedMonth.ToString("MMMM", culture));
        }
    }

    /// <summary>Moves one month back.</summary>
    [RelayCommand(CanExecute = nameof(CanGoToPreviousMonth))]
    private void PreviousMonth() => MoveTo(DisplayedMonth.AddMonths(-1));

    /// <summary>Moves one month forward.</summary>
    [RelayCommand(CanExecute = nameof(CanGoToNextMonth))]
    private void NextMonth() => MoveTo(DisplayedMonth.AddMonths(1));

    /// <summary>Returns to today's month and puts the picker back on today.</summary>
    [RelayCommand]
    private void GoToToday()
    {
        MoveTo(_today);
        SelectedDate = ToPickerDate(_today);
    }

    private bool CanGoToPreviousMonth() => DisplayedMonth > MinMonth;

    private bool CanGoToNextMonth() => DisplayedMonth < MaxMonth;

    /// <summary>Rebuilds the grid and realigns the picker whenever the displayed month changes.</summary>
    partial void OnDisplayedMonthChanged(DateOnly value)
    {
        Days = MonthGrid.Build(value.Year, value.Month, _today, WeekStart);
        AlignPickerTo(value);
    }

    /// <summary>Navigates to the month of the date shown in the picker.</summary>
    partial void OnSelectedDateChanged(DateTimeOffset? value)
    {
        if (value is null)
        {
            return;
        }

        MoveTo(ToDate(value.Value));
    }

    /// <summary>
    /// Points the picker at <paramref name="month"/> when it is showing a different one.
    /// </summary>
    /// <param name="month">First day of the month now on screen.</param>
    /// <remarks>
    /// A picker already inside that month is left untouched, so choosing the 17th and landing
    /// on that month does not snap the selection back to the 1st.
    /// </remarks>
    private void AlignPickerTo(DateOnly month)
    {
        if (SelectedDate is { } selected && FirstDayOf(ToDate(selected)) == month)
        {
            return;
        }

        SelectedDate = ToPickerDate(month);
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
    private static DateOnly FirstDayOf(DateOnly date) => new(date.Year, date.Month, 1);

    /// <summary>Converts a picker value to a plain date, discarding time and offset.</summary>
    private static DateOnly ToDate(DateTimeOffset value) => DateOnly.FromDateTime(value.Date);

    /// <summary>Converts a date to the form the picker binds to.</summary>
    private static DateTimeOffset ToPickerDate(DateOnly date) =>
        new(date.ToDateTime(TimeOnly.MinValue));

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
