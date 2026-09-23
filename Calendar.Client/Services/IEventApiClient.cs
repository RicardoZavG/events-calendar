using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Calendar.Shared.Contracts;
using Calendar.Shared.Models;

namespace Calendar.Client.Services;

/// <summary>
/// Reaches the events API on the machine acting as the central server.
/// </summary>
/// <remarks>
/// No call throws. An unreachable server is an ordinary state on a LAN, not an exceptional one,
/// and every outcome — including that one — comes back as an <see cref="ApiResult{T}"/>.
/// </remarks>
public interface IEventApiClient
{
    /// <summary>Asks for every event overlapping a window of time.</summary>
    /// <param name="fromUtc">Start of the window, inclusive.</param>
    /// <param name="toUtc">End of the window, exclusive.</param>
    /// <param name="cancellationToken">Abandons the call when the window moves on.</param>
    /// <returns>The events ordered by start, or the reason the call did not succeed.</returns>
    Task<ApiResult<IReadOnlyList<Event>>> GetEventsAsync(
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken);

    /// <summary>Creates an event.</summary>
    /// <param name="request">The event to create. Its identifier is assigned by the server.</param>
    /// <param name="cancellationToken">Abandons the call if the caller goes away.</param>
    /// <returns>The stored event, or the reason it was not stored.</returns>
    Task<ApiResult<Event>> CreateEventAsync(
        EventRequest request,
        CancellationToken cancellationToken);

    /// <summary>Replaces an event with a new version of itself.</summary>
    /// <param name="id">Identifier of the event to replace.</param>
    /// <param name="request">Its new contents, in full.</param>
    /// <param name="cancellationToken">Abandons the call if the caller goes away.</param>
    /// <returns>The updated event, or the reason it was not updated.</returns>
    Task<ApiResult<Event>> UpdateEventAsync(
        Guid id,
        EventRequest request,
        CancellationToken cancellationToken);

    /// <summary>Deletes an event.</summary>
    /// <param name="id">Identifier of the event.</param>
    /// <param name="cancellationToken">Abandons the call if the caller goes away.</param>
    /// <returns>
    /// A successful result when it is gone, or the reason it is not. An event that was already
    /// deleted comes back as <see cref="ApiErrorCodes.EventNotFound"/>, which the caller may
    /// well decide to treat as success.
    /// </returns>
    Task<ApiResult<bool>> DeleteEventAsync(Guid id, CancellationToken cancellationToken);
}
