using Microsoft.UI.Xaml.Controls;
using Proton.Mail.ViewModels;

namespace Proton.Mail.Views;

public sealed partial class ComposePage : Page
{
    public ComposeViewModel ViewModel { get; } = new();

    public ComposePage()
    {
        InitializeComponent();
    }
}
