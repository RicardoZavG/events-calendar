namespace Calendar.Shared.Contracts;

/// <summary>
/// The envelope every endpoint answers with, so a client parses one shape instead of guessing
/// from the status code whether a body carries data or an error.
/// </summary>
/// <typeparam name="T">Type of the payload carried on success.</typeparam>
public sealed record ApiResponse<T>
{
    /// <summary>Whether the request was fulfilled.</summary>
    public required bool Success { get; init; }

    /// <summary>The payload; <c>null</c> when the request failed or returns nothing.</summary>
    public T? Data { get; init; }

    /// <summary>
    /// An <see cref="ApiErrorCodes"/> entry naming why the request failed; <c>null</c> when it
    /// succeeded.
    /// </summary>
    /// <remarks>
    /// A code, never a sentence. The server does not know what language the person in front of
    /// a client reads, and wording baked into a response cannot be translated by the side that
    /// actually displays it.
    /// </remarks>
    public string? Error { get; init; }

    /// <summary>Builds a successful response.</summary>
    /// <param name="data">The payload to carry.</param>
    public static ApiResponse<T> Ok(T? data) => new() { Success = true, Data = data };

    /// <summary>Builds a failed response.</summary>
    /// <param name="errorCode">The <see cref="ApiErrorCodes"/> entry naming the reason.</param>
    public static ApiResponse<T> Fail(string errorCode) =>
        new() { Success = false, Error = errorCode };
}
