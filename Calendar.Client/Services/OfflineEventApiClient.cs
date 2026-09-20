using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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
        CancellationToken cancellationToken)
    {
        return Task.FromResult(
            ApiResult<IReadOnlyList<Event>>.Unreachable(ClientErrorCodes.ServerUnreachable));
    }
}
