using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using qutCUT.Agent.Clients;
using qutCUT.Utilities;

namespace qutCUT.Settings;

public sealed partial class SettingsView : ContentDialog
{
    private readonly AppState _state;

    public SettingsView(AppState state)
    {
        _state = state;
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var acc = _state.Account;
        EmailLabel.Text   = acc.IsSignedIn ? acc.Email : "Not signed in";
        TierLabel.Text    = acc.Tier.ToString();
        CreditsLabel.Text = $"{acc.CreditsRemaining:N0} remaining";

        var key = CredentialStore.Load(CredentialStore.Keys.AnthropicApiKey);
        if (!string.IsNullOrEmpty(key)) ApiKeyBox.Password = key;

        CachePathLabel.Text = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "qutCUT", "generated");
    }

    private void OnApiKeyChanged(object sender, RoutedEventArgs e)
    {
        var key = ApiKeyBox.Password.Trim();
        _state.Agent.SetApiKey(key);
    }

    private void OnModelChanged(object sender, SelectionChangedEventArgs e)
    {
        var model = ModelPicker.SelectedIndex switch
        {
            0 => AnthropicModel.Haiku45,
            2 => AnthropicModel.Opus48,
            _ => AnthropicModel.Sonnet46
        };
        _state.Agent.Model = model;
    }

    private void OnSignOut(object sender, RoutedEventArgs e)
    {
        _state.Account.SignOut();
        EmailLabel.Text  = "Not signed in";
        TierLabel.Text   = "Free";
        CreditsLabel.Text = "—";
    }

    private void OnClearCache(object sender, RoutedEventArgs e)
    {
        var cacheDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "qutCUT", "generated");
        try { if (Directory.Exists(cacheDir)) Directory.Delete(cacheDir, true); }
        catch { }
    }
}
