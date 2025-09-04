using System;
using MelonLoader;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace ModsApp.UI.Input.Handlers;

public class ColorInputHandler : IPreferenceInputHandler
{
    private readonly UITheme _theme;
    private readonly MelonLogger.Instance _logger;

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

        var colorButtonGO =
            new GameObject($"{entryKey}_ColorButton", typeof(RectTransform), typeof(Button), typeof(Image));
        colorButtonGO.transform.SetParent(parent.transform, false);
        var buttonImage = colorButtonGO.GetComponent<Image>();
        buttonImage.color = colorValue;

        var button = colorButtonGO.GetComponent<Button>();
        button.onClick.AddListener(() =>
        {
            ShowColorPickerWheel(colorValue, newColor =>
            {
                if (newColor == colorValue) return;

                colorValue = newColor;
                buttonImage.color = colorValue;
                onValueChanged(entryKey, colorValue);
                _logger.Msg($"Modified preference {entryKey}: {ColorUtility.ToHtmlStringRGBA(colorValue)}");
            });
        });

        var layout = colorButtonGO.AddComponent<LayoutElement>();
        layout.minWidth = 50;
        layout.minHeight = 20;
    }

    private void ShowColorPickerWheel(Color initialColor, Action<Color> onColorSelected)
    {
        var canvasGO = new GameObject("ColorPickerCanvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        var panelGO = new GameObject("ColorPickerPanel");
        panelGO.transform.SetParent(canvasGO.transform, false);
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
            () => GameObject.Destroy(canvasGO));
        CreateButton(panelGO.transform, "Apply", Color.green, new Vector2(100, -180),
            () =>
            {
                onColorSelected(initialColor);
                GameObject.Destroy(canvasGO);
            });
    }

    private void CreateColorWheel(Transform parent, float size, Action<Color> onColorChanged)
    {
        var wheelGO = new GameObject("ColorWheel", typeof(RectTransform));
        wheelGO.transform.SetParent(parent, false);
        var wheelRT = wheelGO.GetComponent<RectTransform>();
        wheelRT.sizeDelta = new Vector2(size, size);

        var wheelImage = wheelGO.AddComponent<RawImage>();
        wheelImage.texture = GenerateColorWheelTexture((int)size);

        // Knob
        var knobGO = new GameObject("Knob", typeof(RectTransform), typeof(Image));
        knobGO.transform.SetParent(wheelGO.transform, false);
        var knobImage = knobGO.GetComponent<Image>();
        knobImage.sprite = GenerateCircleSprite(16, Color.white);
        knobImage.type = Image.Type.Simple;
        knobGO.GetComponent<RectTransform>().sizeDelta = new Vector2(16, 16);
        knobGO.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

        var knobFillGO = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        knobFillGO.transform.SetParent(knobGO.transform, false);
        var knobFill = knobFillGO.GetComponent<Image>();
        knobFill.sprite = GenerateCircleSprite(10, Color.white);
        knobFill.type = Image.Type.Simple;
        knobFillGO.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
        knobFillGO.GetComponent<RectTransform>().sizeDelta = new Vector2(10, 10);

        // EventTrigger for click/drag
        var trigger = wheelGO.AddComponent<EventTrigger>();

        void HandleEvent(PointerEventData eventData)
        {
            Vector2 local;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(wheelRT, eventData.position, null,
                    out local)) return;

            var radius = size / 2f;
            var norm = local / radius;
            var dist = norm.magnitude;
            if (dist > 1f) norm = norm.normalized;

            var hue = Mathf.Atan2(norm.y, norm.x) / (2f * Mathf.PI);
            if (hue < 0) hue += 1f;
            var sat = Mathf.Min(dist, 1f);

            Color color = Color.HSVToRGB(hue, sat, 1f);
            onColorChanged?.Invoke(color);

            knobGO.GetComponent<RectTransform>().anchoredPosition = norm * radius;
            knobFill.color = color;
        }

        var entryDown = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
        entryDown.callback.AddListener((data) => HandleEvent((PointerEventData)data));
        trigger.triggers.Add(entryDown);

        var entryDrag = new EventTrigger.Entry { eventID = EventTriggerType.Drag };
        entryDrag.callback.AddListener((data) => HandleEvent((PointerEventData)data));
        trigger.triggers.Add(entryDrag);
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
            var color = Color.HSVToRGB(hue, sat, 1f);
            tex.SetPixel(x, y, color);
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
            var pos = new Vector2(x, y);
            var dist = Vector2.Distance(pos, center);
            tex.SetPixel(x, y, dist <= radius ? color : Color.clear);
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    private void CreateSlider(Transform parent, string label, float initialValue, Vector2 anchoredPosition,
        Action<float> onChanged)
    {
        var sliderGO = new GameObject($"{label}Slider", typeof(RectTransform));
        sliderGO.transform.SetParent(parent, false);
        var sliderRT = sliderGO.GetComponent<RectTransform>();
        sliderRT.sizeDelta = new Vector2(200, 20);
        sliderRT.anchoredPosition = anchoredPosition;

        var slider = sliderGO.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = initialValue;
        slider.onValueChanged.AddListener(v => onChanged(v));

        var bgGO = new GameObject("Background", typeof(Image));
        bgGO.transform.SetParent(sliderGO.transform, false);
        var bg = bgGO.GetComponent<Image>();
        bg.color = new Color(0.3f, 0.3f, 0.3f, 1f);
        var bgRT = bgGO.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = Vector2.zero;
        bgRT.offsetMax = Vector2.zero;
        slider.targetGraphic = bg;

        var fillAreaGO = new GameObject("Fill Area", typeof(RectTransform));
        fillAreaGO.transform.SetParent(sliderGO.transform, false);
        var fillAreaRT = fillAreaGO.GetComponent<RectTransform>();
        fillAreaRT.anchorMin = new Vector2(0, 0);
        fillAreaRT.anchorMax = new Vector2(1, 1);
        fillAreaRT.offsetMin = new Vector2(5, 0);
        fillAreaRT.offsetMax = new Vector2(-5, 0);

        var fillGO = new GameObject("Fill", typeof(Image));
        fillGO.transform.SetParent(fillAreaGO.transform, false);
        var fill = fillGO.GetComponent<Image>();
        fill.color = Color.green;
        slider.fillRect = fill.GetComponent<RectTransform>();
        var fillRT = fillGO.GetComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = Vector2.one;
        fillRT.offsetMin = Vector2.zero;
        fillRT.offsetMax = Vector2.zero;

        var handleGO = new GameObject("Handle", typeof(Image));
        handleGO.transform.SetParent(sliderGO.transform, false);
        var handle = handleGO.GetComponent<Image>();
        handle.color = Color.white;
        slider.handleRect = handle.GetComponent<RectTransform>();
        slider.direction = Slider.Direction.LeftToRight;
        var handleRT = handleGO.GetComponent<RectTransform>();
        handleRT.sizeDelta = new Vector2(20, 20);

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
        btn.onClick.AddListener(() => onClick());

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