using UnityEngine;

namespace ModsApp.UI.Themes;

public class Metal : ITheme
{
    public Color BgPrimary => new Color(0.18f, 0.18f, 0.20f, 1f);
    public Color BgSecondary => new Color(0.22f, 0.22f, 0.25f, 1f);
    public Color BgCard => new Color(0.25f, 0.25f, 0.28f, 1f);
    public Color BgCategory => new Color(0.20f, 0.20f, 0.23f, 1f);
    public Color BgInput => new Color(0.95f, 0.95f, 0.95f, 1f);
    public Color AccentPrimary => new Color(0.60f, 0.60f, 0.65f, 1f);
    public Color AccentSecondary => new Color(0.40f, 0.40f, 0.45f, 1f);
    public Color TextPrimary => new Color(0.95f, 0.95f, 0.95f, 1f);
    public Color TextSecondary => new Color(0.70f, 0.70f, 0.70f, 0.75f);
    public Color InputPrimary => new Color(0.10f, 0.10f, 0.12f, 1f);
    public Color InputSecondary => new Color(0.50f, 0.50f, 0.55f, 1f);
    public Color SuccessColor => new Color(0.20f, 0.80f, 0.20f, 1f);
    public Color WarningColor => new Color(0.95f, 0.75f, 0.20f, 1f);
    public Color ErrorColor => new Color(0.95f, 0.20f, 0.20f, 1f);
}