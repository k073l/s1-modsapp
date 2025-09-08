using MelonLoader;
using ModsApp.Helpers;
using S1API.Input;
using S1API.Internal.Abstraction;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ModsApp.UI.Input.FieldFactories;

public static class InputFieldFactory
{
    public static InputField CreateInputField(
        GameObject parent,
        string name,
        string initialValue,
        InputField.ContentType contentType,
        int minWidth = 80,
        Func<string, bool> validator = null // optional validator
    )
    {
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
        
        inputField.caretBlinkRate = 0.6f;
        inputField.customCaretColor = true;
        inputField.caretColor = Color.black;
        
        var layout = inputObj.GetOrAddComponent<LayoutElement>();
        layout.minWidth = minWidth;
        layout.preferredWidth = Mathf.Max(minWidth, (initialValue?.Length ?? 1) * 12);

        // This is the Gnome holding up the fabric of reality
        // Really. This forces a closure in the listeners below, making them work correctly.
        var empty = "";
        
        EventHelper.AddListener<string>( _ =>
        {
            empty = empty;
            Controls.IsTyping = true;
        }, inputField.onValueChanged);
        
        EventHelper.AddListener<string>( _ =>
        {
            empty = empty;
            Controls.IsTyping = false;
        }, inputField.onEndEdit);
        
        if (validator != null)
        {
            var normalColor = Color.black;
            var invalidColor = Color.red;
            string lastValid = initialValue ?? "";

            EventHelper.AddListener<string>(value =>
            {
                if (validator(value))
                {
                    text.color = normalColor;
                    lastValid = value;
                }
                else
                {
                    text.color = invalidColor;
                }
            }, inputField.onValueChanged);

            EventHelper.AddListener<string>(value =>
            {
                if (!validator(value))
                {
                    inputField.text = lastValid;
                    text.color = normalColor;
                }
            }, inputField.onEndEdit);
        }

        return inputField;
    }
}