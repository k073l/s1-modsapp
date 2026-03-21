using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using System.Collections.Generic;
using MelonLoader;
using ModsApp.Managers;
using S1API.Input;
using S1API.Internal.Abstraction;

namespace ModsApp.UI.Input.FieldFactories;

public static class DropdownFactory
{
    public static DropdownInputComponent<T> CreateDropdownInput<T>(
        GameObject parent,
        string name,
        T initialValue,
        Func<T, string> displaySelector,
        UITheme theme,
        Vector2 containerSize = default,
        int inputFieldWidth = 120,
        Vector2 dropdownSize = default,
        int maxVisibleItems = 4,
        MelonLogger.Instance logger = null)
    {
        if (containerSize == default) containerSize = new Vector2(150, 20);
        if (dropdownSize == default) dropdownSize = new Vector2(150, 110);

        return new DropdownInputComponent<T>(
            parent, name, initialValue, displaySelector, theme,
            containerSize, inputFieldWidth, dropdownSize, maxVisibleItems, logger);
    }
}

public class DropdownInputComponent<T>
{
    public GameObject Container { get; private set; }
    public InputField InputField { get; private set; }
    public Button DropdownButton { get; private set; }
    public DropdownComponent<T> Dropdown { get; private set; }

    private readonly Func<T, string> _displaySelector;
    private readonly MelonLogger.Instance _logger;
    private UITheme _theme;
    private string _lastValidValue;

    public event Action<T> OnValueChanged;
    public event Func<string, IEnumerable<T>> OnFilterItems;
    public event Func<string, T> OnValidateInput;

    public DropdownInputComponent(
        GameObject parent,
        string name,
        T initialValue,
        Func<T, string> displaySelector,
        UITheme theme,
        Vector2 containerSize,
        int inputFieldWidth,
        Vector2 dropdownSize,
        int maxVisibleItems,
        MelonLogger.Instance logger)
    {
        _displaySelector = displaySelector;
        _logger = logger;
        _theme = theme;
        _lastValidValue = displaySelector(initialValue);

        CreateContainer(parent, name, containerSize);
        CreateInputField(name, _lastValidValue, inputFieldWidth);
        CreateDropdownButton(name);
        CreateDropdown(name, dropdownSize, maxVisibleItems);

        WireUpEvents();
    }

    private void CreateContainer(GameObject parent, string name, Vector2 size)
    {
        Container = new GameObject($"{name}_Container");
        Container.transform.SetParent(parent.transform, false);
        var containerRT = Container.AddComponent<RectTransform>();
        containerRT.sizeDelta = size;

        var image = Container.AddComponent<Image>();
        image.color = Color.clear;

        var layout = Container.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 2;
        layout.childControlWidth = false;
        layout.childForceExpandWidth = false;
    }

    private void CreateInputField(string name, string initialValue, int width)
    {
        InputField = InputFieldFactory.CreateInputField(
            Container,
            $"{name}_Input",
            initialValue,
            InputField.ContentType.Standard,
            width,
            validator: ValidateInput
        );
    }

    private bool ValidateInput(string value)
    {
        if (OnValidateInput != null)
        {
            var validated = OnValidateInput(value);
            return validated != null;
        }

        // fallback: any non-empty input is allowed
        return !string.IsNullOrWhiteSpace(value);
    }

    private void CreateDropdownButton(string name)
    {
        var buttonObj = new GameObject($"{name}_DropdownButton");
        buttonObj.transform.SetParent(Container.transform, false);
        var buttonRT = buttonObj.AddComponent<RectTransform>();
        buttonRT.sizeDelta = new Vector2(25, 20);
        var buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.color = _theme.BgCard;
        DropdownButton = buttonObj.AddComponent<Button>();
        DropdownButton.targetGraphic = buttonImage;

        var arrowObj = new GameObject("Arrow");
        arrowObj.transform.SetParent(buttonObj.transform, false);
        var arrowRT = arrowObj.AddComponent<RectTransform>();
        arrowRT.anchorMin = Vector2.zero;
        arrowRT.anchorMax = Vector2.one;
        arrowRT.sizeDelta = Vector2.zero;
        var arrowText = arrowObj.AddComponent<Text>();
        arrowText.text = "▼";
        arrowText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        arrowText.fontSize = UIManager._theme.SizeSmall;
        arrowText.color = _theme.BgInput;
        arrowText.alignment = TextAnchor.MiddleCenter;
    }

