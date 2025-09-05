using MelonLoader;
using ModsApp.UI.Input.FieldFactories;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Globalization;

namespace ModsApp.UI.Input.Handlers;

public class NumericInputHandler : IPreferenceInputHandler
{
    private readonly UITheme _theme;
    private readonly MelonLogger.Instance _logger;

    private static readonly HashSet<Type> SupportedTypes = new()
    {
        typeof(int),
        typeof(float),
        typeof(double),
        typeof(decimal)
    };

    public NumericInputHandler(UITheme theme, MelonLogger.Instance logger)
    {
        _theme = theme;
        _logger = logger;
    }

    public bool CanHandle(Type valueType) => SupportedTypes.Contains(valueType);

    public void CreateInput(MelonPreferences_Entry entry, GameObject parent, string entryKey,
        object currentValue, Action<string, object> onValueChanged)
    {
        var type = currentValue.GetType();
        var stringValue = Convert.ToString(currentValue, CultureInfo.InvariantCulture);

        Func<string, bool> validator = value =>
        {
            try
            {
                var parsed = Convert.ChangeType(value, type, CultureInfo.InvariantCulture);
                return entry.Validator?.IsValid(parsed) ?? true;
            }
            catch
            {
                return false;
            }
        };

        var contentType = (type == typeof(int)) 
            ? InputField.ContentType.IntegerNumber 
            : InputField.ContentType.DecimalNumber;

        var input = InputFieldFactory.CreateInputField(
            parent,
            $"{entryKey}_Input",
            stringValue,
            contentType,
            minWidth: 60,
            validator
        );

        input.onEndEdit.AddListener(value =>
        {
            try
            {
                var parsedValue = Convert.ChangeType(value, type, CultureInfo.InvariantCulture);
                if (!parsedValue.Equals(currentValue))
                {
                    onValueChanged(entryKey, parsedValue);
                    _logger.Msg($"Modified preference {entryKey}: {parsedValue}");
                }
            }
            catch
            {
                // Reset to previous valid value
                input.text = stringValue;
            }
        });
    }
}
