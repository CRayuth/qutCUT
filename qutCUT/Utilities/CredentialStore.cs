using Windows.Security.Credentials;

namespace qutCUT.Utilities;

// Windows Credential Manager equivalent of macOS Keychain
public static class CredentialStore
{
    private const string VaultName = "qutCUT";

    public static void Save(string key, string value)
    {
        var vault = new PasswordVault();
        try { vault.Remove(vault.Retrieve(VaultName, key)); } catch { }
        vault.Add(new PasswordCredential(VaultName, key, value));
    }

    public static string? Load(string key)
    {
        try
        {
            var vault = new PasswordVault();
            var cred = vault.Retrieve(VaultName, key);
            cred.RetrievePassword();
            return cred.Password;
        }
        catch { return null; }
    }

    public static void Delete(string key)
    {
        try
        {
            var vault = new PasswordVault();
            vault.Remove(vault.Retrieve(VaultName, key));
        }
        catch { }
    }

    public static class Keys
    {
        public const string AnthropicApiKey = "anthropic_api_key";
        public const string ClerkSessionToken = "clerk_session_token";
    }
}
