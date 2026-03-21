using System.Collections;
using System.Linq;
using UnityEngine;
using S1API.UI;
using MelonLoader;
using UnityEngine.UI;
using System.Collections.Generic;
using ModsApp.Helpers;
using ModsApp.Managers;
using ModsApp.UI.Input;
using ModsApp.UI.Input.FieldFactories;
using S1API.Input;
using S1API.Internal.Abstraction;
using S1API.Utils;

namespace ModsApp.UI.Panels;

public class ModDetailsPanel
{
    private readonly Transform _parent;
    private readonly ModManager _modManager;
    private readonly UITheme _theme;
    private readonly MelonLogger.Instance _logger;
    private readonly PreferenceInputFactory _inputFactory;

    private RectTransform _detailsContent;
    private Dictionary<string, object> _modifiedPreferences = new Dictionary<string, object>();
    private HashSet<string> _modifiedMods = new HashSet<string>();
    private Text _modifiedLabel;

    private Button _applyButton;
    private Button _resetButton;

    private JsonConfigManager _jsonConfigManager;
    private JsonConfigUI _jsonConfigUI;

    public const string UnassignedModName = "Unassigned";

    public ModDetailsPanel(Transform parent, ModManager modManager, UITheme theme, MelonLogger.Instance logger)
    {
        _parent = parent;
        _modManager = modManager;
        _theme = theme;
        _logger = logger;
        _inputFactory = new PreferenceInputFactory(theme, logger);

        _jsonConfigManager = new JsonConfigManager(logger);
        _jsonConfigUI = new JsonConfigUI(theme, logger, _jsonConfigManager);
    }

    public void Initialize()
    {
        var rightPanel = UIFactory.Panel("ModDetailsPanel", _parent, _theme.BgCard,
            new Vector2(0.36f, 0.05f), new Vector2(0.98f, 0.82f));
        rightPanel.GetComponent<Image>()?.MakeRounded(12, 48);
        UIHelper.ForceRectToAnchors(rightPanel.GetComponent<RectTransform>(),
            new Vector2(0.36f, 0.05f), new Vector2(0.98f, 0.82f),
            Vector2.zero, Vector2.zero);

        UIHelper.AddBorderEffect(rightPanel, _theme.AccentPrimary);

        _detailsContent = UIFactory.ScrollableVerticalList("ModDetailsContent", rightPanel.transform, out var scrollRect);
        if (scrollRect != null) scrollRect.scrollSensitivity = 15f;
        UIHelper.ForceRectToAnchors(_detailsContent, Vector2.zero, Vector2.one,
            Vector2.zero, Vector2.zero, new Vector2(0.5f, 1f));
        UIHelper.SetupLayoutGroup(_detailsContent.gameObject, 6, true, new RectOffset(12, 12, 12, 12));

        var layout = _detailsContent.GetComponent<VerticalLayoutGroup>();
        if (layout != null)
        {
            layout.spacing = 6;
            layout.padding = new RectOffset(12, 12, 12, 12);
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
        }

        UIFactory.FitContentHeight(_detailsContent);

        UIHelper.DumpRect("ModDetailsPanel", rightPanel.GetComponent<RectTransform>());
        UIHelper.DumpRect("ModDetailsContent", _detailsContent);
    }

    public void ShowWelcome()
    {
        if (_detailsContent == null) return;

        UIFactory.ClearChildren(_detailsContent);

        var welcomeCard = CreateInfoCard("WelcomeCard");
        CreateWelcomeContent(welcomeCard);

        var statsCard = CreateInfoCard("StatsCard");
        CreateStatsContent(statsCard);

        UIHelper.RefreshLayout(_detailsContent);
    }

    public void ShowModDetails(MelonMod mod)
    {
        if (_detailsContent == null) return;

        if (mod == null)
        {
            ShowUnassignedDetails();
            return;
        }

        UIFactory.ClearChildren(_detailsContent);
        _modifiedPreferences.Clear();

        _jsonConfigUI.ResetState();

        var headerCard = CreateInfoCard($"{UIHelper.SanitizeName(mod.Info.Name)}_HeaderCard");
        CreateModHeader(mod, headerCard);

        var categories = _modManager.GetPreferencesForMod(mod).ToList();

        if (categories.Count > 0)
        {
            // Has MelonPreferences - use existing system
            var prefsCard = CreateInfoCard($"{UIHelper.SanitizeName(mod.Info.Name)}_PrefsCard");
            CreatePreferencesSection(mod, prefsCard);

            var actionsCard = CreateInfoCard($"{UIHelper.SanitizeName(mod.Info.Name)}_ActionsCard");
            CreateActionButtons(mod, actionsCard);
        }
        else
        {
            // No MelonPreferences - show JSON fallback
            var jsonCard = CreateInfoCard($"{UIHelper.SanitizeName(mod.Info.Name)}_JsonCard");
            _jsonConfigUI.CreateJsonConfigSection(mod, jsonCard, () => UIHelper.RefreshLayout(_detailsContent), () =>
            {
                _modifiedMods.Add(mod.Info.Name);
                _modifiedLabel.text = "Changes applied, restart may be required";
                _modifiedLabel.gameObject.SetActive(true);
            });
        }

        UIHelper.RefreshLayout(_detailsContent);
    }

