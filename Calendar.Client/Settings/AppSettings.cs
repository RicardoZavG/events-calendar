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
    /// <summary>Identifier of the chosen colour theme.</summary>
    public string ThemeId { get; init; } = AppThemes.Default.Id;
}
