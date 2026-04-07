using System;
using MelonLoader;
using ModsApp.Helpers;
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

    private GameObject _parent;
    private string _entryKey;
    private Action<string, object> _onValueChanged;
    private MelonPreferences_Entry _entry;
    private InputField _input;
    private Text _label;

    public FallbackInputHandler(UITheme theme, MelonLogger.Instance logger)
    {
        _theme = theme;
        _logger = logger;
    }

    public bool CanHandle(Type valueType) => true;

    public void CreateInput(MelonPreferences_Entry entry, GameObject parent, string entryKey,
        object currentValue, Action<string, object> onValueChanged)
    {
        _parent = parent;
        _entryKey = entryKey;
        _onValueChanged = onValueChanged;
        _entry = entry;

        string initialValue = NormalizeValue(entry.GetReflectedType(), currentValue);

        _input = InputFieldFactory.CreateInputField(parent, $"{entryKey}_Input",
            initialValue, InputField.ContentType.Standard, 60);
        var inputLayout = _input.gameObject.GetOrAddComponent<LayoutElement>();
        inputLayout.minWidth = 60;
        inputLayout.preferredWidth = 60;
        inputLayout.flexibleWidth = 1;

        _label = UIFactory.Text($"{entryKey}_Label", "(Fallback)", parent.transform,
            _theme.SizeTiny);
        _label.color = _theme.TextSecondary;
        _label.fontStyle = FontStyle.Italic;
        var labelLayout = _label.gameObject.GetOrAddComponent<LayoutElement>();
        labelLayout.flexibleWidth = 0;

        if (ModsApp.InputsOnRightEntry.Value)
        {
            _input.transform.SetSiblingIndex(_label.transform.GetSiblingIndex() + 1);
        }
        else
        {
            _input.transform.SetSiblingIndex(_label.transform.GetSiblingIndex());
        }

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
                _input.text = lastValid;

                _logger.Msg($"Modified preference {entryKey}: {parsed}");
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to parse edited value for {entryKey}: {ex}");
                _input.text = lastValid;
            }
        }, _input.onEndEdit);
    }

    public void Recreate(object currentValue)
    {
        if (_input != null)
            UnityEngine.Object.DestroyImmediate(_input.gameObject);
        if (_label.gameObject != null)
            UnityEngine.Object.DestroyImmediate(_label.gameObject);

        CreateInput(_entry, _parent, _entryKey, currentValue, _onValueChanged);
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