    private void ShowUnassignedDetails()
    {
        UIFactory.ClearChildren(_detailsContent);
        _modifiedPreferences.Clear();

        _jsonConfigUI.ResetState();

        var headerCard = CreateInfoCard("UnassignedHeaderCard");

        var title = UIFactory.Text("UnassignedTitle", "Unassigned Preferences", headerCard.transform,
            _theme.SizeLarge, TextAnchor.MiddleLeft, FontStyle.Bold);
        title.color = _theme.TextPrimary;

        var subtitle = UIFactory.Text("UnassignedSubtitle", "Preferences that don't belong to any mod",
            headerCard.transform, _theme.SizeStandard);
        subtitle.color = _theme.TextSecondary;

        _modifiedLabel = UIFactory.Text("ModifiedLabel", "", headerCard.transform, _theme.SizeSmall,
            TextAnchor.MiddleRight,
            FontStyle.Italic);
        _modifiedLabel.color = _theme.WarningColor;
        if (_modifiedMods.Contains(UnassignedModName))
        {
            _modifiedLabel.text = "Changes applied, restart may be required";
            _modifiedLabel.gameObject.SetActive(true);
        }
        else
            _modifiedLabel.gameObject.SetActive(false);

        var categories = _modManager.GetUnassignedPreferences().ToList();

        if (categories.Count > 0)
        {
            var prefsCard = CreateInfoCard("UnassignedPrefsCard");
            RenderPreferences(categories, prefsCard, UnassignedModName);

            var actionsCard = CreateInfoCard("UnassignedActionsCard");
            CreateActionButtons(UnassignedModName, actionsCard.transform);
        }
        else
        {
            var noPrefsCard = CreateInfoCard("NoUnassignedPrefsCard");
            var noPrefs = UIFactory.Text("NoUnassignedPrefs", "No unassigned preferences found",
                noPrefsCard.transform, _theme.SizeSmall, TextAnchor.UpperLeft, FontStyle.Italic);
            noPrefs.color = _theme.TextSecondary;
        }

        UIHelper.RefreshLayout(_detailsContent);
    }

    private GameObject CreateInfoCard(string name)
    {
        var card = UIFactory.Panel(name, _detailsContent, _theme.BgSecondary);
        card.GetComponent<Image>()?.MakeRounded();

        var vlg = card.GetOrAddComponent<VerticalLayoutGroup>();
        vlg.spacing = 8;
        vlg.padding = new RectOffset(12, 12, 8, 8);
        vlg.childControlWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childAlignment = TextAnchor.UpperLeft;

        var csf = card.GetOrAddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        var layoutElement = card.GetOrAddComponent<LayoutElement>();
        layoutElement.preferredHeight = -1;
        layoutElement.minHeight = 0;
        layoutElement.flexibleHeight = 0;
        layoutElement.flexibleWidth = 1;

        return card;
    }

    private void CreateWelcomeContent(GameObject card)
    {
        var title = UIFactory.Text("WelcomeTitle", "Mods Manager", card.transform, _theme.SizeLarge,
            TextAnchor.UpperLeft,
            FontStyle.Bold);
        title.color = _theme.TextPrimary;

        var desc = UIFactory.Text("WelcomeDesc", "Select a mod from the list to view its details and preferences.",
            card.transform, _theme.SizeStandard);
        desc.color = _theme.TextSecondary;
    }

    private void CreateStatsContent(GameObject card)
    {
        var title = UIFactory.Text("StatsTitle", "Statistics", card.transform, _theme.SizeMedium, TextAnchor.UpperLeft,
            FontStyle.Bold);
        title.color = _theme.TextPrimary;

        var count = UIFactory.Text("ModCount", $"Total Mods: {_modManager.ModCount}", card.transform,
            _theme.SizeStandard);
        count.color = _theme.TextSecondary;
    }

