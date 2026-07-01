using Microsoft.UI.Xaml.Controls;
using Proton.Pass.ViewModels;

namespace Proton.Pass.Views;

public sealed partial class PasswordGeneratorPage : Page
{
    public PasswordGeneratorViewModel ViewModel { get; } = new();

    public PasswordGeneratorPage()
    {
        InitializeComponent();
    }
}
