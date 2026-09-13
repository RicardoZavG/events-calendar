using Calendar.Client.Models;
using Calendar.Shared.Models;

namespace Calendar.Client.Tests.Models;

/// <summary>
/// Covers placing events onto the days of the grid: which day an instant lands on, and which
/// days a multi-day event occupies.
/// </summary>
public sealed class EventLayoutTests
{
    private static readonly DateOnly Today = new(2026, 9, 6);

    /// <summary>
    /// Fixed to UTC so the expected days are the same on every machine. The production path
    /// passes the local zone; what is under test is the arithmetic, not the zone database.
    /// </summary>
    private static readonly TimeZoneInfo Zone = TimeZoneInfo.Utc;

    private static IReadOnlyList<CalendarDay> SeptemberGrid() =>
        MonthGrid.Build(2026, 9, Today, DayOfWeek.Sunday);

    private static Event BuildEvent(
        string title,
        DateTime startUtc,
        DateTime endUtc) => new()
    {
        Id = Guid.NewGuid(),
        Title = title,
        Start = startUtc,
        End = endUtc,
        CreatedAt = DateTime.UtcNow,
    };

    private static DateTime Utc(int year, int month, int day, int hour = 0, int minute = 0) =>
        new(year, month, day, hour, minute, 0, DateTimeKind.Utc);

    private static CalendarDay DayOf(IReadOnlyList<CalendarDay> cells, DateOnly date) =>
        cells.Single(cell => cell.Date == date);

    [Fact]
    public void Distribute_LeavesEveryCellEmpty_WhenThereAreNoEvents()
    {
        // Act
        var cells = EventLayout.Distribute(SeptemberGrid(), [], Zone);

        // Assert
        Assert.All(cells, cell => Assert.Empty(cell.Events));
    }

    [Fact]
    public void Distribute_KeepsTheCellsInOrderAndInNumber()
    {
        // Arrange
        var grid = SeptemberGrid();

        // Act
        var cells = EventLayout.Distribute(grid, [], Zone);

        // Assert
        Assert.Equal(MonthGrid.TotalCells, cells.Count);
        Assert.Equal(grid.Select(cell => cell.Date), cells.Select(cell => cell.Date));
    }

    [Fact]
    public void Distribute_PutsAnEventOnTheDayItHappens()
    {
        // Arrange
        var events = new[] { BuildEvent("Reunión", Utc(2026, 9, 8, 15), Utc(2026, 9, 8, 16)) };

        // Act
        var cells = EventLayout.Distribute(SeptemberGrid(), events, Zone);

        // Assert
        var chip = Assert.Single(DayOf(cells, new DateOnly(2026, 9, 8)).Events);
        Assert.Equal("Reunión", chip.Title);
        Assert.False(chip.IsContinuation);
    }

    [Fact]
    public void Distribute_LeavesEveryOtherDayUntouched()
    {
        // Arrange
        var events = new[] { BuildEvent("Reunión", Utc(2026, 9, 8, 15), Utc(2026, 9, 8, 16)) };

        // Act
        var cells = EventLayout.Distribute(SeptemberGrid(), events, Zone);

        // Assert
        Assert.All(
            cells.Where(cell => cell.Date != new DateOnly(2026, 9, 8)),
            cell => Assert.Empty(cell.Events));
    }

    /// <summary>
    /// An event running across several days belongs on each of them, or the middle days would
    /// look free while they are not.
    /// </summary>
    [Fact]
    public void Distribute_PutsAMultiDayEventOnEveryDayItCovers()
    {
        // Arrange
        var events = new[] { BuildEvent("Congreso", Utc(2026, 9, 10, 9), Utc(2026, 9, 12, 18)) };

        // Act
        var cells = EventLayout.Distribute(SeptemberGrid(), events, Zone);

        // Assert
        Assert.Single(DayOf(cells, new DateOnly(2026, 9, 10)).Events);
        Assert.Single(DayOf(cells, new DateOnly(2026, 9, 11)).Events);
        Assert.Single(DayOf(cells, new DateOnly(2026, 9, 12)).Events);
        Assert.Empty(DayOf(cells, new DateOnly(2026, 9, 13)).Events);
    }

    [Fact]
    public void Distribute_MarksOnlyTheFirstDayAsTheStart()
    {
        // Arrange
        var events = new[] { BuildEvent("Congreso", Utc(2026, 9, 10, 9), Utc(2026, 9, 12, 18)) };

        // Act
        var cells = EventLayout.Distribute(SeptemberGrid(), events, Zone);

        // Assert: repeating a start time on days the event merely runs through would be a lie.
        Assert.False(DayOf(cells, new DateOnly(2026, 9, 10)).Events[0].IsContinuation);
        Assert.NotNull(DayOf(cells, new DateOnly(2026, 9, 10)).Events[0].TimeLabel);

        Assert.True(DayOf(cells, new DateOnly(2026, 9, 11)).Events[0].IsContinuation);
        Assert.Null(DayOf(cells, new DateOnly(2026, 9, 11)).Events[0].TimeLabel);
    }

