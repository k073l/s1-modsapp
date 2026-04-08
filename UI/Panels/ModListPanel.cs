using MelonLoader;
using ModsApp.Helpers;
using ModsApp.Helpers.Registries;
using ModsApp.Managers;
using ModsApp.UI.Search;
using S1API.UI;
using UnityEngine;
using UnityEngine.UI;

namespace ModsApp.UI.Panels;

public class ModListPanel
{
    public event Action<MelonMod> OnModSelected;
    public event Action<InactiveModInfo> OnInactiveModSelected;
    public event Action<IReadOnlyList<SearchResult>, string> OnAllModsSelected;

    private readonly Transform _parent;
    private readonly ModManager _modManager;
    private readonly UITheme _theme;
    private readonly MelonLogger.Instance _logger;

    private RectTransform _listContent;
    private readonly Dictionary<string, GameObject> _modButtons = new();
    private Dictionary<string, Text> _modLabels = new Dictionary<string, Text>();
    internal string SelectedModName;
    private static Dictionary<MelonMod, Image> WarningIcons = new();

    private bool _isUnassignedSelected;
    public const string UnassignedButtonName = "Unassigned";
    public const string AllModsButtonName = "All Mods";
    private bool _isAllModsSelected;
    public bool IsAllModsSelected => _isAllModsSelected;
    private IReadOnlyList<SearchResult> _searchResults = new List<SearchResult>();

    private IEnumerable<MelonMod> _allMods = Enumerable.Empty<MelonMod>();
    private string _searchQuery = string.Empty;

    private SearchManager _searchManager;
    private SearchBar _searchBar;

    public ModListPanel(Transform parent, ModManager modManager, UITheme theme, MelonLogger.Instance logger)
    {
        _parent = parent;
        _modManager = modManager;
        _theme = theme;
        _logger = logger;
        _searchManager = new SearchManager(modManager);
    }

    public void Initialize()
    {
        var leftPanel = UIFactory.Panel("ModListPanel", _parent, _theme.BgSecondary,
            new Vector2(0.02f, 0.05f), new Vector2(0.35f, 0.82f));
        leftPanel.GetComponent<Image>()?.MakeRounded(12, 48);
        UIHelper.ForceRectToAnchors(leftPanel.GetComponent<RectTransform>(),
            new Vector2(0.02f, 0.05f), new Vector2(0.35f, 0.82f),
            Vector2.zero, Vector2.zero);

        UIHelper.AddBorderEffect(leftPanel, _theme.AccentPrimary);

        _searchBar = new SearchBar(_searchManager, _theme, OnSearchChanged, OnAllModsResultsReady);
        _searchBar.Initialize(leftPanel);
        var listPanel = UIFactory.Panel("ListPanel", leftPanel.transform, _theme.BgPrimary);
        listPanel.GetComponent<Image>()?.MakeRounded(12, 48);
        UIHelper.ForceRectToAnchors(listPanel.GetComponent<RectTransform>(),
            new Vector2(0f, 0f), new Vector2(1f, 0.87f),
            Vector2.zero, Vector2.zero);

        _listContent = UIFactory.ScrollableVerticalList("ModListContent", listPanel.transform, out var scrollRect);
        if (scrollRect != null) scrollRect.scrollSensitivity = 15f;
        UIHelper.ForceRectToAnchors(_listContent, Vector2.zero, Vector2.one,
            Vector2.zero, Vector2.zero, new Vector2(0.5f, 1f));
        UIHelper.SetupLayoutGroup(_listContent.gameObject, 4, false, new RectOffset(8, 8, 8, 8));
    }

    private void OnSearchChanged(string query)
    {
        _searchQuery = query;
        _searchResults = _searchBar.Results;
        PopulateList();
            
        if (_isAllModsSelected)
            OnAllModsSelected?.Invoke(_searchResults, _searchQuery);
    }

