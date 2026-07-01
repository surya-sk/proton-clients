namespace Proton.Calendar.Models;

/// <summary>
/// In-memory sample events shared by the Month/Week/Day view models so they show a consistent
/// picture until a real Calendar API client exists in Proton.Core.
/// </summary>
public static class SampleEventStore
{
    public static IReadOnlyList<CalendarEvent> Events { get; } = BuildSampleEvents();

    private static IReadOnlyList<CalendarEvent> BuildSampleEvents()
    {
        DateTime today = DateTime.Today;

        return new List<CalendarEvent>
        {
            new()
            {
                Id = "1",
                Title = "Team standup",
                Start = new DateTimeOffset(today.AddHours(9)),
                End = new DateTimeOffset(today.AddHours(9).AddMinutes(30)),
                Location = "Video call",
            },
            new()
            {
                Id = "2",
                Title = "Design review",
                Start = new DateTimeOffset(today.AddHours(13)),
                End = new DateTimeOffset(today.AddHours(14)),
                Location = "Room 4B",
                Description = "Review the new vault detail layout.",
            },
            new()
            {
                Id = "3",
                Title = "1:1 with manager",
                Start = new DateTimeOffset(today.AddDays(1).AddHours(11)),
                End = new DateTimeOffset(today.AddDays(1).AddHours(11).AddMinutes(30)),
            },
            new()
            {
                Id = "4",
                Title = "Release planning",
                Start = new DateTimeOffset(today.AddDays(3).AddHours(15)),
                End = new DateTimeOffset(today.AddDays(3).AddHours(16)),
                Location = "Room 2A",
            },
            new()
            {
                Id = "5",
                Title = "Dentist appointment",
                Start = new DateTimeOffset(today.AddDays(-2).AddHours(10)),
                End = new DateTimeOffset(today.AddDays(-2).AddHours(11)),
            },
        };
    }
}
