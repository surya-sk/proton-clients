using Microsoft.UI.Xaml.Controls;
using Proton.Calendar.ViewModels;

namespace Proton.Calendar.Views;

public sealed partial class WeekPage : Page
{
    public WeekViewModel ViewModel { get; } = new();

    public WeekPage()
    {
        InitializeComponent();
    }
}
