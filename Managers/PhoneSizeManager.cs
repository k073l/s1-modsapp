using MelonLoader;
using ModsApp.Helpers;
using UnityEngine;

namespace ModsApp.Managers;

internal class PhoneSizeManager
{
    private readonly MelonLogger.Instance _logger;
    private static PhoneSizeManager _instance;
    private bool _active;

    public static PhoneSizeManager Instance =>
        _instance ??= new PhoneSizeManager(new MelonLogger.Instance("PhoneSizeManager"));

    internal bool Available => ReflectionHelper.S1TypesAvailable;

    private PhoneSizeManager(MelonLogger.Instance logger)
    {
        _logger = logger;
        ReflectionHelper.TryInitGameTypes();
    }

    public void Toggle()
    {
        if (!Available)
        {
            _logger.Warning("Required game types not available, cannot toggle phone size.");
            return;
        }

        if (_active)
            Collapse();
        else
            Expand();
    }

    public void Expand()
    {
        var phone = PhoneGameObject;
        if (phone == null) return;
        var scale = GetTargetScale();
        phone.transform.localScale = new Vector3(scale, 5f, scale);
        _active = true;
    }

    public void Collapse()
    {
        var phone = PhoneGameObject;
        if (phone == null) return;
        phone.transform.localScale = new Vector3(5f, 5f, 5f);
        _active = false;
    }

    private float? UIScale
    {
        get
        {
            if (!Available) return null;
            var settings = ReflectionHelper.GetValue(ReflectionHelper.MSettingsInstance, null);
            var display = ReflectionHelper.GetValue(ReflectionHelper.MSettingsDisplaySettings, settings);
            var uiScale = ReflectionHelper.GetValue(ReflectionHelper.MDisplaySettingsUIScale, display);
            return uiScale is float f ? f : null;
        }
    }

    private GameObject? PhoneGameObject
    {
        get
        {
            if (!Available) return null;
            var phone = ReflectionHelper.GetValue(ReflectionHelper.MPhoneInstance, null);
            return phone is Component c ? c.gameObject : null;
        }
    }

    private float GetTargetScale()
    {
        var uiScale = UIScale;
        if (uiScale == null) return 5f;
        var target = -4.28f * uiScale + 11f;
        return Mathf.Clamp(target.Value, 5f, 8f);
    }
}