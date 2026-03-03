using S1API.UI;
using UnityEngine;
using UnityEngine.UI;

namespace ModsApp.UI.Input.FieldFactories;

public class ScrollableTextFactory
{
    public GameObject Root { get; private set; }
    public ScrollRect ScrollRect { get; private set; }
    public RectTransform ContentRect { get; private set; }
    public Text Text { get; private set; }

    public ScrollableTextFactory(
        Transform parent,
        string initialText,
        int fontSize,
        Color textColor,
        Color backgroundColor)
    {
        CreateUI(parent, initialText, fontSize, textColor, backgroundColor);
    }

    private void CreateUI(
        Transform parent,
        string initialText,
        int fontSize,
        Color textColor,
        Color backgroundColor)
    {
        ContentRect = UIFactory.ScrollableVerticalList("ScrollableTextContent", parent, out var scrollRect);
        if (scrollRect != null) scrollRect.scrollSensitivity = 15f;
        ScrollRect = scrollRect;

        Root = new GameObject("ScrollableTextRoot");
        Root.transform.SetParent(parent, false);
        var rootRect = Root.AddComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0, 0);
        rootRect.anchorMax = new Vector2(1, 1);
        rootRect.pivot = new Vector2(0.5f, 0.5f);
        rootRect.sizeDelta = Vector2.zero;

        var bg = Root.AddComponent<Image>();
        bg.color = backgroundColor;

        var layoutGroup = Root.AddComponent<VerticalLayoutGroup>();
        layoutGroup.padding = new RectOffset(10, 10, 10, 10);
        layoutGroup.spacing = 0;
        layoutGroup.childControlWidth = true;
        layoutGroup.childForceExpandWidth = true;
        layoutGroup.childControlHeight = true;
        layoutGroup.childForceExpandHeight = true;

        ScrollRect.gameObject.transform.SetParent(Root.transform, false); // for padding

        var scrollRectTransform = ScrollRect.GetComponent<RectTransform>();
        scrollRectTransform.anchorMin = new Vector2(0, 0);
        scrollRectTransform.anchorMax = new Vector2(1, 1);
        scrollRectTransform.pivot = new Vector2(0.5f, 0.5f);
        scrollRectTransform.offsetMin = Vector2.zero;
        scrollRectTransform.offsetMax = Vector2.zero;

        // ContentRect is inside the ScrollRect, set to stretch horizontally and vertically
        ContentRect.anchorMin = new Vector2(0, 1);
        ContentRect.anchorMax = new Vector2(1, 1);
        ContentRect.pivot = new Vector2(0.5f, 1);
        ContentRect.offsetMin = new Vector2(0, 0);
        ContentRect.offsetMax = new Vector2(0, 0);

        // adjust VerticalLayoutGroup on ContentRect
        if (ContentRect.TryGetComponent(out VerticalLayoutGroup contentLayoutGroup))
        {
            contentLayoutGroup.padding = new RectOffset(0, 0, 0, 0);
            contentLayoutGroup.spacing = 0;
            contentLayoutGroup.childControlWidth = true;
            contentLayoutGroup.childForceExpandWidth = true;
            contentLayoutGroup.childControlHeight = false;
            contentLayoutGroup.childForceExpandHeight = false;
        }

        var textGO = new GameObject("Text");
        textGO.transform.SetParent(ContentRect, false);

        Text = textGO.AddComponent<Text>();
        Text.text = initialText;
        Text.fontSize = fontSize;
        Text.color = textColor;
        Text.alignment = TextAnchor.UpperLeft;
        Text.horizontalOverflow = HorizontalWrapMode.Wrap;
        Text.verticalOverflow = VerticalWrapMode.Overflow;
        Text.supportRichText = true;
        Text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");

        var textRect = Text.rectTransform;
        textRect.anchorMin = new Vector2(0, 1);
        textRect.anchorMax = new Vector2(1, 1);
        textRect.pivot = new Vector2(0.5f, 1);
        textRect.offsetMin = new Vector2(0, 0);
        textRect.offsetMax = new Vector2(0, 0);

        var fitter = textGO.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var layoutElement = textGO.AddComponent<LayoutElement>();
        layoutElement.flexibleWidth = 1;
    }

    public void SetText(string newText)
    {
        Text.text = newText;
        ScrollRect.verticalNormalizedPosition = 1f;
    }
}