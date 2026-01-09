using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using S1API.UI;
using MelonLoader;
using ModsApp.Helpers;
using ModsApp.Managers;
using ModsApp.UI.Input.FieldFactories;
using S1API.Input;
using S1API.Internal.Abstraction;
using S1API.Utils;

namespace ModsApp.UI.Panels;

public class ModListPanel
{
    public event Action<MelonMod> OnModSelected;

    private readonly Transform _parent;
    private readonly ModManager _modManager;
    private readonly UITheme _theme;
    private readonly MelonLogger.Instance _logger;

    private RectTransform _listContent;
    private readonly Dictionary<string, GameObject> _modButtons = new();
    private Dictionary<string, Text> _modLabels = new Dictionary<string, Text>();
    private string _selectedModName;

    private bool _isUnassignedSelected;
    public const string UnassignedButtonName = "Unassigned";

    private IEnumerable<MelonMod> _allMods = Enumerable.Empty<MelonMod>();
    private string _searchQuery = string.Empty;
    private InputField _searchInput;
    private Button _clearButton;

    public ModListPanel(Transform parent, ModManager modManager, UITheme theme, MelonLogger.Instance logger)
    {
        _parent = parent;
        _modManager = modManager;
        _theme = theme;
        _logger = logger;
    }

    public void Initialize()
    {
        var leftPanel = UIFactory.Panel("ModListPanel", _parent, _theme.BgSecondary,
            new Vector2(0.02f, 0.05f), new Vector2(0.35f, 0.82f));
        UIHelper.ForceRectToAnchors(leftPanel.GetComponent<RectTransform>(),
            new Vector2(0.02f, 0.05f), new Vector2(0.35f, 0.82f),
            Vector2.zero, Vector2.zero);

        UIHelper.AddBorderEffect(leftPanel, _theme.AccentPrimary);

        InitializeSearchPanel(leftPanel);

        var listPanel = UIFactory.Panel("ListPanel", leftPanel.transform, _theme.BgPrimary);
        UIHelper.ForceRectToAnchors(listPanel.GetComponent<RectTransform>(),
            new Vector2(0f, 0f), new Vector2(1f, 0.87f),
            Vector2.zero, Vector2.zero);

        _listContent = UIFactory.ScrollableVerticalList("ModListContent", listPanel.transform, out _);
        UIHelper.ForceRectToAnchors(_listContent, Vector2.zero, Vector2.one,
            Vector2.zero, Vector2.zero, new Vector2(0.5f, 1f));
        UIHelper.SetupLayoutGroup(_listContent.gameObject, 4, false, new RectOffset(8, 8, 8, 8));

        UIHelper.DumpRect("ModListPanel", leftPanel.GetComponent<RectTransform>());
        UIHelper.DumpRect("ModListContent", _listContent);
    }

    private void InitializeSearchPanel(GameObject leftPanel)
    {
        var searchPanel = UIFactory.Panel("SearchPanel", leftPanel.transform, _theme.BgCard);
        UIHelper.ForceRectToAnchors(searchPanel.GetComponent<RectTransform>(),
            new Vector2(0f, 0.85f), new Vector2(1f, 0.95f),
            Vector2.zero, Vector2.zero);

        var searchLayout = searchPanel.GetComponent<HorizontalLayoutGroup>();
        if (searchLayout == null) searchLayout = searchPanel.AddComponent<HorizontalLayoutGroup>();
        searchLayout.spacing = 4;
        searchLayout.padding = new RectOffset(8, 8, 0, 1);
        searchLayout.childControlWidth = true;
        searchLayout.childForceExpandWidth = false;
        searchLayout.childForceExpandHeight = false;
        searchLayout.childAlignment = TextAnchor.UpperCenter;

        _searchInput = InputFieldFactory.CreateInputField(
            searchPanel,
            "SearchMods",
            "",
            InputField.ContentType.Standard,
            100,
            null);

        if (_searchInput != null)
        {
            var inputLayoutElem = _searchInput.GetComponent<LayoutElement>();
            if (inputLayoutElem == null) inputLayoutElem = _searchInput.gameObject.AddComponent<LayoutElement>();
            inputLayoutElem.preferredHeight = 28;
            inputLayoutElem.flexibleHeight = 0;
            inputLayoutElem.flexibleWidth = 1;

            // weird hack to not cast
            ProcessTextInChildren(_searchInput.transform, text =>
            {
                if (text.gameObject.name.Contains("Placeholder"))
                    text.text = "Search mods...";
            });

            var empty = "";
            EventHelper.AddListener<string>(text =>
            {
                empty = empty;
                Controls.IsTyping = true;
                OnSearchTextChanged(text);
            }, _searchInput.onValueChanged);

            EventHelper.AddListener<string>(_ =>
            {
                empty = empty;
                Controls.IsTyping = false;
            }, _searchInput.onEndEdit);
        }

        var (_, clearButton, _) = UIFactory.RoundedButtonWithLabel(
            "SearchClear", "x", searchPanel.transform,
            new Color(1, 1, 1, 0.2f), 30, 30, _theme.SizeStandard, _theme.WarningColor);
        clearButton.gameObject.SetActive(false);
        _clearButton = clearButton;

        if (clearButton != null)
        {
            var clearLayoutElem = clearButton.gameObject.GetComponent<LayoutElement>();
            if (clearLayoutElem == null) clearLayoutElem = clearButton.gameObject.AddComponent<LayoutElement>();
            clearLayoutElem.preferredHeight = 28;
            clearLayoutElem.preferredWidth = 28;
            clearLayoutElem.flexibleWidth = 0;

            EventHelper.AddListener(OnSearchClear, clearButton.onClick);
        }
    }

