using UnityEngine;

namespace ModsApp.UI.Themes;

public interface ITheme
{
    Color BgPrimary { get; }
    Color BgSecondary { get; }
    Color BgCard { get; }
    Color BgCategory { get; }
    Color BgInput { get; }
    Color AccentPrimary { get; }
    Color AccentSecondary { get; }
    Color TextPrimary { get; }
    Color TextSecondary { get; }
    Color InputPrimary { get; }
    Color InputSecondary { get; }
    Color SuccessColor { get; }
    Color WarningColor { get; }
    Color ErrorColor { get; }
    Color JsonKeyColor { get; }
    Color JsonStringColor { get; }
    Color JsonNumberColor { get; }
    Color JsonLiteralColor { get; }
    Color JsonBracketColor { get; }
    Color JsonPunctuationColor { get; }
}