using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using Avalonia.Styling;
using Calendar.Client.Resources;

namespace Calendar.Client.Themes;

/// <summary>
/// One colour theme offered in the settings panel.
/// </summary>
public sealed record AppTheme
{
    /// <summary>Stable identifier written to the settings file. Never translated.</summary>
    public required string Id { get; init; }

    /// <summary>Name shown to the user, in the current language.</summary>
    public required string DisplayName { get; init; }

    /// <summary>The variant that selects this theme's entry in the palette dictionary.</summary>
    public required ThemeVariant Variant { get; init; }

    /// <summary>Accent colour, shown as a swatch so the theme is picked by sight, not by name.</summary>
    public required IBrush PreviewBrush { get; init; }
}

/// <summary>
/// The themes the application ships with.
/// </summary>
/// <remarks>
/// Each variant declares whether it builds on the light or the dark base. That inherited base
/// is what the framework's own controls fall back to, so a dark palette does not end up with
/// light-themed pickers and scroll bars.
/// <para>
/// Palettes are hand-picked rather than derived from a seed colour. Generating a full palette
/// from one colour is arithmetically easy and visually unreliable: readable contrast is a
/// design decision, and an automatically lightened accent can leave text unreadable on it.
/// </para>
/// </remarks>
public static class AppThemes
{
    /// <summary>Key of the blue palette. Public because <c>Palette.axaml</c> keys on it.</summary>
    public static ThemeVariant BlueVariantKey { get; } = new("Blue", ThemeVariant.Light);

    /// <summary>Key of the green palette.</summary>
    public static ThemeVariant GreenVariantKey { get; } = new("Green", ThemeVariant.Light);

    /// <summary>Key of the violet palette.</summary>
    public static ThemeVariant VioletVariantKey { get; } = new("Violet", ThemeVariant.Light);

    /// <summary>Key of the graphite palette, which builds on the dark base.</summary>
    public static ThemeVariant GraphiteVariantKey { get; } = new("Graphite", ThemeVariant.Dark);

    /// <summary>Every theme the user can choose, in the order the settings panel lists them.</summary>
    public static IReadOnlyList<AppTheme> All { get; } =
    [
        new AppTheme
        {
            Id = "blue",
            DisplayName = Strings.ThemeBlue,
            Variant = BlueVariantKey,
            PreviewBrush = new SolidColorBrush(Color.Parse("#1A73E8")),
        },
        new AppTheme
        {
            Id = "green",
            DisplayName = Strings.ThemeGreen,
            Variant = GreenVariantKey,
            PreviewBrush = new SolidColorBrush(Color.Parse("#1E8E3E")),
        },
        new AppTheme
        {
            Id = "violet",
            DisplayName = Strings.ThemeViolet,
            Variant = VioletVariantKey,
            PreviewBrush = new SolidColorBrush(Color.Parse("#6C40BF")),
        },
        new AppTheme
        {
            Id = "graphite",
            DisplayName = Strings.ThemeGraphite,
            Variant = GraphiteVariantKey,
            PreviewBrush = new SolidColorBrush(Color.Parse("#8AB4F8")),
        },
    ];

    /// <summary>The theme used when none has been chosen yet.</summary>
    public static AppTheme Default => All[0];

    /// <summary>
    /// Resolves a stored identifier to a theme.
    /// </summary>
    /// <param name="id">Identifier read from the settings file; may be null or unknown.</param>
    /// <returns>
    /// The matching theme, or <see cref="Default"/> when the identifier is missing or does not
    /// name a theme this version ships — a settings file written by a newer build must not
    /// prevent the application from starting.
    /// </returns>
    public static AppTheme FromId(string? id)
    {
        return All.FirstOrDefault(theme => string.Equals(theme.Id, id, StringComparison.Ordinal))
               ?? Default;
    }
}
