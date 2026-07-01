using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Proton.Calendar.ViewModels;

public partial class EventEditorViewModel : ObservableObject
{
    [ObservableProperty]
    private string title = string.Empty;

    [ObservableProperty]
    private DateTimeOffset startDate = DateTimeOffset.Now.Date;

    [ObservableProperty]
    private TimeSpan startTime = RoundToNextHalfHour(DateTime.Now.TimeOfDay);

    [ObservableProperty]
    private DateTimeOffset endDate = DateTimeOffset.Now.Date;

    [ObservableProperty]
    private TimeSpan endTime = RoundToNextHalfHour(DateTime.Now.TimeOfDay) + TimeSpan.FromHours(1);

    [ObservableProperty]
    private string location = string.Empty;

    [ObservableProperty]
    private string description = string.Empty;

    [ObservableProperty]
    private bool isSaved;

    public DateTimeOffset Start => StartDate.Date + StartTime;
    public DateTimeOffset End => EndDate.Date + EndTime;

    /// <summary>
    /// Placeholder save action. Persisting a real event requires the Calendar API client
    /// (encrypted event payloads, attendee handling) that doesn't exist in Proton.Core yet.
    /// </summary>
    [RelayCommand]
    private void Save()
    {
        IsSaved = true;
    }

    [RelayCommand]
    private void Cancel()
    {
        Title = string.Empty;
        Location = string.Empty;
        Description = string.Empty;
    }

    private static TimeSpan RoundToNextHalfHour(TimeSpan time)
    {
        double minutes = Math.Ceiling(time.TotalMinutes / 30.0) * 30.0;
        return TimeSpan.FromMinutes(minutes);
    }
}
