using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using Cellm.AddIn.UserInterface.Forms;
using Cellm.Users;
using ExcelDna.Integration.CustomUI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cellm.AddIn.UserInterface.Ribbon;

public partial class RibbonMain
{
    private static bool? _cachedLoginState = null;
    private static DateTime _cacheExpiry = DateTime.MinValue;
    private static readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(5);
    private static volatile bool _isLoginCheckRunning = false;

    private enum UserGroupControlIds
    {
        UserGroup,
        UserAccountSplitButton,
        UserAccountButton,
        UserAccountMenu,
        LoginButton,
        LogoutButton,
        AccountActionSeparator,
        AccountActionButton
    }

    public string UserGroup()
    {
        var accountConfiguration = CellmAddIn.Services.GetRequiredService<IOptionsMonitor<AccountConfiguration>>();

        if (!accountConfiguration.CurrentValue.IsEnabled)
        {
            return string.Empty;
        }

        return $"""
        <group id="{nameof(UserGroupControlIds.UserGroup)}" label="Account">
            <splitButton id="{nameof(UserGroupControlIds.UserAccountSplitButton)}" size="large">
                <button id="{nameof(UserGroupControlIds.UserAccountButton)}"
                        label="User"
                        getImage="{nameof(GetUserAccountImage)}"
                        getScreentip="{nameof(GetUserScreentip)}" />
                <menu id="{nameof(UserGroupControlIds.UserAccountMenu)}">
                     <button id="{nameof(UserGroupControlIds.LoginButton)}" label="Login..."
                         onAction="{nameof(OnLoginClicked)}"
                         getEnabled="{nameof(IsLoggedOut)}"
                         getImage="{nameof(GetUserLoginImage)}"
                         screentip="Log in to your Cellm account." />
                     <button id="{nameof(UserGroupControlIds.LogoutButton)}" label="Logout"
                         onAction="{nameof(OnLogoutClicked)}"
                         getEnabled="{nameof(IsLoggedIn)}"
                         getImage="{nameof(GetUserLogoutImage)}"
                         screentip="Log out and clear saved credentials." />
                     <menuSeparator id="{nameof(UserGroupControlIds.AccountActionSeparator)}" />
                     <button id="{nameof(UserGroupControlIds.AccountActionButton)}"
                         getLabel="{nameof(GetAccountActionLabel)}"
                         onAction="{nameof(OnAccountActionClicked)}"
                         getImage="{nameof(GetAccountActionImage)}"
                         getScreentip="{nameof(GetAccountActionScreentip)}" />
                 </menu>
            </splitButton>
        </group>
        """;
    }

