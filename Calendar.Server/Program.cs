var builder = WebApplication.CreateBuilder(args);

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// No HTTPS redirection: this is a LAN-only service, reached over plain HTTP at
// http://<host-ip>:5000.

// Liveness probe: lets a client confirm the server is reachable before issuing real requests.
// Returns 200 with { "status": "ok" }.
app.MapGet("/health", () => Results.Ok(new { status = "ok" }))
   .WithName("GetHealth");

app.Run();
