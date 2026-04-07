using MelonLoader;
using MelonLoader.Preferences;
using ModsApp.Helpers;
using ModsApp.Helpers.Registries;
using ModsApp.Managers;
using ModsApp.UI;
using ModsApp.UI.Panels;
using ModsApp.UI.Themes;
using UnityEngine;

[assembly: MelonInfo(
    typeof(ModsApp.ModsApp),
    ModsApp.BuildInfo.Name,
    ModsApp.BuildInfo.Version,
    ModsApp.BuildInfo.Author,
    ModsApp.BuildInfo.DownloadLink
)]
[assembly: MelonColor(1, 255, 0, 0)]
[assembly: MelonGame("TVGS", "Schedule I")]
[assembly: MelonPriority(int.MinValue + 100)] // Ensure this runs before most other mods to capture logs, but after S1API

namespace ModsApp;

public static class BuildInfo
{
    public const string Name = "ModsApp";
    public const string Description = "In-game app to manage mods' preferences";
    public const string DownloadLink = "https://github.com/k073l/s1-modsapp";
    public const string Author = "k073l";
    public const string Version = "1.2.2";
}

public class ModsApp : MelonMod
{
    private static MelonLogger.Instance _logger;

    private bool _shouldUpdate = true;

    public override void OnInitializeMelon()
    {
        _logger = LoggerInstance;
        LogManager.Instance.WireEvents();

        IconRegistry.Prefetch();
        SettingsRegistry.Initialize();

        ReflectionHelper.TryInitTMP();
        ReflectionHelper.TryInitGameTypes();

        CategoryState.Load();
        ModVersionTracker.Load();

        MelonEvents.OnApplicationDefiniteQuit.Subscribe(OnDefiniteQuit);

        _logger.Msg("ModsApp initialized");
    }

    public override void OnUpdate()
    {
        if (!_shouldUpdate) return;
        try
        {
            InitHelper.CloseAppAndPanels();
        }
        catch (Exception e)
        {
            // prevent update from spamming the console
            _shouldUpdate = false;
            _logger.Error($"Update loop error. This can happen if S1API is not installed.\nException: {e}");
        }
    }

    public override void OnSceneWasLoaded(int buildIndex, string sceneName)
    {
        switch (sceneName)
        {
            case "Menu":
                MissingDepsPanelComponent.CheckAndShow();
                break;
            default:
                MissingDepsPanelComponent.Hide();
                break;
        }
    }

    private static void OnDefiniteQuit()
    {
        ModToggleManager.ApplyPendingChanges(_logger);
        CategoryState.Save();
        ModVersionTracker.Save();
    }
}