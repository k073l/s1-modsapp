using MelonLoader;
using System;
using System.Collections;
using System.Linq;
using S1API.Input;
using S1API.Internal.Abstraction;
using UnityEngine;
using UnityEngine.UI;

namespace ModsApp.UI.Input.Handlers;

public class KeyCodeInputHandler : IPreferenceInputHandler
{
    private readonly UITheme _theme;
    private readonly MelonLogger.Instance _logger;
    private static KeyCodeRebindManager _rebindManager;

    public KeyCodeInputHandler(UITheme theme, MelonLogger.Instance logger)
    {
        _theme = theme;
        _logger = logger;
        
        if (_rebindManager == null)
        {
            _rebindManager = new KeyCodeRebindManager();
        }
    }

    public bool CanHandle(Type valueType) => valueType == typeof(KeyCode);

    public void CreateInput(MelonPreferences_Entry entry, GameObject parent,
        string entryKey, object currentValue, Action<string, object> onValueChanged)
    {
        var keyCodeValue = (KeyCode)currentValue;
        
        var rebindInput = new KeyCodeRebindInput(parent, entryKey, keyCodeValue, _theme, _logger, _rebindManager);
        rebindInput.OnValueChanged += (newKeyCode) => { onValueChanged(entryKey, newKeyCode); };

        MelonDebug.Msg($"[{entryKey}] KeyCode rebind input created");
    }
}

public class KeyCodeRebindInput
{
    private readonly GameObject _container;
    private readonly Text _labelText;
    private readonly Button _rebindButton;
    private readonly Button _confirmButton;
    private readonly Button _cancelButton;
    private readonly UITheme _theme;
    private readonly MelonLogger.Instance _logger;
    private readonly KeyCodeRebindManager _rebindManager;

    private KeyCode _currentValue;
    private KeyCode? _pendingValue;
    private bool _isRebinding;

    public event Action<KeyCode> OnValueChanged;

    public KeyCodeRebindInput(GameObject parent, string entryKey, KeyCode initialValue, 
        UITheme theme, MelonLogger.Instance logger, KeyCodeRebindManager rebindManager)
    {
        _currentValue = initialValue;
        _theme = theme;
        _logger = logger;
        _rebindManager = rebindManager;

        // Create main container
        _container = new GameObject($"KeyCodeRebind_{entryKey}");
        _container.transform.SetParent(parent.transform, false);
        
        var containerLayout = _container.AddComponent<HorizontalLayoutGroup>();
        containerLayout.spacing = 5f;
        containerLayout.childControlWidth = false;
        containerLayout.childControlHeight = true;
        containerLayout.childForceExpandWidth = false;
        containerLayout.childForceExpandHeight = false;
        
        var containerLayoutElement = _container.AddComponent<LayoutElement>();
        containerLayoutElement.minHeight = 25f;
        containerLayoutElement.preferredHeight = 25f;

        // Create label
        var labelObject = new GameObject("Label");
        labelObject.transform.SetParent(_container.transform, false);
        _labelText = labelObject.AddComponent<Text>();
        _labelText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        _labelText.fontSize = 12;
        _labelText.color = theme.TextPrimary;
        _labelText.text = _currentValue.ToString();
        _labelText.alignment = TextAnchor.MiddleLeft;
        
        var labelLayoutElement = labelObject.AddComponent<LayoutElement>();
        labelLayoutElement.minWidth = 120f;
        labelLayoutElement.preferredWidth = 120f;
        labelLayoutElement.minHeight = 25f;

        // Create rebind button
        _rebindButton = CreateButton("RebindButton", "Rebind", Color.gray, 80f);
        EventHelper.AddListener(() => BeginRebind(), _rebindButton.onClick);

        // Create confirm button
        _confirmButton = CreateButton("ConfirmButton", "Confirm", new Color(0.1f, 0.4f, 0.1f), 80f);
        EventHelper.AddListener(() => ConfirmRebind(), _confirmButton.onClick);
        _confirmButton.gameObject.SetActive(false);

        // Create cancel button  
        _cancelButton = CreateButton("CancelButton", "Cancel", new Color(0.4f, 0.1f, 0.1f), 80f);
        EventHelper.AddListener(CancelRebind, _cancelButton.onClick);
        _cancelButton.gameObject.SetActive(false);
    }

