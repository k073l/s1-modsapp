using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using S1API.UI;
using S1API.Input;
using MelonLoader;
using ModsApp.Helpers;
using System;
using System.Reflection;
using System.Text.RegularExpressions;
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

    private object _tmpInputField;
    private object _tmpTextComponent;
    private static object _tmpFontAsset;

    private InputField _legacyEditor;

    private Button _saveButton;
    private Button _revertButton;
    private Button _formatButton;
    private InputField _jsonFileInput;
    private GameObject _editorSectionRoot;
    private Font _consolaFont;
    
    private EventTrigger _overlayScrollTrigger;
    private Action<BaseEventData> _scrollCallback;

    // state tracking
    private string _currentJsonContent = "";
    private string _originalJsonContent = "";
    private string _currentFilename = "";
    private Action _onConfigSaved;
    private int _lastRawCaret;

    private static readonly Regex _tagStripper =
        new Regex("<[^>]*>", RegexOptions.Compiled);


    public JsonConfigUI(UITheme theme, MelonLogger.Instance logger, JsonConfigManager configManager)
    {
        _theme = theme;
        _logger = logger;
        _configManager = configManager;
        CheckTMP();
    }

    public void ResetState()
    {
        _currentJsonContent = "";
        _originalJsonContent = "";
        _currentFilename = "";
        _tmpInputField = null;
        _tmpTextComponent = null;
        _legacyEditor = null;
        _jsonFileInput = null;
        _saveButton = null;
        _revertButton = null;
        _formatButton = null;
        _editorSectionRoot = null;
    }


    public void CreateJsonConfigSection(MelonMod mod, GameObject card,
        Action onLayoutRefresh, Action onConfigSaved)
    {
        var header = UIFactory.Text("JsonConfigHeader", "No MelonPreferences", card.transform,
            _theme.SizeMedium, TextAnchor.UpperLeft, FontStyle.Bold);
        header.color = _theme.TextPrimary;

        var description = UIFactory.Text("JsonConfigDesc",
            "No MelonPreferences were found; though they might exist. " +
            "If the mod uses JSON files, you can specify the path manually. " +
            "Long files may not display correctly.",
            card.transform, _theme.SizeSmall);
        description.color = _theme.TextSecondary;

        _onConfigSaved = onConfigSaved;

        CreateFileInputSection(mod, card, onLayoutRefresh);
    }


    private void CreateFileInputSection(MelonMod mod, GameObject parent, Action onLayoutRefresh)
    {
        var container = UIFactory.Panel("FileInputContainer", parent.transform, Color.clear);
        var vlg = container.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 4;
        vlg.childControlWidth = true;
        vlg.childForceExpandHeight = false;

        var pathLabel = UIFactory.Text("PathLabel", "JSON file path (relative to UserData/):",
            container.transform, _theme.SizeSmall);
        pathLabel.color = _theme.TextSecondary;

        var inputRow = UIFactory.Panel("InputRow", container.transform, Color.clear);
        var hLayout = inputRow.AddComponent<HorizontalLayoutGroup>();
        hLayout.spacing = 8;
        hLayout.childForceExpandWidth = false;
        hLayout.childControlWidth = true;

        var savedFilename = _configManager.GetSavedFilename(mod.Info.Name);
        if (string.IsNullOrEmpty(savedFilename))
            savedFilename = GuessFilename(mod.Info.Name);

        _jsonFileInput = InputFieldFactory.CreateInputField(
            inputRow, "JsonFileInput", savedFilename, InputField.ContentType.Standard, 200);

        var (_, loadBtn, _) = UIFactory.RoundedButtonWithLabel(
            "LoadJsonButton", "Load", inputRow.transform,
            _theme.AccentPrimary, 60, 25, _theme.SizeSmall, _theme.TextPrimary);

        EventHelper.AddListener(() =>
        {
            LoadJsonFile(mod);
            if (_editorSectionRoot != null)
            {
                UnityEngine.Object.Destroy(_editorSectionRoot);
                _editorSectionRoot = null;
                _tmpInputField = null;
                _legacyEditor = null;
                _saveButton = null;
                _revertButton = null;
                _formatButton = null;
            }

            if (!string.IsNullOrEmpty(_currentJsonContent))
                CreateEditorSection(mod, parent);
            onLayoutRefresh?.Invoke();
        }, loadBtn.onClick);

        if (!string.IsNullOrEmpty(savedFilename))
        {
            LoadJsonFile(mod);
            if (!string.IsNullOrEmpty(_currentJsonContent))
                CreateEditorSection(mod, parent);
            onLayoutRefresh?.Invoke();
        }

        var pathInfo = UIFactory.Text("UserDataPath",
            $"UserData path: {MelonEnvironment.UserDataDirectory}",
            container.transform, _theme.SizeTiny);
        pathInfo.color = _theme.TextSecondary * 0.7f;
    }

    private string GuessFilename(string modName)
    {
        var guessed = _configManager.FindFileForMod(modName, MelonEnvironment.UserDataDirectory);
        if (string.IsNullOrEmpty(guessed))
            guessed = _configManager.FindFileForMod(modName,
                Path.Combine(MelonEnvironment.UserDataDirectory, "..", "Mods"));

        if (string.IsNullOrEmpty(guessed)) return "";

        var fullPath = Path.Combine(MelonEnvironment.UserDataDirectory, guessed);
        if (File.Exists(fullPath) && new FileInfo(fullPath).Length > 16000)
        {
            _logger.Warning($"[JSON] Skipping large guessed config: {fullPath} (>16kB)");
            return "";
        }

        return guessed;
    }


    private void CreateEditorSection(MelonMod mod, GameObject parent)
    {
        var container = UIFactory.Panel("JsonEditorContainer", parent.transform, Color.clear);
        _editorSectionRoot = container;

        var vlg = container.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 4;
        vlg.childControlWidth = true;
        vlg.childForceExpandHeight = false;

        var label = UIFactory.Text("JsonEditorLabel", "JSON Content:",
            container.transform, _theme.SizeSmall, TextAnchor.UpperLeft, FontStyle.Bold);
        label.color = _theme.TextPrimary;

        if (ReflectionHelper.TMPAvailable)
            CreateTMPEditor(container);
        else
            CreateLegacyEditor(container);

        CreateActionButtons(container);
    }

    private void CreateTMPEditor(GameObject parent)
    {
        var wrapper = UIFactory.Panel("JsonEditorWrapper", parent.transform, _theme.BgInput);
        wrapper.GetComponent<Image>()?.MakeRounded(4, 16);

        var wrapperLayout = wrapper.GetOrAddComponent<LayoutElement>();
        wrapperLayout.minHeight = 200;
        wrapperLayout.preferredHeight = 200;
        wrapperLayout.flexibleWidth = 1;

        var textArea = new GameObject("Text Area");
        textArea.transform.SetParent(wrapper.transform, false);
        textArea.AddComponent<RectMask2D>();
        var textAreaRT = textArea.GetComponent<RectTransform>();
        textAreaRT.anchorMin = Vector2.zero;
        textAreaRT.anchorMax = Vector2.one;
        textAreaRT.offsetMin = new Vector2(8, 6);
        textAreaRT.offsetMax = new Vector2(-8, -6);

        var placeholderGO = new GameObject("Placeholder");
        placeholderGO.transform.SetParent(textArea.transform, false);
        FullStretch(placeholderGO);
        var placeholder = AddTMPText(placeholderGO, "Enter JSON...",
            _theme.InputSecondary, _theme.SizeStandard, richText: false);

        var textGO = new GameObject("Text");
        textGO.transform.SetParent(textArea.transform, false);
        FullStretch(textGO);
        _tmpTextComponent = AddTMPText(textGO, "",
            _theme.InputPrimary, _theme.SizeStandard, richText: true);

        if (_tmpTextComponent == null || placeholder == null)
        {
            _logger.Warning("[JsonConfigUI] TMP text AddComponent failed — falling back.");
            ReflectionHelper.TMPAvailable = false;
            UnityEngine.Object.Destroy(wrapper);
            CreateLegacyEditor(parent);
            return;
        }

        wrapper.SetActive(false);

        object inputField;
        try
        {
            inputField = ReflectionHelper.AddComponent(wrapper, ReflectionHelper.TMPInputFieldType);
        }
        catch (Exception ex)
        {
            _logger.Warning($"[JsonConfigUI] TMP_InputField AddComponent threw: {ex.Message}");
            ReflectionHelper.TMPAvailable = false;
            wrapper.SetActive(true);
            UnityEngine.Object.Destroy(wrapper);
            CreateLegacyEditor(parent);
            return;
        }

        if (inputField == null)
        {
            _logger.Warning("[JsonConfigUI] TMP_InputField AddComponent returned null.");
            ReflectionHelper.TMPAvailable = false;
            wrapper.SetActive(true);
            UnityEngine.Object.Destroy(wrapper);
            CreateLegacyEditor(parent);
            return;
        }

        _tmpInputField = inputField;

        ReflectionHelper.SetValue(ReflectionHelper.MIfTextViewport, _tmpInputField, textAreaRT);
        ReflectionHelper.SetValue(ReflectionHelper.MIfTextComponent, _tmpInputField, _tmpTextComponent);
        ReflectionHelper.SetValue(ReflectionHelper.MIfPlaceholder, _tmpInputField, placeholder);
        ReflectionHelper.SetValue(ReflectionHelper.MIfContentType, _tmpInputField,
            ReflectionHelper.EnumValue(ReflectionHelper.MIfContentType, 0)); // Standard
        ReflectionHelper.SetValue(ReflectionHelper.MIfLineType, _tmpInputField,
            ReflectionHelper.EnumValue(ReflectionHelper.MIfLineType, 2)); // MultiLineNewline
        ReflectionHelper.SetValue(ReflectionHelper.MIfRichText, _tmpInputField, true);
        ReflectionHelper.SetValue(ReflectionHelper.MIfCustomCaret, _tmpInputField, true);
        ReflectionHelper.SetValue(ReflectionHelper.MIfCaretColor, _tmpInputField, _theme.InputPrimary);
        ReflectionHelper.SetValue(ReflectionHelper.MIfCaretBlinkRate, _tmpInputField, 0.6f);

        if (ReflectionHelper.MIfOnFocusSelectAll != null)
            ReflectionHelper.SetValue(ReflectionHelper.MIfOnFocusSelectAll, _tmpInputField, false);

        List<bool> eventWiringResults = [];
        var onValueChangedObj = ReflectionHelper.GetValue(ReflectionHelper.MIfOnValueChanged, _tmpInputField);
        eventWiringResults.Add(ReflectionHelper.AddStringListener(onValueChangedObj, OnEditorValueChanged));

        var onEndEditObj = ReflectionHelper.GetValue(ReflectionHelper.MIfOnEndEdit, _tmpInputField);
        eventWiringResults.Add(ReflectionHelper.AddStringListener(onEndEditObj, OnEditorEndEdit));

        var onSelectMember = ReflectionHelper.GetMember(ReflectionHelper.TMPInputFieldType, "onSelect");
        if (onSelectMember != null)
        {
            var onSelectObj = ReflectionHelper.GetValue(onSelectMember, _tmpInputField);
            eventWiringResults.Add(ReflectionHelper.AddStringListener(onSelectObj, _ => Controls.IsTyping = true));
        }

        var onDeselectMember = ReflectionHelper.GetMember(ReflectionHelper.TMPInputFieldType, "onDeselect");
        if (onDeselectMember != null)
        {
            var onDeselectObj = ReflectionHelper.GetValue(onDeselectMember, _tmpInputField);
            eventWiringResults.Add(ReflectionHelper.AddStringListener(onDeselectObj, _ => Controls.IsTyping = false));
        }

        if (!eventWiringResults.All(b => b))
        {
            _logger.Warning("[JsonConfigUI] Some TMP events couldn't be wired correctly");
            ReflectionHelper.TMPAvailable = false;
            wrapper.SetActive(true);
            UnityEngine.Object.Destroy(wrapper);
            CreateLegacyEditor(parent);
            return;
        }

        AddScrollPassthrough(wrapper);

        wrapper.SetActive(true);
        ApplyHighlight(_currentJsonContent, 0);
    }


    private void AddScrollPassthrough(GameObject wrapper)
    {
        var parentScrollRect = wrapper.GetComponentInParent<ScrollRect>();
        if (parentScrollRect == null) return;

        var trigger = wrapper.GetOrAddComponent<EventTrigger>();

        if (_scrollCallback != null)
            EventHelper.RemoveEventTrigger(trigger, EventTriggerType.Scroll, _scrollCallback);

        _scrollCallback = (data) =>
        {
            var pointerData = data as PointerEventData ?? new PointerEventData(EventSystem.current)
                { scrollDelta = UnityEngine.Input.mouseScrollDelta };
            parentScrollRect.OnScroll(pointerData);
        };
        EventHelper.AddEventTrigger(trigger, EventTriggerType.Scroll, _scrollCallback);
    }


    private void OnEditorValueChanged(string value)
    {
        var newRaw = StripTags(value);
        if (newRaw != _currentJsonContent)
            _lastRawCaret = FindRawCaretAfterEdit(_currentJsonContent, newRaw);
        _currentJsonContent = newRaw;
        ApplyHighlight(newRaw, _lastRawCaret);
        UpdateButtonStates();
        Controls.IsTyping = true;
    }

    private void OnEditorEndEdit(string value)
    {
        var raw = StripTags(value);
        // game calls DeactivateInputField on Enter externally, firing onEndEdit.
        // insert the newline manually, and re-activate the field.
        if (UnityEngine.Input.GetKey(KeyCode.Return) || UnityEngine.Input.GetKey(KeyCode.KeypadEnter))
        {
            var caret = 0;
            if (ReflectionHelper.MIfStringPosition != null)
            {
                var hlPos = (int)ReflectionHelper.GetValue(ReflectionHelper.MIfStringPosition, _tmpInputField);
                caret = HighlightedIndexToRawIndex(raw, hlPos);
            }

            raw = raw.Insert(caret, "\n");
            _currentJsonContent = raw;
            ApplyHighlight(raw, caret);
            UpdateButtonStates();
            if (ReflectionHelper.MiActivateInputField != null)
                ReflectionHelper.MiActivateInputField.Invoke(_tmpInputField, null);
            return;
        }

        _currentJsonContent = raw;
        ApplyHighlight(raw, FindRawCaretAfterEdit(_currentJsonContent, raw));
        Controls.IsTyping = false;
    }


    private static int HighlightedIndexToRawIndex(string raw, int hlPos)
    {
        // walk the highlighted version of raw, skipping tag spans,
        // and count raw chars until we've consumed hlPos highlighted chars
        var highlighted = JsonSyntaxHighlighter.Highlight(raw);
        var rawIdx = 0;
        var hlIdx = 0;
        while (hlIdx < hlPos && hlIdx < highlighted.Length)
        {
            if (highlighted[hlIdx] == '<')
            {
                // skip to end of tag
                var end = highlighted.IndexOf('>', hlIdx);
                hlIdx = end >= 0 ? end + 1 : highlighted.Length;
            }
            else
            {
                hlIdx++;
                if (rawIdx < raw.Length) rawIdx++;
            }
        }

        return rawIdx;
    }


    private static int FindRawCaretAfterEdit(string oldRaw, string newRaw)
    {
        if (oldRaw == null || newRaw == null) return -1;
        var oldLen = oldRaw?.Length ?? 0;
        var newLen = newRaw?.Length ?? 0;

        var firstDiff = 0;
        var minLen = Math.Min(oldLen, newLen);
        while (firstDiff < minLen && oldRaw[firstDiff] == newRaw[firstDiff])
            firstDiff++;

        var oldTail = oldLen - 1;
        var newTail = newLen - 1;
        while (oldTail >= firstDiff && newTail >= firstDiff
                                    && oldRaw[oldTail] == newRaw[newTail])
        {
            oldTail--;
            newTail--;
        }

        var insertedCount = Math.Max(0, newTail - firstDiff + 1);
        return Math.Max(0, Math.Min(firstDiff + insertedCount, newLen));
    }


    private void ApplyHighlight(string raw, int rawCaret = 0)
    {
        if (_tmpInputField == null) return;

        SetTMPFieldText(JsonSyntaxHighlighter.Highlight(raw));

        if (ReflectionHelper.MIfStringPosition != null)
            ReflectionHelper.SetValue(ReflectionHelper.MIfStringPosition, _tmpInputField,
                JsonSyntaxHighlighter.RawIndexToHighlightedIndex(raw, rawCaret));
    }

    private void SetTMPFieldText(string text)
    {
        if (_tmpInputField == null) return;

        if (ReflectionHelper.MiSetTextWithoutNotify != null)
        {
            try
            {
                ReflectionHelper.MiSetTextWithoutNotify.Invoke(_tmpInputField, [text]);
                return;
            }
            catch
            {
                // ignored
            }
        }

        ReflectionHelper.SetValue(ReflectionHelper.MIfText, _tmpInputField, text);
    }

    private static string StripTags(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        return _tagStripper.Replace(s, "")
            .Replace("\u003C", "<")
            .Replace("\u003E", ">");
    }


    private object AddTMPText(GameObject go, string text, Color color, float fontSize, bool richText)
    {
        object tmp;
        try
        {
            tmp = ReflectionHelper.AddComponent(go, ReflectionHelper.TMPTextType);
        }
        catch (Exception ex)
        {
            _logger.Warning($"[JsonConfigUI] AddComponent TMPro on '{go.name}': {ex.Message}");
            return null;
        }

        if (tmp == null) return null;

        if (_tmpFontAsset == null)
            _tmpFontAsset = CreateTMPFontAsset();

        if (_tmpFontAsset != null)
            ReflectionHelper.SetValue(ReflectionHelper.GetMember(ReflectionHelper.TMPTextBaseType, "font"), tmp,
                _tmpFontAsset);

        ReflectionHelper.SetValue(ReflectionHelper.MTText, tmp, text);
        ReflectionHelper.SetValue(ReflectionHelper.MTColor, tmp, color);
        ReflectionHelper.SetValue(ReflectionHelper.MTFontSize, tmp, fontSize);
        ReflectionHelper.SetValue(ReflectionHelper.MTRichText, tmp, richText);
        ReflectionHelper.SetValue(ReflectionHelper.MTWordWrap, tmp, true);
        ReflectionHelper.SetValue(ReflectionHelper.MTAutoSize, tmp, false);
        ReflectionHelper.SetValue(ReflectionHelper.MTOverflow, tmp,
            ReflectionHelper.EnumValue(ReflectionHelper.MTOverflow, 0));
        ReflectionHelper.SetValue(ReflectionHelper.MTAlignment, tmp,
            ReflectionHelper.EnumValue(ReflectionHelper.MTAlignment, 257)); // top left

        return tmp;
    }


    private void CreateLegacyEditor(GameObject parent)
    {
        _legacyEditor = InputFieldFactory.CreateInputField(
            parent, "JsonEditor", "", InputField.ContentType.Standard, 400);

        _legacyEditor.lineType = InputField.LineType.MultiLineNewline;
        _legacyEditor.contentType = InputField.ContentType.Standard;

        var layout = _legacyEditor.GetComponent<LayoutElement>();
        layout.minHeight = 200;
        layout.preferredHeight = 200;

        var tc = _legacyEditor.textComponent;
        tc.font = _consolaFont ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
        tc.fontSize = _theme.SizeStandard;
        tc.alignment = TextAnchor.UpperLeft;
        tc.horizontalOverflow = HorizontalWrapMode.Wrap;
        tc.verticalOverflow = VerticalWrapMode.Overflow;
        tc.supportRichText = false;

        _legacyEditor.text = _currentJsonContent;

        AddScrollPassthrough(_legacyEditor.gameObject);

        EventHelper.AddListener<string>(raw =>
        {
            _currentJsonContent = raw;
            UpdateButtonStates();
        }, _legacyEditor.onValueChanged);
    }


    private void CreateActionButtons(GameObject parent)
    {
        var container = UIFactory.Panel("JsonButtonContainer", parent.transform, Color.clear);
        var layout = container.GetOrAddComponent<LayoutElement>();
        layout.preferredHeight = 35;
        layout.flexibleHeight = 0;

        var hLayout = container.AddComponent<HorizontalLayoutGroup>();
        hLayout.spacing = 8;
        hLayout.childAlignment = TextAnchor.MiddleCenter;
        hLayout.childForceExpandWidth = false;
        hLayout.childForceExpandHeight = false;
        hLayout.childControlWidth = true;
        hLayout.childControlHeight = true;

        var (_, save, _) = UIFactory.RoundedButtonWithLabel(
            "SaveJsonButton", "Save JSON", container.transform,
            _theme.AccentPrimary, 100, 30, _theme.SizeStandard, _theme.TextPrimary);
        _saveButton = save;
        EventHelper.AddListener(() => SaveJsonFile(), save.onClick);

        var (_, revert, _) = UIFactory.RoundedButtonWithLabel(
            "RevertJsonButton", "Revert", container.transform,
            _theme.WarningColor, 80, 30, _theme.SizeStandard, _theme.TextPrimary);
        _revertButton = revert;
        EventHelper.AddListener(() => RevertJsonChanges(), revert.onClick);

        var (_, format, _) = UIFactory.RoundedButtonWithLabel(
            "FormatJsonButton", "Format", container.transform,
            _theme.TextSecondary, 80, 30, _theme.SizeStandard, _theme.TextPrimary);
        _formatButton = format;
        EventHelper.AddListener(() => FormatJsonContent(), format.onClick);

        UpdateButtonStates();
    }


    private void LoadJsonFile(MelonMod mod)
    {
        var result = _configManager.LoadJsonFile(_jsonFileInput.text.Trim());
        if (!result.Success)
        {
            _logger.Warning(result.ErrorMessage);
            return;
        }

        _currentJsonContent = Clean(result.Content);
        _originalJsonContent = Clean(result.OriginalContent);
        _currentFilename = _jsonFileInput.text.Trim();
        _configManager.SaveFilename(mod.Info.Name, _currentFilename);

        FormatJsonContent();
    }

    private void SaveJsonFile()
    {
        if (string.IsNullOrEmpty(_currentFilename))
        {
            _logger.Error("[JSON] No file loaded.");
            return;
        }

        var result = _configManager.SaveJsonFile(_currentFilename, _currentJsonContent);
        if (!result.Success)
        {
            _logger.Error(result.ErrorMessage);
            return;
        }

        _originalJsonContent = _currentJsonContent;
        _onConfigSaved?.Invoke();
        UpdateButtonStates();
    }

    private void RevertJsonChanges()
    {
        _currentJsonContent = _originalJsonContent;
        SetEditorContent(_originalJsonContent);
        UpdateButtonStates();
    }

    private void FormatJsonContent()
    {
        var formatted = Clean(_configManager.FormatJson(_currentJsonContent));
        if (formatted == _currentJsonContent) return;
        _currentJsonContent = formatted;
        SetEditorContent(formatted);
        UpdateButtonStates();
    }

    private void SetEditorContent(string raw)
    {
        if (ReflectionHelper.TMPAvailable && _tmpInputField != null)
            ApplyHighlight(raw, 0);
        else if (_legacyEditor != null)
            _legacyEditor.text = raw;
    }


    private void UpdateButtonStates()
    {
        if (_saveButton == null || _revertButton == null || _formatButton == null) return;

        var hasChanges = _currentJsonContent != _originalJsonContent;
        var isValidJson = _configManager.IsValidJson(_currentJsonContent);

        ApplyButtonState(_saveButton, hasChanges && isValidJson, _theme.AccentPrimary);
        ApplyButtonState(_revertButton, hasChanges, _theme.WarningColor);

        _formatButton.interactable = !string.IsNullOrEmpty(_currentJsonContent);
        var fc = _formatButton.colors;
        fc.normalColor = isValidJson
            ? _theme.TextSecondary
            : new Color(_theme.WarningColor.r, _theme.WarningColor.g, _theme.WarningColor.b, 0.8f);
        _formatButton.colors = fc;
    }

    private static void ApplyButtonState(Button btn, bool active, Color baseColor)
    {
        btn.interactable = active;
        var c = btn.colors;
        c.normalColor = active ? baseColor : new Color(baseColor.r, baseColor.g, baseColor.b, 0.5f);
        c.disabledColor = new Color(baseColor.r, baseColor.g, baseColor.b, 0.3f);
        btn.colors = c;
    }


    private static string Clean(string s) =>
        s.Replace("\r\n", "\n").Replace("\r", "\n")
            .Trim().Replace("\u00A0", " ").Replace("\t", "  ");

    private static void FullStretch(GameObject go)
    {
        var rt = go.GetComponent<RectTransform>() ?? go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private object CreateTMPFontAsset()
    {
        try
        {
            var font = _consolaFont ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (font == null) return null;
            return ReflectionHelper.MiCreateFontAsset == null
                ? null
                : ReflectionHelper.MiCreateFontAsset.Invoke(null, [font]);
        }
        catch (Exception ex)
        {
            return null;
        }
    }


    private void CheckTMP()
    {
        try
        {
            ReflectionHelper.TryInitTMP();
            if (!ReflectionHelper.TMPAvailable)
                _logger.Warning("[JsonConfigUI] TMP incomplete — using legacy editor.");

            foreach (var font in ReflectionHelper.FindObjectsOfType(typeof(Font)))
            {
                var f = font as Font;
                if (f == null) continue;
                if (f.name.ToLower().Contains("consola")) _consolaFont = f;
                if (_consolaFont != null) break;
            }
        }
        catch (Exception ex)
        {
            _logger.Warning($"[JsonConfigUI] TMP init threw: {ex.Message}");
        }
    }
}
