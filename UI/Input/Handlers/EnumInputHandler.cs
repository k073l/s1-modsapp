using MelonLoader;
using System;
using System.Linq;
using UnityEngine;
using ModsApp.UI.Input.FieldFactories;

namespace ModsApp.UI.Input.Handlers
{
    public class EnumInputHandler : IPreferenceInputHandler
    {
        private readonly UITheme _theme;
        private readonly MelonLogger.Instance _logger;

        public EnumInputHandler(UITheme theme, MelonLogger.Instance logger)
        {
            _theme = theme;
            _logger = logger;
        }

        public bool CanHandle(Type valueType) => valueType.IsEnum;

        public void CreateInput(MelonPreferences_Entry entry, GameObject parent,
            string entryKey, object currentValue, Action<string, object> onValueChanged)
        {
            var enumType = currentValue.GetType();
            var enumValues = Enum.GetValues(enumType).Cast<object>().ToArray();
            
            var dropdownInput = DropdownFactory.CreateDropdownInput<object>(
                parent, entryKey, currentValue, val => val.ToString(),
                containerSize: new Vector2(150, 20),
                inputFieldWidth: 120,
                dropdownSize: new Vector2(150, 110),
                maxVisibleItems: 6,
                logger: _logger);

            dropdownInput.OnFilterItems += (filter) => enumValues; // probably a custom enum, no filtering
            
            dropdownInput.OnValidateInput += (input) =>
            {
                if (TryParseEnum(enumType, input, out var exactMatch))
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
            
            dropdownInput.OnValueChanged += (selectedValue) =>
            {
                onValueChanged(entryKey, selectedValue);
            };

            MelonDebug.Msg($"[{entryKey}] Enum input created with {enumValues.Length} options");
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