    private Button CreateButton(string name, string text, Color color, float width)
    {
        var buttonObject = new GameObject(name);
        buttonObject.transform.SetParent(_container.transform, false);
        
        var image = buttonObject.AddComponent<Image>();
        image.color = color;
        
        var button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        
        // Add button color transitions
        var colors = button.colors;
        colors.normalColor = color;
        colors.highlightedColor = new Color(color.r * 1.2f, color.g * 1.2f, color.b * 1.2f);
        colors.pressedColor = new Color(color.r * 0.8f, color.g * 0.8f, color.b * 0.8f);
        colors.disabledColor = new Color(0.3f, 0.3f, 0.3f);
        button.colors = colors;
        
        var buttonLayoutElement = buttonObject.AddComponent<LayoutElement>();
        buttonLayoutElement.minWidth = width;
        buttonLayoutElement.preferredWidth = width;
        buttonLayoutElement.minHeight = 25f;
        
        // Create button text
        var textObject = new GameObject("Text");
        textObject.transform.SetParent(buttonObject.transform, false);
        
        var buttonText = textObject.AddComponent<Text>();
        buttonText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        buttonText.fontSize = 11;
        buttonText.color = _theme.TextPrimary;
        buttonText.text = text;
        buttonText.alignment = TextAnchor.MiddleCenter;
        
        var textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.anchoredPosition = Vector2.zero;
        
        return button;
    }

    private void BeginRebind()
    {
        if (_isRebinding) return;
        
        _isRebinding = true;
        _pendingValue = null;
        
        // Block game input
        Controls.IsTyping = true;
        
        // Update UI
        _rebindButton.gameObject.SetActive(false);
        _confirmButton.gameObject.SetActive(true);
        _confirmButton.interactable = false;
        _cancelButton.gameObject.SetActive(true);
        
        _labelText.text = "<i>Press a key...</i>";
        
        // Start listening for key input
        _rebindManager.StartRebind(OnKeyPressed);
        
        MelonDebug.Msg("Started key rebind");
    }

    private void OnKeyPressed(KeyCode keyCode)
    {
        if (!_isRebinding) return;
        
        _pendingValue = keyCode;
        _labelText.text = $"<i>{keyCode}</i>";
        _confirmButton.interactable = true;
        
        MelonDebug.Msg($"Key pressed: {keyCode}");
    }

    private void ConfirmRebind()
    {
        if (!_isRebinding || !_pendingValue.HasValue) return;
        
        _currentValue = _pendingValue.Value;
        OnValueChanged?.Invoke(_currentValue);
        
        EndRebind();
        RefreshDisplay();
        
        MelonDebug.Msg($"Key rebind confirmed: {_currentValue}");
    }

    private void CancelRebind()
    {
        if (!_isRebinding) return;
        
        EndRebind();
        RefreshDisplay();
        
        MelonDebug.Msg("Key rebind cancelled");
    }

    private void EndRebind()
    {
        _isRebinding = false;
        _pendingValue = null;
        
        _rebindManager.StopRebind();
        
        // Unblock game input
        Controls.IsTyping = false;
        
        // Restore UI
        _rebindButton.gameObject.SetActive(true);
        _confirmButton.gameObject.SetActive(false);
        _cancelButton.gameObject.SetActive(false);
    }

    private void RefreshDisplay()
    {
        _labelText.text = _currentValue.ToString();
    }
}

public class KeyCodeRebindManager
{
    private Action<KeyCode> _onKeyPressed;
    private bool _isListening;
    private object _inputCheckCoroutine;
    
    // Keys to ignore during rebind
    private static readonly KeyCode[] _ignoredKeys = new[]
    {
        KeyCode.None,
        KeyCode.Mouse0, KeyCode.Mouse1, KeyCode.Mouse2, KeyCode.Mouse3, KeyCode.Mouse4, KeyCode.Mouse5, KeyCode.Mouse6
    };

    public void StartRebind(Action<KeyCode> onKeyPressed)
    {
        if (_isListening) return;
        
        _onKeyPressed = onKeyPressed;
        _isListening = true;
        
        // Start the input checking coroutine
        _inputCheckCoroutine = MelonCoroutines.Start(InputCheckCoroutine());
    }

    public void StopRebind()
    {
        _isListening = false;
        _onKeyPressed = null;
        
        if (_inputCheckCoroutine != null)
        {
            MelonCoroutines.Stop(_inputCheckCoroutine);
            _inputCheckCoroutine = null;
        }
    }

    private IEnumerator InputCheckCoroutine()
    {
        while (_isListening)
        {
            // Check for any key press
            foreach (KeyCode keyCode in Enum.GetValues(typeof(KeyCode)))
            {
                if (_ignoredKeys.Contains(keyCode)) continue;
                
                if (UnityEngine.Input.GetKeyDown(keyCode))
                {
                    _onKeyPressed?.Invoke(keyCode);
                    yield break; // Exit after first key press
                }
            }
            
            yield return null; // Wait one frame
        }
    }
}