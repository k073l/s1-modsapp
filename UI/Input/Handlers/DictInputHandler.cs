using System.Collections;
using MelonLoader;
using ModsApp.Helpers;
using ModsApp.Helpers.Registries;
using ModsApp.UI.Panels;
using S1API.Internal.Abstraction;
using S1API.UI;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace ModsApp.UI.Input.Handlers;

public class DictInputHandler : IPreferenceInputHandler
{
    private readonly UITheme _theme;
    private readonly MelonLogger.Instance _logger;
    private readonly PreferenceInputFactory _inputFactory;

    private GameObject _parent;
    private string _entryKey;
    private Action<string, object> _onValueChanged;
    private MelonPreferences_Entry _entry;
    private Type _collectionType;
    private List<KeyValuePair<object, object>> _workingList;
    private RectTransform _contentRT;
    private Text _errorLabel;
    private Button _applyButton;

    public DictInputHandler(UITheme theme, MelonLogger.Instance logger, PreferenceInputFactory inputFactory)
    {
        _theme = theme;
        _logger = logger;
        _inputFactory = inputFactory;
    }

    public bool CanHandle(Type valueType)
    {
        if (valueType?.IsGenericType != true) return false;
        return valueType.GetGenericTypeDefinition() == typeof(Dictionary<,>);
    }

    public void CreateInput(MelonPreferences_Entry entry, GameObject parent, string entryKey,
        object currentValue, Action<string, object> onValueChanged)
    {
        _parent = parent;
        _entryKey = entryKey;
        _onValueChanged = onValueChanged;
        _entry = entry;

        _collectionType = currentValue?.GetType() ?? typeof(Dictionary<object, object>);
        _workingList = ConvertToList(currentValue);

        var count = _workingList.Count;
        var label = count == 0 ? "[empty]" : $"[{count} pairs]";

        var (btn, _) = UIHelper.CreateRectButton($"{entryKey}_ListBtn", label, parent.transform, _theme.BgInput,
            _theme.SizeStandard, _theme.InputPrimary, 90f);

        EventHelper.AddListener(() => ShowEditor(_entry.BoxedValue), btn.onClick);
    }

    public void Recreate(object currentValue)
    {
        FloatingPanelComponent.Cleanup();
        if (_parent != null)
        {
            var oldBtn = _parent.transform.Find($"{_entryKey}_DictBtn");
            if (oldBtn != null) Object.DestroyImmediate(oldBtn.gameObject);
        }

        CreateInput(_entry, _parent, _entryKey, currentValue, _onValueChanged);
    }

    public void CreateStandaloneInput(Type valueType, GameObject parent, string entryKey, object currentValue,
        Action<object> onValueChanged) => throw new NotImplementedException();

