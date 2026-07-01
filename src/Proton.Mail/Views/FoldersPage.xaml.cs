using Microsoft.UI.Xaml.Controls;
using Proton.Mail.ViewModels;

namespace Proton.Mail.Views;

public sealed partial class FoldersPage : Page
{
    public FoldersViewModel ViewModel { get; } = new();

    public FoldersPage()
    {
        InitializeComponent();
    }
}
