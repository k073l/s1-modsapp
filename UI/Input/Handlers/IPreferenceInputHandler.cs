using MelonLoader;
using UnityEngine;

namespace ModsApp.UI.Input.Handlers;

public interface IPreferenceInputHandler
{
    bool CanHandle(Type valueType);

    void CreateInput(MelonPreferences_Entry entry, GameObject parent, string entryKey,
        object currentValue, Action<string, object> onValueChanged);

    void Recreate(object currentValue);

    void CreateStandaloneInput(Type valueType, GameObject parent, string entryKey, object currentValue, Action<object> onValueChanged);
}