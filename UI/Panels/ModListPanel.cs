using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using S1API.UI;
using MelonLoader;
using ModsApp.Helpers;
using ModsApp.Managers;

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

        _listContent = UIFactory.ScrollableVerticalList("ModListContent", leftPanel.transform, out _);
        UIHelper.ForceRectToAnchors(_listContent, Vector2.zero, Vector2.one,
            Vector2.zero, Vector2.zero, new Vector2(0.5f, 1f));
        UIHelper.SetupLayoutGroup(_listContent.gameObject, 4, false, new RectOffset(8, 8, 8, 8));

        UIHelper.DumpRect("ModListPanel", leftPanel.GetComponent<RectTransform>());
        UIHelper.DumpRect("ModListContent", _listContent);
    }

    public void PopulateList()
    {
        if (_listContent == null) return;

        UIFactory.ClearChildren(_listContent);
        _modButtons.Clear();

        foreach (var mod in _modManager.GetAllMods())
        {
            CreateModButton(mod);
        }

        UIHelper.RefreshLayout(_listContent);
    }

    private void CreateModButton(MelonMod mod)
    {
        var buttonGo = UIFactory.Panel($"{UIHelper.SanitizeName(mod.Info.Name)}_Button", _listContent,
            _theme.AccentSecondary);

        var button = buttonGo.GetOrAddComponent<Button>();
        UIHelper.SetupButton(button, _theme, () => SelectMod(mod));
        UIHelper.ConfigureButtonLayout(buttonGo.GetComponent<RectTransform>(), 48f);

        // Main label
        var label = UIFactory.Text($"{UIHelper.SanitizeName(mod.Info.Name)}_Label", mod.Info.Name, buttonGo.transform,
            14, TextAnchor.MiddleLeft);
        label.color = _theme.TextPrimary;
        UIHelper.ConfigureButtonText(label.rectTransform, new Vector2(0f, 0f), new Vector2(0.7f, 1f), 16f, -8f, 4f,
            -4f);

        // Version label
        var versionText = UIFactory.Text($"{UIHelper.SanitizeName(mod.Info.Name)}_Version", $"v{mod.Info.Version}",
            buttonGo.transform, 10, TextAnchor.MiddleRight);
        versionText.color = _theme.TextSecondary;
        UIHelper.ConfigureButtonText(versionText.rectTransform, new Vector2(0.7f, 0f), new Vector2(1f, 1f), 4f, -12f,
            4f, -4f);

        _modButtons[mod.Info.Name] = buttonGo;
        _modLabels[mod.Info.Name] = label;
    }

    private void SelectMod(MelonMod mod)
    {
        _selectedModName = mod.Info.Name;
        UpdateButtonHighlights();
        OnModSelected?.Invoke(mod);
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