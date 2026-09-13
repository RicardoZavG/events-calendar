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

    /// <summary>A message safe to show the user; <c>null</c> when the request succeeded.</summary>
    public string? Error { get; init; }

    /// <summary>Builds a successful response.</summary>
    /// <param name="data">The payload to carry.</param>
    public static ApiResponse<T> Ok(T? data) => new() { Success = true, Data = data };

    /// <summary>Builds a failed response.</summary>
    /// <param name="error">Why it failed, phrased for the person reading it.</param>
    public static ApiResponse<T> Fail(string error) => new() { Success = false, Error = error };
}
