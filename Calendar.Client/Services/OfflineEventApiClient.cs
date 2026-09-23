using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Calendar.Shared.Contracts;
using Calendar.Shared.Models;

namespace Calendar.Client.Services;

/// <summary>
/// An API client that never reaches anything.
/// </summary>
/// <remarks>
/// Used where a real one cannot be supplied — the XAML previewer, which cannot pass constructor
/// arguments — and in tests that need the unreachable path without a network. It answers the
/// way a client with no server would, so nothing downstream has to special-case its absence.
/// </remarks>
public sealed class OfflineEventApiClient : IEventApiClient
{
    /// <inheritdoc />
    public Task<ApiResult<IReadOnlyList<Event>>> GetEventsAsync(
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken) => Unreachable<IReadOnlyList<Event>>();

    /// <inheritdoc />
    public Task<ApiResult<Event>> CreateEventAsync(
        EventRequest request,
        CancellationToken cancellationToken) => Unreachable<Event>();

    /// <inheritdoc />
    public Task<ApiResult<Event>> UpdateEventAsync(
        Guid id,
        EventRequest request,
        CancellationToken cancellationToken) => Unreachable<Event>();

    /// <inheritdoc />
    public Task<ApiResult<bool>> DeleteEventAsync(
        Guid id,
        CancellationToken cancellationToken) => Unreachable<bool>();

    /// <summary>The one answer this client ever gives.</summary>
    /// <typeparam name="T">Payload the caller expected.</typeparam>
    private static Task<ApiResult<T>> Unreachable<T>() =>
        Task.FromResult(ApiResult<T>.Unreachable(ClientErrorCodes.ServerUnreachable));
}
