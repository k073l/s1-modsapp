using System;
using MelonLoader;
using UnityEngine;
using UnityEngine.UI;

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
        
        var colorButtonGO = new GameObject($"{entryKey}_ColorButton", typeof(RectTransform), typeof(Button), typeof(Image));
        colorButtonGO.transform.SetParent(parent.transform, false);
        var buttonImage = colorButtonGO.GetComponent<Image>();
        buttonImage.color = colorValue;

        var button = colorButtonGO.GetComponent<Button>();
        button.onClick.AddListener(() =>
        {
            ShowColorPicker(colorValue, newColor =>
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

    private void ShowColorPicker(Color initialColor, Action<Color> onColorSelected)
    {
        var canvasGO = new GameObject("ColorPickerCanvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();
        
        var panelGO = new GameObject("ColorPickerPanel");
        panelGO.transform.SetParent(canvasGO.transform, false);
        var panelRT = panelGO.AddComponent<RectTransform>();
        panelRT.sizeDelta = new Vector2(300, 400);
        panelRT.anchorMin = new Vector2(0.5f, 0.5f);
        panelRT.anchorMax = new Vector2(0.5f, 0.5f);
        panelRT.anchoredPosition = Vector2.zero;

        var panelImage = panelGO.AddComponent<Image>();
        panelImage.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);
        
        var previewGO = new GameObject("Preview");
        previewGO.transform.SetParent(panelGO.transform, false);
        var previewRT = previewGO.AddComponent<RectTransform>();
        previewRT.sizeDelta = new Vector2(100, 50);
        previewRT.anchoredPosition = new Vector2(0, 160);
        var previewImage = previewGO.AddComponent<Image>();
        previewImage.color = initialColor;
        
        var cancelGO = new GameObject("CancelButton");
        cancelGO.transform.SetParent(panelGO.transform, false);
        cancelGO.AddComponent<RectTransform>().sizeDelta = new Vector2(80, 30);
        cancelGO.GetComponent<RectTransform>().anchoredPosition = new Vector2(-80, -160);
        SetupButton(cancelGO, "Cancel", Color.red, () => GameObject.Destroy(canvasGO));
        
        var applyGO = new GameObject("ApplyButton");
        applyGO.transform.SetParent(panelGO.transform, false);
        applyGO.AddComponent<RectTransform>().sizeDelta = new Vector2(80, 30);
        applyGO.GetComponent<RectTransform>().anchoredPosition = new Vector2(80, -160);
        SetupButton(applyGO, "Apply", Color.green, () =>
        {
            onColorSelected(initialColor);
            GameObject.Destroy(canvasGO);
        });
        
        CreateSlider(panelGO.transform, "R", initialColor.r, new Vector2(0, 100), value =>
        {
            initialColor.r = value;
            previewImage.color = initialColor;
        });
        CreateSlider(panelGO.transform, "G", initialColor.g, new Vector2(0, 50), value =>
        {
            initialColor.g = value;
            previewImage.color = initialColor;
        });
        CreateSlider(panelGO.transform, "B", initialColor.b, new Vector2(0, 0), value =>
        {
            initialColor.b = value;
            previewImage.color = initialColor;
        });
        CreateSlider(panelGO.transform, "A", initialColor.a, new Vector2(0, -50), value =>
        {
            initialColor.a = value;
            previewImage.color = initialColor;
        });
    }

    private void CreateSlider(Transform parent, string label, float initialValue, Vector2 anchoredPosition, Action<float> onChanged)
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
        var bgRT = bgGO.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = Vector2.zero;
        bgRT.offsetMax = Vector2.zero;
        var bg = bgGO.GetComponent<Image>();
        bg.color = new Color(0.3f, 0.3f, 0.3f, 1f);
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
}
