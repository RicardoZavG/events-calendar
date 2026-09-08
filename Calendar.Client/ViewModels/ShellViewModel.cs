using System;
using Avalonia.Styling;
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

    /// <summary>Creates the shell over the user's own settings file.</summary>
    /// <remarks>Used by the XAML previewer, which cannot supply constructor arguments.</remarks>
    public ShellViewModel()
        : this(new SettingsStore(), _ => { })
    {
    }

    /// <summary>Creates the shell and the sections it hosts.</summary>
    /// <param name="store">Where preferences are read from and written to.</param>
    /// <param name="applyTheme">Applies a theme variant to the running application.</param>
    public ShellViewModel(SettingsStore store, Action<ThemeVariant> applyTheme)
    {
        Calendar = new CalendarViewModel();
        Settings = new SettingsViewModel(store, applyTheme);
        _currentSection = Calendar;
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
