using MelonLoader;
using ModsApp.UI.Input.FieldFactories;
using UnityEngine;
using UnityEngine.UI;

namespace ModsApp.UI.Input.Handlers;

public class IntegerInputHandler : IPreferenceInputHandler
{
    private readonly UITheme _theme;
    private readonly MelonLogger.Instance _logger;

    public IntegerInputHandler(UITheme theme, MelonLogger.Instance logger)
    {
        _theme = theme;
        _logger = logger;
    }

    public bool CanHandle(Type valueType) => valueType == typeof(int);

    public void CreateInput(MelonPreferences_Entry entry, GameObject parent, string entryKey,
        object currentValue, Action<string, object> onValueChanged)
    {
        var intValue = (int)currentValue;
        var input = InputFieldFactory.CreateInputField(parent, $"{entryKey}_Input", intValue.ToString(),
            InputField.ContentType.IntegerNumber, 50);

        input.onValueChanged.AddListener(value =>
        {
            if (int.TryParse(value, out var parsedValue))
            {
                if (parsedValue == intValue) return;
                onValueChanged(entryKey, parsedValue);
                _logger.Msg($"Modified preference {entryKey}: {parsedValue}");
            }
            else
            {
                input.text = intValue.ToString();
            }
        });
    }
}