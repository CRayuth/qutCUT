using CommunityToolkit.Mvvm.ComponentModel;
using System.Net.Http.Headers;
using System.Text.Json;
using qutCUT.Utilities;

namespace qutCUT.Account;

public enum AccountTier { Free, Pro, Max }

public sealed partial class AccountService : ObservableObject
{
    [ObservableProperty] private bool _isSignedIn;
    [ObservableProperty] private string _email = string.Empty;
    [ObservableProperty] private AccountTier _tier = AccountTier.Free;
    [ObservableProperty] private int _creditsRemaining;
    [ObservableProperty] private int _creditsTotal;
    [ObservableProperty] private bool _isLoading;

    private string? _sessionToken;
    private readonly HttpClient _http = new();

    public async Task RestoreSessionAsync()
    {
        _sessionToken = CredentialStore.Load(CredentialStore.Keys.ClerkSessionToken);
        if (_sessionToken is null) return;
        await RefreshAccountAsync();
    }

    public async Task SignInAsync(string email, string password)
    {
        IsLoading = true;
        try
        {
            // POST to Clerk sign-in endpoint
            var payload  = JsonSerializer.Serialize(new { identifier = email, password });
            var content  = new System.Net.Http.StringContent(payload, System.Text.Encoding.UTF8, "application/json");
            var response = await _http.PostAsync("https://api.clerk.com/v1/client/sign_ins", content);
            response.EnsureSuccessStatusCode();

            var body = await response.Content.ReadAsStringAsync();
            var doc  = JsonDocument.Parse(body);
            _sessionToken = doc.RootElement
                .GetProperty("client")
                .GetProperty("sessions")[0]
                .GetProperty("last_active_token")
                .GetProperty("jwt")
                .GetString();

            if (_sessionToken is not null)
            {
                CredentialStore.Save(CredentialStore.Keys.ClerkSessionToken, _sessionToken);
                await RefreshAccountAsync();
            }
        }
        catch (Exception ex)
        {
            Log.App.LogError(ex, "Sign-in failed");
            throw;
        }
        finally { IsLoading = false; }
    }

    public async Task RefreshAccountAsync()
    {
        if (_sessionToken is null) return;
        IsLoading = true;
        try
        {
            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _sessionToken);

            var response = await _http.GetAsync("https://api.palmier.io/account");
            if (!response.IsSuccessStatusCode) { SignOut(); return; }

            var body = await response.Content.ReadAsStringAsync();
            var doc  = JsonDocument.Parse(body);
            var root = doc.RootElement;

            Email            = root.TryGetProperty("email", out var ep) ? ep.GetString() ?? "" : "";
            CreditsRemaining = root.TryGetProperty("credits", out var cp) ? cp.GetInt32() : 0;
            CreditsTotal     = root.TryGetProperty("creditsTotal", out var tp) ? tp.GetInt32() : 0;

            Tier = root.TryGetProperty("tier", out var tierProp) ? tierProp.GetString() switch
            {
                "pro" => AccountTier.Pro,
                "max" => AccountTier.Max,
                _     => AccountTier.Free
            } : AccountTier.Free;

            IsSignedIn = true;
        }
        catch (Exception ex)
        {
            Log.App.LogError(ex, "Account refresh failed");
        }
        finally { IsLoading = false; }
    }

    public void SignOut()
    {
        CredentialStore.Delete(CredentialStore.Keys.ClerkSessionToken);
        _sessionToken    = null;
        IsSignedIn       = false;
        Email            = string.Empty;
        Tier             = AccountTier.Free;
        CreditsRemaining = 0;
        CreditsTotal     = 0;
    }

    public string AuthToken => _sessionToken ?? string.Empty;
}
