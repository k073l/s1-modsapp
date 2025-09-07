using UnityEngine;
using UnityEngine.UI;
using S1API.UI;
using S1API.Input;
using MelonLoader;
using ModsApp.Helpers;
using System;
using MelonLoader.Utils;
using ModsApp.Managers;
using ModsApp.UI.Input.FieldFactories;
using S1API.Internal.Abstraction;

namespace ModsApp.UI.Panels;

public class JsonConfigUI
{
    private readonly UITheme _theme;
    private readonly MelonLogger.Instance _logger;
    private readonly JsonConfigManager _configManager;

    private InputField _jsonFileInput;
    private InputField _jsonEditor;
    private Button _saveButton;
    private Button _revertButton;
    private Button _formatButton;

    private string _currentJsonContent = "";
    private string _lastValidJsonContent = "";
    private string _originalJsonContent = "";
    private string _currentFilename = "";

    private System.Action onConfigSaved;

    public JsonConfigUI(UITheme theme, MelonLogger.Instance logger, JsonConfigManager configManager)
    {
        _theme = theme;
        _logger = logger;
        _configManager = configManager;
    }

    public void ResetState()
    {
        _currentJsonContent = "";
        _lastValidJsonContent = "";
        _originalJsonContent = "";
        _currentFilename = "";

        _jsonFileInput = null;
        _jsonEditor = null;
        _saveButton = null;
        _revertButton = null;
        _formatButton = null;
    }

    public void CreateJsonConfigSection(MelonMod mod, GameObject card, System.Action onLayoutRefresh,
        System.Action onConfigSaved)
    {
        var header = UIFactory.Text("JsonConfigHeader", "No MelonPreferences", card.transform, 16,
            TextAnchor.UpperLeft, FontStyle.Bold);
        header.color = _theme.TextPrimary;

        var description = UIFactory.Text("JsonConfigDesc",
            "No MelonPreferences were found; though they might exist. " +
            "If the mod uses JSON files, you can specify the path manually. " +
            "Long files may not display correctly.",
            card.transform, 12);
        description.color = _theme.TextSecondary;

        CreateJsonFileInputSection(mod, card, onLayoutRefresh);
        this.onConfigSaved = onConfigSaved;
    }

    private void CreateJsonFileInputSection(MelonMod mod, GameObject parent, System.Action onLayoutRefresh)
    {
        var fileInputContainer = UIFactory.Panel("FileInputContainer", parent.transform, Color.clear);

        var vlg = fileInputContainer.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 4;
        vlg.childControlWidth = true;
        vlg.childForceExpandHeight = false;

        var pathLabel = UIFactory.Text("PathLabel", "JSON file path (relative to UserData/):",
            fileInputContainer.transform, 12);
        pathLabel.color = _theme.TextSecondary;

        var inputRow = UIFactory.Panel("InputRow", fileInputContainer.transform, Color.clear);
        var hLayout = inputRow.AddComponent<HorizontalLayoutGroup>();
        hLayout.spacing = 8;
        hLayout.childForceExpandWidth = false;
        hLayout.childControlWidth = true;

        string savedFilename = _configManager.GetSavedFilename(mod.Info.Name);

        if (string.IsNullOrEmpty(savedFilename))
        {
            // Try to guess
            var guessed = _configManager.FindFileForMod(mod.Info.Name, MelonEnvironment.UserDataDirectory);
            if (!string.IsNullOrEmpty(guessed))
            {
                savedFilename = guessed;
                _logger.Msg($"[JSON] Guessed config file for {mod.Info.Name}: {guessed}");
            }
            else
            {
                guessed = _configManager.FindFileForMod(mod.Info.Name,
                    Path.Combine(MelonEnvironment.UserDataDirectory, "..", "Mods"));
                if (!string.IsNullOrEmpty(guessed))
                {
                    savedFilename = guessed;
                    _logger.Msg($"[JSON] Guessed config file for {mod.Info.Name}: {guessed}");
                }
            }

            if (!string.IsNullOrEmpty(guessed))
            {
                var fullPath = Path.Combine(MelonEnvironment.UserDataDirectory, guessed);
                if (File.Exists(fullPath) && new FileInfo(fullPath).Length > 16000)
                {
                    savedFilename = ""; // too long, don't auto-load
                    _logger.Msg($"[JSON] Skipping auto-load of guessed large config file: {fullPath} (>16kB)");
                }
            }
        }

        _jsonFileInput = InputFieldFactory.CreateInputField(
            inputRow, "JsonFileInput", savedFilename, InputField.ContentType.Standard, 200);

        var (loadBtnObj, loadBtn, _) = UIFactory.RoundedButtonWithLabel(
            "LoadJsonButton", "Load", inputRow.transform, _theme.AccentPrimary, 60, 25, 12, Color.white);

        EventHelper.AddListener(() =>
        {
            LoadJsonFile(mod);

            if (!string.IsNullOrEmpty(_currentJsonContent))
            {
                CreateJsonEditorSection(mod, parent);
            }

            onLayoutRefresh?.Invoke();
        }, loadBtn.onClick);

        if (!string.IsNullOrEmpty(savedFilename))
        {
            // Auto-load if we have a saved filename
            LoadJsonFile(mod);

            if (!string.IsNullOrEmpty(_currentJsonContent))
            {
                CreateJsonEditorSection(mod, parent);
            }

            onLayoutRefresh?.Invoke();
        }

        var userDataPath = MelonEnvironment.UserDataDirectory;
        var pathInfo = UIFactory.Text("UserDataPath", $"UserData path: {userDataPath}",
            fileInputContainer.transform, 10);
        pathInfo.color = _theme.TextSecondary * 0.7f;
    }

