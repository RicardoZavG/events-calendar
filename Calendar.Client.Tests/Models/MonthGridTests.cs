using Calendar.Client.Models;

namespace Calendar.Client.Tests.Models;

/// <summary>
/// Covers the date arithmetic behind the month view: grid size, the fill taken from the
/// adjacent months, month lengths and year changes.
/// </summary>
public sealed class MonthGridTests
{
    private const DayOfWeek SundayStart = DayOfWeek.Sunday;

    /// <summary>A date far from every month under test, so no cell is ever flagged as today.</summary>
    private static readonly DateOnly UnrelatedToday = new(1980, 6, 15);

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    [InlineData(9)]
    [InlineData(10)]
    [InlineData(11)]
    [InlineData(12)]
    public void Build_ReturnsSixFullWeeks_ForEveryMonth(int month)
    {
        // Arrange & Act
        var cells = MonthGrid.Build(2026, month, UnrelatedToday, SundayStart);

        // Assert
        Assert.Equal(MonthGrid.TotalCells, cells.Count);
    }

    [Fact]
    public void Build_ReturnsConsecutiveDates_WithNoGapsOrRepeats()
    {
        // Arrange & Act
        var cells = MonthGrid.Build(2026, 9, UnrelatedToday, SundayStart);

        // Assert
        for (var index = 1; index < cells.Count; index++)
        {
            Assert.Equal(cells[index - 1].Date.AddDays(1), cells[index].Date);
        }
    }

    [Theory]
    [InlineData(DayOfWeek.Sunday)]
    [InlineData(DayOfWeek.Monday)]
    [InlineData(DayOfWeek.Tuesday)]
    [InlineData(DayOfWeek.Wednesday)]
    [InlineData(DayOfWeek.Thursday)]
    [InlineData(DayOfWeek.Friday)]
    [InlineData(DayOfWeek.Saturday)]
    public void Build_StartsOnTheConfiguredFirstDayOfWeek(DayOfWeek firstDayOfWeek)
    {
        // Arrange & Act
        var cells = MonthGrid.Build(2026, 9, UnrelatedToday, firstDayOfWeek);

        // Assert
        Assert.Equal(firstDayOfWeek, cells[0].Date.DayOfWeek);
    }

    /// <summary>
    /// 1 February 2026 falls on a Sunday, so with a Sunday week start the month fills the
    /// first column itself and no leading days are borrowed.
    /// </summary>
    [Fact]
    public void Build_BorrowsNoLeadingDays_WhenTheMonthStartsOnTheFirstDayOfWeek()
    {
        // Arrange
        var firstOfMonth = new DateOnly(2026, 2, 1);
        Assert.Equal(SundayStart, firstOfMonth.DayOfWeek);

        // Act
        var cells = MonthGrid.Build(2026, 2, UnrelatedToday, SundayStart);

        // Assert
        Assert.Equal(firstOfMonth, cells[0].Date);
        Assert.True(cells[0].IsInCurrentMonth);
    }

    /// <summary>
    /// 1 August 2026 falls on a Saturday, the last column of a Sunday-based week, which is the
    /// case that needs the full six leading days.
    /// </summary>
    [Fact]
    public void Build_BorrowsSixLeadingDays_WhenTheMonthStartsOnTheLastDayOfWeek()
    {
        // Arrange
        var firstOfMonth = new DateOnly(2026, 8, 1);
        Assert.Equal(DayOfWeek.Saturday, firstOfMonth.DayOfWeek);

        // Act
        var cells = MonthGrid.Build(2026, 8, UnrelatedToday, SundayStart);

        // Assert
        Assert.Equal(new DateOnly(2026, 7, 26), cells[0].Date);
        Assert.Equal(firstOfMonth, cells[6].Date);
        Assert.All(cells.Take(6), cell => Assert.False(cell.IsInCurrentMonth));
    }

    [Theory]
    [InlineData(2026, 2, 28)]
    [InlineData(2024, 2, 29)]
    [InlineData(2026, 4, 30)]
    [InlineData(2026, 1, 31)]
    public void Build_FlagsExactlyTheDaysOfTheMonth_AsBelongingToIt(
        int year,
        int month,
        int expectedDayCount)
    {
        // Arrange & Act
        var cells = MonthGrid.Build(year, month, UnrelatedToday, SundayStart);
        var owned = cells.Where(cell => cell.IsInCurrentMonth).ToList();

        // Assert
        Assert.Equal(expectedDayCount, owned.Count);
        Assert.All(owned, cell => Assert.Equal(month, cell.Date.Month));
        Assert.All(owned, cell => Assert.Equal(year, cell.Date.Year));
        Assert.Equal(1, owned[0].DayNumber);
        Assert.Equal(expectedDayCount, owned[^1].DayNumber);
    }

    [Fact]
    public void Build_FillsWithTheNextYear_WhenRenderingDecember()
    {
        // Arrange & Act
        var cells = MonthGrid.Build(2026, 12, UnrelatedToday, SundayStart);
        var trailing = cells.Last();

        // Assert
        Assert.Equal(2027, trailing.Date.Year);
        Assert.Equal(1, trailing.Date.Month);
        Assert.False(trailing.IsInCurrentMonth);
    }

    [Fact]
    public void Build_FillsWithThePreviousYear_WhenRenderingJanuary()
    {
        // Arrange & Act
        var cells = MonthGrid.Build(2027, 1, UnrelatedToday, SundayStart);
        var leading = cells[0];

        // Assert
        Assert.Equal(2026, leading.Date.Year);
        Assert.Equal(12, leading.Date.Month);
        Assert.False(leading.IsInCurrentMonth);
    }

    [Fact]
    public void Build_FlagsASingleCellAsToday_WhenTodayIsInsideTheRenderedRange()
    {
        // Arrange
        var today = new DateOnly(2026, 9, 6);

        // Act
        var cells = MonthGrid.Build(2026, 9, today, SundayStart);

        // Assert
        var flagged = Assert.Single(cells, cell => cell.IsToday);
        Assert.Equal(today, flagged.Date);
    }

    [Fact]
    public void Build_FlagsTodayInTheBorrowedDays_WhenItFallsInAnAdjacentMonth()
    {
        // Arrange: 31 August 2026 is rendered as a leading day of September.
        var today = new DateOnly(2026, 8, 31);

        // Act
        var cells = MonthGrid.Build(2026, 9, today, SundayStart);

        // Assert
        var flagged = Assert.Single(cells, cell => cell.IsToday);
        Assert.False(flagged.IsInCurrentMonth);
    }

    [Fact]
    public void Build_FlagsNoCellAsToday_WhenTodayIsOutsideTheRenderedRange()
    {
        // Arrange & Act
        var cells = MonthGrid.Build(2026, 9, UnrelatedToday, SundayStart);

        // Assert
        Assert.DoesNotContain(cells, cell => cell.IsToday);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    public void Build_Throws_WhenTheMonthIsNotAValidDate(int month)
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(
            () => MonthGrid.Build(2026, month, UnrelatedToday, SundayStart));
    }
}
