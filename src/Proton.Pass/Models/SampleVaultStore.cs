namespace Proton.Pass.Models;

/// <summary>
/// In-memory sample vaults/items shared by the view models until a real Pass API client exists
/// in Proton.Core.
/// </summary>
public static class SampleVaultStore
{
    public static IReadOnlyList<PassVault> Vaults { get; } = new List<PassVault>
    {
        new() { Id = "personal", Name = "Personal" },
        new() { Id = "work", Name = "Work" },
    };

    public static IReadOnlyList<PassItem> Items { get; } = new List<PassItem>
    {
        new()
        {
            Id = "1",
            VaultId = "personal",
            Type = PassItemType.Login,
            Title = "Proton Account",
            Username = "jane.doe@proton.me",
            Password = "correct horse battery staple",
            Url = "https://account.proton.me",
        },
        new()
        {
            Id = "2",
            VaultId = "personal",
            Type = PassItemType.Login,
            Title = "GitHub",
            Username = "janedoe",
            Password = "9fK!2xLmQ7pR",
            Url = "https://github.com",
        },
        new()
        {
            Id = "3",
            VaultId = "personal",
            Type = PassItemType.Note,
            Title = "Wi-Fi passphrase",
            Notes = "Home router: living-room-ap / correcthorsebatterystaple",
        },
        new()
        {
            Id = "4",
            VaultId = "work",
            Type = PassItemType.Login,
            Title = "Company VPN",
            Username = "jane.doe",
            Password = "T3mp0rary!Pass9",
            Url = "https://vpn.example.com",
        },
        new()
        {
            Id = "5",
            VaultId = "work",
            Type = PassItemType.Alias,
            Title = "Newsletter alias",
            Username = "jane.newsletter@passmail.net",
        },
    };
}
