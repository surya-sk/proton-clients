namespace Proton.Pass.Models;

// Placeholder view-layer models, standing in for what will eventually come from a
// Pass-specific Proton.Core API client (see proton-pass-common for the real item/vault shapes).

public enum PassItemType
{
    Login,
    Note,
    Alias,
    CreditCard,
}

public sealed class PassVault
{
    public required string Id { get; init; }
    public required string Name { get; init; }
}

public sealed class PassItem
{
    public required string Id { get; init; }
    public required string VaultId { get; init; }
    public required PassItemType Type { get; init; }
    public required string Title { get; init; }
    public string Username { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string Url { get; init; } = string.Empty;
    public string Notes { get; init; } = string.Empty;

    public string TypeLabel => Type switch
    {
        PassItemType.Login => "Login",
        PassItemType.Note => "Note",
        PassItemType.Alias => "Alias",
        PassItemType.CreditCard => "Credit card",
        _ => "Item",
    };
}
