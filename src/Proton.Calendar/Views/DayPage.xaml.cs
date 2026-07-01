using Microsoft.UI.Xaml.Controls;
using Proton.Calendar.ViewModels;

namespace Proton.Calendar.Views;

public sealed partial class DayPage : Page
{
    public DayViewModel ViewModel { get; } = new();

    public DayPage()
    {
        InitializeComponent();
    }
}
