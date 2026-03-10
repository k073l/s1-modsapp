using System.Collections;
using System.Reflection;
using MelonLoader;
using ModsApp.Helpers;
using ModsApp.Managers;
using ModsApp.UI;
using ModsApp.UI.Input.Handlers;
using ModsApp.UI.Panels;
using ModsApp.UI.Themes;
using S1API.Input;
using UnityEngine;
using S1API.PhoneApp;
using S1API.Utils;

[assembly: MelonInfo(
    typeof(ModsApp.ModsApp),
    ModsApp.BuildInfo.Name,
    ModsApp.BuildInfo.Version,
    ModsApp.BuildInfo.Author
)]
[assembly: MelonColor(1, 255, 0, 0)]
[assembly: MelonGame("TVGS", "Schedule I")]
[assembly: MelonPriority(int.MinValue + 100)] // Ensure this runs before most other mods to capture logs, but after S1API

namespace ModsApp;

public static class BuildInfo
{
    public const string Name = "ModsApp";
    public const string Description = "In-game app to manage mods' preferences";
    public const string Author = "k073l";
    public const string Version = "1.2.1";
}

public class ModsApp : MelonMod
{
    private static MelonLogger.Instance Logger;

    public static Sprite AppIconSprite => GetIcon(ref _appIconSprite, "ModsApp.assets.appicon.png");
    public static Sprite WarningIconSprite => GetIcon(ref _warningIconSprite, "ModsApp.assets.triangle-alert.png");
    public static Sprite ScrollIconSprite => GetIcon(ref _scrollIconSprite, "ModsApp.assets.scroll-text.png");

    private static Sprite _appIconSprite;
    private static Sprite _warningIconSprite;
    private static Sprite _scrollIconSprite;

    public static MelonPreferences_Category AccessibilityCategory;
    public static MelonPreferences_Entry<TextSizeProfile> TextSizeProfileEntry;

    public static MelonPreferences_Category ThemesCategory;
    public static MelonPreferences_Entry<ThemeOption> ThemeOptionEntry;
    public static MelonPreferences_Entry<Color> BackgroundPrimaryEntry;
    public static MelonPreferences_Entry<Color> BackgroundSecondaryEntry;
    public static MelonPreferences_Entry<Color> BackgroundCardEntry;
    public static MelonPreferences_Entry<Color> BackgroundCategoryEntry;
    public static MelonPreferences_Entry<Color> BackgroundInputEntry;
    public static MelonPreferences_Entry<Color> AccentPrimaryEntry;
    public static MelonPreferences_Entry<Color> AccentSecondaryEntry;
    public static MelonPreferences_Entry<Color> TextPrimaryEntry;
    public static MelonPreferences_Entry<Color> TextSecondaryEntry;
    public static MelonPreferences_Entry<Color> InputPrimaryEntry;
    public static MelonPreferences_Entry<Color> InputSecondaryEntry;
    public static MelonPreferences_Entry<Color> SuccessColorEntry;
    public static MelonPreferences_Entry<Color> WarningColorEntry;
    public static MelonPreferences_Entry<Color> ErrorColorEntry;

