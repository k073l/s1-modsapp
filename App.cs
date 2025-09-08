using System;
using System.IO;
using MelonLoader;
using MelonLoader.Utils;
using ModsApp.Managers;
using ModsApp.UI.Input.Handlers;
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
    private UIManager _uiManager;
    private ModManager _modManager;

    protected override void OnCreated()
    {
        if (ModsApp.AppIconSprite == null)
        {
            // il2cpp :)
            ModsApp.AppIconSprite = ModsApp.LoadEmbeddedPNG("ModsApp.assets.appicon.png");
        }
        
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
    }

    protected override void OnPhoneClosed()
    {
        Controls.IsTyping = false;
        if (ColorInputHandler.ColorPickerCanvas != null)
            GameObject.Destroy(ColorInputHandler.ColorPickerCanvas);
        base.OnPhoneClosed();
    }
}