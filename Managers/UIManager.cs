using System;
using UnityEngine;
using MelonLoader;
using ModsApp.Helpers;
using ModsApp.UI;
using ModsApp.UI.Panels;
using S1API.Internal.Abstraction;
using S1API.UI;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace ModsApp.Managers;

public class UIManager
{
    private readonly GameObject _container;
    private readonly ModManager _modManager;
    private readonly MelonLogger.Instance _logger;

    private ModListPanel _modListPanel;
    private ModDetailsPanel _modDetailsPanel;
    private Action _openLogsAction;
    internal static GameObject MainPanel;
    internal static Button LogsBtn;
    public static UITheme _theme;
    
    private MelonMod _selectedMod;

    public UIManager(GameObject container, ModManager modManager, MelonLogger.Instance logger)
    {
        _container = container;
        _modManager = modManager;
        _logger = logger;
        _theme = new UITheme();
        WirePreferences();
    }

    public void Initialize()
    {
        CreateMainLayout();
        SetupPanels();
        _modListPanel.PopulateList();
        _modDetailsPanel.ShowWelcome();
    }

    private void CreateMainLayout()
    {
        var mainBg = UIFactory.Panel("MainBG", _container.transform, _theme.BgPrimary, fullAnchor: true);
        MainPanel = mainBg;
        var topBar = UIFactory.TopBar("ModsTopBar", mainBg.transform, "Mods", 0.95f, 75, 75, 85, 35);

        var (_, logsBtn, _) = UIFactory.RoundedButtonWithLabel("LogsBtn", "Logs", topBar.transform, _theme.AccentPrimary, 80, 40, _theme.SizeMedium, _theme.TextPrimary);
        var hLayout = topBar.GetComponent<HorizontalLayoutGroup>();
        if (hLayout != null)
        {
            hLayout.childControlHeight = false;
        }
        LogsBtn = logsBtn;

        // Apply theme to top bar
        var topBarImg = topBar.GetComponent<UnityEngine.UI.Image>();
        if (topBarImg != null) topBarImg.color = _theme.BgSecondary;

        UIHelper.DumpRect(_container.name, _container.GetComponent<RectTransform>());
        UIHelper.DumpRect("MainBG", mainBg.GetComponent<RectTransform>());
        UIHelper.DumpRect("TopBar", topBar.GetComponent<RectTransform>());
    }

    private void SetupPanels()
    {
        var mainBg = MainPanel.transform;

        _modListPanel = new ModListPanel(mainBg, _modManager, _theme, _logger);
        _modDetailsPanel = new ModDetailsPanel(mainBg, _modManager, _theme, _logger);

        _modListPanel.Initialize();
        _modDetailsPanel.Initialize();

        // Connect panels
        _modListPanel.OnModSelected += mod =>
        {
            _selectedMod = mod;
            _modDetailsPanel.ShowModDetails(mod);
        };
        _openLogsAction = () =>
        {
            _ = new LogExplorerPanel(_selectedMod);
        };

        EventHelper.AddListener(_openLogsAction, LogsBtn.onClick);
    }

    private void WirePreferences()
    {
        ModsApp.TextSizeProfileEntry.OnEntryValueChanged.Subscribe((_, newVal) => _theme.SetTextScale(newVal));
        ModsApp.ThemeOptionEntry.OnEntryValueChanged.Subscribe((_, newVal) =>
        {
            _theme.SetTheme(newVal);
            App.Instance.CloseApp();
            EventHelper.RemoveListener(_openLogsAction, LogsBtn.onClick);
            _selectedMod = null;
            Object.Destroy(MainPanel);
            Initialize();
        });
    }
}