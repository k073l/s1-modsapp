using System.Reflection;
using S1API.Utils;
using UnityEngine;

namespace ModsApp.Helpers.Registries;

public class IconRegistry
{
    private static Sprite _appIconSprite;
    private static Sprite _warningIconSprite;
    private static Sprite _scrollIconSprite;
    private static Sprite _undoIconSprite;
    private static Sprite _maximizeIconSprite;
    public static Sprite AppIconSprite => GetIcon(ref _appIconSprite, "ModsApp.assets.appicon.png");
    public static Sprite WarningIconSprite => GetIcon(ref _warningIconSprite, "ModsApp.assets.triangle-alert.png");
    public static Sprite ScrollIconSprite => GetIcon(ref _scrollIconSprite, "ModsApp.assets.scroll-text.png");
    public static Sprite UndoIconSprite => GetIcon(ref _undoIconSprite, "ModsApp.assets.undo.png");
    public static Sprite MaximizeIconSprite => GetIcon(ref _maximizeIconSprite, "ModsApp.assets.maximize.png");

    public static void Prefetch()
    {
        try
        {
            _ = AppIconSprite;
            _ = WarningIconSprite;
            _ = ScrollIconSprite;
            _ = UndoIconSprite;
            _ = MaximizeIconSprite;
        }
        catch
        {
            // ignored
        }
    }
    
    private static Sprite GetIcon(ref Sprite spriteField, string resourceName)
    {
        if (spriteField == null)
        {
            spriteField = LoadEmbeddedPNG(resourceName);
        }

        return spriteField;
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
}