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
        Color colorValue = currentValue is Color c ? c : Color.white;

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
        panelRT.anchorMin = new Vector2(0.5f, 0.5f);
        panelRT.anchorMax = new Vector2(0.5f, 0.5f);
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

        var wheelGO = new GameObject("ColorWheel");
        wheelGO.transform.SetParent(panelGO.transform, false);
        var wheelRT = wheelGO.AddComponent<RectTransform>();
        wheelRT.sizeDelta = new Vector2(200, 200);
        wheelRT.anchoredPosition = new Vector2(0, 50);

        var rawImage = wheelGO.AddComponent<RawImage>();
        rawImage.texture = GenerateColorWheelTexture(200);

        var wheelEvent = wheelGO.AddComponent<WheelPicker>();
        wheelEvent.OnColorChanged = color =>
        {
            initialColor.r = color.r;
            initialColor.g = color.g;
            initialColor.b = color.b;
            previewImage.color = initialColor;
        };

        CreateSlider(panelGO.transform, "A", initialColor.a, new Vector2(0, -80), value =>
        {
            initialColor.a = value;
            previewImage.color = initialColor;
        });

        var cancelGO = new GameObject("CancelButton");
        cancelGO.transform.SetParent(panelGO.transform, false);
        cancelGO.AddComponent<RectTransform>().sizeDelta = new Vector2(80, 30);
        cancelGO.GetComponent<RectTransform>().anchoredPosition = new Vector2(-100, -160);
        SetupButton(cancelGO, "Cancel", Color.red, () => GameObject.Destroy(canvasGO));

        var applyGO = new GameObject("ApplyButton");
        applyGO.transform.SetParent(panelGO.transform, false);
        applyGO.AddComponent<RectTransform>().sizeDelta = new Vector2(80, 30);
        applyGO.GetComponent<RectTransform>().anchoredPosition = new Vector2(100, -160);
        SetupButton(applyGO, "Apply", Color.green, () =>
        {
            onColorSelected(initialColor);
            GameObject.Destroy(canvasGO);
        });
    }

    private Texture2D GenerateColorWheelTexture(int size)
    {
        Texture2D tex = new Texture2D(size, size);
        tex.wrapMode = TextureWrapMode.Clamp;

        float radius = size / 2f;
        Vector2 center = new Vector2(radius, radius);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 pos = new Vector2(x, y) - center;
                float r = pos.magnitude / radius;
                if (r > 1)
                {
                    tex.SetPixel(x, y, Color.clear);
                    continue;
                }

                float hue = Mathf.Atan2(pos.y, pos.x) / (2f * Mathf.PI);
                if (hue < 0) hue += 1f;
                float sat = r;
                Color color = Color.HSVToRGB(hue, sat, 1f);
                tex.SetPixel(x, y, color);
            }
        }

        tex.Apply();
        return tex;
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

    private void SetupButton(GameObject buttonGO, string label, Color color, Action onClick)
    {
        var image = buttonGO.GetComponent<Image>();
        if (!image) image = buttonGO.AddComponent<Image>();
        image.color = color;

        var btn = buttonGO.GetComponent<Button>();
        if (!btn) btn = buttonGO.AddComponent<Button>();
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

    [RegisterTypeInIl2CppWithInterfaces(typeof(IPointerClickHandler),
        typeof(IDragHandler))] // we HOPE that melon will handle this without the constructor
    private class WheelPicker : MonoBehaviour, IPointerDownHandler, IDragHandler
    {
        public Action<Color> OnColorChanged;
        private RectTransform rect;

        private GameObject knobOuter;
        private Image knobInnerImage;

        void Awake()
        {
            rect = GetComponent<RectTransform>();

            // Outer knob
            knobOuter = new GameObject("KnobOuter", typeof(RectTransform), typeof(Image));
            knobOuter.transform.SetParent(transform, false);
            var outerImg = knobOuter.GetComponent<Image>();
            outerImg.sprite = GenerateCircleSprite(16, Color.white);
            outerImg.type = Image.Type.Simple;
            var outerRT = knobOuter.GetComponent<RectTransform>();
            outerRT.sizeDelta = new Vector2(16, 16);
            outerRT.anchorMin = outerRT.anchorMax = new Vector2(0.5f, 0.5f);

            // Inner knob
            var knobInner = new GameObject("KnobInner", typeof(RectTransform), typeof(Image));
            knobInner.transform.SetParent(knobOuter.transform, false);
            knobInnerImage = knobInner.GetComponent<Image>();
            knobInnerImage.sprite = GenerateCircleSprite(10, Color.white); // will update color dynamically
            knobInnerImage.type = Image.Type.Simple;
            var innerRT = knobInner.GetComponent<RectTransform>();
            innerRT.sizeDelta = new Vector2(10, 10);
            innerRT.anchorMin = innerRT.anchorMax = new Vector2(0.5f, 0.5f);
            innerRT.anchoredPosition = Vector2.zero;
        }

        private Sprite GenerateCircleSprite(int size, Color color)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            float radius = size / 2f;
            Vector2 center = new Vector2(radius, radius);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 pos = new Vector2(x, y);
                    float dist = Vector2.Distance(pos, center);
                    tex.SetPixel(x, y, dist <= radius ? color : Color.clear);
                }
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        public void OnPointerDown(PointerEventData eventData) => PickColor(eventData);
        public void OnDrag(PointerEventData eventData) => PickColor(eventData);

        private void PickColor(PointerEventData eventData)
        {
            Vector2 local;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, eventData.position, null, out local))
                return;

            float radius = rect.sizeDelta.x / 2f;
            Vector2 norm = local / radius;
            float dist = norm.magnitude;
            if (dist > 1) norm = norm.normalized; // clamp to edge

            float hue = Mathf.Atan2(norm.y, norm.x) / (2f * Mathf.PI);
            if (hue < 0) hue += 1f;
            float sat = Mathf.Min(dist, 1f);

            Color color = Color.HSVToRGB(hue, sat, 1f);
            OnColorChanged?.Invoke(color);

            knobOuter.GetComponent<RectTransform>().anchoredPosition = norm * radius;

            knobInnerImage.color = color;
        }
    }
}