    private void ShowEditor(object currentValue)
    {
        FloatingPanelComponent.Cleanup();

        if (currentValue == null)
            currentValue = System.Activator.CreateInstance(_collectionType);

        _workingList = ConvertToList(currentValue);

        var panel = new FloatingPanelComponent(750, 560, $"Dict Editor - {_entryKey.Split('.')[^1]}");
        var content = panel.ContentPanel.transform;

        var outerVlg = panel.ContentPanel.AddComponent<VerticalLayoutGroup>();
        outerVlg.spacing = 0;
        outerVlg.padding = new RectOffset(0, 0, 0, 0);
        outerVlg.childControlWidth = true;
        outerVlg.childForceExpandWidth = true;
        outerVlg.childControlHeight = true;
        outerVlg.childForceExpandHeight = false;

        var scrollWrapper = new GameObject("ScrollWrapper");
        scrollWrapper.transform.SetParent(content, false);
        var swRT = scrollWrapper.AddComponent<RectTransform>();
        swRT.anchorMin = Vector2.zero;
        swRT.anchorMax = Vector2.one;
        swRT.offsetMin = swRT.offsetMax = Vector2.zero;
        scrollWrapper.AddComponent<LayoutElement>().flexibleHeight = 1;

        _contentRT = UIFactory.ScrollableVerticalList("ItemsDict", scrollWrapper.transform, out var scrollRect);
        if (scrollRect != null) scrollRect.scrollSensitivity = 15f;
        UIHelper.ForceRectToAnchors(_contentRT, Vector2.zero, Vector2.one,
            Vector2.zero, Vector2.zero, new Vector2(0.5f, 1f));

        var vlg = _contentRT.gameObject.GetOrAddComponent<VerticalLayoutGroup>();
        vlg.spacing = 4;
        vlg.padding = new RectOffset(8, 8, 8, 8);
        vlg.childControlWidth = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlHeight = true;
        vlg.childAlignment = TextAnchor.UpperLeft;

        var csf = _contentRT.gameObject.GetOrAddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        UIFactory.FitContentHeight(_contentRT);
        
        var errorLabelGO = new GameObject("ErrorLabel");
        errorLabelGO.transform.SetParent(content, false);
        _errorLabel = UIFactory.Text("ErrorLabelText", "", errorLabelGO.transform, _theme.SizeStandard, TextAnchor.MiddleLeft);
        _errorLabel.color = _theme.ErrorColor;
        _errorLabel.gameObject.SetActive(false);
        var errorLE = errorLabelGO.AddComponent<LayoutElement>();
        errorLE.preferredHeight = 28;
        errorLE.preferredWidth = 60;

        var bottomBar = UIFactory.Panel("BottomBar", content, Color.clear);
        var bottomLE = bottomBar.GetOrAddComponent<LayoutElement>();
        bottomLE.preferredHeight = 76;
        bottomLE.flexibleHeight = 0;
        bottomLE.flexibleWidth = 1;

        var bottomVlg = bottomBar.AddComponent<VerticalLayoutGroup>();
        bottomVlg.spacing = 6;
        bottomVlg.padding = new RectOffset(8, 8, 6, 6);
        bottomVlg.childControlWidth = true;
        bottomVlg.childForceExpandWidth = true;
        bottomVlg.childForceExpandHeight = false;
        bottomVlg.childControlHeight = true;

        var (addBtnGO, addBtn, _) = UIFactory.RoundedButtonWithLabel(
            "AddItem", "+ Add Pair", bottomBar.transform,
            _theme.AccentPrimary, 0, 28, _theme.SizeStandard, _theme.TextPrimary);
        addBtnGO.GetOrAddComponent<LayoutElement>().preferredHeight = 28;
        var s = "";
        EventHelper.AddListener(() =>
        {
            s = s;
            AddPair();
        }, addBtn.onClick);

        var btnRow = new GameObject("ButtonRow");
        btnRow.transform.SetParent(bottomBar.transform, false);
        var btnRowHlg = btnRow.AddComponent<HorizontalLayoutGroup>();
        btnRowHlg.spacing = 8;
        btnRowHlg.childAlignment = TextAnchor.MiddleCenter;
        btnRowHlg.childForceExpandWidth = true;
        btnRowHlg.childForceExpandHeight = false;
        btnRowHlg.childControlWidth = true;
        btnRowHlg.childControlHeight = true;
        btnRow.AddComponent<LayoutElement>().preferredHeight = 28;

        var (_, applyBtn, _) = UIFactory.RoundedButtonWithLabel(
            "ApplyBtn", "Apply", btnRow.transform,
            _theme.SuccessColor, 0, 28, _theme.SizeStandard, _theme.TextPrimary);
        _applyButton = applyBtn;
        EventHelper.AddListener(() =>
        {
            s = s;
            if (!ValidateKeys())
            {
                DisplayValidationError();
                MelonLogger.Warning($"Validation failed for dictionary input '{_entryKey}': duplicate keys found");
                return;
            }
            ClearValidationError();

            _onValueChanged(_entryKey, BuildCollection());
            FloatingPanelComponent.Cleanup();
        }, applyBtn.onClick);

        var (_, cancelBtn, _) = UIFactory.RoundedButtonWithLabel(
            "CancelBtn", "Cancel", btnRow.transform,
            _theme.WarningColor, 0, 28, _theme.SizeStandard, _theme.TextPrimary);
        EventHelper.AddListener(() =>
        {
            s = s;
            ClearValidationError();
            FloatingPanelComponent.Cleanup();
        }, cancelBtn.onClick);

        ShowAllRows();
    }

    private void ShowAllRows()
    {
        UIFactory.ClearChildren(_contentRT.transform);

        if (_workingList.Count == 0)
        {
            var emptyText = UIFactory.Text("EmptyLabel", "(no pairs - click + Add Pair)",
                _contentRT.transform, _theme.SizeStandard, TextAnchor.MiddleCenter);
            emptyText.color = _theme.TextSecondary;
            emptyText.gameObject.AddComponent<LayoutElement>().preferredHeight = 40;
            return;
        }

        for (var i = 0; i < _workingList.Count; i++)
            CreateRow(i, _workingList[i].Key, _workingList[i].Value);

        UIHelper.RefreshLayout(_contentRT);
    }