    private void CreateJsonEditorSection(MelonMod mod, GameObject parent)
    {
        var editorContainer = UIFactory.Panel("JsonEditorContainer", parent.transform, Color.clear);

        var vlg = editorContainer.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 4;
        vlg.childControlWidth = true;
        vlg.childForceExpandHeight = false;

        var editorLabel = UIFactory.Text("JsonEditorLabel", "JSON Content:",
            editorContainer.transform, 12, TextAnchor.UpperLeft, FontStyle.Bold);
        editorLabel.color = _theme.TextPrimary;

        _jsonEditor = InputFieldFactory.CreateInputField(
            editorContainer, "JsonEditor", "",
            InputField.ContentType.Standard, 400);

        _jsonEditor.lineType = InputField.LineType.MultiLineNewline;
        _jsonEditor.contentType = InputField.ContentType.Standard;

        var editorRect = _jsonEditor.GetComponent<RectTransform>();
        editorRect.sizeDelta = new Vector2(editorRect.sizeDelta.x, 400);
        var layoutElement = _jsonEditor.GetComponent<LayoutElement>();
        layoutElement.minHeight = 200;
        layoutElement.preferredHeight = 200;

        var textComponent = _jsonEditor.textComponent;
        textComponent.fontSize = 12;
        // textComponent.font = GetBestMonospaceFont();
        // textComponent.color = Color.black;
        // textComponent.material = textComponent.font.material;

        textComponent.alignment = TextAnchor.UpperLeft;
        textComponent.horizontalOverflow = HorizontalWrapMode.Wrap;
        textComponent.verticalOverflow = VerticalWrapMode.Overflow;
        textComponent.supportRichText = false;

        var placeholder = _jsonEditor.placeholder as Text;
        if (placeholder != null)
        {
            placeholder.alignment = TextAnchor.UpperLeft;
            placeholder.horizontalOverflow = HorizontalWrapMode.Wrap;
            placeholder.verticalOverflow = VerticalWrapMode.Overflow;
        }

        _jsonEditor.text = _currentJsonContent;

        textComponent.SetAllDirty();
        if (placeholder != null) placeholder.SetAllDirty();
        _jsonEditor.ForceLabelUpdate();
        Canvas.ForceUpdateCanvases();

        EventHelper.AddListener<string>((str) => OnJsonContentChanged(str), _jsonEditor.onValueChanged);

        CreateJsonActionButtons(mod, editorContainer);
    }

    private void CreateJsonActionButtons(MelonMod mod, GameObject parent)
    {
        var buttonContainer = UIFactory.Panel("JsonButtonContainer", parent.transform, Color.clear);

        var buttonLayout = buttonContainer.GetOrAddComponent<LayoutElement>();
        buttonLayout.preferredHeight = 35;
        buttonLayout.flexibleHeight = 0;

        var hLayout = buttonContainer.AddComponent<HorizontalLayoutGroup>();
        hLayout.spacing = 8;
        hLayout.childAlignment = TextAnchor.MiddleCenter;
        hLayout.childForceExpandWidth = false;
        hLayout.childForceExpandHeight = false;
        hLayout.childControlWidth = true;
        hLayout.childControlHeight = true;

        var (saveObj, saveButton, _) = UIFactory.RoundedButtonWithLabel(
            "SaveJsonButton", "Save JSON", buttonContainer.transform,
            _theme.AccentPrimary, 100, 30, 14, Color.white);

        _saveButton = saveButton;
        EventHelper.AddListener(() => SaveJsonFile(), saveButton.onClick);

        var (revertObj, revertButton, _) = UIFactory.RoundedButtonWithLabel(
            "RevertJsonButton", "Revert", buttonContainer.transform,
            _theme.WarningColor, 80, 30, 14, Color.white);

        _revertButton = revertButton;
        EventHelper.AddListener(() => RevertJsonChanges(), revertButton.onClick);

        var (formatObj, formatButton, _) = UIFactory.RoundedButtonWithLabel(
            "FormatJsonButton", "Format", buttonContainer.transform,
            _theme.TextSecondary, 80, 30, 14, Color.white);

        _formatButton = formatButton;
        EventHelper.AddListener(() => FormatJsonContent(), formatButton.onClick);

        UpdateJsonButtonStates();
    }

