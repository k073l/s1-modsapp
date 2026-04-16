using System.Collections;
using System.Collections.Generic;
using MelonLoader;
using ModsApp.Helpers;
using ModsApp.Helpers.Registries;
using ModsApp.UI.Input.FieldFactories;
using ModsApp.UI.Panels;
using S1API.Internal.Abstraction;
using S1API.UI;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace ModsApp.UI.Input.Handlers;

public class ListInputHandler : IPreferenceInputHandler
{
    private readonly UITheme _theme;
    private readonly MelonLogger.Instance _logger;
    private readonly PreferenceInputFactory _inputFactory;

    private GameObject _parent;
    private string _entryKey;
    private Action<string, object> _onValueChanged;
    private MelonPreferences_Entry _entry;
    private Type _collectionType;
    private List<object> _workingList;
    private RectTransform _contentRT;

    public ListInputHandler(UITheme theme, MelonLogger.Instance logger, PreferenceInputFactory inputFactory)
    {
        _theme = theme;
        _logger = logger;
        _inputFactory = inputFactory;
    }

    public bool CanHandle(Type valueType)
    {
        if (valueType?.IsGenericType != true) return false;
        var def = valueType.GetGenericTypeDefinition();
        return def == typeof(List<>) || def == typeof(HashSet<>);
    }

    public void CreateInput(MelonPreferences_Entry entry, GameObject parent, string entryKey,
        object currentValue, Action<string, object> onValueChanged)
    {
        _parent = parent;
        _entryKey = entryKey;
        _onValueChanged = onValueChanged;
        _entry = entry;

        _collectionType = currentValue?.GetType() ?? typeof(List<object>);
        _workingList = ((ICollection)currentValue)?.Cast<object>().ToList() ?? new List<object>();

        var count = _workingList.Count;
        var label = count == 0 ? "[empty]" : $"[{count} items]";

        var (_, btn, btnText) = UIFactory.RoundedButtonWithLabel(
            $"{entryKey}_ListBtn", label, parent.transform,
            _theme.BgInput, 90, 20, _theme.SizeSmall, _theme.InputPrimary);
        btnText.alignment = TextAnchor.MiddleCenter;
        btnText.fontStyle = FontStyle.Bold;
        btn.gameObject.GetOrAddComponent<LayoutElement>().minWidth = 90;

        EventHelper.AddListener(() => ShowListEditor(_entry.BoxedValue), btn.onClick);
    }

    public void Recreate(object currentValue)
    {
        FloatingPanelComponent.Cleanup();
        if (_parent != null)
        {
            var oldBtn = _parent.transform.Find($"{_entryKey}_ListBtn");
            if (oldBtn != null) Object.DestroyImmediate(oldBtn.gameObject);
        }

        CreateInput(_entry, _parent, _entryKey, currentValue, _onValueChanged);
    }

    public void CreateStandaloneInput(Type valueType, GameObject parent, string entryKey, object currentValue,
        Action<object> onValueChanged) => throw new NotImplementedException();

