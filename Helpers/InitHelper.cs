using ModsApp.Managers;
using ModsApp.UI.Input.FieldFactories;
using ModsApp.UI.Panels;
using S1API.Input;
using UnityEngine;

namespace ModsApp.Helpers;

internal static class InitHelper
{
    internal static void CloseAppAndPanels()
    {
        // Failsafe to exit typing mode when Escape is pressed
        if (App.Instance == null || !App.Instance.IsOpen()) return;
        if (!Input.GetKeyDown(KeyCode.Escape)) return;
        FloatingPanelComponent.Cleanup();
        DropdownComponent<object>.CloseAll();
        PhoneSizeManager.Instance.Collapse();
        Controls.IsTyping = false;
    }
}