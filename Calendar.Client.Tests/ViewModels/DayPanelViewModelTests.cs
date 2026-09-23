using Calendar.Client.Models;
using Calendar.Client.Services;
using Calendar.Client.ViewModels;
using Calendar.Shared.Contracts;
using Calendar.Shared.Models;

namespace Calendar.Client.Tests.ViewModels;

/// <summary>
/// Covers the day panel: what it sends, what it refuses to send, and what it does with the
/// answer.
/// </summary>
public sealed class DayPanelViewModelTests
{
    private static readonly DateOnly Day = new(2026, 9, 8);
    private static readonly TimeZoneInfo Zone = TimeZoneInfo.Utc;

    /// <summary>Records what it was asked to do, and answers as it was told to.</summary>
    private sealed class RecordingApiClient : IEventApiClient
    {
        public bool Succeeds { get; init; } = true;

        public string ErrorCode { get; init; } = ApiErrorCodes.ServerUnexpected;

        public EventRequest? Created { get; private set; }

        public EventRequest? Updated { get; private set; }

        public Guid? UpdatedId { get; private set; }

        public Guid? Deleted { get; private set; }

        public Task<ApiResult<IReadOnlyList<Event>>> GetEventsAsync(
            DateTime fromUtc,
            DateTime toUtc,
            CancellationToken cancellationToken) =>
            Task.FromResult(ApiResult<IReadOnlyList<Event>>.Ok([]));

        public Task<ApiResult<Event>> CreateEventAsync(
            EventRequest request,
            CancellationToken cancellationToken)
        {
            Created = request;
            return Task.FromResult(Answer(BuildEvent("stored")));
        }

        public Task<ApiResult<Event>> UpdateEventAsync(
            Guid id,
            EventRequest request,
            CancellationToken cancellationToken)
        {
            UpdatedId = id;
            Updated = request;
            return Task.FromResult(Answer(BuildEvent("stored")));
        }

        public Task<ApiResult<bool>> DeleteEventAsync(
            Guid id,
            CancellationToken cancellationToken)
        {
            Deleted = id;
            return Task.FromResult(Succeeds
                ? ApiResult<bool>.Ok(true)
                : ApiResult<bool>.Refused(ErrorCode));
        }

        private ApiResult<Event> Answer(Event stored) =>
            Succeeds ? ApiResult<Event>.Ok(stored) : ApiResult<Event>.Refused(ErrorCode);
    }

    private static Event BuildEvent(
        string title,
        int startHour = 15,
        int endHour = 16,
        Guid? id = null) => new()
    {
        Id = id ?? Guid.NewGuid(),
        Title = title,
        Description = "Detalle",
        Start = new DateTime(2026, 9, 8, startHour, 0, 0, DateTimeKind.Utc),
        End = new DateTime(2026, 9, 8, endHour, 0, 0, DateTimeKind.Utc),
        CreatedAt = DateTime.UtcNow,
    };

    private static DayPanelViewModel Create(
        IEventApiClient api,
        Action<int>? onRefresh = null,
        Action? onClose = null)
    {
        var refreshes = 0;
        return new DayPanelViewModel(
            Day,
            api,
            Zone,
            () =>
            {
                onRefresh?.Invoke(++refreshes);
                return Task.CompletedTask;
            },
            onClose ?? (() => { }));
    }

    [Fact]
    public void Show_ListsTheEventsOfTheDayOrderedByStart()
    {
        // Arrange
        var panel = Create(new RecordingApiClient());

        // Act
        panel.Show([BuildEvent("Tarde", 17, 18), BuildEvent("Mañana", 9, 10)]);

        // Assert
        Assert.Equal(["Mañana", "Tarde"], panel.Events.Select(row => row.Title));
        Assert.False(panel.IsEmpty);
    }

    [Fact]
    public void Show_ReportsAnEmptyDay()
    {
        // Arrange & Act
        var panel = Create(new RecordingApiClient());

        // Assert
        Assert.True(panel.IsEmpty);
    }