    private void OnAllModsResultsReady(IReadOnlyList<SearchResult> results, string query)
    {
        _searchResults = results;
        _searchQuery = query;
        OnAllModsSelected?.Invoke(results, query);
    }

    public void PopulateList()
    {
        if (_listContent == null) return;

        if (!_allMods.Any())
            _allMods = _modManager.GetAllMods();

        UIFactory.ClearChildren(_listContent);
        _modButtons.Clear();
        WarningIcons.Clear();

        if (_modManager.HasUnassignedPreferences())
            CreateModButton(UnassignedButtonName, "Unassigned", "0.0", isUnassigned: true);

        if (_searchResults.Any())
            CreateModButton(AllModsButtonName, "All Mods", "settings", isUnassigned: false, isAllMods: true);

        var modsToShow = FilterMods(_searchQuery).ToList();
        foreach (var mod in modsToShow)
            CreateModButton(mod.Info.Name, mod.Info.Name, $"v{mod.Info.Version}", isUnassigned: false);

        // Inactive (disabled) mods — scanned from disk
        ModFolderScanner.Scan(out _, out var inactiveMods, out _);
        var filteredInactive = string.IsNullOrWhiteSpace(_searchQuery)
            ? inactiveMods
            : inactiveMods.Where(m =>
                m.Name.IndexOf(_searchQuery, StringComparison.OrdinalIgnoreCase) >= 0 ||
                m.Author.IndexOf(_searchQuery, StringComparison.OrdinalIgnoreCase) >= 0).ToList();

        foreach (var inactive in filteredInactive)
            CreateInactiveModButton(inactive);

        if (!modsToShow.Any() && !filteredInactive.Any())
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
            mod.Info.Name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
            mod.Info.Author.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0);
    }

    private void CreateModButton(string internalName, string displayName, string version, bool isUnassigned,
        bool isAllMods = false)
    {
        var buttonGo = UIFactory.Panel($"{UIHelper.SanitizeName(internalName)}_Button", _listContent,
            _theme.AccentSecondary);
        buttonGo.GetComponent<Image>()?.MakeRounded(4, 16);

        var button = buttonGo.GetOrAddComponent<Button>();
        UIHelper.SetupButton(button, _theme, () => SelectMod(internalName, isUnassigned, isAllMods));
        UIHelper.ConfigureButtonLayout(buttonGo.GetComponent<RectTransform>(), 48f);

        // Main label
        var label = UIFactory.Text($"{UIHelper.SanitizeName(internalName)}_Label", displayName, buttonGo.transform,
            _theme.SizeStandard, TextAnchor.MiddleLeft);
        label.color = _theme.TextPrimary;
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        label.verticalOverflow = VerticalWrapMode.Truncate;
        UIHelper.ConfigureButtonText(label.rectTransform, new Vector2(0f, 0f), new Vector2(0.65f, 1f), 16f, -8f, 4f,
            -4f);

        if (!isUnassigned && !isAllMods)
        {
            var mod = _modManager.GetMod(internalName);
            if (mod != null)
            {
                var isNew = ModVersionTracker.IsNew(mod);
                var isUpdated = ModVersionTracker.IsUpdated(mod);
                var warningAnchorX = Mathf.Lerp(0.875f, 0.80f, (int)SettingsRegistry.TextSizeProfileEntry.Value / 4f);
                var dotAnchorX = Mathf.Lerp(0.815f, 0.73f, (int)SettingsRegistry.TextSizeProfileEntry.Value / 4f);

                if (!WarningIcons.TryGetValue(mod, out var icon))
                {
                    icon = UIHelper.AddIcon(
                        IconRegistry.WarningIconSprite,
                        buttonGo.transform,
                        new Vector2(warningAnchorX, 0.5f),
                        Vector2.zero,
                        _theme.SizeStandard * 1.2f);
                    icon.color = _theme.ErrorColor;
                    WarningIcons.TryAdd(mod, icon);
                }

                var deps = _modManager.GetModDependencies(mod);
                var hasWarning = deps.Missing.Count > 0 || LogManager.Instance.HasErrorsForMod(mod.Info.Name);
                icon.gameObject.SetActive(hasWarning);

                if (isNew || isUpdated)
                {
                    var dotColor = isUpdated
                        ? new Color(_theme.SuccessColor.r, _theme.SuccessColor.g, _theme.SuccessColor.b, 0.7f)
                        : new Color(_theme.WarningColor.r, _theme.WarningColor.g, _theme.WarningColor.b, 0.5f);

                    var dot = UIHelper.AddIcon(
                        UIHelper.GetRoundedSprite(32, 16),
                        buttonGo.transform,
                        new Vector2(hasWarning ? dotAnchorX : warningAnchorX, 0.5f),
                        Vector2.zero,
                        _theme.SizeStandard * 0.6f);
                    dot.color = dotColor;
                }
            }
        }

        var versionText = UIFactory.Text($"{UIHelper.SanitizeName(internalName)}_Version", version,
            buttonGo.transform, _theme.SizeTiny, TextAnchor.MiddleRight);
        versionText.color = _theme.TextSecondary;
        UIHelper.ConfigureButtonText(versionText.rectTransform, new Vector2(0.7f, 0f), new Vector2(1f, 1f), 4f, -12f,
            4f, -4f);

        _modButtons[internalName] = buttonGo;
        _modLabels[internalName] = label;
    }

    private void CreateInactiveModButton(InactiveModInfo inactive)
    {
        var safeName = UIHelper.SanitizeName(inactive.Name) + "_Inactive";
        var buttonKey = inactive.FilePath; // path as key

        var buttonGo = UIFactory.Panel($"{safeName}_Button", _listContent, _theme.AccentSecondary);
        buttonGo.GetComponent<Image>()?.MakeRounded(4, 16);

        // dim items to signal disabled state
        var bg = buttonGo.GetComponent<Image>();
        if (bg != null)
            bg.color = new Color(
                _theme.AccentSecondary.r * 0.65f,
                _theme.AccentSecondary.g * 0.65f,
                _theme.AccentSecondary.b * 0.65f,
                _theme.AccentSecondary.a);

        var button = buttonGo.GetOrAddComponent<Button>();
        UIHelper.SetupButton(button, _theme, () => SelectInactiveMod(inactive));
        UIHelper.ConfigureButtonLayout(buttonGo.GetComponent<RectTransform>(), 48f);

        var label = UIFactory.Text($"{safeName}_Label", inactive.Name, buttonGo.transform,
            _theme.SizeStandard, TextAnchor.MiddleLeft);
        label.color = new Color(_theme.TextPrimary.r, _theme.TextPrimary.g, _theme.TextPrimary.b, 0.45f);
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        label.verticalOverflow = VerticalWrapMode.Truncate;
        UIHelper.ConfigureButtonText(label.rectTransform, new Vector2(0f, 0f), new Vector2(0.65f, 1f), 16f, -8f, 4f,
            -4f);

        // "disabled" tag - or pending re-enable indicator
        var hasPending = ModToggleManager.HasPendingChange(inactive.ActivePath);
        var pendingEnable = hasPending && ModToggleManager.GetDesiredState(inactive.ActivePath);

        var tagText = pendingEnable ? "→ enable" : "disabled";
        var tagColor = pendingEnable
            ? new Color(_theme.SuccessColor.r, _theme.SuccessColor.g, _theme.SuccessColor.b, 0.7f)
            : new Color(_theme.TextSecondary.r, _theme.TextSecondary.g, _theme.TextSecondary.b, 0.5f);

        var disabledTag = UIFactory.Text($"{safeName}_Tag", tagText,
            buttonGo.transform, _theme.SizeTiny, TextAnchor.MiddleRight);
        disabledTag.color = tagColor;
        disabledTag.fontStyle = FontStyle.Italic;
        UIHelper.ConfigureButtonText(disabledTag.rectTransform, new Vector2(0.7f, 0f), new Vector2(1f, 1f), 4f, -12f,
            4f, -4f);

        _modButtons[buttonKey] = buttonGo;
        _modLabels[buttonKey] = label;
    }

    private void SelectMod(string modName, bool isUnassigned, bool isAllMods = false)
    {
        SelectedModName = modName;
        _isUnassignedSelected = isUnassigned;
        _isAllModsSelected = isAllMods;
        UpdateButtonHighlights();
        if (isAllMods)
            OnAllModsSelected?.Invoke(_searchResults, _searchQuery);
        else
            OnModSelected?.Invoke(isUnassigned ? null : _modManager.GetMod(modName));
    }

    public void SelectModByName(string modName)
    {
        SelectedModName = modName;
        _isUnassignedSelected = false;
        _isAllModsSelected = false;
        UpdateButtonHighlights();
    }

    private void SelectInactiveMod(InactiveModInfo inactive)
    {
        SelectedModName = inactive.FilePath;
        _isUnassignedSelected = false;
        _isAllModsSelected = false;
        UpdateButtonHighlights();
        OnInactiveModSelected?.Invoke(inactive);
    }

    internal void UpdateButtonHighlights()
    {
        foreach (var kvp in _modButtons)
        {
            var isSelected = kvp.Key == SelectedModName;

            // Inactive mods are keyed by file path (contains path separator or .inactive)
            var isInactive = kvp.Key.Contains(Path.DirectorySeparatorChar) ||
                             kvp.Key.Contains('/') ||
                             kvp.Key.EndsWith(ModToggleManager.InactiveExtension);
            var isPending = ModToggleManager.HasPendingChangeByName(kvp.Key);

            var img = kvp.Value.GetComponent<Image>();
            if (img != null)
            {
                switch (isSelected)
                {
                    // blend accent with the dimmed base
                    case true when isInactive:
                        img.color = new Color(
                            _theme.AccentPrimary.r * 0.65f,
                            _theme.AccentPrimary.g * 0.65f,
                            _theme.AccentPrimary.b * 0.65f,
                            _theme.AccentPrimary.a * 0.65f);
                        break;
                    case true:
                        img.color = _theme.AccentPrimary;
                        break;
                    default:
                    {
                        if (isInactive)
                            img.color = new Color(
                                _theme.AccentSecondary.r * 0.45f,
                                _theme.AccentSecondary.g * 0.45f,
                                _theme.AccentSecondary.b * 0.45f,
                                _theme.AccentSecondary.a * 0.45f);
                        else
                            img.color = _theme.AccentSecondary;
                        break;
                    }
                }
            }

            if (!_modLabels.TryGetValue(kvp.Key, out var label)) continue;
            if (isSelected)
            {
                label.fontStyle = FontStyle.Bold;
                label.color = isInactive
                    ? new Color(_theme.TextPrimary.r, _theme.TextPrimary.g, _theme.TextPrimary.b, 0.7f)
                    : _theme.TextPrimary + new Color(0.05f, 0.05f, 0.05f, 1f);
            }
            else
            {
                label.fontStyle = FontStyle.Normal;
                label.color = isInactive
                    ? new Color(_theme.TextPrimary.r, _theme.TextPrimary.g, _theme.TextPrimary.b, 0.45f)
                    : _theme.TextPrimary;
            }

            if (isPending)
            {
                var pendingColor = ModToggleManager.GetDesiredState(kvp.Key)
                    ? _theme.SuccessColor
                    : _theme.WarningColor;

                var baseColor = img.color;
                var pushed = Color.Lerp(baseColor, pendingColor, 0.75f);
                img.color = Color.Lerp(pushed, baseColor, 0.25f);
            }
        }
    }
}