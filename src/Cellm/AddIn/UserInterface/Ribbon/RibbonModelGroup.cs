using Cellm.Models.Providers.Anthropic;
using Cellm.Models.Providers.Cellm;
using Cellm.Models.Providers.DeepSeek;
using Cellm.Models.Providers.Mistral;
using Cellm.Models.Providers.Ollama;
using Cellm.Models.Providers.OpenAi;
using Cellm.Models.Providers.OpenAiCompatible;
using Cellm.Models.Providers;
using System.Diagnostics;
using System.Text;
using ExcelDna.Integration.CustomUI;
using Cellm.AddIn.UserInterface.Forms;
using Cellm.Users;
using Microsoft.Extensions.DependencyInjection;
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

    private readonly Dictionary<int, ProviderItem> _providerItems = new Dictionary<int, ProviderItem>
    {
        [0] = new ProviderItem { Id = $"{nameof(Provider)}.{nameof(Provider.Anthropic)}", Image = $"{ResourcesBasePath}/anthropic.png", Label = nameof(Provider.Anthropic), Entitlement = Entitlement.EnableAnthropicProvider },
        [1] = new ProviderItem { Id = $"{nameof(Provider)}.{nameof(Provider.DeepSeek)}", Image = $"{ResourcesBasePath}/deepseek.png", Label = nameof(Provider.DeepSeek), Entitlement = Entitlement.EnableDeepSeekProvider },
        [2] = new ProviderItem { Id = $"{nameof(Provider)}.{nameof(Provider.Mistral)}", Image = $"{ResourcesBasePath}/mistral.png", Label = nameof(Provider.Mistral), Entitlement = Entitlement.EnableMistralProvider },
        [3] = new ProviderItem { Id = $"{nameof(Provider)}.{nameof(Provider.Ollama)}", Image = $"{ResourcesBasePath}/ollama.png", Label = nameof(Provider.Ollama), Entitlement = Entitlement.EnableOllamaProvider },
        [4] = new ProviderItem { Id = $"{nameof(Provider)}.{nameof(Provider.OpenAi)}", Image = $"{ResourcesBasePath}/openai.png", Label = nameof(Provider.OpenAi), Entitlement = Entitlement.EnableOpenAiProvider },
        [5] = new ProviderItem { Id = $"{nameof(Provider)}.{nameof(Provider.OpenAiCompatible)}", Image = $"{ResourcesBasePath}/openai.png", Label = nameof(Provider.OpenAiCompatible) }
    };

    internal int _selectedProviderIndex = 0; // Default to the first item (Red)

    private void InitializeSelectedProviderIndex()
    {
        try
        {
            var defaultProviderName = GetValue($"{nameof(ProviderConfiguration)}:{nameof(ProviderConfiguration.DefaultProvider)}");
            var defaultProvider = Enum.Parse<Provider>(defaultProviderName, true);

            _selectedProviderIndex = _providerItems.FirstOrDefault(kvp => kvp.Value.Label.Equals(defaultProvider.ToString(), StringComparison.OrdinalIgnoreCase)).Key;
            // Fallback to 0 if not found, though EnsureDefaultProvider should handle this case.
            if (!_providerItems.ContainsKey(_selectedProviderIndex))
            {
                _selectedProviderIndex = 0; // Default to first item if lookup fails
            }
        }
        catch (KeyNotFoundException)
        {
            // Set default if missing (Ollama corresponds to index 3 in our example)
            SetValue($"{nameof(ProviderConfiguration)}:{nameof(ProviderConfiguration.DefaultProvider)}", nameof(Provider.Ollama));
            _selectedProviderIndex = _providerItems.FirstOrDefault(kvp => kvp.Value.Label.Equals(nameof(Provider.Ollama), StringComparison.OrdinalIgnoreCase)).Key;
            if (!_providerItems.ContainsKey(_selectedProviderIndex)) _selectedProviderIndex = 3; // Hardcode index if needed as fallback
        }
        catch (Exception ex) // Catch other potential errors during init
        {
            Debug.WriteLine($"Error initializing selected provider index: {ex.Message}");
            _selectedProviderIndex = 0; // Safe default
        }
    }

    public string ModelGroup()
    {
        var providerMenuItemsXml = new StringBuilder();
        // Dynamic provider menu items (IDs generated here, not in enum)
        foreach (var kvp in _providerItems.OrderBy(p => p.Value.Label))
        {
            int index = kvp.Key;
            ProviderItem item = kvp.Value;
            string menuItemId = item.Id; // Dynamic ID
            providerMenuItemsXml.AppendLine(
                $@"<button id=""{menuItemId}""
                     label=""{System.Security.SecurityElement.Escape(item.Label)}""
                     getImage=""{nameof(GetProviderMenuItemImage)}""
                     tag=""{index}""
                     onAction=""{nameof(HandleProviderMenuSelection)}"" 
                     getEnabled=""{nameof(IsProviderEnabled)}"" />");
        }

        // Add Separator and Settings Button
        providerMenuItemsXml.AppendLine($@"<menuSeparator id=""providerMenuSeparator"" />");
        providerMenuItemsXml.AppendLine(
            $@"<button id=""{nameof(ModelGroupControlIds.ProviderSettingsButton)}""
                 getLabel=""{nameof(GetProviderSettingsButtonLabel)}""
                 onAction=""{nameof(ShowProviderSettingsForm)}"" />");

        return $"""
            <group id="{nameof(ModelGroupControlIds.ModelProviderGroup)}" label="Model">
                <box id="{nameof(ModelGroupControlIds.VerticalContainer)}" boxStyle="vertical">
                    <box id="{nameof(ModelGroupControlIds.ProviderModelBox)}" boxStyle="horizontal">
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
                        <comboBox id="{nameof(ModelGroupControlIds.ModelComboBox)}"
                                  label="Model"
                                  showLabel="false"
                                  sizeString="WWWWWWWWWWWWW"
                                  getText="{nameof(GetSelectedModelText)}"
                                  onChange="{nameof(OnModelComboBoxChange)}"
                                  getItemCount="{nameof(GetModelComboBoxItemCount)}"
                                  getItemLabel="{nameof(GetModelComboBoxItemLabel)}"
                                  />
                    </box>
                </box>
                <toggleButton id="{nameof(ModelGroupControlIds.CacheToggleButton)}" label="Cache" size="large" imageMso="SourceControlRefreshStatus"
                    screentip="Enable/disable local caching of model responses. Disabling cache will clear all cached responses."
                    onAction="{nameof(OnCacheToggled)}" getPressed="{nameof(OnGetCachePressed)}" />
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
        if (int.TryParse(control.Tag, out int index))
        {
            Debug.WriteLine($"GetProviderMenuItemImage called for index: {index} (from tag)");
            if (_providerItems.TryGetValue(index, out var item) && !string.IsNullOrEmpty(item.Image))
            {
                // Use smaller size for menu items (e.g., 16x16)
                return account.HasEntitlement(item.Entitlement); // Adjust size as needed
            }
            Debug.WriteLine($"WARNING: Could not get image for menu item index {index}.");
        }
        else
        {
            Debug.WriteLine($"WARNING: Could not parse index from tag '{control.Tag}' for menu item '{control.Id}'.");
        }
        return false; // Or a default placeholder image
    }

    /// <summary>
    /// Gets the image for a specific item IN THE DROPDOWN LIST.
    /// </summary>
    public Bitmap? GetProviderItemImage(IRibbonControl control, int index)
    {
        Debug.WriteLine($"GetProviderItemImage called for index: {index}");
        if (_providerItems.TryGetValue(index, out var item) && !string.IsNullOrEmpty(item.Image))
        {
            // Assuming ImageLoader can handle embedded paths and resizing if needed
            return ImageLoader.LoadEmbeddedPngResized(item.Image); // Use appropriate size for dropdown item
        }
        return null;
    }

    /// <summary>
    /// Gets the index of the currently selected provider.
    /// </summary>
    public int GetSelectedProviderIndex(IRibbonControl control)
    {
        Debug.WriteLine($"GetSelectedProviderIndex called. Returning: {_selectedProviderIndex}");
        return _selectedProviderIndex;
    }

    public string GetSelectedProviderLabel(IRibbonControl control)
    {
        Debug.WriteLine($"GetSelectedProviderLabel (for splitButton) called. Index: {_selectedProviderIndex}");
        // Ensure _selectedProviderIndex is valid before accessing _providerItems
        if (_providerItems.TryGetValue(_selectedProviderIndex, out var item))
        {
            return item.Label;
        }
        Debug.WriteLine($"WARNING: _selectedProviderIndex {_selectedProviderIndex} not found in _providerItems. Returning default label.");
        return "Select Provider"; // Fallback label
    }

    // Keep this: Gets the image for the MAIN button part of the splitButton.
    public Bitmap? GetSelectedProviderImage(IRibbonControl control)
    {
        Debug.WriteLine($"GetSelectedProviderImage (for splitButton) called. Index: {_selectedProviderIndex}");
        if (_providerItems.TryGetValue(_selectedProviderIndex, out var item) && !string.IsNullOrEmpty(item.Image))
        {
            // Use appropriate size for the main split button display (e.g., 32x32 or 24x24)
            return ImageLoader.LoadEmbeddedPngResized(item.Image, 128, 128); // Adjust size as needed
        }
        Debug.WriteLine($"WARNING: Could not get image for index {_selectedProviderIndex}.");
        return null;
    }

    // --- NEW: Callback for Menu Item Images ---
    public Bitmap? GetProviderMenuItemImage(IRibbonControl control)
    {
        // The 'tag' property of the menu button holds the index we stored.
        if (int.TryParse(control.Tag, out int index))
        {
            Debug.WriteLine($"GetProviderMenuItemImage called for index: {index} (from tag)");
            if (_providerItems.TryGetValue(index, out var item) && !string.IsNullOrEmpty(item.Image))
            {
                // Use smaller size for menu items (e.g., 16x16)
                return ImageLoader.LoadEmbeddedPngResized(item.Image, 16, 16); // Adjust size as needed
            }
            Debug.WriteLine($"WARNING: Could not get image for menu item index {index}.");
        }
        else
        {
            Debug.WriteLine($"WARNING: Could not parse index from tag '{control.Tag}' for menu item '{control.Id}'.");
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
        string configKey = $"{provider}Configuration:{nameof(OpenAiConfiguration.DefaultModel)}"; // Using OpenAI as template name
        string currentModel = string.Empty;
        bool presetsAvailable = false;

        try
        {
            // Check if a default model is already set
            currentModel = GetValue(configKey); // Might throw KeyNotFoundException if never set
        }
        catch (KeyNotFoundException)
        {
            Debug.WriteLine($"DefaultModel key '{configKey}' not found for provider {provider}.");
            currentModel = string.Empty; // Explicitly null
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error reading model config '{configKey}': {ex.Message}");
            return "Error"; // Fallback on error
        }

        // Check if preset models (Small/Medium/Large) are configured
        presetsAvailable = GetAvailableModelNamesForProvider(provider).Count > 0;

        Debug.WriteLine($"GetSelectedModelText for {provider}: CurrentModel='{currentModel ?? "null"}', PresetsAvailable={presetsAvailable}");

        // Determine the text to display
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
                return "Select Model";
            }
            else
            {
                // No presets exist (dropdown will be empty), prompt user to type
                return "[Type model]"; // Short placeholder
            }
        }
    }

    public void OnModelComboBoxChange(IRibbonControl control, string text)
    {
        var provider = GetCurrentProvider();
        string configKey = $"{provider}Configuration:{nameof(OpenAiConfiguration.DefaultModel)}"; // Using OpenAI as template name

        Debug.WriteLine($"OnModelComboBoxChange for {provider}. Received text: '{text}'");

        // *** THIS IS THE CRITICAL PART ***
        if (text == NoPresetsPlaceholder)
        {
            Debug.WriteLine("Ignoring change event because placeholder text was selected/entered. Invalidating control.");
            // Force the UI to re-query the text for the box using GetSelectedModelText
            _ribbonUi?.InvalidateControl(control.Id);
            return; // IMPORTANT: Exit before saving or processing further
        }
        // *********************************

        if (string.IsNullOrWhiteSpace(text))
        {
            Debug.WriteLine("Warning: Model name cannot be empty. Change ignored.");
            _ribbonUi?.InvalidateControl(control.Id);
            return;
        }

        try
        {
            // Only save if it wasn't the placeholder or whitespace
            SetValue(configKey, text);
            Debug.WriteLine($"DefaultModel set to '{text}' for provider {provider}.");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"ERROR updating DefaultModel setting '{configKey}' to '{text}': {ex.Message}");
        }
        Debug.WriteLine("OnModelComboBoxChange finished.");
    }

    /// <summary>
    /// Gets the number of items for the ComboBox dropdown.
    /// Returns 1 for a placeholder message if no real presets are configured.
    /// </summary>
    public int GetModelComboBoxItemCount(IRibbonControl control)
    {
        var provider = GetCurrentProvider();
        int actualCount = GetAvailableModelNamesForProvider(provider).Count;

        if (actualCount == 0)
        {
            Debug.WriteLine($"GetModelComboBoxItemCount for {provider}: No actual presets, returning 1 (for placeholder).");
            return 1; // Return 1 to show our placeholder message
        }
        else
        {
            Debug.WriteLine($"GetModelComboBoxItemCount for {provider}: Returning actual count {actualCount}.");
            return actualCount; // Return the real count of presets
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
                Debug.WriteLine($"GetModelComboBoxItemLabel for {provider}, index {index}: Returning placeholder '{NoPresetsPlaceholder}'.");
                return NoPresetsPlaceholder; // Return the placeholder message
            }
            else
            {
                // This case should technically not happen if GetItemCount returns 1
                Debug.WriteLine($"Warning: Invalid index {index} requested for GetModelComboBoxItemLabel when showing placeholder.");
                return string.Empty;
            }
        }
        else // We have actual presets
        {
            if (index >= 0 && index < availableModels.Count)
            {
                string label = availableModels[index];
                Debug.WriteLine($"GetModelComboBoxItemLabel for {provider}, index {index}: Returning actual model '{label}'.");
                return label;
            }
            else
            {
                Debug.WriteLine($"Warning: Invalid index {index} requested for GetModelComboBoxItemLabel (Provider: {provider}, Count: {availableModels.Count})");
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
        string small = GetModelNameForProvider(provider, "Small");
        string big = GetModelNameForProvider(provider, "Big");
        string thinking = GetModelNameForProvider(provider, "Thinking");

        if (!string.IsNullOrEmpty(small) && !small.StartsWith("No ")) modelNames.Add(small);
        if (!string.IsNullOrEmpty(big) && !big.StartsWith("No ")) modelNames.Add(big);
        if (!string.IsNullOrEmpty(thinking) && !thinking.StartsWith("No ")) modelNames.Add(thinking);

        // Optional: Remove duplicates if Small/Big/Thinking might be configured to the same model
        return modelNames.Distinct().ToList();
    }

    private string GetModelNameForProvider(Provider provider, string modelType)
    {
        try
        {
            return provider switch
            {
                Provider.Cellm => modelType switch
                {
                    "Small" => GetProviderConfiguration<CellmConfiguration>()?.SmallModel ?? "No small model",
                    "Big" => GetProviderConfiguration<CellmConfiguration>()?.MediumModel ?? "No big model",
                    "Thinking" => GetProviderConfiguration<CellmConfiguration>()?.LargeModel ?? "No thinking model",
                    _ => "N/A"
                },
                Provider.Anthropic => modelType switch
                {
                    "Small" => GetProviderConfiguration<AnthropicConfiguration>()?.SmallModel ?? "No small model",
                    "Big" => GetProviderConfiguration<AnthropicConfiguration>()?.MediumModel ?? "No big model",
                    "Thinking" => GetProviderConfiguration<AnthropicConfiguration>()?.LargeModel ?? "No thinking model",
                    _ => "N/A"
                },
                Provider.DeepSeek => modelType switch
                {
                    "Small" => GetProviderConfiguration<DeepSeekConfiguration>()?.SmallModel ?? "No small model",
                    "Big" => GetProviderConfiguration<DeepSeekConfiguration>()?.MediumModel ?? "No big model",
                    "Thinking" => GetProviderConfiguration<DeepSeekConfiguration>()?.LargeModel ?? "No thinking model",
                    _ => "N/A"
                },
                Provider.Mistral => modelType switch
                {
                    "Small" => GetProviderConfiguration<MistralConfiguration>()?.SmallModel ?? "No small model",
                    "Big" => GetProviderConfiguration<MistralConfiguration>()?.MediumModel ?? "No big model",
                    "Thinking" => GetProviderConfiguration<MistralConfiguration>()?.LargeModel ?? "No thinking model",
                    _ => "N/A"
                },
                Provider.Ollama => modelType switch
                {
                    "Small" => GetProviderConfiguration<OllamaConfiguration>()?.SmallModel ?? "No small model",
                    "Big" => GetProviderConfiguration<OllamaConfiguration>()?.MediumModel ?? "No big model",
                    "Thinking" => GetProviderConfiguration<OllamaConfiguration>()?.LargeModel ?? "No thinking model",
                    _ => "N/A"
                },
                Provider.OpenAi => modelType switch
                {
                    "Small" => GetProviderConfiguration<OpenAiConfiguration>()?.SmallModel ?? "No small model",
                    "Big" => GetProviderConfiguration<OpenAiConfiguration>()?.MediumModel ?? "No big model",
                    "Thinking" => GetProviderConfiguration<OpenAiConfiguration>()?.LargeModel ?? "No thinking model",
                    _ => "N/A"
                },
                Provider.OpenAiCompatible => modelType switch
                {
                    "Small" => GetProviderConfiguration<OpenAiCompatibleConfiguration>()?.SmallModel ?? "No small model",
                    "Big" => GetProviderConfiguration<OpenAiCompatibleConfiguration>()?.MediumModel ?? "No big model",
                    "Thinking" => GetProviderConfiguration<OpenAiCompatibleConfiguration>()?.LargeModel ?? "No thinking model",
                    _ => "N/A"
                },
                _ => "N/A" // Default case for unhandled providers
            };
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error retrieving model name for {provider}/{modelType}: {ex.Message}");
            return string.Empty;
        }
    }


    public void HandleProviderMenuSelection(IRibbonControl control)
    {
        // ... (parsing index and getting selectedProviderItem logic remains the same) ...
        if (int.TryParse(control.Tag, out int index))
        {
            Debug.WriteLine($"HandleProviderMenuSelection called. Tag (Index): {index}, Control ID: {control.Id}");

            if (_providerItems.ContainsKey(index))
            {
                var selectedProviderItem = _providerItems[index];
                if (_selectedProviderIndex != index) // Only update if selection actually changed
                {
                    _selectedProviderIndex = index;
                    Debug.WriteLine($"_selectedProviderIndex updated to: {_selectedProviderIndex} ({selectedProviderItem.Label})");

                    // ... (Update DefaultProvider setting logic remains the same) ...
                    try
                    {
                        if (Enum.TryParse<Provider>(selectedProviderItem.Label, true, out var selectedProviderEnum))
                        {
                            SetValue($"{nameof(ProviderConfiguration)}:{nameof(ProviderConfiguration.DefaultProvider)}", selectedProviderEnum.ToString());
                            Debug.WriteLine($"DefaultProvider set to: {selectedProviderEnum}");
                        }
                        else
                        {
                            Debug.WriteLine($"WARNING: Could not parse '{selectedProviderItem.Label}' to Provider enum.");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"ERROR updating DefaultProvider setting: {ex.Message}");
                    }

                    // Invalidate controls that need updating
                    if (_ribbonUi != null)
                    {
                        _ribbonUi.InvalidateControl(nameof(ModelGroupControlIds.ProviderSplitButton));
                        _ribbonUi.InvalidateControl(nameof(ModelGroupControlIds.ProviderDisplayButton));
                        _ribbonUi.InvalidateControl(nameof(ModelGroupControlIds.ModelComboBox));
                        _ribbonUi.InvalidateControl(nameof(ModelGroupControlIds.ProviderSettingsButton));
                    }
                    else
                    {
                        Debug.WriteLine("ERROR: _ribbonUi is null in HandleProviderMenuSelection!");
                    }
                }
                else
                {
                    Debug.WriteLine("Selection hasn't changed. No update needed.");
                }
            }
            else
            {
                Debug.WriteLine($"WARNING: Invalid index from tag: {index}");
            }
        }
        else
        {
            Debug.WriteLine($"WARNING: Could not parse index from tag '{control.Tag}' for selected menu item '{control.Id}'.");
        }
        Debug.WriteLine("HandleProviderMenuSelection finished.");
    }

    public string GetSelectedModelLabel(IRibbonControl control)
    {
        var provider = GetCurrentProvider();
        string configKey = $"{provider}Configuration:{nameof(OpenAiConfiguration.DefaultModel)}"; // Using OpenAI as template name
        try
        {
            string model = GetValue(configKey);
            // Optional: Add padding like we did for the provider label if needed for consistent width
            // int maxModelLen = 30; // Estimate or calculate max model name length
            // int padding = maxModelLen - model.Length;
            // if (padding > 0) model += new string('\u00A0', padding);
            return model;
        }
        catch (KeyNotFoundException)
        {
            Debug.WriteLine($"DefaultModel key '{configKey}' not found for provider {provider}.");
            return "Select Model"; // Fallback
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error reading model config '{configKey}': {ex.Message}");
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

        SetValue($"{nameof(ProviderConfiguration)}:{nameof(ProviderConfiguration.DefaultProvider)}", provider.ToString());
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
        string currentApiKey = "";
        string currentBaseAddress = "";

        // Safely get current values
        try
        {
            currentApiKey = GetValue($"{provider}Configuration:ApiKey");
        }
        catch (KeyNotFoundException) { /* Ignore, leave empty */ }
        catch (Exception ex) { Debug.WriteLine($"Error getting ApiKey for {provider}: {ex.Message}"); }

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
        catch (Exception ex) { Debug.WriteLine($"Error getting BaseAddress for {provider}: {ex.Message}"); }

        // Instantiate and show the form
        using (var settingsForm = new ProviderSettingsForm(provider, currentApiKey, currentBaseAddress))
        {
            var result = settingsForm.ShowDialog(); // Show modally

            if (result == DialogResult.OK)
            {
                // User clicked OK, save the potentially updated values
                string newApiKey = settingsForm.ApiKey;
                string newBaseAddress = settingsForm.BaseAddress;

                try
                {
                    // Save ApiKey (always relevant unless provider has no key concept)
                    if (IsApiKeyEditable(provider)) // Helper function to check if provider uses API Key
                    {
                        SetValue($"{provider}Configuration:ApiKey", newApiKey);
                        Debug.WriteLine($"Saved ApiKey for {provider}");
                    }

                    // Save BaseAddress only if it's relevant AND editable for this provider
                    if (IsBaseAddressEditable(provider))
                    {
                        // The key name might differ slightly, adjust if needed
                        SetValue($"{provider}Configuration:BaseAddress", newBaseAddress);
                        Debug.WriteLine($"Saved BaseAddress for {provider}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"ERROR saving settings for {provider}: {ex.Message}");
                    // Consider showing an error message to the user
                    MessageBox.Show($"Error saving settings: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                // User clicked Cancel or closed the form, do nothing
                Debug.WriteLine($"Settings changes cancelled for {provider}");
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
            Provider.Ollama or Provider.OpenAiCompatible => true,
            _ => false
        };
    }
}