    /// <summary>
    /// The common case is typing a title and saving, so the times are already filled in.
    /// </summary>
    [Fact]
    public void New_OpensAnEmptyFormWithAnHourAlreadyChosen()
    {
        // Arrange
        var panel = Create(new RecordingApiClient());

        // Act
        panel.NewCommand.Execute(null);

        // Assert
        Assert.True(panel.IsEditing);
        Assert.True(panel.IsCreating);
        Assert.Equal(string.Empty, panel.DraftTitle);
        Assert.NotNull(panel.DraftStartTime);
        Assert.NotNull(panel.DraftEndTime);
    }

    [Fact]
    public void New_DefaultsToAnHourLong()
    {
        // Arrange
        var panel = Create(new RecordingApiClient());

        // Act
        panel.NewCommand.Execute(null);

        // Assert
        Assert.Equal(
            TimeSpan.FromHours(1),
            panel.DraftEndDate!.Value.Date + panel.DraftEndTime!.Value
            - (panel.DraftStartDate!.Value.Date + panel.DraftStartTime!.Value));
    }

    [Fact]
    public void Edit_FillsTheFormFromTheEvent()
    {
        // Arrange
        var panel = Create(new RecordingApiClient());
        panel.Show([BuildEvent("Reunión")]);

        // Act
        panel.EditCommand.Execute(panel.Events[0]);

        // Assert
        Assert.True(panel.IsEditing);
        Assert.False(panel.IsCreating);
        Assert.Equal("Reunión", panel.DraftTitle);
        Assert.Equal("Detalle", panel.DraftDescription);
    }

    [Fact]
    public void Cancel_ClosesTheFormWithoutSending()
    {
        // Arrange
        var api = new RecordingApiClient();
        var panel = Create(api);
        panel.NewCommand.Execute(null);

        // Act
        panel.CancelCommand.Execute(null);

        // Assert
        Assert.False(panel.IsEditing);
        Assert.Null(api.Created);
    }

    [Fact]
    public async Task Save_SendsANewEvent()
    {
        // Arrange
        var api = new RecordingApiClient();
        var panel = Create(api);
        panel.NewCommand.Execute(null);
        panel.DraftTitle = "Reunión";

        // Act
        await panel.SaveAsync();

        // Assert
        Assert.Equal("Reunión", api.Created!.Title);
        Assert.Null(api.Updated);
        Assert.False(panel.IsEditing);
    }

    [Fact]
    public async Task Save_ReplacesTheEventBeingEdited()
    {
        // Arrange
        var api = new RecordingApiClient();
        var panel = Create(api);
        var stored = BuildEvent("Original");
        panel.Show([stored]);
        panel.EditCommand.Execute(panel.Events[0]);
        panel.DraftTitle = "Cambiado";

        // Act
        await panel.SaveAsync();

        // Assert
        Assert.Equal(stored.Id, api.UpdatedId);
        Assert.Equal("Cambiado", api.Updated!.Title);
        Assert.Null(api.Created);
    }

    [Fact]
    public async Task Save_RefreshesTheMonth()
    {
        // Arrange
        var refreshes = 0;
        var panel = Create(new RecordingApiClient(), onRefresh: count => refreshes = count);
        panel.NewCommand.Execute(null);
        panel.DraftTitle = "Reunión";

        // Act
        await panel.SaveAsync();

        // Assert: the grid behind must never disagree with the panel.
        Assert.Equal(1, refreshes);
    }

    /// <summary>
    /// A round trip to be told the title is empty is a round trip wasted, and the rules are the
    /// same object the server uses.
    /// </summary>
    [Fact]
    public async Task Save_IsRefusedLocally_WhenTheTitleIsBlank()
    {
        // Arrange
        var api = new RecordingApiClient();
        var panel = Create(api);
        panel.NewCommand.Execute(null);
        panel.DraftTitle = "   ";

        // Act
        await panel.SaveAsync();

        // Assert
        Assert.Null(api.Created);
        Assert.False(string.IsNullOrWhiteSpace(panel.ErrorMessage));
        Assert.True(panel.IsEditing);
    }