    private void CreateModHeader(MelonMod mod, GameObject card)
    {
        var headerContainer = UIFactory.Panel("HeaderContainer", card.transform, Color.clear);
        headerContainer.GetComponent<Image>()?.MakeRounded();

        var headerLayout = headerContainer.GetOrAddComponent<LayoutElement>();
        headerLayout.preferredHeight = 28;
        headerLayout.flexibleHeight = 0;

        var hLayout = headerContainer.AddComponent<HorizontalLayoutGroup>();
        hLayout.spacing = 8;
        hLayout.childAlignment = TextAnchor.MiddleLeft;
        hLayout.childForceExpandWidth = false;
        hLayout.childForceExpandHeight = false;
        hLayout.childControlWidth = true;
        hLayout.childControlHeight = true;

        var title = UIFactory.Text("ModTitle", mod.Info.Name, headerContainer.transform, _theme.SizeLarge,
            TextAnchor.MiddleLeft,
            FontStyle.Bold);
        title.color = _theme.TextPrimary;

        string backendName = "";
        bool compatible = _modManager.isCompatible(mod, ref backendName);

        var (badgeObj, badgeBtn, badgeText) = UIFactory.RoundedButtonWithLabel(
            "BackendBadge",
            backendName,
            headerContainer.transform,
            compatible ? _theme.SuccessColor : _theme.WarningColor,
            70, // width
            22, // height
            _theme.SizeSmall, // font size
            _theme.TextPrimary
        );

        badgeBtn.transition = Selectable.Transition.None;
        badgeBtn.interactable = true;
        badgeBtn.enabled = false;

        badgeText.fontStyle = FontStyle.Bold;
        badgeText.alignment = TextAnchor.MiddleCenter;
        
        CheckAndCreateDocsButton(mod, headerContainer.transform);

        var spacer = new GameObject("Spacer");
        spacer.transform.SetParent(headerContainer.transform, false);
        var spacerLayout = spacer.AddComponent<LayoutElement>();
        spacerLayout.flexibleWidth = 1;
        spacerLayout.flexibleHeight = 0;
        spacerLayout.minWidth = 10;

        _modifiedLabel = UIFactory.Text("ModifiedLabel", "", headerContainer.transform, _theme.SizeSmall,
            TextAnchor.MiddleRight,
            FontStyle.Italic);
        _modifiedLabel.color = _theme.WarningColor;
        if (_modifiedMods.Contains(mod.Info.Name))
        {
            _modifiedLabel.text = "Changes applied, restart may be required";
            _modifiedLabel.gameObject.SetActive(true);
        }
        else
            _modifiedLabel.gameObject.SetActive(false);

        var author = UIFactory.Text("ModAuthor", $"by: {mod.Info.Author}", card.transform, _theme.SizeStandard);
        author.color = _theme.TextSecondary;

        var versionText = $"v. {mod.Info.Version}";
        if (ModVersionTracker.IsUpdated(mod))
        {
            var successColor = ColorUtility.ToHtmlStringRGB(_theme.SuccessColor);
            versionText += $" - <color=#{successColor}>Updated since last launch!</color>";
        }

        if (ModVersionTracker.IsNew(mod))
        {
            var warningColor = ColorUtility.ToHtmlStringRGB(_theme.WarningColor);
            versionText += $" - <color=#{warningColor}>New mod!</color>";
        }

        var version = UIFactory.Text("ModVersion", versionText, card.transform, _theme.SizeStandard);
        version.color = _theme.TextSecondary;
        version.supportRichText = true;

        var dependenciesText = CheckAndFormatDependencies(mod);
        if (!string.IsNullOrEmpty(dependenciesText))
        {
            var dependencies = UIFactory.Text("ModDependencies", dependenciesText, card.transform,
                _theme.SizeSmall);
            dependencies.color = _theme.TextSecondary;
            dependencies.supportRichText = true;
        }

        if (LogManager.Instance.HasErrorsForMod(mod.Info.Name))
        {
            var errors = UIFactory.Text("ModErrors", "This mod has logged errors - click the Logs button for details",
                card.transform, _theme.SizeSmall);
            errors.color = _theme.ErrorColor;
        }
    }

