using MelonLoader;
using MelonLoader.Preferences;
using ModsApp.UI;
using ModsApp.UI.Themes;
using UnityEngine;

namespace ModsApp.Helpers.Registries;

public static class SettingsRegistry
{
    public static MelonPreferences_Category AccessibilityCategory;

    public static MelonPreferences_Entry<TextSizeProfile> TextSizeProfileEntry;
    public static MelonPreferences_Entry<bool> InputsOnRightEntry;
    public static MelonPreferences_Entry<int> EntryVlgSpacingEntry;
    public static MelonPreferences_Entry<bool> UseNewJsonEditor;
    public static MelonPreferences_Entry<float> SearchSimilarityThreshold;

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

    public static void Initialize()
    {
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
            "Use new JSON editor",
            description:
            "Determines if ModsApp should use TMP JSON editor. Set to false if you have any issues (switches to legacy editor)");

        SearchSimilarityThreshold = AccessibilityCategory.CreateEntry("ModsAppSearchSimilarityThreshold", 0.6f,
            "Search Matching Sensitivity",
            description:
            "Controls how closely an item needs to match the search query to be included in results. Higher values will require a closer match, while lower values will include more results with looser matching. 1 disables fuzzy matching",
            validator: new ValueRange<float>(0.3f, 1f));

        ThemesCategory = MelonPreferences.CreateCategory("ModsApp_Themes", "Themes");
        ThemeOptionEntry = ThemesCategory.CreateEntry("ModsAppThemeOption", ThemeOption.Slate,
            "Theme Preset",
            description:
            "Predefined theme presets. Set to Custom to use custom colors defined below - Custom may require game restart to apply fully, but changing presets should apply immediately");
        CopyCurrentToCustom = ThemesCategory.CreateEntry("ModsAppCopyToCustom", false,
            "Copy Theme to Custom", description: "Copy the colors of current theme to custom theme's colors");
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
        JsonKeyColor = ThemesCategory.CreateEntry("ModsAppJsonKeyColor", slate.JsonKeyColor, "JSON Editor Key Color",
            description: "Color used for keys in the JSON editor (the property names before colons).");
        JsonStringColor = ThemesCategory.CreateEntry("ModsAppJsonStringColor", slate.JsonStringColor,
            "JSON Editor String Color", description: "Color used for string values in the JSON editor.");
        JsonNumberColor = ThemesCategory.CreateEntry("ModsAppJsonNumberColor", slate.JsonNumberColor,
            "JSON Editor Number Color", description: "Color used for numeric values in the JSON editor.");
        JsonLiteralColor = ThemesCategory.CreateEntry("ModsAppJsonLiteralColor", slate.JsonLiteralColor,
            "JSON Editor Literal Color",
            description: "Color used for boolean and null values (true, false, null) in the JSON editor.");
        JsonBracketColor = ThemesCategory.CreateEntry("ModsAppJsonBracketColor", slate.JsonBracketColor,
            "JSON Editor Bracket Color", description: "Color used for brackets in JSON ({, }, [, ]) in the editor.");
        JsonPunctuationColor = ThemesCategory.CreateEntry("ModsAppJsonPunctuationColor", slate.JsonPunctuationColor,
            "JSON Editor Punctuation Color",
            description: "Color used for punctuation characters (colon ':' and comma ',') in the JSON editor.");
    }
}