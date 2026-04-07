using System.Reflection;
using ModsApp.Managers;
using ModsApp.UI.Input.FieldFactories;
using ModsApp.UI.Panels;
using S1API.Input;
using S1API.Utils;
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

    private static Sprite LoadEmbeddedPNG(string resourceName)
    {
        var asm = Assembly.GetExecutingAssembly();

        using var stream = asm.GetManifestResourceStream(resourceName);
        if (stream == null) return null;

        var data = new byte[stream.Length];
        stream.Read(data, 0, data.Length);
        var sprite = ImageUtils.LoadImageRaw(data);
        if (sprite != null) sprite.name = resourceName;
        return sprite;
    }

    internal static Sprite GetIcon(ref Sprite spriteField, string resourceName)
    {
        if (spriteField == null)
        {
            spriteField = LoadEmbeddedPNG(resourceName);
        }

        return spriteField;
    }
}