using MelonLoader;
using S1API.UI;
using UnityEngine;
using UnityEngine.UI;

namespace ModsApp.UI.Input.Handlers;

public class FallbackInputHandler : IPreferenceInputHandler
{
    private readonly UITheme _theme;
    private readonly MelonLogger.Instance _logger;

    public FallbackInputHandler(UITheme theme, MelonLogger.Instance logger)
    {
        _theme = theme;
        _logger = logger;
    }

    public bool CanHandle(Type valueType) => true; // Handles any type as fallback

    public void CreateInput(MelonPreferences_Entry entry, GameObject parent, string entryKey,
        object currentValue, Action<string, object> onValueChanged)
    {
        // // Read-only text for unknown types
        // var valueText = UIFactory.Text($"{entryKey}_Value", currentValue?.ToString() ?? "null",
        //     parent.transform, 11);
        // valueText.color = _theme.TextPrimary;
        // valueText.fontStyle = FontStyle.Italic;
        
        // We edit as a string instead and try to convert it back
        var stringValue = currentValue?.ToString() ?? "";
        var input = InputFieldFactory.CreateInputField(parent, $"{entryKey}_Input", stringValue,
            InputField.ContentType.Standard, 100);
        input.onValueChanged.AddListener(value =>
        {
            if (value == stringValue) return;
            onValueChanged(entryKey, value);
            _logger.Msg($"Modified preference {entryKey}: {value}");
        });
    }
}