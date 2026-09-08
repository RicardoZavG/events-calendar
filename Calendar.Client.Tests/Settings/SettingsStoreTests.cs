using Calendar.Client.Settings;
using Calendar.Client.Themes;

namespace Calendar.Client.Tests.Settings;

/// <summary>
/// Covers reading and writing the preferences file, including the ways it can be unusable:
/// a damaged file must degrade to defaults, never stop the application from starting.
/// </summary>
public sealed class SettingsStoreTests : IDisposable
{
    private readonly string _folder;
    private readonly string _filePath;

    public SettingsStoreTests()
    {
        _folder = Path.Combine(Path.GetTempPath(), "calendar-tests-" + Guid.NewGuid().ToString("N"));
        _filePath = Path.Combine(_folder, "settings.json");
    }

    public void Dispose()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }

    [Fact]
    public void Load_ReturnsDefaults_WhenTheFileDoesNotExist()
    {
        // Arrange
        var store = new SettingsStore(_filePath);

        // Act
        var settings = store.Load();

        // Assert
        Assert.Equal(AppThemes.Default.Id, settings.ThemeId);
    }

    [Fact]
    public void Save_ThenLoad_ReturnsWhatWasStored()
    {
        // Arrange
        var store = new SettingsStore(_filePath);

        // Act
        var saved = store.Save(new AppSettings { ThemeId = "graphite" });
        var settings = new SettingsStore(_filePath).Load();

        // Assert
        Assert.True(saved);
        Assert.Equal("graphite", settings.ThemeId);
    }

    [Fact]
    public void Save_CreatesTheFolder_WhenItIsMissing()
    {
        // Arrange
        var store = new SettingsStore(_filePath);

        // Act
        var saved = store.Save(new AppSettings());

        // Assert
        Assert.True(saved);
        Assert.True(File.Exists(_filePath));
    }

    [Fact]
    public void Load_ReturnsDefaults_WhenTheFileIsNotValidJson()
    {
        // Arrange
        Directory.CreateDirectory(_folder);
        File.WriteAllText(_filePath, "{ this is not json");

        // Act
        var settings = new SettingsStore(_filePath).Load();

        // Assert
        Assert.Equal(AppThemes.Default.Id, settings.ThemeId);
    }

    [Fact]
    public void Load_ReturnsDefaults_WhenTheFileIsEmpty()
    {
        // Arrange
        Directory.CreateDirectory(_folder);
        File.WriteAllText(_filePath, string.Empty);

        // Act
        var settings = new SettingsStore(_filePath).Load();

        // Assert
        Assert.Equal(AppThemes.Default.Id, settings.ThemeId);
    }

    /// <summary>
    /// A file written by a newer build can name a theme this one does not ship. It must not be
    /// rejected — it is resolved to the default when the theme is looked up.
    /// </summary>
    [Fact]
    public void Load_KeepsAnUnknownThemeId_WhichResolvesToTheDefaultTheme()
    {
        // Arrange
        Directory.CreateDirectory(_folder);
        File.WriteAllText(_filePath, """{ "ThemeId": "from-a-newer-version" }""");

        // Act
        var settings = new SettingsStore(_filePath).Load();

        // Assert
        Assert.Equal("from-a-newer-version", settings.ThemeId);
        Assert.Equal(AppThemes.Default.Id, AppThemes.FromId(settings.ThemeId).Id);
    }

    [Fact]
    public void Load_FillsMissingFields_WhenTheFileOmitsThem()
    {
        // Arrange
        Directory.CreateDirectory(_folder);
        File.WriteAllText(_filePath, "{}");

        // Act
        var settings = new SettingsStore(_filePath).Load();

        // Assert
        Assert.Equal(AppThemes.Default.Id, settings.ThemeId);
    }
}
