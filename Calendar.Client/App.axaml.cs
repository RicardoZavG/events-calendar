using System;
using System.Net.Http;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Calendar.Client.Services;
using Calendar.Client.Settings;
using Calendar.Client.Themes;
using Calendar.Client.ViewModels;
using Calendar.Client.Views;

namespace Calendar.Client;

public partial class App : Application
{
    /// <summary>
    /// How long to wait for the server before giving up on one attempt.
    /// </summary>
    /// <remarks>
    /// Far shorter than the default, because on a LAN the usual failure is a machine that is
    /// switched off and the default leaves the calendar looking frozen for the best part of a
    /// minute before admitting it. Not as short as it could be, either: the first request of a
    /// session pays for the connection and for the server compiling its first query, and five
    /// seconds proved too tight for it in practice.
    /// </remarks>
    private static readonly TimeSpan ServerTimeout = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Loads the XAML declared in <c>App.axaml</c> (themes, styles and application resources).
    /// </summary>
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    /// <summary>
    /// Runs once Avalonia has finished initializing. On desktop platforms it restores the saved
    /// theme, builds the sections and binds the main window to the shell.
    /// </summary>
    /// <remarks>
    /// The theme is applied before the window is created, so the first frame is already painted
    /// in the user's colours instead of flashing the default palette.
    /// </remarks>
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var store = new SettingsStore();
            ApplyTheme(AppThemes.FromId(store.Load().ThemeId).Variant);

            var settings = new SettingsViewModel(store, ApplyTheme);

            // The address is read from the settings on every call rather than captured here,
            // so changing it takes effect without restarting.
            var http = new HttpClient { Timeout = ServerTimeout };
            var events = new EventApiClient(http, () => settings.ServerAddress);

            desktop.MainWindow = new MainWindow
            {
                DataContext = new ShellViewModel(settings, new CalendarViewModel(events)),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>Switches the whole application to a colour theme.</summary>
    /// <param name="variant">The variant that selects a palette in the theme dictionaries.</param>
    private void ApplyTheme(ThemeVariant variant)
    {
        RequestedThemeVariant = variant;
    }
}
