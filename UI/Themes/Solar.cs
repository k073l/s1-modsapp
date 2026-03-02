using UnityEngine;

namespace ModsApp.UI.Themes;

public class Solar : ITheme
{
    public Color BgPrimary => new Color(1f, 0.95f, 0.85f, 1f);
    public Color BgSecondary => new Color(0.98f, 0.90f, 0.75f, 1f);
    public Color BgCard => new Color(1f, 0.85f, 0.60f, 1f);
    public Color BgCategory => new Color(1f, 0.88f, 0.65f, 1f);
    public Color BgInput => new Color(1f, 1f, 1f, 1f);
    public Color AccentPrimary => new Color(1f, 0.60f, 0.10f, 1f);
    public Color AccentSecondary => new Color(0.85f, 0.45f, 0.05f, 1f);
    public Color TextPrimary => new Color(0.15f, 0.10f, 0.05f, 1f);
    public Color TextSecondary => new Color(0.35f, 0.25f, 0.15f, 0.75f);
    public Color InputPrimary => new Color(0.95f, 0.90f, 0.80f, 1f);
    public Color InputSecondary => new Color(0.70f, 0.60f, 0.50f, 1f);
    public Color SuccessColor => new Color(0.20f, 0.60f, 0.10f, 1f);
    public Color WarningColor => new Color(0.95f, 0.60f, 0.05f, 1f);
    public Color ErrorColor => new Color(0.85f, 0.10f, 0.05f, 1f);
}