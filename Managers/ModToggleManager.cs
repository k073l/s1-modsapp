using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MelonLoader;

namespace ModsApp.Managers;

public static class ModToggleManager
{
    public const string InactiveExtension = ".inactive";

    private static readonly Dictionary<string, bool> _pendingChanges = new();
    private static HashSet<string> _protectedPaths;

    public static void RegisterProtectedAssembly(string dllPath) =>
        GetProtectedPaths().Add(NormalizePath(dllPath));

    public static bool IsProtected(string dllPath) =>
        GetProtectedPaths().Contains(NormalizePath(dllPath));

    private static HashSet<string> GetProtectedPaths()
    {
        // Protect ModsApp and it's deps from being disabled
        if (_protectedPaths != null) return _protectedPaths;
        _protectedPaths = [];

        var modsAppRefs = typeof(ModToggleManager).Assembly
            .GetReferencedAssemblies()
            .Select(r => r.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var mod in MelonMod.RegisteredMelons
                     .Where(m => m?.MelonAssembly?.Assembly?.Location != null))
        {
            var loc = mod.MelonAssembly.Assembly.Location;
            if (mod.GetType().Assembly == typeof(ModToggleManager).Assembly ||
                modsAppRefs.Contains(System.Reflection.AssemblyName.GetAssemblyName(loc).Name))
                _protectedPaths.Add(NormalizePath(loc));
        }

        return _protectedPaths;
    }

    public static bool GetDesiredState(string dllPath)
    {
        var key = NormalizePath(dllPath);
        if (_pendingChanges.TryGetValue(key, out var desired))
            return desired;
        return File.Exists(dllPath) && !File.Exists(dllPath + InactiveExtension);
    }

    public static bool HasPendingChanges => _pendingChanges.Count > 0;

    public static bool HasPendingChange(string dllPath) =>
        _pendingChanges.ContainsKey(NormalizePath(dllPath));

    public static bool HasPendingChangeByName(string modName)
    {
        var normalizedModName = modName.Trim().ToLowerInvariant();
        return _pendingChanges.Keys.Any(path =>
            ModManager.MatchesModName(Path.GetFileNameWithoutExtension(path).Trim().ToLowerInvariant(), normalizedModName));
    }

    public static void RequestEnable(string dllPath)
    {
        if (IsProtected(dllPath)) return;
        var key = NormalizePath(dllPath);
        if (File.Exists(dllPath))
            _pendingChanges.Remove(key);
        else
            _pendingChanges[key] = true;
    }

    public static void RequestDisable(string dllPath)
    {
        if (IsProtected(dllPath)) return;
        var key = NormalizePath(dllPath);
        if (!File.Exists(dllPath))
            _pendingChanges.Remove(key);
        else
            _pendingChanges[key] = false;
    }

    public static void RevertChange(string dllPath) =>
        _pendingChanges.Remove(NormalizePath(dllPath));

    public static void ApplyPendingChanges(MelonLogger.Instance logger)
    {
        foreach (var kv in _pendingChanges)
        {
            var dllPath = kv.Key;
            var shouldEnable = kv.Value;
            var inactivePath = dllPath + InactiveExtension;

            try
            {
                if (shouldEnable)
                {
                    if (File.Exists(inactivePath) && !File.Exists(dllPath))
                        File.Move(inactivePath, dllPath);
                    else if (File.Exists(dllPath))
                        logger.Warning($"[ModToggle] Skipped enable for {Path.GetFileName(dllPath)} - already active");
                }
                else
                {
                    if (File.Exists(dllPath) && !File.Exists(inactivePath))
                        File.Move(dllPath, inactivePath);
                    else if (File.Exists(dllPath) && File.Exists(inactivePath))
                        logger.Warning(
                            $"[ModToggle] Both {Path.GetFileName(dllPath)} and {InactiveExtension} exist - delete one manually");
                }
            }
            catch (Exception ex)
            {
                logger.Error($"[ModToggle] Failed to toggle {Path.GetFileName(dllPath)}: {ex.Message}");
            }
        }

        _pendingChanges.Clear();
    }

    private static string NormalizePath(string path)
    {
        var p = Path.GetFullPath(path);
        if (p.EndsWith(InactiveExtension))
            p = p[..^InactiveExtension.Length];
        return p;
    }
}

public class InactiveModInfo
{
    public string Name { get; set; }
    public string Version { get; set; }
    public string Author { get; set; }
    public string FilePath { get; set; }
    public string ActivePath => FilePath[..^ModToggleManager.InactiveExtension.Length];
}