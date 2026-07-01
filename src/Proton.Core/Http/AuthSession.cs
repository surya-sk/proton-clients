namespace Proton.Core.Http;

/// <summary>
/// Mutable holder for the tokens a logged-in <see cref="ProtonApiClient"/> attaches to every
/// request. Updated in place by <c>AuthService</c> after login and after each token refresh so
/// the API client always sees the latest tokens without needing to be re-constructed.
/// </summary>
public sealed class AuthSession
{
    public string? Uid { get; set; }
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }

    public bool IsAuthenticated => !string.IsNullOrEmpty(Uid) && !string.IsNullOrEmpty(AccessToken);

    public void Clear()
    {
        Uid = null;
        AccessToken = null;
        RefreshToken = null;
    }
}
