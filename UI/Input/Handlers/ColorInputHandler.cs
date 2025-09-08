using System;
using System.Reflection;
using MelonLoader;
using S1API.Input;
using S1API.Internal.Abstraction;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace ModsApp.UI.Input.Handlers;

public class ColorInputHandler : IPreferenceInputHandler
{
    private readonly UITheme _theme;
    private readonly MelonLogger.Instance _logger;
    public static GameObject ColorPickerCanvas;

    public ColorInputHandler(UITheme theme, MelonLogger.Instance logger)
    {
        _theme = theme;
        _logger = logger;
    }

    public bool CanHandle(Type valueType) => valueType == typeof(Color);

    public void CreateInput(MelonPreferences_Entry entry, GameObject parent, string entryKey,
        object currentValue, Action<string, object> onValueChanged)
    {
        var colorValue = currentValue is Color c ? c : Color.white;

        var colorButtonGO = new GameObject($"{entryKey}_ColorButton");
        colorButtonGO.transform.SetParent(parent.transform, false);

        var buttonImage = colorButtonGO.AddComponent<Image>();
        buttonImage.color = colorValue;

        var button = colorButtonGO.AddComponent<Button>();
        EventHelper.AddListener(() =>
        {
            ShowColorPickerWheel(colorValue, newColor =>
            {
                if (newColor == colorValue) return;

                colorValue = newColor;
                buttonImage.color = colorValue;
                onValueChanged(entryKey, colorValue);
                _logger.Msg($"Modified preference {entryKey}: {ColorUtility.ToHtmlStringRGBA(colorValue)}");
            });
        }, button.onClick);

        var layout = colorButtonGO.AddComponent<LayoutElement>();
        layout.minWidth = 50;
        layout.minHeight = 20;

        colorButtonGO.AddComponent<RectTransform>();
    }

    private void ShowColorPickerWheel(Color initialColor, Action<Color> onColorSelected)
    {
        ColorPickerCanvas = new GameObject("ColorPickerCanvas");
        var canvas = ColorPickerCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        ColorPickerCanvas.AddComponent<CanvasScaler>();
        ColorPickerCanvas.AddComponent<GraphicRaycaster>();

        var panelGO = new GameObject("ColorPickerPanel");
        panelGO.transform.SetParent(ColorPickerCanvas.transform, false);
        var panelRT = panelGO.AddComponent<RectTransform>();
        panelRT.sizeDelta = new Vector2(350, 450);
        panelRT.anchorMin = panelRT.anchorMax = new Vector2(0.5f, 0.5f);
        panelRT.anchoredPosition = Vector2.zero;

        var panelImage = panelGO.AddComponent<Image>();
        panelImage.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);

        var previewGO = new GameObject("Preview");
        previewGO.transform.SetParent(panelGO.transform, false);
        var previewRT = previewGO.AddComponent<RectTransform>();
        previewRT.sizeDelta = new Vector2(100, 50);
        previewRT.anchoredPosition = new Vector2(0, 180);
        var previewImage = previewGO.AddComponent<Image>();
        previewImage.color = initialColor;

        Controls.IsTyping = true;
        CreateColorWheel(panelGO.transform, 200, color =>
        {
            initialColor.r = color.r;
            initialColor.g = color.g;
            initialColor.b = color.b;
            previewImage.color = initialColor;
        });

        CreateSlider(panelGO.transform, "A", initialColor.a, new Vector2(0, -130), value =>
        {
            initialColor.a = value;
            previewImage.color = initialColor;
        });

