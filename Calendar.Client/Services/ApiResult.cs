namespace Calendar.Client.Services;

/// <summary>
/// The outcome of a call to the server: what came back, or why nothing did.
/// </summary>
/// <typeparam name="T">Type of the payload on success.</typeparam>
/// <remarks>
/// Returned instead of throwing. A server that cannot be reached is an ordinary state on a LAN,
/// not an exceptional one, and code that has to remember which calls throw ends up with a
/// try/catch around every one of them.
/// </remarks>
public sealed record ApiResult<T>
{
    /// <summary>Whether the server answered and the answer was usable.</summary>
    public required bool Succeeded { get; init; }

    /// <summary>The payload; <c>null</c> when the call did not succeed.</summary>
    public T? Data { get; init; }

    /// <summary>
    /// Why it failed, as an error code; <c>null</c> on success. Either an
    /// <see cref="Shared.Contracts.ApiErrorCodes"/> entry the server sent, or a
    /// <see cref="ClientErrorCodes"/> entry for a failure that never reached it.
    /// </summary>
    public string? ErrorCode { get; init; }

    /// <summary>
    /// Whether the server could not be reached at all, as opposed to answering with a refusal.
    /// </summary>
    /// <remarks>
    /// The distinction is what the person using the app needs: one means the central computer
    /// is unreachable, the other means the request itself was wrong. Reporting both the same
    /// way is how someone ends up restarting a server that was never the problem.
    /// </remarks>
    public bool IsUnreachable { get; init; }

    /// <summary>Builds a successful result.</summary>
    /// <param name="data">What the server returned.</param>
    public static ApiResult<T> Ok(T data) => new() { Succeeded = true, Data = data };

    /// <summary>Builds a result for a request the server refused.</summary>
    /// <param name="errorCode">The code the server answered with.</param>
    public static ApiResult<T> Refused(string errorCode) =>
        new() { Succeeded = false, ErrorCode = errorCode };

    /// <summary>Builds a result for a request that never got an answer.</summary>
    /// <param name="errorCode">Which way the call failed to complete.</param>
    public static ApiResult<T> Unreachable(string errorCode) =>
        new() { Succeeded = false, ErrorCode = errorCode, IsUnreachable = true };
}
