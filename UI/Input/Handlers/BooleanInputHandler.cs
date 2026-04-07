using MelonLoader;
using ModsApp.UI.Input.FieldFactories;
using S1API.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace ModsApp.UI.Input.Handlers;

public class BooleanInputHandler : IPreferenceInputHandler
{
    private readonly UITheme _theme;
    private readonly MelonLogger.Instance _logger;

    private GameObject _parent;
    private string _entryKey;
    private MelonPreferences_Entry _entry;
    private Action<string, object> _onValueChanged;
    private Toggle _toggle;

    public BooleanInputHandler(UITheme theme, MelonLogger.Instance logger)
    {
        _theme = theme;
        _logger = logger;
    }

    public bool CanHandle(Type valueType) => valueType == typeof(bool);

    public void CreateInput(
        MelonPreferences_Entry entry,
        GameObject parent,
        string entryKey,
        object currentValue,
        Action<string, object> onValueChanged)
    {
        _parent = parent;
        _entryKey = entryKey;
        _onValueChanged = onValueChanged;
        _entry = entry;

        var initial = (bool)currentValue;

        _toggle = ToggleFactory.CreateSliding(
            parent.transform,
            $"{entryKey}_Toggle",
            initial,
            _theme.SuccessColor,
            _theme.TextSecondary,
            _theme.BgInput,
            _theme.BgInput
        );

        ToggleUtils.AddListener(_toggle, value =>
        {
            onValueChanged(entryKey, value);
            _logger.Msg($"Modified preference {entryKey}: {value}");
        });
    }

    public void Recreate(object currentValue)
    {
        if (_toggle != null)
            UnityEngine.Object.DestroyImmediate(_toggle.gameObject);

        CreateInput(_entry, _parent, _entryKey, currentValue, _onValueChanged);
    }
}