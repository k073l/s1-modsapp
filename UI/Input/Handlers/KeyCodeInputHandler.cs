using MelonLoader;
using System;
using System.Linq;
using UnityEngine;
using ModsApp.UI.Input.FieldFactories;

namespace ModsApp.UI.Input.Handlers;

public class KeyCodeInputHandler : IPreferenceInputHandler
{
    private readonly UITheme _theme;
    private readonly MelonLogger.Instance _logger;
    private static readonly KeyCode[] _allKeyCodes;

    static KeyCodeInputHandler()
    {
        _allKeyCodes = Enum.GetValues(typeof(KeyCode))
            .Cast<KeyCode>()
            .Where(k => !k.ToString().StartsWith("Mouse") && !k.ToString().StartsWith("Joystick"))
            .OrderBy(k => k.ToString())
            .ToArray();
    }

    public KeyCodeInputHandler(UITheme theme, MelonLogger.Instance logger)
    {
        _theme = theme;
        _logger = logger;
    }

    public bool CanHandle(Type valueType) => valueType == typeof(KeyCode);

    public void CreateInput(MelonPreferences_Entry entry, GameObject parent,
        string entryKey, object currentValue, Action<string, object> onValueChanged)
    {
        var keyCodeValue = (KeyCode)currentValue;

        var dropdownInput = DropdownFactory.CreateDropdownInput<KeyCode>(
            parent, entryKey, keyCodeValue, kc => kc.ToString(),
            containerSize: new Vector2(150, 20),
            inputFieldWidth: 120,
            dropdownSize: new Vector2(150, 110),
            maxVisibleItems: 4,
            logger: _logger);

        dropdownInput.OnFilterItems += (filter) => GetFilteredKeyCodes(filter);

        dropdownInput.OnValidateInput += (input) =>
        {
            var exactMatch = _allKeyCodes.FirstOrDefault(k =>
                string.Equals(k.ToString(), input, StringComparison.OrdinalIgnoreCase));
            if (exactMatch != default)
                return exactMatch;

            if (!string.IsNullOrEmpty(input))
            {
                var partialMatch = _allKeyCodes.FirstOrDefault(k =>
                    k.ToString().StartsWith(input, StringComparison.OrdinalIgnoreCase));
                if (partialMatch != default)
                    return partialMatch;
            }

            return default(KeyCode);
        };

        dropdownInput.OnValueChanged += (selectedKeyCode) => { onValueChanged(entryKey, selectedKeyCode); };

        _logger.Msg($"[{entryKey}] KeyCode input created with {_allKeyCodes.Length} options");
    }

    private KeyCode[] GetFilteredKeyCodes(string filter)
    {
        if (string.IsNullOrEmpty(filter))
            return _allKeyCodes.Take(20).ToArray();

        return _allKeyCodes
            .Where(k => k.ToString().IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
            .Take(20)
            .ToArray();
    }
}