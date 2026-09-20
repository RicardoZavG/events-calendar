using System.Globalization;
using System.Resources;

namespace Calendar.Client.Resources;

/// <summary>
/// Typed access to the user-visible text held in <c>Strings.resx</c>, which is the single
/// source for every label, tooltip and caption in the application.
/// </summary>
/// <remarks>
/// Written by hand rather than produced by the IDE's resource generator. The generator emits
/// its comments in whatever language the IDE runs in and overwrites its own output on every
/// edit of the .resx, so its result cannot be kept consistent with the rest of the codebase.
/// Adding a string therefore means adding the key to the .resx and one property here; the
/// property name and the resource key are deliberately identical.
/// </remarks>
public static class Strings
{
    private static readonly ResourceManager Resources =
        new("Calendar.Client.Resources.Strings", typeof(Strings).Assembly);

    public static string ActionToday => Get(nameof(ActionToday));

    public static string TooltipPreviousMonth => Get(nameof(TooltipPreviousMonth));

    public static string TooltipNextMonth => Get(nameof(TooltipNextMonth));

    public static string TooltipGoToToday => Get(nameof(TooltipGoToToday));

    public static string TooltipJumpToDate => Get(nameof(TooltipJumpToDate));

    public static string WindowTitle => Get(nameof(WindowTitle));

    public static string SectionCalendar => Get(nameof(SectionCalendar));

    public static string SectionSettings => Get(nameof(SectionSettings));

    public static string SettingsSaveFailed => Get(nameof(SettingsSaveFailed));

    public static string SettingsThemeDescription => Get(nameof(SettingsThemeDescription));

    public static string SettingsThemeTitle => Get(nameof(SettingsThemeTitle));

    public static string ThemeBlue => Get(nameof(ThemeBlue));

    public static string ThemeGraphite => Get(nameof(ThemeGraphite));

    public static string ThemeGreen => Get(nameof(ThemeGreen));

    public static string ThemeViolet => Get(nameof(ThemeViolet));

    public static string TooltipToggleMenu => Get(nameof(TooltipToggleMenu));

    public static string ErrorEventDatesRequired => Get(nameof(ErrorEventDatesRequired));

    public static string ErrorEventDescriptionTooLong => Get(nameof(ErrorEventDescriptionTooLong));

    public static string ErrorEventEndNotAfterStart => Get(nameof(ErrorEventEndNotAfterStart));

    public static string ErrorEventNotFound => Get(nameof(ErrorEventNotFound));

    public static string ErrorEventTitleRequired => Get(nameof(ErrorEventTitleRequired));

    public static string ErrorEventTitleTooLong => Get(nameof(ErrorEventTitleTooLong));

    public static string ErrorRequestInvalid => Get(nameof(ErrorRequestInvalid));

    public static string ErrorRequestTimedOut => Get(nameof(ErrorRequestTimedOut));

    public static string ErrorResponseUnreadable => Get(nameof(ErrorResponseUnreadable));

    public static string ErrorServerUnreachable => Get(nameof(ErrorServerUnreachable));

    public static string ErrorUnexpected => Get(nameof(ErrorUnexpected));

    public static string EventsMoreFormat => Get(nameof(EventsMoreFormat));

    public static string SettingsServerDescription => Get(nameof(SettingsServerDescription));

    public static string SettingsServerTitle => Get(nameof(SettingsServerTitle));

    public static string ActionRetry => Get(nameof(ActionRetry));

    /// <summary>Reads one entry from the resource file.</summary>
    /// <param name="key">Resource key, always the name of the calling property.</param>
    /// <returns>
    /// The text for the current UI culture, or the key itself when the entry is missing, so a
    /// forgotten resource shows up on screen instead of failing at runtime.
    /// </returns>
    private static string Get(string key)
    {
        return Resources.GetString(key, CultureInfo.CurrentUICulture) ?? key;
    }
}
