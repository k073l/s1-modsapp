using System;
using MelonLoader;
using ModsApp.UI.Input.FieldFactories;
using S1API.Internal.Abstraction;
using S1API.UI;
using Tomlet;
using Tomlet.Models;
using UnityEngine;
using UnityEngine.UI;

namespace ModsApp.UI.Input.Handlers;

public class FallbackInputHandler : IPreferenceInputHandler
{
    private readonly UITheme _theme;
    private readonly MelonLogger.Instance _logger;
    private readonly Tomlet.TomlParser _parser = new Tomlet.TomlParser();

    public FallbackInputHandler(UITheme theme, MelonLogger.Instance logger)
    {
        _theme = theme;
        _logger = logger;
    }

    public bool CanHandle(Type valueType) => true;

    public void CreateInput(MelonPreferences_Entry entry, GameObject parent, string entryKey,
        object currentValue, Action<string, object> onValueChanged)
    {
        string initialValue = NormalizeValue(entry.GetReflectedType(), currentValue);

        var input = InputFieldFactory.CreateInputField(parent, $"{entryKey}_Input",
            initialValue, InputField.ContentType.Standard, 100);

        var label = UIFactory.Text($"{entryKey}_Label", "(Fallback, might not work properly)", parent.transform,
            10);
        label.color = _theme.TextSecondary;
        label.fontStyle = FontStyle.Italic;

        string lastValid = initialValue;

        EventHelper.AddListener<string>((raw) =>
        {
            if (raw == lastValid)
                return;

            try
            {
                string wrapped = $"Temp = {raw}";
                TomlDocument doc = _parser.Parse(wrapped);
                TomlValue value = doc.GetValue("Temp");

                object parsed = TomletMain.To(entry.GetReflectedType(), value);

                onValueChanged(entryKey, parsed);
                lastValid = NormalizeValue(entry.GetReflectedType(), parsed);
                input.text = lastValid;

                _logger.Msg($"Modified preference {entryKey}: {parsed}");
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to parse edited value for {entryKey}: {ex}");
                input.text = lastValid;
            }
        }, input.onEndEdit);
    }

    private string NormalizeValue(Type targetType, object value)
    {
        if (value == null)
            return "null";

        try
        {
            TomlValue tomlValue = value is string s
                ? TomletMain.ValueFrom(TomletMain.To(targetType, _parser.Parse($"Temp = {s}").GetValue("Temp")))
                : TomletMain.ValueFrom(value);

            return tomlValue.SerializedValue;
        }
        catch
        {
            return value.ToString();
        }
    }
}