    public override void OnInitializeMelon()
    {
        LogManager.Instance.WireEvents();

        Logger = LoggerInstance;
        Logger.Msg("ModsApp initialized");
        // prefetch
        _ = AppIconSprite;
        _ = WarningIconSprite;
        _ = ScrollIconSprite;

        AccessibilityCategory = MelonPreferences.CreateCategory("ModsApp_Accessibility", "Accessibility");
        TextSizeProfileEntry = AccessibilityCategory.CreateEntry("ModsAppTextSize", TextSizeProfile.Normal,
            "Text Size Setting", description: "Text size preset");

        ThemesCategory = MelonPreferences.CreateCategory("ModsApp_Themes", "Themes");
        ThemeOptionEntry = ThemesCategory.CreateEntry("ModsAppThemeOption", ThemeOption.Slate,
            "Theme Preset",
            description:
            "Predefined theme presets. Set to Custom to use custom colors defined below - Custom may require game restart to apply fully, but changing presets should apply immediately");
        var slate = new Slate();
        BackgroundPrimaryEntry = ThemesCategory.CreateEntry("ModsAppBackgroundPrimary", slate.BgPrimary,
            "Background Primary",
            description: "Primary background color. Set Theme Preset to Custom to use this color");
        BackgroundSecondaryEntry = ThemesCategory.CreateEntry("ModsAppBackgroundSecondary", slate.BgSecondary,
            "Background Secondary",
            description: "Secondary background color. Set Theme Preset to Custom to use this color");
        BackgroundCardEntry = ThemesCategory.CreateEntry("ModsAppBackgroundCard", slate.BgCard, "Background Card",
            description: "Card background color. Set Theme Preset to Custom to use this color");
        BackgroundCategoryEntry = ThemesCategory.CreateEntry("ModsAppBackgroundCategory", slate.BgCategory,
            "Background Category",
            description: "Category background color. Set Theme Preset to Custom to use this color");
        BackgroundInputEntry = ThemesCategory.CreateEntry("ModsAppBackgroundInput", slate.BgInput, "Background Input",
            description: "Input background color. Set Theme Preset to Custom to use this color");
        AccentPrimaryEntry = ThemesCategory.CreateEntry("ModsAppAccentPrimary", slate.AccentPrimary, "Accent Primary",
            description: "Primary accent color. Set Theme Preset to Custom to use this color");
        AccentSecondaryEntry = ThemesCategory.CreateEntry("ModsAppAccentSecondary", slate.AccentSecondary,
            "Accent Secondary", description: "Secondary accent color. Set Theme Preset to Custom to use this color");
        TextPrimaryEntry = ThemesCategory.CreateEntry("ModsAppTextPrimary", slate.TextPrimary, "Text Primary",
            description: "Primary text color. Set Theme Preset to Custom to use this color");
        TextSecondaryEntry = ThemesCategory.CreateEntry("ModsAppTextSecondary", slate.TextSecondary, "Text Secondary",
            description: "Secondary text color. Set Theme Preset to Custom to use this color");
        InputPrimaryEntry = ThemesCategory.CreateEntry("ModsAppInputPrimary", slate.InputPrimary, "Input Primary",
            description: "Primary input color. Set Theme Preset to Custom to use this color");
        InputSecondaryEntry = ThemesCategory.CreateEntry("ModsAppInputSecondary", slate.InputSecondary,
            "Input Secondary", description: "Secondary input color. Set Theme Preset to Custom to use this color");
        SuccessColorEntry = ThemesCategory.CreateEntry("ModsAppSuccessColor", slate.SuccessColor, "Success Color",
            description: "Color used to indicate success. Set Theme Preset to Custom to use this color");
        WarningColorEntry = ThemesCategory.CreateEntry("ModsAppWarningColor", slate.WarningColor, "Warning Color",
            description: "Color used to indicate warnings. Set Theme Preset to Custom to use this color");
        ErrorColorEntry = ThemesCategory.CreateEntry("ModsAppErrorColor", slate.ErrorColor, "Error Color",
            description: "Color used to indicate errors. Set Theme Preset to Custom to use this color");

        CategoryState.Load();
        ModVersionTracker.Load();
    }

    private static Sprite LoadEmbeddedPNG(string resourceName)
    {
        Assembly asm = Assembly.GetExecutingAssembly();

        using Stream stream = asm.GetManifestResourceStream(resourceName);
        if (stream == null) return null;

        var data = new byte[stream.Length];
        stream.Read(data, 0, data.Length);
        var sprite = ImageUtils.LoadImageRaw(data);
        if (sprite != null) sprite.name = resourceName;
        return sprite;
    }

    private static Sprite GetIcon(ref Sprite spriteField, string resourceName)
    {
        if (spriteField == null)
        {
            spriteField = LoadEmbeddedPNG(resourceName);
        }

        return spriteField;
    }

    public override void OnUpdate()
    {
        // Failsafe to exit typing mode when Escape is pressed
        if (App.Instance == null || !App.Instance.IsOpen()) return;
        if (UnityEngine.Input.GetKeyDown(KeyCode.Escape))
        {
            FloatingPanelComponent.Cleanup();
            Controls.IsTyping = false;
        }
    }

    public override void OnApplicationQuit()
    {
        CategoryState.Save();
        ModVersionTracker.Save();
    }
}