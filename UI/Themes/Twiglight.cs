using UnityEngine;

namespace ModsApp.UI.Themes;

public class Twilight : ITheme
{
    public Color BgPrimary => new Color(0.10f, 0.05f, 0.25f, 1f);
    public Color BgSecondary => new Color(0.12f, 0.08f, 0.30f, 1f);
    public Color BgCard => new Color(0.15f, 0.10f, 0.35f, 1f);
    public Color BgCategory => new Color(0.11f, 0.07f, 0.28f, 1f);
    public Color BgInput => new Color(0.95f, 0.95f, 0.98f, 1f);
    public Color AccentPrimary => new Color(0.70f, 0.40f, 0.95f, 1f);
    public Color AccentSecondary => new Color(0.50f, 0.25f, 0.80f, 1f);
    public Color TextPrimary => new Color(0.95f, 0.95f, 0.95f, 1f);
    public Color TextSecondary => new Color(0.70f, 0.70f, 0.85f, 0.75f);
    public Color InputPrimary => new Color(0.08f, 0.05f, 0.12f, 1f);
    public Color InputSecondary => new Color(0.45f, 0.35f, 0.55f, 1f);
    public Color SuccessColor => new Color(0.20f, 0.80f, 0.20f, 1f);
    public Color WarningColor => new Color(0.95f, 0.75f, 0.20f, 1f);
    public Color ErrorColor => new Color(0.95f, 0.20f, 0.25f, 1f);
    public Color JsonKeyColor => new Color(0.016f, 0.318f, 0.647f, 1f);
    public Color JsonStringColor => new Color(0.639f, 0.082f, 0.082f, 1f);
    public Color JsonNumberColor => new Color(0.035f, 0.525f, 0.345f, 1f);
    public Color JsonLiteralColor => new Color(0f, 0f, 1f, 1f);
    public Color JsonBracketColor => new Color(0.686f, 0f, 0.859f, 1f);
    public Color JsonPunctuationColor => new Color(0.200f, 0.200f, 0.200f, 1f);
}