using System;
using System.IO;
using System.Text.Json;

namespace Calendar.Client.Settings;

/// <summary>
/// Reads and writes the user's preferences as a JSON file.
/// </summary>
/// <remarks>
/// The file lives in the user's application-data folder, never beside the executable, which
/// may sit in a read-only location. Its contents are treated as untrusted input: they can be
/// absent, truncated or hand-edited, and none of those may stop the application from starting.
/// </remarks>
public sealed class SettingsStore
{
    private const string FolderName = "CalendarLan";
    private const string FileName = "settings.json";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
    };

    private readonly string _filePath;

    /// <summary>Creates a store over the file in the user's application-data folder.</summary>
    public SettingsStore()
        : this(DefaultFilePath())
    {
    }

    /// <summary>Creates a store over a specific file.</summary>
    /// <param name="filePath">Full path of the settings file, which need not exist yet.</param>
    public SettingsStore(string filePath)
    {
        _filePath = filePath;
    }

    /// <summary>The location used when no path is given.</summary>
    /// <returns>Full path of the settings file inside the user's application-data folder.</returns>
    public static string DefaultFilePath()
    {
        var root = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(root, FolderName, FileName);
    }

    /// <summary>Reads the stored preferences.</summary>
    /// <returns>
    /// The stored settings, or a default instance when the file is missing, unreadable or not
    /// valid JSON. Falling back is deliberate: a damaged preferences file must degrade to the
    /// default appearance, never block startup.
    /// </returns>
    public AppSettings Load()
    {
        try
        {
            if (!File.Exists(_filePath))
            {
                return new AppSettings();
            }

            var json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<AppSettings>(json, SerializerOptions)
                   ?? new AppSettings();
        }
        catch (Exception exception) when (IsRecoverable(exception))
        {
            return new AppSettings();
        }
    }

    /// <summary>Writes the preferences, creating the folder if needed.</summary>
    /// <param name="settings">The preferences to persist.</param>
    /// <returns>
    /// <c>true</c> when the file was written; <c>false</c> when it could not be. The failure is
    /// reported rather than thrown so the caller can tell the user their choice will not
    /// outlive the session, instead of the application dying on a preference change.
    /// </returns>
    public bool Save(AppSettings settings)
    {
        try
        {
            var folder = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(folder))
            {
                Directory.CreateDirectory(folder);
            }

            File.WriteAllText(_filePath, JsonSerializer.Serialize(settings, SerializerOptions));
            return true;
        }
        catch (Exception exception) when (IsRecoverable(exception))
        {
            return false;
        }
    }

    /// <summary>
    /// Whether a failure is one of the expected ways a file on disk can be unavailable, as
    /// opposed to a defect that should surface.
    /// </summary>
    /// <param name="exception">The failure raised while reading or writing.</param>
    /// <returns><c>true</c> when the operation can safely fall back to defaults.</returns>
    private static bool IsRecoverable(Exception exception)
    {
        return exception is IOException
            or UnauthorizedAccessException
            or JsonException
            or NotSupportedException;
    }
}
