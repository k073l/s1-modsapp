using MelonLoader;
using ModsApp.Helpers;
using S1API.Input;
using UnityEngine;
using UnityEngine.UI;

namespace ModsApp.UI.Input.FieldFactories;

public static class InputFieldFactory
{
    public static InputField CreateInputField(GameObject parent, string name, string initialValue,
        InputField.ContentType contentType, int minWidth = 80)
    {
        MelonLogger.Msg($"[UI] Creating InputField: {name}, initial='{initialValue}', type={contentType}");

        var inputObj = new GameObject(name);
        inputObj.transform.SetParent(parent.transform, false);
        
        var bg = inputObj.AddComponent<Image>();
        bg.color = new Color(1f, 1f, 1f, 0.9f);

        var inputField = inputObj.AddComponent<InputField>();
        inputField.contentType = contentType;
        inputField.transition = Selectable.Transition.ColorTint;

        var font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        if (font == null)
            MelonLogger.Msg("[UI][WARN] Arial font missing!");
        
        var textGO = new GameObject("Text");
        textGO.transform.SetParent(inputObj.transform, false);
        textGO.AddComponent<RectTransform>();
        var text = textGO.AddComponent<Text>();
        text.font = font;
        text.text = initialValue ?? "";
        text.fontSize = 14;
        text.alignment = TextAnchor.MiddleLeft;
        text.fontStyle = FontStyle.Normal;
        text.color = Color.black;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;

        var textRT = text.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = new Vector2(6, 2);
        textRT.offsetMax = new Vector2(-6, -2);

        inputField.textComponent = text;
        inputField.text = initialValue ?? "";
        MelonLogger.Msg($"[UI] Assigned textComponent with font={font?.name}");
        
        var placeholderGO = new GameObject("Placeholder");
        placeholderGO.transform.SetParent(inputObj.transform, false);
        placeholderGO.AddComponent<RectTransform>();
        var placeholder = placeholderGO.AddComponent<Text>();
        placeholder.font = font;
        placeholder.fontSize = 14;
        placeholder.alignment = TextAnchor.MiddleLeft;
        placeholder.color = new Color(0.5f, 0.5f, 0.5f, 0.75f);
        placeholder.text = "Enter value...";

        var phRT = placeholder.GetComponent<RectTransform>();
        phRT.anchorMin = Vector2.zero;
        phRT.anchorMax = Vector2.one;
        phRT.offsetMin = new Vector2(6, 2);
        phRT.offsetMax = new Vector2(-6, -2);

        inputField.placeholder = placeholder;
        MelonLogger.Msg($"[UI] Assigned placeholder with font={font?.name}");
        
        inputField.caretBlinkRate = 0.6f;
        inputField.customCaretColor = true;
        inputField.caretColor = Color.black;
        
        var layout = inputObj.GetOrAddComponent<LayoutElement>();
        layout.minWidth = minWidth;
        layout.preferredWidth = Mathf.Max(minWidth, (initialValue?.Length ?? 1) * 12);

        inputField.onValueChanged.AddListener(_ => Controls.IsTyping = true);
        inputField.onEndEdit.AddListener(_ => Controls.IsTyping = false);

        return inputField;
    }
}