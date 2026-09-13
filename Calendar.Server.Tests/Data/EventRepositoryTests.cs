using Calendar.Server.Data;
using Calendar.Shared.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Calendar.Server.Tests.Data;

/// <summary>
/// Covers the repository against a real SQLite database, so the range query and the UTC round
/// trip are exercised through the provider rather than against an in-memory substitute that
/// would not behave the same way.
/// </summary>
public sealed class EventRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly CalendarDbContext _context;
    private readonly EventRepository _repository;

    public EventRepositoryTests()
    {
        // A shared in-memory database stays alive as long as the connection is open, which
        // gives a real SQLite engine with none of the leftovers of a file.
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<CalendarDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new CalendarDbContext(options);
        _context.Database.EnsureCreated();
        _repository = new EventRepository(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    private static Event BuildEvent(string title, DateTime startUtc, DateTime endUtc) => new()
    {
        Id = Guid.NewGuid(),
        Title = title,
        Start = startUtc,
        End = endUtc,
        CreatedAt = DateTime.UtcNow,
    };

    private static DateTime Utc(int year, int month, int day, int hour = 0) =>
        new(year, month, day, hour, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task CreateAsync_ThenFindByIdAsync_ReturnsTheStoredEvent()
    {
        // Arrange
        var stored = BuildEvent("Reunión", Utc(2026, 9, 8, 15), Utc(2026, 9, 8, 16));

        // Act
        await _repository.CreateAsync(stored, CancellationToken.None);
        var found = await _repository.FindByIdAsync(stored.Id, CancellationToken.None);

        // Assert
        Assert.NotNull(found);
        Assert.Equal("Reunión", found.Title);
    }

    /// <summary>
    /// SQLite has no date type, so without an explicit conversion an instant saved as UTC comes
    /// back with its kind lost and is then treated as local time.
    /// </summary>
    [Fact]
    public async Task Instants_SurviveTheRoundTripAsUtc()
    {
        // Arrange
        var start = Utc(2026, 9, 8, 15);
        var stored = BuildEvent("Reunión", start, Utc(2026, 9, 8, 16));

        // Act
        await _repository.CreateAsync(stored, CancellationToken.None);
        var found = await _repository.FindByIdAsync(stored.Id, CancellationToken.None);

        // Assert
        Assert.Equal(DateTimeKind.Utc, found!.Start.Kind);
        Assert.Equal(start, found.Start);
    }

    [Fact]
    public async Task FindByIdAsync_ReturnsNull_WhenNoEventCarriesTheIdentifier()
    {
        // Act
        var found = await _repository.FindByIdAsync(Guid.NewGuid(), CancellationToken.None);

        // Assert
        Assert.Null(found);
    }

    [Fact]
    public async Task FindInRangeAsync_ReturnsOnlyTheEventsInsideTheWindow()
    {
        // Arrange
        await _repository.CreateAsync(
            BuildEvent("Antes", Utc(2026, 8, 10), Utc(2026, 8, 11)), CancellationToken.None);
        await _repository.CreateAsync(
            BuildEvent("Dentro", Utc(2026, 9, 10), Utc(2026, 9, 11)), CancellationToken.None);
        await _repository.CreateAsync(
            BuildEvent("Después", Utc(2026, 10, 10), Utc(2026, 10, 11)), CancellationToken.None);

        // Act
        var found = await _repository.FindInRangeAsync(
            Utc(2026, 9, 1), Utc(2026, 10, 1), CancellationToken.None);

        // Assert
        Assert.Equal("Dentro", Assert.Single(found).Title);
    }

    /// <summary>
    /// A month view that dropped events already running on its first day would be wrong, so the
    /// query matches on overlap rather than on the start alone.
    /// </summary>
    [Fact]
    public async Task FindInRangeAsync_IncludesAnEventThatStartedBeforeTheWindow()
    {
        // Arrange
        await _repository.CreateAsync(
            BuildEvent("Cruza el borde", Utc(2026, 8, 30), Utc(2026, 9, 2)),
            CancellationToken.None);

        // Act
        var found = await _repository.FindInRangeAsync(
            Utc(2026, 9, 1), Utc(2026, 10, 1), CancellationToken.None);

        // Assert
        Assert.Single(found);
    }

    [Fact]
    public async Task FindInRangeAsync_ExcludesAnEventEndingExactlyWhenTheWindowOpens()
    {
        // Arrange: the window is half-open, so an event ending at its start does not overlap.
        await _repository.CreateAsync(
            BuildEvent("Justo antes", Utc(2026, 8, 31), Utc(2026, 9, 1)),
            CancellationToken.None);

        // Act
        var found = await _repository.FindInRangeAsync(
            Utc(2026, 9, 1), Utc(2026, 10, 1), CancellationToken.None);

        // Assert
        Assert.Empty(found);
    }

    [Fact]
    public async Task FindInRangeAsync_OrdersByStart()
    {
        // Arrange
        await _repository.CreateAsync(
            BuildEvent("Tercero", Utc(2026, 9, 20), Utc(2026, 9, 21)), CancellationToken.None);
        await _repository.CreateAsync(
            BuildEvent("Primero", Utc(2026, 9, 5), Utc(2026, 9, 6)), CancellationToken.None);
        await _repository.CreateAsync(
            BuildEvent("Segundo", Utc(2026, 9, 12), Utc(2026, 9, 13)), CancellationToken.None);

        // Act
        var found = await _repository.FindInRangeAsync(
            Utc(2026, 9, 1), Utc(2026, 10, 1), CancellationToken.None);

        // Assert
        Assert.Equal(["Primero", "Segundo", "Tercero"], found.Select(item => item.Title));
    }

    [Fact]
    public async Task FindInRangeAsync_ReturnsOverlappingEvents()
    {
        // Arrange: overlaps are allowed, so both are stored and both come back.
        await _repository.CreateAsync(
            BuildEvent("Uno", Utc(2026, 9, 8, 15), Utc(2026, 9, 8, 17)), CancellationToken.None);
        await _repository.CreateAsync(
            BuildEvent("Dos", Utc(2026, 9, 8, 16), Utc(2026, 9, 8, 18)), CancellationToken.None);

        // Act
        var found = await _repository.FindInRangeAsync(
            Utc(2026, 9, 1), Utc(2026, 10, 1), CancellationToken.None);

        // Assert
        Assert.Equal(2, found.Count);
    }

    [Fact]
    public async Task UpdateAsync_ReplacesTheStoredEvent()
    {
        // Arrange
        var stored = BuildEvent("Original", Utc(2026, 9, 8, 15), Utc(2026, 9, 8, 16));
        await _repository.CreateAsync(stored, CancellationToken.None);

        // Act
        var replaced = await _repository.UpdateAsync(
            stored with { Title = "Cambiado" }, CancellationToken.None);
        var found = await _repository.FindByIdAsync(stored.Id, CancellationToken.None);

        // Assert
        Assert.True(replaced);
        Assert.Equal("Cambiado", found!.Title);
    }

    [Fact]
    public async Task UpdateAsync_ReportsFailure_WhenTheEventDoesNotExist()
    {
        // Arrange
        var unknown = BuildEvent("Fantasma", Utc(2026, 9, 8, 15), Utc(2026, 9, 8, 16));

        // Act
        var replaced = await _repository.UpdateAsync(unknown, CancellationToken.None);

        // Assert
        Assert.False(replaced);
    }

    [Fact]
    public async Task DeleteAsync_RemovesTheEvent()
    {
        // Arrange
        var stored = BuildEvent("Temporal", Utc(2026, 9, 8, 15), Utc(2026, 9, 8, 16));
        await _repository.CreateAsync(stored, CancellationToken.None);

        // Act
        var removed = await _repository.DeleteAsync(stored.Id, CancellationToken.None);
        var found = await _repository.FindByIdAsync(stored.Id, CancellationToken.None);

        // Assert
        Assert.True(removed);
        Assert.Null(found);
    }

    [Fact]
    public async Task DeleteAsync_ReportsFailure_WhenTheEventDoesNotExist()
    {
        // Act
        var removed = await _repository.DeleteAsync(Guid.NewGuid(), CancellationToken.None);

        // Assert
        Assert.False(removed);
    }
}
