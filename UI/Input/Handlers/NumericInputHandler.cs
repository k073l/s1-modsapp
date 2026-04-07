using System.Globalization;
using MelonLoader;
using MelonLoader.Preferences;
using ModsApp.Helpers;
using ModsApp.UI.Input.FieldFactories;
using S1API.Internal.Abstraction;
using UnityEngine;
using UnityEngine.UI;

namespace ModsApp.UI.Input.Handlers;

public class NumericInputHandler : IPreferenceInputHandler
{
    private readonly UITheme _theme;
    private readonly MelonLogger.Instance _logger;

    private static readonly HashSet<Type> SupportedTypes = new()
    {
        typeof(byte),
        typeof(sbyte),
        typeof(short),
        typeof(ushort),
        typeof(int),
        typeof(uint),
        typeof(long),
        typeof(ulong),
        typeof(float),
        typeof(double),
        typeof(decimal),
    };

    private GameObject _parent;
    private string _entryKey;
    private Action<string, object> _onValueChanged;
    private MelonPreferences_Entry _entry;
    private GameObject _sliderContainer;
    private InputField _input;

    public NumericInputHandler(UITheme theme, MelonLogger.Instance logger)
    {
        _theme = theme;
        _logger = logger;
    }

    public bool CanHandle(Type valueType) => SupportedTypes.Contains(valueType);

    public void CreateInput(MelonPreferences_Entry entry, GameObject parent, string entryKey,
        object currentValue, Action<string, object> onValueChanged)
    {
        _parent = parent;
        _entryKey = entryKey;
        _onValueChanged = onValueChanged;
        _entry = entry;

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

        var isInteger = type == typeof(int) || type == typeof(uint) || type == typeof(short) ||
                        type == typeof(ushort) || type == typeof(byte) || type == typeof(sbyte) ||
                        type == typeof(long) || type == typeof(ulong);

        var contentType = isInteger
            ? InputField.ContentType.IntegerNumber
            : InputField.ContentType.DecimalNumber;

        if (entry.Validator is IValueRange range)
        {
            try
            {
                var min = Convert.ToSingle(range.MinValue);
                var max = Convert.ToSingle(range.MaxValue);
                var value = Convert.ToSingle(currentValue);

                var result = SliderFactory.CreateSlider(
                    parent,
                    $"{entryKey}_Slider",
                    min,
                    max,
                    value,
                    isInteger,
                    contentType,
                    validator,
                    v =>
                    {
                        try
                        {
                            object converted =
                                isInteger
                                    ? Convert.ChangeType(Mathf.RoundToInt(v), type)
                                    : Convert.ChangeType(v, type);

                            if (!converted.Equals(currentValue))
                            {
                                if (Time.frameCount % 20 == 0)
                                    _logger.Msg($"Modified preference {entryKey}: {converted}");
                                onValueChanged(entryKey, converted);
                            }
                        }
                        catch
                        {
                        }
                    });

                (_, _, _input) = result;
                _sliderContainer = result.container;
                var sliderContainerLayout = _sliderContainer.GetOrAddComponent<LayoutElement>();
                sliderContainerLayout.flexibleWidth = 2;
                sliderContainerLayout.preferredHeight = 20;
                sliderContainerLayout.flexibleHeight = 0;

                return;
            }
            catch (Exception e)
            {
            }
        }

        _input = InputFieldFactory.CreateInputField(
            parent,
            $"{entryKey}_Input",
            stringValue,
            contentType,
            minWidth: 60,
            validator
        );

        EventHelper.AddListener<string>(value =>
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
                _input.text = stringValue;
            }
        }, _input.onEndEdit);
    }

    public void Recreate(object currentValue)
    {
        if (_sliderContainer != null)
            UnityEngine.Object.DestroyImmediate(_sliderContainer);
        if (_input != null)
            UnityEngine.Object.DestroyImmediate(_input.gameObject);

        CreateInput(_entry, _parent, _entryKey, currentValue, _onValueChanged);
    }
}