    public void OnLoginClicked(IRibbonControl control)
    {
        using var loginForm = new LoginForm();

        // Show the form modally
        // Note: ShowDialog() blocks until the form is closed.
        if (loginForm.ShowDialog() == DialogResult.OK)
        {
            var username = loginForm.Username;
            var password = loginForm.Password;

            // Perform the check immediately with the entered credentials
            var checkTask = PerformServerLoginCheckAsync(username, password);

            // Block and wait for the result (acceptable after modal dialog)
            var loginSuccess = checkTask.Result; // Use .Result here as we need the outcome now

            if (loginSuccess)
            {
                _logger.LogInformation("Login successful for {username}, saving credentials.", username);

                // Save credentials ONLY if server check passed
                SetValue($"{nameof(AccountConfiguration)}:{nameof(AccountConfiguration.Username)}", username);
                SetValue($"{nameof(AccountConfiguration)}:{nameof(AccountConfiguration.Password)}", password);

                // Update cache immediately
                _cachedLoginState = true;
                _cacheExpiry = DateTime.UtcNow.Add(_cacheDuration);

                // Invalidate UI to reflect logged-in state
                InvalidateUserControls();
                InvalidateEntitledControls();
                MessageBox.Show("Login successful!", "Cellm", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                // DO NOT save credentials
                _logger.LogInformation("Login failed for {username}, not saving credentials.", username);

                // Show error message
                MessageBox.Show("Login failed. Please check your username and password.", "Login Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);


                // Explicitly set login state to false in case previous state was true
                _cachedLoginState = false; // Ensure state reflects failure
                _cacheExpiry = DateTime.UtcNow.Add(_cacheDuration);

                InvalidateUserControls();
                InvalidateEntitledControls();
            }
        }
        // else: User cancelled, do nothing.
    }

    public void OnLogoutClicked(IRibbonControl control)
    {
        SetValue($"{nameof(AccountConfiguration)}:{nameof(AccountConfiguration.Username)}", string.Empty);
        SetValue($"{nameof(AccountConfiguration)}:{nameof(AccountConfiguration.Password)}", string.Empty);

        _cachedLoginState = false;
        _cacheExpiry = DateTime.UtcNow.Add(_cacheDuration);

        InvalidateUserControls();
        InvalidateEntitledControls();
    }

    // Use cached state for enabling/disabling menu items
    public bool IsLoggedIn(IRibbonControl control)
    {
        // If first run (not HasValue) or cached expired
        if (!_cachedLoginState.HasValue || DateTime.UtcNow > _cacheExpiry)
        {
            // Only trigger if not already running to avoid multiple checks
            if (!_isLoginCheckRunning)
            {
                TriggerBackgroundLoginCheck();
            }

            // While check runs, return the cached value (or false if first run) 
            return _cachedLoginState ?? false;
        }

        return _cachedLoginState.Value;
    }

    public bool IsLoggedOut(IRibbonControl control)
    {
        return !IsLoggedIn(control);
    }


    public Bitmap? GetUserAccountImage(IRibbonControl control)
    {
        // Use cached state directly
        var svgPath = _cachedLoginState ?? false ? "AddIn/UserInterface/Resources/logged-in.svg" : "AddIn/UserInterface/Resources/logged-out.svg";

        return ImageLoader.LoadEmbeddedSvgResized(svgPath, 128, 128);
    }

    public Bitmap? GetUserLoginImage(IRibbonControl control)
    {
        return ImageLoader.LoadEmbeddedSvgResized($"{ResourcesBasePath}/login.svg", 64, 64);
    }

    public Bitmap? GetUserLogoutImage(IRibbonControl control)
    {
        return ImageLoader.LoadEmbeddedSvgResized($"{ResourcesBasePath}/logout.svg", 64, 64);
    }

    public string GetUserScreentip(IRibbonControl control)
    {
        var isLoggedIn = _cachedLoginState ?? false;

        if (isLoggedIn)
        {
            try
            {
                var username = GetValue($"{nameof(AccountConfiguration)}:{nameof(AccountConfiguration.Username)}");
                return $"Account: {username}\nStatus: Logged In";
            }
            catch
            {
                return "Account: Unknown\nStatus: Logged In";
            }
        }
        else
        {
            return "Status: Logged Out";
        }
    }
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
                var actualLoginState = await PerformServerLoginCheckAsync();
                var stateChanged = !_cachedLoginState.HasValue || _cachedLoginState.Value != actualLoginState;
                _cachedLoginState = actualLoginState;
                _cacheExpiry = DateTime.UtcNow.Add(_cacheDuration);
                if (stateChanged && _ribbonUi != null) InvalidateUserControls();
            }
            catch (Exception)
            {
                _cachedLoginState = false;
                _cacheExpiry = DateTime.UtcNow.Add(_cacheDuration);
                InvalidateUserControls();
            }
            finally
            {
                _isLoginCheckRunning = false;
            }
        });
    }

    private async Task<bool> PerformServerLoginCheckAsync(string? usernameToCheck = null, string? passwordToCheck = null)
    {
        var username = usernameToCheck;
        var password = passwordToCheck;

        _logger.LogInformation("Performing server login check for user: {}", (string.IsNullOrWhiteSpace(username) ? "(Saved User)" : username));

        // If no credentials passed, try reading saved ones
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            try
            {
                username = GetValue($"{nameof(AccountConfiguration)}:{nameof(AccountConfiguration.Username)}");
                password = GetValue($"{nameof(AccountConfiguration)}:{nameof(AccountConfiguration.Password)}");
            }
            catch (Exception ex)
            {
                _logger.LogError("Error reading saved credentials for login check: {message}", ex.Message);
                return false; // Cannot check without credentials
            }
        }

        // If still no credentials (neither passed nor saved), definitely not logged in
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            _logger.LogInformation("No valid credentials found (passed or saved). Assuming logged out.");
            return false;
        }

        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
        try
        {
            var account = CellmAddIn.Services.GetRequiredService<Account>();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", account.GetBasicAuthCredentials(username, password));

            var accountConfiguration = CellmAddIn.Services.GetRequiredService<IOptionsMonitor<AccountConfiguration>>();
            var response = await client.GetAsync($"{accountConfiguration.CurrentValue.BaseAddress}/up");
            _logger.LogInformation("Authorization response: {statusCode}", response.StatusCode);

            return response.StatusCode == HttpStatusCode.OK;
        }
        catch (Exception ex)
        {
            _logger.LogError("Error during login check HTTP request: {message}", ex.Message);
            return false;
        }
    }


    // Helper to invalidate user-related controls
    private void InvalidateUserControls()
    {
        if (_ribbonUi == null)
        {
            return;
        }

        _ribbonUi.InvalidateControl(nameof(UserGroupControlIds.UserAccountSplitButton));
        _ribbonUi.InvalidateControl(nameof(UserGroupControlIds.UserAccountButton));
        _ribbonUi.InvalidateControl(nameof(UserGroupControlIds.LoginButton));
        _ribbonUi.InvalidateControl(nameof(UserGroupControlIds.LogoutButton));
        _ribbonUi.InvalidateControl(nameof(UserGroupControlIds.AccountActionButton));
    }

    private void InvalidateEntitledControls()
    {
        if (_ribbonUi == null)
        {
            return;
        }

        _ribbonUi.InvalidateControl(nameof(ModelGroupControlIds.ProviderSelectionMenu));

        foreach (var item in _providerItems)
        {
            _ribbonUi.InvalidateControl(item.Value.Id);
        }

        _ribbonUi.InvalidateControl(nameof(ToolsGroupControlIds.McpSplitButton));
    }


    public string GetAccountActionLabel(IRibbonControl control)
    {
        return IsLoggedIn(control) ? "Manage Account..." : "Sign up...";
    }

    public string GetAccountActionScreentip(IRibbonControl control)
    {
        return IsLoggedIn(control)
            ? "Open your Cellm account settings in getcell.com"
            : "Sign up on getcell.com to enable all features";
    }

    public Bitmap? GetAccountActionImage(IRibbonControl control)
    {
        return ImageLoader.LoadEmbeddedSvgResized("AddIn/UserInterface/Resources/external-link.svg", 64, 64);
    }

    public void OnAccountActionClicked(IRibbonControl control)
    {
        var accountConfiguration = CellmAddIn.Services.GetRequiredService<IOptionsMonitor<AccountConfiguration>>();
        var url = IsLoggedIn(control) ? $"{accountConfiguration.CurrentValue.Homepage}/account" : $"{accountConfiguration.CurrentValue.Homepage}/user/new";

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            };
            Process.Start(psi);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error opening URL {url}: {message}", url, ex.Message);

            MessageBox.Show($"Could not open the link: {url}\n\nError: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
