using System.Security.Cryptography;
using Proton.Core.Auth.Models;
using Proton.Core.Auth.Srp;
using Proton.Core.Crypto;
using Proton.Core.Http;

namespace Proton.Core.Auth;

/// <summary>
/// Orchestrates the login/refresh flow against the Proton API, mirroring
/// go-proton-api's Manager.NewClientWithLogin (manager_auth.go): fetch auth info, run the SRP
/// exchange, verify the server's proof, then store the resulting session. Registers itself as
/// the <see cref="ProtonApiClient"/>'s 401 handler so token refresh happens transparently for
/// every other call made through the same client.
/// </summary>
public sealed class AuthService
{
    private readonly ProtonApiClient _apiClient;
    private readonly AuthSession _session;
    private readonly SrpClient _srpClient = new();

    public AuthService(ProtonApiClient apiClient, AuthSession session)
    {
        _apiClient = apiClient;
        _session = session;
        _apiClient.SetUnauthorizedHandler(RefreshAsync);
    }

    public async Task<LoginResult> LoginAsync(string username, string password, CancellationToken ct = default)
    {
        AuthInfoResponse info = await _apiClient.PostAsync<AuthInfoResponse>(
            "auth/v4/info", new AuthInfoRequest { Username = username }, requiresAuth: false, ct).ConfigureAwait(false);

        byte[] modulus = ModulusVerifier.VerifyAndDecode(info.Modulus);
        byte[]? salt = info.Version >= 3 ? Convert.FromBase64String(info.Salt) : null;
        byte[] hashedPassword = PasswordHasher.HashPassword(info.Version, password, username, salt, modulus);
        byte[] serverEphemeral = Convert.FromBase64String(info.ServerEphemeral);

        SrpProofs proofs = _srpClient.GenerateProofs(modulus, serverEphemeral, hashedPassword);

        var authRequest = new AuthRequest
        {
            Username = username,
            ClientEphemeral = Convert.ToBase64String(proofs.ClientEphemeral),
            ClientProof = Convert.ToBase64String(proofs.ClientProof),
            SRPSession = info.SRPSession,
        };

        AuthResponse auth = await _apiClient.PostAsync<AuthResponse>(
            "auth/v4", authRequest, requiresAuth: false, ct).ConfigureAwait(false);

        byte[] serverProof = Convert.FromBase64String(auth.ServerProof);
        if (!CryptographicOperations.FixedTimeEquals(serverProof, proofs.ExpectedServerProof))
        {
            // The server could not prove it knows the verifier derived from our password - treat
            // this as a potential machine-in-the-middle and refuse to store the session.
            throw new InvalidOperationException("SRP server proof verification failed.");
        }

        _session.Uid = auth.UID;
        _session.AccessToken = auth.AccessToken;
        _session.RefreshToken = auth.RefreshToken;

        return new LoginResult
        {
            UserId = auth.UserID,
            PasswordMode = auth.PasswordMode,
            TwoFactorStatus = auth.TwoFA.Enabled,
        };
    }

    /// <summary>Completes login when <see cref="LoginResult.RequiresTwoFactor"/> was true.</summary>
    public async Task SubmitTwoFactorCodeAsync(string code, CancellationToken ct = default)
    {
        await _apiClient.PostAsync<Unit>(
            "auth/v4/2fa", new TwoFactorSubmitRequest { TwoFactorCode = code }, requiresAuth: true, ct).ConfigureAwait(false);
    }

    public void Logout()
    {
        _session.Clear();
    }

    /// <summary>
    /// Refreshes the current session's tokens in place. Registered as the API client's
    /// unauthorized handler, so this also runs automatically on any 401 - callers should not
    /// normally need to invoke it directly.
    /// </summary>
    public async Task<bool> RefreshAsync(CancellationToken ct)
    {
        if (string.IsNullOrEmpty(_session.Uid) || string.IsNullOrEmpty(_session.RefreshToken))
        {
            return false;
        }

        var refreshRequest = new AuthRefreshRequest
        {
            UID = _session.Uid,
            RefreshToken = _session.RefreshToken,
            AccessToken = _session.AccessToken,
            State = Convert.ToBase64String(RandomNumberGenerator.GetBytes(24)),
        };

        try
        {
            AuthResponse auth = await _apiClient.PostAsync<AuthResponse>(
                "auth/v4/refresh", refreshRequest, requiresAuth: false, ct).ConfigureAwait(false);

            _session.AccessToken = auth.AccessToken;
            _session.RefreshToken = auth.RefreshToken;
            return true;
        }
        catch (ProtonApiException)
        {
            _session.Clear();
            return false;
        }
    }
}
