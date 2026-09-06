using System.Globalization;
using Calendar.Client.Models;
using Calendar.Client.ViewModels;

namespace Calendar.Client.Tests.ViewModels;

/// <summary>
/// Covers the navigation the month view exposes: stepping by month, returning to today,
/// jumping to a date, and the guards at the ends of the supported range.
/// </summary>
public sealed class CalendarViewModelTests
{
    private static readonly DateOnly Today = new(2026, 9, 6);

    private static CalendarViewModel CreateViewModel() => new(Today);

    private static DateOnly PickedDate(CalendarViewModel viewModel) =>
        DateOnly.FromDateTime(viewModel.SelectedDate!.Value.Date);

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
    public void Constructor_StartsThePickerOnToday()
    {
        // Arrange & Act
        var viewModel = CreateViewModel();

        // Assert: a picker left empty renders its own untranslated placeholders.
        Assert.NotNull(viewModel.SelectedDate);
        Assert.Equal(Today, PickedDate(viewModel));
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
        viewModel.NextMonthCommand.Execute(null);
        viewModel.NextMonthCommand.Execute(null);

        // Act
        viewModel.GoToTodayCommand.Execute(null);

        // Assert
        Assert.Equal(new DateOnly(2026, 9, 1), viewModel.DisplayedMonth);
    }

    [Fact]
    public void GoToToday_PutsThePickerBackOnToday()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.SelectedDate = new DateTimeOffset(new DateTime(2031, 3, 17));

        // Act
        viewModel.GoToTodayCommand.Execute(null);

        // Assert
        Assert.Equal(Today, PickedDate(viewModel));
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

    /// <summary>
    /// Landing on the month the user picked must not snap the selection back to the 1st: the
    /// picker is the only place the exact date is shown.
    /// </summary>
    [Fact]
    public void SelectedDate_KeepsTheChosenDay_WhenItAlreadyMatchesTheDisplayedMonth()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        viewModel.SelectedDate = new DateTimeOffset(new DateTime(2031, 3, 17));

        // Assert
        Assert.Equal(new DateOnly(2031, 3, 17), PickedDate(viewModel));
    }

    /// <summary>
    /// The picker carries the year, which the title no longer shows, so it has to follow
    /// navigation instead of only feeding it.
    /// </summary>
    [Fact]
    public void SelectedDate_FollowsTheArrows_ToTheFirstOfTheNewMonth()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        viewModel.NextMonthCommand.Execute(null);

        // Assert
        Assert.Equal(new DateOnly(2026, 10, 1), PickedDate(viewModel));
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
        Assert.True(viewModel.NextMonthCommand.CanExecute(null));
    }

    [Fact]
    public void Navigation_IsBlockedForwards_AtTheEndOfTheSupportedRange()
    {
        // Arrange
        var viewModel = new CalendarViewModel(new DateOnly(CalendarViewModel.MaxYear, 12, 15));

        // Act & Assert
        Assert.False(viewModel.NextMonthCommand.CanExecute(null));
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

    /// <summary>
    /// The year belongs to the picker, not to the title; showing it in both was the redundancy
    /// this layout removes.
    /// </summary>
    [Fact]
    public void MonthTitle_NamesTheMonthWithoutTheYear()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        viewModel.NextMonthCommand.Execute(null);

        // Assert: the month name follows the machine's culture, so only its absence of digits
        // and its match with the culture's own name are asserted.
        var expected = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(
            CultureInfo.CurrentCulture.DateTimeFormat.MonthNames[9]);
        Assert.Equal(expected, viewModel.MonthTitle);
        Assert.False(viewModel.MonthTitle.Any(char.IsDigit));
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
