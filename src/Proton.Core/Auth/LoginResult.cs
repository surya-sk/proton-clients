using Proton.Core.Auth.Models;

namespace Proton.Core.Auth;

public sealed class LoginResult
{
    public required string UserId { get; init; }
    public required PasswordMode PasswordMode { get; init; }
    public required TwoFactorStatus TwoFactorStatus { get; init; }

    public bool RequiresTwoFactor => TwoFactorStatus != TwoFactorStatus.None;
}