    private void CreateRow(int index, object key, object value)
    {
        var (keyType, valueType) = GetKeyValueTypes();
        var capturedIndex = index;

        var row = UIFactory.Panel($"Pair_{index}", _contentRT.transform, _theme.BgSecondary);
        row.GetComponent<Image>()?.MakeRounded(4, 16);

        var hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 6;
        hlg.padding = new RectOffset(6, 6, 4, 4);
        hlg.childControlWidth = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;
        hlg.childControlHeight = true;

        var rowLE = row.AddComponent<LayoutElement>();
        rowLE.preferredHeight = _theme.SizeStandard + 16f;
        rowLE.flexibleWidth = 1;

        // Key input
        _inputFactory.CreateInnerInput(keyType, row, $"{_entryKey}_Key_{index}", key, newVal =>
        {
            _workingList[capturedIndex] = new KeyValuePair<object, object>(newVal, _workingList[capturedIndex].Value);
            ClearValidationError();
        });

        if (SettingsRegistry.InputsOnRightEntry.Value)
        {
            var spacer = new GameObject("Spacer");
            spacer.transform.SetParent(row.transform, false);
            spacer.GetOrAddComponent<LayoutElement>().flexibleWidth = 0.1f;
        }

        // Type hint
        var (kt, vt) = GetKeyValueTypes();
        var typeHintText = UIFactory.Text($"TypeHint_{index}",
            $"{TypeNameHelper.GetFriendlyTypeName(kt)} : {TypeNameHelper.GetFriendlyTypeName(vt)}",
            row.transform, _theme.SizeSmall, TextAnchor.MiddleCenter);
        typeHintText.color = _theme.TextSecondary;
        typeHintText.horizontalOverflow = HorizontalWrapMode.Overflow;
        var typeHintLE = typeHintText.gameObject.GetOrAddComponent<LayoutElement>();
        typeHintLE.preferredWidth = 80;
        typeHintLE.flexibleWidth = 0;

        if (SettingsRegistry.InputsOnRightEntry.Value)
        {
            var spacer = new GameObject("Spacer");
            spacer.transform.SetParent(row.transform, false);
            spacer.GetOrAddComponent<LayoutElement>().flexibleWidth = 0.1f;
        }

        // Value input
        _inputFactory.CreateInnerInput(valueType, row, $"{_entryKey}_Value_{index}", value, newVal =>
        {
            _workingList[capturedIndex] = new KeyValuePair<object, object>(_workingList[capturedIndex].Key, newVal);
        });

        // Delete
        var (_, delBtn, _) = UIFactory.RoundedButtonWithLabel("DelBtn", "×", row.transform,
            _theme.ErrorColor, 22, 22, _theme.SizeMedium, _theme.TextPrimary);
        delBtn.gameObject.GetOrAddComponent<LayoutElement>().preferredWidth = 22;
        var s = "";
        EventHelper.AddListener(() =>
        {
            s = s;
            RemovePair(capturedIndex);
        }, delBtn.onClick);
    }

    private void AddPair()
    {
        var (keyType, _) = GetKeyValueTypes();
        var keyDefault = keyType == typeof(string)
            ? (object)""
            : keyType.IsValueType
                ? System.Activator.CreateInstance(keyType)
                : null;

        var (_, valueType) = GetKeyValueTypes();
        var valueDefault = valueType == typeof(string)
            ? (object)""
            : valueType.IsValueType
                ? System.Activator.CreateInstance(valueType)
                : null;

        _workingList.Add(new KeyValuePair<object, object>(keyDefault, valueDefault));
        ClearValidationError();
        ShowAllRows();
    }

    private void RemovePair(int index)
    {
        _workingList.RemoveAt(index);
        ClearValidationError();
        ShowAllRows();
    }

    private object BuildCollection()
    {
        var (keyType, valueType) = GetKeyValueTypes();
        var dictType = typeof(Dictionary<,>).MakeGenericType(keyType, valueType);
        var dict = System.Activator.CreateInstance(dictType);
        var addMethod = dictType.GetMethod("Add");
        foreach (var kvp in _workingList)
            addMethod?.Invoke(dict, [kvp.Key, kvp.Value]);
        return dict;
    }

    private bool ValidateKeys()
    {
        var seen = new HashSet<string>();
        foreach (var kvp in _workingList)
            if (!seen.Add(kvp.Key?.ToString()))
                return false;
        return true;
    }

    private List<KeyValuePair<object, object>> ConvertToList(object currentValue)
    {
        var result = new List<KeyValuePair<object, object>>();
        if (currentValue == null) return result;
        var dict = (IDictionary)currentValue;
        foreach (var key in dict.Keys)
            result.Add(new KeyValuePair<object, object>(key, dict[key]));
        return result;
    }
    
    private (Type, Type) GetKeyValueTypes()
    {
        if (_collectionType?.IsGenericType == true)
        {
            var args = _collectionType.GetGenericArguments();
            return (args[0], args[1]);
        }

        return (typeof(object), typeof(object));
    }

    private void DisplayValidationError()
    {
        if (_errorLabel != null)
        {
            _errorLabel.text = "Duplicate keys found";
            _errorLabel.gameObject.SetActive(true);
        }
    }

    private void ClearValidationError()
    {
        if (_errorLabel != null)
        {
            _errorLabel.text = "";
            _errorLabel.gameObject.SetActive(false);
        }
    }
}