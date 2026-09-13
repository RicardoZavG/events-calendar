using Calendar.Shared.Contracts;
using Microsoft.AspNetCore.Diagnostics;

namespace Calendar.Server.Errors;

/// <summary>
/// Turns any failure the endpoints did not foresee into the same response shape as everything
/// else.
/// </summary>
public static class ExceptionHandling
{
    /// <summary>Installs the handler that catches whatever the endpoints let through.</summary>
    /// <param name="app">The application to install it on.</param>
    /// <returns>The same application, so calls can be chained.</returns>
    /// <remarks>
    /// Validation failures are answered by the endpoints themselves, and a body the framework
    /// cannot parse is answered by the framework as a 400. This covers what is left — a locked
    /// database, a full disk, a defect — all of which really are this server's fault. The
    /// detail is written to the log, where an operator can reach it, and the caller gets a
    /// generic message: an exception carries file paths, SQL and stack frames that have no
    /// business crossing the network.
    /// <para>
    /// In Development the framework's own diagnostic page runs first and still shows the full
    /// detail, which is the point of that environment.
    /// </para>
    /// </remarks>
    public static WebApplication UseApiExceptionHandler(this WebApplication app)
    {
        app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
        {
            var failure = context.Features.Get<IExceptionHandlerFeature>()?.Error;

            var logger = context.RequestServices
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger(nameof(ExceptionHandling));

            logger.LogError(
                failure,
                "Unhandled failure processing {Method} {Path}",
                context.Request.Method,
                context.Request.Path);

            await WriteAsync(
                context,
                StatusCodes.Status500InternalServerError,
                "Ocurrió un error inesperado en el servidor.");
        }));

        return app;
    }

    /// <summary>Writes the failure in the same envelope every endpoint answers with.</summary>
    /// <param name="context">The request being answered.</param>
    /// <param name="statusCode">Status to answer with.</param>
    /// <param name="message">
    /// What went wrong, phrased for the caller and carrying no internal detail: an exception
    /// holds file paths, SQL and stack frames that have no business crossing the network.
    /// </param>
    private static async Task WriteAsync(HttpContext context, int statusCode, string message)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        await context.Response.WriteAsJsonAsync(ApiResponse<object>.Fail(message));
    }
}
