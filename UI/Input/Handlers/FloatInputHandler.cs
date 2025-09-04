using MelonLoader;
using UnityEngine;
using UnityEngine.UI;

namespace ModsApp.UI.Input.Handlers;

public class FloatInputHandler : IPreferenceInputHandler
{
    private readonly UITheme _theme;
    private readonly MelonLogger.Instance _logger;

    public FloatInputHandler(UITheme theme, MelonLogger.Instance logger)
    {
        _theme = theme;
        _logger = logger;
    }

    public bool CanHandle(Type valueType) => valueType == typeof(float);

    public void CreateInput(MelonPreferences_Entry entry, GameObject parent, string entryKey,
        object currentValue, Action<string, object> onValueChanged)
    {
        var floatValue = (float)currentValue;
        var input = InputFieldFactory.CreateInputField(parent, $"{entryKey}_Input", floatValue.ToString("0.##"),
            InputField.ContentType.DecimalNumber, 60);

        input.onValueChanged.AddListener(value =>
        {
            if (float.TryParse(value, out var parsedValue))
            {
                if (Mathf.Approximately(parsedValue, floatValue)) return;
                onValueChanged(entryKey, parsedValue);
                _logger.Msg($"Modified preference {entryKey}: {parsedValue}");
            }
            else
            {
                input.text = floatValue.ToString("0.##");
            }
        });
    }
}