    private string CheckAndFormatDependencies(MelonMod mod)
    {
        var dependencies = _modManager.GetModDependencies(mod);
        var successColor = ColorUtility.ToHtmlStringRGB(_theme.SuccessColor);
        var errorColor = ColorUtility.ToHtmlStringRGB(_theme.ErrorColor);

        var parts = new List<string>();

        // Required dependencies
        if (dependencies.Required.Count != 0)
        {
            var requiredParts = dependencies.Required.Select(dep =>
            {
                var found = _modManager.GetModByAssemblyName(dep);
                if (found != null)
                    return $"<color=#{successColor}>{found.Info.Name} ({found.Info.Version})</color>";
                else if (!dependencies.Missing.Contains(dep))
                    return
                        $"<color=#{successColor}>{dep} (found)</color>"; // likely a library or something we don't have info on, but at least it's present
                return null; // will be handled as missing later
            }).Where(x => x != null);

            if (requiredParts.Any())
                parts.Add("Depends on: " + string.Join(", ", requiredParts));
        }

        // Missing dependencies
        if (dependencies.Missing.Count != 0)
        {
            var missingParts = dependencies.Missing
                .Select(dep => $"<color=#{errorColor}>{dep} (missing)</color>");
            parts.Add(string.Join(", ", missingParts));
        }

        // Optional dependencies
        if (dependencies.Optional.Count != 0)
        {
            var optionalParts = dependencies.Optional.Select(dep =>
            {
                var found = _modManager.GetModByAssemblyName(dep);
                if (found != null)
                    return $"{found.Info.Name} ({found.Info.Version})";
                return _modManager.GetAssemblyByAssemblyName(dep) != null ? $"{dep} (found)" : $"{dep} (not found)";
            });
            parts.Add("Optional dependencies: " + string.Join(", ", optionalParts));
        }

        return string.Join(". ", parts) + (parts.Count > 0 ? "." : "");
    }

    private void CheckAndCreateDocsButton(MelonMod mod, Transform headerContainer)
    {
        var changelog = _modManager.GetChangelog(mod, out var changelogPath);
        var readme = _modManager.GetReadme(mod, out var readmePath);

        if (string.IsNullOrEmpty(changelog) && string.IsNullOrEmpty(readme)) return;

        var (_, docsBtn, docsIcon) = UIHelper.RoundedButtonWithIcon(
            "DocsButton",
            ModsApp.ScrollIconSprite,
            headerContainer,
            _theme.AccentSecondary,
            25, 25,
            _theme.SizeSmall
        );
        docsIcon.color = _theme.TextPrimary;

        EventHelper.AddListener(() =>
        {
            var hasBoth = !string.IsNullOrEmpty(changelog) && !string.IsNullOrEmpty(readme);
            var panel = new FloatingPanelComponent(1000, 500, $"{mod.Info.Name} - Docs");
            var showChangelog = !string.IsNullOrEmpty(changelog);

            ScrollableTextFactory scrollable = null;
            Text filepathLabel = null;

            void BuildScrollable(string text, string path)
            {
                if (scrollable != null)
                    UnityEngine.Object.Destroy(scrollable.Root);

                scrollable = new ScrollableTextFactory(
                    panel.ContentPanel.transform,
                    text,
                    _theme.SizeStandard,
                    _theme.InputPrimary,
                    _theme.BgInput
                );
                var rootRect = scrollable.Root.GetComponent<RectTransform>();
                rootRect.anchorMin = Vector2.zero;
                rootRect.anchorMax = Vector2.one;
                rootRect.offsetMin = new Vector2(6, 6);
                rootRect.offsetMax = new Vector2(-6, hasBoth ? -80 : -44);

                if (filepathLabel != null)
                    filepathLabel.text = $"Source: {path}";

                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(scrollable.ContentRect);
            }

            var filepathGO = UIFactory.Text(
                "FilepathText",
                $"Source: {(showChangelog ? changelogPath : readmePath)}",
                panel.ContentPanel.transform,
                _theme.SizeSmall,
                TextAnchor.MiddleLeft
            );
            filepathGO.color = _theme.TextSecondary;
            filepathLabel = filepathGO;
            var fileRect = filepathGO.GetComponent<RectTransform>();
            fileRect.anchorMin = new Vector2(0, 1);
            fileRect.anchorMax = new Vector2(1, 1);
            fileRect.pivot = new Vector2(0.5f, 1);
            fileRect.sizeDelta = new Vector2(0, 28);
            fileRect.anchoredPosition = new Vector2(2, -8);

            if (hasBoth)
            {
                var filterBar = UIFactory.Panel("DocsFilterBar", panel.ContentPanel.transform, Color.clear);
                var filterRect = filterBar.GetComponent<RectTransform>();
                filterRect.anchorMin = new Vector2(0, 1);
                filterRect.anchorMax = new Vector2(1, 1);
                filterRect.pivot = new Vector2(0.5f, 1);
                filterRect.sizeDelta = new Vector2(0, 36);
                filterRect.anchoredPosition = new Vector2(0, -44);

                var hLayout = filterBar.AddComponent<HorizontalLayoutGroup>();
                hLayout.spacing = 16;
                hLayout.padding = new RectOffset(8, 8, 6, 6);
                hLayout.childAlignment = TextAnchor.MiddleLeft;
                hLayout.childForceExpandWidth = false;
                hLayout.childForceExpandHeight = false;
                hLayout.childControlWidth = false;
                hLayout.childControlHeight = true;

                Toggle changelogToggle = null;
                Toggle readmeToggle = null;

                changelogToggle = UIHelper.CreateLabelledToggle(filterBar.transform, "ChangelogToggle", "Changelog",
                    showChangelog);
                readmeToggle =
                    UIHelper.CreateLabelledToggle(filterBar.transform, "ReadmeToggle", "README", !showChangelog);

                ToggleUtils.AddListener(changelogToggle, val =>
                {
                    if (!val) return;
                    showChangelog = true;
                    if (readmeToggle.isOn) readmeToggle.isOn = false;
                    BuildScrollable(changelog, changelogPath);
                });
                ToggleUtils.AddListener(readmeToggle, val =>
                {
                    if (!val) return;
                    showChangelog = false;
                    if (changelogToggle.isOn) changelogToggle.isOn = false;
                    BuildScrollable(readme, readmePath);
                });
            }

            BuildScrollable(showChangelog ? changelog : readme, showChangelog ? changelogPath : readmePath);
        }, docsBtn.onClick);
    }
    
