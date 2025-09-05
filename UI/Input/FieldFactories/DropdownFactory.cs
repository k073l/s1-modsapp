using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using MelonLoader;
using S1API.Input;

namespace ModsApp.UI.Input.FieldFactories;

public static class DropdownFactory
{
    public static DropdownInputComponent<T> CreateDropdownInput<T>(
        GameObject parent,
        string name,
        T initialValue,
        Func<T, string> displaySelector,
        Vector2 containerSize = default,
        int inputFieldWidth = 120,
        Vector2 dropdownSize = default,
        int maxVisibleItems = 4,
        MelonLogger.Instance logger = null)
    {
        if (containerSize == default) containerSize = new Vector2(150, 20);
        if (dropdownSize == default) dropdownSize = new Vector2(150, 110);

        return new DropdownInputComponent<T>(
            parent, name, initialValue, displaySelector,
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
    private string _lastValidValue;

    public event Action<T> OnValueChanged;
    public event Func<string, IEnumerable<T>> OnFilterItems;
    public event Func<string, T> OnValidateInput;

    public DropdownInputComponent(
        GameObject parent,
        string name,
        T initialValue,
        Func<T, string> displaySelector,
        Vector2 containerSize,
        int inputFieldWidth,
        Vector2 dropdownSize,
        int maxVisibleItems,
        MelonLogger.Instance logger)
    {
        _displaySelector = displaySelector;
        _logger = logger;
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

        var layout = Container.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 2;
        layout.childControlWidth = false;
        layout.childForceExpandWidth = false;
    }

    private void CreateInputField(string name, string initialValue, int width)
    {
        InputField = InputFieldFactory.CreateInputField(
            Container, $"{name}_Input",
            initialValue,
            InputField.ContentType.Standard,
            width);
    }

    private void CreateDropdownButton(string name)
    {
        var buttonObj = new GameObject($"{name}_DropdownButton");
        buttonObj.transform.SetParent(Container.transform, false);
        var buttonRT = buttonObj.AddComponent<RectTransform>();
        buttonRT.sizeDelta = new Vector2(25, 20);
        var buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.color = Color.gray;
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
        arrowText.fontSize = 12;
        arrowText.color = Color.black;
        arrowText.alignment = TextAnchor.MiddleCenter;
    }

    private void CreateDropdown(string name, Vector2 size, int maxVisibleItems)
    {
        Dropdown = new DropdownComponent<T>(Container, name, size, maxVisibleItems, _logger);
    }

    private void WireUpEvents()
    {
        DropdownButton.onClick.AddListener(() =>
        {
            _logger?.Msg($"Dropdown button clicked. IsOpen={Dropdown.IsOpen}");

            if (Dropdown.IsOpen)
            {
                Dropdown.Close();
            }
            else
            {
                // Get filtered items based on current input
                var items = OnFilterItems?.Invoke(InputField.text) ?? [];
                Dropdown.PopulateItems(items, _displaySelector);
                Dropdown.Show(Container.GetComponent<RectTransform>());
            }
        });

        Dropdown.OnItemSelected += (selectedValue) =>
        {
            var displayText = _displaySelector(selectedValue);
            InputField.text = displayText;
            _lastValidValue = displayText;
            OnValueChanged?.Invoke(selectedValue);
            _logger?.Msg($"Selected: {selectedValue}");
        };

        InputField.onEndEdit.AddListener((value) =>
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
            {
                InputField.text = _lastValidValue;
            }
        });
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
    public GameObject Panel { get; private set; }
    public bool IsOpen { get; private set; }

    private readonly Transform _content;
    private readonly RectTransform _panelRT;
    private readonly ScrollRect _scrollRect;
    private readonly int _maxVisibleItems;
    private readonly MelonLogger.Instance _logger;

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
    }

    private GameObject CreateDropdownPanel(Transform parent, string name, Vector2 size)
    {
        var panel = new GameObject($"{name}_DropdownPanel");
        panel.transform.SetParent(parent, false);

        var panelRT = panel.AddComponent<RectTransform>();
        panelRT.sizeDelta = size;

        var panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0.9f, 0.9f, 0.9f, 1f);

        var mask = panel.AddComponent<Mask>();

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
        scrollRect.horizontal = false;
        scrollRect.content = contentRT;

        panel.SetActive(false);
        return panel;
    }

