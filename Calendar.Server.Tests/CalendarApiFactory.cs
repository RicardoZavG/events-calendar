using Calendar.Server.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Calendar.Server.Tests;

/// <summary>
/// Hosts the real application for the endpoint tests, with its database pointed at a private
/// SQLite instance.
/// </summary>
/// <remarks>
/// Only the connection is replaced. Routing, model binding, validation and JSON serialization
/// all run exactly as they do in production, which is the point of testing through HTTP rather
/// than calling the handlers directly.
/// </remarks>
public sealed class CalendarApiFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // An in-memory database lives only as long as its connection, so it is opened here and
        // held for the lifetime of the factory.
        _connection.Open();

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<CalendarDbContext>>();
            services.RemoveAll<CalendarDbContext>();
            services.AddDbContext<CalendarDbContext>(options => options.UseSqlite(_connection));
        });
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            _connection.Dispose();
        }
    }
}
