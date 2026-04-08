using System.Text.RegularExpressions;
using MelonLoader;
using MelonLoader.Utils;
using ModsApp.Managers;
using ModsApp.UI;
using Newtonsoft.Json;
using S1API.Internal.Abstraction;
using Semver;
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

        if (_roundedCache.TryGetValue(key, out var cached) && cached != null)
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

        sprite.name = $"Rounded_{size}_{radius}";
        _roundedCache[key] = sprite;
        return sprite;
    }
    
    public static (GameObject, Button, Image) RoundedButtonWithIcon(
        string name,
        Sprite icon,
        Transform parent,
        Color bgColor,
        float width,
        float height,
        float size = 12f)
    {
        var maskGO = new GameObject(name + "_RoundedMask");
        maskGO.transform.SetParent(parent, false);
        var maskRect = maskGO.AddComponent<RectTransform>();
        maskRect.sizeDelta = new Vector2(width, height);
        var layout = maskGO.AddComponent<LayoutElement>();
        layout.preferredWidth = width;
        layout.preferredHeight = height;

        var maskImage = maskGO.AddComponent<Image>();
        maskImage.sprite = GetRoundedSprite();
        maskImage.type = Image.Type.Sliced;

        maskGO.AddComponent<Mask>().showMaskGraphic = true;

        var buttonGO = new GameObject(name);
        buttonGO.transform.SetParent(maskGO.transform, false);
        var buttonRect = buttonGO.AddComponent<RectTransform>();
        buttonRect.anchorMin = Vector2.zero;
        buttonRect.anchorMax = Vector2.one;
        buttonRect.offsetMin = Vector2.zero;
        buttonRect.offsetMax = Vector2.zero;

        var buttonImage = buttonGO.AddComponent<Image>();
        buttonImage.sprite = GetRoundedSprite();
        buttonImage.type = Image.Type.Sliced;
        buttonImage.color = bgColor;

        var button = buttonGO.AddComponent<Button>();
        button.targetGraphic = buttonImage;

        var iconGO = new GameObject("Icon");
        iconGO.transform.SetParent(buttonGO.transform, false);
        var iconRect = iconGO.AddComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.5f, 0.5f);
        iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.sizeDelta = new Vector2(size, size);
        iconRect.anchoredPosition = Vector2.zero;

        var iconImage = iconGO.AddComponent<Image>();
        iconImage.raycastTarget = false;
        iconImage.sprite = icon;
        iconImage.preserveAspect = true;

        return (maskGO, button, iconImage);
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
    
    public static Image AddIcon(
        Sprite sprite,
        Transform parent,
        Vector2 anchor,
        Vector2 anchoredPosition,
        float size = 12f)
    {
        var gameObject = new GameObject("Icon");
        gameObject.transform.SetParent(parent, false);

        var rectTransform = gameObject.AddComponent<RectTransform>();

        rectTransform.anchorMin = anchor;
        rectTransform.anchorMax = anchor;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);

        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = new Vector2(size, size);

        var image = gameObject.AddComponent<Image>();
        image.type = Image.Type.Simple;
        image.sprite = sprite;
        image.preserveAspect = true;

        return image;
    }
}

public static class GameObjectExtensions
{
    public static T GetOrAddComponent<T>(this GameObject go) where T : Component
    {
        return go.GetComponent<T>() ?? go.AddComponent<T>();
    }
}

public static class TransformExtensions
{
    public static Transform FindInHierarchy(this Transform startTransform, string path)
    {
        var currentTransform = startTransform;
        var pathParts = path.Split('/');

        foreach (var part in pathParts)
        {
            currentTransform = currentTransform.FindChildWithNameRecursive(part);
            if (currentTransform == null) return null;
        }

        return currentTransform;
    }

    public static Transform FindChildWithNameRecursive(this Transform parent, string name)
    {
        for (var i = 0; i < parent.childCount; i++)
        {
            var child = parent.GetChild(i);
            if (child.name == name) return child;
            var result = child.FindChildWithNameRecursive(name);
            if (result != null) return result;
        }

        return null;
    }
}

public static class CategoryState
{
    private static Dictionary<MelonPreferences_Category, bool> _expandedCategories = new();
    private static HashSet<string> _explicitlySet = new();
    private static HashSet<MelonPreferences_Category> _tempExpanded = new();
    private static string _savePath = Path.Combine(MelonEnvironment.UserDataDirectory, "ModsApp", "CategoryState.json");

    public static bool IsExpanded(MelonPreferences_Category category)
    {
        if (_tempExpanded.Contains(category)) return true;
        if (!_expandedCategories.TryGetValue(category, out var isExpanded))
        {
            isExpanded = true;
            _expandedCategories[category] = isExpanded;
        }

        return isExpanded;
    }

    public static void Expand(MelonPreferences_Category category, bool isUserIntent = true)
    {
        if (isUserIntent)
        {
            _expandedCategories[category] = true;
            _explicitlySet.Add(category.Identifier);
        }
        else
        {
            _tempExpanded.Add(category);
        }
    }

    public static bool IsTempExpanded(MelonPreferences_Category category) => _tempExpanded.Contains(category);