        CreateButton(panelGO.transform, "Cancel", Color.red, new Vector2(-100, -180),
            () =>
            {
                GameObject.Destroy(ColorPickerCanvas);
                Controls.IsTyping = false;
            });
        CreateButton(panelGO.transform, "Apply", Color.green, new Vector2(100, -180),
            () =>
            {
                onColorSelected(initialColor);
                GameObject.Destroy(ColorPickerCanvas);
                Controls.IsTyping = false;
            });
    }

    private void CreateColorWheel(Transform parent, float size, Action<Color> onColorChanged)
    {
        var wheelGO = new GameObject("ColorWheel");
        wheelGO.transform.SetParent(parent, false);
        var wheelRT = wheelGO.AddComponent<RectTransform>();
        wheelRT.sizeDelta = new Vector2(size, size);

        var wheelImage = wheelGO.AddComponent<RawImage>();
        wheelImage.texture = GenerateColorWheelTexture((int)size);

        var knobGO = new GameObject("Knob");
        knobGO.transform.SetParent(wheelGO.transform, false);
        var knobImage = knobGO.AddComponent<Image>();
        knobImage.sprite = GenerateCircleSprite(16, Color.white);
        knobImage.type = Image.Type.Simple;
        knobGO.GetComponent<RectTransform>().sizeDelta = new Vector2(16, 16);

        var knobFillGO = new GameObject("Fill");
        knobFillGO.transform.SetParent(knobGO.transform, false);
        var knobFill = knobFillGO.AddComponent<Image>();
        knobFill.sprite = GenerateCircleSprite(10, Color.white);
        knobFill.type = Image.Type.Simple;
        knobFillGO.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
        knobFillGO.GetComponent<RectTransform>().sizeDelta = new Vector2(10, 10);

        var trigger = wheelGO.AddComponent<EventTrigger>();

        void HandleEvent(PointerEventData eventData)
        {
            Vector2 screenPos = eventData.position;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(wheelRT, screenPos, null, out var local))
                return;

            var radius = size / 2f;
            var norm = local / radius;
            if (norm.magnitude > 1f) norm = norm.normalized;

            var hue = Mathf.Atan2(norm.y, norm.x) / (2f * Mathf.PI);
            if (hue < 0) hue += 1f;
            var sat = Mathf.Min(norm.magnitude, 1f);

            Color color = Color.HSVToRGB(hue, sat, 1f);
            onColorChanged?.Invoke(color);

            knobGO.GetComponent<RectTransform>().anchoredPosition = norm * radius;
            knobFill.color = color;
        }
        
        // Inline lambda that enforces PointerEventData before calling HandleEvent
        EventHelper.AddEventTrigger(trigger, EventTriggerType.PointerDown, (BaseEventData e) => {
            if (e is PointerEventData pe)
                HandleEvent(pe);
            else
                HandleEvent(new PointerEventData(EventSystem.current) { position = UnityEngine.Input.mousePosition });
        });

        EventHelper.AddEventTrigger(trigger, EventTriggerType.Drag, (BaseEventData e) => {
            if (e is PointerEventData pe)
                HandleEvent(pe);
            else
                HandleEvent(new PointerEventData(EventSystem.current) { position = UnityEngine.Input.mousePosition });
        });

    }

    private Texture2D GenerateColorWheelTexture(int size)
    {
        var tex = new Texture2D(size, size);
        tex.wrapMode = TextureWrapMode.Clamp;

        var radius = size / 2f;
        var center = new Vector2(radius, radius);

        for (var y = 0; y < size; y++)
        for (var x = 0; x < size; x++)
        {
            var pos = new Vector2(x, y) - center;
            var r = pos.magnitude / radius;
            if (r > 1)
            {
                tex.SetPixel(x, y, Color.clear);
                continue;
            }

            var hue = Mathf.Atan2(pos.y, pos.x) / (2f * Mathf.PI);
            if (hue < 0) hue += 1f;
            var sat = r;
            tex.SetPixel(x, y, Color.HSVToRGB(hue, sat, 1f));
        }

        tex.Apply();
        return tex;
    }

    private Sprite GenerateCircleSprite(int size, Color color)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        var radius = size / 2f;
        var center = new Vector2(radius, radius);

        for (var y = 0; y < size; y++)
        for (var x = 0; x < size; x++)
        {
            var dist = Vector2.Distance(new Vector2(x, y), center);
            tex.SetPixel(x, y, dist <= radius ? color : Color.clear);
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    private void CreateSlider(Transform parent, string label, float initialValue, Vector2 anchoredPosition,
        Action<float> onChanged)
    {
        var sliderGO = new GameObject($"{label}Slider");
        sliderGO.transform.SetParent(parent, false);
        var sliderRT = sliderGO.AddComponent<RectTransform>();
        sliderRT.sizeDelta = new Vector2(200, 20);
        sliderRT.anchoredPosition = anchoredPosition;

        var slider = sliderGO.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = initialValue;
        EventHelper.AddListener<float>((v) => onChanged(v), slider.onValueChanged);

        var bgGO = new GameObject("Background");
        bgGO.transform.SetParent(sliderGO.transform, false);
        var bg = bgGO.AddComponent<Image>();
        bg.color = new Color(0.3f, 0.3f, 0.3f, 1f);
        var bgRT = bgGO.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = Vector2.zero;
        bgRT.offsetMax = Vector2.zero;
        slider.targetGraphic = bg;

        var fillAreaGO = new GameObject("Fill Area");
        fillAreaGO.transform.SetParent(sliderGO.transform, false);
        var fillAreaRT = fillAreaGO.AddComponent<RectTransform>();
        fillAreaRT.anchorMin = Vector2.zero;
        fillAreaRT.anchorMax = Vector2.one;
        fillAreaRT.offsetMin = new Vector2(5, 0);
        fillAreaRT.offsetMax = new Vector2(-5, 0);

        var fillGO = new GameObject("Fill");
        fillGO.transform.SetParent(fillAreaGO.transform, false);
        var fill = fillGO.AddComponent<Image>();
        fill.color = Color.green;
        slider.fillRect = fill.GetComponent<RectTransform>();
        var fillRT = fillGO.GetComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = Vector2.one;
        fillRT.offsetMin = Vector2.zero;
        fillRT.offsetMax = Vector2.zero;

        var handleGO = new GameObject("Handle");
        handleGO.transform.SetParent(sliderGO.transform, false);
        var handle = handleGO.AddComponent<Image>();
        handle.color = Color.white;
        slider.handleRect = handle.GetComponent<RectTransform>();
        slider.direction = Slider.Direction.LeftToRight;
        handleGO.GetComponent<RectTransform>().sizeDelta = new Vector2(20, 20);

        var textGO = new GameObject($"{label}Label");
        textGO.transform.SetParent(sliderGO.transform, false);
        var textRT = textGO.AddComponent<RectTransform>();
        textRT.sizeDelta = new Vector2(40, 20);
        textRT.anchoredPosition = new Vector2(-120, 0);

        var text = textGO.AddComponent<Text>();
        text.text = label;
        text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleLeft;
        text.fontSize = 14;
    }

    private void CreateButton(Transform parent, string label, Color color, Vector2 anchoredPosition, Action onClick)
    {
        var buttonGO = new GameObject($"{label}Button");
        buttonGO.transform.SetParent(parent, false);
        var rt = buttonGO.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(80, 30);
        rt.anchoredPosition = anchoredPosition;

        var image = buttonGO.AddComponent<Image>();
        image.color = color;

        var btn = buttonGO.AddComponent<Button>();
        EventHelper.AddListener(() => onClick(), btn.onClick);

        var textGO = new GameObject("Text");
        textGO.transform.SetParent(buttonGO.transform, false);
        var textRT = textGO.AddComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;

        var text = textGO.AddComponent<Text>();
        text.text = label;
        text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.color = Color.black;
        text.alignment = TextAnchor.MiddleCenter;
        text.fontSize = 14;
    }
}