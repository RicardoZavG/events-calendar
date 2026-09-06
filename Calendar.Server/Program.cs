var builder = WebApplication.CreateBuilder(args);

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// No HTTPS redirection: the server is reached over plain HTTP on the LAN
// (see CLAUDE.md — clients connect to http://<host-ip>:5000).

app.MapGet("/health", () => Results.Ok(new { status = "ok" }))
   .WithName("GetHealth");

app.Run();
