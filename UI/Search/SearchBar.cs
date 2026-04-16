using ModsApp.Helpers;
using ModsApp.UI;
using ModsApp.UI.Input.FieldFactories;
using S1API.Input;
using S1API.Internal.Abstraction;
using S1API.UI;
using UnityEngine;
using UnityEngine.UI;

namespace ModsApp.UI.Search;

public class SearchBar
{
    private readonly SearchManager _searchManager;
    private readonly UITheme _theme;
    private readonly Action<string> _onSearchChanged;
    private readonly Action<IReadOnlyList<SearchResult>, string> _onAllModsSelected;

    private InputField _searchInput;
    private Button _clearButton;

    public string Query { get; private set; } = string.Empty;
    public IReadOnlyList<SearchResult> Results { get; private set; } = new List<SearchResult>();
    public bool IsAllModsSelected { get; private set; }

    public SearchBar(SearchManager searchManager, UITheme theme, Action<string> onSearchChanged,
        Action<IReadOnlyList<SearchResult>, string> onAllModsSelected)
    {
        _searchManager = searchManager;
        _theme = theme;
        _onSearchChanged = onSearchChanged;
        _onAllModsSelected = onAllModsSelected;
    }

    public void Initialize(GameObject parent)
    {
        var searchPanel = UIFactory.Panel("SearchPanel", parent.transform, _theme.BgCard);
        searchPanel.GetComponent<Image>()?.MakeRounded(4, 16);
        UIHelper.ForceRectToAnchors(searchPanel.GetComponent<RectTransform>(),
            new Vector2(0f, 0.85f), new Vector2(1f, 0.95f),
            Vector2.zero, Vector2.zero);

        var searchLayout = searchPanel.GetComponent<HorizontalLayoutGroup>();
        if (searchLayout == null) searchLayout = searchPanel.AddComponent<HorizontalLayoutGroup>();
        searchLayout.spacing = 4;
        searchLayout.padding = new RectOffset(8, 8, 0, 0);
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
            _searchInput.gameObject.GetComponent<Image>()?.MakeRounded(4, 16);
            var inputLayoutElem = _searchInput.GetComponent<LayoutElement>();
            if (inputLayoutElem == null) inputLayoutElem = _searchInput.gameObject.AddComponent<LayoutElement>();
            inputLayoutElem.preferredHeight = 28;
            inputLayoutElem.flexibleHeight = 0;
            inputLayoutElem.flexibleWidth = 1;

            ProcessTextInChildren(_searchInput.transform, text =>
            {
                if (text.gameObject.name.Contains("Placeholder"))
                    text.text = "Search mods...";
            });

            EventHelper.AddListener<string>(text =>
            {
                Controls.IsTyping = true;
                OnSearchTextChanged(text);
            }, _searchInput.onValueChanged);

            EventHelper.AddListener<string>(_ => { Controls.IsTyping = false; }, _searchInput.onEndEdit);
        }

        var (_, clearButton, _) = UIFactory.RoundedButtonWithLabel(
            "SearchClear", "x", searchPanel.transform,
            _theme.TextPrimary * new Color(0, 0, 0, 0.2f),
            30, 30, _theme.SizeStandard, _theme.WarningColor);
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

            Tooltip.Attach(clearButton.gameObject, "Clear search", maxWidth: _theme.SizeSmall * 8f);
        }
    }

    private void OnSearchTextChanged(string text)
    {
        Query = text;
        CategoryState.ClearTempExpanded();

        if (_clearButton != null)
            _clearButton.gameObject.SetActive(!string.IsNullOrWhiteSpace(text));

        if (!string.IsNullOrWhiteSpace(text))
            Results = _searchManager.Search(text);
        else
        {
            Results = new List<SearchResult>();
            IsAllModsSelected = false;
        }

        _onSearchChanged?.Invoke(Query);

        if (IsAllModsSelected)
        {
            _onAllModsSelected?.Invoke(Results, text);
        }
    }

    private void OnSearchClear()
    {
        if (_searchInput != null)
            _searchInput.text = string.Empty;
        Query = string.Empty;
        Results = new List<SearchResult>();
        IsAllModsSelected = false;
        CategoryState.ClearTempExpanded();
        _onSearchChanged?.Invoke(Query);
    }

    private static void ProcessTextInChildren(Transform root, Action<Text> action)
    {
        if (root == null) return;
        if (root.TryGetComponent<Text>(out var text)) action(text);
        for (var i = 0; i < root.childCount; i++)
            ProcessTextInChildren(root.GetChild(i), action);
    }
}