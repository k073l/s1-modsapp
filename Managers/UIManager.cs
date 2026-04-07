using MelonLoader;
using ModsApp.Helpers;
using ModsApp.Helpers.Registries;
using ModsApp.UI;
using ModsApp.UI.Panels;
using ModsApp.UI.Themes;
using S1API.Internal.Abstraction;
using S1API.UI;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace ModsApp.Managers;

public class UIManager
{
    private readonly GameObject _container;
    private readonly ModManager _modManager;
    private readonly MelonLogger.Instance _logger;

    internal static ModListPanel ModListPanel;
    private ModDetailsPanel _modDetailsPanel;
    private Action _openLogsAction;
    private Action _maximizeAppAction;
    internal static GameObject MainPanel;
    internal static Button LogsBtn;
    internal static Button MaximizeBtn;
    public static UITheme _theme;
    
    private MelonMod _selectedMod;
    private InactiveModInfo _selectedInactiveMod;

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
        ModListPanel.PopulateList();
        _modDetailsPanel.ShowWelcome();
    }

    private void CreateMainLayout()
    {
        var mainBg = UIFactory.Panel("MainBG", _container.transform, _theme.BgPrimary, fullAnchor: true);
        MainPanel = mainBg;
        var topBar = UIFactory.TopBar("ModsTopBar", mainBg.transform, "Mods", 0.95f, 75, 75, 85, 35);

        var (_, maximizeBtn, _) = UIHelper.RoundedButtonWithIcon("MaximizeBtn", IconRegistry.MaximizeIconSprite, topBar.transform, _theme.AccentSecondary, 40, 40, _theme.SizeMedium);
        var (_, logsBtn, _) = UIFactory.RoundedButtonWithLabel("LogsBtn", "Logs", topBar.transform, _theme.AccentPrimary, 80, 40, _theme.SizeMedium, _theme.TextPrimary);
        var hLayout = topBar.GetComponent<HorizontalLayoutGroup>();
        if (hLayout != null)
        {
            hLayout.childControlHeight = false;
        }
        MaximizeBtn = maximizeBtn;
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

        ModListPanel = new ModListPanel(mainBg, _modManager, _theme, _logger);
        _modDetailsPanel = new ModDetailsPanel(mainBg, _modManager, _theme, _logger);

        ModListPanel.Initialize();
        _modDetailsPanel.Initialize();

        // Connect panels
        ModListPanel.OnModSelected += mod =>
        {
            _selectedMod = mod;
            _selectedInactiveMod = null;
            _modDetailsPanel.ShowModDetails(mod);
        };
        ModListPanel.OnInactiveModSelected += inactive =>
        {
            _selectedInactiveMod = inactive;
            _selectedMod = null;
            _modDetailsPanel.ShowInactiveModDetails(inactive);
        };
        _openLogsAction = () =>
        {
            _ = new LogExplorerPanel(_selectedMod);
        };
        _maximizeAppAction = () =>
        {
            if (!PhoneSizeManager.Instance.Available)
            {
                _logger.Warning("Changing phone size is not available. Report this issue.");
                return;
            }
            PhoneSizeManager.Instance.Toggle();
        };

        EventHelper.AddListener(_openLogsAction, LogsBtn.onClick);
        EventHelper.AddListener(_maximizeAppAction, MaximizeBtn.onClick);
    }

    private void WirePreferences()
    {
        SettingsRegistry.TextSizeProfileEntry.OnEntryValueChanged.Subscribe((_, newVal) =>
        {
            _theme.SetTextScale(newVal);
            FullRepaint();
        });

        SettingsRegistry.TextSizeProfileEntry.OnEntryValueChanged.Subscribe((_, newVal) => _theme.SetTextScale(newVal));
        SettingsRegistry.UseNewJsonEditor.OnEntryValueChanged.Subscribe((_, _) => ReflectionHelper.TryInitTMP());
        SettingsRegistry.ThemeOptionEntry.OnEntryValueChanged.Subscribe((_, newVal) =>
        {
            _theme.SetTheme(newVal);
            if (newVal != ThemeOption.Custom && SettingsRegistry.CopyCurrentToCustom.Value)
                _theme.CopyThemeToCustom();
            FullRepaint();
        });
        SettingsRegistry.CopyCurrentToCustom.OnEntryValueChanged.Subscribe((_, newVal) =>
        {
            if (!newVal) return;
            if (SettingsRegistry.ThemeOptionEntry.Value == ThemeOption.Custom) return;
            _theme.CopyThemeToCustom();
        });
    }

    private void FullRepaint()
    {
        Melon<ModsApp>.Logger.Msg("Closing app for a full UI refresh...");
        var openedMod = _selectedMod;
        var openedInactive = _selectedInactiveMod;
        App.Instance.CloseApp();
        EventHelper.RemoveListener(_openLogsAction, LogsBtn.onClick);
        EventHelper.RemoveListener(_maximizeAppAction, MaximizeBtn.onClick);
        _selectedMod = null;
        Object.Destroy(MainPanel);
        Initialize();
        App.Instance.OpenApp();
        if (_selectedInactiveMod != null)
        {
            ModListPanel.SelectedModName = openedInactive.FilePath;
            ModListPanel.UpdateButtonHighlights();
            _modDetailsPanel.ShowInactiveModDetails(openedInactive);
            return;
        }
        if (openedMod == null || _modDetailsPanel == null || ModListPanel == null) return;
        ModListPanel.SelectedModName = openedMod.Info.Name;
        ModListPanel.UpdateButtonHighlights();
        _modDetailsPanel.ShowModDetails(openedMod);
    }
}