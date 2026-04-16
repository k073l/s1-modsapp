using System.Collections;
using MelonLoader;
using ModsApp.Helpers;
using ModsApp.Managers;
using S1API.Internal.Abstraction;
using S1API.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace ModsApp.UI;

public static class Tooltip
{
    private static GameObject _tooltipObject;
    private static RectTransform _tooltipRect;
    private static Text _tooltipText;
    private static GameObject _tooltipCanvas;
    private static readonly Dictionary<GameObject, TooltipInstance> _instances = new();
    private static object _showCoroutine;
    private static GameObject _currentTarget;
    private static float _maxWidth = float.MinValue;

    private const float ShowDelay = 0.3f;
    private const float CursorOffsetX = -5f;
    private const float CursorOffsetY = -25f - TooltipHeight;
    private const float TooltipWidthMultiplier = 12f;
    private const float TooltipMinWidth = 150f;
    private const float TooltipHeight = 32f;
    private const float TooltipPadding = 8f;

    private class TooltipInstance
    {
        public GameObject Target;
        public string Text;
        public float MaxWidth;
        public EventTrigger Trigger;
        public Action<BaseEventData> EnterHandler;
        public Action<BaseEventData> ExitHandler;
        public WeakReference<GameObject> TargetRef;
    }

    public static void Attach(GameObject target, string text, float maxWidth = float.MinValue)
    {
        if (target == null) return;

        _maxWidth = maxWidth;
        Detach(target);

        var instance = new TooltipInstance
        {
            Target = target,
            Text = text,
            MaxWidth = maxWidth,
            TargetRef = new WeakReference<GameObject>(target)
        };

        var trigger = target.GetComponent<EventTrigger>() ?? target.AddComponent<EventTrigger>();

        instance.EnterHandler = _ => OnPointerEnter(instance);
        instance.ExitHandler = _ => OnPointerExit(instance);
        instance.Trigger = trigger;

        EventHelper.AddEventTrigger(trigger, EventTriggerType.PointerEnter, instance.EnterHandler);
        EventHelper.AddEventTrigger(trigger, EventTriggerType.PointerExit, instance.ExitHandler);

        _instances[target] = instance;

        EnsureTooltipExists();
    }

    public static void Detach(GameObject target)
    {
        if (target == null || !_instances.TryGetValue(target, out var instance)) return;

        if (instance.Trigger != null)
        {
            EventHelper.RemoveEventTrigger(instance.Trigger, EventTriggerType.PointerEnter, instance.EnterHandler);
            EventHelper.RemoveEventTrigger(instance.Trigger, EventTriggerType.PointerExit, instance.ExitHandler);
            Object.Destroy(instance.Trigger);
        }

        _instances.Remove(target);

        if (_currentTarget == target)
        {
            _currentTarget = null;
            HideTooltip();
        }
    }

    public static void Cleanup()
    {
        foreach (var target in _instances.Keys.ToList())
        {
            Detach(target);
        }

        if (_showCoroutine != null)
            MelonCoroutines.Stop(_showCoroutine);

        if (_tooltipObject != null)
        {
            Object.Destroy(_tooltipObject);
            _tooltipObject = null;
        }

        if (_tooltipCanvas != null)
        {
            Object.Destroy(_tooltipCanvas);
            _tooltipCanvas = null;
        }
    }

    public static void Update()
    {
        if (_currentTarget == null)
        {
            HideTooltip();
            return;
        }

        if (!_currentTarget.activeInHierarchy)
        {
            _currentTarget = null;
            HideTooltip();
            return;
        }

        if (!_instances.TryGetValue(_currentTarget, out var instance))
        {
            _currentTarget = null;
            HideTooltip();
            return;
        }

        if (!instance.TargetRef.TryGetTarget(out var resolved) || resolved == null)
        {
            Detach(_currentTarget);
            _currentTarget = null;
            HideTooltip();
            return;
        }

        PositionNearCursor();
    }

