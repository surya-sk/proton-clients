using Microsoft.UI.Xaml.Controls;
using Proton.Calendar.ViewModels;

namespace Proton.Calendar.Views;

public sealed partial class MonthPage : Page
{
    public MonthViewModel ViewModel { get; } = new();

    public MonthPage()
    {
        InitializeComponent();
    }
}