    private void ShowListEditor(object currentValue)
    {
        FloatingPanelComponent.Cleanup();

        if (currentValue == null)
        {
            var innerType = GetInnerType();
            currentValue = System.Activator.CreateInstance(typeof(List<>).MakeGenericType(innerType));
        }

        _workingList = ((ICollection)currentValue).Cast<object>().ToList();

        var panel = new FloatingPanelComponent(540, 510, $"List Editor - {_entryKey}");
        var content = panel.ContentPanel.transform;

        // scroll on top, buttons pinned at bottom
        var outerVlg = panel.ContentPanel.AddComponent<VerticalLayoutGroup>();
        outerVlg.spacing = 0;
        outerVlg.padding = new RectOffset(0, 0, 0, 0);
        outerVlg.childControlWidth = true;
        outerVlg.childForceExpandWidth = true;
        outerVlg.childControlHeight = true;
        outerVlg.childForceExpandHeight = false;

        // wrapper takes all available space
        var scrollWrapper = new GameObject("ScrollWrapper");
        scrollWrapper.transform.SetParent(content, false);
        var swRT = scrollWrapper.AddComponent<RectTransform>();
        swRT.anchorMin = Vector2.zero;
        swRT.anchorMax = Vector2.one;
        swRT.offsetMin = swRT.offsetMax = Vector2.zero;
        scrollWrapper.AddComponent<LayoutElement>().flexibleHeight = 1;

        _contentRT = UIFactory.ScrollableVerticalList("ItemsList", scrollWrapper.transform, out var scrollRect);
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

        var s = "";
        var (addBtnGO, addBtn, _) = UIFactory.RoundedButtonWithLabel(
            "AddItem", "+ Add Item", bottomBar.transform,
            _theme.AccentPrimary, 0, 28, _theme.SizeStandard, _theme.TextPrimary);
        addBtnGO.GetOrAddComponent<LayoutElement>().preferredHeight = 28;
        EventHelper.AddListener(() =>
        {
            s = s;
            AddItem();
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
        EventHelper.AddListener(() =>
        {
            s = s;
            var collection = BuildCollection();
            _onValueChanged(_entryKey, collection);
            var collectionAsList = ((IEnumerable)collection)?
                .Cast<object>()
                .ToList();
            _logger.Msg($"Modified preference {_entryKey}: [{string.Join(',', collectionAsList ?? [])}]");
            FloatingPanelComponent.Cleanup();
        }, applyBtn.onClick);

        var (_, cancelBtn, _) = UIFactory.RoundedButtonWithLabel(
            "CancelBtn", "Cancel", btnRow.transform,
            _theme.WarningColor, 0, 28, _theme.SizeStandard, _theme.TextPrimary);
        EventHelper.AddListener(() =>
        {
            s = s;
            FloatingPanelComponent.Cleanup();
        }, cancelBtn.onClick);

        ShowAllRows();
    }

    private void ShowAllRows()
    {
        UIFactory.ClearChildren(_contentRT.transform);

        if (_workingList.Count == 0)
        {
            var emptyText = UIFactory.Text("EmptyLabel", "(no items - click + Add Item)",
                _contentRT.transform, _theme.SizeStandard, TextAnchor.MiddleCenter);
            emptyText.color = _theme.TextSecondary;
            emptyText.gameObject.AddComponent<LayoutElement>().preferredHeight = 40;
            return;
        }

        for (var i = 0; i < _workingList.Count; i++)
            CreateRow(i, _workingList[i]);

        UIHelper.RefreshLayout(_contentRT);
    }

    private void CreateRow(int index, object item)
    {
        var itemType = item?.GetType() ?? GetInnerType();
        var capturedIndex = index;

        var row = UIFactory.Panel($"Item_{index}", _contentRT.transform, _theme.BgSecondary);
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

        // Index
        var idxText = UIFactory.Text($"Idx_{index}", $"[{index}]", row.transform,
            _theme.SizeSmall, TextAnchor.MiddleRight);
        idxText.color = _theme.TextSecondary;
        idxText.gameObject.GetOrAddComponent<LayoutElement>().preferredWidth = 28;

        // Type hint
        var typeText = UIFactory.Text($"Type_{index}",
            TypeNameHelper.GetFriendlyTypeName(itemType),
            row.transform, _theme.SizeSmall, TextAnchor.MiddleLeft);
        typeText.color = _theme.TextSecondary;
        var typeLE = typeText.gameObject.GetOrAddComponent<LayoutElement>();
        typeLE.preferredWidth = 50;
        typeLE.flexibleWidth = 0;

        if (SettingsRegistry.InputsOnRightEntry.Value)
        {
            var spacer = new GameObject("Spacer");
            spacer.transform.SetParent(row.transform, false);
            spacer.GetOrAddComponent<LayoutElement>().flexibleWidth = 0.1f;
        }

        // Input
        _inputFactory.CreateInnerInput(itemType, row, $"{_entryKey}_Item_{index}", item,
            newVal => { _workingList[capturedIndex] = newVal; });

        var s = "";
        // Up
        if (index > 0)
        {
            var (_, upBtn, _) = UIFactory.RoundedButtonWithLabel("UpBtn", "▲", row.transform,
                _theme.AccentSecondary, 22, 22, _theme.SizeSmall, _theme.TextPrimary);
            upBtn.gameObject.GetOrAddComponent<LayoutElement>().preferredWidth = 22;
            EventHelper.AddListener(() =>
            {
                s = s;
                SwapItems(capturedIndex, capturedIndex - 1);
            }, upBtn.onClick);
        }
        else AddSpacer(row.transform, 22);

        // Down
        if (index < _workingList.Count - 1)
        {
            var (_, downBtn, _) = UIFactory.RoundedButtonWithLabel("DownBtn", "▼", row.transform,
                _theme.AccentSecondary, 22, 22, _theme.SizeSmall, _theme.TextPrimary);
            downBtn.gameObject.GetOrAddComponent<LayoutElement>().preferredWidth = 22;
            EventHelper.AddListener(() =>
            {
                s = s;
                SwapItems(capturedIndex, capturedIndex + 1);
            }, downBtn.onClick);
        }
        else AddSpacer(row.transform, 22);

        // Delete
        var (_, delBtn, _) = UIFactory.RoundedButtonWithLabel("DelBtn", "×", row.transform,
            _theme.ErrorColor, 22, 22, _theme.SizeMedium, _theme.TextPrimary);
        delBtn.gameObject.GetOrAddComponent<LayoutElement>().preferredWidth = 22;
        EventHelper.AddListener(() =>
        {
            s = s;
            RemoveItem(capturedIndex);
        }, delBtn.onClick);
    }

    private void AddSpacer(Transform parent, float width)
    {
        var sp = new GameObject("Spacer");
        sp.transform.SetParent(parent, false);
        sp.AddComponent<LayoutElement>().preferredWidth = width;
    }

    private void SwapItems(int a, int b)
    {
        (_workingList[a], _workingList[b]) = (_workingList[b], _workingList[a]);
        ShowAllRows();
    }

    private void RemoveItem(int index)
    {
        _workingList.RemoveAt(index);
        ShowAllRows();
    }

    private void AddItem()
    {
        var innerType = GetInnerType();
        var defaultVal = innerType == typeof(string)
            ? (object)""
            : innerType.IsValueType
                ? System.Activator.CreateInstance(innerType)
                : null;
        _workingList.Add(defaultVal);
        ShowAllRows();
    }

    private Type GetInnerType()
    {
        var entryType = _entry?.GetReflectedType();
        if (entryType?.IsGenericType == true)
            return entryType.GetGenericArguments()[0];
        if (_workingList.Count > 0 && _workingList[0] != null)
            return _workingList[0].GetType();
        return typeof(string);
    }

    private object BuildCollection()
    {
        var innerType = GetInnerType();
        var listType = typeof(List<>).MakeGenericType(innerType);
        var list = (IList)System.Activator.CreateInstance(listType);

        foreach (var item in _workingList)
        {
            try
            {
                list.Add(item == null ? null : System.Convert.ChangeType(item, innerType));
            }
            catch
            {
                list.Add(innerType.IsValueType ? System.Activator.CreateInstance(innerType) : null);
            }
        }

        if (_collectionType?.IsGenericType == true &&
            _collectionType.GetGenericTypeDefinition() == typeof(HashSet<>))
            return System.Activator.CreateInstance(_collectionType, list);

        return list;
    }
}