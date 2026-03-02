using System;
using System.Text.RegularExpressions;
using MelonLoader;
using S1API.Internal.Abstraction;
using UnityEngine;
using UnityEngine.UI;

namespace ModsApp.Helpers;

public static class UIHelper
{
    private static readonly Dictionary<(int size, int radius), Sprite> _roundedCache
        = new();

    public static Sprite GetRoundedSprite(int size = 32, int radius = 8)
    {
        var key = (size, radius);

        if (_roundedCache.TryGetValue(key, out var cached))
            return cached;

        var texture = new Texture2D(size, size, TextureFormat.ARGB32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        float r = radius;
        const float aa = 1.0f; // 1px anti-alias width

        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                // Distance from nearest edge
                var dx = Mathf.Min(x + 0.5f, size - x - 0.5f);
                var dy = Mathf.Min(y + 0.5f, size - y - 0.5f);

                var alpha = 1f;

                if (dx < r && dy < r)
                {
                    var dist = Mathf.Sqrt((dx - r) * (dx - r) + (dy - r) * (dy - r));

                    // Smooth edge
                    alpha = Mathf.Clamp01((r - dist) / aa);
                }

                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply(false, true);

        var border = new Vector4(radius, radius, radius, radius);

        var sprite = Sprite.Create(
            texture,
            new Rect(0, 0, size, size),
            new Vector2(0.5f, 0.5f),
            100f,
            0,
            SpriteMeshType.FullRect,
            border);

        _roundedCache[key] = sprite;
        return sprite;
    }

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

        var parentImage = panel.GetComponent<Image>();
        var borderImage = borderPanel.GetComponent<Image>();
        if (parentImage == null || borderImage == null) return;
        borderImage.sprite = parentImage.sprite;
        borderImage.type = parentImage.type;
    }

    public static Image MakeRounded(this Image image, int radius = 8, int size = 32)
    {
        image.sprite = GetRoundedSprite(size, radius);
        image.type = Image.Type.Sliced;
        return image;
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