using ModsApp.Helpers.Registries;
using UnityEngine;

namespace ModsApp.UI.Themes;

public class Custom : ITheme
{
    public Color BgPrimary => SettingsRegistry.BackgroundPrimaryEntry.Value;
    public Color BgSecondary => SettingsRegistry.BackgroundSecondaryEntry.Value;
    public Color BgCard => SettingsRegistry.BackgroundCardEntry.Value;
    public Color BgCategory => SettingsRegistry.BackgroundCategoryEntry.Value;
    public Color BgInput => SettingsRegistry.BackgroundInputEntry.Value;
    public Color AccentPrimary => SettingsRegistry.AccentPrimaryEntry.Value;
    public Color AccentSecondary => SettingsRegistry.AccentSecondaryEntry.Value;
    public Color TextPrimary => SettingsRegistry.TextPrimaryEntry.Value;
    public Color TextSecondary => SettingsRegistry.TextSecondaryEntry.Value;
    public Color InputPrimary => SettingsRegistry.InputPrimaryEntry.Value;
    public Color InputSecondary => SettingsRegistry.InputSecondaryEntry.Value;
    public Color SuccessColor => SettingsRegistry.SuccessColorEntry.Value;
    public Color WarningColor => SettingsRegistry.WarningColorEntry.Value;
    public Color ErrorColor => SettingsRegistry.ErrorColorEntry.Value;
    public Color JsonKeyColor => SettingsRegistry.JsonKeyColor.Value;
    public Color JsonStringColor => SettingsRegistry.JsonStringColor.Value;
    public Color JsonNumberColor => SettingsRegistry.JsonNumberColor.Value;
    public Color JsonLiteralColor => SettingsRegistry.JsonLiteralColor.Value;
    public Color JsonBracketColor => SettingsRegistry.JsonBracketColor.Value;
    public Color JsonPunctuationColor => SettingsRegistry.JsonPunctuationColor.Value;
}