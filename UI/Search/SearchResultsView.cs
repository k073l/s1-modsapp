using MelonLoader;
using ModsApp.Helpers;
using S1API.Internal.Abstraction;
using S1API.UI;
using UnityEngine;
using UnityEngine.UI;

namespace ModsApp.UI.Search;

public class SearchResultsView
{
    private readonly RectTransform _detailsContent;
    private readonly UITheme _theme;
    private readonly GameObject _actionButtonContainer;

    public event Action<MelonMod, MelonPreferences_Category, MelonPreferences_Entry> OnNavigateToEntry;

    public SearchResultsView(RectTransform detailsContent, UITheme theme, GameObject actionButtonContainer)
    {
        _detailsContent = detailsContent;
        _theme = theme;
        _actionButtonContainer = actionButtonContainer;
    }

    public void Show(IReadOnlyList<SearchResult> results, string searchQuery = "")
    {
        UIFactory.ClearChildren(_detailsContent);
        _actionButtonContainer?.SetActive(false);

        var headerCard = CreateInfoCard("AllModsSearchHeaderCard");

        var title = UIFactory.Text("AllModsSearchTitle", "All Mods - Settings Search", headerCard.transform,
            _theme.SizeLarge, TextAnchor.MiddleLeft, FontStyle.Bold);
        title.color = _theme.TextPrimary;

        var subtitle = UIFactory.Text("AllModsSearchSubtitle", "Click a result to navigate to it",
            headerCard.transform, _theme.SizeStandard);
        subtitle.color = _theme.TextSecondary;

        if (results.Count >= SearchManager.MaxSearchResults)
        {
            var warning = UIFactory.Text("TruncatedWarning",
                $"Showing first {SearchManager.MaxSearchResults} results...",
                headerCard.transform, _theme.SizeSmall);
            warning.color = _theme.WarningColor;
        }

        var grouped = results
            .GroupBy(r => (mod: r.Mod, category: r.Category))
            .OrderBy(g => g.Key.mod.Info.Name)
            .ThenBy(g => g.Key.category.DisplayName ?? g.Key.category.Identifier);

        var resultsCard = CreateInfoCard("AllModsSearchResultsCard");

        foreach (var group in grouped)
        {
            var mod = group.Key.mod;
            var category = group.Key.category;
            var categoryDisplay = category.DisplayName ?? category.Identifier;
            var modName = mod.Info.Name;
            var groupKey = $"{modName}.{category.Identifier}";

            var highlightedCategoryDisplay = HighlightMatch(categoryDisplay, searchQuery);
            var categoryRow = CreateSearchResultCategory(resultsCard, modName, highlightedCategoryDisplay, groupKey,
                mod, category);

            var entriesInGroup = group.Where(g => g.Entry != null);
            foreach (var entryResult in entriesInGroup)
            {
                var entry = entryResult.Entry;
                var entryDisplay = entry.DisplayName ?? entry.Identifier;
                var highlightedEntryDisplay = HighlightMatch(entryDisplay, searchQuery);
                CreateSearchResultEntry(categoryRow.transform, highlightedEntryDisplay, mod, category, entry,
                    searchQuery);
            }
        }

        UIHelper.RefreshLayout(_detailsContent);
    }

    private GameObject CreateInfoCard(string name, Transform parent = null)
    {
        var parentTransform = parent ?? _detailsContent;
        var card = UIFactory.Panel(name, parentTransform, _theme.BgSecondary);
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

    private string HighlightMatch(string text, string query)
    {
        if (string.IsNullOrEmpty(query) || string.IsNullOrEmpty(text)) return text;

        var index = text.IndexOf(query, StringComparison.OrdinalIgnoreCase);
        if (index < 0) return text;

        var hex = ColorUtility.ToHtmlStringRGB(_theme.AccentPrimary);
        return text.Insert(index + query.Length, "</color>")
            .Insert(index, $"<color=#{hex}>");
    }

    private GameObject CreateSearchResultCategory(GameObject parent, string modName, string categoryName,
        string groupKey, MelonMod mod = null, MelonPreferences_Category category = null)
    {
        var categoryPanel = UIFactory.Panel($"SearchResult_{groupKey}", parent.transform, _theme.BgCategory);
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

        var titleText = $"{modName} > {categoryName}";
        var title = UIFactory.Text($"SearchResult_{groupKey}_Title", titleText,
            categoryPanel.transform, _theme.SizeMedium, TextAnchor.UpperLeft, FontStyle.Bold);
        title.color = _theme.TextPrimary;
        title.supportRichText = true;

        if (mod != null && category != null)
        {
            var button = title.gameObject.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor = _theme.AccentPrimary;
            colors.highlightedColor = _theme.AccentPrimary;
            colors.pressedColor = _theme.AccentPrimary;
            colors.selectedColor = _theme.AccentPrimary;
            colors.fadeDuration = 0.1f;
            button.colors = colors;
            EventHelper.AddListener(() => OnNavigateToEntry?.Invoke(mod, category, null), button.onClick);
        }

        return categoryPanel;
    }

    private void CreateSearchResultEntry(Transform parent, string entryName, MelonMod mod,
        MelonPreferences_Category category, MelonPreferences_Entry entry, string searchQuery)
    {
        var entryRow = UIFactory.Panel($"SearchResultEntry_{category.Identifier}_{entry.Identifier}", parent,
            _theme.AccentSecondary);
        entryRow.GetComponent<Image>()?.MakeRounded(4, 16);

        var hLayout = entryRow.AddComponent<HorizontalLayoutGroup>();
        hLayout.spacing = 8;
        hLayout.padding = new RectOffset(8, 8, 4, 4);
        hLayout.childAlignment = TextAnchor.MiddleLeft;
        hLayout.childForceExpandWidth = false;
        hLayout.childForceExpandHeight = false;
        hLayout.childControlHeight = true;
        hLayout.childControlWidth = true;

        var entryLayoutElem = entryRow.GetOrAddComponent<LayoutElement>();
        entryLayoutElem.preferredHeight = 36;
        entryLayoutElem.flexibleWidth = 1;

        var entryLabel = UIFactory.Text($"EntryLabel_{entry.Identifier}", entryName,
            entryRow.transform, _theme.SizeStandard, TextAnchor.MiddleLeft);
        entryLabel.color = _theme.TextPrimary;
        entryLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
        entryLabel.verticalOverflow = VerticalWrapMode.Truncate;
        entryLabel.supportRichText = true;

        if (!string.IsNullOrEmpty(entry.Description))
        {
            var descToShow = entry.Description;
            if (!string.IsNullOrEmpty(searchQuery) &&
                entry.Description.IndexOf(searchQuery, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                descToShow = HighlightMatch(entry.Description, searchQuery);
            }

            var descText = UIFactory.Text($"EntryDesc_{entry.Identifier}", descToShow,
                entryRow.transform, _theme.SizeSmall, TextAnchor.UpperLeft);
            descText.color = _theme.TextSecondary;
            descText.horizontalOverflow = HorizontalWrapMode.Wrap;
            descText.supportRichText = true;
        }

        var button = entryRow.GetOrAddComponent<Button>();
        var colors = button.colors;
        colors.normalColor = Color.clear;
        colors.highlightedColor = _theme.AccentSecondary;
        colors.pressedColor = _theme.AccentPrimary;
        colors.selectedColor = _theme.AccentPrimary;
        colors.fadeDuration = 0.1f;
        button.colors = colors;
        EventHelper.AddListener(() => OnNavigateToEntry?.Invoke(mod, category, entry), button.onClick);
    }
}