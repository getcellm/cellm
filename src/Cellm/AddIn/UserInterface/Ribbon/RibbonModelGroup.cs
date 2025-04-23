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

namespace Cellm.AddIn.UserInterface.Ribbon;

public partial class RibbonMain
{
    private enum ModelGroupControlIds
    {
        ModelProviderGroup,
        ProviderModelBox,
        ProviderSplitButton,
        ProviderDisplayButton,
        ProviderSelectionMenu,

        ModelSplitButton,
        ModelDisplayButton,
        ModelSelectionMenu,
        ModelMenuItemSmall,
        ModelMenuItemBig,
        ModelMenuItemThinking,

        ProviderSettingsBox,
        ApiKeyEditBox,
        BaseAddressEditBox
    }

    private class ProviderItem
    {
        public string Id { get; set; } = string.Empty;

        public string Label { get; set; } = string.Empty;

        public string Image { get; set; } = string.Empty;

        public string SmallModel { get; set; } = string.Empty;

        public string LargeModel { get; set; } = string.Empty;

        public string ThinkingModel { get; set; } = string.Empty;
    }

    // TODO: Just finished putting in these data. Now:
    //  - Resolve errors (add property Models = [SmallModel, LargeModel, ThinkingModel] to all configurations
    //  - Use this data in NewModelGroup() (copy confgurations)
    //  - Resolve error of gallery image not updating when changing provider
    //  - Can we get _two_ rows in the ribbon instead of three???
    private readonly Dictionary<int, ProviderItem> _providerItems = new Dictionary<int, ProviderItem>
    {
        [0] = new ProviderItem { Id = "providerAnthropic", Image = "AddIn/UserInterface/Resources/anthropic.png", Label = "Anthropic" },
        [1] = new ProviderItem { Id = "providerDeepSeek", Image = "AddIn/UserInterface/Resources/deepseek.png", Label = "DeepSeek" },
        [2] = new ProviderItem { Id = "providerMistral", Image = "AddIn/UserInterface/Resources/mistral.png", Label = "Mistral" },
        [3] = new ProviderItem { Id = "providerOllama", Image = "AddIn/UserInterface/Resources/ollama.png", Label = "Ollama" },
        [4] = new ProviderItem { Id = "providerOpenAi", Image = "AddIn/UserInterface/Resources/openai.png", Label = "OpenAI" },
        [5] = new ProviderItem { Id = "providerOpenAiCompatible", Image = "AddIn/UserInterface/Resources/openai.png", Label = "OpenAI-compatible" }
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
            string menuItemId = $"providerMenuItem_{index}"; // Dynamic ID
            providerMenuItemsXml.AppendLine(
                $@"<button id=""{menuItemId}""
                         label=""{System.Security.SecurityElement.Escape(item.Label)}""
                         getImage=""{nameof(GetProviderMenuItemImage)}""
                         tag=""{index}""
                         onAction=""{nameof(HandleProviderMenuSelection)}"" />");
        }

        return $"""
        <group id="{nameof(ModelGroupControlIds.ModelProviderGroup)}" label="Model">
            <box id="{nameof(ModelGroupControlIds.ProviderModelBox)}" boxStyle="horizontal">
                <splitButton id="{nameof(ModelGroupControlIds.ProviderSplitButton)}" size="normal" showLabel="false">
                    <button id="{nameof(ModelGroupControlIds.ProviderDisplayButton)}"
                            getLabel="{nameof(GetSelectedProviderLabel)}"
                            getImage="{nameof(GetSelectedProviderImage)}"
                            showImage="true"
                            showLabel="true"
                            />
                    <menu id="{nameof(ModelGroupControlIds.ProviderSelectionMenu)}" itemSize="normal">
                        {providerMenuItemsXml}
                    </menu>
                </splitButton>

        <splitButton id="{nameof(ModelGroupControlIds.ModelSplitButton)}" size="normal" showLabel="false">
            <button id="{nameof(ModelGroupControlIds.ModelDisplayButton)}"
                    getLabel="{nameof(GetSelectedModelLabel)}"
                    showImage="false"
                    showLabel="true"
                   />
            <menu id="{nameof(ModelGroupControlIds.ModelSelectionMenu)}" itemSize="normal">
                <button id="{nameof(ModelGroupControlIds.ModelMenuItemSmall)}"
                        tag="Small"
                        getLabel="{nameof(GetModelMenuItemLabel)}"
                        getVisible="{nameof(GetModelMenuItemVisible)}"
                        onAction="{nameof(HandleModelMenuSelection)}" />
                <button id="{nameof(ModelGroupControlIds.ModelMenuItemBig)}"
                        tag="Big"
                        getLabel="{nameof(GetModelMenuItemLabel)}"
                        getVisible="{nameof(GetModelMenuItemVisible)}"
                        onAction="{nameof(HandleModelMenuSelection)}" />
                <button id="{nameof(ModelGroupControlIds.ModelMenuItemThinking)}"
                        tag="Thinking"
                        getLabel="{nameof(GetModelMenuItemLabel)}"
                        getVisible="{nameof(GetModelMenuItemVisible)}"
                        onAction="{nameof(HandleModelMenuSelection)}" />
            </menu>
        </splitButton>
            </box>
            <box id="{nameof(ModelGroupControlIds.ProviderSettingsBox)}" boxStyle="horizontal">
                 <editBox id="{nameof(ModelGroupControlIds.ApiKeyEditBox)}" label="API Key" sizeString="WWWWWWWWWWWW" getEnabled="{nameof(OnGetApiKeyEnabled)}" getText="{nameof(OnGetApiKey)}" onChange="{nameof(OnApiKeyChanged)}" />
                 <editBox id="{nameof(ModelGroupControlIds.BaseAddressEditBox)}" label="Address" sizeString="WWWWWWWWWWWW" getEnabled="{nameof(OnGetBaseAddressEnabled)}" getText="{nameof(OnGetBaseAddress)}" onChange="{nameof(OnBaseAddressChanged)}" />
            </box>
        </group>
        """;
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
            return ImageLoader.LoadEmbeddedPngResized(item.Image, 32, 32); // Adjust size as needed
        }
        Debug.WriteLine($"WARNING: Could not get image for index {_selectedProviderIndex}.");
        return null; // Or a default placeholder image
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

