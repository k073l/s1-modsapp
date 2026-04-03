using MelonLoader;
using MelonLoader.Preferences;
using ModsApp.Helpers;
using ModsApp.Managers;
using ModsApp.UI;
using ModsApp.UI.Panels;
using ModsApp.UI.Themes;
using UnityEngine;

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
    public const string Version = "1.2.2";
}

public class ModsApp : MelonMod
{
    private static MelonLogger.Instance Logger;

    public static Sprite AppIconSprite => InitHelper.GetIcon(ref _appIconSprite, "ModsApp.assets.appicon.png");
    public static Sprite WarningIconSprite => InitHelper.GetIcon(ref _warningIconSprite, "ModsApp.assets.triangle-alert.png");
    public static Sprite ScrollIconSprite => InitHelper.GetIcon(ref _scrollIconSprite, "ModsApp.assets.scroll-text.png");

    private static Sprite _appIconSprite;
    private static Sprite _warningIconSprite;
    private static Sprite _scrollIconSprite;

    public static MelonPreferences_Category AccessibilityCategory;
    public static MelonPreferences_Entry<TextSizeProfile> TextSizeProfileEntry;
    public static MelonPreferences_Entry<bool> InputsOnRightEntry;
    public static MelonPreferences_Entry<int> EntryVlgSpacingEntry;

    public static MelonPreferences_Entry<bool> UseNewJsonEditor;
    
    public static MelonPreferences_Category ThemesCategory;
    public static MelonPreferences_Entry<ThemeOption> ThemeOptionEntry;
    public static MelonPreferences_Entry<bool> CopyCurrentToCustom;
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
    public static MelonPreferences_Entry<Color> JsonKeyColor;
    public static MelonPreferences_Entry<Color> JsonStringColor;
    public static MelonPreferences_Entry<Color> JsonNumberColor;
    public static MelonPreferences_Entry<Color> JsonLiteralColor;
    public static MelonPreferences_Entry<Color> JsonBracketColor;
    public static MelonPreferences_Entry<Color> JsonPunctuationColor;

    private bool _shouldUpdate = true;

