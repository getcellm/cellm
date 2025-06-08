using System.Text;
using Cellm.AddIn.UserInterface.Forms;
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
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cellm.AddIn.UserInterface.Ribbon;

public partial class RibbonMain
{
    private enum ModelGroupControlIds
    {
        VerticalContainer,

        ModelProviderGroup,
        ProviderModelBox,
        ProviderSplitButton,
        ProviderDisplayButton,
        ProviderSelectionMenu,

        ModelComboBox,
        TemperatureComboBox,
        CacheToggleButton,

        ProviderSettingsButton
    }

    private class ProviderItem
    {
        public string Id { get; set; } = string.Empty;

        public string Label { get; set; } = string.Empty;

        public string Image { get; set; } = string.Empty;

        public string SmallModel { get; set; } = string.Empty;

        public string MediumModel { get; set; } = string.Empty;

        public string LargeModel { get; set; } = string.Empty;

        public Entitlement Entitlement { get; set; } = Entitlement.EnableCellmProvider;
    }

    private const string NoPresetsPlaceholder = "(No presets configured)";

    private readonly Dictionary<int, ProviderItem> _providerItems = new()
    {
        [0] = new ProviderItem { Id = $"{nameof(Provider)}.{nameof(Provider.Anthropic)}", Image = $"{ResourcesBasePath}/anthropic.png", Label = nameof(Provider.Anthropic), Entitlement = Entitlement.EnableAnthropicProvider },
        [1] = new ProviderItem { Id = $"{nameof(Provider)}.{nameof(Provider.Azure)}", Image = $"{ResourcesBasePath}/azure.png", Label = nameof(Provider.Azure), Entitlement = Entitlement.EnableAzureProvider },
        [2] = new ProviderItem { Id = $"{nameof(Provider)}.{nameof(Provider.Gemini)}", Image = $"{ResourcesBasePath}/google.png", Label = nameof(Provider.Gemini), Entitlement = Entitlement.EnableGeminiProvider },
        [3] = new ProviderItem { Id = $"{nameof(Provider)}.{nameof(Provider.DeepSeek)}", Image = $"{ResourcesBasePath}/deepseek.png", Label = nameof(Provider.DeepSeek), Entitlement = Entitlement.EnableDeepSeekProvider },
        [4] = new ProviderItem { Id = $"{nameof(Provider)}.{nameof(Provider.Mistral)}", Image = $"{ResourcesBasePath}/mistral.png", Label = nameof(Provider.Mistral), Entitlement = Entitlement.EnableMistralProvider },
        [5] = new ProviderItem { Id = $"{nameof(Provider)}.{nameof(Provider.Ollama)}", Image = $"{ResourcesBasePath}/ollama.png", Label = nameof(Provider.Ollama), Entitlement = Entitlement.EnableOllamaProvider },
        [6] = new ProviderItem { Id = $"{nameof(Provider)}.{nameof(Provider.OpenAi)}", Image = $"{ResourcesBasePath}/openai.png", Label = nameof(Provider.OpenAi), Entitlement = Entitlement.EnableOpenAiProvider },
        [7] = new ProviderItem { Id = $"{nameof(Provider)}.{nameof(Provider.OpenAiCompatible)}", Image = $"{ResourcesBasePath}/openai.png", Label = nameof(Provider.OpenAiCompatible) }
    };

    internal int _selectedProviderIndex = 3; // Default to Ollama

    private void InitializeSelectedProviderIndex()
    {
        try
        {
            var defaultProviderName = GetValue($"{nameof(CellmAddInConfiguration)}:{nameof(CellmAddInConfiguration.DefaultProvider)}");
            var defaultProvider = Enum.Parse<Provider>(defaultProviderName, true);

            _selectedProviderIndex = _providerItems.FirstOrDefault(kvp => kvp.Value.Label.Equals(defaultProvider.ToString(), StringComparison.OrdinalIgnoreCase)).Key;
        }
        catch (Exception ex) when (ex is KeyNotFoundException || ex is ArgumentException)
        {
            SetValue($"{nameof(CellmAddInConfiguration)}:{nameof(CellmAddInConfiguration.DefaultProvider)}", nameof(Provider.Ollama));
            _selectedProviderIndex = _providerItems.FirstOrDefault(kvp => kvp.Value.Label.Equals(nameof(Provider.Ollama), StringComparison.OrdinalIgnoreCase)).Key;
        }
        catch (Exception ex) // Catch other potential errors during init
        {
            _logger.LogError("Error initializing selected provider index: {message}", ex.Message);
        }
    }

