using Proton.Core.Http;

namespace Proton.Core.Security;

/// <summary>Persists an <see cref="AuthSession"/> so a signed-in user isn't asked to log in on every launch.</summary>
public interface ITokenStore
{
    void SaveSession(string accountKey, AuthSession session);

    AuthSession? LoadSession(string accountKey);

    void DeleteSession(string accountKey);
}
