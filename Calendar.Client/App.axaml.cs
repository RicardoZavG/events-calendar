using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Calendar.Client.Settings;
using Calendar.Client.Themes;
using Calendar.Client.ViewModels;
using Calendar.Client.Views;

namespace Calendar.Client;

public partial class App : Application
{
    /// <summary>
    /// Loads the XAML declared in <c>App.axaml</c> (themes, styles and application resources).
    /// </summary>
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    /// <summary>
    /// Runs once Avalonia has finished initializing. On desktop platforms it restores the saved
    /// theme, then creates the main window and binds it to the shell.
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

            desktop.MainWindow = new MainWindow
            {
                DataContext = new ShellViewModel(store, ApplyTheme),
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
