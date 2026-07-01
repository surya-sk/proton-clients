namespace Proton.Calendar.Models;

// Placeholder view-layer models, standing in for what will eventually come from a
// Calendar-specific Proton.Core API client (see WebClients' calendar event endpoints).

public sealed class CalendarEvent
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public required DateTimeOffset Start { get; init; }
    public required DateTimeOffset End { get; init; }
    public string Location { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;

    public bool HasLocation => !string.IsNullOrWhiteSpace(Location);

    public string TimeRangeLabel => Start.Date == End.Date
        ? $"{Start:t} - {End:t}"
        : $"{Start:g} - {End:g}";
}

/// <summary>One cell in the month grid: a date plus whatever events fall on it.</summary>
public sealed class CalendarDayCell
{
    public required DateOnly Date { get; init; }
    public required bool IsInCurrentMonth { get; init; }
    public bool IsToday { get; init; }
    public IReadOnlyList<CalendarEvent> Events { get; init; } = Array.Empty<CalendarEvent>();

    public string DayNumberLabel => Date.Day.ToString();
    public double CellOpacity => IsInCurrentMonth ? 1.0 : 0.35;
}

/// <summary>One column in the week view: a date plus its agenda of events.</summary>
public sealed class CalendarDayColumn
{
    public required DateOnly Date { get; init; }
    public bool IsToday { get; init; }
    public IReadOnlyList<CalendarEvent> Events { get; init; } = Array.Empty<CalendarEvent>();

    public string HeaderLabel => Date.ToString("ddd d");
}
