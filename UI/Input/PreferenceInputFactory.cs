using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MelonLoader;
using ModsApp.Helpers;
using ModsApp.UI.Input.Handlers;
using S1API.UI;
using S1API.Input;
using Object = UnityEngine.Object;

namespace ModsApp.UI.Input;



public class PreferenceInputFactory
{
    private readonly List<IPreferenceInputHandler> _handlers;
    private readonly UITheme _theme;

    public PreferenceInputFactory(UITheme theme, MelonLogger.Instance logger)
    {
        _theme = theme;
        _handlers = new List<IPreferenceInputHandler>
        {
            new BooleanInputHandler(theme, logger),
            // new IntegerInputHandler(theme, logger),
            // new FloatInputHandler(theme, logger),
            new NumericInputHandler(theme, logger),
            new StringInputHandler(theme, logger),
            new KeyCodeInputHandler(theme, logger),
            new ColorInputHandler(theme, logger),
            new EnumInputHandler(theme, logger),
            new FallbackInputHandler(theme, logger) // Always last as it handles any type
        };
    }

    public void CreatePreferenceInput(MelonPreferences_Entry entry, GameObject parent, string entryKey,
        object currentValue, Action<string, object> onValueChanged)
    {
        var valueType = entry.BoxedValue?.GetType();

        foreach (var handler in _handlers)
        {
            if (handler.CanHandle(valueType))
            {
                handler.CreateInput(entry, parent, entryKey, currentValue, onValueChanged);
                return;
            }
        }
    }

    public void RegisterHandler(IPreferenceInputHandler handler)
    {
        // Insert before fallback handler (which should always be last)
        _handlers.Insert(_handlers.Count - 1, handler);
    }
}