    private void OnSearchTextChanged(string text)
    {
        _searchQuery = text;

        if (_clearButton != null)
            _clearButton.gameObject.SetActive(!string.IsNullOrWhiteSpace(text));

        PopulateList();
    }

    private void OnSearchClear()
    {
        if (_searchInput != null)
            _searchInput.text = string.Empty;
        _searchQuery = string.Empty;
        PopulateList();
    }

    private static void ProcessTextInChildren(Transform root, Action<Text> action)
    {
        if (root == null)
            return;

        if (root.TryGetComponent<Text>(out var text))
        {
            action(text);
        }

        for (int i = 0; i < root.childCount; i++)
        {
            ProcessTextInChildren(root.GetChild(i), action);
        }
    }


    public void PopulateList()
    {
        if (_listContent == null) return;

        if (!_allMods.Any())
            _allMods = _modManager.GetAllMods();

        UIFactory.ClearChildren(_listContent);
        _modButtons.Clear();

        if (_modManager.HasUnassignedPreferences())
        {
            CreateModButton(UnassignedButtonName, "Unassigned", "0.0", isUnassigned: true);
        }

        var modsToShow = FilterMods(_searchQuery);
        foreach (var mod in modsToShow)
        {
            CreateModButton(mod.Info.Name, mod.Info.Name, $"v{mod.Info.Version}", isUnassigned: false);
        }

        if (!modsToShow.Any())
        {
            var noResults = UIFactory.Text("NoSearchResults", "No mods found",
                _listContent, _theme.SizeSmall, TextAnchor.MiddleCenter);
            noResults.color = _theme.TextSecondary;
        }

        UpdateButtonHighlights();
        UIHelper.RefreshLayout(_listContent);
    }

    private IEnumerable<MelonMod> FilterMods(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return _allMods;

        return _allMods.Where(mod =>
            mod.Info.Name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0);
    }

    private void CreateModButton(string internalName, string displayName, string version, bool isUnassigned)
    {
        var buttonGo = UIFactory.Panel($"{UIHelper.SanitizeName(internalName)}_Button", _listContent,
            _theme.AccentSecondary);

        var button = buttonGo.GetOrAddComponent<Button>();
        UIHelper.SetupButton(button, _theme, () => SelectMod(internalName, isUnassigned));
        UIHelper.ConfigureButtonLayout(buttonGo.GetComponent<RectTransform>(), 48f);

        // Main label
        var label = UIFactory.Text($"{UIHelper.SanitizeName(internalName)}_Label", displayName, buttonGo.transform,
            _theme.SizeStandard, TextAnchor.MiddleLeft);
        label.color = _theme.TextPrimary;
        UIHelper.ConfigureButtonText(label.rectTransform, new Vector2(0f, 0f), new Vector2(0.7f, 1f), 16f, -8f, 4f,
            -4f);

        // Version label
        var versionText = UIFactory.Text($"{UIHelper.SanitizeName(internalName)}_Version", version,
            buttonGo.transform, _theme.SizeTiny, TextAnchor.MiddleRight);
        versionText.color = _theme.TextSecondary;
        UIHelper.ConfigureButtonText(versionText.rectTransform, new Vector2(0.7f, 0f), new Vector2(1f, 1f), 4f, -12f,
            4f, -4f);

        _modButtons[internalName] = buttonGo;
        _modLabels[internalName] = label;
    }

    private void SelectMod(string modName, bool isUnassigned)
    {
        _selectedModName = modName;
        _isUnassignedSelected = isUnassigned;
        UpdateButtonHighlights();
        OnModSelected?.Invoke(isUnassigned ? null : _modManager.GetMod(modName));
    }

    private void UpdateButtonHighlights()
    {
        foreach (var kvp in _modButtons)
        {
            bool isSelected = kvp.Key == _selectedModName;

            var img = kvp.Value.GetComponent<Image>();
            if (img != null)
                img.color = isSelected ? _theme.AccentPrimary : _theme.AccentSecondary;

            if (_modLabels.TryGetValue(kvp.Key, out var label))
            {
                label.fontStyle = isSelected ? FontStyle.Bold : FontStyle.Normal;
                label.color = isSelected ? Color.white : _theme.TextPrimary;
            }
        }
    }
}