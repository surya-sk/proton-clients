namespace Proton.Core.Http;

public sealed class ProtonApiOptions
{
    /// <summary>Matches go-proton-api's DefaultHostURL.</summary>
    public string BaseUrl { get; init; } = "https://mail.proton.me/api";

    /// <summary>
    /// Sent as the x-pm-appversion header on every request. The API uses this to gate
    /// supported clients; production use requires a value registered with Proton
    /// (e.g. "windows-mail@1.0.0").
    /// </summary>
    public required string AppVersion { get; init; }

    public string UserAgent { get; init; } = "ProtonWinUI/1.0";
}
