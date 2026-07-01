using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Proton.Pass.ViewModels;

namespace Proton.Pass.Views;

public sealed partial class ItemDetailPage : Page
{
    public ItemDetailViewModel ViewModel { get; } = new();

    public ItemDetailPage()
    {
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is string itemId)
        {
            ViewModel.Load(itemId);
        }
    }
}