    private void RenderPreferences(List<MelonPreferences_Category> categories, GameObject card, string modName)
    {
        var header = UIFactory.Text("PrefsHeader", "Preferences", card.transform, _theme.SizeMedium,
            TextAnchor.UpperLeft,
            FontStyle.Bold);
        header.color = _theme.TextPrimary;

        if (categories.Count == 0)
        {
            var noPrefs = UIFactory.Text("NoPrefs", "No preferences available",
                card.transform, _theme.SizeSmall, TextAnchor.UpperLeft, FontStyle.Italic);
            noPrefs.color = _theme.TextSecondary;
            return;
        }

        foreach (var category in categories)
        {
            CreateCategorySection(category, card);
        }
    }

    private void CreatePreferencesSection(MelonMod mod, GameObject card)
    {
        var header = UIFactory.Text("PrefsHeader", "Preferences", card.transform, _theme.SizeMedium,
            TextAnchor.UpperLeft,
            FontStyle.Bold);
        header.color = _theme.TextPrimary;

        var categories = _modManager.GetPreferencesForMod(mod).ToList();

        if (categories.Count == 0)
        {
            var noPrefs = UIFactory.Text("NoPrefs", "No preferences available for this mod :( (that we know of)",
                card.transform, _theme.SizeSmall,
                TextAnchor.UpperLeft, FontStyle.Italic);
            noPrefs.color = _theme.TextSecondary;
            return;
        }

        // only auto-collapse if we have no explicit saved state for ANY of these categories
        var hasAnySavedState = categories.Any(CategoryState.HasExplicitState);
        if (!hasAnySavedState)
        {
            var estimatedHeight = EstimateExpandedHeight(categories);
            var scrollViewHeight = _detailsContent.rect.height;

            if (estimatedHeight > scrollViewHeight * 2f)
            {
                // collapse all except first
                for (var i = 0; i < categories.Count; i++)
                    CategoryState.SetDefault(categories[i], expanded: i == 0);
            }
        }

        foreach (var category in categories)
        {
            CreateCategorySection(category, card);
        }
    }

    private float EstimateExpandedHeight(List<MelonPreferences_Category> categories)
    {
        const float categoryTopBottomPadding = 12f; // vlg.padding top(6) + bottom(6)
        const float categorySpacing = 6f; // spacing between category panels in parent vlg
        var titleHeight = _theme.SizeStandard + 4f; // title text + vlg spacing(4)
        const float entrySpacing = 4f; // vLayout.spacing
        var mainRowHeight = _theme.SizeStandard + 4f; // input row
        var descriptionHeight = _theme.SizeSmall + 4f; // if present
        var commentHeight = _theme.SizeTiny + 4f; // if present

        return categories.Sum(cat =>
        {
            var height = categoryTopBottomPadding + titleHeight;

            foreach (var entry in cat.Entries ?? [])
            {
                height += ModsApp.EntryVlgSpacingEntry.Value + mainRowHeight;
                if (!string.IsNullOrEmpty(entry.Description)) height += descriptionHeight + entrySpacing;
                if (!string.IsNullOrEmpty(entry.Comment)) height += commentHeight + entrySpacing;
            }

            // between entries
            if ((cat.Entries?.Count ?? 0) > 1)
                height += (cat.Entries!.Count - 1) * ModsApp.EntryVlgSpacingEntry.Value;

            return height + categorySpacing;
        });
    }

