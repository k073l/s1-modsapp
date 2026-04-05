using System;
using MelonLoader;
using ModsApp.UI.Themes;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace ModsApp.UI.Panels;

public class MissingDepsPanelComponent
{
    private static MissingDepsPanelComponent _instance;
    private GameObject _canvasObject;
    private GameObject _panelObject;
    private static readonly Dictionary<(int size, int radius), Sprite> _roundedCache = new();
    // TODO: We don't draw buttons on il2cpp (listeners are annoying) - but we should
    private static readonly bool IsIl2Cpp = MelonUtils.IsGameIl2Cpp();
    private readonly Slate _slate = new();
    private const float TopBarHeight = 65f;

    private static readonly List<ModsAppDependencyInfo> RequiredDependencies =
    [
        new()
        {
            DependencyName = "S1API",
            DependencyVersion = "3.0.1",
            DependencySuggestedUrls =
            [
                new DependencyUrl
                {
                    DisplayName = "Thunderstore", Url = "https://thunderstore.io/c/schedule-i/p/ifBars/S1API_Forked/"
                },
                new DependencyUrl { DisplayName = "NexusMods", Url = "https://www.nexusmods.com/schedule1/mods/1194" },
                new DependencyUrl { DisplayName = "Github", Url = "https://github.com/ifBars/S1API/releases/" }
            ]
        }
    ];

    private static List<ModsAppDependencyInfo> _missing;

    public static void CheckAndShow()
    {
        _missing = null;
        if (CheckDependenciesPresent()) return;

        _instance ??= new MissingDepsPanelComponent();
    }

    public static void Hide()
    {
        _instance?.Destroy();
        _instance = null;
    }

