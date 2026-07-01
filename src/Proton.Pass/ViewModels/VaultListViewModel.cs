using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Proton.Pass.Models;

namespace Proton.Pass.ViewModels;

public partial class VaultListViewModel : ObservableObject
{
    public ObservableCollection<PassVault> Vaults { get; } = new();
    public ObservableCollection<PassItem> Items { get; } = new();

    [ObservableProperty]
    private PassVault? selectedVault;

    public VaultListViewModel()
    {
        foreach (PassVault vault in SampleVaultStore.Vaults)
        {
            Vaults.Add(vault);
        }

        SelectedVault = Vaults.FirstOrDefault();
    }

    partial void OnSelectedVaultChanged(PassVault? value)
    {
        Items.Clear();
        if (value is null)
        {
            return;
        }

        foreach (PassItem item in SampleVaultStore.Items.Where(i => i.VaultId == value.Id))
        {
            Items.Add(item);
        }
    }
}
