using MelonLoader;
using UnityEngine;
using UnityEngine.UI;

namespace ModsApp.UI.Input.Handlers;

public class KeyCodeInputHandler : IPreferenceInputHandler
{
    private readonly UITheme _theme;
    private readonly MelonLogger.Instance _logger;
    private static readonly KeyCode[] _allKeyCodes;

    static KeyCodeInputHandler()
    {
        _allKeyCodes = Enum.GetValues(typeof(KeyCode))
            .Cast<KeyCode>()
            .Where(k => !k.ToString().StartsWith("Mouse") &&
                        !k.ToString().StartsWith("Joystick"))
            .OrderBy(k => k.ToString())
            .ToArray();
    }

    public KeyCodeInputHandler(UITheme theme, MelonLogger.Instance logger)
    {
        _theme = theme;
        _logger = logger;
    }

    public bool CanHandle(Type valueType) => valueType == typeof(KeyCode);

    public void CreateInput(MelonPreferences_Entry entry, GameObject parent,
        string entryKey, object currentValue, Action<string, object> onValueChanged)
    {
        var keyCodeValue = (KeyCode)currentValue;

        // container for input field + dropdown button
        var container = new GameObject($"{entryKey}_Container");
        container.transform.SetParent(parent.transform, false);

        var containerRT = container.AddComponent<RectTransform>();
        containerRT.sizeDelta = new Vector2(150, 20);
        
        var layout = container.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 2;
        layout.childControlWidth = false;
        layout.childForceExpandWidth = false;
        
        var input = InputFieldFactory.CreateInputField(
            container, $"{entryKey}_KeyCodeInput",
            keyCodeValue.ToString(),
            InputField.ContentType.Standard,
            120);

        var lastValidKeyCode = keyCodeValue.ToString();
        
        
        var buttonObj = new GameObject($"{entryKey}_DropdownButton");
        buttonObj.transform.SetParent(container.transform, false);

        var buttonRT = buttonObj.AddComponent<RectTransform>();
        buttonRT.sizeDelta = new Vector2(25, 20);

        var buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.color = new Color(0.8f, 0.8f, 0.8f);

        var button = buttonObj.AddComponent<Button>();
        button.targetGraphic = buttonImage;


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

        var dropdownPanel = CreateDropdownPanel(parent.transform.parent, entryKey);

        var isDropdownOpen = false;

        button.onClick.AddListener(() =>
        {
            _logger.Msg($"Dropdown button clicked, current input: '{input.text}'");

            if (isDropdownOpen)
            {
                dropdownPanel.SetActive(false);
                isDropdownOpen = false;
                _logger.Msg("Closed dropdown");
            }
            else
            {
                // Get filtered keycodes based on current input
                var filtered = GetFilteredKeyCodes(input.text);
                PopulateDropdown(dropdownPanel, filtered, (selectedKeyCode) =>
                {
                    input.text = selectedKeyCode.ToString();
                    lastValidKeyCode = selectedKeyCode.ToString();
                    onValueChanged(entryKey, selectedKeyCode);
                    dropdownPanel.SetActive(false);
                    isDropdownOpen = false;
                    _logger.Msg($"Selected from dropdown: {selectedKeyCode}");
                });

                // Position dropdown below the container
                var panelRT = dropdownPanel.GetComponent<RectTransform>();
                var containerWorldPos = containerRT.TransformPoint(Vector3.zero);
                panelRT.position = containerWorldPos + new Vector3(0, -panelRT.sizeDelta.y, 0);

                dropdownPanel.SetActive(true);
                isDropdownOpen = true;
                _logger.Msg($"Opened dropdown with {filtered.Length} filtered options");
            }
        });
        
        input.onValueChanged.AddListener((value) => { _logger.Msg($"Input changed to: '{value}'"); });

        input.onEndEdit.AddListener((value) =>
        {
            _logger.Msg($"Input end edit: '{value}'");
            
            if (isDropdownOpen)
            {
                dropdownPanel.SetActive(false);
                isDropdownOpen = false;
            }
            
            var exactMatch = _allKeyCodes.FirstOrDefault(k =>
                string.Equals(k.ToString(), value, StringComparison.OrdinalIgnoreCase));

            if (exactMatch != default(KeyCode))
            {
                lastValidKeyCode = exactMatch.ToString();
                input.text = lastValidKeyCode;
                onValueChanged(entryKey, exactMatch);
                _logger.Msg($"Valid KeyCode: {exactMatch}");
            }
            else if (!string.IsNullOrEmpty(value))
            {
                var partialMatch = _allKeyCodes.FirstOrDefault(k =>
                    k.ToString().StartsWith(value, StringComparison.OrdinalIgnoreCase));

                if (partialMatch != default(KeyCode))
                {
                    lastValidKeyCode = partialMatch.ToString();
                    input.text = lastValidKeyCode;
                    onValueChanged(entryKey, partialMatch);
                    _logger.Msg($"Partial match KeyCode: {partialMatch}");
                }
                else
                {
                    input.text = lastValidKeyCode;
                    _logger.Msg($"Invalid input '{value}', reverted to: {lastValidKeyCode}");
                }
            }
        });

        _logger.Msg($"Created KeyCode input with custom dropdown ({_allKeyCodes.Length} total options)");
    }