    private static void OnPointerEnter(TooltipInstance instance)
    {
        if (_showCoroutine != null)
            MelonCoroutines.Stop(_showCoroutine);

        if (_currentTarget != instance.Target)
        {
            HideTooltip();
        }

        _currentTarget = instance.Target;
        _showCoroutine = MelonCoroutines.Start(ShowAfterDelay(instance));
    }

    private static void OnPointerExit(TooltipInstance instance)
    {
        if (_currentTarget == instance.Target)
        {
            _currentTarget = null;
            HideTooltip();
        }

        if (_showCoroutine != null)
            MelonCoroutines.Stop(_showCoroutine);
    }

    private static IEnumerator ShowAfterDelay(TooltipInstance instance)
    {
        yield return new WaitForSeconds(ShowDelay);

        if (!IsTargetValid(instance.Target) || _currentTarget != instance.Target)
            yield break;

        if (_tooltipText == null || _tooltipObject == null)
            yield break;

        _tooltipText.text = WrapText(instance.Text, instance.MaxWidth);
        _tooltipObject.SetActive(true);
        PositionNearCursor();
    }

    private static void HideTooltip()
    {
        if (_tooltipObject != null)
            _tooltipObject.SetActive(false);
    }

    private static void PositionNearCursor()
    {
        if (_tooltipRect == null) return;

        _tooltipRect.position = new Vector2(
            UnityEngine.Input.mousePosition.x + CursorOffsetX,
            UnityEngine.Input.mousePosition.y + CursorOffsetY
        );
    }

    private static void EnsureTooltipExists()
    {
        if (_tooltipObject != null) return;

        if (_tooltipCanvas == null)
        {
            _tooltipCanvas = new GameObject("ModsAppTooltipCanvas");
            var canvas = _tooltipCanvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999;
            _tooltipCanvas.AddComponent<CanvasScaler>();
            _tooltipCanvas.AddComponent<GraphicRaycaster>();
            Object.DontDestroyOnLoad(_tooltipCanvas);
        }

        var theme = UIManager._theme;

        float width;
        if (_maxWidth > 0)
            width = _maxWidth;
        else if (theme != null)
            width = theme.SizeSmall * TooltipWidthMultiplier;
        else
            width = TooltipMinWidth;

        _tooltipObject = new GameObject("Tooltip");
        _tooltipObject.transform.SetParent(_tooltipCanvas.transform, false);
        _tooltipObject.SetActive(false);

        var img = _tooltipObject.AddComponent<Image>();
        img.sprite = UIHelper.GetRoundedSprite(32, 8);
        img.type = Image.Type.Sliced;
        img.color = theme!.BgPrimary; // if null, the whole UI breaks so whatever
        img.raycastTarget = false;

        _tooltipRect = _tooltipObject.GetComponent<RectTransform>();
        _tooltipRect.pivot = new Vector2(0f, 0f);
        _tooltipRect.sizeDelta = new Vector2(width, TooltipHeight);

        _tooltipText = UIFactory.Text("TooltipText", string.Empty, _tooltipObject.transform, theme.SizeSmall,
            TextAnchor.UpperLeft);
        _tooltipText.color = theme.TextPrimary;
        _tooltipText.horizontalOverflow = HorizontalWrapMode.Wrap;
        _tooltipText.verticalOverflow = VerticalWrapMode.Overflow;
        _tooltipText.raycastTarget = false;

        var textRect = _tooltipText.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(TooltipPadding, TooltipPadding);
        textRect.offsetMax = new Vector2(-TooltipPadding, -TooltipPadding);
    }

    private static bool IsTargetValid(GameObject target)
    {
        if (target == null) return false;
        if (!_instances.TryGetValue(target, out var instance)) return false;
        return instance.TargetRef.TryGetTarget(out var resolved) && resolved != null;
    }

    private static string WrapText(string text, float maxWidth)
    {
        return text;
    }
}