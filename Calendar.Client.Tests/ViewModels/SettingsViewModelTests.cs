using Avalonia.Styling;
using Calendar.Client.Settings;
using Calendar.Client.Themes;
using Calendar.Client.ViewModels;

namespace Calendar.Client.Tests.ViewModels;

/// <summary>
/// Covers the settings section: picking a theme applies it, remembers it, and reports when it
/// could not be remembered.
/// </summary>
public sealed class SettingsViewModelTests : IDisposable
{
    private readonly string _folder;
    private readonly string _filePath;

    public SettingsViewModelTests()
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

    private SettingsViewModel CreateViewModel(List<ThemeVariant> applied) =>
        new(new SettingsStore(_filePath), applied.Add);

    [Fact]
    public void Constructor_StartsOnTheDefaultTheme_WhenNothingWasStored()
    {
        // Arrange & Act
        var viewModel = CreateViewModel([]);

        // Assert
        Assert.Equal(AppThemes.Default.Id, viewModel.SelectedTheme.Id);
    }

    [Fact]
    public void Constructor_StartsOnTheStoredTheme()
    {
        // Arrange
        new SettingsStore(_filePath).Save(new AppSettings { ThemeId = "violet" });

        // Act
        var viewModel = CreateViewModel([]);

        // Assert
        Assert.Equal("violet", viewModel.SelectedTheme.Id);
    }

    [Fact]
    public void SelectedTheme_AppliesTheVariant_WhenItChanges()
    {
        // Arrange
        var applied = new List<ThemeVariant>();
        var viewModel = CreateViewModel(applied);
        var graphite = viewModel.Themes.Single(theme => theme.Id == "graphite");

        // Act
        viewModel.SelectedTheme = graphite;

        // Assert
        Assert.Equal(graphite.Variant, Assert.Single(applied));
    }

    [Fact]
    public void SelectedTheme_IsRememberedForTheNextStart()
    {
        // Arrange
        var viewModel = CreateViewModel([]);
        var green = viewModel.Themes.Single(theme => theme.Id == "green");

        // Act
        viewModel.SelectedTheme = green;

        // Assert
        Assert.Equal("green", new SettingsStore(_filePath).Load().ThemeId);
    }

    [Fact]
    public void SelectedTheme_ReportsNoProblem_WhenTheChoiceIsStored()
    {
        // Arrange
        var viewModel = CreateViewModel([]);

        // Act
        viewModel.SelectedTheme = viewModel.Themes.Single(theme => theme.Id == "green");

        // Assert
        Assert.Null(viewModel.StatusMessage);
    }

    /// <summary>
    /// A preference that cannot be written must still apply for the session, and the user has
    /// to be told it will not survive a restart rather than being silently misled.
    /// </summary>
    [Fact]
    public void SelectedTheme_StillAppliesAndWarns_WhenTheChoiceCannotBeStored()
    {
        // Arrange: a directory where the settings file should be makes the write fail.
        Directory.CreateDirectory(_filePath);
        var applied = new List<ThemeVariant>();
        var viewModel = CreateViewModel(applied);
        var violet = viewModel.Themes.Single(theme => theme.Id == "violet");

        // Act
        viewModel.SelectedTheme = violet;

        // Assert
        Assert.Equal(violet.Variant, Assert.Single(applied));
        Assert.False(string.IsNullOrWhiteSpace(viewModel.StatusMessage));
    }

    [Fact]
    public void Themes_AreAllOfferedAndDistinct()
    {
        // Arrange & Act
        var viewModel = CreateViewModel([]);

        // Assert
        Assert.NotEmpty(viewModel.Themes);
        Assert.Equal(
            viewModel.Themes.Count,
            viewModel.Themes.Select(theme => theme.Id).Distinct().Count());
        Assert.All(viewModel.Themes, theme => Assert.False(string.IsNullOrWhiteSpace(theme.DisplayName)));
    }
}
