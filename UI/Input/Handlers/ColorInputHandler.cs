using MelonLoader;
using ModsApp.Helpers;
using ModsApp.UI;
using ModsApp.UI.Input.FieldFactories;
using ModsApp.UI.Panels;
using S1API.Input;
using S1API.Internal.Abstraction;
using S1API.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ModsApp.UI.Input.Handlers;

public class ColorInputHandler : IPreferenceInputHandler
{
    private readonly UITheme _theme;
    private readonly MelonLogger.Instance _logger;

    private GameObject _parent;
    private string _entryKey;
    private MelonPreferences_Entry _entry;
    private Action<string, object> _onValueChanged;
    private GameObject _colorButtonGO;

    public ColorInputHandler(UITheme theme, MelonLogger.Instance logger)
    {
        _theme = theme;
        _logger = logger;
    }

    public bool CanHandle(Type valueType) => valueType == typeof(Color);

    public void CreateInput(MelonPreferences_Entry entry, GameObject parent, string entryKey,
        object currentValue, Action<string, object> onValueChanged)
    {
        _parent = parent;
        _entryKey = entryKey;
        _onValueChanged = onValueChanged;
        _entry = entry;

        var colorValue = currentValue is Color c ? c : Color.white;

        _colorButtonGO = new GameObject($"{entryKey}_ColorButton");
        _colorButtonGO.transform.SetParent(parent.transform, false);

        var buttonImage = _colorButtonGO.AddComponent<Image>();
        buttonImage.color = colorValue;
        var btnOutline = _colorButtonGO.AddComponent<Outline>();
        btnOutline.effectColor = _theme.BgInput;
        btnOutline.effectDistance = new Vector2(1.5f, 1.5f);

        var button = _colorButtonGO.AddComponent<Button>();
        EventHelper.AddListener(() =>
        {
            ShowColorPicker(colorValue, newColor =>
            {
                if (newColor == colorValue) return;
                colorValue = newColor;
                buttonImage.color = colorValue;
                onValueChanged(entryKey, colorValue);
                _logger.Msg($"Modified preference {entryKey}: #{ColorUtility.ToHtmlStringRGBA(colorValue)}");
            });
        }, button.onClick);

        var layout = _colorButtonGO.AddComponent<LayoutElement>();
        layout.minWidth = 50;
        layout.minHeight = 20;

        _colorButtonGO.GetOrAddComponent<RectTransform>();

        Tooltip.Attach(_colorButtonGO, "Open color picker");
    }

    public void Recreate(object currentValue)
    {
        FloatingPanelComponent.Cleanup();
        if (_colorButtonGO != null)
            UnityEngine.Object.DestroyImmediate(_colorButtonGO);

        CreateInput(_entry, _parent, _entryKey, currentValue, _onValueChanged);
    }


