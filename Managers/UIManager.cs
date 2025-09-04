using System;
using UnityEngine;
using MelonLoader;
using ModsApp.Helpers;
using ModsApp.UI.Panels;
using S1API.UI;

namespace ModsApp.Managers;

public class UIManager
{
    private readonly GameObject _container;
    private readonly ModManager _modManager;
    private readonly MelonLogger.Instance _logger;

    private ModListPanel _modListPanel;
    private ModDetailsPanel _modDetailsPanel;
    private UITheme _theme;

    public UIManager(GameObject container, ModManager modManager, MelonLogger.Instance logger)
    {
        _container = container;
        _modManager = modManager;
        _logger = logger;
        _theme = new UITheme();
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
        var topBar = UIFactory.TopBar("ModsTopBar", mainBg.transform, "Mods", 0.95f, 75, 75, 85, 35);

        // Apply theme to top bar
        var topBarImg = topBar.GetComponent<UnityEngine.UI.Image>();
        if (topBarImg != null) topBarImg.color = _theme.BgSecondary;

        UIHelper.DumpRect(_container.name, _container.GetComponent<RectTransform>());
        UIHelper.DumpRect("MainBG", mainBg.GetComponent<RectTransform>());
        UIHelper.DumpRect("TopBar", topBar.GetComponent<RectTransform>());
    }

    private void SetupPanels()
    {
        var mainBg = _container.transform.Find("MainBG");

        _modListPanel = new ModListPanel(mainBg, _modManager, _theme, _logger);
        _modDetailsPanel = new ModDetailsPanel(mainBg, _modManager, _theme, _logger);

        _modListPanel.Initialize();
        _modDetailsPanel.Initialize();

        // Connect panels
        _modListPanel.OnModSelected += _modDetailsPanel.ShowModDetails;
    }
}