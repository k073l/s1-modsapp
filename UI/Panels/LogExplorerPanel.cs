using MelonLoader;
using ModsApp.Helpers;
using ModsApp.Managers;
using ModsApp.UI.Input.FieldFactories;
using S1API.UI;
using System.Text;
using S1API.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace ModsApp.UI.Panels;

public class LogExplorerPanel
{
    private readonly UITheme _theme;
    private readonly MelonMod _mod; // null = all mods

    private bool _modOnly;
    private bool _issuesOnly; // true = warnings+errors only, false = all

    private ScrollableTextFactory _scrollable;
    
    private string _accentSecondaryHex;
    private string _accentPrimaryHex;
    private string _errorHex;
    private string _warningHex;
    private string _inputHex;

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
            ? $"{mod.Info.Name} — Log Explorer"
            : "Log Explorer — All Mods";

        var panel = new FloatingPanelComponent(1100, 600, title);

        BuildFilterBar(panel.ContentPanel.transform);
        BuildLogView(panel.ContentPanel.transform);
    }

    private void BuildFilterBar(Transform parent)
    {
        var filterBar = UIFactory.Panel("LogFilterBar", parent, Color.clear);
        var filterRect = filterBar.GetComponent<RectTransform>();
        filterRect.anchorMin = new Vector2(0, 1);
        filterRect.anchorMax = new Vector2(1, 1);
        filterRect.pivot = new Vector2(0.5f, 1);
        filterRect.sizeDelta = new Vector2(0, 36);
        filterRect.anchoredPosition = Vector2.zero;

        var hLayout = filterBar.AddComponent<HorizontalLayoutGroup>();
        hLayout.spacing = 16;
        hLayout.padding = new RectOffset(8, 8, 6, 6);
        hLayout.childAlignment = TextAnchor.MiddleLeft;
        hLayout.childForceExpandWidth = false;
        hLayout.childForceExpandHeight = false;
        hLayout.childControlWidth = false;
        hLayout.childControlHeight = true;
        
        if (_mod != null)
        {
            var modOnlyToggle = CreateLabelledToggle(
                filterBar.transform,
                "ModOnlyToggle",
                "Selected mod only",
                _modOnly
            );
            ToggleUtils.AddListener(modOnlyToggle, val =>
            {
                _modOnly = val;
                RefreshText();
            });

            var sep = UIFactory.Text("FilterSep", "|", filterBar.transform,
                _theme.SizeSmall, TextAnchor.MiddleCenter);
            sep.color = _theme.TextSecondary;
            sep.gameObject.GetOrAddComponent<LayoutElement>().preferredWidth = 12;
        }

        // radio pair
        Toggle issuesToggle = null;
        Toggle allLogsToggle = null;

        issuesToggle = CreateLabelledToggle(
            filterBar.transform,
            "IssuesOnlyToggle",
            "Warns & Errors",
            _issuesOnly
        );

        allLogsToggle = CreateLabelledToggle(
            filterBar.transform,
            "AllLogsToggle",
            "All messages",
            !_issuesOnly
        );

        ToggleUtils.AddListener(issuesToggle, val =>
        {
            if (!val) return; // only act on the one being turned on
            _issuesOnly = true;
            if (allLogsToggle.isOn) allLogsToggle.isOn = false;
            RefreshText();
        });

        ToggleUtils.AddListener(allLogsToggle, val =>
        {
            if (!val) return;
            _issuesOnly = false;
            if (issuesToggle.isOn) issuesToggle.isOn = false;
            RefreshText();
        });

        var spacer = new GameObject("FilterSpacer");
        spacer.transform.SetParent(filterBar.transform, false);
        spacer.AddComponent<LayoutElement>().flexibleWidth = 1;
    }

    private void BuildLogView(Transform parent)
    {
        var logContainer = new GameObject("LogContainer");
        logContainer.transform.SetParent(parent, false);
        var logRect = logContainer.AddComponent<RectTransform>();
        logRect.anchorMin = new Vector2(0, 0);
        logRect.anchorMax = new Vector2(1, 1);
        logRect.offsetMin = new Vector2(6, 6);
        logRect.offsetMax = new Vector2(-6, -42);

        _scrollable = new ScrollableTextFactory(
            logContainer.transform,
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

            switch (entry.Level)
            {
                case LogLevel.Warning:
                    sb.Append($"<color=#{_warningHex}>[WARN]</color> ");
                    break;
                case LogLevel.Error:
                    sb.Append($"<color=#{_errorHex}>[ERR]</color> ");
                    break;
                case LogLevel.Msg:
                    sb.Append($"<color=#{_accentSecondaryHex}>[MSG]</color> ");
                    break;
            }

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
                e.Level == LogLevel.Warning || e.Level == LogLevel.Error);

        return source;
    }

    private Toggle CreateLabelledToggle(Transform parent, string name, string labelText, bool initialValue)
    {
        var container = new GameObject(name);
        container.transform.SetParent(parent, false);

        var hLayout = container.AddComponent<HorizontalLayoutGroup>();
        hLayout.spacing = 6;
        hLayout.childAlignment = TextAnchor.MiddleLeft;
        hLayout.childForceExpandWidth = false;
        hLayout.childForceExpandHeight = false;
        hLayout.childControlWidth = false;
        hLayout.childControlHeight = true;

        container.AddComponent<LayoutElement>().preferredHeight = 24;

        var toggleObj = new GameObject($"{name}_Box");
        toggleObj.transform.SetParent(container.transform, false);

        var bg = toggleObj.AddComponent<Image>();
        bg.color = _theme.BgInput;

        var toggle = toggleObj.AddComponent<Toggle>();
        toggle.targetGraphic = bg;

        var rt = toggleObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(20, 20);

        var toggleLayout = toggleObj.AddComponent<LayoutElement>();
        toggleLayout.minWidth = 20;
        toggleLayout.minHeight = 20;
        toggleLayout.preferredWidth = 20;
        toggleLayout.preferredHeight = 20;

        var checkmarkGO = new GameObject("Checkmark");
        checkmarkGO.transform.SetParent(toggleObj.transform, false);
        var checkmarkImg = checkmarkGO.AddComponent<Image>();
        checkmarkImg.color = _theme.BgCard;

        var crt = checkmarkGO.GetComponent<RectTransform>();
        crt.anchorMin = new Vector2(0.2f, 0.2f);
        crt.anchorMax = new Vector2(0.8f, 0.8f);
        crt.offsetMin = crt.offsetMax = Vector2.zero;

        ToggleUtils.SetGraphic(toggle, checkmarkImg);
        toggle.isOn = initialValue;

        var label = UIFactory.Text($"{name}_Label", labelText, container.transform,
            _theme.SizeSmall, TextAnchor.MiddleLeft);
        label.color = _theme.TextPrimary;
        label.gameObject.AddComponent<LayoutElement>().preferredWidth = label.preferredWidth;

        return toggle;
    }
}