    private void LoadJsonFile(MelonMod mod)
    {
        var filename = _jsonFileInput.text.Trim();
        var result = _configManager.LoadJsonFile(filename);

        if (result.Success)
        {
            _currentJsonContent = CleanJsonContent(result.Content);
            _lastValidJsonContent = _currentJsonContent;
            _originalJsonContent = CleanJsonContent(result.OriginalContent);
            _currentFilename = filename;

            _configManager.SaveFilename(mod.Info.Name, filename);

            FormatJsonContent();
        }
        else
        {
            _logger.Warning(result.ErrorMessage);
        }
    }

    private string CleanJsonContent(string content)
    {
        // Remove various invisible characters that can break JSON
        return content
            .Replace("\r\n", "\n")
            .Replace("\r", "\n")
            .Trim()
            .Replace("\u00A0", " ")
            .Replace("\t", "  ");
    }

    private void SaveJsonFile()
    {
        if (string.IsNullOrEmpty(_currentFilename))
        {
            _logger.Error("No JSON file loaded");
            return;
        }

        var result = _configManager.SaveJsonFile(_currentFilename, _currentJsonContent);

        if (result.Success)
        {
            _originalJsonContent = _currentJsonContent;
            _lastValidJsonContent = _currentJsonContent;

            // JSON config changed, notify details panel
            onConfigSaved?.Invoke();

            UpdateJsonButtonStates();
        }
        else
        {
            _logger.Error(result.ErrorMessage);
        }
    }

    private void RevertJsonChanges()
    {
        if (_jsonEditor != null)
        {
            _jsonEditor.text = _originalJsonContent;
            _currentJsonContent = _originalJsonContent;
            _lastValidJsonContent = _originalJsonContent;
            UpdateJsonButtonStates();
            _logger.Msg("Reverted JSON changes");
        }
    }

    private void FormatJsonContent()
    {
        var formatted = _configManager.FormatJson(_currentJsonContent);
        var cleanedFormatted = CleanJsonContent(formatted);

        if (_jsonEditor != null && cleanedFormatted != _currentJsonContent)
        {
            _jsonEditor.text = cleanedFormatted;
            _currentJsonContent = cleanedFormatted;
            _lastValidJsonContent = cleanedFormatted;
            _logger.Msg("JSON formatted successfully");
        }
        else if (cleanedFormatted == _currentJsonContent)
        {
            _logger.Warning("JSON is already formatted or invalid");
        }
    }

    private void OnJsonContentChanged(string newContent)
    {
        _currentJsonContent = newContent;

        if (_configManager.IsValidJson(newContent))
        {
            _lastValidJsonContent = newContent;
        }

        UpdateJsonButtonStates();
    }

    private void UpdateJsonButtonStates()
    {
        if (_saveButton == null || _revertButton == null) return;

        var hasChanges = _currentJsonContent != _originalJsonContent;
        var isValidJson = _configManager.IsValidJson(_currentJsonContent);

        _saveButton.interactable = hasChanges && isValidJson;
        var saveColors = _saveButton.colors;
        saveColors.normalColor = (hasChanges && isValidJson)
            ? _theme.AccentPrimary
            : new Color(_theme.AccentPrimary.r, _theme.AccentPrimary.g, _theme.AccentPrimary.b, 0.5f);
        saveColors.disabledColor =
            new Color(_theme.AccentPrimary.r, _theme.AccentPrimary.g, _theme.AccentPrimary.b, 0.3f);
        _saveButton.colors = saveColors;

        _revertButton.interactable = hasChanges;
        var revertColors = _revertButton.colors;
        revertColors.normalColor = hasChanges
            ? _theme.WarningColor
            : new Color(_theme.WarningColor.r, _theme.WarningColor.g, _theme.WarningColor.b, 0.5f);
        revertColors.disabledColor =
            new Color(_theme.WarningColor.r, _theme.WarningColor.g, _theme.WarningColor.b, 0.3f);
        _revertButton.colors = revertColors;

        _formatButton.interactable = !string.IsNullOrEmpty(_currentJsonContent);
        var formatColors = _formatButton.colors;
        formatColors.normalColor = isValidJson
            ? _theme.TextSecondary
            : new Color(_theme.WarningColor.r, _theme.WarningColor.g, _theme.WarningColor.b, 0.8f);
        _formatButton.colors = formatColors;
    }

    private static Font GetBestMonospaceFont()
    {
        string[] monospaceNames = { "Consolas", "Courier New", "Courier", "Monaco", "Menlo" };

        foreach (string fontName in monospaceNames)
        {
            var font = Resources.GetBuiltinResource<Font>(fontName.Replace(" ", "") + ".ttf");
            if (font != null) return font;
        }

        // string[] systemFonts = Font.GetOSInstalledFontNames();
        // foreach (string systemFont in systemFonts)
        // {
        //     if (systemFont.Contains("Consolas") || systemFont.Contains("Courier") ||
        //         systemFont.Contains("Mono") || systemFont.Contains("Code"))
        //     {
        //         return Font.CreateDynamicFontFromOSFont(systemFont, 12);
        //     }
        // }
        // Nope, breaks on IL2CPP

        var fallbackFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
        if (fallbackFont == null)
            MelonLogger.Msg("[UI][WARN] Arial font missing!");
        return fallbackFont;
    }
}