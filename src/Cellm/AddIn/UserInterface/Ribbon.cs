using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cellm.Models.Providers;
using Cellm.Models.Providers.Anthropic;
using Cellm.Models.Providers.Cellm;
using Cellm.Models.Providers.DeepSeek;
using Cellm.Models.Providers.Mistral;
using Cellm.Models.Providers.Ollama;
using Cellm.Models.Providers.OpenAi;
using Cellm.Models.Providers.OpenAiCompatible;
using Cellm.Users;
using ExcelDna.Integration.CustomUI;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cellm.AddIn.UserInterface;

[ComVisible(true)]
public class Ribbon : ExcelRibbon
{
    // Icons: https://developer.microsoft.com/en-us/fluentui#/styles/web/icons
    private IRibbonUI? _ribbonUi;

    private static readonly string _appSettingsPath = Path.Combine(CellmAddIn.ConfigurationPath, "appsettings.json");
    private static readonly string _appsettingsLocalPath = Path.Combine(CellmAddIn.ConfigurationPath, "appsettings.Local.json");

    private static bool? _cachedLoginState = null;
    private static DateTime _cacheExpiry = DateTime.MinValue;
    private static readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(5);
    private static volatile bool _isLoginCheckRunning = false;
    private const string AuthCheckUrl = "https://dev.getcellm.com/v1/up";
    private const string SignUpUrl = "https://dev.getcellm.com/signup";
    private const string ManageAccountUrl = "https://dev.getcellm.com/account";

    public Ribbon()
    {
        EnsureDefaultProvider();
        EnsureDefaultCache();
    }

    private void EnsureDefaultProvider()
    {
        try
        {
            // Verify if default provider exists
            GetValue($"{nameof(ProviderConfiguration)}:{nameof(ProviderConfiguration.DefaultProvider)}");
        }
        catch (KeyNotFoundException)
        {
            // Set default if missing
            SetValue($"{nameof(ProviderConfiguration)}:{nameof(ProviderConfiguration.DefaultProvider)}", nameof(Provider.Ollama));
        }
    }

    private void EnsureDefaultCache()
    {
        try
        {
            // Check if EnableCache exists
            GetValue($"{nameof(ProviderConfiguration)}:{nameof(ProviderConfiguration.EnableCache)}");
        }
        catch (KeyNotFoundException)
        {
            // Set default to false if missing
            SetValue($"{nameof(ProviderConfiguration)}:{nameof(ProviderConfiguration.EnableCache)}", "False");
        }
    }

    public override string GetCustomUI(string RibbonID)
    {
        return $"""
<customUI xmlns="http://schemas.microsoft.com/office/2006/01/customui" onLoad="OnLoad" loadImage="GetEmbeddedImage">
    <ribbon>
        <tabs>
            <tab id="cellm" label="Cellm">
                {UserGroup()}
                {ModelGroup()}
                {BehaviorGroup()}
            </tab>
        </tabs>
    </ribbon>
</customUI>
""";
    }

    public void OnLoad(IRibbonUI ribbonUi)
    {
        _ribbonUi = ribbonUi;
    }

    // --- MODIFIED: User Group XML ---
    public string UserGroup()
    {
        var accountConfiguration = CellmAddIn.Services.GetRequiredService<IOptionsMonitor<AccountConfiguration>>();

        if (!accountConfiguration.CurrentValue.IsEnabled)
        {
            return string.Empty;
        }

        // Removed editBoxes, added Login button to menu
        return $$"""
<group id="userGroup" label="Cellm">
    <splitButton id="userAccountSplitButton" size="large">
        <button id="userAccountButton"
                label="Account"
                getImage="GetUserAccountImage"
                getScreentip="GetUserScreentip" />
        <menu id="userAccountMenu">
             <button id="loginButton" label="Login..."
                 onAction="OnLoginClicked"
                 getEnabled="IsLoggedOut"
                 getImage="GetUserLoginImage"
                 screentip="Log in to your Cellm account." />
             <button id="logoutButton" label="Logout"
                 onAction="OnLogoutClicked"
                 getEnabled="IsLoggedIn"
                 getImage="GetUserLogoutImage"
                 screentip="Log out and clear saved credentials." />
        <menuSeparator id="accountActionSeparator" />
        <button id="accountActionButton"
            getLabel="GetAccountActionLabel"
            onAction="OnAccountActionClicked"
            getImage="GetAccountActionImage"
            getScreentip="GetAccountActionScreentip" />
         </menu>
    </splitButton>
</group>
""";
    }

