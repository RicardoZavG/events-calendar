using System;
using System.Collections.Generic;
using Avalonia.Styling;
using Calendar.Client.Resources;
using Calendar.Client.Settings;
using Calendar.Client.Themes;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Calendar.Client.ViewModels;

/// <summary>
/// Drives the settings section: the colour theme and where the server is.
/// </summary>
public partial class SettingsViewModel : ViewModelBase
{
    private readonly SettingsStore _store;
    private readonly Action<ThemeVariant> _applyTheme;

    private AppSettings _settings;

    /// <summary>Theme currently in use.</summary>
    [ObservableProperty]
    private AppTheme _selectedTheme;

    /// <summary>Address of the machine acting as the central server.</summary>
    [ObservableProperty]
    private string _serverAddress;

    /// <summary>Message shown when a preference could not be stored; <c>null</c> when all is well.</summary>
    [ObservableProperty]
    private string? _statusMessage;

    /// <summary>
    /// Creates the settings section over a preferences store.
    /// </summary>
    /// <param name="store">Where the preferences are read from and written to.</param>
    /// <param name="applyTheme">
    /// Applies a variant to the running application. Passed in rather than reached for through
    /// the application singleton, so the view model can be exercised without a UI.
    /// </param>
    public SettingsViewModel(SettingsStore store, Action<ThemeVariant> applyTheme)
    {
        _store = store;
        _applyTheme = applyTheme;
        _settings = store.Load();
        _selectedTheme = AppThemes.FromId(_settings.ThemeId);
        _serverAddress = _settings.ServerAddress;
    }

    /// <summary>The themes offered, in display order.</summary>
    public IReadOnlyList<AppTheme> Themes => AppThemes.All;

    /// <summary>Applies the chosen theme immediately and remembers it for the next start.</summary>
    partial void OnSelectedThemeChanged(AppTheme value)
    {
        _applyTheme(value.Variant);
        Persist(_settings with { ThemeId = value.Id });
    }

    /// <summary>Remembers the server address.</summary>
    /// <param name="value">The address as typed.</param>
    /// <remarks>
    /// Stored as written, without checking that anything answers there. A machine that is
    /// simply switched off is indistinguishable from a typo at this point, and refusing to
    /// save an address because the server is not running right now would be worse than useless.
    /// The calendar reports the failure when it actually tries.
    /// </remarks>
    partial void OnServerAddressChanged(string value)
    {
        Persist(_settings with { ServerAddress = value.Trim() });
    }

    /// <summary>Writes the preferences and reports when they could not be written.</summary>
    /// <param name="updated">The preferences as they now stand.</param>
    private void Persist(AppSettings updated)
    {
        _settings = updated;
        StatusMessage = _store.Save(updated) ? null : Strings.SettingsSaveFailed;
    }
}
