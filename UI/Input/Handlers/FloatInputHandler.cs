using MelonLoader;
using ModsApp.UI.Input.FieldFactories;
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
        
        Func<string, bool> validator = value =>
        {
            if (!float.TryParse(value, out var parsed))
                return false;

            return entry.Validator?.IsValid(parsed) ?? true;
        };

        var input = InputFieldFactory.CreateInputField(
            parent,
            $"{entryKey}_Input",
            floatValue.ToString("0.##"),
            InputField.ContentType.DecimalNumber,
            60,
            validator // pass validator down
        );

        // Fire onValueChanged only when value is valid and different
        input.onEndEdit.AddListener(value =>
        {
            if (float.TryParse(value, out var parsedValue))
            {
                if (!Mathf.Approximately(parsedValue, floatValue))
                {
                    onValueChanged(entryKey, parsedValue);
                    _logger.Msg($"Modified preference {entryKey}: {parsedValue}");
                }
            }
        });
    }
}