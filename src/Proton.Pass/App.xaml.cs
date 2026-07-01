using Microsoft.UI.Xaml;
using Proton.Core.Auth;
using Proton.Core.Http;
using Proton.Core.Security;

namespace Proton.Pass;

public partial class App : Application
{
    private Window? _window;

    public App()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Shared Proton.Core services for this app instance. Kept as simple app-level singletons for
    /// now rather than a full DI container - pages resolve what they need through this until a
    /// proper composition root is introduced.
    /// </summary>
    public static AuthSession Session { get; } = new();

    public static ProtonApiClient ApiClient { get; } = new(
        new ProtonApiOptions { AppVersion = "windows-pass@0.1.0" }, Session);

    public static AuthService AuthService { get; } = new(ApiClient, Session);

    public static ITokenStore TokenStore { get; } = new CredentialManagerStore();

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _window = new MainWindow();
        _window.Activate();
    }
}
