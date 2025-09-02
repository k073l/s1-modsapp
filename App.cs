using System;
using System.IO;
using MelonLoader;
using MelonLoader.Utils;
using S1API.PhoneApp;
using UnityEngine;

namespace ModsApp
{
    public class App : PhoneApp
    {
        public static App Instance;
        
        protected override string AppName => "Mods";
        protected override string AppTitle => "Mods";
        protected override string IconLabel => "ModsApp";
        protected override string IconFileName => Path.Combine(MelonEnvironment.UserDataDirectory, "ModsApp", "heart.png");
        
        private static MelonLogger.Instance _logger = Melon<ModsApp>.Logger;
        private UIManager _uiManager;
        private ModManager _modManager;

        protected override void OnCreated()
        {
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
    }
}