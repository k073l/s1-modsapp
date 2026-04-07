using System.Text;
using MelonLoader;
using ModsApp.Helpers;
using ModsApp.Managers;
using ModsApp.UI.Input.FieldFactories;
using S1API.UI;
using UnityEngine;
using UnityEngine.UI;

namespace ModsApp.UI.Panels;

public class LogExplorerPanel
{
    private readonly UITheme _theme;
    private readonly MelonMod _mod;

    private bool _modOnly;
    private bool _issuesOnly;

    private ScrollableTextFactory _scrollable;

    private readonly string _accentSecondaryHex;
    private readonly string _accentPrimaryHex;
    private readonly string _errorHex;
    private readonly string _warningHex;
    private readonly string _inputHex;

    public LogExplorerPanel(MelonMod mod)
    {
        _theme = UIManager._theme;
        _mod = mod;
        _modOnly = mod != null;
        _issuesOnly = true;

        _accentSecondaryHex = ColorUtility.ToHtmlStringRGB(_theme.AccentSecondary);
        _accentPrimaryHex = ColorUtility.ToHtmlStringRGB(_theme.AccentPrimary);
        _errorHex = ColorUtility.ToHtmlStringRGB(_theme.ErrorColor);
        _warningHex = ColorUtility.ToHtmlStringRGB(_theme.WarningColor);
        _inputHex = ColorUtility.ToHtmlStringRGB(_theme.InputPrimary);

        var title = mod != null
            ? $"{mod.Info.Name} - Log Explorer"
            : "Log Explorer - All Mods";

        var panel = new FloatingPanelComponent(1100, 600, title);

        BuildFilterBar(panel.ContentPanel.transform);
        BuildLogView(panel.ContentPanel.transform);
    }

    private void BuildFilterBar(Transform parent)
    {
        var filterBar = UIFactory.Panel("LogFilterBar", parent, Color.clear);

        var rect = filterBar.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(1, 1);
        rect.pivot = new Vector2(0.5f, 1);
        rect.sizeDelta = new Vector2(0, 36);
        rect.anchoredPosition = Vector2.zero;

        var layout = filterBar.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 16;
        layout.padding = new RectOffset(8, 8, 6, 6);
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.childControlWidth = false;
        layout.childControlHeight = true;

        if (_mod != null)
        {
            ToggleFactory.CreateSlidingWithLabel(
                filterBar.transform,
                "ModOnlyToggle",
                "Selected mod only",
                _modOnly,
                _theme.SuccessColor,
                _theme.BgInput,
                _theme.BgInput,
                _theme.BgPrimary,
                val =>
                {
                    _modOnly = val;
                    RefreshText();
                }
            );

            CreateSeparator(filterBar.transform);
        }

        var group = filterBar.AddComponent<ToggleGroup>();

        var issuesToggle = ToggleFactory.CreateSlidingWithLabel(
            filterBar.transform,
            "IssuesOnlyToggle",
            "Warns & Errors",
            _issuesOnly,
            _theme.WarningColor,
            _theme.BgInput,
            _theme.BgInput,
            _theme.BgPrimary,
            val =>
            {
                if (!val) return;
                _issuesOnly = true;
                RefreshText();
            }
        );
        CreateSpacer(filterBar.transform);

        var allLogsToggle = ToggleFactory.CreateSlidingWithLabel(
            filterBar.transform,
            "AllLogsToggle",
            "All messages",
            !_issuesOnly,
            _theme.AccentPrimary,
            _theme.BgInput,
            _theme.BgInput,
            _theme.BgPrimary,
            val =>
            {
                if (!val) return;
                _issuesOnly = false;
                RefreshText();
            }
        );

        issuesToggle.group = group;
        allLogsToggle.group = group;

        CreateSpacer(filterBar.transform);
    }

    private void CreateSeparator(Transform parent)
    {
        var sep = UIFactory.Text("FilterSep", "|", parent,
            _theme.SizeSmall, TextAnchor.MiddleCenter);

        sep.color = _theme.TextSecondary;
        sep.gameObject.GetOrAddComponent<LayoutElement>().preferredWidth = 12;
    }

    private void CreateSpacer(Transform parent)
    {
        var spacer = new GameObject("FilterSpacer");
        spacer.transform.SetParent(parent, false);
        spacer.AddComponent<LayoutElement>().flexibleWidth = 1;
    }

    private void BuildLogView(Transform parent)
    {
        var container = new GameObject("LogContainer");
        container.transform.SetParent(parent, false);

        var rect = container.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 0);
        rect.anchorMax = new Vector2(1, 1);
        rect.offsetMin = new Vector2(6, 6);
        rect.offsetMax = new Vector2(-6, -42);

        _scrollable = new ScrollableTextFactory(
            container.transform,
            BuildText(),
            _theme.SizeStandard,
            _theme.TextPrimary,
            _theme.BgInput
        );

        var rootRect = _scrollable.Root.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(_scrollable.ContentRect);
    }

    private void RefreshText()
    {
        _scrollable.SetText(BuildText());
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(_scrollable.ContentRect);
    }

    private string BuildText()
    {
        var entries = GetFilteredEntries().ToList();
        if (entries.Count == 0)
            return BuildEmptyText();

        var sb = new StringBuilder();
        foreach (var entry in entries)
        {
            sb.Append($"<color=#{_accentSecondaryHex}>[{entry.Time:HH:mm:ss}]</color> ");

            if (_mod == null || !_modOnly)
            {
                var modLabel = entry.ModName ?? entry.Section;
                sb.Append($"<color=#{_accentPrimaryHex}>[{modLabel}]</color> ");
            }

            sb.Append(entry.Level switch
            {
                LogLevel.Warning => $"<color=#{_warningHex}>[WARN]</color> ",
                LogLevel.Error => $"<color=#{_errorHex}>[ERR]</color> ",
                _ => $"<color=#{_accentSecondaryHex}>[MSG]</color> "
            });

            sb.AppendLine($"<color=#{_inputHex}>{entry.Message}</color>");
        }

        return sb.ToString().TrimEnd();
    }

    private string BuildEmptyText()
    {
        if (_mod != null && _modOnly)
            return _issuesOnly
                ? $"<color=#{_inputHex}>No warnings or errors recorded for {_mod.Info.Name}.</color>"
                : $"<color=#{_inputHex}>No log entries recorded for {_mod.Info.Name}.</color>";

        return _issuesOnly
            ? $"<color=#{_inputHex}>No warnings or errors recorded.</color>"
            : $"<color=#{_inputHex}>No log entries recorded.</color>";
    }

    private IEnumerable<LogEntry> GetFilteredEntries()
    {
        var source = (_mod != null && _modOnly)
            ? LogManager.Instance.GetLogsForMod(_mod.Info.Name)
            : LogManager.Instance.GetAllLogs();

        if (_issuesOnly)
            source = source.Where(e =>
                e.Level is LogLevel.Warning or LogLevel.Error);

        return source;
    }
}