    private void CreateDropdown(string name, Vector2 size, int maxVisibleItems)
    {
        Dropdown = new DropdownComponent<T>(Container, name, size, maxVisibleItems, _logger);
    }

    private void WireUpEvents()
    {
        var parentScrollRect = Container.GetComponentInParent<ScrollRect>();
        Dropdown.ParentScrollRect = parentScrollRect;

        EventHelper.AddListener(() =>
        {
            if (Dropdown.IsOpen)
            {
                Dropdown.Close();
            }
            else
            {
                var items = OnFilterItems?.Invoke(InputField.text) ?? [];
                Dropdown.PopulateItems(items, _displaySelector);
                Dropdown.Show(Container.GetComponent<RectTransform>());
            }
        }, DropdownButton.onClick);

        Dropdown.OnItemSelected += (selectedValue) =>
        {
            var displayText = _displaySelector(selectedValue);
            InputField.text = displayText;
            _lastValidValue = displayText;
            OnValueChanged?.Invoke(selectedValue);
        };

        EventHelper.AddListener<string>((value) =>
        {
            if (Dropdown.IsOpen)
                Dropdown.Close();

            if (OnValidateInput != null)
            {
                var validatedValue = OnValidateInput(value);
                if (validatedValue != null)
                {
                    var displayText = _displaySelector(validatedValue);
                    _lastValidValue = displayText;
                    InputField.text = displayText;
                    OnValueChanged?.Invoke(validatedValue);
                    return;
                }
            }

            if (!string.IsNullOrEmpty(value) && value != _lastValidValue)
                InputField.text = _lastValidValue;
        }, InputField.onEndEdit);
    }

    public void SetValue(T value)
    {
        var displayText = _displaySelector(value);
        _lastValidValue = displayText;
        InputField.text = displayText;
    }

    public void Destroy()
    {
        Dropdown?.Destroy();
        if (Container != null)
            UnityEngine.Object.DestroyImmediate(Container);
    }
}

public class DropdownComponent<T>
{
    private static readonly List<Action> _allCloseCallbacks = new();

    public GameObject Panel { get; private set; }
    public bool IsOpen { get; private set; }
    public ScrollRect ParentScrollRect { get; set; }

    private readonly Transform _content;
    private readonly RectTransform _panelRT;
    private readonly ScrollRect _scrollRect;
    private readonly int _maxVisibleItems;
    private Action<BaseEventData> _overlayClickCallback;
    private Action<BaseEventData> _overlayScrollCallback;
    private EventTrigger _overlayTrigger;
    private readonly MelonLogger.Instance _logger;

    private GameObject _overlay;

    public event Action<T> OnItemSelected;
    public event Action OnDropdownOpened;
    public event Action OnDropdownClosed;

    public DropdownComponent(GameObject parent, string name, Vector2 size, int maxVisibleItems,
        MelonLogger.Instance logger)
    {
        _maxVisibleItems = maxVisibleItems;
        _logger = logger;

        Panel = CreateDropdownPanel(parent.transform.root, name, size);
        _content = Panel.transform.Find("Content");
        _panelRT = Panel.GetComponent<RectTransform>();
        _scrollRect = Panel.GetComponent<ScrollRect>();

        _allCloseCallbacks.Add(Close);
    }

