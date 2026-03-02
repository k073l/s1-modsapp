using UnityEngine;

namespace ModsApp.UI.Themes;

public class Custom : ITheme
{
    public Color BgPrimary => ModsApp.BackgroundPrimaryEntry.Value;
    public Color BgSecondary => ModsApp.BackgroundSecondaryEntry.Value;
    public Color BgCard => ModsApp.BackgroundCardEntry.Value;
    public Color BgCategory => ModsApp.BackgroundCategoryEntry.Value;
    public Color BgInput => ModsApp.BackgroundInputEntry.Value;
    public Color AccentPrimary => ModsApp.AccentPrimaryEntry.Value;
    public Color AccentSecondary => ModsApp.AccentSecondaryEntry.Value;
    public Color TextPrimary => ModsApp.TextPrimaryEntry.Value;
    public Color TextSecondary => ModsApp.TextSecondaryEntry.Value;
    public Color InputPrimary => ModsApp.InputPrimaryEntry.Value;
    public Color InputSecondary => ModsApp.InputSecondaryEntry.Value;
    public Color SuccessColor => ModsApp.SuccessColorEntry.Value;
    public Color WarningColor => ModsApp.WarningColorEntry.Value;
    public Color ErrorColor => ModsApp.ErrorColorEntry.Value;
}