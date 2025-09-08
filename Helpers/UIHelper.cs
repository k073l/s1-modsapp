using System;
using System.Text.RegularExpressions;
using MelonLoader;
using S1API.Internal.Abstraction;
using UnityEngine;
using UnityEngine.UI;

namespace ModsApp.Helpers;

public static class UIHelper
{
    public static string SanitizeName(string input)
    {
        return string.IsNullOrEmpty(input) ? "Unknown" : Regex.Replace(input, @"[^\w\d_]", "_");
    }

    public static void DumpRect(string id, RectTransform rt)
    {
        if (rt == null)
        {
            MelonDebug.Msg($"{id}: NULL");
            return;
        }

        MelonDebug.Msg(
            $"{id}: anchorMin={rt.anchorMin}, anchorMax={rt.anchorMax}, offsetMin={rt.offsetMin}, offsetMax={rt.offsetMax}, pivot={rt.pivot}, sizeDelta={rt.sizeDelta}, localPos={rt.localPosition}, siblingIndex={rt.GetSiblingIndex()}");
    }

    public static void ForceRectToAnchors(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax,
        Vector2? offsetMin = null, Vector2? offsetMax = null, Vector2? pivot = null)
    {
        if (rt == null) return;
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot ?? new Vector2(0.5f, 0.5f);
        rt.offsetMin = offsetMin ?? Vector2.zero;
        rt.offsetMax = offsetMax ?? Vector2.zero;
    }

    public static void SetupLayoutGroup(GameObject go, int spacing, bool fitContent, RectOffset padding = null)
    {
        if (go == null) return;

        var vlg = go.GetOrAddComponent<VerticalLayoutGroup>();
        vlg.spacing = spacing;
        vlg.childControlHeight = false;
        vlg.childControlWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childAlignment = TextAnchor.UpperLeft;
        vlg.padding = padding ?? new RectOffset(8, 8, 8, 8);

        if (fitContent)
        {
            var csf = go.GetOrAddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        }
    }

    public static void SetupButton(Button button, UITheme theme, Action onClick)
    {
        var colors = button.colors;
        colors.normalColor = theme.AccentSecondary;
        colors.highlightedColor = new Color(theme.AccentSecondary.r + 0.1f, theme.AccentSecondary.g + 0.1f,
            theme.AccentSecondary.b + 0.1f, 1f);
        colors.pressedColor = theme.AccentPrimary;
        colors.selectedColor = theme.AccentPrimary;
        colors.fadeDuration = 0.15f;
        button.colors = colors;

        EventHelper.AddListener(onClick, button.onClick);
    }

    public static void ConfigureButtonLayout(RectTransform rt, float height)
    {
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(0f, height);

        var layoutElement = rt.gameObject.GetOrAddComponent<LayoutElement>();
        layoutElement.preferredHeight = height;
        layoutElement.minHeight = height;
        layoutElement.flexibleHeight = 0f;
    }

    public static void ConfigureButtonText(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, float leftOffset,
        float rightOffset, float topOffset, float bottomOffset)
    {
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = new Vector2(leftOffset, bottomOffset);
        rt.offsetMax = new Vector2(rightOffset, topOffset);
    }

    public static void AddBorderEffect(GameObject panel, Color accentColor)
    {
        var borderPanel = S1API.UI.UIFactory.Panel($"{panel.name}_Border", panel.transform.parent,
            new Color(accentColor.r, accentColor.g, accentColor.b, 0.2f));

        var borderRt = borderPanel.GetComponent<RectTransform>();
        var panelRt = panel.GetComponent<RectTransform>();

        if (borderRt != null && panelRt != null)
        {
            borderPanel.transform.SetSiblingIndex(panel.transform.GetSiblingIndex());
            borderRt.anchorMin = panelRt.anchorMin;
            borderRt.anchorMax = panelRt.anchorMax;
            borderRt.offsetMin = panelRt.offsetMin - Vector2.one * 2f;
            borderRt.offsetMax = panelRt.offsetMax + Vector2.one * 2f;
        }
    }

    public static void RefreshLayout(RectTransform content)
    {
        try
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
            S1API.UI.UIFactory.FitContentHeight(content);
        }
        catch (Exception ex)
        {
            // Silently continue if layout refresh fails
        }
    }
}

public static class GameObjectExtensions
{
    public static T GetOrAddComponent<T>(this GameObject go) where T : Component
    {
        return go.GetComponent<T>() ?? go.AddComponent<T>();
    }
}