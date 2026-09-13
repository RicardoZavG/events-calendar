using System.Net;
using System.Net.Http.Json;
using System.Text;
using Calendar.Shared.Contracts;
using Calendar.Shared.Models;

namespace Calendar.Server.Tests.Events;

/// <summary>
/// Covers the API over HTTP: status codes, validation answers and serialization, exercised the
/// way a LAN client will actually reach it.
/// </summary>
public sealed class EventEndpointsTests : IClassFixture<CalendarApiFactory>
{
    private const string SeptemberWindow =
        "/events?from=2026-09-01T00:00:00Z&to=2026-10-01T00:00:00Z";

    private readonly HttpClient _client;

    public EventEndpointsTests(CalendarApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    private static EventRequest BuildRequest(
        string title = "Reunión",
        string? description = "Revisión del sprint",
        int startHour = 15,
        int endHour = 16) => new()
    {
        Title = title,
        Description = description,
        Start = new DateTime(2026, 9, 8, startHour, 0, 0, DateTimeKind.Utc),
        End = new DateTime(2026, 9, 8, endHour, 0, 0, DateTimeKind.Utc),
    };

    private async Task<Event> CreateAsync(EventRequest? request = null)
    {
        var response = await _client.PostAsJsonAsync("/events", request ?? BuildRequest());
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<Event>>();
        return body!.Data!;
    }

    [Fact]
    public async Task Create_AnswersCreated_WithTheStoredEvent()
    {
        // Act
        var response = await _client.PostAsJsonAsync("/events", BuildRequest("Junta anual"));
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<Event>>();

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.True(body!.Success);
        Assert.Equal("Junta anual", body.Data!.Title);
        Assert.NotEqual(Guid.Empty, body.Data.Id);
    }

    [Fact]
    public async Task Create_PointsAtTheNewEvent()
    {
        // Act
        var response = await _client.PostAsJsonAsync("/events", BuildRequest());
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<Event>>();

        // Assert
        Assert.Equal($"/events/{body!.Data!.Id}", response.Headers.Location?.ToString());
    }

    /// <summary>
    /// The identifier and the creation instant belong to the server; a client cannot set them.
    /// </summary>
    [Fact]
    public async Task Create_AssignsTheIdentifierAndTheCreationInstant()
    {
        // Act
        var created = await CreateAsync();

        // Assert
        Assert.NotEqual(Guid.Empty, created.Id);
        Assert.NotEqual(default, created.CreatedAt);
    }

    [Fact]
    public async Task Create_TrimsSurroundingWhitespace()
    {
        // Act
        var created = await CreateAsync(BuildRequest("   Con espacios   "));

        // Assert
        Assert.Equal("Con espacios", created.Title);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Create_IsRejected_WhenTheTitleIsBlank(string title)
    {
        // Act
        var response = await _client.PostAsJsonAsync("/events", BuildRequest(title));
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<Event>>();

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.False(body!.Success);
        Assert.Equal(ApiErrorCodes.EventTitleRequired, body.Error);
    }

    [Fact]
    public async Task Create_IsRejected_WhenTheEndIsNotAfterTheStart()
    {
        // Act
        var response = await _client.PostAsJsonAsync(
            "/events", BuildRequest(startHour: 16, endHour: 15));
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<Event>>();

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(ApiErrorCodes.EventEndNotAfterStart, body!.Error);
    }

    [Fact]
    public async Task Create_IsRejected_WhenTheDatesAreMissing()
    {
        // Act
        var response = await _client.PostAsJsonAsync(
            "/events", new EventRequest { Title = "Sin fechas" });

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_IsRejected_WhenTheTitleIsTooLong()
    {
        // Act
        var response = await _client.PostAsJsonAsync(
            "/events", BuildRequest(new string('a', EventLimits.TitleMaxLength + 1)));
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<Event>>();

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(ApiErrorCodes.EventTitleTooLong, body!.Error);
    }

    [Fact]
    public async Task Create_IsRejected_WhenTheDescriptionIsTooLong()
    {
        // Act
        var response = await _client.PostAsJsonAsync(
            "/events",
            BuildRequest(description: new string('a', EventLimits.DescriptionMaxLength + 1)));

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// A body the framework cannot read is the caller's mistake, so it must be answered as a
    /// bad request rather than as a server failure.
    /// </summary>
    [Fact]
    public async Task Create_AnswersBadRequest_WhenTheBodyIsNotValidJson()
    {
        // Arrange
        using var content = new StringContent(
            "{ \"title\": ", Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/events", content);

        // Assert: only the status is checked. This answer comes from the framework, before any
        // of our code runs, so it does not carry the response envelope — there is no handler
        // to build one from a body that could not be read in the first place.
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Get_AnswersTheStoredEvent()
    {
        // Arrange
        var created = await CreateAsync();

        // Act
        var body = await _client.GetFromJsonAsync<ApiResponse<Event>>($"/events/{created.Id}");

        // Assert
        Assert.True(body!.Success);
        Assert.Equal(created.Id, body.Data!.Id);
    }

    [Fact]
    public async Task Get_AnswersNotFound_WhenTheEventDoesNotExist()
    {
        // Act
        var response = await _client.GetAsync($"/events/{Guid.NewGuid()}");
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<Event>>();

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.False(body!.Success);
        Assert.Equal(ApiErrorCodes.EventNotFound, body.Error);
    }

    [Fact]
    public async Task List_ReturnsTheEventsInsideTheWindow()
    {
        // Arrange
        var created = await CreateAsync(BuildRequest("En septiembre"));

        // Act
        var body = await _client.GetFromJsonAsync<ApiResponse<List<Event>>>(SeptemberWindow);

        // Assert
        Assert.True(body!.Success);
        Assert.Contains(body.Data!, item => item.Id == created.Id);
    }

    [Fact]
    public async Task List_ExcludesEventsOutsideTheWindow()
    {
        // Arrange
        await CreateAsync();

        // Act
        var body = await _client.GetFromJsonAsync<ApiResponse<List<Event>>>(
            "/events?from=2027-01-01T00:00:00Z&to=2027-02-01T00:00:00Z");

        // Assert
        Assert.Empty(body!.Data!);
    }

    [Fact]
    public async Task List_IsRejected_WhenTheWindowIsMissing()
    {
        // Act
        var response = await _client.GetAsync("/events");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task List_IsRejected_WhenTheWindowEndsBeforeItStarts()
    {
        // Act
        var response = await _client.GetAsync(
            "/events?from=2026-10-01T00:00:00Z&to=2026-09-01T00:00:00Z");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Update_ReplacesTheEvent()
    {
        // Arrange
        var created = await CreateAsync();

        // Act
        var response = await _client.PutAsJsonAsync(
            $"/events/{created.Id}", BuildRequest("Título nuevo"));
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<Event>>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Título nuevo", body!.Data!.Title);
    }

    /// <summary>
    /// The creation instant records when the event first appeared, not when it was last edited.
    /// </summary>
    [Fact]
    public async Task Update_KeepsTheOriginalCreationInstant()
    {
        // Arrange
        var created = await CreateAsync();

        // Act
        var response = await _client.PutAsJsonAsync(
            $"/events/{created.Id}", BuildRequest("Título nuevo"));
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<Event>>();

        // Assert
        Assert.Equal(created.CreatedAt, body!.Data!.CreatedAt);
    }

    [Fact]
    public async Task Update_AnswersNotFound_WhenTheEventDoesNotExist()
    {
        // Act
        var response = await _client.PutAsJsonAsync($"/events/{Guid.NewGuid()}", BuildRequest());

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Update_IsRejected_WhenThePayloadIsInvalid()
    {
        // Arrange
        var created = await CreateAsync();

        // Act
        var response = await _client.PutAsJsonAsync(
            $"/events/{created.Id}", BuildRequest(string.Empty));

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Delete_RemovesTheEvent()
    {
        // Arrange
        var created = await CreateAsync();

        // Act
        var response = await _client.DeleteAsync($"/events/{created.Id}");
        var afterwards = await _client.GetAsync($"/events/{created.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, afterwards.StatusCode);
    }

    [Fact]
    public async Task Delete_AnswersNotFound_WhenTheEventDoesNotExist()
    {
        // Act
        var response = await _client.DeleteAsync($"/events/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Instants_ComeBackAsTheSameMomentInUtc()
    {
        // Arrange
        var request = BuildRequest();

        // Act
        var created = await CreateAsync(request);

        // Assert
        Assert.Equal(request.Start!.Value, created.Start.ToUniversalTime());
        Assert.Equal(request.End!.Value, created.End.ToUniversalTime());
    }
}
