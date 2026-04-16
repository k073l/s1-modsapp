using MelonLoader;
using ModsApp.UI.Input.FieldFactories;
using UnityEngine;

namespace ModsApp.UI.Input.Handlers
{
    public class EnumInputHandler : IPreferenceInputHandler
    {
        private readonly UITheme _theme;
        private readonly MelonLogger.Instance _logger;

        private GameObject _parent;
        private string _entryKey;
        private MelonPreferences_Entry _entry;
        private Action<string, object> _onValueChanged;
        private Type _enumType;
        private DropdownInputComponent<object> _dropdownInput;

        public EnumInputHandler(UITheme theme, MelonLogger.Instance logger)
        {
            _theme = theme;
            _logger = logger;
        }

        public bool CanHandle(Type valueType) => valueType.IsEnum;

        public void CreateInput(MelonPreferences_Entry entry, GameObject parent,
            string entryKey, object currentValue, Action<string, object> onValueChanged)
        {
            _parent = parent;
            _entryKey = entryKey;
            _onValueChanged = onValueChanged;
            _entry = entry;
            _enumType = currentValue.GetType();
            var enumValues = Enum.GetValues(_enumType).Cast<object>().ToArray();

            _dropdownInput = DropdownFactory.CreateDropdownInput<object>(
                parent, entryKey, currentValue, val => val.ToString(), _theme,
                containerSize: new Vector2(150, 20),
                inputFieldWidth: 120,
                dropdownSize: new Vector2(150, 110),
                maxVisibleItems: 6,
                logger: _logger);

            _dropdownInput.OnFilterItems += (filter) => enumValues;

            _dropdownInput.OnValidateInput += (input) =>
            {
                if (TryParseEnum(_enumType, input, out var exactMatch))
                    return exactMatch;

                if (!string.IsNullOrEmpty(input))
                {
                    var partialMatch = enumValues.FirstOrDefault(e =>
                        e.ToString().StartsWith(input, StringComparison.OrdinalIgnoreCase));
                    if (partialMatch != null)
                        return partialMatch;
                }

                return null;
            };

            _dropdownInput.OnValueChanged += (selectedValue) => { onValueChanged(entryKey, selectedValue); };

            MelonDebug.Msg($"[{entryKey}] Enum input created with {enumValues.Length} options");
        }

        public void Recreate(object currentValue)
        {
            if (_dropdownInput != null && _dropdownInput.Container != null)
                _dropdownInput.Destroy();
            CreateInput(_entry, _parent, _entryKey, currentValue, _onValueChanged);
        }

        public void CreateStandaloneInput(Type valueType, GameObject parent, string entryKey, object currentValue, Action<object> onValueChanged)
        {
            _enumType = valueType;
            var enumValues = Enum.GetValues(_enumType).Cast<object>().ToArray();

            _dropdownInput = DropdownFactory.CreateDropdownInput<object>(
                parent, "Standalone", currentValue, val => val.ToString(), _theme,
                containerSize: new Vector2(150, 20),
                inputFieldWidth: 120,
                dropdownSize: new Vector2(150, 110),
                maxVisibleItems: 6,
                logger: _logger);

            _dropdownInput.OnFilterItems += (filter) => enumValues;

            _dropdownInput.OnValidateInput += (input) =>
            {
                if (TryParseEnum(_enumType, input, out var exactMatch))
                    return exactMatch;

                if (!string.IsNullOrEmpty(input))
                {
                    var partialMatch = enumValues.FirstOrDefault(e =>
                        e.ToString().StartsWith(input, StringComparison.OrdinalIgnoreCase));
                    if (partialMatch != null)
                        return partialMatch;
                }

                return null;
            };

            _dropdownInput.OnValueChanged += (selectedValue) => onValueChanged(selectedValue);
        }

        private bool TryParseEnum(Type enumType, string value, out object result)
        {
            try
            {
                result = Enum.Parse(enumType, value, true);
                return true;
            }
            catch
            {
                result = null;
                return false;
            }
        }
    }
}