    public string BehaviorGroup()
    {
        return $"""
<group id="tools" label="Tools">
    <splitButton id="Functions" size="large">
        <button id="functionsButton" label="Functions" imageMso="FunctionWizard" screentip="Enable/disable built-in functions" />
        <menu id="functionsMenu">
            <checkBox id="filesearch" label="File Search" 
                 screentip="Lets a model specify glob patterns and get back matching file paths."
                 onAction="OnFileSearchToggled"
                 getPressed="OnGetFileSearchPressed" />
        <checkBox id="filereader" label="File Reader" 
                 screentip="Lets a model specify a file path and get back its content as plain text. Supports PDF, Markdown, and common text formats."
                 onAction="OnFileReaderToggled"
                 getPressed="OnGetFileReaderPressed" />
         </menu>
    </splitButton>
</group>
""";
    }

    public string ModelGroup()
    {
        var providerAndModels = new List<string>();

        var accountConfiguration = CellmAddIn.Services.GetRequiredService<IOptionsMonitor<AccountConfiguration>>().CurrentValue;

        if (accountConfiguration.IsEnabled)
        {
            var cellmConfiguration = CellmAddIn.Services.GetRequiredService<IOptionsMonitor<CellmConfiguration>>().CurrentValue;
            providerAndModels.AddRange(cellmConfiguration.Models.Select(m => $"{nameof(Provider.Cellm)}/{m}"));
        }

        var anthropicConfiguration = CellmAddIn.Services.GetRequiredService<IOptionsMonitor<AnthropicConfiguration>>().CurrentValue;

        var deepSeekConfiguration = CellmAddIn.Services.GetRequiredService<IOptionsMonitor<DeepSeekConfiguration>>().CurrentValue;
        var mistralConfiguration = CellmAddIn.Services.GetRequiredService<IOptionsMonitor<MistralConfiguration>>().CurrentValue;
        var ollamaConfiguration = CellmAddIn.Services.GetRequiredService<IOptionsMonitor<OllamaConfiguration>>().CurrentValue;
        var openAiConfiguration = CellmAddIn.Services.GetRequiredService<IOptionsMonitor<OpenAiConfiguration>>().CurrentValue;
        var openAiCompatibleConfiguration = CellmAddIn.Services.GetRequiredService<IOptionsMonitor<OpenAiCompatibleConfiguration>>().CurrentValue;

        providerAndModels.AddRange(anthropicConfiguration.Models.Select(m => $"{nameof(Provider.Anthropic)}/{m}"));
        providerAndModels.AddRange(deepSeekConfiguration.Models.Select(m => $"{nameof(Provider.DeepSeek)}/{m}"));
        providerAndModels.AddRange(mistralConfiguration.Models.Select(m => $"{nameof(Provider.Mistral)}/{m}"));
        providerAndModels.AddRange(ollamaConfiguration.Models.Select(m => $"{nameof(Provider.Ollama)}/{m}"));
        providerAndModels.AddRange(openAiConfiguration.Models.Select(m => $"{nameof(Provider.OpenAi)}/{m}"));
        providerAndModels.AddRange(openAiCompatibleConfiguration.Models.Select(m => $"{nameof(Provider.OpenAiCompatible)}/{m}"));

        var stringBuilder = new StringBuilder();

        foreach (var providerAndModel in providerAndModels)
        {
            stringBuilder.AppendLine($"<item label=\"{providerAndModel.ToLower()}\" id=\"{new string(providerAndModel.Where(char.IsLetterOrDigit).ToArray())}\" />");
        }

        return $"""
<group id="models" label="Provider">
    <comboBox id="comboBox" 
        label="Model" 
        sizeString="WWWWWWWWWWWWWWW" 
        onChange="OnModelChanged"
        getText="OnGetSelectedModel">
        {stringBuilder}
    </comboBox>
    <editBox id="baseAddress" label="Address" sizeString="WWWWWWWWWWWWWWW" getEnabled="OnGetBaseAddressEnabled" getText="OnGetBaseAddress" onChange="OnBaseAddressChanged" />
    <editBox id="apiKey" label="API Key" sizeString="WWWWWWWWWWWWWWW" getEnabled="OnGetApiKeyEnabled" getText="OnGetApiKey" onChange="OnApiKeyChanged" />
    <toggleButton id="cache" label="Cache" size="large" imageMso="SourceControlRefreshStatus" 
        screentip="Enable/disable local caching of model responses. Disabling cache will clear all cached responses." 
        onAction="OnCacheToggled" getPressed="OnGetCachePressed" />
</group>
""";
    }

