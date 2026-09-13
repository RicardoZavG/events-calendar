namespace Calendar.Client.Services;

/// <summary>
/// Reasons a call failed before the server could answer it.
/// </summary>
/// <remarks>
/// Separate from the server's own codes on purpose: these describe the call, not the request.
/// The server never sends them because, by definition, it was never reached.
/// </remarks>
public static class ClientErrorCodes
{
    /// <summary>The server could not be reached: off, wrong address, or blocked.</summary>
    public const string ServerUnreachable = "client.server_unreachable";

    /// <summary>The server took too long to answer.</summary>
    public const string RequestTimedOut = "client.request_timed_out";

    /// <summary>Something answered, but not something this client understands.</summary>
    /// <remarks>
    /// Usually the configured address points at a different service rather than at this one.
    /// </remarks>
    public const string ResponseUnreadable = "client.response_unreadable";
}
