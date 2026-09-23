using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Calendar.Shared.Contracts;
using Calendar.Shared.Models;

namespace Calendar.Client.Services;

/// <summary>
/// Talks to the events API over HTTP.
/// </summary>
/// <param name="httpClient">The transport. Carries no base address of its own.</param>
/// <param name="serverAddress">
/// Where the server is, read on every call rather than captured once: the user can change it in
/// the settings while the application is running, and the next request has to go to the new one.
/// </param>
/// <remarks>
/// Every failure is turned into an <see cref="ApiResult{T}"/> rather than an exception, and the
/// two kinds are kept apart: a call that never arrived, and a request the server refused.
/// </remarks>
public sealed class EventApiClient(HttpClient httpClient, Func<string> serverAddress)
    : IEventApiClient
{
    /// <summary>Format that the server parses the same way whatever its culture is.</summary>
    private const string InstantFormat = "yyyy-MM-ddTHH:mm:ssZ";

    /// <inheritdoc />
    public Task<ApiResult<IReadOnlyList<Event>>> GetEventsAsync(
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken)
    {
        var path = $"events?from={Format(fromUtc)}&to={Format(toUtc)}";

        return CallAsync<List<Event>, IReadOnlyList<Event>>(
            () => new HttpRequestMessage(HttpMethod.Get, path),
            data => data ?? [],
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<ApiResult<Event>> CreateEventAsync(
        EventRequest request,
        CancellationToken cancellationToken)
    {
        return CallAsync<Event, Event>(
            () => Json(HttpMethod.Post, "events", request),
            data => data!,
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<ApiResult<Event>> UpdateEventAsync(
        Guid id,
        EventRequest request,
        CancellationToken cancellationToken)
    {
        return CallAsync<Event, Event>(
            () => Json(HttpMethod.Put, $"events/{id}", request),
            data => data!,
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<ApiResult<bool>> DeleteEventAsync(Guid id, CancellationToken cancellationToken)
    {
        // Delete answers with the envelope and an empty payload, so success is the envelope
        // saying so rather than anything in the body.
        return CallAsync<Event, bool>(
            () => new HttpRequestMessage(HttpMethod.Delete, $"events/{id}"),
            _ => true,
            cancellationToken);
    }

    /// <summary>Performs a call, with one retry when the first attempt times out.</summary>
    /// <typeparam name="TBody">Type the server's payload deserializes into.</typeparam>
    /// <typeparam name="TResult">Type the caller receives.</typeparam>
    /// <param name="buildRequest">
    /// Builds the request. A factory rather than a single message because a request that has
    /// been sent cannot be sent again, and the retry needs a fresh one.
    /// </param>
    /// <param name="project">Turns the payload into what the caller asked for.</param>
    /// <param name="cancellationToken">Abandons the call if the caller goes away.</param>
    /// <returns>The projected payload, or the reason the call did not produce one.</returns>
    /// <remarks>
    /// A timeout is the one failure a second attempt usually clears: the first request of a
    /// session pays for the connection and for the server compiling its first query, and that
    /// cost is never paid again. It is also safe to repeat — a server that is switched off
    /// refuses the connection immediately instead of timing out, so this costs nothing in the
    /// case it would actually hurt.
    /// </remarks>
    private async Task<ApiResult<TResult>> CallAsync<TBody, TResult>(
        Func<HttpRequestMessage> buildRequest,
        Func<TBody?, TResult> project,
        CancellationToken cancellationToken)
    {
        var result = await SendAsync(buildRequest, project, cancellationToken);

        if (result.ErrorCode == ClientErrorCodes.RequestTimedOut
            && !cancellationToken.IsCancellationRequested)
        {
            return await SendAsync(buildRequest, project, cancellationToken);
        }

        return result;
    }

    /// <summary>Performs one attempt.</summary>
    /// <typeparam name="TBody">Type the server's payload deserializes into.</typeparam>
    /// <typeparam name="TResult">Type the caller receives.</typeparam>
    /// <param name="buildRequest">Builds the request to send.</param>
    /// <param name="project">Turns the payload into what the caller asked for.</param>
    /// <param name="cancellationToken">Abandons the call if the caller goes away.</param>
    /// <returns>The outcome of this attempt.</returns>
    private async Task<ApiResult<TResult>> SendAsync<TBody, TResult>(
        Func<HttpRequestMessage> buildRequest,
        Func<TBody?, TResult> project,
        CancellationToken cancellationToken)
    {
        using var request = buildRequest();

        if (!TryResolve(request))
        {
            // The configured address is not a usable URL at all; nothing can be sent.
            return ApiResult<TResult>.Unreachable(ClientErrorCodes.ServerUnreachable);
        }

        try
        {
            var response = await httpClient.SendAsync(request, cancellationToken);

            var body = await response.Content
                .ReadFromJsonAsync<ApiResponse<TBody>>(cancellationToken);

            if (body is null)
            {
                return ApiResult<TResult>.Unreachable(ClientErrorCodes.ResponseUnreadable);
            }

            if (!body.Success)
            {
                return ApiResult<TResult>.Refused(body.Error ?? ApiErrorCodes.ServerUnexpected);
            }

            return ApiResult<TResult>.Ok(project(body.Data));
        }
        catch (HttpRequestException)
        {
            // Nothing listening, no route to the host, or the connection was refused.
            return ApiResult<TResult>.Unreachable(ClientErrorCodes.ServerUnreachable);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // Cancellation the caller did not ask for is this client's own timeout.
            return ApiResult<TResult>.Unreachable(ClientErrorCodes.RequestTimedOut);
        }
        catch (JsonException)
        {
            // Something answered, but not this API — usually the address points elsewhere.
            return ApiResult<TResult>.Unreachable(ClientErrorCodes.ResponseUnreadable);
        }
        catch (NotSupportedException)
        {
            // An answer in a content type this client cannot read.
            return ApiResult<TResult>.Unreachable(ClientErrorCodes.ResponseUnreadable);
        }
    }

    /// <summary>Builds a request carrying a JSON body.</summary>
    /// <param name="method">The verb to use.</param>
    /// <param name="path">The path, without a leading slash.</param>
    /// <param name="payload">The object to send.</param>
    /// <returns>The request, with its body serialized as UTF-8 JSON.</returns>
    private static HttpRequestMessage Json(HttpMethod method, string path, EventRequest payload) =>
        new(method, path) { Content = JsonContent.Create(payload) };

    /// <summary>Turns the request's relative path into an absolute address.</summary>
    /// <param name="request">The request being prepared.</param>
    /// <returns>
    /// <c>false</c> when the configured address is empty or not a valid URL, so a typo in the
    /// settings is reported like any other unreachable server rather than crashing the client.
    /// </returns>
    private bool TryResolve(HttpRequestMessage request)
    {
        var root = serverAddress()?.Trim();

        if (string.IsNullOrEmpty(root))
        {
            return false;
        }

        if (!Uri.TryCreate($"{root.TrimEnd('/')}/{request.RequestUri}", UriKind.Absolute, out var uri))
        {
            return false;
        }

        request.RequestUri = uri;
        return true;
    }

    /// <summary>Renders an instant the way the API expects it.</summary>
    /// <param name="value">The instant, already in UTC.</param>
    /// <returns>An ISO-8601 string with the trailing Z, free of any local formatting.</returns>
    private static string Format(DateTime value) =>
        value.ToUniversalTime().ToString(InstantFormat, CultureInfo.InvariantCulture);
}