    public static void ClearTempExpanded() => _tempExpanded.Clear();

    public static bool HasExplicitState(MelonPreferences_Category category) =>
        _explicitlySet.Contains(category.Identifier);

    public static void SetDefault(MelonPreferences_Category category, bool expanded) =>
        _expandedCategories[category] = expanded;

    public static void Toggle(MelonPreferences_Category category)
    {
        _expandedCategories[category] = !IsExpanded(category);
        _explicitlySet.Add(category.Identifier);
        _tempExpanded.Remove(category);
    }

    public static void Load()
    {
        if (!File.Exists(_savePath)) return;
        try
        {
            var json = File.ReadAllText(_savePath);
            var dto = JsonConvert.DeserializeObject<Dictionary<string, bool>>(json);
            if (dto == null)
            {
                _expandedCategories = new();
                _explicitlySet = new();
                return;
            }

            _expandedCategories = dto
                .Select(kv => new
                {
                    Category = MelonPreferences.Categories
                        .FirstOrDefault(cat => cat.Identifier == kv.Key),
                    kv.Value
                })
                .Where(x => x.Category != null)
                .ToDictionary(x => x.Category!, x => x.Value);

            // saved data is user intent
            _explicitlySet = new HashSet<string>(dto.Keys);
        }
        catch
        {
            _expandedCategories = new();
            _explicitlySet = new();
        }
    }

    public static void Save()
    {
        // only save explicitly user-set states, not auto-collapse defaults
        var dto = _expandedCategories
            .Where(kv => _explicitlySet.Contains(kv.Key.Identifier))
            .ToDictionary(kv => kv.Key.Identifier, kv => kv.Value);
        var json = JsonConvert.SerializeObject(dto, Formatting.Indented);
        Directory.CreateDirectory(Path.GetDirectoryName(_savePath) ?? string.Empty);
        File.WriteAllText(_savePath, json);
    }
}

public static class ModVersionTracker
{
    private static Dictionary<MelonMod, string> _versions = new();
    private static Dictionary<MelonMod, string> _thisSession = new();
    private static Dictionary<string, string> _unmatched = new();

    private static string _savePath =
        Path.Combine(MelonEnvironment.UserDataDirectory, "ModsApp", "VersionTracker.json");

    public static bool IsNew(MelonMod mod)
    {
        if (mod?.Info?.Name == null) return false;
        _thisSession.TryAdd(mod, mod.Info.Version);
        return !_versions.ContainsKey(mod) || _versions[mod] == null;
    }

    public static bool IsUpdated(MelonMod mod)
    {
        if (mod?.Info?.Name == null) return false;
        _thisSession.TryAdd(mod, mod.Info.Version);
        return _versions.TryGetValue(mod, out var saved)
               && saved != null
               && IsNewerVersion(mod.Info.Version, saved);
    }

    public static bool AreAnyUpdatedOrNew() =>
        MelonMod.RegisteredMelons.Any(m => IsNew(m) || IsUpdated(m));

    private static bool IsNewerVersion(string current, string saved)
    {
        if (SemVersion.TryParse(current, out var c) &&
            SemVersion.TryParse(saved, out var s))
            return c > s;
        return current != saved;
    }

    public static void Load()
    {
        var fallback = () => _versions = MelonMod.RegisteredMelons
            .Where(m => m?.Info?.Name != null)
            .ToDictionary(m => m, m => m.Info.Version);
 
        if (!File.Exists(_savePath)) { fallback(); return; }
 
        try
        {
            var json = File.ReadAllText(_savePath);
            var dto  = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
            if (dto == null) { fallback(); return; }
 
            _versions = dto
                .Select(kv => new
                {
                    Mod = MelonMod.RegisteredMelons.FirstOrDefault(m => m.Info.Name == kv.Key),
                    kv.Value
                })
                .Where(x => x.Mod != null)
                .ToDictionary(x => x.Mod!, x => x.Value);
 
            // Capture entries that didn't match any loaded mod
            // These are either disabled mods or deleted mods - we'll filter on Save()
            _unmatched = dto
                .Where(kv => MelonMod.RegisteredMelons.All(m => m.Info.Name != kv.Key))
                .ToDictionary(kv => kv.Key, kv => kv.Value);
        }
        catch { fallback(); }
    }

    public static void Save()
    {
        ModFolderScanner.Scan(out _, out var inactiveMods, out _);
        var inactiveNames = inactiveMods
            .Select(m => m.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
 
        var merged = _versions.ToDictionary(kv => kv.Key.Info.Name, kv => kv.Value);
        foreach (var kv in _thisSession)
            merged[kv.Key.Info.Name] = kv.Value;
 
        // preserve unmatched entries ONLY if they correspond to a currently disabled mod
        foreach (var kv in _unmatched)
            if (inactiveNames.Contains(kv.Key))
                merged.TryAdd(kv.Key, kv.Value);
 
        var json = JsonConvert.SerializeObject(merged, Formatting.Indented);
        Directory.CreateDirectory(Path.GetDirectoryName(_savePath) ?? string.Empty);
        File.WriteAllText(_savePath, json);
    }
}
