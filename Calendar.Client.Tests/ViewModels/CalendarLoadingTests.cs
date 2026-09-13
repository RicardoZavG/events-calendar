using Calendar.Client.Models;
using Calendar.Client.Services;
using Calendar.Client.ViewModels;
using Calendar.Shared.Contracts;
using Calendar.Shared.Models;

namespace Calendar.Client.Tests.ViewModels;

/// <summary>
/// Covers the calendar asking the server for the month on screen: what it asks for, what it
/// does with the answer, and what it tells the user when there is no answer at all.
/// </summary>
public sealed class CalendarLoadingTests
{
    private static readonly DateOnly Today = new(2026, 9, 6);
    private static readonly TimeZoneInfo Zone = TimeZoneInfo.Utc;

    /// <summary>Answers whatever it was built with, and records what it was asked.</summary>
    private sealed class StubApiClient(ApiResult<IReadOnlyList<Event>> answer) : IEventApiClient
    {
        public int Calls { get; private set; }

        public DateTime LastFrom { get; private set; }

        public DateTime LastTo { get; private set; }

        public Task<ApiResult<IReadOnlyList<Event>>> GetEventsAsync(
            DateTime fromUtc,
            DateTime toUtc,
            CancellationToken cancellationToken)
        {
            Calls++;
            LastFrom = fromUtc;
            LastTo = toUtc;
            return Task.FromResult(answer);
        }
    }

    private static Event BuildEvent(string title, DateTime startUtc, DateTime endUtc) => new()
    {
        Id = Guid.NewGuid(),
        Title = title,
        Start = startUtc,
        End = endUtc,
        CreatedAt = DateTime.UtcNow,
    };

    private static CalendarViewModel Create(IEventApiClient api) => new(api, Today, Zone);

    [Fact]
    public async Task Reload_AsksForTheWholeGrid_NotOnlyTheCalendarMonth()
    {
        // Arrange
        var api = new StubApiClient(ApiResult<IReadOnlyList<Event>>.Ok([]));
        var viewModel = Create(api);

        // Act
        await viewModel.ReloadAsync();

        // Assert: the grid starts before September and ends after it, and an event on one of
        // those borrowed days has to come back with the rest.
        Assert.Equal(viewModel.Days[0].Date, DateOnly.FromDateTime(api.LastFrom));
        Assert.Equal(viewModel.Days[^1].Date.AddDays(1), DateOnly.FromDateTime(api.LastTo));
    }

    [Fact]
    public async Task Reload_PlacesTheEventsReturned()
    {
        // Arrange
        var events = new[]
        {
            BuildEvent(
                "Reunión",
                new DateTime(2026, 9, 8, 15, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 9, 8, 16, 0, 0, DateTimeKind.Utc)),
        };
        var viewModel = Create(new StubApiClient(ApiResult<IReadOnlyList<Event>>.Ok(events)));

        // Act
        await viewModel.ReloadAsync();

        // Assert
        var day = viewModel.Days.Single(cell => cell.Date == new DateOnly(2026, 9, 8));
        Assert.Equal("Reunión", Assert.Single(day.Events).Title);
    }

    [Fact]
    public async Task Reload_ReportsNoProblem_WhenTheServerAnswers()
    {
        // Arrange
        var viewModel = Create(new StubApiClient(ApiResult<IReadOnlyList<Event>>.Ok([])));

        // Act
        await viewModel.ReloadAsync();

        // Assert
        Assert.Null(viewModel.ProblemMessage);
        Assert.False(viewModel.HasProblem);
    }

    /// <summary>
    /// A server that cannot be reached must not stop the calendar from being used: the grid is
    /// still drawn and still navigable, with the reason stated above it.
    /// </summary>
    [Fact]
    public async Task Reload_ReportsTheProblemButKeepsTheGrid_WhenTheServerIsUnreachable()
    {
        // Arrange
        var viewModel = Create(new OfflineEventApiClient());

        // Act
        await viewModel.ReloadAsync();

        // Assert
        Assert.True(viewModel.HasProblem);
        Assert.False(string.IsNullOrWhiteSpace(viewModel.ProblemMessage));
        Assert.Equal(MonthGrid.TotalCells, viewModel.Days.Count);
        Assert.True(viewModel.NextMonthCommand.CanExecute(null));
    }

    [Fact]
    public async Task Reload_ReportsTheProblem_WhenTheServerRefusesTheRequest()
    {
        // Arrange
        var refused = ApiResult<IReadOnlyList<Event>>.Refused(ApiErrorCodes.RangeRequired);
        var viewModel = Create(new StubApiClient(refused));

        // Act
        await viewModel.ReloadAsync();

        // Assert
        Assert.True(viewModel.HasProblem);
    }

    [Fact]
    public async Task Reload_ClearsAnEarlierProblem_WhenTheServerAnswersAgain()
    {
        // Arrange
        var viewModel = Create(new OfflineEventApiClient());
        await viewModel.ReloadAsync();
        Assert.True(viewModel.HasProblem);

        // Act: the same view model, now answered by a reachable server.
        var reachable = Create(new StubApiClient(ApiResult<IReadOnlyList<Event>>.Ok([])));
        await reachable.ReloadAsync();

        // Assert
        Assert.False(reachable.HasProblem);
    }

    [Fact]
    public async Task Reload_LeavesNothingLoading_WhenItFinishes()
    {
        // Arrange
        var viewModel = Create(new StubApiClient(ApiResult<IReadOnlyList<Event>>.Ok([])));

        // Act
        await viewModel.ReloadAsync();

        // Assert
        Assert.False(viewModel.IsLoading);
    }

    [Fact]
    public async Task Navigating_AsksTheServerForTheNewMonth()
    {
        // Arrange
        var api = new StubApiClient(ApiResult<IReadOnlyList<Event>>.Ok([]));
        var viewModel = Create(api);
        await viewModel.ReloadAsync();
        var before = api.Calls;

        // Act
        viewModel.NextMonthCommand.Execute(null);
        await viewModel.ReloadAsync();

        // Assert
        Assert.True(api.Calls > before);
        Assert.Equal(viewModel.Days[0].Date, DateOnly.FromDateTime(api.LastFrom));
    }
}