    public void PopulateItems<TItem>(IEnumerable<TItem> items, Func<TItem, T> valueSelector,
        Func<TItem, string> displaySelector)
    {
        // Clear existing items
        for (int i = _content.childCount - 1; i >= 0; i--)
            UnityEngine.Object.DestroyImmediate(_content.GetChild(i).gameObject);

        _logger?.Msg($"[Dropdown] Populating items");

        var itemList = new List<TItem>(items);

        foreach (var item in itemList)
        {
            var value = valueSelector(item);
            var displayText = displaySelector(item);

            CreateDropdownItem(value, displayText);
        }

        UpdatePanelSize(itemList.Count);
        LayoutRebuilder.ForceRebuildLayoutImmediate(_content.GetComponent<RectTransform>());

        _logger?.Msg($"[Dropdown] Content has {_content.childCount} children after population");
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

        var itemButton = item.AddComponent<Button>();
        var itemImage = item.AddComponent<Image>();
        itemImage.color = Color.gray;
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
        textComp.fontSize = 14;
        textComp.color = Color.black;
        textComp.alignment = TextAnchor.MiddleLeft;

        // Capture value for closure
        var capturedValue = value;
        itemButton.onClick.AddListener(() =>
        {
            OnItemSelected?.Invoke(capturedValue);
            Close();
        });
    }

    private void UpdatePanelSize(int itemCount)
    {
        int itemHeight = 25;
        int spacing = 1;
        int padding = 4;

        int preferredHeight;
        if (itemCount <= _maxVisibleItems)
        {
            preferredHeight = (itemCount * itemHeight) + ((itemCount - 1) * spacing) + padding;
        }
        else
        {
            preferredHeight = (_maxVisibleItems * itemHeight) + ((_maxVisibleItems - 1) * spacing) + padding;
        }

        _panelRT.sizeDelta = new Vector2(_panelRT.sizeDelta.x, preferredHeight);
        _logger?.Msg($"[Dropdown] Set height to {preferredHeight} for {itemCount} items");
    }

    public void Show(RectTransform anchorTransform, Vector2 offset = default)
    {
        if (IsOpen) return;

        Panel.SetActive(true);
        IsOpen = true;

        LayoutRebuilder.ForceRebuildLayoutImmediate(_panelRT);

        Canvas canvas = anchorTransform.GetComponentInParent<Canvas>();
        if (!canvas)
        {
            _logger?.Msg("[Dropdown] ERROR: no Canvas found");
            return;
        }

        Panel.transform.SetParent(canvas.transform, false);
        Panel.transform.SetAsLastSibling();

        // Position dropdown below the anchor
        Vector3[] corners = new Vector3[4];
        anchorTransform.GetWorldCorners(corners);
        Vector3 bottomLeft = corners[0];

        RectTransform canvasRT = canvas.GetComponent<RectTransform>();
        Vector2 localPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRT,
            RectTransformUtility.WorldToScreenPoint(null, bottomLeft),
            null,
            out localPos
        );

        _panelRT.pivot = new Vector2(0, 1);
        _panelRT.localPosition = localPos + offset + new Vector2(0, -5);

        OnDropdownOpened?.Invoke();
        _logger?.Msg($"[Dropdown] Opened at position: {_panelRT.localPosition}");
    }

    public void Close()
    {
        if (!IsOpen) return;

        Panel.SetActive(false);
        IsOpen = false;
        OnDropdownClosed?.Invoke();
        Controls.IsTyping = false; // Ensure typing state is reset
        _logger?.Msg("[Dropdown] Closed");
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
        if (Panel != null)
            UnityEngine.Object.DestroyImmediate(Panel);
    }
}