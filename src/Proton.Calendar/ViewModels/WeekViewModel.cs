using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Proton.Calendar.Models;

namespace Proton.Calendar.ViewModels;

public partial class WeekViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WeekLabel))]
    private DateOnly weekStart = StartOfWeek(DateOnly.FromDateTime(DateTime.Today));

    public string WeekLabel => $"{WeekStart:MMM d} - {WeekStart.AddDays(6):MMM d, yyyy}";

    public ObservableCollection<CalendarDayColumn> Days { get; } = new();

    public WeekViewModel()
    {
        RebuildDays();
    }

    [RelayCommand]
    private void PreviousWeek()
    {
        WeekStart = WeekStart.AddDays(-7);
        RebuildDays();
    }

    [RelayCommand]
    private void NextWeek()
    {
        WeekStart = WeekStart.AddDays(7);
        RebuildDays();
    }

    private static DateOnly StartOfWeek(DateOnly date) => date.AddDays(-(int)date.DayOfWeek);

    private void RebuildDays()
    {
        Days.Clear();

        var today = DateOnly.FromDateTime(DateTime.Today);
        for (int i = 0; i < 7; i++)
        {
            DateOnly date = WeekStart.AddDays(i);
            List<CalendarEvent> eventsOnDay = SampleEventStore.Events
                .Where(e => DateOnly.FromDateTime(e.Start.DateTime) == date)
                .OrderBy(e => e.Start)
                .ToList();

            Days.Add(new CalendarDayColumn
            {
                Date = date,
                IsToday = date == today,
                Events = eventsOnDay,
            });
        }
    }
}