    private void CreateCategorySection(MelonPreferences_Category category, GameObject parent)
    {
        var categoryPanel = UIFactory.Panel($"{UIHelper.SanitizeName(category.Identifier)}_Category",
            parent.transform,
            _theme.BgCategory);
        categoryPanel.GetComponent<Image>()?.MakeRounded();

        var vlg = categoryPanel.GetOrAddComponent<VerticalLayoutGroup>();
        vlg.spacing = 4;
        vlg.padding = new RectOffset(8, 8, 6, 6);
        vlg.childControlWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childAlignment = TextAnchor.UpperLeft;

        var csf = categoryPanel.GetOrAddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        var layoutElement = categoryPanel.GetOrAddComponent<LayoutElement>();
        layoutElement.preferredHeight = -1;
        layoutElement.flexibleHeight = 0;
        layoutElement.flexibleWidth = 1;

        var categoryTitle = string.IsNullOrWhiteSpace(category.DisplayName)
            ? category.Identifier
            : category.DisplayName;

        var arrowColor = ColorUtility.ToHtmlStringRGB(_theme.TextSecondary);
        var expandedIcon = $"<color=#{arrowColor}>▼</color>";
        var collapsedIcon = $"<color=#{arrowColor}>▶</color>";
        var title = UIFactory.Text($"{UIHelper.SanitizeName(category.Identifier)}_Title",
            CategoryState.IsExpanded(category) ? $"{expandedIcon} {categoryTitle}" : $"{collapsedIcon} {categoryTitle}",
            categoryPanel.transform, _theme.SizeStandard, TextAnchor.UpperLeft, FontStyle.Bold);
        title.supportRichText = true; // just to make sure
        title.color = new Color(_theme.AccentPrimary.r, _theme.AccentPrimary.g, _theme.AccentPrimary.b, 0.9f);

        if (!(category.Entries?.Count > 0)) return;
        var button = title.gameObject.AddComponent<Button>();

        EventHelper.AddListener(() =>
        {
            CategoryState.Toggle(category);
            var arrowColor = ColorUtility.ToHtmlStringRGB(_theme.TextSecondary);
            var expandedIcon = $"<color=#{arrowColor}>▼</color>";
            var collapsedIcon = $"<color=#{arrowColor}>▶</color>";
            title.text = CategoryState.IsExpanded(category)
                ? $"{expandedIcon} {categoryTitle}"
                : $"{collapsedIcon} {categoryTitle}";
            UpdateCategoryVisibility();
        }, button.onClick);

        foreach (var entry in category.Entries)
        {
            CreatePreferenceEntry(entry, categoryPanel, category.Identifier);
        }

        UpdateCategoryVisibility(); // make sure to set initial visibility based on saved state
        return;

        void UpdateCategoryVisibility()
        {
            for (var i = 0; i < categoryPanel.transform.childCount; i++)
            {
                var child = categoryPanel.transform.GetChild(i);
                if (child.gameObject != title.gameObject)
                    child.gameObject.SetActive(CategoryState.IsExpanded(category));
            }

            var parentRT = categoryPanel.transform.parent.GetComponent<RectTransform>();
            if (parentRT != null)
                UIHelper.RefreshLayout(parentRT);
        }
    }

