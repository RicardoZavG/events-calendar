using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Calendar.Client.Models;
using Calendar.Client.Resources;
using Calendar.Client.Services;
using Calendar.Shared.Contracts;
using Calendar.Shared.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Calendar.Client.ViewModels;

/// <summary>
/// The panel for one day: what is on it, and the form that adds to it or changes it.
/// </summary>
/// <remarks>
/// The cell in the grid has room for a few truncated titles; this is where a day is actually
/// read and edited. It owns no events of its own — it is handed the ones the calendar already
/// fetched, and asks the calendar to refresh after anything it changes.
/// </remarks>
public partial class DayPanelViewModel : ViewModelBase
{
    /// <summary>How long a new event lasts unless the user says otherwise.</summary>
    private static readonly TimeSpan DefaultDuration = TimeSpan.FromHours(1);

    private readonly IEventApiClient _api;
    private readonly TimeZoneInfo _timeZone;
    private readonly Func<Task> _refresh;
    private readonly Action _close;

    /// <summary>The events on this day, ordered by start.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEmpty))]
    private IReadOnlyList<DayEvent> _events = [];

    /// <summary>Whether the form is showing instead of the list.</summary>
    [ObservableProperty]
    private bool _isEditing;

    /// <summary>The event the user has asked to delete, pending confirmation.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsConfirmingDelete))]
    private DayEvent? _pendingDelete;

    /// <summary>Whether a call to the server is in flight.</summary>
    [ObservableProperty]
    private bool _isBusy;

    /// <summary>Why the last attempt failed; <c>null</c> when nothing has.</summary>
    [ObservableProperty]
    private string? _errorMessage;

    /// <summary>Title being typed.</summary>
    [ObservableProperty]
    private string _draftTitle = string.Empty;

    /// <summary>Description being typed.</summary>
    [ObservableProperty]
    private string _draftDescription = string.Empty;

    /// <summary>Day the event starts on.</summary>
    [ObservableProperty]
    private DateTimeOffset? _draftStartDate;

    /// <summary>Time the event starts at.</summary>
    [ObservableProperty]
    private TimeSpan? _draftStartTime;

    /// <summary>Day the event ends on.</summary>
    [ObservableProperty]
    private DateTimeOffset? _draftEndDate;

    /// <summary>Time the event ends at.</summary>
    [ObservableProperty]
    private TimeSpan? _draftEndTime;

    private Guid? _editingId;

    /// <summary>Creates the panel for a day.</summary>
    /// <param name="date">The day being shown.</param>
    /// <param name="api">Where events are created, changed and removed.</param>
    /// <param name="timeZone">The zone the times on screen are read in.</param>
    /// <param name="refresh">
    /// Reloads the month. Called after every change so the grid behind never disagrees with
    /// this panel.
    /// </param>
    /// <param name="close">Dismisses the panel, leaving the grid on its own.</param>
    public DayPanelViewModel(
        DateOnly date,
        IEventApiClient api,
        TimeZoneInfo timeZone,
        Func<Task> refresh,
        Action close)
    {
        Date = date;
        _api = api;
        _timeZone = timeZone;
        _refresh = refresh;
        _close = close;
    }

    /// <summary>The day this panel is showing.</summary>
    public DateOnly Date { get; }

    /// <summary>The day written out, e.g. "martes, 8 de septiembre de 2026".</summary>
    public string Title => Date.ToDateTime(TimeOnly.MinValue)
        .ToString("D", CultureInfo.CurrentCulture);

    /// <summary>Whether there is nothing on this day.</summary>
    public bool IsEmpty => Events.Count == 0;

    /// <summary>Whether the panel is asking the user to confirm a deletion.</summary>
    public bool IsConfirmingDelete => PendingDelete is not null;

    /// <summary>Whether the form is creating rather than changing an existing event.</summary>
    public bool IsCreating => _editingId is null;

    /// <summary>Hands the panel the events the calendar fetched for this day.</summary>
    /// <param name="events">Every event overlapping this day.</param>
    public void Show(IReadOnlyList<Event> events)
    {
        Events = events
            .OrderBy(item => item.Start)
            .Select(item => DayEvent.From(item, _timeZone))
            .ToList();
    }

    /// <summary>Dismisses the panel.</summary>
    [RelayCommand]
    private void Close() => _close();

    /// <summary>Opens an empty form for a new event on this day.</summary>
    [RelayCommand]
    private void New()
    {
        // The next whole hour, an hour long: the common case is then typing a title and saving.
        var start = NextWholeHour();

        _editingId = null;
        DraftTitle = string.Empty;
        DraftDescription = string.Empty;
        SetDraftRange(start, start + DefaultDuration);

        ErrorMessage = null;
        PendingDelete = null;
        IsEditing = true;
        OnPropertyChanged(nameof(IsCreating));
    }

