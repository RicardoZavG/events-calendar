using System;
using System.Collections.Generic;
using Avalonia.Styling;
using Calendar.Client.Resources;
using Calendar.Client.Settings;
using Calendar.Client.Themes;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Calendar.Client.ViewModels;

/// <summary>
/// Drives the settings section: currently the choice of colour theme.
/// </summary>
public partial class SettingsViewModel : ViewModelBase
{
    private readonly SettingsStore _store;
    private readonly Action<ThemeVariant> _applyTheme;

    /// <summary>Theme currently in use.</summary>
    [ObservableProperty]
    private AppTheme _selectedTheme;

    /// <summary>Message shown when a preference could not be stored; <c>null</c> when all is well.</summary>
    [ObservableProperty]
    private string? _statusMessage;

    /// <summary>
    /// Creates the settings section over a preferences store.
    /// </summary>
    /// <param name="store">Where the chosen theme is read from and written to.</param>
    /// <param name="applyTheme">
    /// Applies a variant to the running application. Passed in rather than reached for through
    /// the application singleton, so the view model can be exercised without a UI.
    /// </param>
    public SettingsViewModel(SettingsStore store, Action<ThemeVariant> applyTheme)
    {
        _store = store;
        _applyTheme = applyTheme;
        _selectedTheme = AppThemes.FromId(store.Load().ThemeId);
    }

    /// <summary>The themes offered, in display order.</summary>
    public IReadOnlyList<AppTheme> Themes => AppThemes.All;

    /// <summary>Applies the chosen theme immediately and remembers it for the next start.</summary>
    partial void OnSelectedThemeChanged(AppTheme value)
    {
        _applyTheme(value.Variant);

        var stored = _store.Save(new AppSettings { ThemeId = value.Id });
        StatusMessage = stored ? null : Strings.SettingsSaveFailed;
    }
}