    private void CreatePreferenceEntry(MelonPreferences_Entry entry, GameObject parent, string categoryId)
    {
        var entryName = entry.DisplayName ?? entry.Identifier ?? "Entry";
        var entryKey = $"{categoryId}.{entry.Identifier}";

        var entryContainer = UIFactory.Panel(
            $"{UIHelper.SanitizeName(categoryId)}_{UIHelper.SanitizeName(entryName)}_Container",
            parent.transform, Color.clear);
        entryContainer.GetComponent<Image>()?.MakeRounded(4, 16);

        // Layout for main row + description + comment
        var vLayout = entryContainer.AddComponent<VerticalLayoutGroup>();
        vLayout.spacing = 4;
        vLayout.childAlignment = TextAnchor.UpperLeft;
        vLayout.childForceExpandHeight = false;
        vLayout.childForceExpandWidth = true;
        vLayout.childControlHeight = true;

        vLayout.padding = new RectOffset(0, 0, ModsApp.EntryVlgSpacingEntry.Value, 0); // some top padding for spacing between entries

        var containerLayout = entryContainer.GetOrAddComponent<LayoutElement>();
        containerLayout.flexibleWidth = 1;
        containerLayout.flexibleHeight = 0;

        var mainRow = UIFactory.Panel(
            $"{UIHelper.SanitizeName(categoryId)}_{UIHelper.SanitizeName(entryName)}_MainRow",
            entryContainer.transform, Color.clear);

        var hLayout = mainRow.AddComponent<HorizontalLayoutGroup>();
        hLayout.spacing = 8;
        hLayout.childAlignment = TextAnchor.MiddleLeft;
        hLayout.childForceExpandWidth = false;
        hLayout.childForceExpandHeight = false;
        hLayout.childControlHeight = true;

        var label = UIFactory.Text(
            $"{UIHelper.SanitizeName(categoryId)}_{UIHelper.SanitizeName(entryName)}_Label",
            entryName, mainRow.transform, _theme.SizeStandard, TextAnchor.MiddleLeft);
        label.color = _theme.TextPrimary;
        var labelLayout = label.gameObject.GetOrAddComponent<LayoutElement>();
        labelLayout.minWidth = 80;
        labelLayout.flexibleWidth = 0;

        var typeHint = UIFactory.Text(
            $"{UIHelper.SanitizeName(categoryId)}_{UIHelper.SanitizeName(entryName)}_TypeHint",
            TypeNameHelper.GetFriendlyTypeName(entry.BoxedValue.GetType()), mainRow.transform, _theme.SizeSmall,
            TextAnchor.MiddleLeft);
        typeHint.color = _theme.TextSecondary;
        var typeHintLayout = typeHint.gameObject.GetOrAddComponent<LayoutElement>();
        typeHintLayout.minWidth = 50;
        typeHintLayout.flexibleWidth = 0;

        if (ModsApp.InputsOnRightEntry.Value)
        {
            var spacer = new GameObject("Spacer");
            spacer.transform.SetParent(mainRow.transform, false);
            spacer.GetOrAddComponent<LayoutElement>().flexibleWidth = 0.1f;
        }

        var currentValue = _modifiedPreferences.ContainsKey(entryKey)
            ? _modifiedPreferences[entryKey]
            : entry.BoxedValue;
        _inputFactory.CreatePreferenceInput(entry, mainRow, entryKey, currentValue, OnPreferenceValueChanged);

        if (!string.IsNullOrEmpty(entry.Description))
        {
            var descriptionText = UIFactory.Text(
                $"{UIHelper.SanitizeName(categoryId)}_{UIHelper.SanitizeName(entryName)}_Description",
                entry.Description, entryContainer.transform, _theme.SizeSmall, TextAnchor.UpperLeft);
            descriptionText.color = _theme.TextSecondary * 0.9f; // slightly dimmed

            var descLayout = descriptionText.gameObject.GetOrAddComponent<LayoutElement>();
            descLayout.flexibleWidth = 1;

            var descFitter = descriptionText.gameObject.AddComponent<ContentSizeFitter>();
            descFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            descFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        if (!string.IsNullOrEmpty(entry.Comment))
        {
            var commentText = UIFactory.Text(
                $"{UIHelper.SanitizeName(categoryId)}_{UIHelper.SanitizeName(entryName)}_Comment",
                entry.Comment, entryContainer.transform, _theme.SizeTiny, TextAnchor.UpperLeft);
            commentText.color = _theme.TextSecondary * 0.8f;

            var commentLayout = commentText.gameObject.GetOrAddComponent<LayoutElement>();
            commentLayout.flexibleWidth = 1;

            var commentFitter = commentText.gameObject.AddComponent<ContentSizeFitter>();
            commentFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            commentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }
    }

    private void OnPreferenceValueChanged(string entryKey, object newValue)
    {
        _modifiedPreferences[entryKey] = newValue;
        UpdateButtonStates();
    }

    private void CreateActionButtons(MelonMod mod, GameObject card) =>
        CreateActionButtons(mod.Info.Name, card.transform);

    private void CreateActionButtons(string modName, Transform parent)
    {
        var buttonContainer = UIFactory.Panel("ButtonContainer", parent, Color.clear);

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

        var (applyObj, applyButton, _) = UIFactory.RoundedButtonWithLabel(
            "ApplyButton", "Apply Changes", buttonContainer.transform,
            _theme.AccentPrimary, 120, 30, _theme.SizeStandard, _theme.TextPrimary);

        _applyButton = applyButton;
        EventHelper.AddListener(() => ApplyPreferenceChanges(modName), applyButton.onClick);

        var (resetObj, resetButton, _) = UIFactory.RoundedButtonWithLabel(
            "ResetButton", "Reset", buttonContainer.transform,
            _theme.WarningColor, 80, 30, _theme.SizeStandard, _theme.TextPrimary);

        _resetButton = resetButton;
        EventHelper.AddListener(() => ResetPreferenceChanges(modName), resetButton.onClick);

        UpdateButtonStates();
    }

    private void ApplyPreferenceChanges(string modName)
    {
        var isUnassigned = modName == UnassignedModName;
        var mod = isUnassigned ? null : _modManager.GetMod(modName);
        Controls.IsTyping = false;
        foreach (var kvp in _modifiedPreferences)
        {
            var parts = kvp.Key.Split('.');
            if (parts.Length == 2)
            {
                var categoryId = parts[0];
                var entryId = parts[1];

                // Find the preference entry and update it
                MelonPreferences_Category category = null;
                MelonPreferences_Entry entry = null;

                if (isUnassigned)
                {
                    var unassignedCategories = _modManager.GetUnassignedPreferences().ToList();
                    category = unassignedCategories.FirstOrDefault(c => c.Identifier == categoryId);
                }
                else
                {
                    var categories = _modManager.GetPreferencesForMod(mod).ToList();
                    category = categories.FirstOrDefault(c => c.Identifier == categoryId);
                }

                entry = category?.Entries?.FirstOrDefault(e => e.Identifier == entryId);

                if (entry != null)
                {
                    try
                    {
                        var type = entry.BoxedValue.GetType();
                        // if it's not of the type, try to convert
                        if (kvp.Value.GetType() == type)
                            entry.BoxedValue = kvp.Value;
                        else if (type.IsEnum)
                            entry.BoxedValue = System.Enum.Parse(type, kvp.Value.ToString());
                        else
                            entry.BoxedValue = System.Convert.ChangeType(kvp.Value, type);
                        _logger.Msg($"Applied preference change: {kvp.Key} = {kvp.Value}");
                    }
                    catch (System.Exception ex)
                    {
                        _logger.Error($"Failed to apply preference {kvp.Key}: {ex.Message}");
                    }
                }
            }
        }

        // Save preferences
        MelonPreferences.Save();
        _modifiedPreferences.Clear();

        // Refresh the display
        if (isUnassigned)
            ShowUnassignedDetails();
        else
            ShowModDetails(mod);

        _logger.Msg("Preferences applied and saved successfully");

        _modifiedMods.Add(modName);
        _modifiedLabel.text = "Changes applied, restart may be required";
        _modifiedLabel.gameObject.SetActive(true);
    }

    private void ResetPreferenceChanges(string modName)
    {
        bool isUnassigned = modName == UnassignedModName;
        _modifiedPreferences.Clear();

        if (isUnassigned)
            ShowUnassignedDetails();
        else
            ShowModDetails(_modManager.GetMod(modName));

        Controls.IsTyping = false;
        _logger.Msg("Preference changes reset");
    }

    private void UpdateButtonStates()
    {
        if (_applyButton == null || _resetButton == null) return;

        var hasChanges = _modifiedPreferences.Count > 0;

        _applyButton.interactable = hasChanges;
        var applyColors = _applyButton.colors;
        applyColors.normalColor = hasChanges
            ? _theme.AccentPrimary
            : new Color(_theme.AccentPrimary.r, _theme.AccentPrimary.g, _theme.AccentPrimary.b, 0.5f);
        applyColors.disabledColor =
            new Color(_theme.AccentPrimary.r, _theme.AccentPrimary.g, _theme.AccentPrimary.b, 0.3f);
        _applyButton.colors = applyColors;

        _resetButton.interactable = hasChanges;
        var resetColors = _resetButton.colors;
        resetColors.normalColor = hasChanges
            ? _theme.WarningColor
            : new Color(_theme.WarningColor.r, _theme.WarningColor.g, _theme.WarningColor.b, 0.5f);
        resetColors.disabledColor =
            new Color(_theme.WarningColor.r, _theme.WarningColor.g, _theme.WarningColor.b, 0.3f);
        _resetButton.colors = resetColors;
    }
}