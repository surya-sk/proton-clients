using Microsoft.UI.Xaml.Controls;
using Proton.Pass.Models;
using Proton.Pass.ViewModels;

namespace Proton.Pass.Views;

public sealed partial class VaultListPage : Page
{
    public VaultListViewModel ViewModel { get; } = new();

    public VaultListPage()
    {
        InitializeComponent();
    }

    private void ItemListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ItemListView.SelectedItem is PassItem item)
        {
            Frame.Navigate(typeof(ItemDetailPage), item.Id);
        }
    }
}
