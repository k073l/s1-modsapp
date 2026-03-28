using MelonLoader;
using ModsApp.Helpers;
using ModsApp.Managers;
using S1API.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace ModsApp.UI.Input.FieldFactories;

public static class ToggleFactory
{
    private const int SpriteSize = 32;
    private const int TrackRadius = 5;
    private const int NubRadius = 3;

    private static float SliderWidth => UIManager._theme.SizeStandard * 2.6f;
    private static float SliderHeight => UIManager._theme.SizeSmall * 1.8f;
    private static float NubSize => UIManager._theme.SizeStandard;
    private const float NubPadding = 3f;
    private const float AnimationDuration = 0.1f;

    public static Toggle CreateSliding(
        Transform parent,
        string name,
        bool initialValue,
        Color bgOn,
        Color bgOff,
        Color nubOn,
        Color nubOff,
        System.Action<bool> onChanged = null,
        float animationDuration = AnimationDuration
    )
    {
        var track = CreateTrack(parent, name, initialValue ? bgOn : bgOff);
        var nub = CreateNub(track.transform, initialValue, initialValue ? nubOn : nubOff);

        var toggle = track.gameObject.AddComponent<Toggle>();
        toggle.targetGraphic = track;
        toggle.isOn = initialValue;
        toggle.transition = Selectable.Transition.Animation;

        ToggleUtils.AddListener(toggle, val =>
        {
            var targetTrackColor = val ? bgOn : bgOff;
            var targetNubColor = val ? nubOn : nubOff;
            var targetNubPos = NubPosition(val);

            var nubImg = nub.GetComponent<Image>();
            var startPos = nub.anchoredPosition;
            var startTrackColor = track.color;
            var startNubColor = nubImg != null ? nubImg.color : targetNubColor;

            MelonCoroutines.Start(AnimateToggle(nub, track, startTrackColor, targetTrackColor, nubImg, startNubColor,
                targetNubColor, startPos, targetNubPos, animationDuration));

            onChanged?.Invoke(val);
        });

        return toggle;
    }

    public static Toggle CreateSlidingWithLabel(
        Transform parent,
        string name,
        string labelText,
        bool initialValue,
        Color bgOn,
        Color bgOff,
        Color nubOn,
        Color nubOff,
        Action<bool> onChanged = null)
    {
        var theme = UIManager._theme;

        var root = new GameObject(name);
        root.transform.SetParent(parent, false);

        var layout = root.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 6;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.childControlWidth = false;
        layout.childControlHeight = true;

        root.AddComponent<LayoutElement>().preferredHeight = 24;

        var toggle = CreateSliding(
            root.transform,
            name + "_Toggle",
            initialValue,
            bgOn,
            bgOff,
            nubOn,
            nubOff,
            onChanged
        );

        var label = S1API.UI.UIFactory.Text(
            name + "_Label",
            labelText,
            root.transform,
            theme.SizeSmall,
            TextAnchor.MiddleLeft
        );
        label.color = theme.TextPrimary;

        label.gameObject.AddComponent<LayoutElement>().preferredWidth = label.preferredWidth;

        return toggle;
    }

    public static (Toggle toggle, System.Action<bool, bool> refreshColors) CreateModToggleSlider(
        Transform parent,
        string name,
        bool initialOn,
        bool initialPending,
        float animationDuration = AnimationDuration)
    {
        var theme = UIManager._theme;

        var nubColor = theme.BgInput;

        var initialTrackColor = TrackColorFor(initialOn, initialPending);

        var toggle = CreateSliding(
            parent,
            name,
            initialOn,
            initialTrackColor, // bgOn
            initialTrackColor, // bgOff (we'll lerp manually later)
            nubColor,
            nubColor,
            val =>
            {
                /* noop for now; we'll override in Refresh */
            },
            animationDuration
        );

        var trackImg = toggle.targetGraphic as Image;
        var nub = toggle.transform.Find("Nub")?.GetComponent<RectTransform>();
        var nubImg = nub?.GetComponent<Image>();

        var isPending = initialPending;

        // Override listener to include pending state
        ToggleUtils.AddListener(toggle, val =>
        {
            if (trackImg != null && nubImg != null && nub != null)
            {
                MelonCoroutines.Start(ToggleFactory.AnimateToggle(
                    nub,
                    trackImg,
                    trackImg.color,
                    TrackColorFor(val, isPending),
                    nubImg,
                    nubImg.color,
                    nubColor,
                    nub.anchoredPosition,
                    NubPosition(val),
                    animationDuration
                ));
            }
        });

        return (toggle, Refresh);

        void Refresh(bool on, bool pending)
        {
            isPending = pending;
            if (trackImg == null || nub == null || nubImg == null) return;
            trackImg.color = TrackColorFor(on, pending);
            nub.anchoredPosition = NubPosition(on);
            nubImg.color = nubColor;
        }

        Color TrackColorFor(bool on, bool pending) => pending
            ? new Color(theme.WarningColor.r, theme.WarningColor.g, theme.WarningColor.b, 0.85f)
            : on
                ? new Color(theme.SuccessColor.r, theme.SuccessColor.g, theme.SuccessColor.b, 0.85f)
                : new Color(theme.TextSecondary.r, theme.TextSecondary.g, theme.TextSecondary.b, 0.4f);
    }

    private static Image CreateTrack(Transform parent, string name, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);

        var img = go.AddComponent<Image>();
        img.sprite = UIHelper.GetRoundedSprite(SpriteSize, TrackRadius);
        img.type = Image.Type.Sliced;
        img.color = color;

        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(SliderWidth, SliderHeight);

        var le = go.AddComponent<LayoutElement>();
        le.minWidth = SliderWidth;
        le.minHeight = SliderHeight;
        le.preferredWidth = SliderWidth;
        le.preferredHeight = SliderHeight;

        return img;
    }

    private static RectTransform CreateNub(Transform parent, bool initialOn, Color color)
    {
        var go = new GameObject("Nub");
        go.transform.SetParent(parent, false);

        var img = go.AddComponent<Image>();
        img.sprite = UIHelper.GetRoundedSprite(SpriteSize, NubRadius);
        img.type = Image.Type.Sliced;
        img.color = color;

        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(NubSize, NubSize);
        rt.anchorMin = new Vector2(0, 0.5f);
        rt.anchorMax = new Vector2(0, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = NubPosition(initialOn);

        return rt;
    }

    private static System.Collections.IEnumerator AnimateToggle(RectTransform nub, Image track, Color startTrack,
        Color targetTrack, Image nubImg, Color startNub, Color targetNub, Vector2 startPos, Vector2 targetPos,
        float duration)
    {
        var elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            var t = Mathf.SmoothStep(0f, 1f, elapsed / duration);

            // Animate nub
            nub.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);

            // Animate track color
            track.color = Color.Lerp(startTrack, targetTrack, t);

            // Animate nub color
            if (nubImg != null)
                nubImg.color = Color.Lerp(startNub, targetNub, t);

            yield return null;
        }

        // Ensure final values
        nub.anchoredPosition = targetPos;
        track.color = targetTrack;
        if (nubImg != null)
            nubImg.color = targetNub;
    }

    private static Vector2 NubPosition(bool on) => on
        ? new Vector2(SliderWidth - NubSize / 2f - NubPadding, 0)
        : new Vector2(NubSize / 2f + NubPadding, 0);
}