using System.Globalization;
using ModsApp.Managers;
using S1API.Input;
using S1API.Internal.Abstraction;
using UnityEngine;
using UnityEngine.UI;

namespace ModsApp.UI.Input.FieldFactories;

public static class SliderFactory
{
    public static (
        GameObject container,
        Slider slider,
        InputField field)
        CreateSlider(
            GameObject parent,
            string name,
            float min,
            float max,
            float initialValue,
            bool wholeNumbers,
            InputField.ContentType contentType,
            Func<string, bool> validator,
            Action<float> onChanged)
    {
        var container = new GameObject(name);
        container.transform.SetParent(parent.transform, false);

        var layout = container.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 6;
        layout.childControlWidth = true;
        layout.childForceExpandWidth = false;

        // slider root
        var sliderGO = new GameObject("Slider");
        sliderGO.transform.SetParent(container.transform, false);

        var sliderRT = sliderGO.AddComponent<RectTransform>();
        sliderRT.sizeDelta = new Vector2(200, 10);

        var slider = sliderGO.AddComponent<Slider>();
        slider.minValue = min;
        slider.maxValue = max;
        slider.wholeNumbers = wholeNumbers;
        slider.direction = Slider.Direction.LeftToRight;

        var sliderLE = sliderGO.AddComponent<LayoutElement>();
        sliderLE.flexibleWidth = 1f;

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

        // fill area
        var fillAreaGO = new GameObject("Fill Area");
        fillAreaGO.transform.SetParent(sliderGO.transform, false);

        var fillAreaRT = fillAreaGO.AddComponent<RectTransform>();
        fillAreaRT.anchorMin = Vector2.zero;
        fillAreaRT.anchorMax = Vector2.one;
        fillAreaRT.offsetMin = new Vector2(1, 0);
        fillAreaRT.offsetMax = new Vector2(-5, 0);

        var fillGO = new GameObject("Fill");
        fillGO.transform.SetParent(fillAreaGO.transform, false);

        var fill = fillGO.AddComponent<Image>();
        fill.color = UIManager._theme.AccentPrimary;

        var fillRT = fillGO.GetComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = Vector2.one;
        fillRT.offsetMin = Vector2.zero;
        fillRT.offsetMax = Vector2.zero;

        slider.fillRect = fillRT;

        // handle
        var handleGO = new GameObject("Handle");
        handleGO.transform.SetParent(sliderGO.transform, false);

        var handle = handleGO.AddComponent<Image>();
        handle.color = UIManager._theme.BgInput;

        var handleRT = handleGO.GetComponent<RectTransform>();
        handleRT.sizeDelta = new Vector2(10, 10);

        slider.handleRect = handleRT;

        // input field
        var stringValue = wholeNumbers
            ? ((int)initialValue).ToString()
            : initialValue.ToString("G6",
                CultureInfo.InvariantCulture);

        var field = InputFieldFactory.CreateInputField(
            container,
            $"{name}_Input",
            stringValue,
            contentType,
            minWidth: 60,
            validator
        );

        // precision
        var step = wholeNumbers
            ? 1f
            : InferFloatStep(min, max);

        var suppress = false;

        void Apply(float v)
        {
            v = Mathf.Clamp(v, min, max);
            v = Quantize(v, wholeNumbers, step);

            suppress = true;

            slider.value = v;

            field.text = wholeNumbers
                ? ((int)v).ToString()
                : v.ToString("G6",
                    CultureInfo.InvariantCulture);
            Controls.IsTyping = false;

            onChanged?.Invoke(v);

            suppress = false;
        }

        EventHelper.AddListener(v =>
        {
            if (!suppress)
                Apply(v);
        }, slider.onValueChanged);

        EventHelper.AddListener(text =>
        {
            if (suppress) return;

            if (!float.TryParse(
                    text,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out var v))
                return;

            Apply(v);
        }, field.onEndEdit);

        Apply(initialValue);

        return (container, slider, field);
    }

    public static Slider CreateVerticalSlider(
        Transform parent,
        string name,
        float min,
        float max,
        float initialValue,
        float width,
        float height,
        Action<float> onChanged)
    {
        var sliderGO = new GameObject(name);
        sliderGO.transform.SetParent(parent, false);

        var rt = sliderGO.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(width, height);

        var slider = sliderGO.AddComponent<Slider>();
        slider.minValue = min;
        slider.maxValue = max;
        slider.wholeNumbers = false;
        slider.direction = Slider.Direction.BottomToTop;

        var le = sliderGO.AddComponent<LayoutElement>();
        le.preferredWidth = width;
        le.preferredHeight = height;
        le.flexibleWidth = 0;
        le.flexibleHeight = 0;

        // background
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

        // fill area
        var fillAreaGO = new GameObject("Fill Area");
        fillAreaGO.transform.SetParent(sliderGO.transform, false);
        var fillAreaRT = fillAreaGO.AddComponent<RectTransform>();
        fillAreaRT.anchorMin = Vector2.zero;
        fillAreaRT.anchorMax = Vector2.one;
        fillAreaRT.offsetMin = new Vector2(0, 1);
        fillAreaRT.offsetMax = new Vector2(0, -5);

        var fillGO = new GameObject("Fill");
        fillGO.transform.SetParent(fillAreaGO.transform, false);
        var fill = fillGO.AddComponent<Image>();
        fill.color = UIManager._theme.AccentPrimary;
        var fillRT = fillGO.GetComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = Vector2.one;
        fillRT.offsetMin = Vector2.zero;
        fillRT.offsetMax = Vector2.zero;
        slider.fillRect = fillRT;

        // handle
        var handleGO = new GameObject("Handle");
        handleGO.transform.SetParent(sliderGO.transform, false);
        var handle = handleGO.AddComponent<Image>();
        handle.color = UIManager._theme.BgInput;
        var handleRT = handleGO.GetComponent<RectTransform>();
        handleRT.sizeDelta = new Vector2(width, 10);
        slider.handleRect = handleRT;

        slider.value = Mathf.Clamp(initialValue, min, max);

        EventHelper.AddListener(v => onChanged?.Invoke(v), slider.onValueChanged);

        return slider;
    }

    private static float InferFloatStep(float min, float max)
    {
        var range = Mathf.Abs(max - min);
        if (range <= 0f) return 0.01f;

        var magnitude =
            Mathf.Pow(10f, Mathf.Floor(Mathf.Log10(range)));

        return magnitude / 100f;
    }

    private static float Quantize(float v, bool wholeNumbers = false, float step = 0.01f)
    {
        if (wholeNumbers)
            return Mathf.Round(v);

        return Mathf.Round(v / step) * step;
    }
}