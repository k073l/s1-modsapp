using ModsApp.UI.Themes;
using UnityEngine;

namespace ModsApp.UI;

public class UITheme
{
    private ITheme _theme;
    public Color BgPrimary => _theme.BgPrimary;
    public Color BgSecondary => _theme.BgSecondary;
    public Color BgCard => _theme.BgCard;
    public Color BgCategory => _theme.BgCategory;
    public Color BgInput => _theme.BgInput;
    public Color AccentPrimary => _theme.AccentPrimary;
    public Color AccentSecondary => _theme.AccentSecondary;
    public Color TextPrimary => _theme.TextPrimary;
    public Color TextSecondary => _theme.TextSecondary;
    public Color InputPrimary => _theme.InputPrimary;
    public Color InputSecondary => _theme.InputSecondary;
    public Color SuccessColor => _theme.SuccessColor;
    public Color WarningColor => _theme.WarningColor;
    public Color ErrorColor => _theme.ErrorColor;
    public Color JsonKeyColor => _theme.JsonKeyColor;
    public Color JsonStringColor => _theme.JsonStringColor;
    public Color JsonNumberColor => _theme.JsonNumberColor;
    public Color JsonLiteralColor => _theme.JsonLiteralColor;
    public Color JsonBracketColor => _theme.JsonBracketColor;
    public Color JsonPunctuationColor => _theme.JsonPunctuationColor;

    private const int _sizeTiny = 10;
    private const int _sizeSmall = 12;
    private const int _sizeStandard = 14;
    private const int _sizeMedium = 16;
    private const int _sizeLarge = 20;

    private float _textScale = 1.0f;

    public int SizeTiny => Mathf.RoundToInt(_sizeTiny * _textScale);
    public int SizeSmall => Mathf.RoundToInt(_sizeSmall * _textScale);
    public int SizeStandard => Mathf.RoundToInt(_sizeStandard * _textScale);
    public int SizeMedium => Mathf.RoundToInt(_sizeMedium * _textScale);
    public int SizeLarge => Mathf.RoundToInt(_sizeLarge * _textScale);

    public void SetTextScale(TextSizeProfile profile)
    {
        _textScale = profile switch
        {
            TextSizeProfile.ExtraSmall => 0.75f,
            TextSizeProfile.Small => 0.85f,
            TextSizeProfile.Normal => 1.0f,
            TextSizeProfile.Large => 1.25f,
            TextSizeProfile.ExtraLarge => 1.5f,
            _ => 1.0f
        };
    }

    public void SetTheme(ThemeOption theme)
    {
        _theme = theme switch
        {
            ThemeOption.Forest => new Forest(),
            ThemeOption.Metal => new Metal(),
            ThemeOption.Neon => new Neon(),
            ThemeOption.Ocean => new Ocean(),
            ThemeOption.Slate => new Slate(),
            ThemeOption.Solar => new Solar(),
            ThemeOption.Twilight => new Twilight(),
            ThemeOption.Custom => new Custom(),
            _ => new Slate()
        };
    }

    public UITheme()
    {
        SetTextScale(ModsApp.TextSizeProfileEntry.Value);
        SetTheme(ModsApp.ThemeOptionEntry.Value);
    }
}