    private KeyCode[] GetFilteredKeyCodes(string filter)
    {
        if (string.IsNullOrEmpty(filter))
            return _allKeyCodes.Take(20).ToArray();

        return _allKeyCodes
            .Where(k => k.ToString().IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
            .Take(20)
            .ToArray();
    }

    private GameObject CreateDropdownPanel(Transform parent, string entryKey)
    {
        var panel = new GameObject($"{entryKey}_DropdownPanel");
        panel.transform.SetParent(parent, false);

        var panelRT = panel.AddComponent<RectTransform>();
        panelRT.sizeDelta = new Vector2(150, 200);

        var panelImage = panel.AddComponent<Image>();
        panelImage.color = Color.white;
        
        var outline = panel.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(1, 1);
        
        var scrollView = panel.AddComponent<ScrollRect>();
        scrollView.horizontal = false;
        scrollView.vertical = true;
        
        var content = new GameObject("Content");
        content.transform.SetParent(panel.transform, false);

        var contentRT = content.AddComponent<RectTransform>();
        contentRT.anchorMin = Vector2.zero;
        contentRT.anchorMax = Vector2.one;
        contentRT.sizeDelta = Vector2.zero;

        var contentLayout = content.AddComponent<VerticalLayoutGroup>();
        contentLayout.childControlHeight = false;
        contentLayout.childForceExpandHeight = false;
        contentLayout.spacing = 1;

        var contentFitter = content.AddComponent<ContentSizeFitter>();
        contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollView.content = contentRT;

        panel.SetActive(false); // Start hidden

        return panel;
    }

    private void PopulateDropdown(GameObject dropdownPanel, KeyCode[] keyCodes, Action<KeyCode> onSelect)
    {
        var content = dropdownPanel.transform.Find("Content");
        
        for (int i = content.childCount - 1; i >= 0; i--)
        {
            UnityEngine.Object.DestroyImmediate(content.GetChild(i).gameObject);
        }
        
        foreach (var keyCode in keyCodes)
        {
            var item = new GameObject($"Item_{keyCode}");
            item.transform.SetParent(content, false);

            var itemRT = item.AddComponent<RectTransform>();
            itemRT.sizeDelta = new Vector2(0, 20);

            var itemButton = item.AddComponent<Button>();
            var itemImage = item.AddComponent<Image>();
            itemImage.color = new Color(0.95f, 0.95f, 0.95f);
            itemButton.targetGraphic = itemImage;

            var text = new GameObject("Text");
            text.transform.SetParent(item.transform, false);

            var textRT = text.AddComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.sizeDelta = Vector2.zero;
            textRT.offsetMin = new Vector2(5, 0);
            textRT.offsetMax = new Vector2(-5, 0);

            var textComp = text.AddComponent<Text>();
            textComp.text = keyCode.ToString();
            textComp.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            textComp.fontSize = 12;
            textComp.color = Color.black;
            textComp.alignment = TextAnchor.MiddleLeft;
            
            var capturedKeyCode = keyCode;
            itemButton.onClick.AddListener(() => onSelect(capturedKeyCode));
        }
    }
}