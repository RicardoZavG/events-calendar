using System;

namespace Calendar.Client.Models;

/// <summary>
/// One event as it appears inside a day cell.
/// </summary>
/// <remarks>
/// A projection rather than the event itself: the grid needs a label and something to identify
/// what was clicked, not the whole record. An event spanning several days produces one of these
/// per day it covers.
/// </remarks>
public sealed record EventChip
{
    /// <summary>Identifier of the event this stands for, so a click can open it.</summary>
    public required Guid EventId { get; init; }

    /// <summary>The event's title.</summary>
    public required string Title { get; init; }

    /// <summary>
    /// Start time, on the day the event begins; <c>null</c> on the days it merely continues
    /// through, where repeating a time that already passed would be misleading.
    /// </summary>
    /// <remarks>
    /// Not drawn in the cell. At seven columns wide a Spanish time takes eleven characters and
    /// leaves the title unreadable, and the day panel shows the real times anyway. It survives
    /// here because the tooltip is built from it.
    /// </remarks>
    public string? TimeLabel { get; init; }

    /// <summary>What the chip says on hover: the time, when there is one, and the title.</summary>
    public string Tooltip => TimeLabel is null ? Title : $"{TimeLabel} — {Title}";

    /// <summary>Whether this day is one the event runs through rather than starts on.</summary>
    public required bool IsContinuation { get; init; }
}
