using Calendar.Client.Services;
using Calendar.Client.Settings;
using Calendar.Client.ViewModels;

namespace Calendar.Client.Tests.ViewModels;

/// <summary>
/// Covers the shell: which section is on screen and the state of the side panel.
/// </summary>
public sealed class ShellViewModelTests : IDisposable
{
    private readonly string _folder;
    private readonly ShellViewModel _shell;

    public ShellViewModelTests()
    {
        _folder = Path.Combine(Path.GetTempPath(), "calendar-tests-" + Guid.NewGuid().ToString("N"));
        _shell = new ShellViewModel(
            new SettingsViewModel(new SettingsStore(Path.Combine(_folder, "settings.json")), _ => { }),
            new CalendarViewModel(new OfflineEventApiClient()));
    }

    public void Dispose()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }

    [Fact]
    public void Constructor_OpensOnTheCalendar()
    {
        // Assert
        Assert.Same(_shell.Calendar, _shell.CurrentSection);
        Assert.True(_shell.IsCalendarSelected);
        Assert.False(_shell.IsSettingsSelected);
    }

    [Fact]
    public void Constructor_LeavesThePanelCollapsed()
    {
        // Assert: the calendar is the point of the window, so it starts unobstructed.
        Assert.False(_shell.IsPaneOpen);
    }

    [Fact]
    public void ShowSettings_SwitchesTheSection()
    {
        // Act
        _shell.ShowSettingsCommand.Execute(null);

        // Assert
        Assert.Same(_shell.Settings, _shell.CurrentSection);
        Assert.True(_shell.IsSettingsSelected);
        Assert.False(_shell.IsCalendarSelected);
    }

    [Fact]
    public void ShowCalendar_SwitchesBack()
    {
        // Arrange
        _shell.ShowSettingsCommand.Execute(null);

        // Act
        _shell.ShowCalendarCommand.Execute(null);

        // Assert
        Assert.Same(_shell.Calendar, _shell.CurrentSection);
        Assert.True(_shell.IsCalendarSelected);
    }

    /// <summary>
    /// The sections are kept alive rather than rebuilt, so returning to the calendar shows the
    /// month the user had navigated to instead of resetting to today.
    /// </summary>
    [Fact]
    public void Sections_KeepTheirState_WhenSwitchingAway()
    {
        // Arrange
        _shell.Calendar.NextMonthCommand.Execute(null);
        var displayed = _shell.Calendar.DisplayedMonth;

        // Act
        _shell.ShowSettingsCommand.Execute(null);
        _shell.ShowCalendarCommand.Execute(null);

        // Assert
        Assert.Equal(displayed, _shell.Calendar.DisplayedMonth);
    }

    [Fact]
    public void TogglePane_OpensAndClosesThePanel()
    {
        // Act & Assert
        _shell.TogglePaneCommand.Execute(null);
        Assert.True(_shell.IsPaneOpen);

        _shell.TogglePaneCommand.Execute(null);
        Assert.False(_shell.IsPaneOpen);
    }
}
