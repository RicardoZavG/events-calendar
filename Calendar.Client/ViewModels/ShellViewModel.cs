using Calendar.Client.Services;
using Calendar.Client.Settings;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Calendar.Client.ViewModels;

/// <summary>
/// The application shell: owns the side panel and decides which section fills the window.
/// </summary>
public partial class ShellViewModel : ViewModelBase
{
    /// <summary>Whether the side panel is expanded; collapsed it shows only the icons.</summary>
    [ObservableProperty]
    private bool _isPaneOpen;

    /// <summary>The section currently on screen.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCalendarSelected))]
    [NotifyPropertyChangedFor(nameof(IsSettingsSelected))]
    private ViewModelBase _currentSection;

    /// <summary>Creates a shell wired to nothing in particular.</summary>
    /// <remarks>Used by the XAML previewer, which cannot supply constructor arguments.</remarks>
    public ShellViewModel()
        : this(
            new SettingsViewModel(new SettingsStore(), _ => { }),
            new CalendarViewModel(new OfflineEventApiClient()))
    {
    }

    /// <summary>Creates the shell over the sections it hosts.</summary>
    /// <param name="settings">The settings section.</param>
    /// <param name="calendar">The calendar section.</param>
    public ShellViewModel(SettingsViewModel settings, CalendarViewModel calendar)
    {
        Settings = settings;
        Calendar = calendar;
        _currentSection = calendar;
    }

    /// <summary>The calendar section.</summary>
    public CalendarViewModel Calendar { get; }

    /// <summary>The settings section.</summary>
    public SettingsViewModel Settings { get; }

    /// <summary>Whether the calendar is the section on screen, used to mark it in the panel.</summary>
    public bool IsCalendarSelected => ReferenceEquals(CurrentSection, Calendar);

    /// <summary>Whether settings is the section on screen.</summary>
    public bool IsSettingsSelected => ReferenceEquals(CurrentSection, Settings);

    /// <summary>Expands or collapses the side panel.</summary>
    [RelayCommand]
    private void TogglePane() => IsPaneOpen = !IsPaneOpen;

    /// <summary>Shows the calendar.</summary>
    [RelayCommand]
    private void ShowCalendar() => CurrentSection = Calendar;

    /// <summary>Shows the settings.</summary>
    [RelayCommand]
    private void ShowSettings() => CurrentSection = Settings;
}
