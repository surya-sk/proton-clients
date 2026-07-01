using Microsoft.UI.Xaml.Controls;
using Proton.Calendar.ViewModels;

namespace Proton.Calendar.Views;

public sealed partial class EventEditorPage : Page
{
    public EventEditorViewModel ViewModel { get; } = new();

    public EventEditorPage()
    {
        InitializeComponent();
    }
}
