namespace Proton.Core.Auth.Srp;

/// <summary>Client-side output of an SRP exchange, ready to send to POST /auth/v4.</summary>
public sealed class SrpProofs
{
    public required byte[] ClientEphemeral { get; init; }
    public required byte[] ClientProof { get; init; }
    public required byte[] ExpectedServerProof { get; init; }

    /// <summary>The raw shared session secret (not sent to the server).</summary>
    public required byte[] SharedSession { get; init; }
}
