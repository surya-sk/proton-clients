using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Proton.Calendar.Models;

namespace Proton.Calendar.ViewModels;

public partial class DayViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DayLabel))]
    private DateOnly currentDay = DateOnly.FromDateTime(DateTime.Today);

    public string DayLabel => CurrentDay.ToString("dddd, MMMM d, yyyy");

    public ObservableCollection<CalendarEvent> Events { get; } = new();

    public DayViewModel()
    {
        RebuildEvents();
    }

    [RelayCommand]
    private void PreviousDay()
    {
        CurrentDay = CurrentDay.AddDays(-1);
        RebuildEvents();
    }

    [RelayCommand]
    private void NextDay()
    {
        CurrentDay = CurrentDay.AddDays(1);
        RebuildEvents();
    }

    private void RebuildEvents()
    {
        Events.Clear();
        foreach (CalendarEvent calendarEvent in SampleEventStore.Events
                     .Where(e => DateOnly.FromDateTime(e.Start.DateTime) == CurrentDay)
                     .OrderBy(e => e.Start))
        {
            Events.Add(calendarEvent);
        }
    }
}