    private GameObject CreateDropdownPanel(Transform parent, string name, Vector2 size)
    {
        var panel = new GameObject($"{name}_DropdownPanel");
        panel.transform.SetParent(parent, false);

        var panelRT = panel.AddComponent<RectTransform>();
        panelRT.sizeDelta = size;

        var panelImage = panel.AddComponent<Image>();
        panelImage.color = UIManager._theme.BgInput;

        panel.AddComponent<Mask>();

        var content = new GameObject("Content");
        content.transform.SetParent(panel.transform, false);
        var contentRT = content.AddComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0, 1);
        contentRT.anchorMax = new Vector2(1, 1);
        contentRT.pivot = new Vector2(0.5f, 1f);
        contentRT.anchoredPosition = Vector2.zero;
        contentRT.sizeDelta = new Vector2(0, 0);

        var layout = content.AddComponent<VerticalLayoutGroup>();
        layout.childForceExpandHeight = false;
        layout.childControlHeight = true;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.spacing = 1;

        var fitter = content.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var scrollRect = panel.AddComponent<ScrollRect>();
        scrollRect.scrollSensitivity = 15f;
        scrollRect.horizontal = false;
        scrollRect.content = contentRT;

        panel.SetActive(false);
        return panel;
    }

    public void PopulateItems<TItem>(IEnumerable<TItem> items, Func<TItem, T> valueSelector,
        Func<TItem, string> displaySelector)
    {
        for (var i = _content.childCount - 1; i >= 0; i--)
            UnityEngine.Object.DestroyImmediate(_content.GetChild(i).gameObject);

        var itemList = new List<TItem>(items);

        foreach (var item in itemList)
            CreateDropdownItem(valueSelector(item), displaySelector(item));

        UpdatePanelSize(itemList.Count);
        LayoutRebuilder.ForceRebuildLayoutImmediate(_content.GetComponent<RectTransform>());
    }

    public void PopulateItems(IEnumerable<T> items, Func<T, string> displaySelector)
    {
        PopulateItems(items, item => item, displaySelector);
    }

    private void CreateDropdownItem(T value, string displayText)
    {
        var item = new GameObject($"Item_{displayText}");
        item.transform.SetParent(_content, false);

        var itemRT = item.AddComponent<RectTransform>();
        itemRT.sizeDelta = new Vector2(0, 25);

        var layoutElement = item.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = 25;

        var itemImage = item.AddComponent<Image>();
        itemImage.color = UIManager._theme.InputSecondary * 0.75f;

        var itemButton = item.AddComponent<Button>();
        itemButton.targetGraphic = itemImage;

        var textGO = new GameObject("Text");
        textGO.transform.SetParent(item.transform, false);
        var textRT = textGO.AddComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = new Vector2(5, 0);
        textRT.offsetMax = new Vector2(-5, 0);

        var textComp = textGO.AddComponent<Text>();
        textComp.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        textComp.text = displayText;
        textComp.fontSize = UIManager._theme.SizeStandard;
        textComp.color = UIManager._theme.InputPrimary;
        textComp.alignment = TextAnchor.MiddleLeft;

        // Capture value for closure
        var capturedValue = value;
        EventHelper.AddListener(() =>
        {
            OnItemSelected?.Invoke(capturedValue);
            Close();
        }, itemButton.onClick);
    }

    private void UpdatePanelSize(int itemCount)
    {
        const int itemHeight = 25;
        const int spacing = 1;
        const int padding = 4;

        var visibleCount = Mathf.Min(itemCount, _maxVisibleItems);
        var preferredHeight = (visibleCount * itemHeight) + ((visibleCount - 1) * spacing) + padding;
        _panelRT.sizeDelta = new Vector2(_panelRT.sizeDelta.x, preferredHeight);
    }

    public void Show(RectTransform anchorTransform, Vector2 offset = default)
    {
        if (IsOpen) return;

        var canvas = anchorTransform.GetComponentInParent<Canvas>();
        if (!canvas)
        {
            _logger?.Error("[Dropdown] ERROR: no Canvas found");
            return;
        }

        // Overlay behind the panel catches click/scrolls outside of panel and closes the dropdown
        _overlay = new GameObject("DropdownOverlay");
        _overlay.transform.SetParent(canvas.transform, false);
        var overlayRT = _overlay.AddComponent<RectTransform>();
        overlayRT.anchorMin = Vector2.zero;
        overlayRT.anchorMax = Vector2.one;
        overlayRT.offsetMin = Vector2.zero;
        overlayRT.offsetMax = Vector2.zero;
        var overlayImage = _overlay.AddComponent<Image>();
        overlayImage.color = Color.clear;

        var overlayTrigger = _overlay.AddComponent<EventTrigger>();
        _overlayTrigger = overlayTrigger;

        _overlayClickCallback = (_) => Close();
        _overlayScrollCallback = (data) =>
        {
            Close();
            if (data is PointerEventData pointerData)
                ParentScrollRect?.OnScroll(pointerData);
        };

        EventHelper.AddEventTrigger(overlayTrigger, EventTriggerType.PointerClick, _overlayClickCallback);
        EventHelper.AddEventTrigger(overlayTrigger, EventTriggerType.Scroll, _overlayScrollCallback);

        Panel.SetActive(true);
        IsOpen = true;
        Panel.transform.SetParent(canvas.transform, false);
        Panel.transform.SetAsLastSibling();

        LayoutRebuilder.ForceRebuildLayoutImmediate(_panelRT);

        var rect = anchorTransform.rect;
        var bottomLeft = anchorTransform.TransformPoint(new Vector3(rect.xMin, rect.yMin, 0f));

        var canvasRT = canvas.GetComponent<RectTransform>();
        var canvasCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRT,
            RectTransformUtility.WorldToScreenPoint(canvasCamera, bottomLeft),
            canvasCamera,
            out var localPos
        );

        _panelRT.pivot = new Vector2(0, 1);
        _panelRT.localPosition = localPos + offset + new Vector2(0, -5);

        OnDropdownOpened?.Invoke();
    }

    private void DestroyOverlay()
    {
        if (_overlay == null) return;

        ForgetOverlayListeners();

        _overlayClickCallback = null;
        _overlayScrollCallback = null;
        _overlayTrigger = null;

        _overlay.SetActive(false);
        UnityEngine.Object.Destroy(_overlay);
        _overlay = null;
    }

    // il2cpp didn't want to play nice with accessing on _overlayTrigger.triggers,
    // so we remove manually from the dedup dict
    private void ForgetOverlayListeners()
    {
        if (_overlayClickCallback == null && _overlayScrollCallback == null) return;

        var dict = typeof(EventHelper)
            .GetField("SubscribedGenericActions", // this will blow up one day i'm sure
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
            ?.GetValue(null) as Dictionary<Delegate, Delegate>;

        if (dict == null) return;

        if (_overlayClickCallback != null) dict.Remove(_overlayClickCallback);
        if (_overlayScrollCallback != null) dict.Remove(_overlayScrollCallback);
    }

    public void Close()
    {
        if (!IsOpen) return;

        Panel.SetActive(false);
        IsOpen = false;

        DestroyOverlay();

        OnDropdownClosed?.Invoke();
        Controls.IsTyping = false;
    }

    public void Toggle(RectTransform anchorTransform, Vector2 offset = default)
    {
        if (IsOpen)
            Close();
        else
            Show(anchorTransform, offset);
    }

    public void Destroy()
    {
        _allCloseCallbacks.Remove(Close);
        DestroyOverlay();

        if (Panel != null)
            UnityEngine.Object.DestroyImmediate(Panel);
    }

    public static void CloseAll()
    {
        foreach (var close in _allCloseCallbacks.ToArray())
            close();
    }
}