    public string GetModelMenuItemLabel(IRibbonControl control)
    {
        string modelType = control.Tag; // "Small", "Big", or "Thinking"
        if (string.IsNullOrEmpty(modelType)) return string.Empty; // Should not happen with static tags

        var provider = GetCurrentProvider();
        string modelName = GetModelNameForProvider(provider, modelType);

        // Return the actual model name or a placeholder if not configured
        return !string.IsNullOrEmpty(modelName) ? modelName : $"({modelType} N/A)";
    }

    // --- NEW: Callback for Static Model Menu Item Visibility ---
    public bool GetModelMenuItemVisible(IRibbonControl control)
    {
        string modelType = control.Tag;
        if (string.IsNullOrEmpty(modelType)) return false;

        var provider = GetCurrentProvider();
        string modelName = GetModelNameForProvider(provider, modelType);

        // Only show the button if a model name is configured for this type
        return !string.IsNullOrEmpty(modelName);
    }

    // --- MODIFIED: Callback for Model Menu Item Selection ---
    public void HandleModelMenuSelection(IRibbonControl control)
    {
        string selectedModelType = control.Tag; // "Small", "Big", or "Thinking"
        if (string.IsNullOrEmpty(selectedModelType))
        {
            Debug.WriteLine($"HandleModelMenuSelection: Tag is empty for control '{control.Id}'. Cannot determine model type.");
            return;
        }

        var provider = GetCurrentProvider();
        // Get the ACTUAL model name corresponding to the selected type (Small/Big/Thinking)
        string selectedModelName = GetModelNameForProvider(provider, selectedModelType);

        if (string.IsNullOrEmpty(selectedModelName))
        {
            Debug.WriteLine($"HandleModelMenuSelection: No actual model name found for type '{selectedModelType}' and provider '{provider}'. Cannot set default.");
            return; // Don't proceed if the model name is missing (button should have been hidden anyway)
        }

        string configKey = $"{provider}Configuration:{nameof(OpenAiConfiguration.DefaultModel)}"; // Config key for the *default* model

        Debug.WriteLine($"HandleModelMenuSelection called. Type: '{selectedModelType}', Resolved Model: '{selectedModelName}', Provider: {provider}");

        try
        {
            // Save the selected model name as the default for this provider
            SetValue(configKey, selectedModelName);
            Debug.WriteLine($"DefaultModel set to '{selectedModelName}' for provider {provider}.");

            // Invalidate the model splitButton's display button to update its label
            if (_ribbonUi != null)
            {
                Debug.WriteLine("Invalidating modelDisplayButton");
                _ribbonUi.InvalidateControl("modelDisplayButton");
                // Optional: Invalidate splitButton itself if needed for other reasons
                // _ribbonUi.InvalidateControl("modelSplitButton");
            }
            else
            {
                Debug.WriteLine("ERROR: _ribbonUi is null in HandleModelMenuSelection!");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"ERROR updating DefaultModel setting '{configKey}' to '{selectedModelName}': {ex.Message}");
        }
        Debug.WriteLine("HandleModelMenuSelection finished.");
    }

