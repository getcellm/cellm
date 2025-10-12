using System.Globalization;
using System.Text;
using Cellm.AddIn.UserInterface.Forms;
using Cellm.Models.Providers;
using Cellm.Models.Providers.Anthropic;
using Cellm.Models.Providers.Aws;
using Cellm.Models.Providers.Azure;
using Cellm.Models.Providers.Cellm;
using Cellm.Models.Providers.DeepSeek;
using Cellm.Models.Providers.Google;
using Cellm.Models.Providers.Mistral;
using Cellm.Models.Providers.Ollama;
using Cellm.Models.Providers.OpenAi;
using Cellm.Models.Providers.OpenAiCompatible;
using Cellm.Users;
using ExcelDna.Integration.CustomUI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cellm.AddIn.UserInterface.Ribbon;

public partial class RibbonMain
{
    public enum ModelGroupControlIds
    {
        VerticalContainer,
        HorizontalContainer,

        ModelProviderGroup,
        ProviderModelBox,
        ProviderSplitButton,
        ProviderDisplayButton,
        ProviderMenu,

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

    public string ModelGroup()
    {
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
                    <menu id="{nameof(ModelGroupControlIds.ProviderMenu)}" itemSize="normal">
                        {GetProviderMenuItems()}
                    </menu>
                </splitButton>
                <separator id="providerSeparator" />
                <box id="{nameof(ModelGroupControlIds.VerticalContainer)}" boxStyle="vertical">
                    <box id="{nameof(ModelGroupControlIds.ProviderModelBox)}" boxStyle="horizontal">
                        <comboBox id="{nameof(ModelGroupControlIds.ModelComboBox)}"
                                  label="Model"
                                  showLabel="false"
                                  sizeString="WWWWWWWWWWWWW"
                                  getText="{nameof(GetSelectedModelText)}"
                                  onChange="{nameof(OnModelComboBoxChange)}"
                                  getItemCount="{nameof(GetModelComboBoxItemCount)}"
                                  getItemLabel="{nameof(GetModelComboBoxItemLabel)}" />
                        <comboBox id="{nameof(ModelGroupControlIds.TemperatureComboBox)}"
                                 label="Temp"
                                 showLabel="false"
                                 sizeString="Consistent"
                                 screentip="Temperature. Controls the balance between deterministic outputs and creative exploration. Fow low values the model will almost always give you the same responses for the same prompts, for high values the responses to the same prompts will vary. Must be a number between 0.0 and 1.0 or Consistent (0.0), Neutral (0.3), or Creative (0.7)."
                                 getText="{nameof(GetTemperatureText)}"
                                 onChange="{nameof(OnTemperatureChange)}"
                                 getItemCount="{nameof(GetTemperatureItemCount)}"
                                 getItemLabel="{nameof(GetTemperatureItemLabel)}" />
                    </box>
                    {ModelGroupStatistics()}
                </box>
                <separator id="cacheSeparator" />
                <toggleButton id="{nameof(ModelGroupControlIds.CacheToggleButton)}" 
                    getLabel="{nameof(GetCacheLabel)}"
                    size="large" 
                    imageMso="TableAutoFormat"
                    screentip="Enable/disable local caching of model responses. Enabled: Return cached responses for identical prompts. Disabled: Always return new responses. Disabling memory will wipe saved responses."
                    onAction="{nameof(OnCacheToggled)}" 
                    getPressed="{nameof(GetCachePressed)}" />
            </group>
            """;
    }

    public string GetProviderMenuItems()
    {
        var providerConfigurations = CellmAddIn.GetProviderConfigurations();

        var providerMenuItemsXml = providerConfigurations
            .Where(providerConfiguration => providerConfiguration != null && providerConfiguration.IsEnabled)
            .Select(providerConfiguration =>
                $@"<button id=""{GetProviderMenuItemId(providerConfiguration.Id)}""
                    label=""{providerConfiguration.Name}""
                    tag=""{providerConfiguration.Id}""
                    getImage=""{nameof(GetProviderImage)}""
                    onAction=""{nameof(OnProviderSelected)}"" 
                    getEnabled=""{nameof(IsProviderEnabled)}"" />");

        var providerMenuItems = new StringBuilder();
        providerMenuItems.AppendJoin(Environment.NewLine, providerMenuItemsXml);
        providerMenuItems.AppendLine($@"<menuSeparator id=""providerMenuSeparator"" />");
        providerMenuItems.AppendLine(
            $@"<button id=""{nameof(ModelGroupControlIds.ProviderSettingsButton)}""
                 getLabel=""{nameof(GetProviderSettingsButtonLabel)}""
                 onAction=""{nameof(ShowProviderSettingsForm)}"" />");

        return providerMenuItems.ToString();
    }

    private string GetProviderMenuItemId(Provider provider)
    {
        return $"{ModelGroupControlIds.ProviderMenu}.{provider}";
    }

    public string GetCacheLabel(IRibbonControl control)
    {
        var isEnabledString = GetValue($"{nameof(CellmAddInConfiguration)}:{nameof(CellmAddInConfiguration.EnableCache)}");

        if (!bool.TryParse(isEnabledString, out var isEnabled))
        {
            return "Memory";
        }

        return isEnabled ? "Memory On" : "Memory Off";
    }

    public bool IsProviderEnabled(IRibbonControl control)
    {
        var accountConfiguration = CellmAddIn.Services.GetRequiredService<IOptionsMonitor<AccountConfiguration>>();

        if (!accountConfiguration.CurrentValue.IsEnabled)
        {
            return true;
        }

        var account = CellmAddIn.Services.GetRequiredService<Account>();

        if (Enum.TryParse<Provider>(control.Tag, true, out var provider))
        {
            return account.HasEntitlement(CellmAddIn.GetProviderConfiguration(provider).Entitlement);
        }

        _logger.LogWarning("Could not parse tag '{tag}' for menu item '{id}'.", control.Tag, control.Id);

        return false; // Or a default placeholder image
    }

    public Bitmap? GetSelectedProviderImage(IRibbonControl control)
    {
        var providerAsString = GetValue($"{nameof(CellmAddInConfiguration)}:{nameof(CellmAddInConfiguration.DefaultProvider)}");

        if (Enum.TryParse<Provider>(providerAsString, true, out var provider))
        {
            var resource = CellmAddIn.GetProviderConfiguration(provider).Icon;

            if (resource.ToLower().EndsWith("png"))
            {
                return ImageLoader.LoadEmbeddedPngResized(resource, 128, 128);
            }

            if (resource.ToLower().EndsWith("svg"))
            {
                return ImageLoader.LoadEmbeddedSvgResized(resource, 128, 128);
            }
        }

        _logger.LogWarning("Could not get image for provider {provider}.", providerAsString);

        return null;
    }

    public Bitmap? GetProviderImage(IRibbonControl control)
    {
        if (Enum.TryParse<Provider>(control.Tag, true, out var provider))
        {
            var resource = CellmAddIn.GetProviderConfiguration(provider).Icon;

            if (resource.ToLower().EndsWith("png"))
            {
                return ImageLoader.LoadEmbeddedPngResized(resource, 128, 128);
            }

            if (resource.ToLower().EndsWith("svg"))
            {
                return ImageLoader.LoadEmbeddedSvgResized(resource, 128, 128);
            }
        }

        _logger.LogWarning("Could not parse index from tag '{tag}' for menu item '{id}'.", control.Tag, control.Id);

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

        if (!string.IsNullOrEmpty(currentModel))
        {
            // If a model is already configured (preset or custom), display it
            return currentModel;
        }

        return string.Empty;
    }

    public void OnModelComboBoxChange(IRibbonControl control, string text)
    {
        var provider = GetCurrentProvider();
        var configKey = $"{provider}Configuration:{nameof(OpenAiConfiguration.DefaultModel)}";


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
            return string.Empty;
        }
    }


    public void OnProviderSelected(IRibbonControl control)
    {
        if (!Enum.TryParse<Provider>(control.Tag, true, out var newProvider))
        {
            _logger.LogWarning("Could not parse provider tag {tag}.", control.Tag);
            return;
        }

        var oldProviderAsString = GetValue($"{nameof(CellmAddInConfiguration)}:{nameof(CellmAddInConfiguration.DefaultProvider)}");

        if (!Enum.TryParse<Provider>(oldProviderAsString, true, out var oldProvider))
        {
            _logger.LogWarning("Could not parse provider {oldProviderAsString}.", oldProviderAsString);
            return;
        }

        if (oldProvider == newProvider)
        {
            return;
        }

        SetValue($"{nameof(CellmAddInConfiguration)}:{nameof(CellmAddInConfiguration.DefaultProvider)}", newProvider.ToString());

        InvalidateModelControls();

        // Invalidate structured output buttons which enabled/disabled state depend on combination of provider and tool use
        _ribbonUi?.InvalidateControl(nameof(PromptGroupControlIds.PromptToRow));
        _ribbonUi?.InvalidateControl(nameof(PromptGroupControlIds.PromptToColumn));
        _ribbonUi?.InvalidateControl(nameof(PromptGroupControlIds.PromptToRange));
    }

    /// <summary>
    /// Gets the label for the settings button in the provider dropdown menu.
    /// </summary>
    public string GetProviderSettingsButtonLabel(IRibbonControl control)
    {
        var providerAsString = GetValue($"{nameof(CellmAddInConfiguration)}:{nameof(CellmAddInConfiguration.DefaultProvider)}");

        if (!Enum.TryParse<Provider>(providerAsString, true, out var provider))
        {
            _logger.LogWarning("Could not parse provider {provider}", provider);
            throw new ArgumentException($"Invalid provider: {providerAsString}");
        }

        var providerConfiguration = CellmAddIn.GetProviderConfiguration(provider);

        return $"{providerConfiguration.Name} settings...";
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
                case Provider.Azure:
                    currentBaseAddress = GetProviderConfiguration<AzureConfiguration>()?.BaseAddress?.ToString() ?? "";
                    break;
                case Provider.Aws:
                    currentBaseAddress = GetProviderConfiguration<AwsConfiguration>()?.BaseAddress?.ToString() ?? "";
                    break;
                case Provider.Cellm:
                    currentBaseAddress = GetProviderConfiguration<CellmConfiguration>()?.BaseAddress?.ToString() ?? "";
                    break;
                case Provider.DeepSeek:
                    currentBaseAddress = GetProviderConfiguration<DeepSeekConfiguration>()?.BaseAddress?.ToString() ?? "";
                    break;
                case Provider.Gemini:
                    currentBaseAddress = GetProviderConfiguration<GeminiConfiguration>()?.BaseAddress?.ToString() ?? "";
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
            Provider.Azure or Provider.Aws or Provider.OpenAiCompatible => true,
            _ => false
        };
    }

    // Array of temperature suggestions
    private static readonly string[] TemperatureOptions = ["Consistent", "Neutral", "Creative"];

    public string GetTemperatureText(IRibbonControl control)
    {
        try
        {
            var temperature = GetValue($"{nameof(CellmAddInConfiguration)}:{nameof(CellmAddInConfiguration.DefaultTemperature)}");

            if (temperature == "Consistent")
            {
                return temperature;
            }

            if (temperature == "Neutral")
            {
                return temperature;
            }

            if (temperature == "Creative")
            {
                return temperature;
            }

            if (double.TryParse(temperature, out var tempVal))
            {
                if (tempVal == 0)
                {
                    return "Consistent";
                }

                if (tempVal == 0.3)
                {
                    return "Neutral";
                }

                if (tempVal == 0.7)
                {
                    return "Creative";
                }

                return tempVal.ToString("0.0");
            }

            return "Deterministic";
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

    public void OnTemperatureChange(IRibbonControl control, string temperatureAsString)
    {
        if (string.IsNullOrWhiteSpace(temperatureAsString))
        {
            _logger.LogDebug("Warning: Temperature cannot be empty. Change ignored.");
            _ribbonUi?.InvalidateControl(control.Id);
            return;
        }

        if (temperatureAsString == "Consistent")
        {
            temperatureAsString = "0";
        }

        if (temperatureAsString == "Neutral")
        {
            temperatureAsString = "0.3";
        }

        if (temperatureAsString == "Creative")
        {
            temperatureAsString = "0.7";
        }

        // Validate that the input is a valid temperature (between 0 and 1)
        if (double.TryParse(temperatureAsString.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var temperature))
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

    public async Task OnCacheToggled(IRibbonControl control, bool enabled)
    {
        if (!enabled)
        {
            var cache = CellmAddIn.Services.GetRequiredService<HybridCache>();
            await cache.RemoveByTagAsync(nameof(ProviderResponse));

        }

        SetValue($"{nameof(CellmAddInConfiguration)}:{nameof(CellmAddInConfiguration.EnableCache)}", enabled);
        _ribbonUi?.InvalidateControl(control.Id);
    }

    public bool GetCachePressed(IRibbonControl control)
    {
        return bool.Parse(GetValue($"{nameof(CellmAddInConfiguration)}:{nameof(CellmAddInConfiguration.EnableCache)}"));
    }

    private void InvalidateModelControls()
    {
        if (_ribbonUi == null)
        {
            return;
        }

        _ribbonUi.InvalidateControl(nameof(ModelGroupControlIds.ProviderMenu));

        var ids = CellmAddIn.GetProviderConfigurations()
            .Where(providerConfiguration => providerConfiguration != null && providerConfiguration.IsEnabled)
            .Select(providerConfiguration => GetProviderMenuItemId(providerConfiguration.Id));

        foreach (var id in ids)
        {
            _ribbonUi.InvalidateControl(id);
        }

        _ribbonUi.InvalidateControl(nameof(ModelGroupControlIds.ModelComboBox));
        _ribbonUi.InvalidateControl(nameof(ModelGroupControlIds.ProviderSplitButton));
        _ribbonUi.InvalidateControl(nameof(ModelGroupControlIds.ProviderDisplayButton));
        _ribbonUi.InvalidateControl(nameof(ModelGroupControlIds.ProviderSettingsButton));
    }
}