    public override void OnInitializeMelon()
    {
        LogManager.Instance.WireEvents();

        Logger = LoggerInstance;
        Logger.Msg("ModsApp initialized");
        // prefetch
        try
        {
            _ = AppIconSprite;
            _ = WarningIconSprite;
            _ = ScrollIconSprite;
        }
        catch (Exception e)
        {
            // ignored
        }

        AccessibilityCategory = MelonPreferences.CreateCategory("ModsApp_Accessibility", "Accessibility");
        TextSizeProfileEntry = AccessibilityCategory.CreateEntry("ModsAppTextSize", TextSizeProfile.Normal,
            "Text Size Setting", description: "Text size preset");
        InputsOnRightEntry = AccessibilityCategory.CreateEntry("ModsAppRightAlignedInputs", true,
            "Right Aligned Inputs",
            description:
            "If true, entry inputs such as checkboxes, dropdowns, number fields etc. will be aligned to the right side");
        EntryVlgSpacingEntry = AccessibilityCategory.CreateEntry("ModsAppMelonEntryVLGSpacing", 4,
            "Entry Spacing", description: "Controls the spacing between each entry line",
            validator: new ValueRange<int>(1, 20));

        UseNewJsonEditor = AccessibilityCategory.CreateEntry("ModsAppNewJSONEditor", true,
            "Use new JSON editor", description: "Determines if ModsApp should use TMP JSON editor. Set to false if you have any issues (switches to legacy editor)");
        
        ThemesCategory = MelonPreferences.CreateCategory("ModsApp_Themes", "Themes");
        ThemeOptionEntry = ThemesCategory.CreateEntry("ModsAppThemeOption", ThemeOption.Slate,
            "Theme Preset",
            description:
            "Predefined theme presets. Set to Custom to use custom colors defined below - Custom may require game restart to apply fully, but changing presets should apply immediately");
        CopyCurrentToCustom = ThemesCategory.CreateEntry("ModsAppCopyToCustom", false,
            "Copy Theme to Custom", description: "Copy the colors of current theme to custom theme's colors");
        var slate = new Slate();
        BackgroundPrimaryEntry = ThemesCategory.CreateEntry("ModsAppBackgroundPrimary", slate.BgPrimary, "Background Primary", description: "Primary background color. Set Theme Preset to Custom to use this color");
        BackgroundSecondaryEntry = ThemesCategory.CreateEntry("ModsAppBackgroundSecondary", slate.BgSecondary, "Background Secondary", description: "Secondary background color. Set Theme Preset to Custom to use this color");
        BackgroundCardEntry = ThemesCategory.CreateEntry("ModsAppBackgroundCard", slate.BgCard, "Background Card", description: "Card background color. Set Theme Preset to Custom to use this color");
        BackgroundCategoryEntry = ThemesCategory.CreateEntry("ModsAppBackgroundCategory", slate.BgCategory, "Background Category", description: "Category background color. Set Theme Preset to Custom to use this color");
        BackgroundInputEntry = ThemesCategory.CreateEntry("ModsAppBackgroundInput", slate.BgInput, "Background Input", description: "Input background color. Set Theme Preset to Custom to use this color");
        AccentPrimaryEntry = ThemesCategory.CreateEntry("ModsAppAccentPrimary", slate.AccentPrimary, "Accent Primary", description: "Primary accent color. Set Theme Preset to Custom to use this color");
        AccentSecondaryEntry = ThemesCategory.CreateEntry("ModsAppAccentSecondary", slate.AccentSecondary, "Accent Secondary", description: "Secondary accent color. Set Theme Preset to Custom to use this color");
        TextPrimaryEntry = ThemesCategory.CreateEntry("ModsAppTextPrimary", slate.TextPrimary, "Text Primary", description: "Primary text color. Set Theme Preset to Custom to use this color");
        TextSecondaryEntry = ThemesCategory.CreateEntry("ModsAppTextSecondary", slate.TextSecondary, "Text Secondary", description: "Secondary text color. Set Theme Preset to Custom to use this color");
        InputPrimaryEntry = ThemesCategory.CreateEntry("ModsAppInputPrimary", slate.InputPrimary, "Input Primary", description: "Primary input color. Set Theme Preset to Custom to use this color");
        InputSecondaryEntry = ThemesCategory.CreateEntry("ModsAppInputSecondary", slate.InputSecondary, "Input Secondary", description: "Secondary input color. Set Theme Preset to Custom to use this color");
        SuccessColorEntry = ThemesCategory.CreateEntry("ModsAppSuccessColor", slate.SuccessColor, "Success Color", description: "Color used to indicate success. Set Theme Preset to Custom to use this color");
        WarningColorEntry = ThemesCategory.CreateEntry("ModsAppWarningColor", slate.WarningColor, "Warning Color", description: "Color used to indicate warnings. Set Theme Preset to Custom to use this color");
        ErrorColorEntry = ThemesCategory.CreateEntry("ModsAppErrorColor", slate.ErrorColor, "Error Color", description: "Color used to indicate errors. Set Theme Preset to Custom to use this color");
        JsonKeyColor = ThemesCategory.CreateEntry("ModsAppJsonKeyColor", slate.JsonKeyColor, "JSON Editor Key Color", description: "Color used for keys in the JSON editor (the property names before colons).");
        JsonStringColor = ThemesCategory.CreateEntry("ModsAppJsonStringColor", slate.JsonStringColor, "JSON Editor String Color", description: "Color used for string values in the JSON editor.");
        JsonNumberColor = ThemesCategory.CreateEntry( "ModsAppJsonNumberColor", slate.JsonNumberColor, "JSON Editor Number Color", description: "Color used for numeric values in the JSON editor.");
        JsonLiteralColor = ThemesCategory.CreateEntry( "ModsAppJsonLiteralColor", slate.JsonLiteralColor, "JSON Editor Literal Color", description: "Color used for boolean and null values (true, false, null) in the JSON editor.");
        JsonBracketColor = ThemesCategory.CreateEntry( "ModsAppJsonBracketColor", slate.JsonBracketColor, "JSON Editor Bracket Color", description: "Color used for brackets in JSON ({, }, [, ]) in the editor.");
        JsonPunctuationColor = ThemesCategory.CreateEntry( "ModsAppJsonPunctuationColor", slate.JsonPunctuationColor, "JSON Editor Punctuation Color", description: "Color used for punctuation characters (colon ':' and comma ',') in the JSON editor.");

        CategoryState.Load();
        ReflectionHelper.TryInitTMP();
        ModVersionTracker.Load();
        MelonEvents.OnApplicationDefiniteQuit.Subscribe(() => ModToggleManager.ApplyPendingChanges(Logger));
    }

    public override void OnUpdate()
    {
        if (!_shouldUpdate) return;
        try
        {
            InitHelper.CloseAppAndPanels();
        }
        catch (Exception e)
        {
            // prevent update from spamming the console
            _shouldUpdate = false;
            Logger.Error($"Update loop error. This can happen if S1API is not installed.\nException: {e}");
        }
    }

    public override void OnSceneWasLoaded(int buildIndex, string sceneName)
    {
        switch (sceneName)
        {
            case "Menu":
                MissingDepsPanel.CheckAndShow();
                break;
            default:
                MissingDepsPanel.Hide();
                break;
        }
    }

    public override void OnGUI()
    {
        MissingDepsPanel.OnGUI();
    }

    public override void OnApplicationQuit()
    {
        CategoryState.Save();
        ModVersionTracker.Save();
    }
}