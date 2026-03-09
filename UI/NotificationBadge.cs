using MelonLoader;
using UnityEngine;
using UnityEngine.UI;

namespace ModsApp.UI;

public static class NotificationBadge
{
    private static GameObject _countBadge;
    private static GameObject _dotBadge;

    private static Image _countImage;
    private static Text _countText;
    private static Image _dotImage;

    private static bool _ready;

    public static void Initialize(Transform modsIcon)
    {
        _ready = false;

        try
        {
            if (modsIcon == null) return;

            var notificationsRoot = modsIcon.Find("Notifications");
            if (notificationsRoot == null) return;

            var textT = notificationsRoot.Find("Text");
            if (textT == null) return;

            _countBadge = notificationsRoot.gameObject;
            _countImage = notificationsRoot.GetComponent<Image>();
            _countText = textT.GetComponent<Text>();
            if (_countImage == null || _countText == null) return;
            _dotBadge = BuildDotBadge(notificationsRoot);
            if (_dotBadge == null) return;

            _dotImage = _dotBadge.GetComponent<Image>();
            _countBadge.SetActive(false);
            _dotBadge.SetActive(false);

            _ready = true;
        }
        catch (Exception ex)
        {
            Melon<ModsApp>.Logger.Warning($"[NotificationBadge] Init failed: {ex.Message}");
        }
    }

    public static void ShowCount(int count)
    {
        if (!_ready || !RefsValid()) return;

        if (count <= 0)
        {
            Hide();
            return;
        }

        _dotBadge.SetActive(false);
        _countText.gameObject.SetActive(true);
        _countText.text = count > 99 ? "99" : count.ToString();
        _countBadge.SetActive(true);
    }

    public static void ShowDot()
    {
        if (!_ready || !RefsValid()) return;

        _countBadge.SetActive(true);
        _dotBadge.SetActive(true);
        _countText.gameObject.SetActive(false);
    }

    public static void Hide()
    {
        if (!_ready || !RefsValid()) return;

        _countBadge.SetActive(false);
        _dotBadge.SetActive(false);
    }

    private static bool RefsValid() =>
        _countBadge != null && _dotBadge != null &&
        _countImage != null && _countText != null && _dotImage != null;

    private static GameObject BuildDotBadge(Transform sourceImage)
    {
        try
        {
            var sourceRT = sourceImage.GetComponent<RectTransform>();
            if (sourceRT == null) return null;

            var dot = new GameObject("NotificationDot");
            dot.transform.SetParent(sourceImage.parent, false);

            var dotRT = dot.AddComponent<RectTransform>();
            dotRT.anchorMin = sourceRT.anchorMin;
            dotRT.anchorMax = sourceRT.anchorMax;
            dotRT.pivot = sourceRT.pivot;
            dotRT.sizeDelta = sourceRT.sizeDelta * 0.4f;
            dotRT.anchoredPosition = sourceRT.anchoredPosition;

            var img = dot.AddComponent<Image>();
            img.color = Color.white;
            var srcImg = sourceImage.GetComponent<Image>();
            if (srcImg != null)
            {
                img.sprite = srcImg.sprite;
                img.type = srcImg.type;
            }
            return dot;
        }
        catch (Exception ex)
        {
            Melon<ModsApp>.Logger.Warning($"[NotificationBadge] BuildDotBadge failed: {ex.Message}");
            return null;
        }
    }
}