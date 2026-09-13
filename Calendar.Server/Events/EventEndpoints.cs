using Calendar.Server.Data;
using Calendar.Shared.Contracts;
using Calendar.Shared.Models;

namespace Calendar.Server.Events;

/// <summary>
/// The REST surface for events.
/// </summary>
public static class EventEndpoints
{
    private const string BasePath = "/events";

    /// <summary>Maps every event endpoint onto the application.</summary>
    /// <param name="app">The route builder to register on.</param>
    /// <returns>The same builder, so calls can be chained.</returns>
    /// <remarks>
    /// Every endpoint answers with <see cref="ApiResponse{T}"/>, including delete, which
    /// returns <c>200</c> rather than <c>204</c>. One response shape means the client parses
    /// one thing instead of branching on the status code to know whether a body exists.
    /// </remarks>
    public static IEndpointRouteBuilder MapEventEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(BasePath);

        group.MapGet("/", ListAsync).WithName("ListEvents");
        group.MapGet("/{id:guid}", GetAsync).WithName("GetEvent");
        group.MapPost("/", CreateAsync).WithName("CreateEvent");
        group.MapPut("/{id:guid}", UpdateAsync).WithName("UpdateEvent");
        group.MapDelete("/{id:guid}", DeleteAsync).WithName("DeleteEvent");

        return app;
    }

    /// <summary>Lists the events overlapping a window of time.</summary>
    /// <param name="from">Start of the window, inclusive.</param>
    /// <param name="to">End of the window, exclusive.</param>
    /// <param name="repository">Where events are read from.</param>
    /// <param name="cancellationToken">Cancels the query if the caller goes away.</param>
    /// <returns>
    /// <c>200</c> with the matching events, or <c>400</c> when the window is not a valid range.
    /// </returns>
    private static async Task<IResult> ListAsync(
        DateTime? from,
        DateTime? to,
        IEventRepository repository,
        CancellationToken cancellationToken)
    {
        if (from is null || to is null)
        {
            return Results.BadRequest(
                ApiResponse<IReadOnlyList<Event>>.Fail("Los parámetros 'from' y 'to' son obligatorios."));
        }

        if (to <= from)
        {
            return Results.BadRequest(
                ApiResponse<IReadOnlyList<Event>>.Fail("'to' debe ser posterior a 'from'."));
        }

        var events = await repository.FindInRangeAsync(
            EventValidator.ToUtc(from.Value),
            EventValidator.ToUtc(to.Value),
            cancellationToken);

        return Results.Ok(ApiResponse<IReadOnlyList<Event>>.Ok(events));
    }

    /// <summary>Reads one event.</summary>
    /// <param name="id">Identifier of the event.</param>
    /// <param name="repository">Where events are read from.</param>
    /// <param name="cancellationToken">Cancels the query if the caller goes away.</param>
    /// <returns><c>200</c> with the event, or <c>404</c> when it does not exist.</returns>
    private static async Task<IResult> GetAsync(
        Guid id,
        IEventRepository repository,
        CancellationToken cancellationToken)
    {
        var stored = await repository.FindByIdAsync(id, cancellationToken);

        if (stored is null)
        {
            return NotFound<Event>();
        }

        return Results.Ok(ApiResponse<Event>.Ok(stored));
    }

    /// <summary>Creates an event.</summary>
    /// <param name="request">The submitted payload.</param>
    /// <param name="repository">Where the event is stored.</param>
    /// <param name="cancellationToken">Cancels the write if the caller goes away.</param>
    /// <returns>
    /// <c>201</c> with the stored event and its location, or <c>400</c> with the reason the
    /// payload was rejected.
    /// </returns>
    private static async Task<IResult> CreateAsync(
        EventRequest? request,
        IEventRepository repository,
        CancellationToken cancellationToken)
    {
        var error = EventValidator.Validate(request);

        if (error is not null)
        {
            return Results.BadRequest(ApiResponse<Event>.Fail(error));
        }

        // The identifier and the creation instant belong to the server, never to the caller.
        var created = new Event
        {
            Id = Guid.NewGuid(),
            Title = request!.Title!.Trim(),
            Description = request.Description?.Trim(),
            Start = EventValidator.ToUtc(request.Start!.Value),
            End = EventValidator.ToUtc(request.End!.Value),
            CreatedAt = DateTime.UtcNow,
        };

        await repository.CreateAsync(created, cancellationToken);

        return Results.Created($"{BasePath}/{created.Id}", ApiResponse<Event>.Ok(created));
    }

    /// <summary>Replaces an event with a new version of itself.</summary>
    /// <param name="id">Identifier of the event to replace.</param>
    /// <param name="request">The submitted payload.</param>
    /// <param name="repository">Where the event is stored.</param>
    /// <param name="cancellationToken">Cancels the write if the caller goes away.</param>
    /// <returns>
    /// <c>200</c> with the updated event, <c>400</c> when the payload is rejected, or
    /// <c>404</c> when no such event exists.
    /// </returns>
    private static async Task<IResult> UpdateAsync(
        Guid id,
        EventRequest? request,
        IEventRepository repository,
        CancellationToken cancellationToken)
    {
        var error = EventValidator.Validate(request);

        if (error is not null)
        {
            return Results.BadRequest(ApiResponse<Event>.Fail(error));
        }

        var stored = await repository.FindByIdAsync(id, cancellationToken);

        if (stored is null)
        {
            return NotFound<Event>();
        }

        // CreatedAt is carried over: it records when the event first appeared, not when it was
        // last touched.
        var updated = stored with
        {
            Title = request!.Title!.Trim(),
            Description = request.Description?.Trim(),
            Start = EventValidator.ToUtc(request.Start!.Value),
            End = EventValidator.ToUtc(request.End!.Value),
        };

        await repository.UpdateAsync(updated, cancellationToken);

        return Results.Ok(ApiResponse<Event>.Ok(updated));
    }

    /// <summary>Deletes an event.</summary>
    /// <param name="id">Identifier of the event.</param>
    /// <param name="repository">Where the event is stored.</param>
    /// <param name="cancellationToken">Cancels the write if the caller goes away.</param>
    /// <returns><c>200</c> when it was removed, or <c>404</c> when it did not exist.</returns>
    private static async Task<IResult> DeleteAsync(
        Guid id,
        IEventRepository repository,
        CancellationToken cancellationToken)
    {
        var removed = await repository.DeleteAsync(id, cancellationToken);

        if (!removed)
        {
            return NotFound<Event>();
        }

        return Results.Ok(ApiResponse<Event>.Ok(null));
    }

    /// <summary>The single "not found" answer, so every endpoint words it identically.</summary>
    /// <typeparam name="T">Payload type the caller expected.</typeparam>
    private static IResult NotFound<T>() =>
        Results.NotFound(ApiResponse<T>.Fail("El evento no existe."));
}