    [Fact]
    public async Task Save_IsRefusedLocally_WhenTheEndIsNotAfterTheStart()
    {
        // Arrange
        var api = new RecordingApiClient();
        var panel = Create(api);
        panel.NewCommand.Execute(null);
        panel.DraftTitle = "Reunión";
        panel.DraftEndTime = panel.DraftStartTime;
        panel.DraftEndDate = panel.DraftStartDate;

        // Act
        await panel.SaveAsync();

        // Assert
        Assert.Null(api.Created);
        Assert.False(string.IsNullOrWhiteSpace(panel.ErrorMessage));
    }

    [Fact]
    public async Task Save_ShowsWhatTheServerSaid_WhenItRefuses()
    {
        // Arrange
        var api = new RecordingApiClient
        {
            Succeeds = false,
            ErrorCode = ApiErrorCodes.EventTitleTooLong,
        };
        var panel = Create(api);
        panel.NewCommand.Execute(null);
        panel.DraftTitle = "Reunión";

        // Act
        await panel.SaveAsync();

        // Assert: the form stays open so the work is not lost.
        Assert.True(panel.IsEditing);
        Assert.False(string.IsNullOrWhiteSpace(panel.ErrorMessage));
    }

    [Fact]
    public void AskDelete_AsksBeforeRemovingAnything()
    {
        // Arrange
        var api = new RecordingApiClient();
        var panel = Create(api);
        panel.Show([BuildEvent("Reunión")]);

        // Act
        panel.AskDeleteCommand.Execute(panel.Events[0]);

        // Assert
        Assert.True(panel.IsConfirmingDelete);
        Assert.Null(api.Deleted);
    }

    [Fact]
    public void CancelDelete_LeavesTheEventAlone()
    {
        // Arrange
        var api = new RecordingApiClient();
        var panel = Create(api);
        panel.Show([BuildEvent("Reunión")]);
        panel.AskDeleteCommand.Execute(panel.Events[0]);

        // Act
        panel.CancelDeleteCommand.Execute(null);

        // Assert
        Assert.False(panel.IsConfirmingDelete);
        Assert.Null(api.Deleted);
    }

    [Fact]
    public async Task ConfirmDelete_RemovesTheEventAndRefreshes()
    {
        // Arrange
        var api = new RecordingApiClient();
        var refreshes = 0;
        var panel = Create(api, onRefresh: count => refreshes = count);
        var stored = BuildEvent("Reunión");
        panel.Show([stored]);
        panel.AskDeleteCommand.Execute(panel.Events[0]);

        // Act
        await panel.ConfirmDeleteAsync();

        // Assert
        Assert.Equal(stored.Id, api.Deleted);
        Assert.False(panel.IsConfirmingDelete);
        Assert.Equal(1, refreshes);
    }

    /// <summary>
    /// An event someone else already removed is gone, which is what was wanted. Reporting a
    /// failure would be true and useless.
    /// </summary>
    [Fact]
    public async Task ConfirmDelete_TreatsAnAlreadyDeletedEventAsDone()
    {
        // Arrange
        var api = new RecordingApiClient
        {
            Succeeds = false,
            ErrorCode = ApiErrorCodes.EventNotFound,
        };
        var panel = Create(api);
        panel.Show([BuildEvent("Reunión")]);
        panel.AskDeleteCommand.Execute(panel.Events[0]);

        // Act
        await panel.ConfirmDeleteAsync();

        // Assert
        Assert.False(panel.IsConfirmingDelete);
        Assert.Null(panel.ErrorMessage);
    }

    [Fact]
    public async Task ConfirmDelete_ReportsARealFailure()
    {
        // Arrange
        var api = new RecordingApiClient
        {
            Succeeds = false,
            ErrorCode = ApiErrorCodes.ServerUnexpected,
        };
        var panel = Create(api);
        panel.Show([BuildEvent("Reunión")]);
        panel.AskDeleteCommand.Execute(panel.Events[0]);

        // Act
        await panel.ConfirmDeleteAsync();

        // Assert
        Assert.True(panel.IsConfirmingDelete);
        Assert.False(string.IsNullOrWhiteSpace(panel.ErrorMessage));
    }

    [Fact]
    public void Close_DismissesThePanel()
    {
        // Arrange
        var closed = false;
        var panel = Create(new RecordingApiClient(), onClose: () => closed = true);

        // Act
        panel.CloseCommand.Execute(null);

        // Assert
        Assert.True(closed);
    }
}