    /// <summary>
    /// An event ending exactly at midnight finishes as the next day begins, so it does not
    /// occupy it.
    /// </summary>
    [Fact]
    public void Distribute_ExcludesADayTheEventOnlyEndsOn()
    {
        // Arrange
        var events = new[] { BuildEvent("Guardia", Utc(2026, 9, 10, 20), Utc(2026, 9, 11)) };

        // Act
        var cells = EventLayout.Distribute(SeptemberGrid(), events, Zone);

        // Assert
        Assert.Single(DayOf(cells, new DateOnly(2026, 9, 10)).Events);
        Assert.Empty(DayOf(cells, new DateOnly(2026, 9, 11)).Events);
    }

    /// <summary>
    /// The grid shows days of the neighbouring months, and the events falling on them belong
    /// there too — that is the whole reason the client asks for the grid's window and not the
    /// calendar month's.
    /// </summary>
    [Fact]
    public void Distribute_FillsTheDaysBorrowedFromTheAdjacentMonths()
    {
        // Arrange
        var events = new[]
        {
            BuildEvent("Cierre de agosto", Utc(2026, 8, 31, 10), Utc(2026, 8, 31, 11)),
            BuildEvent("Inicio de octubre", Utc(2026, 10, 1, 10), Utc(2026, 10, 1, 11)),
        };

        // Act
        var cells = EventLayout.Distribute(SeptemberGrid(), events, Zone);

        // Assert
        Assert.Single(DayOf(cells, new DateOnly(2026, 8, 31)).Events);
        Assert.Single(DayOf(cells, new DateOnly(2026, 10, 1)).Events);
    }

    [Fact]
    public void Distribute_OrdersTheEventsOfADayByStart()
    {
        // Arrange
        var events = new[]
        {
            BuildEvent("Tarde", Utc(2026, 9, 8, 17), Utc(2026, 9, 8, 18)),
            BuildEvent("Mañana", Utc(2026, 9, 8, 9), Utc(2026, 9, 8, 10)),
            BuildEvent("Mediodía", Utc(2026, 9, 8, 13), Utc(2026, 9, 8, 14)),
        };

        // Act
        var cells = EventLayout.Distribute(SeptemberGrid(), events, Zone);

        // Assert
        Assert.Equal(
            ["Mañana", "Mediodía", "Tarde"],
            DayOf(cells, new DateOnly(2026, 9, 8)).Events.Select(chip => chip.Title));
    }

    [Fact]
    public void Distribute_KeepsOverlappingEvents()
    {
        // Arrange: overlaps are allowed, so the cell carries both.
        var events = new[]
        {
            BuildEvent("Uno", Utc(2026, 9, 8, 15), Utc(2026, 9, 8, 17)),
            BuildEvent("Dos", Utc(2026, 9, 8, 16), Utc(2026, 9, 8, 18)),
        };

        // Act
        var cells = EventLayout.Distribute(SeptemberGrid(), events, Zone);

        // Assert
        Assert.Equal(2, DayOf(cells, new DateOnly(2026, 9, 8)).Events.Count);
    }

    [Fact]
    public void VisibleEvents_ShowsWhatFitsAndCountsTheRest()
    {
        // Arrange
        var events = Enumerable.Range(0, CalendarDay.MaxVisibleEvents + 2)
            .Select(index => BuildEvent(
                $"Evento {index}",
                Utc(2026, 9, 8, 8 + index),
                Utc(2026, 9, 8, 9 + index)))
            .ToArray();

        // Act
        var day = DayOf(
            EventLayout.Distribute(SeptemberGrid(), events, Zone),
            new DateOnly(2026, 9, 8));

        // Assert
        Assert.Equal(CalendarDay.MaxVisibleEvents, day.VisibleEvents.Count);
        Assert.Equal(2, day.HiddenEventCount);
        Assert.True(day.HasHiddenEvents);
    }

    [Fact]
    public void VisibleEvents_ShowsEverything_WhenItAllFits()
    {
        // Arrange
        var events = new[] { BuildEvent("Único", Utc(2026, 9, 8, 15), Utc(2026, 9, 8, 16)) };

        // Act
        var day = DayOf(
            EventLayout.Distribute(SeptemberGrid(), events, Zone),
            new DateOnly(2026, 9, 8));

        // Assert
        Assert.Single(day.VisibleEvents);
        Assert.Equal(0, day.HiddenEventCount);
        Assert.False(day.HasHiddenEvents);
    }
}