    public string ModelGroup()
    {
        var providerMenuItemsXml = new StringBuilder();

        // Dynamic provider menu items (IDs generated here, not in enum)
        foreach (var kvp in _providerItems.OrderBy(p => p.Value.Label))
        {
            var index = kvp.Key;
            var item = kvp.Value;
            var menuItemId = item.Id; // Dynamic ID
            providerMenuItemsXml.AppendLine(
                $@"<button id=""{menuItemId}""
                     label=""{System.Security.SecurityElement.Escape(item.Label)}""
                     getImage=""{nameof(GetProviderMenuItemImage)}""
                     tag=""{index}""
                     onAction=""{nameof(HandleProviderMenuSelection)}"" 
                     getEnabled=""{nameof(IsProviderEnabled)}"" />");
        }

        providerMenuItemsXml.AppendLine($@"<menuSeparator id=""providerMenuSeparator"" />");
        providerMenuItemsXml.AppendLine(
            $@"<button id=""{nameof(ModelGroupControlIds.ProviderSettingsButton)}""
                 getLabel=""{nameof(GetProviderSettingsButtonLabel)}""
                 onAction=""{nameof(ShowProviderSettingsForm)}"" />");

        return $"""
            <group id="{nameof(ModelGroupControlIds.ModelProviderGroup)}" label="Model">
                <splitButton id="{nameof(ModelGroupControlIds.ProviderSplitButton)}" size="large" showLabel="false">
                    <button id="{nameof(ModelGroupControlIds.ProviderDisplayButton)}"
                            label="{nameof(Provider)}"
                            getImage="{nameof(GetSelectedProviderImage)}"
                            showImage="true"
                            showLabel="true"
                            onAction="{nameof(ShowProviderSettingsForm)}"
                            />
                    <menu id="{nameof(ModelGroupControlIds.ProviderSelectionMenu)}" itemSize="normal">
                        {providerMenuItemsXml}
                    </menu>
                </splitButton>
                <box id="{nameof(ModelGroupControlIds.VerticalContainer)}" boxStyle="vertical">
                    <box id="{nameof(ModelGroupControlIds.ProviderModelBox)}" boxStyle="horizontal">
                        <comboBox id="{nameof(ModelGroupControlIds.ModelComboBox)}"
                                  label="Model"
                                  showLabel="false"
                                  sizeString="WWWWWWWWWWWWW"
                                  getText="{nameof(GetSelectedModelText)}"
                                  onChange="{nameof(OnModelComboBoxChange)}"
                                  getItemCount="{nameof(GetModelComboBoxItemCount)}"
                                  getItemLabel="{nameof(GetModelComboBoxItemLabel)}"
                                  />
                        <comboBox id="{nameof(ModelGroupControlIds.TemperatureComboBox)}"
                                 label="Temp"
                                 showLabel="false"
                                 sizeString="0.0"
                                 screentip="Temperature (0.0-1.0). Lower values make responses more deterministic. Automatically scaled for providers with other ranges."
                                 getText="{nameof(GetTemperatureText)}"
                                 onChange="{nameof(OnTemperatureChange)}"
                                 getItemCount="{nameof(GetTemperatureItemCount)}"
                                 getItemLabel="{nameof(GetTemperatureItemLabel)}"
                                 />
                    </box>
                </box>
                <separator id="cacheSeparator" />
                <toggleButton id="{nameof(ModelGroupControlIds.CacheToggleButton)}" label="Cache" size="large" imageMso="SourceControlRefreshStatus"
                    screentip="Enable/disable local caching of model responses. Enabled: Return cached responses for identical prompts. Disabled: Always request new responses. Disabling cache will clear entries."
                    onAction="{nameof(OnCacheToggled)}" getPressed="{nameof(GetCachePressed)}" />
            </group>
            """;
    }

    public bool IsProviderEnabled(IRibbonControl control)
    {
        var accountConfiguration = CellmAddIn.Services.GetRequiredService<IOptionsMonitor<AccountConfiguration>>();

        if (!accountConfiguration.CurrentValue.IsEnabled)
        {
            return true;
        }

        var account = CellmAddIn.Services.GetRequiredService<Account>();

        // The 'tag' property of the menu button holds the index we stored.

        if (int.TryParse(control.Tag, out var index))
        {
            if (_providerItems.TryGetValue(index, out var item) && !string.IsNullOrEmpty(item.Image))
            {
                // Use smaller size for menu items (e.g., 16x16)
                return account.HasEntitlement(item.Entitlement); // Adjust size as needed
            }

            _logger.LogWarning("Could not get image for menu item index {index}.", index);
        }
        else
        {
            _logger.LogWarning("Could not parse index from tag '{tag}' for menu item '{id}'.", control.Tag, control.Id);
        }

        return false; // Or a default placeholder image
    }

    /// <summary>
    /// Gets the image for a specific item in the dropdown list.
    /// </summary>
    public Bitmap? GetProviderItemImage(IRibbonControl control, int index)
    {
        if (_providerItems.TryGetValue(index, out var item) && !string.IsNullOrEmpty(item.Image))
        {
            return ImageLoader.LoadEmbeddedPngResized(item.Image, 64, 64);
        }
        return null;
    }

    /// <summary>
    /// Gets the index of the currently selected provider.
    /// </summary>
    public int GetSelectedProviderIndex(IRibbonControl control)
    {
        return _selectedProviderIndex;
    }

    public string GetSelectedProviderLabel(IRibbonControl control)
    {
        // Ensure _selectedProviderIndex is valid before accessing _providerItems
        if (_providerItems.TryGetValue(_selectedProviderIndex, out var item))
        {
            return item.Label;
        }

        _logger.LogWarning("index {index} not found in _providerItems. Returning default label.", _selectedProviderIndex);

        return "Select Provider"; // Fallback label
    }

    public Bitmap? GetSelectedProviderImage(IRibbonControl control)
    {
        if (_providerItems.TryGetValue(_selectedProviderIndex, out var item) && !string.IsNullOrEmpty(item.Image))
        {
            // Use appropriate size for the main split button display (e.g., 32x32 or 24x24)
            return ImageLoader.LoadEmbeddedPngResized(item.Image, 128, 128); // Adjust size as needed
        }

        _logger.LogWarning("Could not get image for index {index}.", _selectedProviderIndex);

        return null;
    }

    public Bitmap? GetProviderMenuItemImage(IRibbonControl control)
    {
        // The 'tag' property of the menu button holds the index we stored.
        if (int.TryParse(control.Tag, out var index))
        {
            if (_providerItems.TryGetValue(index, out var item) && !string.IsNullOrEmpty(item.Image))
            {
                // Use smaller size for menu items (e.g., 16x16)
                return ImageLoader.LoadEmbeddedPngResized(item.Image, 16, 16); // Adjust size as needed
            }

            _logger.LogWarning("Could not get image for menu item index {index}.", index);
        }
        else
        {
            _logger.LogWarning("Could not parse index from tag '{tag}' for menu item '{id}'.", control.Tag, control.Id);
        }

        return null; // Or a default placeholder image
    }

    /// <summary>
    /// Gets the currently selected/configured default model name for the ComboBox text.
    /// </summary>
    /// <summary>
    /// Gets the text for the ComboBox. Shows the current default model,
    /// or a placeholder prompting the user based on whether presets are available.
    /// </summary>
    public string? GetSelectedModelText(IRibbonControl control)
    {
        var provider = GetCurrentProvider();
        var configKey = $"{provider}Configuration:{nameof(CellmAddInConfiguration.DefaultModel)}";
        var currentModel = string.Empty;
        var presetsAvailable = false;

        try
        {
            // Check if a default model is already set
            currentModel = GetValue(configKey); // Might throw KeyNotFoundException if never set
        }
        catch (KeyNotFoundException)
        {
            currentModel = string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError("Error reading model config '{configKey}': {message}", configKey, ex.Message);
            return "Error"; // Fallback on error
        }

        // Check if preset models (Small/Medium/Large) are configured
        presetsAvailable = GetAvailableModelNamesForProvider(provider).Count > 0;


        if (!string.IsNullOrEmpty(currentModel))
        {
            // If a model is already configured (preset or custom), display it
            return currentModel;
        }
        else
        {
            // No model is currently configured
            if (presetsAvailable)
            {
                // Presets exist, prompt user to select from dropdown
                return "[Select Model]";
            }
            else
            {
                // No presets exist (dropdown will be empty), prompt user to type
                return "[Type model]";
            }
        }
    }

    public void OnModelComboBoxChange(IRibbonControl control, string text)
    {
        var provider = GetCurrentProvider();
        var configKey = $"{provider}Configuration:{nameof(OpenAiConfiguration.DefaultModel)}"; // Using OpenAI as template name


        if (text == NoPresetsPlaceholder)
        {
            _logger.LogDebug("Ignoring change event because placeholder text was selected/entered. Invalidating control.");

            _ribbonUi?.InvalidateControl(control.Id);
            return;
        }


        if (string.IsNullOrWhiteSpace(text))
        {
            _logger.LogDebug("Warning: Model name cannot be empty. Change ignored.");
            _ribbonUi?.InvalidateControl(control.Id);
            return;
        }

        try
        {
            // Only save if it wasn't the placeholder or whitespace
            SetValue(configKey, text);
        }
        catch (Exception ex)
        {
            _logger.LogError("ERROR updating DefaultModel setting '{configKey}' to '{text}': {ex.Message}", configKey, text, ex.Message);
        }
    }

    /// <summary>
    /// Gets the number of items for the ComboBox dropdown.
    /// Returns 1 for a placeholder message if no real presets are configured.
    /// </summary>
    public int GetModelComboBoxItemCount(IRibbonControl control)
    {
        var provider = GetCurrentProvider();
        var actualCount = GetAvailableModelNamesForProvider(provider).Count;

        if (actualCount == 0)
        {
            _logger.LogDebug("GetModelComboBoxItemCount for {provider}: No actual presets, returning 1 (for placeholder).", provider);
            return 1; // Return 1 to show our placeholder message
        }
        else
        {
            _logger.LogDebug("GetModelComboBoxItemCount for {provider}: Returning actual count {actualCount}.", provider, actualCount);
            return actualCount;
        }
    }

    /// <summary>
    /// Gets the label for a specific item in the ComboBox dropdown list by index.
    /// Shows a placeholder message if no real presets are available.
    /// </summary>
    public string GetModelComboBoxItemLabel(IRibbonControl control, int index)
    {
        var provider = GetCurrentProvider();
        var availableModels = GetAvailableModelNamesForProvider(provider);

        if (availableModels.Count == 0) // Check if we are in the placeholder scenario
        {
            if (index == 0)
            {
                _logger.LogDebug("GetModelComboBoxItemLabel for {provider}, index {index}: Returning placeholder.", provider, index);
                return NoPresetsPlaceholder; // Return the placeholder message
            }
            else
            {
                // This case should not happen if GetItemCount returns 1
                _logger.LogDebug("Warning: Invalid index {index} requested for GetModelComboBoxItemLabel when showing placeholder.", index);
                return string.Empty;
            }
        }
        else // We have actual presets
        {
            if (index >= 0 && index < availableModels.Count)
            {
                var label = availableModels[index];
                _logger.LogDebug("GetModelComboBoxItemLabel for {provider}, index {index}: Returning actual model '{label}'.", provider, index, label);
                return label;
            }
            else
            {
                _logger.LogDebug("Warning: Invalid index {index} requested for GetModelComboBoxItemLabel (Provider: {provider}, Count: {availableModels.Count})", index, provider, availableModels.Count);
                return string.Empty;
            }
        }
    }

    /// <summary>
    /// Helper method to get a list of *configured* Small, Big, Thinking model names for the current provider.
    /// </summary>
    private List<string> GetAvailableModelNamesForProvider(Provider provider)
    {
        var modelNames = new List<string>();
        var small = GetModelNameForProvider(provider, "SmallModel");
        var big = GetModelNameForProvider(provider, "MediumModel");
        var thinking = GetModelNameForProvider(provider, "LargeModel");

        if (!string.IsNullOrEmpty(small) && !small.StartsWith("No ")) modelNames.Add(small);
        if (!string.IsNullOrEmpty(big) && !big.StartsWith("No ")) modelNames.Add(big);
        if (!string.IsNullOrEmpty(thinking) && !thinking.StartsWith("No ")) modelNames.Add(thinking);

        // Remove duplicates
        return modelNames.Distinct().ToList();
    }

    private string GetModelNameForProvider(Provider provider, string modelType)
    {
        try
        {
            var configuration = CellmAddIn.Services.GetRequiredService<IConfiguration>();
            var key = $"{provider}Configuration:{modelType}";
            return configuration[key] ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError("Error retrieving model name for {provider}/{modelType}: {ex.Message}", provider, modelType, ex.Message);
            return string.Empty ;
        }
    }


    public void HandleProviderMenuSelection(IRibbonControl control)
    {
        // ... (parsing index and getting selectedProviderItem logic remains the same) ...
        if (int.TryParse(control.Tag, out var index))
        {
            if (_providerItems.ContainsKey(index))
            {
                var selectedProviderItem = _providerItems[index];
                if (_selectedProviderIndex != index) // Only update if selection actually changed
                {
                    _selectedProviderIndex = index;

                    try
                    {
                        if (Enum.TryParse<Provider>(selectedProviderItem.Label, true, out var selectedProviderEnum))
                        {
                            SetValue($"{nameof(CellmAddInConfiguration)}:{nameof(CellmAddInConfiguration.DefaultProvider)}", selectedProviderEnum.ToString());
                        }
                        else
                        {
                            _logger.LogWarning("Could not parse '{label}' to Provider enum.", selectedProviderItem.Label);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug("Error updating DefaultProvider setting: {message}", ex.Message);
                    }

                    // Invalidate controls that need updating
                    if (_ribbonUi != null)
                    {
                        _ribbonUi.InvalidateControl(nameof(ModelGroupControlIds.ProviderSplitButton));
                        _ribbonUi.InvalidateControl(nameof(ModelGroupControlIds.ProviderDisplayButton));
                        _ribbonUi.InvalidateControl(nameof(ModelGroupControlIds.ModelComboBox));
                        _ribbonUi.InvalidateControl(nameof(ModelGroupControlIds.ProviderSettingsButton));
                    }
                }
            }
            else
            {
                _logger.LogWarning("Invalid index from tag: {index}", index);
            }
        }
        else
        {
            _logger.LogWarning("Could not parse index from tag '{tag}' for selected menu item '{id}'.", control.Tag, control.Id);
        }
    }

    public string GetSelectedModelLabel(IRibbonControl control)
    {
        var provider = GetCurrentProvider();
        var configKey = $"{provider}Configuration:{nameof(OpenAiConfiguration.DefaultModel)}"; // Using OpenAI as template name
        try
        {
            var model = GetValue(configKey);
            return model;
        }
        catch (KeyNotFoundException)
        {
            return "Select Model"; // Fallback
        }
        catch (Exception ex)
        {
            _logger.LogError("Error reading model config '{configKey}': {message}", configKey, ex.Message);
            return "Error"; // Fallback
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

        SetValue($"{nameof(CellmAddInConfiguration)}:{nameof(CellmAddInConfiguration.DefaultProvider)}", provider.ToString());
        SetValue($"{provider}Configuration:{nameof(OpenAiConfiguration.DefaultModel)}", model);

        _ribbonUi?.Invalidate();
    }

    /// <summary>
    /// Gets the label for the settings button in the provider dropdown menu.
    /// </summary>
    public string GetProviderSettingsButtonLabel(IRibbonControl control)
    {
        var providerName = "Unknown";
        if (_providerItems.TryGetValue(_selectedProviderIndex, out var item))
        {
            providerName = item.Label;
        }
        return $"{providerName} Settings...";
    }

    /// <summary>
    /// Handles the click event for the settings button. Opens the Provider Settings Form.
    /// </summary>
    public void ShowProviderSettingsForm(IRibbonControl control)
    {
        var provider = GetCurrentProvider();
        var currentApiKey = "";
        var currentBaseAddress = "";

        // Safely get current values
        try
        {
            currentApiKey = GetValue($"{provider}Configuration:ApiKey");
        }
        catch (KeyNotFoundException)
        {
            // Ignore, leave empty 
        }
        catch (Exception ex)
        {
            _logger.LogError("Error getting ApiKey for {provider}: {message}", provider, ex.Message);
        }

        try
        {
            // Determine if BaseAddress is relevant and get its value if so
            switch (provider)
            {
                case Provider.DeepSeek:
                    currentBaseAddress = GetProviderConfiguration<DeepSeekConfiguration>()?.BaseAddress?.ToString() ?? "";
                    break;
                case Provider.Mistral:
                    currentBaseAddress = GetProviderConfiguration<MistralConfiguration>()?.BaseAddress?.ToString() ?? "";
                    break;
                case Provider.Anthropic:
                    currentBaseAddress = GetProviderConfiguration<AnthropicConfiguration>()?.BaseAddress?.ToString() ?? "";
                    break;
                case Provider.OpenAi:
                    currentBaseAddress = GetProviderConfiguration<OpenAiConfiguration>()?.BaseAddress?.ToString() ?? "";
                    break;
                case Provider.Ollama:
                    currentBaseAddress = GetProviderConfiguration<OllamaConfiguration>()?.BaseAddress?.ToString() ?? "";
                    break;
                case Provider.OpenAiCompatible:
                    currentBaseAddress = GetProviderConfiguration<OpenAiCompatibleConfiguration>()?.BaseAddress?.ToString() ?? "";
                    break;
                // Add other providers if they have a BaseAddress property, even if fixed
                case Provider.Cellm: // Assuming Cellm doesn't have a user-settable BaseAddress
                    currentBaseAddress = GetProviderConfiguration<CellmConfiguration>()?.BaseAddress?.ToString() ?? "";
                    break;
                default:
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Error getting BaseAddress for {provider}: {message}", provider, ex.Message);
        }

        // Instantiate and show the form
        using var settingsForm = new ProviderSettingsForm(provider, currentApiKey, currentBaseAddress);
        var result = settingsForm.ShowDialog(); // Show modally

        if (result == DialogResult.OK)
        {
            // User clicked OK, save the potentially updated values
            var newApiKey = settingsForm.ApiKey;
            var newBaseAddress = settingsForm.BaseAddress;

            try
            {
                if (IsApiKeyEditable(provider))
                {
                    SetValue($"{provider}Configuration:ApiKey", newApiKey);
                }

                // Save BaseAddress only if it's relevant AND editable for this provider
                if (IsBaseAddressEditable(provider))
                {
                    SetValue($"{provider}Configuration:BaseAddress", newBaseAddress);
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug("ERROR saving settings for {provider}: {message}", provider, ex.Message);
                MessageBox.Show($"Error saving settings: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    static internal bool IsApiKeyEditable(Provider provider)
    {
        return provider switch
        {
            Provider.Cellm => false,
            _ => true
        };
    }

    static internal bool IsBaseAddressEditable(Provider provider)
    {
        return provider switch
        {
            Provider.Azure or Provider.Gemini or Provider.Ollama or Provider.OpenAiCompatible => true,
            _ => false
        };
    }

    // Array of temperature suggestions
    private static readonly string[] TemperatureOptions = ["0.0", "0.3", "0.7", "1.0"];

    public string GetTemperatureText(IRibbonControl control)
    {
        try
        {
            var temperature = GetValue($"{nameof(CellmAddInConfiguration)}:{nameof(CellmAddInConfiguration.DefaultTemperature)}");

            if (double.TryParse(temperature, out var tempVal))
            {
                return tempVal.ToString("0.0");
            }

            return "0.0";
        }
        catch (KeyNotFoundException)
        {
            // If not found, set a default value
            SetValue($"{nameof(CellmAddInConfiguration)}:{nameof(CellmAddInConfiguration.DefaultTemperature)}", "0");
            return "0.0";
        }
        catch (Exception ex)
        {
            _logger.LogError("Error reading temperature: {message}", ex.Message);
            return "0.0"; // Fallback
        }
    }

    public void OnTemperatureChange(IRibbonControl control, string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            _logger.LogDebug("Warning: Temperature cannot be empty. Change ignored.");
            _ribbonUi?.InvalidateControl(control.Id);
            return;
        }

        // Validate that the input is a valid temperature (between 0 and 1)
        if (double.TryParse(text, out var temperature))
        {
            if (temperature < 0 || temperature > 1)
            {
                MessageBox.Show("Temperature must be between 0 and 1.", "Invalid Temperature",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _ribbonUi?.InvalidateControl(control.Id);
                return;
            }

            try
            {
                // Format to ensure consistent display
                var formattedTemperature = temperature.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
                SetValue($"{nameof(CellmAddInConfiguration)}:{nameof(CellmAddInConfiguration.DefaultTemperature)}", formattedTemperature);
            }
            catch (Exception ex)
            {
                _logger.LogDebug("ERROR updating DefaultTemperature setting: {message}", ex.Message);
            }
        }
        else
        {
            MessageBox.Show("Temperature must a number between 0 and 1.", "Invalid Temperature",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _ribbonUi?.InvalidateControl(control.Id);
        }
    }

    public int GetTemperatureItemCount(IRibbonControl control)
    {
        // Return the number of temperature options
        return TemperatureOptions.Length;
    }

    public string GetTemperatureItemLabel(IRibbonControl control, int index)
    {
        // Return the temperature option at the specified index
        if (index >= 0 && index < TemperatureOptions.Length)
        {
            return TemperatureOptions[index];
        }

        _logger.LogWarning("Invalid index {index} requested for GetTemperatureItemLabel", index);
        return string.Empty;
    }
}
