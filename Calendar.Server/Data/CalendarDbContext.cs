using Calendar.Shared.Contracts;
using Calendar.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Calendar.Server.Data;

/// <summary>
/// The SQLite database holding every event on this LAN.
/// </summary>
public sealed class CalendarDbContext(DbContextOptions<CalendarDbContext> options)
    : DbContext(options)
{
    /// <summary>Every stored event.</summary>
    public DbSet<Event> Events => Set<Event>();

    /// <summary>Maps the shared <see cref="Event"/> model onto the database.</summary>
    /// <param name="modelBuilder">Builder the provider hands in while building the model.</param>
    /// <remarks>
    /// SQLite has no date type, so instants are stored as text and come back with their kind
    /// lost. The converter below writes them as UTC and restores that kind on read; without it
    /// a value saved as UTC would be read as unspecified and quietly compared as local time.
    /// </remarks>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var utcConverter = new ValueConverter<DateTime, DateTime>(
            value => value.ToUniversalTime(),
            value => DateTime.SpecifyKind(value, DateTimeKind.Utc));

        modelBuilder.Entity<Event>(entity =>
        {
            entity.HasKey(item => item.Id);

            entity.Property(item => item.Title)
                .IsRequired()
                .HasMaxLength(EventLimits.TitleMaxLength);

            entity.Property(item => item.Description)
                .HasMaxLength(EventLimits.DescriptionMaxLength);

            entity.Property(item => item.Start).HasConversion(utcConverter);
            entity.Property(item => item.End).HasConversion(utcConverter);
            entity.Property(item => item.CreatedAt).HasConversion(utcConverter);

            // The month view always asks for a window of time, so that is what is indexed.
            entity.HasIndex(item => new { item.Start, item.End });
        });
    }
}
