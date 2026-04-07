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

    private GameObject _parent;
    private string _entryKey;
    private MelonPreferences_Entry _entry;
    private Action<string, object> _onValueChanged;
    private InputField _input;

    public StringInputHandler(UITheme theme, MelonLogger.Instance logger)
    {
        _theme = theme;
        _logger = logger;
    }

    public bool CanHandle(Type valueType) => valueType == typeof(string);

    public void CreateInput(MelonPreferences_Entry entry, GameObject parent, string entryKey,
        object currentValue, Action<string, object> onValueChanged)
    {
        _parent = parent;
        _entryKey = entryKey;
        _onValueChanged = onValueChanged;
        _entry = entry;

        var stringValue = (string)currentValue ?? "";
        _input = InputFieldFactory.CreateInputField(parent, $"{entryKey}_Input", stringValue,
            InputField.ContentType.Standard, 100);

        EventHelper.AddListener<string>((value) =>
        {
            if (value == stringValue) return;
            onValueChanged(entryKey, value);
            _logger.Msg($"Modified preference {entryKey}: {value}");
        }, _input.onValueChanged);
    }

    public void Recreate(object currentValue)
    {
        if (_input != null)
            UnityEngine.Object.DestroyImmediate(_input.gameObject);

        CreateInput(_entry, _parent, _entryKey, currentValue, _onValueChanged);
    }
}