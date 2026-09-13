using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Calendar.Shared.Models;

namespace Calendar.Client.Services;

/// <summary>
/// Reaches the events API on the machine acting as the central server.
/// </summary>
public interface IEventApiClient
{
    /// <summary>Asks for every event overlapping a window of time.</summary>
    /// <param name="fromUtc">Start of the window, inclusive.</param>
    /// <param name="toUtc">End of the window, exclusive.</param>
    /// <param name="cancellationToken">Abandons the call when the window moves on.</param>
    /// <returns>
    /// The events ordered by start, or the reason the call did not succeed. Never throws:
    /// an unreachable server is an ordinary state here, not an exceptional one.
    /// </returns>
    Task<ApiResult<IReadOnlyList<Event>>> GetEventsAsync(
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken);
}
