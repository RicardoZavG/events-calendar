using System.Net;
using System.Net.Http;
using Calendar.Client.Services;
using Calendar.Shared.Contracts;
using Calendar.Shared.Models;

namespace Calendar.Client.Tests.Services;

/// <summary>
/// Covers how the API client turns what comes back — or does not — into a result the calendar
/// can act on, without ever throwing at its caller.
/// </summary>
public sealed class EventApiClientTests
{
    private const string Address = "http://localhost:5000";

    private static readonly DateTime From = new(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime To = new(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>Answers each call with whatever the next entry in its script says.</summary>
    private sealed class ScriptedHandler(params Func<HttpResponseMessage>[] script)
        : HttpMessageHandler
    {
        public int Calls { get; private set; }

        public Uri? LastUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastUri = request.RequestUri;
            var step = script[Math.Min(Calls, script.Length - 1)];
            Calls++;
            return Task.FromResult(step());
        }
    }

    private static HttpResponseMessage Json(string body) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json"),
    };

    private static HttpResponseMessage Succeeds(string title) => Json(
        $$"""
        {
          "success": true,
          "data": [
            {
              "id": "af0e2077-aab0-4425-a3d3-e1d75e0b6fd5",
              "title": "{{title}}",
              "description": null,
              "start": "2026-09-08T15:00:00Z",
              "end": "2026-09-08T16:00:00Z",
              "createdAt": "2026-09-08T12:00:00Z"
            }
          ],
          "error": null
        }
        """);

    private static EventApiClient Create(HttpMessageHandler handler, string address = Address) =>
        new(new HttpClient(handler), () => address);

    private static Task<ApiResult<IReadOnlyList<Event>>> CallAsync(EventApiClient client) =>
        client.GetEventsAsync(From, To, CancellationToken.None);

    [Fact]
    public async Task GetEvents_ReturnsWhatTheServerSent()
    {
        // Arrange
        var client = Create(new ScriptedHandler(() => Succeeds("Reunión")));

        // Act
        var result = await CallAsync(client);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal("Reunión", Assert.Single(result.Data!).Title);
    }

    [Fact]
    public async Task GetEvents_SendsTheWindowAsUtc()
    {
        // Arrange
        var handler = new ScriptedHandler(() => Succeeds("Reunión"));

        // Act
        await CallAsync(Create(handler));

        // Assert: the instants must not pick up any local formatting on the way out.
        Assert.Contains("from=2026-09-01T00:00:00Z", handler.LastUri!.Query, StringComparison.Ordinal);
        Assert.Contains("to=2026-10-01T00:00:00Z", handler.LastUri.Query, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetEvents_ReportsTheServerUnreachable_WhenTheConnectionFails()
    {
        // Arrange
        var client = Create(new ScriptedHandler(() => throw new HttpRequestException()));

        // Act
        var result = await CallAsync(client);

        // Assert
        Assert.False(result.Succeeded);
        Assert.True(result.IsUnreachable);
        Assert.Equal(ClientErrorCodes.ServerUnreachable, result.ErrorCode);
    }

    /// <summary>
    /// A refusal is not the same as silence: the server was reached and said no, and the person
    /// reading it must not be told the central computer is down.
    /// </summary>
    [Fact]
    public async Task GetEvents_ReportsARefusal_WhenTheServerAnswersWithAnError()
    {
        // Arrange
        var body = $$"""
            { "success": false, "data": null, "error": "{{ApiErrorCodes.RangeRequired}}" }
            """;
        var client = Create(new ScriptedHandler(() => Json(body)));

        // Act
        var result = await CallAsync(client);

        // Assert
        Assert.False(result.Succeeded);
        Assert.False(result.IsUnreachable);
        Assert.Equal(ApiErrorCodes.RangeRequired, result.ErrorCode);
    }

    [Fact]
    public async Task GetEvents_ReportsTheAnswerUnreadable_WhenSomethingElseIsListening()
    {
        // Arrange
        var client = Create(new ScriptedHandler(() => Json("<html>not this api</html>")));

        // Act
        var result = await CallAsync(client);

        // Assert
        Assert.Equal(ClientErrorCodes.ResponseUnreadable, result.ErrorCode);
    }

    [Fact]
    public async Task GetEvents_ReportsTheServerUnreachable_WhenTheAddressIsNotAUrl()
    {
        // Arrange
        var client = Create(new ScriptedHandler(() => Succeeds("Reunión")), "no es una dirección");

        // Act
        var result = await CallAsync(client);

        // Assert: a typo in the settings must not crash the calendar.
        Assert.True(result.IsUnreachable);
        Assert.Equal(ClientErrorCodes.ServerUnreachable, result.ErrorCode);
    }

    [Fact]
    public async Task GetEvents_ReportsTheServerUnreachable_WhenNoAddressIsConfigured()
    {
        // Arrange
        var client = Create(new ScriptedHandler(() => Succeeds("Reunión")), string.Empty);

        // Act
        var result = await CallAsync(client);

        // Assert
        Assert.True(result.IsUnreachable);
    }

    /// <summary>
    /// The first request of a session pays for the connection and for the server compiling its
    /// first query. Making the user press "retry" for a cost that is never paid again is the
    /// kind of friction nobody reports as a bug and everybody resents.
    /// </summary>
    [Fact]
    public async Task GetEvents_TriesAgain_WhenTheFirstAttemptTimesOut()
    {
        // Arrange
        var handler = new ScriptedHandler(
            () => throw new TaskCanceledException(),
            () => Succeeds("Reunión"));
        var client = Create(handler);

        // Act
        var result = await CallAsync(client);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(2, handler.Calls);
    }

    [Fact]
    public async Task GetEvents_GivesUp_WhenBothAttemptsTimeOut()
    {
        // Arrange
        var handler = new ScriptedHandler(() => throw new TaskCanceledException());
        var client = Create(handler);

        // Act
        var result = await CallAsync(client);

        // Assert
        Assert.Equal(ClientErrorCodes.RequestTimedOut, result.ErrorCode);
        Assert.Equal(2, handler.Calls);
    }

    /// <summary>
    /// A connection that is refused outright is not retried: the server is off, and trying
    /// again immediately only delays telling the user so.
    /// </summary>
    [Fact]
    public async Task GetEvents_DoesNotTryAgain_WhenTheConnectionIsRefused()
    {
        // Arrange
        var handler = new ScriptedHandler(() => throw new HttpRequestException());

        // Act
        await CallAsync(Create(handler));

        // Assert
        Assert.Equal(1, handler.Calls);
    }
}
