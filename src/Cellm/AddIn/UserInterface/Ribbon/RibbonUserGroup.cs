using System.Diagnostics;
using Cellm.AddIn.UserInterface.Forms;
using Cellm.Users;
using ExcelDna.Integration;
using ExcelDna.Integration.CustomUI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cellm.AddIn.UserInterface.Ribbon;

public partial class RibbonMain
{
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
                        getLabel="{nameof(GetAccountButtonLabel)}"
                        getImage="{nameof(GetAccountImage)}"
                        getScreentip="{nameof(GetAccountScreentip)}" />
                <menu id="{nameof(UserGroupControlIds.UserAccountMenu)}">
                     <button id="{nameof(UserGroupControlIds.LoginButton)}"
                         getLabel="{nameof(GetLoginButtonLabel)}"
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

        if (loginForm.ShowDialog() == DialogResult.OK)
        {
            var username = loginForm.Email;
            var password = loginForm.Password;

            Task.Factory.StartNew(async () => {
                var account = CellmAddIn.Services.GetRequiredService<Account>();

                try
                {
                    var token = await account.GetTokenAsync(username, password, CancellationToken.None);

                    SetValue($"{nameof(AccountConfiguration)}:{nameof(AccountConfiguration.Email)}", username);
                    SetValue($"{nameof(AccountConfiguration)}:{nameof(AccountConfiguration.ApiKey)}", token);

                    // TODO: Fix possible race condition. The message box blocks the worker thread and incidentally
                    //   gives IOptionsMonitor<AccountConfiguration> time to pick up changes before UI is refreshed
                    ExcelAsyncUtil.QueueAsMacro(() =>
                    {
                        MessageBox.Show("Login successful", "Cellm", MessageBoxButtons.OK, MessageBoxIcon.Information);

                        // Trigger token validation so the cache gets populated
                        IsLoggedIn(control);

                        InvalidateUserControls();
                        InvalidateEntitledControls();
                    });
                }
                catch (Exception)
                {
                    // Save username but clear token
                    SetValue($"{nameof(AccountConfiguration)}:{nameof(AccountConfiguration.Email)}", username);
                    SetValue($"{nameof(AccountConfiguration)}:{nameof(AccountConfiguration.ApiKey)}", string.Empty);

                    ExcelAsyncUtil.QueueAsMacro(() =>
                    {
                        MessageBox.Show("Login failed. Please check your username and password.", "Cellm", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    });
                }
            });
        }
        // else: User cancelled, do nothing.
    }

    public void OnLogoutClicked(IRibbonControl control)
    {
        SetValue($"{nameof(AccountConfiguration)}:{nameof(AccountConfiguration.ApiKey)}", string.Empty);

        var account = CellmAddIn.Services.GetRequiredService<Account>();
        account.Clear();

        InvalidateUserControls();
        InvalidateEntitledControls();
    }

    public bool IsLoggedIn(IRibbonControl control)
    {
        var accountConfiguration = CellmAddIn.Services.GetRequiredService<IOptionsMonitor<AccountConfiguration>>();

        if (string.IsNullOrWhiteSpace(accountConfiguration.CurrentValue.ApiKey))
        {
            return false;
        }

        var account = CellmAddIn.Services.GetRequiredService<Account>();
        
        return account.HasValidToken(accountConfiguration.CurrentValue.ApiKey);
    }

    public bool IsLoggedOut(IRibbonControl control)
    {
        return !IsLoggedIn(control);
    }


    public Bitmap? GetAccountImage(IRibbonControl control)
    {
        // Use cached state directly
        var svgPath = IsLoggedIn(control) ? "AddIn/UserInterface/Resources/logged-in.svg" : "AddIn/UserInterface/Resources/logged-out.svg";

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

    public string GetAccountScreentip(IRibbonControl control)
    {
        if (IsLoggedIn(control))
        {
            try
            {
                var email = GetValue($"{nameof(AccountConfiguration)}:{nameof(AccountConfiguration.Email)}");
                return $"Account: {email}\nStatus: Logged In";
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

        InvalidateModelControls();

        _ribbonUi.InvalidateControl(nameof(ToolsGroupControlIds.McpSplitButton));
    }

    public string GetAccountButtonLabel(IRibbonControl control)
    {
        if (IsLoggedIn(control))
        {
            try
            {
                var username = GetValue($"{nameof(AccountConfiguration)}:{nameof(AccountConfiguration.Email)}");
                return username.Length > 6 ? string.Concat(username.AsSpan(0, 6), "…") : username;
            }
            catch
            {
                return "Account";
            }
        }
        else
        {
            return "Login";
        }
    }

    public string GetLoginButtonLabel(IRibbonControl control)
    {
        if (IsLoggedIn(control))
        {
            try
            {
                var email = GetValue($"{nameof(AccountConfiguration)}:{nameof(AccountConfiguration.Email)}");
                return email;
            }
            catch
            {
                return "Logged in";
            }
        }
        else
        {
            return "Login...";
        }
    }

    public string GetAccountActionLabel(IRibbonControl control)
    {
        return IsLoggedIn(control) ? "Manage Account..." : "Create account...";
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
