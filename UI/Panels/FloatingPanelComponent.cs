using ModsApp.Helpers;
using ModsApp.Managers;
using S1API.Internal.Abstraction;
using S1API.UI;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace ModsApp.UI.Panels;

public class FloatingPanelComponent
{
    private static GameObject _panelObject;
    private RectTransform _rectTransform;
    private UITheme _theme;
    private static Button _closeBtn;

    public GameObject ContentPanel;

    private const float topBarHeight = 65f;

    public FloatingPanelComponent(float width, float height, string title)
    {
        _theme = UIManager._theme;

        // look in main panel for existing floating panel (in case multiple are opened, kill the old one)
        var existing = UIManager.MainPanel.transform.Find("FloatingPanel");
        if (existing != null) Cleanup();

        _panelObject = UIFactory.Panel("FloatingPanel", UIManager.MainPanel.transform, _theme.BgPrimary);
        _panelObject.transform.SetParent(UIManager.MainPanel.transform, false);
        _panelObject.transform.SetAsLastSibling();

        _panelObject.GetComponent<Image>()?.MakeRounded(12, 48);

        _rectTransform = _panelObject.GetComponent<RectTransform>();
        _rectTransform.sizeDelta = new Vector2(width, height);

        AddTopBar(title);
        UIHelper.AddBorderEffect(_panelObject, _theme.AccentPrimary);
        ContentPanel = AddContentPanel();
    }

    private void AddTopBar(string title)
    {
        var topBar = UIFactory.TopBar(
            "FloatingPanelTopBar",
            _panelObject.transform,
            title,
            0.99f, // ignored
            8, 8, 4, 4
        );
        topBar.GetComponent<Image>()?.MakeRounded(12, 48);
        var topRect = topBar.GetComponent<RectTransform>();
        topRect.anchorMin = new Vector2(0, 1f);
        topRect.anchorMax = new Vector2(1, 1f);
        topRect.pivot = new Vector2(0.5f, 1f);

        topRect.sizeDelta = new Vector2(0, topBarHeight);
        topRect.anchoredPosition = new Vector2(0, 0);

        var hLayout = topBar.GetComponent<HorizontalLayoutGroup>();
        if (hLayout != null)
        {
            hLayout.childForceExpandHeight = false;
        }

        var titleText = topBar.transform.Find("TopBarTitle")?.GetComponent<Text>();
        if (titleText != null)
        {
            titleText.fontSize = _theme.SizeMedium;
        }

        var (closeBtnGO, closeBtn, _) = UIFactory.RoundedButtonWithLabel(
            "CloseButton",
            "X",
            topBar.transform,
            _theme.WarningColor,
            topBarHeight * 0.75f, topBarHeight * 0.75f,
            _theme.SizeMedium,
            _theme.TextPrimary
        );

        var rect = closeBtnGO.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 0.5f);
        rect.anchorMax = new Vector2(1f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(-20f, 0f);

        EventHelper.AddListener(Cleanup, closeBtn.onClick);
        _closeBtn = closeBtn;
        closeBtnGO.transform.SetAsLastSibling();
    }

    private GameObject AddContentPanel()
    {
        var contentPanel = UIFactory.Panel("ContentPanel", _panelObject.transform, _theme.BgSecondary);
        var rect = contentPanel.GetComponent<RectTransform>();

        rect.anchorMin = new Vector2(0, 0);
        rect.anchorMax = new Vector2(1, 1);
        rect.offsetMin = new Vector2(8, 8);
        rect.offsetMax = new Vector2(-8, -topBarHeight - 8);

        contentPanel.GetComponent<Image>()?.MakeRounded(8, 48);

        return contentPanel;
    }

    public static void Cleanup()
    {
        if (_closeBtn != null) EventHelper.RemoveListener(Cleanup, _closeBtn.onClick);
        if (_panelObject == null) return;
        Object.Destroy(_panelObject);
        var border = _panelObject.transform.parent.Find($"{_panelObject.name}_Border");
        if (border != null) Object.Destroy(border.gameObject);
    }
}