    /// <summary>Opens the form on an existing event.</summary>
    /// <param name="item">The event to change.</param>
    [RelayCommand]
    private void Edit(DayEvent item)
    {
        _editingId = item.Source.Id;
        DraftTitle = item.Source.Title;
        DraftDescription = item.Source.Description ?? string.Empty;
        SetDraftRange(ToLocal(item.Source.Start), ToLocal(item.Source.End));

        ErrorMessage = null;
        PendingDelete = null;
        IsEditing = true;
        OnPropertyChanged(nameof(IsCreating));
    }

    /// <summary>Closes the form without sending anything.</summary>
    [RelayCommand]
    private void Cancel()
    {
        IsEditing = false;
        ErrorMessage = null;
    }

    /// <summary>Sends the form, creating or replacing as appropriate.</summary>
    /// <returns>A task that completes once the server has answered and the month reloaded.</returns>
    [RelayCommand]
    public async Task SaveAsync()
    {
        var request = BuildRequest();

        // Checked here as well as on the server: a round trip to be told the title is empty is
        // a round trip wasted, and the rules are the same object on both sides.
        var invalid = EventValidation.Validate(request);

        if (invalid is not null)
        {
            ErrorMessage = ErrorMessages.For(invalid);
            return;
        }

        IsBusy = true;

        try
        {
            var result = _editingId is { } id
                ? await _api.UpdateEventAsync(id, request, CancellationToken.None)
                : await _api.CreateEventAsync(request, CancellationToken.None);

            if (!result.Succeeded)
            {
                ErrorMessage = ErrorMessages.For(result.ErrorCode);
                return;
            }

            IsEditing = false;
            ErrorMessage = null;
            await _refresh();
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>Asks the user to confirm removing an event.</summary>
    /// <param name="item">The event to remove.</param>
    [RelayCommand]
    private void AskDelete(DayEvent item)
    {
        // Deleting is asked about rather than done: there is no undo, and the rows are small.
        PendingDelete = item;
        ErrorMessage = null;
    }

    /// <summary>Abandons a pending deletion.</summary>
    [RelayCommand]
    private void CancelDelete()
    {
        PendingDelete = null;
    }

    /// <summary>Removes the event the user confirmed.</summary>
    /// <returns>A task that completes once the server has answered and the month reloaded.</returns>
    [RelayCommand]
    public async Task ConfirmDeleteAsync()
    {
        if (PendingDelete is not { } item)
        {
            return;
        }

        IsBusy = true;

        try
        {
            var result = await _api.DeleteEventAsync(item.Source.Id, CancellationToken.None);

            // An event someone else already removed is gone, which is what was wanted. Telling
            // the user it failed would be true and useless.
            if (!result.Succeeded && result.ErrorCode != ApiErrorCodes.EventNotFound)
            {
                ErrorMessage = ErrorMessages.For(result.ErrorCode);
                return;
            }

            PendingDelete = null;
            ErrorMessage = null;
            await _refresh();
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>Builds the payload from what is in the form.</summary>
    /// <returns>The request, with its instants converted to UTC.</returns>
    private EventRequest BuildRequest() => new()
    {
        Title = DraftTitle,
        Description = string.IsNullOrWhiteSpace(DraftDescription) ? null : DraftDescription,
        Start = Combine(DraftStartDate, DraftStartTime),
        End = Combine(DraftEndDate, DraftEndTime),
    };

    /// <summary>Fills the four form fields from a local start and end.</summary>
    /// <param name="start">When the event starts, in the display zone.</param>
    /// <param name="end">When it ends, in the display zone.</param>
    private void SetDraftRange(DateTime start, DateTime end)
    {
        DraftStartDate = new DateTimeOffset(start.Date);
        DraftStartTime = start.TimeOfDay;
        DraftEndDate = new DateTimeOffset(end.Date);
        DraftEndTime = end.TimeOfDay;
    }

    /// <summary>The next whole hour on this day.</summary>
    /// <returns>
    /// Today's next hour when the panel is showing today, and nine in the morning on any other
    /// day — an hour picked from the day being looked at, not from the clock.
    /// </returns>
    private DateTime NextWholeHour()
    {
        var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _timeZone);

        if (DateOnly.FromDateTime(now) != Date)
        {
            return Date.ToDateTime(new TimeOnly(9, 0));
        }

        return Date.ToDateTime(new TimeOnly(now.Hour, 0)).AddHours(1);
    }

    /// <summary>Joins a date and a time from the form into one instant in UTC.</summary>
    /// <param name="date">The day part, as the picker holds it.</param>
    /// <param name="time">The time part, as the picker holds it.</param>
    /// <returns><c>null</c> when either half is missing, which validation then rejects.</returns>
    private DateTime? Combine(DateTimeOffset? date, TimeSpan? time)
    {
        if (date is null || time is null)
        {
            return null;
        }

        var local = DateTime.SpecifyKind(date.Value.Date + time.Value, DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(local, _timeZone);
    }

    /// <summary>Converts a stored instant to the zone the panel displays.</summary>
    /// <param name="value">The instant, stored in UTC.</param>
    /// <returns>The same moment as a local time.</returns>
    private DateTime ToLocal(DateTime value) =>
        TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.SpecifyKind(value.ToUniversalTime(), DateTimeKind.Utc),
            _timeZone);
}
