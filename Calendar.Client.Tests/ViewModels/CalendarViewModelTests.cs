using System.Globalization;
using Calendar.Client.Models;
using Calendar.Client.ViewModels;

namespace Calendar.Client.Tests.ViewModels;

/// <summary>
/// Covers the navigation the month view exposes: stepping by month and by year, returning to
/// today, jumping to a date, and the guards at the ends of the supported range.
/// </summary>
public sealed class CalendarViewModelTests
{
    private static readonly DateOnly Today = new(2026, 9, 6);

    private static CalendarViewModel CreateViewModel() => new(Today);

    [Fact]
    public void Constructor_OpensOnTheMonthContainingToday()
    {
        // Arrange & Act
        var viewModel = CreateViewModel();

        // Assert
        Assert.Equal(new DateOnly(2026, 9, 1), viewModel.DisplayedMonth);
    }

    [Fact]
    public void Constructor_FillsTheGrid()
    {
        // Arrange & Act
        var viewModel = CreateViewModel();

        // Assert
        Assert.Equal(MonthGrid.TotalCells, viewModel.Days.Count);
    }

    [Fact]
    public void PreviousMonth_StepsBackOneMonth()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        viewModel.PreviousMonthCommand.Execute(null);

        // Assert
        Assert.Equal(new DateOnly(2026, 8, 1), viewModel.DisplayedMonth);
    }

    [Fact]
    public void NextMonth_StepsForwardOneMonth()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        viewModel.NextMonthCommand.Execute(null);

        // Assert
        Assert.Equal(new DateOnly(2026, 10, 1), viewModel.DisplayedMonth);
    }

    [Fact]
    public void NextMonth_RollsIntoTheNextYear_WhenLeavingDecember()
    {
        // Arrange
        var viewModel = new CalendarViewModel(new DateOnly(2026, 12, 20));

        // Act
        viewModel.NextMonthCommand.Execute(null);

        // Assert
        Assert.Equal(new DateOnly(2027, 1, 1), viewModel.DisplayedMonth);
    }

    [Fact]
    public void PreviousMonth_RollsIntoThePreviousYear_WhenLeavingJanuary()
    {
        // Arrange
        var viewModel = new CalendarViewModel(new DateOnly(2026, 1, 20));

        // Act
        viewModel.PreviousMonthCommand.Execute(null);

        // Assert
        Assert.Equal(new DateOnly(2025, 12, 1), viewModel.DisplayedMonth);
    }

    [Fact]
    public void PreviousYear_KeepsTheSameMonth()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        viewModel.PreviousYearCommand.Execute(null);

        // Assert
        Assert.Equal(new DateOnly(2025, 9, 1), viewModel.DisplayedMonth);
    }

    [Fact]
    public void NextYear_KeepsTheSameMonth()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        viewModel.NextYearCommand.Execute(null);

        // Assert
        Assert.Equal(new DateOnly(2027, 9, 1), viewModel.DisplayedMonth);
    }

    /// <summary>
    /// Stepping from a 31-day month into a shorter one must not overflow into the month after
    /// it, which is what naive day-preserving arithmetic would do.
    /// </summary>
    [Fact]
    public void NextMonth_LandsOnFebruary_WhenSteppingFromA31DayJanuary()
    {
        // Arrange
        var viewModel = new CalendarViewModel(new DateOnly(2026, 1, 31));

        // Act
        viewModel.NextMonthCommand.Execute(null);

        // Assert
        Assert.Equal(new DateOnly(2026, 2, 1), viewModel.DisplayedMonth);
    }

    [Fact]
    public void GoToToday_ReturnsToTheCurrentMonth()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.NextYearCommand.Execute(null);
        viewModel.NextMonthCommand.Execute(null);

        // Act
        viewModel.GoToTodayCommand.Execute(null);

        // Assert
        Assert.Equal(new DateOnly(2026, 9, 1), viewModel.DisplayedMonth);
    }

    [Fact]
    public void SelectedDate_NavigatesToTheMonthOfTheChosenDate()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        viewModel.SelectedDate = new DateTimeOffset(new DateTime(2031, 3, 17));

        // Assert
        Assert.Equal(new DateOnly(2031, 3, 1), viewModel.DisplayedMonth);
    }

    [Fact]
    public void SelectedDate_LeavesTheViewUnchanged_WhenClearedToNull()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.NextMonthCommand.Execute(null);

        // Act
        viewModel.SelectedDate = null;

        // Assert
        Assert.Equal(new DateOnly(2026, 10, 1), viewModel.DisplayedMonth);
    }

    [Fact]
    public void SelectedDate_IsIgnored_WhenTheDateFallsOutsideTheSupportedRange()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        viewModel.SelectedDate = new DateTimeOffset(new DateTime(CalendarViewModel.MaxYear + 1, 5, 4));

        // Assert
        Assert.Equal(new DateOnly(2026, 9, 1), viewModel.DisplayedMonth);
    }

    [Fact]
    public void Navigation_IsBlockedBackwards_AtTheStartOfTheSupportedRange()
    {
        // Arrange
        var viewModel = new CalendarViewModel(new DateOnly(CalendarViewModel.MinYear, 1, 15));

        // Act & Assert
        Assert.False(viewModel.PreviousMonthCommand.CanExecute(null));
        Assert.False(viewModel.PreviousYearCommand.CanExecute(null));
        Assert.True(viewModel.NextMonthCommand.CanExecute(null));
    }

    [Fact]
    public void Navigation_IsBlockedForwards_AtTheEndOfTheSupportedRange()
    {
        // Arrange
        var viewModel = new CalendarViewModel(new DateOnly(CalendarViewModel.MaxYear, 12, 15));

        // Act & Assert
        Assert.False(viewModel.NextMonthCommand.CanExecute(null));
        Assert.False(viewModel.NextYearCommand.CanExecute(null));
        Assert.True(viewModel.PreviousMonthCommand.CanExecute(null));
    }

    [Fact]
    public void Days_AreRebuilt_WhenTheDisplayedMonthChanges()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var before = viewModel.Days;

        // Act
        viewModel.NextMonthCommand.Execute(null);

        // Assert
        Assert.NotSame(before, viewModel.Days);
        Assert.Equal(MonthGrid.TotalCells, viewModel.Days.Count);
        Assert.Contains(viewModel.Days, day => day.Date == new DateOnly(2026, 10, 1));
    }

    [Fact]
    public void MonthTitle_NamesTheDisplayedMonthAndYear()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        viewModel.NextYearCommand.Execute(null);

        // Assert: the month name follows the machine's culture, so only the year is asserted
        // literally; the name is only required to be present.
        Assert.Contains("2027", viewModel.MonthTitle);
        Assert.True(viewModel.MonthTitle.Length > 4);
    }

    [Fact]
    public void WeekdayNames_ProvidesOneDistinctLabelPerColumn()
    {
        // Arrange & Act
        var viewModel = CreateViewModel();

        // Assert
        Assert.Equal(MonthGrid.DaysPerWeek, viewModel.WeekdayNames.Count);
        Assert.Equal(MonthGrid.DaysPerWeek, viewModel.WeekdayNames.Distinct().Count());
        Assert.All(viewModel.WeekdayNames, name => Assert.False(string.IsNullOrWhiteSpace(name)));
    }

    [Fact]
    public void WeekdayNames_StartsOnTheSameDayAsTheFirstGridColumn()
    {
        // Arrange & Act
        var viewModel = CreateViewModel();

        // Assert: the header must line up with the grid, whatever the configured week start is.
        var firstColumnDay = viewModel.Days[0].Date.DayOfWeek;
        var expected = CultureInfo.CurrentCulture.DateTimeFormat.AbbreviatedDayNames[(int)firstColumnDay];
        Assert.Equal(expected, viewModel.WeekdayNames[0]);
    }
}
