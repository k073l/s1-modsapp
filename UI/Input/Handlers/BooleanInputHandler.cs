using HarmonyLib;
using MelonLoader;
using ModsApp;
using ModsApp.Helpers;
using ModsApp.UI.Input.FieldFactories;
using S1API.Internal.Abstraction;
using S1API.Utils;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ModsApp.UI.Input.Handlers;

public class BooleanInputHandler : IPreferenceInputHandler
{
    private readonly UITheme _theme;
    private readonly MelonLogger.Instance _logger;

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
        var initial = (bool)currentValue;
        var current = initial;

        var toggle = ToggleFactory.CreateSliding(
            parent.transform,
            $"{entryKey}_Toggle",
            initial,
            _theme.SuccessColor,
            _theme.TextSecondary,
            _theme.BgInput,
            _theme.BgInput
        );

        ToggleUtils.AddListener(toggle, value =>
        {
            if (value == current) return;
            current = value;
            onValueChanged(entryKey, value);
            _logger.Msg($"Modified preference {entryKey}: {value}");
        });
    }
}