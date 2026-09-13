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
    public async Task<ApiResult<IReadOnlyList<Event>>> GetEventsAsync(
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken)
    {
        var path = $"events?from={Format(fromUtc)}&to={Format(toUtc)}";

        if (!TryBuildUri(path, out var uri))
        {
            // The configured address is not a usable URL at all; nothing can be sent.
            return ApiResult<IReadOnlyList<Event>>.Unreachable(ClientErrorCodes.ServerUnreachable);
        }

        var result = await SendAsync(uri!, cancellationToken);

        // A timeout is the one failure a second attempt usually clears: the first request of a
        // session pays for the connection and for the server compiling its first query, and
        // that cost is never paid again. It is also safe to repeat — a server that is simply
        // switched off refuses the connection immediately instead of timing out, so this costs
        // nothing in the case it would actually hurt.
        if (result.ErrorCode == ClientErrorCodes.RequestTimedOut
            && !cancellationToken.IsCancellationRequested)
        {
            return await SendAsync(uri!, cancellationToken);
        }

        return result;
    }

    /// <summary>Performs one attempt.</summary>
    /// <param name="uri">The absolute address of the call.</param>
    /// <param name="cancellationToken">Abandons the call when the window moves on.</param>
    /// <returns>The events, or the reason this attempt did not produce them.</returns>
    private async Task<ApiResult<IReadOnlyList<Event>>> SendAsync(
        Uri uri,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await httpClient.GetAsync(uri, cancellationToken);

            var body = await response.Content
                .ReadFromJsonAsync<ApiResponse<List<Event>>>(cancellationToken);

            if (body is null)
            {
                return ApiResult<IReadOnlyList<Event>>.Unreachable(
                    ClientErrorCodes.ResponseUnreadable);
            }

            if (!body.Success)
            {
                return ApiResult<IReadOnlyList<Event>>.Refused(
                    body.Error ?? ApiErrorCodes.ServerUnexpected);
            }

            return ApiResult<IReadOnlyList<Event>>.Ok(body.Data ?? []);
        }
        catch (HttpRequestException)
        {
            // Nothing listening, no route to the host, or the connection was refused.
            return ApiResult<IReadOnlyList<Event>>.Unreachable(ClientErrorCodes.ServerUnreachable);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // Cancellation the caller did not ask for is this client's own timeout.
            return ApiResult<IReadOnlyList<Event>>.Unreachable(ClientErrorCodes.RequestTimedOut);
        }
        catch (JsonException)
        {
            // Something answered, but not this API — usually the address points elsewhere.
            return ApiResult<IReadOnlyList<Event>>.Unreachable(
                ClientErrorCodes.ResponseUnreadable);
        }
        catch (NotSupportedException)
        {
            // An answer in a content type this client cannot read.
            return ApiResult<IReadOnlyList<Event>>.Unreachable(
                ClientErrorCodes.ResponseUnreadable);
        }
    }

    /// <summary>Builds the absolute address of a call from the configured server address.</summary>
    /// <param name="path">The path and query, without a leading slash.</param>
    /// <param name="uri">The resulting address, when one could be built.</param>
    /// <returns>
    /// <c>false</c> when the configured address is empty or not a valid URL, so a typo in the
    /// settings is reported like any other unreachable server rather than crashing the client.
    /// </returns>
    private bool TryBuildUri(string path, out Uri? uri)
    {
        uri = null;
        var root = serverAddress()?.Trim();

        if (string.IsNullOrEmpty(root))
        {
            return false;
        }

        return Uri.TryCreate($"{root.TrimEnd('/')}/{path}", UriKind.Absolute, out uri);
    }

    /// <summary>Renders an instant the way the API expects it.</summary>
    /// <param name="value">The instant, already in UTC.</param>
    /// <returns>An ISO-8601 string with the trailing Z, free of any local formatting.</returns>
    private static string Format(DateTime value) =>
        value.ToUniversalTime().ToString(InstantFormat, CultureInfo.InvariantCulture);
}
