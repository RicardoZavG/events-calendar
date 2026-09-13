using Calendar.Server.Data;
using Calendar.Server.Errors;
using Calendar.Server.Events;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

// A body the framework cannot parse is the caller's mistake, not a failure of this server.
// Left at its Development default this is raised as an exception and ends up answered as a
// 500, which sends whoever sent it looking for a bug on the wrong side. Turned off, the
// framework answers 400 itself, which is what a malformed request deserves.
builder.Services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = false);

// The database file lives beside the server's content root, so the whole LAN shares the one
// copy held by the machine acting as the central server.
var connectionString = builder.Configuration.GetConnectionString("Calendar")
                       ?? "Data Source=calendar.db";

builder.Services.AddDbContext<CalendarDbContext>(options => options.UseSqlite(connectionString));
builder.Services.AddScoped<IEventRepository, EventRepository>();

var app = builder.Build();

// Installed before anything can throw, so no failure reaches the client as a stack trace.
app.UseApiExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Creates the file and schema on first run. This is not a migration: the model is still
// changing, and a migration has to be generated as a build step. Before anyone stores events
// they care about, this must become a real migration so the schema can evolve without the
// database being deleted.
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<CalendarDbContext>();
    await context.Database.EnsureCreatedAsync();
}

// No HTTPS redirection: this is a LAN-only service, reached over plain HTTP at
// http://<host-ip>:5000.
// Liveness probe: lets a client confirm the server is reachable before issuing real requests.
// Returns 200 with { "status": "ok" }.
app.MapGet("/health", () => Results.Ok(new { status = "ok" }))
   .WithName("GetHealth");

app.MapEventEndpoints();

await app.RunAsync();

/// <summary>
/// Exposed so the integration tests can host the application through
/// <c>WebApplicationFactory</c>. Top-level statements make this class internal by default.
/// </summary>
public partial class Program;
