using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Calendar.Client.Models;
using Calendar.Client.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Calendar.Client.ViewModels;

/// <summary>
/// Drives the month view: owns the month currently on screen, rebuilds its day cells whenever
/// that changes, and asks the server for the events that fall in it.
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
    private readonly IEventApiClient _events;
    private readonly TimeZoneInfo _timeZone;

    private CancellationTokenSource? _loading;

    /// <summary>
    /// First day of the month on screen. Every other piece of displayed state derives from it.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MonthTitle))]
    [NotifyCanExecuteChangedFor(nameof(PreviousMonthCommand), nameof(NextMonthCommand))]
    private DateOnly _displayedMonth;

    /// <summary>The cells rendered by the grid, always <see cref="MonthGrid.TotalCells"/>.</summary>
    [ObservableProperty]
    private IReadOnlyList<CalendarDay> _days = [];

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

    /// <summary>Whether the events of the displayed month are being fetched.</summary>
    [ObservableProperty]
    private bool _isLoading;

    /// <summary>
    /// Why the events could not be shown; <c>null</c> when they could. Shown as a banner over
    /// the grid, which stays usable either way.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasProblem))]
    private string? _problemMessage;

    /// <summary>Creates a calendar with no server behind it.</summary>
    /// <remarks>Used by the XAML previewer, which cannot supply constructor arguments.</remarks>
    public CalendarViewModel()
        : this(new OfflineEventApiClient())
    {
    }

    /// <summary>Creates the view model positioned on the current month.</summary>
    /// <param name="events">Where the events are fetched from.</param>
    public CalendarViewModel(IEventApiClient events)
        : this(events, DateOnly.FromDateTime(DateTime.Today), TimeZoneInfo.Local)
    {
    }

    /// <summary>
    /// Creates the view model positioned on the month containing <paramref name="today"/>.
    /// </summary>
    /// <param name="events">Where the events are fetched from.</param>
    /// <param name="today">
    /// The date treated as "today". Injected rather than read from the clock so the view model
    /// can be tested against fixed dates.
    /// </param>
    /// <param name="timeZone">
    /// The zone the grid is read in. Events are stored as instants; which day one falls on is a
    /// question only a zone can answer, and a test needs to fix it to get a stable answer.
    /// </param>
    public CalendarViewModel(IEventApiClient events, DateOnly today, TimeZoneInfo timeZone)
    {
        _events = events;
        _today = today;
        _timeZone = timeZone;
        DisplayedMonth = FirstDayOf(today);
        SelectedDate = ToPickerDate(today);
    }

    /// <summary>Abbreviated weekday names for the header row, ordered from <see cref="WeekStart"/>.</summary>
    public IReadOnlyList<string> WeekdayNames { get; } = BuildWeekdayNames();

    /// <summary>Whether there is something to tell the user about the connection.</summary>
    public bool HasProblem => ProblemMessage is not null;

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

    /// <summary>Fetches the events of the month on screen.</summary>
    /// <returns>A task that completes once the grid reflects the answer, or the failure.</returns>
    /// <remarks>
    /// Called whenever the month changes and available as a command so the view can offer a
    /// retry after a failure. A fetch already in flight is cancelled first: navigating quickly
    /// through months must not leave an older answer overwriting a newer one.
    /// </remarks>
    [RelayCommand]
    public async Task ReloadAsync()
    {
        if (Days.Count == 0)
        {
            return;
        }

        var previous = _loading;
        var current = new CancellationTokenSource();
        _loading = current;

        if (previous is not null)
        {
            await previous.CancelAsync();
            previous.Dispose();
        }

        // The window is the whole grid, not the calendar month: the six-week grid shows days of
        // the neighbouring months, and an event on 31 August belongs in September's view.
        var from = ToUtc(Days[0].Date);
        var to = ToUtc(Days[^1].Date.AddDays(1));

        IsLoading = true;

        try
        {
            var result = await _events.GetEventsAsync(from, to, current.Token);

            if (current.IsCancellationRequested)
            {
                return;
            }

            if (result.Succeeded)
            {
                Days = EventLayout.Distribute(Days, result.Data!, _timeZone);
                ProblemMessage = null;
                return;
            }

            ProblemMessage = ErrorMessages.For(result.ErrorCode);
        }
        finally
        {
            if (ReferenceEquals(_loading, current))
            {
                IsLoading = false;
            }
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

    /// <summary>Rebuilds the grid, realigns the picker and fetches the month's events.</summary>
    partial void OnDisplayedMonthChanged(DateOnly value)
    {
        Days = MonthGrid.Build(value.Year, value.Month, _today, WeekStart);
        AlignPickerTo(value);

        // Deliberately not awaited: navigation stays responsive and the grid is already on
        // screen. Failures are reported through ProblemMessage, never thrown.
        _ = ReloadAsync();
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

    /// <summary>The instant a local day begins, expressed in UTC.</summary>
    /// <param name="date">A day as shown in the grid.</param>
    /// <returns>Midnight of that day in the display zone, converted to UTC.</returns>
    private DateTime ToUtc(DateOnly date) =>
        TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(date.ToDateTime(TimeOnly.MinValue), DateTimeKind.Unspecified),
            _timeZone);

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
