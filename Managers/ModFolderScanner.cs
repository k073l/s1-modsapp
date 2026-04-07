using System.Reflection;
using MelonLoader;
using MelonLoader.Utils;
using Mono.Cecil;

namespace ModsApp.Managers;

public static class ModFolderScanner
{
    private static List<string> GetModDirectories()
    {
        try
        {
            var handlerType = typeof(MelonMod).Assembly
                .GetType("MelonLoader.Melons.MelonFolderHandler");
            var modDirsField = handlerType?.GetField("_modDirs",
                BindingFlags.NonPublic | BindingFlags.Static);
            if (modDirsField?.GetValue(null) is List<string> { Count: > 0 } modDirs)
                return [..modDirs];
        }
        catch
        {
            // ignored
        }

        return ScanModDirectoriesFallback();
    }

    private static List<string> ScanModDirectoriesFallback()
    {
        var result = new List<string>();
        var root = MelonEnvironment.ModsDirectory;
        if (!Directory.Exists(root)) return result;
        result.Add(root);

        try
        {
            var loader = LoaderConfig.Current.Loader;
            var disabled = (bool?)loader?.GetType().GetProperty("DisableSubFolderLoad")?.GetValue(loader);
            if (disabled == true) return result;
            var noManifest = (bool?)loader?.GetType().GetProperty("DisableSubFolderManifest")?.GetValue(loader) ??
                             false;
            ScanSubFolders(root, noManifest, result);
        }
        catch
        {
            ScanSubFolders(root, false, result);
        }

        return result;
    }

    private static void ScanSubFolders(string path, bool noManifest, List<string> result)
    {
        foreach (var dir in Directory.GetDirectories(path, "*", SearchOption.TopDirectoryOnly))
        {
            if (!Directory.Exists(dir)) continue;
            var name = Path.GetFileName(dir);
            if (IsNameExcluded(name)) continue;
            if (!noManifest && !File.Exists(Path.Combine(dir, "manifest.json"))) continue;
            result.Add(dir);
            ScanSubFolders(dir, true, result);
        }
    }

    private static bool IsNameExcluded(string name) =>
        name.StartsWith("~") || name.StartsWith(".") ||
        name == "Broken" || name == "Retired" || name == "Disabled";

    private static List<string> GetAllModFilePaths()
    {
        var files = new List<string>();
        foreach (var dir in GetModDirectories())
        {
            if (!Directory.Exists(dir)) continue;
            files.AddRange(Directory.GetFiles(dir, "*.dll", SearchOption.TopDirectoryOnly));
            files.AddRange(Directory.GetFiles(dir, $"*.dll{ModToggleManager.InactiveExtension}", SearchOption.TopDirectoryOnly));
        }

        return files;
    }

    private static InactiveModInfo ReadInactiveMod(string filePath)
    {
        try
        {
            var assemblyDef = AssemblyDefinition.ReadAssembly(filePath,
                new ReaderParameters { ReadWrite = false });
            var attr = assemblyDef.CustomAttributes
                .FirstOrDefault(a => a.AttributeType.Name == "MelonInfoAttribute");
            if (attr == null || attr.ConstructorArguments.Count < 4) return null;

            var name = attr.ConstructorArguments[1].Value as string;
            if (string.IsNullOrWhiteSpace(name)) return null;

            return new InactiveModInfo
            {
                Name = name,
                Version = attr.ConstructorArguments[2].Value as string ?? "1.0.0",
                Author = attr.ConstructorArguments[3].Value as string ?? "Unknown",
                FilePath = filePath,
            };
        }
        catch
        {
            return null;
        }
    }

    public static void Scan(
        out List<MelonMod> activeMods,
        out List<InactiveModInfo> inactiveMods,
        out List<string> unmatchedDlls)
    {
        activeMods = [];
        inactiveMods = [];
        unmatchedDlls = [];

        var loadedByPath = MelonMod.RegisteredMelons
            .Where(m => m?.MelonAssembly?.Assembly?.Location != null)
            .ToDictionary(m => NormalizePath(m.MelonAssembly.Assembly.Location), m => m);

        foreach (var file in GetAllModFilePaths())
        {
            if (file.EndsWith(ModToggleManager.InactiveExtension, StringComparison.OrdinalIgnoreCase))
            {
                // Skip if the active version also exists - dll wins
                var activePath = file[..^ModToggleManager.InactiveExtension.Length];
                if (File.Exists(activePath)) continue;

                var info = ReadInactiveMod(file);
                if (info != null) inactiveMods.Add(info);
            }
            else
            {
                var normalized = NormalizePath(file);
                if (loadedByPath.TryGetValue(normalized, out var mod))
                    activeMods.Add(mod);
                else
                    unmatchedDlls.Add(file);
            }
        }
    }

    private static string NormalizePath(string path) => Path.GetFullPath(path);
}