    private static bool CheckDependenciesPresent()
    {
        var loadedAssemblyNames = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a != null).Select(a => a.GetName().Name)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        return RequiredDependencies.All(dep => loadedAssemblyNames.Contains(dep.DependencyName));
    }

    private MissingDepsPanelComponent()
    {
        _missing ??= GetMissingDependencies();
        CreateCanvas();
        CreatePanel();
    }

    private void CreateCanvas()
    {
        _canvasObject = new GameObject("MissingDepsCanvas");
        var canvas = _canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999;

        var scaler = _canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        _canvasObject.AddComponent<GraphicRaycaster>();
    }

    private void CreatePanel()
    {
        _panelObject = new GameObject("MissingDepsPanel");
        _panelObject.transform.SetParent(_canvasObject.transform, false);

        var rect = _panelObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);
        rect.anchoredPosition = new Vector2(-20f, 20f);
        rect.sizeDelta = new Vector2(580f, 0f);

        var bg = _panelObject.AddComponent<Image>();
        bg.color = _slate.BgPrimary;
        bg.raycastTarget = false;
        bg.sprite = GetRoundedSprite(48, 12);
        bg.type = Image.Type.Sliced;

        var csf = _panelObject.AddComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var layout = _panelObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(12, 12, 12, 12);
        layout.spacing = 8f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        CreateTopBar();
        CreateContent();
        CreateFooter();
    }

    private void CreateTopBar()
    {
        var topBar = new GameObject("TopBar");
        topBar.transform.SetParent(_panelObject.transform, false);

        var rect = topBar.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;

        var le = topBar.AddComponent<LayoutElement>();
        le.preferredHeight = TopBarHeight;
        le.flexibleWidth = 1f;

        var bg = topBar.AddComponent<Image>();
        bg.color = _slate.BgPrimary;
        bg.raycastTarget = false;

        var titleObj = new GameObject("Title");
        titleObj.transform.SetParent(topBar.transform, false);
        var titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = Vector2.zero;
        titleRect.anchorMax = Vector2.one;
        titleRect.offsetMin = new Vector2(16f, 0f);
        titleRect.offsetMax = IsIl2Cpp ? Vector2.zero : new Vector2(-60f, 0f);

        var titleText = titleObj.AddComponent<Text>();
        titleText.text = "ModsApp - Missing Dependencies";
        titleText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        titleText.fontSize = 20;
        titleText.fontStyle = FontStyle.Bold;
        titleText.color = _slate.TextPrimary;
        titleText.alignment = TextAnchor.MiddleLeft;
        titleText.supportRichText = false;
        titleText.raycastTarget = false;

        if (!IsIl2Cpp)
            CreateCloseButton(topBar.transform);
    }

    private void CreateCloseButton(Transform parent)
    {
        var btnObj = new GameObject("CloseButton");
        btnObj.transform.SetParent(parent, false);

        var rect = btnObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 0.5f);
        rect.anchorMax = new Vector2(1f, 0.5f);
        rect.pivot = new Vector2(1f, 0.5f);
        rect.anchoredPosition = new Vector2(-12f, 0f);
        rect.sizeDelta = new Vector2(36f, 36f);

        var bg = btnObj.AddComponent<Image>();
        bg.color = _slate.WarningColor;
        bg.sprite = GetRoundedSprite(24, 6);
        bg.type = Image.Type.Sliced;

        var btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = bg;
        var colors = btn.colors;
        colors.normalColor = _slate.WarningColor;
        colors.highlightedColor = new Color(_slate.WarningColor.r + 0.1f, _slate.WarningColor.g + 0.1f,
            _slate.WarningColor.b + 0.1f);
        colors.pressedColor = _slate.WarningColor;
        colors.disabledColor = new Color(0.5f, 0.5f, 0.5f);
        colors.colorMultiplier = 1f;
        btn.colors = colors;

        var textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);
        var textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        var text = textObj.AddComponent<Text>();
        text.text = "X";
        text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.fontSize = 20;
        text.fontStyle = FontStyle.Bold;
        text.color = _slate.BgPrimary;
        text.alignment = TextAnchor.MiddleCenter;
        text.supportRichText = false;
        text.raycastTarget = false;

        AddButtonListener(Hide, btn);
    }

    private void CreateContent()
    {
        CreateWarningLabel();

        if (_missing != null)
        {
            foreach (var dep in _missing)
            {
                CreateDependencySection(dep);
            }
        }
    }

    private void CreateWarningLabel()
    {
        var labelObj = new GameObject("WarningLabel");
        labelObj.transform.SetParent(_panelObject.transform, false);

        var rect = labelObj.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;

        var le = labelObj.AddComponent<LayoutElement>();
        le.preferredHeight = 40f;
        le.flexibleWidth = 1f;

        var text = labelObj.AddComponent<Text>();
        text.text =
            "The following required assemblies could not be found.\nModsApp will not function until they are installed.";
        text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.fontSize = 17;
        text.color = _slate.TextSecondary;
        text.alignment = TextAnchor.UpperLeft;
        text.supportRichText = false;
        text.raycastTarget = false;
    }

    private void CreateDependencySection(ModsAppDependencyInfo dep)
    {
        var depLabelObj = new GameObject("DependencyLabel");
        depLabelObj.transform.SetParent(_panelObject.transform, false);

        var depRect = depLabelObj.AddComponent<RectTransform>();
        depRect.anchorMin = Vector2.zero;
        depRect.anchorMax = Vector2.one;
        depRect.sizeDelta = Vector2.zero;

        var depLe = depLabelObj.AddComponent<LayoutElement>();
        depLe.preferredHeight = 24f;
        depLe.flexibleWidth = 1f;

        var depText = depLabelObj.AddComponent<Text>();
        depText.text = $"  - {dep.DependencyName}, version >= {dep.DependencyVersion}";
        depText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        depText.fontSize = 17;
        depText.color = _slate.ErrorColor;
        depText.alignment = TextAnchor.UpperLeft;
        depText.supportRichText = false;
        depText.raycastTarget = false;

        var sourcesObj = new GameObject("SourcesLabel");
        sourcesObj.transform.SetParent(_panelObject.transform, false);

        var sourcesRect = sourcesObj.AddComponent<RectTransform>();
        sourcesRect.anchorMin = Vector2.zero;
        sourcesRect.anchorMax = Vector2.one;
        sourcesRect.sizeDelta = Vector2.zero;

        var sourcesLe = sourcesObj.AddComponent<LayoutElement>();
        sourcesLe.preferredHeight = 24f;
        sourcesLe.flexibleWidth = 1f;

        var sourcesText = sourcesObj.AddComponent<Text>();
        sourcesText.text = "     Suggested sources:";
        sourcesText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        sourcesText.fontSize = 17;
        sourcesText.color = _slate.TextSecondary;
        sourcesText.alignment = TextAnchor.UpperLeft;
        sourcesText.supportRichText = false;
        sourcesText.raycastTarget = false;

        foreach (var url in dep.DependencySuggestedUrls)
        {
            CreateLinkButton(url.DisplayName, url.Url);
        }
    }

    private void CreateLinkButton(string displayName, string url)
    {
        var btnObj = new GameObject(displayName);
        btnObj.transform.SetParent(_panelObject.transform, false);

        var rect = btnObj.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;

        var le = btnObj.AddComponent<LayoutElement>();
        le.preferredHeight = 24f;
        le.flexibleWidth = 1f;

        if (IsIl2Cpp)
        {
            var text = btnObj.AddComponent<Text>();
            text.text = $"       {displayName}: {url}";
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = 17;
            text.color = _slate.AccentPrimary;
            text.alignment = TextAnchor.UpperLeft;
            text.supportRichText = false;
            text.raycastTarget = false;
        }
        else
        {
            var btnBg = btnObj.AddComponent<Image>();
            btnBg.sprite = GetRoundedSprite(24, 6);
            btnBg.type = Image.Type.Sliced;

            var btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = btnBg;
            var colors = btn.colors;
            colors.normalColor = _slate.BgPrimary;
            colors.highlightedColor = _slate.BgCard;
            colors.pressedColor = _slate.BgCategory;
            colors.disabledColor = _slate.BgPrimary;
            colors.colorMultiplier = 1f;
            btn.colors = colors;

            var textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);
            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(24f, 2f);
            textRect.offsetMax = new Vector2(-16f, -2f);

            var text = textObj.AddComponent<Text>();
            text.text = displayName;
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = 17;
            text.color = _slate.AccentPrimary;
            text.alignment = TextAnchor.UpperLeft;
            text.supportRichText = false;
            text.raycastTarget = false;

            AddButtonListener(() => Application.OpenURL(url), btn);
        }
    }

    private void CreateFooter()
    {
        var installObj = new GameObject("InstallLabel");
        installObj.transform.SetParent(_panelObject.transform, false);

        var installRect = installObj.AddComponent<RectTransform>();
        installRect.anchorMin = Vector2.zero;
        installRect.anchorMax = Vector2.one;
        installRect.sizeDelta = Vector2.zero;

        var installLe = installObj.AddComponent<LayoutElement>();
        installLe.preferredHeight = 24f;
        installLe.flexibleWidth = 1f;

        var installText = installObj.AddComponent<Text>();
        installText.text = "Install the missing dependencies and restart the game.";
        installText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        installText.fontSize = 17;
        installText.color = _slate.TextSecondary;
        installText.alignment = TextAnchor.UpperLeft;
        installText.supportRichText = false;
        installText.raycastTarget = false;

        if (!IsIl2Cpp)
        {
            var btnObj = new GameObject("DismissButton");
            btnObj.transform.SetParent(_panelObject.transform, false);

            var btnRect = btnObj.AddComponent<RectTransform>();
            btnRect.anchorMin = Vector2.zero;
            btnRect.anchorMax = Vector2.one;
            btnRect.sizeDelta = Vector2.zero;

            var le = btnObj.AddComponent<LayoutElement>();
            le.preferredHeight = 28f;
            le.flexibleWidth = 0f;

            var bg = btnObj.AddComponent<Image>();
            bg.color = _slate.AccentPrimary;
            bg.sprite = GetRoundedSprite(24, 6);
            bg.type = Image.Type.Sliced;

            var btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = bg;
            var colors = btn.colors;
            colors.normalColor = _slate.AccentPrimary;
            colors.highlightedColor = new Color(_slate.AccentPrimary.r + 0.1f, _slate.AccentPrimary.g + 0.1f,
                _slate.AccentPrimary.b + 0.1f);
            colors.pressedColor = _slate.AccentPrimary;
            colors.disabledColor = new Color(0.5f, 0.5f, 0.5f);
            colors.colorMultiplier = 1f;
            btn.colors = colors;

            var textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);
            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            var text = textObj.AddComponent<Text>();
            text.text = "Dismiss";
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = 17;
            text.color = _slate.TextPrimary;
            text.alignment = TextAnchor.MiddleCenter;
            text.supportRichText = false;
            text.raycastTarget = false;

            AddButtonListener(Hide, btn);
        }
    }

    private void AddButtonListener(Action listener, Button button)
    {
        try
        {
            var uAction = new UnityAction(listener);
            button.onClick.AddListener(uAction);
        }
        catch (Exception)
        {
        }
    }

    private static Sprite GetRoundedSprite(int size = 32, int radius = 8)
    {
        var key = (size, radius);
        if (_roundedCache.TryGetValue(key, out var cached) && cached != null)
            return cached;

        var texture = new Texture2D(size, size, TextureFormat.ARGB32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        float r = radius;
        const float aa = 1.0f;

        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var dx = Mathf.Min(x + 0.5f, size - x - 0.5f);
                var dy = Mathf.Min(y + 0.5f, size - y - 0.5f);

                var alpha = 1f;

                if (dx < r && dy < r)
                {
                    var dist = Mathf.Sqrt((dx - r) * (dx - r) + (dy - r) * (dy - r));
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

    private static List<ModsAppDependencyInfo> GetMissingDependencies()
    {
        var loadedAssemblyNames = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a != null).Select(a => a.GetName().Name)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        return RequiredDependencies.Where(dep => !loadedAssemblyNames.Contains(dep.DependencyName)).ToList();
    }

    public void Destroy()
    {
        if (_panelObject != null)
        {
            Object.Destroy(_panelObject);
            _panelObject = null;
        }

        if (_canvasObject != null)
        {
            Object.Destroy(_canvasObject);
            _canvasObject = null;
        }
    }

    private class ModsAppDependencyInfo
    {
        public string DependencyName { get; set; }
        public string DependencyVersion { get; set; }
        public List<DependencyUrl> DependencySuggestedUrls { get; set; } = [];
    }

    private class DependencyUrl
    {
        public string DisplayName { get; set; }
        public string Url { get; set; }
    }
}