    // --- NEW: Helper method to get specific model name ---
    private string GetModelNameForProvider(Provider provider, string modelType)
    {
        try
        {
            return provider switch // Using C# 8.0 switch expression
            {
                Provider.Cellm => modelType switch
                {
                    "Small" => GetProviderConfiguration<CellmConfiguration>()?.SmallModel ?? "No small model",
                    "Big" => GetProviderConfiguration<CellmConfiguration>()?.BigModel ?? "No big model",
                    "Thinking" => GetProviderConfiguration<CellmConfiguration>()?.ThinkingModel ?? "No thinking model",
                    _ => "N/A"
                },
                Provider.Anthropic => modelType switch
                {
                    "Small" => GetProviderConfiguration<AnthropicConfiguration>()?.SmallModel ?? "No small model",
                    "Big" => GetProviderConfiguration<AnthropicConfiguration>()?.BigModel ?? "No big model",
                    "Thinking" => GetProviderConfiguration<AnthropicConfiguration>()?.ThinkingModel ?? "No thinking model",
                    _ => "N/A"
                },
                Provider.DeepSeek => modelType switch
                {
                    "Small" => GetProviderConfiguration<DeepSeekConfiguration>()?.SmallModel ?? "No small model",
                    "Big" => GetProviderConfiguration<DeepSeekConfiguration>()?.BigModel ?? "No big model",
                    "Thinking" => GetProviderConfiguration<DeepSeekConfiguration>()?.ThinkingModel ?? "No thinking model",
                    _ => "N/A"
                },
                Provider.Mistral => modelType switch
                {
                    "Small" => GetProviderConfiguration<MistralConfiguration>()?.SmallModel ?? "No small model",
                    "Big" => GetProviderConfiguration<MistralConfiguration>()?.BigModel ?? "No big model",
                    "Thinking" => GetProviderConfiguration<MistralConfiguration>()?.ThinkingModel ?? "No thinking model",
                    _ => "N/A"
                },
                Provider.Ollama => modelType switch
                {
                    "Small" => GetProviderConfiguration<OllamaConfiguration>()?.SmallModel ?? "No small model",
                    "Big" => GetProviderConfiguration<OllamaConfiguration>()?.BigModel ?? "No big model",
                    "Thinking" => GetProviderConfiguration<OllamaConfiguration>()?.ThinkingModel ?? "No thinking model",
                    _ => "N/A"
                },
                Provider.OpenAi => modelType switch
                {
                    "Small" => GetProviderConfiguration<OpenAiConfiguration>()?.SmallModel ?? "No small model",
                    "Big" => GetProviderConfiguration<OpenAiConfiguration>()?.BigModel ?? "No big model",
                    "Thinking" => GetProviderConfiguration<OpenAiConfiguration>()?.ThinkingModel ?? "No thinking model",
                    _ => "N/A"
                },
                Provider.OpenAiCompatible => modelType switch
                {
                    "Small" => GetProviderConfiguration<OpenAiCompatibleConfiguration>()?.SmallModel ?? "No small model",
                    "Big" => GetProviderConfiguration<OpenAiCompatibleConfiguration>()?.BigModel ?? "No big model",
                    "Thinking" => GetProviderConfiguration<OpenAiCompatibleConfiguration>()?.ThinkingModel ?? "No thinking model",
                    _ => "N/A"
                },
                _ => "N/A" // Default case for unhandled providers
            };
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error retrieving model name for {provider}/{modelType}: {ex.Message}");
            return null;
        }
    }


    // --- NEW: Callback for Menu Item Selection ---
    public void HandleProviderMenuSelection(IRibbonControl control)
    {
        // The 'tag' property of the menu button holds the index we stored.
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

                    // Update the DefaultProvider setting in config
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

                    // Invalidate the splitButton to update its appearance
                    if (_ribbonUi != null)
                    {
                        if (_ribbonUi != null)
                        {
                            Debug.WriteLine($"Invalidating {nameof(ModelGroupControlIds.ProviderSplitButton)}");
                            _ribbonUi.InvalidateControl(nameof(ModelGroupControlIds.ProviderSplitButton));
                            Debug.WriteLine($"Invalidating {nameof(ModelGroupControlIds.ProviderDisplayButton)}");
                            _ribbonUi.InvalidateControl(nameof(ModelGroupControlIds.ProviderDisplayButton));

                            Debug.WriteLine($"Invalidating {nameof(ModelGroupControlIds.ModelSplitButton)}");
                            _ribbonUi.InvalidateControl(nameof(ModelGroupControlIds.ModelSplitButton));
                            Debug.WriteLine($"Invalidating {nameof(ModelGroupControlIds.ModelDisplayButton)}");
                            _ribbonUi.InvalidateControl(nameof(ModelGroupControlIds.ModelDisplayButton));

                            Debug.WriteLine($"Invalidating {nameof(ModelGroupControlIds.ModelMenuItemSmall)}");
                            _ribbonUi.InvalidateControl(nameof(ModelGroupControlIds.ModelMenuItemSmall));
                            Debug.WriteLine($"Invalidating {nameof(ModelGroupControlIds.ModelMenuItemBig)}");
                            _ribbonUi.InvalidateControl(nameof(ModelGroupControlIds.ModelMenuItemBig));
                            Debug.WriteLine($"Invalidating {nameof(ModelGroupControlIds.ModelMenuItemThinking)}");
                            _ribbonUi.InvalidateControl(nameof(ModelGroupControlIds.ModelMenuItemThinking));

                            Debug.WriteLine($"Invalidating {nameof(ModelGroupControlIds.BaseAddressEditBox)}");
                            _ribbonUi.InvalidateControl(nameof(ModelGroupControlIds.BaseAddressEditBox));
                            Debug.WriteLine($"Invalidating {nameof(ModelGroupControlIds.ApiKeyEditBox)}");
                            _ribbonUi.InvalidateControl(nameof(ModelGroupControlIds.ApiKeyEditBox));
                        }
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
}
