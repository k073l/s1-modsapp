using UnityEngine;

namespace ModsApp.UI.Themes;

public class Neon : ITheme
{
    public Color BgPrimary => new Color(0.05f, 0.05f, 0.10f, 1f);
    public Color BgSecondary => new Color(0.08f, 0.08f, 0.15f, 1f);
    public Color BgCard => new Color(0.10f, 0.10f, 0.20f, 1f);
    public Color BgCategory => new Color(0.06f, 0.06f, 0.12f, 1f);
    public Color BgInput => new Color(0.98f, 0.98f, 0.98f, 1f);
    public Color AccentPrimary => new Color(0.00f, 1.00f, 0.90f, 1f);
    public Color AccentSecondary => new Color(1.00f, 0.10f, 0.80f, 1f);
    public Color TextPrimary => new Color(0.95f, 0.95f, 0.95f, 1f);
    public Color TextSecondary => new Color(0.70f, 0.70f, 0.70f, 0.75f);
    public Color InputPrimary => new Color(0.05f, 0.05f, 0.05f, 1f);
    public Color InputSecondary => new Color(0.50f, 0.50f, 0.50f, 1f);
    public Color SuccessColor => new Color(0.00f, 1.00f, 0.40f, 1f);
    public Color WarningColor => new Color(1.00f, 0.80f, 0.00f, 1f);
    public Color ErrorColor => new Color(1.00f, 0.05f, 0.20f, 1f);
}