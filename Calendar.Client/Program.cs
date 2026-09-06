using Avalonia;
using System;

namespace Calendar.Client;

sealed class Program
{
    /// <summary>
    /// Entry point of the client. Builds the Avalonia application and runs it under the
    /// classic desktop lifetime, which keeps the process alive until the main window closes.
    /// </summary>
    /// <param name="args">Command-line arguments forwarded to the Avalonia lifetime.</param>
    /// <remarks>
    /// Do not use Avalonia, third-party APIs or any SynchronizationContext-reliant code
    /// before this runs: nothing is initialized yet and things may break.
    /// </remarks>
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);

    /// <summary>
    /// Configures the Avalonia application: platform backend, fonts and logging.
    /// </summary>
    /// <returns>The configured <see cref="AppBuilder"/>, ready to be started.</returns>
    /// <remarks>Also called by the visual designer, so it must not be removed or renamed.</remarks>
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();
}
