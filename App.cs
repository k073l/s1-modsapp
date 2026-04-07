using System;
using System.Collections;
using System.IO;
using System.Reflection;
using MelonLoader;
using MelonLoader.Utils;
using ModsApp.Helpers;
using ModsApp.Managers;
using ModsApp.UI;
using ModsApp.UI.Input.FieldFactories;
using ModsApp.UI.Input.Handlers;
using ModsApp.UI.Panels;
using S1API.Input;
using S1API.PhoneApp;
using UnityEngine;

namespace ModsApp;

public class App : PhoneApp
{
    public static App Instance;

    protected override string AppName => "Mods";
    protected override string AppTitle => "Mods";
    protected override string IconLabel => "Mods";
    protected override string IconFileName => Path.Combine(MelonEnvironment.UserDataDirectory, "ModsApp", "appicon.png");

    private static MelonLogger.Instance _logger = Melon<ModsApp>.Logger;
    private static Transform _homeScreenInstanceTransform;
    private UIManager _uiManager;
    private ModManager _modManager;

    protected override void OnCreated()
    {
        if (!SetIconSprite(ModsApp.AppIconSprite))
            _logger.Error("[Pre-base] Failed to set app icon sprite");
        
        base.OnCreated();
        Instance = this;

        _modManager = new ModManager(_logger);
        _logger.Msg($"ModsApp initialized with {_modManager.ModCount} mods");
    }

    protected override void OnCreatedUI(GameObject container)
    {
        try
        {
            _uiManager = new UIManager(container, _modManager, _logger);
            _uiManager.Initialize();
        }
        catch (Exception ex)
        {
            _logger.Error($"UI initialization failed: {ex.Message}");
            _logger.Error(ex.StackTrace);
        }

        var homeScreenField =
            typeof(PhoneApp).GetField("_homeScreenInstance", BindingFlags.NonPublic | BindingFlags.Instance);
        var homeScreen = (Component)homeScreenField?.GetValue(this);

        _homeScreenInstanceTransform = homeScreen != null
            ? homeScreen.transform
            : container.transform.root.FindInHierarchy(
                "Player_Local/CameraContainer/Camera/OverlayCamera/GameplayMenu/Phone/phone/HomeScreen");

        if (_homeScreenInstanceTransform != null)
        {
            MelonCoroutines.Start(WaitForModsIcon());
        }
    }

    protected override void OnPhoneClosed()
    {
        Controls.IsTyping = false;
        DropdownComponent<object>.CloseAll();
        FloatingPanelComponent.Cleanup();
        PhoneSizeManager.Instance.Collapse();
        base.OnPhoneClosed();
    }

    private IEnumerator WaitForModsIcon()
    {
        Transform modsappIcon;
        while ((modsappIcon = _homeScreenInstanceTransform.FindInHierarchy("AppIcons/Mods")) == null)
            yield return new WaitForSeconds(0.1f);
        NotificationBadge.Initialize(modsappIcon);
        if (ModVersionTracker.AreAnyUpdatedOrNew())
            NotificationBadge.ShowDot();
        UpdateNotificationBadge();
        LogManager.Instance.OnError += UpdateNotificationBadge;
    }
    
    private static void UpdateNotificationBadge()
    {
        if (LogManager.Instance.ModsWithErrors.Count > 0)
        {
            NotificationBadge.ShowCount(LogManager.Instance.ModsWithErrors.Count);
        }
    }
}