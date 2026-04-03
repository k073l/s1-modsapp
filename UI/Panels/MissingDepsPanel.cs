using ModsApp.UI.Themes;
using UnityEngine;

namespace ModsApp.UI.Panels;

public static class MissingDepsPanel
{
    private static bool _show;
    private static List<ModsAppDependencyInfo>? _missing;
    private static Rect _windowRect;
    private static GUISkin _skin;

    private static readonly Slate _slate = new();
    private const float Padding = 20f;
    private const float TopBarHeight = 65f;

    // define explicitly, so we can add more info about the dep
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
                    DisplayName = "Thunderstore",
                    Url = "https://thunderstore.io/c/schedule-i/p/ifBars/S1API_Forked/"
                },

                new DependencyUrl
                {
                    DisplayName = "NexusMods",
                    Url = "https://www.nexusmods.com/schedule1/mods/1194"
                },

                new DependencyUrl
                {
                    DisplayName = "Github",
                    Url = "https://github.com/ifBars/S1API/releases/"
                }
            ]
        }
    ];

    public static void CheckAndShow()
    {
        _missing = null;
        if (CheckDependenciesPresent()) return;

        const float w = 580f, h = 360f;
        _windowRect = new Rect(Screen.width - w - Padding, Screen.height - h - Padding, w, h);
        _show = true;
    }

    public static void Hide() => _show = false;

    private static bool CheckDependenciesPresent()
    {
        var loadedAssemblyNames = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a != null).Select(a => a.GetName().Name)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        return RequiredDependencies.All(dep => loadedAssemblyNames.Contains(dep.DependencyName));
    }

    public static void OnGUI()
    {
        if (!_show) return;

        _missing ??= GetMissingDependencies();
        if (_missing?.Count == 0) return;

        BuildSkinIfNeeded();
        GUI.Box(_windowRect, string.Empty, _skin.window);
        DrawWindowContents();
        GUI.DragWindow(new Rect(_windowRect.x, _windowRect.y, _windowRect.width, TopBarHeight));
    }

    private static List<ModsAppDependencyInfo> GetMissingDependencies()
    {
        var loadedAssemblyNames = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a != null).Select(a => a.GetName().Name)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var missing = RequiredDependencies.Where(dep => !loadedAssemblyNames.Contains(dep.DependencyName)).ToList();
        return missing;
    }

    private static void DrawWindowContents()
    {
        var old = GUI.skin;
        GUI.skin = _skin;

        var w = _windowRect.width;
        var h = _windowRect.height;

        // Title
        const float titleTopPad = 6f;
        GUI.Label(new Rect(_windowRect.x + 16, _windowRect.y + titleTopPad, w - 80, TopBarHeight - titleTopPad * 2), "ModsApp - Missing Dependencies",
            _skin.GetStyle("Title"));

        // Close button
        const float closeBtnSize = 36f;
        var closeBtnRect = new Rect(_windowRect.x + w - closeBtnSize - 12, _windowRect.y + (TopBarHeight - closeBtnSize) / 2f, closeBtnSize,
            closeBtnSize);
        if (GUI.Button(closeBtnRect, "X", _skin.GetStyle("CloseButton")))
            _show = false;

        // Body
        const float bodyX = 16f;
        const float bodyY = TopBarHeight + 16f;
        var bodyW = w - bodyX * 2;

        var linkY = bodyY + 48f;
        foreach (var dep in _missing)
        {
            GUI.Label(new Rect(_windowRect.x + bodyX, _windowRect.y + bodyY, bodyW, 40),
                "The following required assemblies could not be found.\nModsApp will not function until they are installed.",
                _skin.GetStyle("Body"));

            GUI.Label(new Rect(_windowRect.x + bodyX, _windowRect.y + linkY, bodyW, 24),
                $"  - {dep.DependencyName}, version >= {dep.DependencyVersion}", _skin.GetStyle("Missing"));

            linkY += 28f;
            GUI.Label(new Rect(_windowRect.x + bodyX, _windowRect.y + linkY, bodyW, 24), "     Suggested sources:", _skin.GetStyle("Body"));

            linkY += 28f;
            foreach (var url in dep.DependencySuggestedUrls)
            {
                var linkStyle = _skin.GetStyle("Link");
                var linkSize = linkStyle.CalcSize(new GUIContent(url.DisplayName));
                var linkRect = new Rect(_windowRect.x + bodyX + 24f, _windowRect.y + linkY, linkSize.x + 16f, linkSize.y + 4f);
                if (GUI.Button(linkRect, url.DisplayName, linkStyle))
                    Application.OpenURL(url.Url);
                linkY += 28f;
            }

            linkY += 12f;
        }

        GUI.Label(new Rect(_windowRect.x + bodyX, _windowRect.y + h - 80f, bodyW, 24),
            "Install the missing dependencies and restart the game.", _skin.GetStyle("Body"));

        // Footer
        var footerY = h - 44;
        const float btnWidth = 100f;
        const float btnHeight = 28f;
        var btnX = (w - btnWidth) / 2f;
        if (GUI.Button(new Rect(_windowRect.x + btnX, _windowRect.y + footerY, btnWidth, btnHeight), "Dismiss", _skin.button))
            _show = false;

        GUI.skin = old;
    }

    private static void BuildSkinIfNeeded()
    {
        if (_skin != null) return;
        _skin = ScriptableObject.CreateInstance<GUISkin>();

        // Window
        _skin.window.normal.background = MakeTex(2, 2, _slate.BgPrimary);
        _skin.window.padding = new RectOffset(12, 12, (int)TopBarHeight + 8, 12);
        _skin.window.border = new RectOffset(0, 0, 0, 0);

        // Button
        _skin.button.normal.background = MakeTex(2, 2, _slate.AccentPrimary);
        _skin.button.hover.background = MakeTex(2, 2,
            new Color(_slate.AccentPrimary.r + 0.1f, _slate.AccentPrimary.g + 0.1f, _slate.AccentPrimary.b + 0.1f));
        _skin.button.normal.textColor = _slate.TextPrimary;
        _skin.button.hover.textColor = _slate.TextPrimary;
        _skin.button.fontSize = 17;
        _skin.button.alignment = TextAnchor.MiddleCenter;
        _skin.button.border = new RectOffset(4, 4, 4, 4);

        // Label
        _skin.label.normal.textColor = _slate.TextPrimary;
        _skin.label.fontSize = 17;
        _skin.label.wordWrap = true;

        // Custom styles
        var title = new GUIStyle(_skin.label)
        {
            fontSize = 20,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft,
            normal =
            {
                textColor = _slate.TextPrimary
            },
            name = "Title"
        };

        var body = new GUIStyle(_skin.label)
        {
            fontSize = 17,
            normal =
            {
                textColor = _slate.TextSecondary
            },
            name = "Body"
        };

        var missing = new GUIStyle(_skin.label)
        {
            fontSize = 17,
            normal =
            {
                textColor = _slate.ErrorColor
            },
            name = "Missing"
        };

        var closeBtn = new GUIStyle(_skin.button)
        {
            fontSize = 20,
            fontStyle = FontStyle.Bold,
            name = "CloseButton",
            normal =
            {
                background = MakeTex(2, 2, _slate.WarningColor),
                textColor = _slate.BgPrimary
            },
            hover =
            {
                background = MakeTex(2, 2,
                    new Color(_slate.WarningColor.r + 0.1f, _slate.WarningColor.g + 0.1f,
                        _slate.WarningColor.b + 0.1f)),
                textColor = _slate.BgPrimary
            },
            alignment = TextAnchor.MiddleCenter,
            border = new RectOffset(4, 4, 4, 4)
        };

        var link = new GUIStyle(_skin.label)
        {
            fontSize = 17,
            name = "Link",
            normal =
            {
                textColor = _slate.AccentPrimary
            },
            hover =
            {
                textColor = new Color(_slate.AccentPrimary.r + 0.2f, _slate.AccentPrimary.g + 0.2f,
                    _slate.AccentPrimary.b + 0.2f)
            },
            alignment = TextAnchor.UpperLeft,
            stretchWidth = false
        };

        _skin.customStyles = [title, body, missing, closeBtn, link];
    }

    private static Texture2D MakeTex(int w, int h, Color col)
    {
        var tex = new Texture2D(w, h);
        var pixels = new Color[w * h];
        for (var i = 0; i < pixels.Length; i++) pixels[i] = col;
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
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