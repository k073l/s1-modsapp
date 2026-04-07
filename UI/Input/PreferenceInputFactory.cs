using MelonLoader;
using ModsApp.UI.Input.Handlers;
using UnityEngine;

namespace ModsApp.UI.Input;



public class PreferenceInputFactory
{
    private readonly UITheme _theme;
    private readonly MelonLogger.Instance _logger;
    private readonly List<Func<IPreferenceInputHandler>> _handlerFactories;

    public PreferenceInputFactory(UITheme theme, MelonLogger.Instance logger)
    {
        _theme = theme;
        _logger = logger;
        _handlerFactories =
        [
            () => new BooleanInputHandler(_theme, _logger),
            () => new NumericInputHandler(_theme, _logger),
            () => new StringInputHandler(_theme, _logger),
            () => new KeyCodeInputHandler(_theme, _logger),
            () => new ColorInputHandler(_theme, _logger),
            () => new EnumInputHandler(_theme, _logger),
            () => new VectorHandler(_theme, _logger),
            () => new FallbackInputHandler(_theme, _logger)
        ];
    }

    public IPreferenceInputHandler CreatePreferenceInput(MelonPreferences_Entry entry, GameObject parent, string entryKey,
        object currentValue, Action<string, object> onValueChanged)
    {
        var valueType = entry.BoxedValue?.GetType();

        foreach (var handlerFactory in _handlerFactories)
        {
            var handler = handlerFactory();
            if (handler.CanHandle(valueType))
            {
                handler.CreateInput(entry, parent, entryKey, currentValue, onValueChanged);
                return handler;
            }
        }
        return null;
    }

    public void RegisterHandler(Func<IPreferenceInputHandler> handlerFactory)
    {
        _handlerFactories.Insert(_handlerFactories.Count - 1, handlerFactory);
    }
}