    private void ShowColorPicker(Color initialColor, Action<Color> onColorSelected)
    {
        var current = initialColor;
        Color.RGBToHSV(current, out var initH, out var initS, out var initV);

        var panel = new FloatingPanelComponent(460, 560, "Color Picker");
        var content = panel.ContentPanel.transform;

        var vlg = panel.ContentPanel.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 4;
        vlg.padding = new RectOffset(8, 8, 6, 6);
        vlg.childControlWidth = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlHeight = true;

        var previewGO = new GameObject("ColorPreview");
        previewGO.transform.SetParent(content, false);

        var previewImage = previewGO.AddComponent<Image>();
        previewImage.color = current;
        previewGO.AddComponent<LayoutElement>().preferredHeight = 28;
        var prevOutline = previewGO.AddComponent<Outline>();
        prevOutline.effectColor = _theme.BgInput;
        prevOutline.effectDistance = new Vector2(1, 1);

        // declared here for sync callbacks
        Slider rSlider = null, gSlider = null, bSlider = null, aSlider = null;
        InputField rField = null, gField = null, bField = null, aField = null;
        InputField hexField = null;
        RectTransform knobRT = null;
        Image knobFillImg = null;
        Image knobBorderImg = null;
        Slider vSlider = null;

        var currentV = initV;
        var syncing = false;

        void SyncAll()
        {
            if (syncing) return;
            syncing = true;

            previewImage.color = current;

            if (rSlider != null) rSlider.value = current.r;
            if (gSlider != null) gSlider.value = current.g;
            if (bSlider != null) bSlider.value = current.b;
            if (aSlider != null) aSlider.value = current.a;

            if (rField != null) rField.text = current.r.ToString("F2");
            if (gField != null) gField.text = current.g.ToString("F2");
            if (bField != null) bField.text = current.b.ToString("F2");
            if (aField != null) aField.text = current.a.ToString("F2");

            if (hexField != null) hexField.text = ColorUtility.ToHtmlStringRGBA(current);

            Color.RGBToHSV(current, out var h, out var s, out var v);
            currentV = v;

            if (vSlider != null) vSlider.SetValueWithoutNotify(currentV);

            if (knobRT != null)
            {
                var angle = h * 2f * Mathf.PI;
                const float radius = 120f;
                knobRT.anchoredPosition = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * s * radius;
            }

            if (knobFillImg != null)
                knobFillImg.color = Color.HSVToRGB(h, s, currentV);
            if (knobBorderImg != null)
                knobBorderImg.color = Color.black;

            syncing = false;
        }

        var wheelSlot = new GameObject("WheelSlot");
        wheelSlot.transform.SetParent(content, false);
        wheelSlot.AddComponent<RectTransform>();
        var wheelSlotLE = wheelSlot.AddComponent<LayoutElement>();
        wheelSlotLE.preferredHeight = 260;
        wheelSlotLE.flexibleHeight = 0;

        var wheelHLG = wheelSlot.AddComponent<HorizontalLayoutGroup>();
        wheelHLG.spacing = 24;
        wheelHLG.childAlignment = TextAnchor.MiddleRight;
        wheelHLG.childControlWidth = false;
        wheelHLG.childForceExpandWidth = false;
        wheelHLG.childForceExpandHeight = false;
        wheelHLG.childControlHeight = false;

        const float wheelSize = 240f;
        const float wheelRadius = wheelSize / 2f;
        const float vSliderW = 18f;

        var wheelGO = new GameObject("ColorWheel");
        wheelGO.transform.SetParent(wheelSlot.transform, false);
        var wheelRT = wheelGO.AddComponent<RectTransform>();
        wheelRT.sizeDelta = new Vector2(wheelSize, wheelSize);
        var wheelLE = wheelGO.AddComponent<LayoutElement>();
        wheelLE.preferredWidth = wheelSize;
        wheelLE.preferredHeight = wheelSize;

        var wheelImage = wheelGO.AddComponent<RawImage>();
        wheelImage.texture = GenerateColorWheelTexture((int)wheelSize);

        var knobGO = new GameObject("Knob");
        knobGO.transform.SetParent(wheelGO.transform, false);
        var knobImg = knobGO.AddComponent<Image>();
        knobImg.sprite = GenerateCircleSprite(16, Color.black);
        knobImg.type = Image.Type.Simple;
        knobRT = knobGO.GetComponent<RectTransform>();
        knobRT.sizeDelta = new Vector2(16, 16);
        knobBorderImg = knobImg;

        var knobFillGO = new GameObject("Fill");
        knobFillGO.transform.SetParent(knobGO.transform, false);
        var knobFill = knobFillGO.AddComponent<Image>();
        knobFill.sprite = GenerateCircleSprite(10, Color.white);
        knobFill.type = Image.Type.Simple;
        knobFillImg = knobFill;
        var knobFillRT = knobFillGO.GetComponent<RectTransform>();
        knobFillRT.anchoredPosition = Vector2.zero;
        knobFillRT.sizeDelta = new Vector2(10, 10);

        var vRow = new GameObject("VSliderRow");
        vRow.transform.SetParent(wheelSlot.transform, false);

        var vRowHLG = vRow.AddComponent<HorizontalLayoutGroup>();
        vRowHLG.spacing = 8;
        vRowHLG.childAlignment = TextAnchor.MiddleCenter;
        vRowHLG.childControlWidth = false;
        vRowHLG.childForceExpandWidth = false;
        vRowHLG.childForceExpandHeight = false;
        vRowHLG.childControlHeight = false;

        vSlider = SliderFactory.CreateVerticalSlider(
            vRow.transform, "BrightnessSlider",
            0f, 1f, initV,
            vSliderW, wheelSize,
            v =>
            {
                if (syncing) return;
                currentV = v;
                Color.RGBToHSV(current, out var h, out var s, out _);
                var rgb = Color.HSVToRGB(h, s, currentV);
                current.r = rgb.r;
                current.g = rgb.g;
                current.b = rgb.b;
                if (knobFillImg != null) knobFillImg.color = Color.HSVToRGB(h, s, currentV);
                SyncAll();
            });

        var vLabel = UIFactory.Text(
            "VLabel",
            "V",
            vRow.transform,
            _theme.SizeSmall,
            TextAnchor.MiddleLeft
        );
        vLabel.color = _theme.TextSecondary;
        vLabel.gameObject.GetOrAddComponent<LayoutElement>().preferredWidth = 12;

        var canvas = wheelGO.GetComponentInParent<Canvas>();
        var worldCamera = canvas != null ? canvas.worldCamera : null;
        var trigger = wheelGO.AddComponent<EventTrigger>();

        void HandleWheel(PointerEventData eventData)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    wheelRT, eventData.position, worldCamera, out var local))
                return;

            var norm = local / wheelRadius;
            if (norm.magnitude > 1f) norm = norm.normalized;

            var hue = Mathf.Atan2(norm.y, norm.x) / (2f * Mathf.PI);
            if (hue < 0) hue += 1f;
            var sat = Mathf.Min(norm.magnitude, 1f);

            var rgb = Color.HSVToRGB(hue, sat, currentV);
            current.r = rgb.r;
            current.g = rgb.g;
            current.b = rgb.b;

            knobRT.anchoredPosition = norm * wheelRadius;
            knobFill.color = Color.HSVToRGB(hue, sat, currentV);

            SyncAll();
        }

        EventHelper.AddEventTrigger(trigger, EventTriggerType.PointerDown, e =>
        {
            if (e is PointerEventData pe) HandleWheel(pe);
            else HandleWheel(new PointerEventData(EventSystem.current) { position = UnityEngine.Input.mousePosition });
        });
        EventHelper.AddEventTrigger(trigger, EventTriggerType.Drag, e =>
        {
            if (e is PointerEventData pe) HandleWheel(pe);
            else HandleWheel(new PointerEventData(EventSystem.current) { position = UnityEngine.Input.mousePosition });
        });

        (Slider s, InputField f) AddLabeledSlider(string label, float initial, Action<float> onChanged)
        {
            var row = new GameObject($"{label}Row");
            row.transform.SetParent(content, false);
            row.AddComponent<RectTransform>();
            var rowLE = row.AddComponent<LayoutElement>();
            rowLE.preferredHeight = 20;
            rowLE.flexibleHeight = 0;
            var rowHLG = row.AddComponent<HorizontalLayoutGroup>();
            rowHLG.spacing = 6;
            rowHLG.childControlWidth = true;
            rowHLG.childForceExpandWidth = false;
            rowHLG.childForceExpandHeight = false;
            rowHLG.childControlHeight = true;

            var lbl = UIFactory.Text($"{label}Lbl", label, row.transform, _theme.SizeSmall, TextAnchor.MiddleLeft);
            lbl.color = _theme.TextSecondary;
            lbl.gameObject.GetOrAddComponent<LayoutElement>().preferredWidth = 14;

            var (_, slider, field) = SliderFactory.CreateSlider(
                row, $"{label}Slider",
                0f, 1f, initial,
                false, InputField.ContentType.DecimalNumber,
                t => float.TryParse(t, out var v) && v >= 0f && v <= 1f,
                onChanged);

            slider.gameObject.GetOrAddComponent<LayoutElement>().flexibleWidth = 1;
            field.gameObject.GetOrAddComponent<LayoutElement>().preferredWidth = 48;

            return (slider, field);
        }

        syncing = true;
        (rSlider, rField) = AddLabeledSlider("R", current.r, v =>
        {
            current.r = v;
            SyncAll();
        });
        (gSlider, gField) = AddLabeledSlider("G", current.g, v =>
        {
            current.g = v;
            SyncAll();
        });
        (bSlider, bField) = AddLabeledSlider("B", current.b, v =>
        {
            current.b = v;
            SyncAll();
        });
        (aSlider, aField) = AddLabeledSlider("A", current.a, v =>
        {
            current.a = v;
            SyncAll();
        });
        syncing = false;

        var hexRow = new GameObject("HexRow");
        hexRow.transform.SetParent(content, false);
        hexRow.AddComponent<RectTransform>();
        var hexLayout = hexRow.AddComponent<HorizontalLayoutGroup>();
        hexLayout.spacing = 6;
        hexLayout.childControlWidth = true;
        hexLayout.childForceExpandWidth = false;
        hexLayout.childForceExpandHeight = false;
        hexLayout.childControlHeight = true;
        hexRow.AddComponent<LayoutElement>().preferredHeight = 22;

        var hexLabel = UIFactory.Text("HexLabel", "#", hexRow.transform, _theme.SizeStandard);
        hexLabel.color = _theme.TextSecondary;
        hexLabel.gameObject.GetOrAddComponent<LayoutElement>().preferredWidth = 12;

        hexField = InputFieldFactory.CreateInputField(
            hexRow, "HexInput",
            ColorUtility.ToHtmlStringRGBA(current),
            InputField.ContentType.Standard, 160);
        hexField.gameObject.GetOrAddComponent<LayoutElement>().flexibleWidth = 1;

        EventHelper.AddListener<string>(text =>
        {
            if (ColorUtility.TryParseHtmlString("#" + text.TrimStart('#'), out var parsed))
            {
                current = parsed;
                SyncAll();
            }
        }, hexField.onEndEdit);

        var btnRow = new GameObject("ButtonRow");
        btnRow.transform.SetParent(content, false);
        btnRow.AddComponent<RectTransform>();
        var btnLayout = btnRow.AddComponent<HorizontalLayoutGroup>();
        btnLayout.spacing = 8;
        btnLayout.childAlignment = TextAnchor.MiddleCenter;
        btnLayout.childForceExpandWidth = false;
        btnLayout.childForceExpandHeight = false;
        btnLayout.childControlWidth = true;
        btnLayout.childControlHeight = true;
        btnRow.AddComponent<LayoutElement>().preferredHeight = 30;

        var (_, applyBtn, _) = UIFactory.RoundedButtonWithLabel(
            "ApplyBtn", "Apply", btnRow.transform,
            _theme.AccentPrimary, 100, 30, _theme.SizeStandard, _theme.TextPrimary);
        EventHelper.AddListener(() =>
        {
            onColorSelected(current);
            Controls.IsTyping = false;
            FloatingPanelComponent.Cleanup();
        }, applyBtn.onClick);

        var (_, cancelBtn, _) = UIFactory.RoundedButtonWithLabel(
            "CancelBtn", "Cancel", btnRow.transform,
            _theme.WarningColor, 100, 30, _theme.SizeStandard, _theme.TextPrimary);
        var empty = "";
        EventHelper.AddListener(() =>
        {
            empty = empty;
            Controls.IsTyping = false;
            FloatingPanelComponent.Cleanup();
        }, cancelBtn.onClick);

        Controls.IsTyping = true;

        // try restoring current - slider quantization might have skewed the values
        current = initialColor;
        SyncAll();
    }


    private static Texture2D GenerateColorWheelTexture(int size)
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
            tex.SetPixel(x, y, Color.HSVToRGB(hue, r, 1f));
        }

        tex.Apply();
        return tex;
    }

    private static Sprite GenerateCircleSprite(int size, Color color)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        var radius = size / 2f;
        var center = new Vector2(radius, radius);

        for (var y = 0; y < size; y++)
        for (var x = 0; x < size; x++)
            tex.SetPixel(x, y,
                Vector2.Distance(new Vector2(x, y), center) <= radius ? color : Color.clear);

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }
}