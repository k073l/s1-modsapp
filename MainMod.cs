using System.Collections;
using System.Reflection;
using MelonLoader;
using ModsApp.Helpers;
using S1API.Input;
using S1API.Internal.Utils;
using UnityEngine;
using S1API.PhoneApp;

[assembly: MelonInfo(
    typeof(ModsApp.ModsApp),
    ModsApp.BuildInfo.Name,
    ModsApp.BuildInfo.Version,
    ModsApp.BuildInfo.Author
)]
[assembly: MelonColor(1, 255, 0, 0)]
[assembly: MelonGame("TVGS", "Schedule I")]

namespace ModsApp;

public static class BuildInfo
{
    public const string Name = "ModsApp";
    public const string Description = "does stuff i guess";
    public const string Author = "me";
    public const string Version = "1.0.0";
}

public class ModsApp : MelonMod
{
    private static MelonLogger.Instance Logger;
    public static Sprite AppIconSprite;

    public override void OnInitializeMelon()
    {
        Logger = LoggerInstance;
        Logger.Msg("ModsApp initialized");
        AppIconSprite = LoadEmbeddedPNG("ModsApp.assets.appicon.png");
    }
    
    public static Sprite LoadEmbeddedPNG(string resourceName)
    {
        Assembly asm = Assembly.GetExecutingAssembly();

        using Stream stream = asm.GetManifestResourceStream(resourceName);
        if (stream == null) return null;

        var data = new byte[stream.Length];
        stream.Read(data, 0, data.Length);
        return ImageUtils.LoadImageRaw(data);
    }

    public override void OnUpdate()
    {
        // Failsafe to exit typing mode when Escape is pressed
        if (App.Instance != null && App.Instance.IsOpen())
            if (UnityEngine.Input.GetKeyDown(KeyCode.Escape))
                Controls.IsTyping = false;
    }
}