using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
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
    /// Runs once Avalonia has finished initializing. On desktop platforms it creates the main
    /// window and binds it to its view model.
    /// </summary>
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainViewModel(),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}