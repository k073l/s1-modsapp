using System.Collections.Generic;
using System.Linq;
using MelonLoader;
using ModsApp.Helpers;
using ModsApp.Managers;
using ModsApp.UI.Input.FieldFactories;
using ModsApp.UI.Panels;
using S1API.Internal.Abstraction;
using S1API.UI;
using S1API.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace ModsApp.UI;

public static class ModToggleUI
{
    private static ModManager _modManager;

    public static void Initialize(ModManager modManager) => _modManager = modManager;

    public static Toggle CreateToggleForActive(MelonMod mod, Transform parent, UITheme theme,
        Action onToggled = null)
    {
        var dllPath = mod.MelonAssembly?.Assembly?.Location;
        if (string.IsNullOrEmpty(dllPath)) return null;
        if (ModToggleManager.IsProtected(dllPath)) return null;
        return CreateModToggle(dllPath, parent, theme, onToggled);
    }

    public static Toggle CreateToggleForInactive(InactiveModInfo mod, Transform parent, UITheme theme,
        Action onToggled = null)
    {
        if (ModToggleManager.IsProtected(mod.ActivePath)) return null;
        return CreateModToggle(mod.ActivePath, parent, theme, onToggled);
    }

    private static Toggle CreateModToggle(string dllPath, Transform parent, UITheme theme,
        Action onToggled = null)
    {
        var (toggle, refreshColors) = ToggleFactory.CreateModToggleSlider(
            parent, "ModToggle",
            ModToggleManager.GetDesiredState(dllPath),
            ModToggleManager.HasPendingChange(dllPath));

        ToggleUtils.AddListener(toggle, val =>
        {
            HandleToggleClick(dllPath, val, toggle, refreshColors, theme);
            onToggled?.Invoke();
        });

        return toggle;
    }

    private static void HandleToggleClick(
        string dllPath,
        bool newVal,
        Toggle toggle,
        Action<bool, bool> refreshColors,
        UITheme theme)
    {
        var shiftHeld = UnityEngine.Input.GetKey(KeyCode.LeftShift) ||
                        UnityEngine.Input.GetKey(KeyCode.RightShift);

        if (!newVal)
        {
            var dependants = _modManager?.GetDependants(dllPath) ?? new List<MelonMod>();

            if (dependants.Count > 0 && !shiftHeld)
            {
                toggle.SetIsOnWithoutNotify(true);
                refreshColors(true, ModToggleManager.HasPendingChange(dllPath));
                ShowDependencyWarningPanel(dllPath, dependants, toggle, refreshColors, theme);
                return;
            }

            ModToggleManager.RequestDisable(dllPath);

            if (dependants.Count > 0 && shiftHeld)
            {
                // shift: only this mod, dependants stay
            }
            else
            {
                foreach (var dep in dependants)
                {
                    var depPath = dep.MelonAssembly?.Assembly?.Location;
                    if (!string.IsNullOrEmpty(depPath))
                        ModToggleManager.RequestDisable(depPath);
                }
            }
        }
        else
        {
            ModToggleManager.RequestEnable(dllPath);
        }

        refreshColors(newVal, ModToggleManager.HasPendingChange(dllPath));
        UIManager.ModListPanel?.UpdateButtonHighlights();
    }

    private static void ShowDependencyWarningPanel(
        string dllPath,
        List<MelonMod> dependants,
        Toggle sourceToggle,
        Action<bool, bool> refreshColors,
        UITheme theme)
    {
        var panel = new FloatingPanelComponent(480, 300, "Dependency Warning");
        var content = panel.ContentPanel.transform;

        var vlg = panel.ContentPanel.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 8;
        vlg.padding = new RectOffset(12, 12, 10, 10);
        vlg.childControlWidth = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlHeight = true;

        var warn = UIFactory.Text("WarnText",
            $"The following mods depend on this one and may not work if it's disabled:\n\n{string.Join(", ", dependants.Select(d => d.Info.Name))}",
            content, theme.SizeStandard, TextAnchor.UpperLeft);
        warn.color = theme.TextPrimary;
        warn.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        warn.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        warn.gameObject.GetOrAddComponent<LayoutElement>().flexibleWidth = 1;

        var hint = UIFactory.Text("HintText",
            "Hold Shift when clicking the toggle to disable only this mod (dependants stay enabled at your own risk).",
            content, theme.SizeSmall, TextAnchor.UpperLeft, FontStyle.Italic);
        hint.color = theme.TextSecondary;
        hint.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        hint.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        var spacer = new GameObject("Spacer");
        spacer.transform.SetParent(content, false);
        spacer.AddComponent<LayoutElement>().flexibleHeight = 1;

        var btnRow = new GameObject("BtnRow");
        btnRow.transform.SetParent(content, false);
        var btnHLG = btnRow.AddComponent<HorizontalLayoutGroup>();
        btnHLG.spacing = 8;
        btnHLG.childAlignment = TextAnchor.MiddleCenter;
        btnHLG.childForceExpandWidth = false;
        btnHLG.childForceExpandHeight = false;
        btnHLG.childControlWidth = true;
        btnHLG.childControlHeight = true;
        btnRow.AddComponent<LayoutElement>().preferredHeight = 30;

        var (_, disableAllBtn, _) = UIFactory.RoundedButtonWithLabel(
            "DisableAllBtn", "Disable All", btnRow.transform,
            theme.ErrorColor, 120, 28, theme.SizeSmall, theme.TextPrimary);

        EventHelper.AddListener(() =>
        {
            ModToggleManager.RequestDisable(dllPath);
            foreach (var dep in dependants)
            {
                var depPath = dep.MelonAssembly?.Assembly?.Location;
                if (!string.IsNullOrEmpty(depPath))
                    ModToggleManager.RequestDisable(depPath);
            }

            sourceToggle.SetIsOnWithoutNotify(false);
            refreshColors(false, ModToggleManager.HasPendingChange(dllPath));
            UIManager.ModListPanel?.UpdateButtonHighlights();
            FloatingPanelComponent.Cleanup();
        }, disableAllBtn.onClick);

        var (_, cancelBtn, _) = UIFactory.RoundedButtonWithLabel(
            "CancelBtn", "Cancel", btnRow.transform,
            theme.AccentSecondary, 80, 28, theme.SizeSmall, theme.TextPrimary);

        var empty = "";
        EventHelper.AddListener(() =>
        {
            empty = empty;
            FloatingPanelComponent.Cleanup();
        }, cancelBtn.onClick);
    }
}