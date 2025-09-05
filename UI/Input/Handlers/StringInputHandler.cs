using MelonLoader;
using ModsApp.UI.Input.FieldFactories;
using S1API.Internal.Abstraction;
using UnityEngine;
using UnityEngine.UI;

namespace ModsApp.UI.Input.Handlers;

public class StringInputHandler : IPreferenceInputHandler
{
    private readonly UITheme _theme;
    private readonly MelonLogger.Instance _logger;

    public StringInputHandler(UITheme theme, MelonLogger.Instance logger)
    {
        _theme = theme;
        _logger = logger;
    }

    public bool CanHandle(Type valueType) => valueType == typeof(string);

    public void CreateInput(MelonPreferences_Entry entry, GameObject parent, string entryKey,
        object currentValue, Action<string, object> onValueChanged)
    {
        var stringValue = (string)currentValue ?? "";
        var input = InputFieldFactory.CreateInputField(parent, $"{entryKey}_Input", stringValue,
            InputField.ContentType.Standard, 100);

        EventHelper.AddListener((value) =>
        {
            if (value == stringValue) return;
            onValueChanged(entryKey, value);
            _logger.Msg($"Modified preference {entryKey}: {value}");
        }, input.onValueChanged);
    }
}