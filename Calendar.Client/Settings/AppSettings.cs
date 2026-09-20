using Calendar.Client.Themes;

namespace Calendar.Client.Settings;

/// <summary>
/// The user's preferences, as stored on disk.
/// </summary>
/// <remarks>
/// Every member carries a default, so a settings file missing a field — one written by an
/// older build, for instance — still deserializes into a usable object.
/// </remarks>
public sealed record AppSettings
{
    /// <summary>
    /// Where the server is when nothing has been configured. Right for the machine running the
    /// server and wrong for every other one, which is exactly why it is a setting.
    /// </summary>
    public const string DefaultServerAddress = "http://localhost:5000";

    /// <summary>Identifier of the chosen colour theme.</summary>
    public string ThemeId { get; init; } = AppThemes.Default.Id;

    /// <summary>Address of the machine acting as the central server.</summary>
    public string ServerAddress { get; init; } = DefaultServerAddress;
}
