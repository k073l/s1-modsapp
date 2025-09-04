using MelonLoader;
using ModsApp;
using ModsApp.Helpers;
using UnityEngine;
using UnityEngine.UI;

namespace ModsApp.UI.Input.Handlers;

public class BooleanInputHandler : IPreferenceInputHandler
{
    private readonly UITheme _theme;
    private readonly MelonLogger.Instance _logger;

    public BooleanInputHandler(UITheme theme, MelonLogger.Instance logger)
    {
        _theme = theme;
        _logger = logger;
    }

    public bool CanHandle(Type valueType) => valueType == typeof(bool);

    public void CreateInput(MelonPreferences_Entry entry, GameObject parent, string entryKey,
        object currentValue, Action<string, object> onValueChanged)
    {
        var boolValue = (bool)currentValue;

        var toggleObj = new GameObject($"{entryKey}_Toggle");
        toggleObj.transform.SetParent(parent.transform, false);

        var bg = toggleObj.AddComponent<Image>();
        bg.color = Color.white;

        var toggle = toggleObj.AddComponent<Toggle>();
        toggle.targetGraphic = bg;

        // Square background
        var rt = toggleObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(20, 20);

        // Checkmark child
        var checkmarkGO = new GameObject("Checkmark");
        checkmarkGO.transform.SetParent(toggleObj.transform, false);
        var checkmarkImg = checkmarkGO.AddComponent<Image>();
        checkmarkImg.color = Color.black;

        var crt = checkmarkGO.GetComponent<RectTransform>();
        crt.anchorMin = new Vector2(0.2f, 0.2f);
        crt.anchorMax = new Vector2(0.8f, 0.8f);
        crt.offsetMin = crt.offsetMax = Vector2.zero;

        toggle.graphic = checkmarkImg;
        toggle.isOn = boolValue;

        toggle.onValueChanged.AddListener(value =>
        {
            if (value == boolValue) return;
            onValueChanged(entryKey, value);
            _logger.Msg($"Modified preference {entryKey}: {value}");
        });

        var layout = toggleObj.GetOrAddComponent<LayoutElement>();
        layout.minWidth = 20;
        layout.minHeight = 20;
        layout.preferredWidth = 20;
        layout.preferredHeight = 20;
    }
}