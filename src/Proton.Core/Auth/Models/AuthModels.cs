using System.Text.Json.Serialization;

namespace Proton.Core.Auth.Models;

// These mirror go-proton-api's manager_auth_types.go field-for-field (including casing) since
// Proton's API has no camelCase convention - JSON keys are the literal Go struct field names.

public sealed class AuthInfoRequest
{
    public required string Username { get; init; }
}

public sealed class AuthInfoResponse
{
    public int Version { get; init; }
    public string Modulus { get; init; } = string.Empty;
    public string ServerEphemeral { get; init; } = string.Empty;
    public string Salt { get; init; } = string.Empty;
    public string SRPSession { get; init; } = string.Empty;

    [JsonPropertyName("2FA")]
    public TwoFactorInfo TwoFA { get; init; } = new();
}

public sealed class AuthRequest
{
    public required string Username { get; init; }
    public required string ClientEphemeral { get; init; }
    public required string ClientProof { get; init; }
    public required string SRPSession { get; init; }

    [JsonPropertyName("TwoFactorCode")]
    public string? TwoFactorCode { get; init; }
}

public sealed class AuthResponse
{
    public string UserID { get; init; } = string.Empty;
    public string UID { get; init; } = string.Empty;
    public string AccessToken { get; init; } = string.Empty;
    public string RefreshToken { get; init; } = string.Empty;
    public string ServerProof { get; init; } = string.Empty;
    public string Scope { get; init; } = string.Empty;
    public PasswordMode PasswordMode { get; init; }

    [JsonPropertyName("2FA")]
    public TwoFactorInfo TwoFA { get; init; } = new();
}

public sealed class AuthRefreshRequest
{
    public required string UID { get; init; }
    public required string RefreshToken { get; init; }
    public string ResponseType { get; init; } = "token";
    public string GrantType { get; init; } = "refresh_token";
    public string RedirectURI { get; init; } = "https://protonmail.ch";
    public required string State { get; init; }

    [JsonPropertyName("AccessToken")]
    public string? AccessToken { get; init; }
}

public sealed class TwoFactorSubmitRequest
{
    public required string TwoFactorCode { get; init; }
}

public sealed class TwoFactorInfo
{
    public TwoFactorStatus Enabled { get; init; }
}

public enum TwoFactorStatus
{
    None = 0,
    Totp = 1,
    Fido2 = 2,
    Fido2AndTotp = 3,
}

public enum PasswordMode
{
    Unknown = 0,
    OnePassword = 1,
    TwoPassword = 2,
}