    // *** NEW Login Click Handler ***
    public void OnLoginClicked(IRibbonControl control)
    {
        using var loginForm = new LoginForm();

        // Show the form modally
        // Note: ShowDialog() blocks until the form is closed.
        if (loginForm.ShowDialog() == DialogResult.OK)
        {
            string username = loginForm.Username;
            string password = loginForm.Password;

            // Perform the check immediately with the entered credentials
            // Running synchronously for simplicity after modal dialog closes.
            // For very slow networks, consider a brief "Checking..." UI feedback.
            Task<bool> checkTask = PerformServerLoginCheckAsync(username, password);

            // Block and wait for the result (acceptable after modal dialog)
            bool loginSuccess = checkTask.Result; // Use .Result here as we need the outcome now

            if (loginSuccess)
            {
                System.Diagnostics.Debug.WriteLine($"Login successful for {username}. Saving credentials.");
                // Save credentials ONLY if server check passed
                SetValue("AccountConfiguration:Username", username);
                SetValue("AccountConfiguration:Password", password);

                // Update cache immediately
                _cachedLoginState = true;
                _cacheExpiry = DateTime.UtcNow.Add(_cacheDuration);

                // Invalidate UI to reflect logged-in state
                InvalidateUserControls();
                MessageBox.Show("Login successful!", "Cellm", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"Login failed for {username}. Not saving credentials.");
                // Show error message
                MessageBox.Show("Login failed. Please check your username and password.", "Login Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                // DO NOT save credentials
                // DO NOT update cache (or explicitly set to false if previous state might have been true)
                _cachedLoginState = false; // Ensure state reflects failure
                _cacheExpiry = DateTime.UtcNow.Add(_cacheDuration);
                InvalidateUserControls(); // Update UI to show logged-out state
            }
        }
        // else: User cancelled, do nothing.
    }


    public void OnLogoutClicked(IRibbonControl control)
    {
        SetValue("AccountConfiguration:Username", "");
        SetValue("AccountConfiguration:Password", "");
        _cachedLoginState = false;
        _cacheExpiry = DateTime.UtcNow.Add(_cacheDuration);
        InvalidateUserControls();
    }

    // Use cached state for enabling/disabling menu items
    public bool IsLoggedIn(IRibbonControl control)
    {
        if (!_cachedLoginState.HasValue && DateTime.UtcNow > _cacheExpiry)
        {
            // If state is unknown and cache expired, trigger check but return last known state
            TriggerBackgroundLoginCheck();
        }
        return _cachedLoginState ?? false;
    }

    public bool IsLoggedOut(IRibbonControl control)
    {
        return !IsLoggedIn(control); // Simply the inverse
    }


    public Bitmap? GetUserAccountImage(IRibbonControl control)
    {
        // Use cached state directly
        var svgPath = _cachedLoginState ?? false ? "AddIn/UserInterface/Resources/logged-in.svg" : "AddIn/UserInterface/Resources/logged-out.svg";

        return ImageLoader.LoadEmbeddedSvgResized(svgPath, 128, 128);
    }

    public Bitmap? GetUserLoginImage(IRibbonControl control)
    {
        return ImageLoader.LoadEmbeddedSvgResized("AddIn/UserInterface/Resources/login.svg", 64, 64);
    }

    public Bitmap? GetUserLogoutImage(IRibbonControl control)
    {
        return ImageLoader.LoadEmbeddedSvgResized("AddIn/UserInterface/Resources/logout.svg", 64, 64);
    }

    public string GetUserScreentip(IRibbonControl control)
    {
        bool isLoggedIn = _cachedLoginState ?? false;

        if (isLoggedIn)
        {
            try
            {
                var username = GetValue("AccountConfiguration:Username");
                return $"Account: {username}\nStatus: Logged In";
            }
            catch { return "Account: Unknown\nStatus: Logged In"; }
        }
        else
        {
            return "Status: Logged Out";
        }
    }

    // --- Background Check Trigger (Used mainly for OnLoad now) ---
    private void TriggerBackgroundLoginCheck(bool forceCheck = false)
    {
        if (_isLoginCheckRunning)
        {
            return;
        }

        if (!forceCheck && _cachedLoginState.HasValue && DateTime.UtcNow < _cacheExpiry) 
        { 
            return; 
        }

        _isLoginCheckRunning = true;

        Task.Run(async () =>
        {
            try
            {
                // Perform check using SAVED credentials (no args passed)
                bool actualLoginState = await PerformServerLoginCheckAsync();
                bool stateChanged = !_cachedLoginState.HasValue || _cachedLoginState.Value != actualLoginState;
                _cachedLoginState = actualLoginState;
                _cacheExpiry = DateTime.UtcNow.Add(_cacheDuration);
                if (stateChanged && _ribbonUi != null) InvalidateUserControls();
            }
            catch (Exception)
            {
                /* ... error handling ... */
                _cachedLoginState = false;
                _cacheExpiry = DateTime.UtcNow.Add(_cacheDuration);
                if (_ribbonUi != null)
                {
                    InvalidateUserControls();
                }
            }
            finally
            {
                _isLoginCheckRunning = false;
            }
        });
    }

    // --- MODIFIED Async HTTP Check Implementation ---
    private async Task<bool> PerformServerLoginCheckAsync(string? usernameToCheck = null, string? passwordToCheck = null)
    {
        string? effectiveUsername = usernameToCheck;
        string? effectivePassword = passwordToCheck;

        System.Diagnostics.Debug.WriteLine($"Performing server login check for user: {(string.IsNullOrWhiteSpace(effectiveUsername) ? "(Saved User)" : effectiveUsername)}");

        // If no credentials passed, try reading saved ones
        if (string.IsNullOrWhiteSpace(effectiveUsername) || string.IsNullOrWhiteSpace(effectivePassword))
        {
            try
            {
                effectiveUsername = GetValue("AccountConfiguration:Username");
                effectivePassword = GetValue("AccountConfiguration:Password");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error reading saved credentials for login check: {ex.Message}");
                return false; // Cannot check without credentials
            }
        }

        // If still no credentials (neither passed nor saved), definitely not logged in
        if (string.IsNullOrWhiteSpace(effectiveUsername) || string.IsNullOrWhiteSpace(effectivePassword))
        {
            System.Diagnostics.Debug.WriteLine("No valid credentials found (passed or saved). Assuming logged out.");
            return false;
        }

        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
        try
        {
            var plainTextBytes = Encoding.UTF8.GetBytes($"{effectiveUsername}:{effectivePassword}");
            var base64Credentials = Convert.ToBase64String(plainTextBytes);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", base64Credentials);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("CellmExcelAddIn/1.0 (AuthCheck)");

            HttpResponseMessage response = await client.GetAsync(AuthCheckUrl);
            System.Diagnostics.Debug.WriteLine($"Auth check response: {response.StatusCode}");
            return response.StatusCode == HttpStatusCode.OK;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error during login check HTTP request: {ex.Message}");
            return false;
        }
    }


    // Helper to invalidate user-related controls
    private void InvalidateUserControls()
    {
        _ribbonUi?.InvalidateControl("userAccountSplitButton");  // Tooltip (depends on GetUserScreentip)
        _ribbonUi?.InvalidateControl("userAccountButton");       // Icon (depends on GetUserAccountImage)
        _ribbonUi?.InvalidateControl("loginButton");             // Enabled state (depends on IsLoggedOut)
        _ribbonUi?.InvalidateControl("logoutButton");            // Enabled state (depends on IsLoggedIn)
        _ribbonUi?.InvalidateControl("accountActionButton");     // Label, Screentip, Icon (depends on GetAccountAction* callbacks)
    }

    public string GetAccountActionLabel(IRibbonControl control)
    {
        return IsLoggedIn(control) ? "Manage Account..." : "Sign up...";
    }

    public string GetAccountActionScreentip(IRibbonControl control)
    {
        return IsLoggedIn(control)
            ? "Open your Cellm account settings in your browser."
            : "Open the Cellm sign-up page in your browser.";
    }

    public Bitmap? GetAccountActionImage(IRibbonControl control)
    {
        return ImageLoader.LoadEmbeddedSvgResized("AddIn/UserInterface/Resources/external-link.svg", 64, 64);
    }

    public void OnAccountActionClicked(IRibbonControl control)
    {
        string url = IsLoggedIn(control) ? ManageAccountUrl : SignUpUrl;

        try
        {
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            };
            Process.Start(psi);
        }
        catch (Exception ex)
        {
            // Log the error for diagnostics
            Debug.WriteLine($"Error opening URL '{url}': {ex.Message}");
            MessageBox.Show($"Could not open the link: {url}\n\nError: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    public string OnGetSelectedModel(IRibbonControl control)
    {
        var provider = GetCurrentProvider();
        var model = GetValue($"{provider}Configuration:{nameof(OpenAiConfiguration.DefaultModel)}");
        return $"{provider.ToString().ToLower()}/{model}";
    }

    public string OnGetBaseAddress(IRibbonControl control)
    {
        var provider = GetCurrentProvider();

        return provider switch
        {
            Provider.DeepSeek => GetProviderConfiguration<DeepSeekConfiguration>().BaseAddress.ToString(),
            Provider.Mistral => GetProviderConfiguration<MistralConfiguration>().BaseAddress.ToString(),
            Provider.Ollama => GetProviderConfiguration<OllamaConfiguration>().BaseAddress.ToString(),
            Provider.OpenAiCompatible => GetProviderConfiguration<OpenAiCompatibleConfiguration>().BaseAddress.ToString(),
            _ => "Built-in"
        };
    }

    public string OnGetApiKey(IRibbonControl control)
    {
        var provider = GetCurrentProvider();
        return GetValue($"{provider}Configuration:ApiKey");
    }

    public void OnModelChanged(IRibbonControl control, string providerAndModel)
    {
        var provider = GetProvider(providerAndModel);
        var model = GetModel(providerAndModel);

        SetValue($"{nameof(ProviderConfiguration)}:{nameof(ProviderConfiguration.DefaultProvider)}", provider.ToString());
        SetValue($"{provider}Configuration:{nameof(OpenAiConfiguration.DefaultModel)}", model);

        _ribbonUi?.Invalidate();
    }

    public void OnBaseAddressChanged(IRibbonControl control, string text)
    {
        var provider = GetCurrentProvider();
        SetValue($"{provider}Configuration:{nameof(DeepSeekConfiguration.BaseAddress)}", text);
    }

    public void OnApiKeyChanged(IRibbonControl control, string text)
    {
        var provider = GetCurrentProvider();
        SetValue($"{provider}Configuration:{nameof(OpenAiConfiguration.ApiKey)}", text);
    }

    public bool OnGetBaseAddressEnabled(IRibbonControl control)
    {
        var provider = GetCurrentProvider();
        return provider switch
        {
            Provider.OpenAiCompatible or Provider.Ollama => true,
            _ => false
        };
    }

    public bool OnGetApiKeyEnabled(IRibbonControl control)
    {
        var provider = GetCurrentProvider();
        return provider switch
        {
            Provider.OpenAiCompatible or Provider.Anthropic or Provider.DeepSeek
                or Provider.Mistral or Provider.OpenAi => true,
            _ => false
        };
    }

    public async Task OnCacheToggled(IRibbonControl control, bool enabled)
    {
        if (!enabled)
        {
            var cache = CellmAddIn.Services.GetRequiredService<HybridCache>();
            await cache.RemoveByTagAsync(nameof(ProviderResponse));

        }

        SetValue($"{nameof(ProviderConfiguration)}:{nameof(ProviderConfiguration.EnableCache)}", enabled.ToString());
    }

    public bool OnGetCachePressed(IRibbonControl control)
    {
        return bool.Parse(GetValue($"{nameof(ProviderConfiguration)}:{nameof(ProviderConfiguration.EnableCache)}"));
    }

    public void OnFileSearchToggled(IRibbonControl control, bool pressed)
    {
        SetValue("ProviderConfiguration:EnableTools:FileSearchRequest", pressed.ToString());
    }

    public bool OnGetFileSearchPressed(IRibbonControl control)
    {
        var value = GetValue("ProviderConfiguration:EnableTools:FileSearchRequest");
        return bool.Parse(value);
    }

    public void OnFileReaderToggled(IRibbonControl control, bool pressed)
    {
        SetValue("ProviderConfiguration:EnableTools:FileReaderRequest", pressed.ToString());
    }

    public bool OnGetFileReaderPressed(IRibbonControl control)
    {
        var value = GetValue("ProviderConfiguration:EnableTools:FileReaderRequest");
        return bool.Parse(value);
    }

    private Provider GetCurrentProvider()
    {
        return Enum.Parse<Provider>(GetValue($"{nameof(ProviderConfiguration)}:{nameof(ProviderConfiguration.DefaultProvider)}"), true);
    }

    private static T GetProviderConfiguration<T>()
    {
        return CellmAddIn.Services.GetRequiredService<IOptionsMonitor<T>>().CurrentValue;
    }

    private static Provider GetProvider(string providerAndModel)
    {
        var index = providerAndModel.IndexOf('/');

        if (index < 0)
        {
            throw new ArgumentException($"Provider and model argument must on the form \"Provider/Model\"");
        }

        if (!Enum.TryParse<Provider>(providerAndModel[..index], true, out var provider))
        {
            throw new ArgumentException($"Unsupported default provider: {providerAndModel[..index]}");
        }

        return provider;
    }

    private static string GetModel(string providerAndModel)
    {
        var index = providerAndModel.IndexOf('/');

        if (index < 0)
        {
            throw new ArgumentException($"Provider and model argument must on the form \"Provider/Model\"");
        }

        return providerAndModel[(index + 1)..];
    }

    public static string GetValue(string key)
    {
        var keySegments = key.Split(':');

        // 1. Check local settings first
        if (File.Exists(_appsettingsLocalPath))
        {
            var localNode = JsonNode.Parse(File.ReadAllText(_appsettingsLocalPath));
            var value = GetValueFromNode(localNode, keySegments);
            if (value != null) return value.ToString();
        }

        // 2. Fall back to base settings
        if (File.Exists(_appSettingsPath))
        {
            var baseNode = JsonNode.Parse(File.ReadAllText(_appSettingsPath));
            var value = GetValueFromNode(baseNode, keySegments);
            if (value != null) return value.ToString();
        }

        throw new KeyNotFoundException($"Key '{key}' not found in configuration files");
    }

    public static void SetValue(string key, string value)
    {
        var keySegments = key.Split(':');
        JsonNode localNode = File.Exists(_appsettingsLocalPath)
            ? JsonNode.Parse(File.ReadAllText(_appsettingsLocalPath)) ?? new JsonObject()
            : new JsonObject();

        SetValueInNode(localNode.AsObject(), keySegments, value);

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        Directory.CreateDirectory(Path.GetDirectoryName(_appsettingsLocalPath)!);
        File.WriteAllText(_appsettingsLocalPath, localNode.ToJsonString(options));
    }

    private static JsonNode? GetValueFromNode(JsonNode? node, string[] keySegments)
    {
        foreach (var segment in keySegments)
        {
            node = node is JsonObject obj
                && obj.TryGetPropertyValue(segment, out var childNode)
                ? childNode
                : null;

            if (node == null) break;
        }
        return node;
    }

    private static void SetValueInNode(JsonObject node, string[] keySegments, string value)
    {
        var current = node;
        for (int i = 0; i < keySegments.Length; i++)
        {
            var isLast = i == keySegments.Length - 1;
            var segment = keySegments[i];

            if (isLast)
            {
                current[segment] = value;
            }
            else
            {
                if (!current.TryGetPropertyValue(segment, out var nextNode))
                {
                    nextNode = new JsonObject();
                    current[segment] = nextNode;
                }
                current = nextNode!.AsObject();
            }
        }
    }
}
