using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Proton.Calendar.Models;

namespace Proton.Calendar.ViewModels;

public partial class MonthViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MonthLabel))]
    private DateOnly currentMonth = DateOnly.FromDateTime(DateTime.Today);

    public string MonthLabel => CurrentMonth.ToString("MMMM yyyy");

    public ObservableCollection<CalendarDayCell> Days { get; } = new();

    public MonthViewModel()
    {
        RebuildDays();
    }

    [RelayCommand]
    private void PreviousMonth()
    {
        CurrentMonth = CurrentMonth.AddMonths(-1);
        RebuildDays();
    }

    [RelayCommand]
    private void NextMonth()
    {
        CurrentMonth = CurrentMonth.AddMonths(1);
        RebuildDays();
    }

    [RelayCommand]
    private void Today()
    {
        CurrentMonth = DateOnly.FromDateTime(DateTime.Today);
        RebuildDays();
    }

    private void RebuildDays()
    {
        Days.Clear();

        var today = DateOnly.FromDateTime(DateTime.Today);
        var firstOfMonth = new DateOnly(CurrentMonth.Year, CurrentMonth.Month, 1);
        int leadingDays = (int)firstOfMonth.DayOfWeek;
        var gridStart = firstOfMonth.AddDays(-leadingDays);

        for (int i = 0; i < 42; i++)
        {
            DateOnly date = gridStart.AddDays(i);
            List<CalendarEvent> eventsOnDay = SampleEventStore.Events
                .Where(e => DateOnly.FromDateTime(e.Start.DateTime) == date)
                .OrderBy(e => e.Start)
                .ToList();

            Days.Add(new CalendarDayCell
            {
                Date = date,
                IsInCurrentMonth = date.Month == CurrentMonth.Month,
                IsToday = date == today,
                Events = eventsOnDay,
            });
        }
    }
}
