using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MelonLoader;
using Mono.Cecil;

namespace ModsApp.Managers;

public class ModManager
{
    private readonly Dictionary<string, MelonMod> _mods = new();
    private readonly MelonLogger.Instance _logger;

    private static FieldInfo _cachedFileField;
    private static PropertyInfo _cachedFileProperty;
    private static FieldInfo _cachedFilePathField;
    private static PropertyInfo _cachedFilePathProperty;
    private static FieldInfo _cachedDirectFilePathField;
    private static PropertyInfo _cachedDirectFilePathProperty;
    private static bool _reflectionCacheInitialized;

    public ModManager(MelonLogger.Instance logger)
    {
        _logger = logger;
        LoadMods();
    }

    public int ModCount => _mods.Count;

    public IEnumerable<MelonMod> GetAllMods() =>
        _mods.Values.OrderBy(m => m.Info.Name, StringComparer.OrdinalIgnoreCase);

    public MelonMod GetMod(string name) => _mods.ContainsKey(name) ? _mods[name] : null;

    private void LoadMods()
    {
        foreach (var mod in MelonMod.RegisteredMelons)
        {
            _mods[mod.Info.Name] = mod;
        }
    }

    public IEnumerable<MelonPreferences_Category> GetPreferencesForMod(MelonMod mod)
    {
        var modName = mod.Info.Name;
        var categories = new List<MelonPreferences_Category>();

        try
        {
            foreach (var catObj in MelonPreferences.Categories)
            {
                var category = ExtractCategoryFromObject(catObj);
                if (category != null && IsCategoryForMod(category, modName))
                {
                    categories.Add(category);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Error retrieving preferences for {modName}: {ex.Message}");
        }

        return categories;
    }

    private MelonPreferences_Category ExtractCategoryFromObject(object catObj)
    {
        if (catObj is MelonPreferences_Category direct)
            return direct;

        var type = catObj?.GetType();
        if (type?.IsGenericType != true || type.GetGenericTypeDefinition() != typeof(KeyValuePair<,>)) return null;
        var valueProp = type.GetProperty("Value");
        return valueProp?.GetValue(catObj) as MelonPreferences_Category;
    }

    private bool IsCategoryForMod(MelonPreferences_Category category, string modName)
    {
        // first check: Category Identifier
        if (!string.IsNullOrEmpty(category.Identifier) &&
            category.Identifier.IndexOf(modName, StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return true;
        }

        // second check: Category Display Name
        if (!string.IsNullOrEmpty(category.DisplayName) &&
            category.DisplayName.IndexOf(modName, StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return true;
        }

        if (!_reflectionCacheInitialized)
        {
            try
            {
                // try both field and property on MelonPreferences_Category
                _cachedFileField = typeof(MelonPreferences_Category).GetField("File",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                _cachedFileProperty = typeof(MelonPreferences_Category).GetProperty("File",
                    BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                _cachedDirectFilePathField = typeof(MelonPreferences_Category).GetField("FilePath",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                _cachedDirectFilePathProperty = typeof(MelonPreferences_Category).GetProperty("FilePath",
                    BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            }
            catch
            {
                /* Ignore reflection failures during initialization */
            }

            _reflectionCacheInitialized = true;
        }

        // third check: File path as last resort
        try
        {
            string filePath = null;

            object fileObj = _cachedFileField?.GetValue(category);
            if (fileObj == null)
            {
                fileObj = _cachedFileProperty?.GetValue(category);
            }

            if (fileObj != null)
            {
                if (_cachedFilePathField == null && _cachedFilePathProperty == null)
                {
                    _cachedFilePathField = fileObj.GetType().GetField("FilePath",
                        BindingFlags.NonPublic | BindingFlags.Instance);
                    _cachedFilePathProperty = fileObj.GetType().GetProperty("FilePath",
                        BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                }

                filePath = _cachedFilePathField?.GetValue(fileObj) as string;
                if (string.IsNullOrEmpty(filePath))
                {
                    filePath = _cachedFilePathProperty?.GetValue(fileObj) as string;
                }
            }

            if (string.IsNullOrEmpty(filePath))
            {
                filePath = _cachedDirectFilePathField?.GetValue(category) as string;
                if (string.IsNullOrEmpty(filePath))
                {
                    filePath = _cachedDirectFilePathProperty?.GetValue(category) as string;
                }
            }

            if (!string.IsNullOrEmpty(filePath))
            {
                var fileName = Path.GetFileNameWithoutExtension(filePath);

                if (fileName.IndexOf(modName, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }

                var currentDir = Path.GetDirectoryName(filePath);
                while (!string.IsNullOrEmpty(currentDir))
                {
                    var directoryName = Path.GetFileName(currentDir);
                    if (!string.IsNullOrEmpty(directoryName) &&
                        directoryName.IndexOf(modName, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return true;
                    }

                    currentDir = Path.GetDirectoryName(currentDir);
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.Msg($"Could not check file path for category matching {modName}: {ex.Message}");
        }

        return false;
    }

    private Backend DetermineBackend(MelonMod mod)
    {
        var assemblyPath = mod.MelonAssembly.Assembly.Location;
        var assemblyDef = AssemblyDefinition.ReadAssembly(assemblyPath);

        var hasIL2CPP = false;
        var hasMono = false;
        var hasS1API = false;

        foreach (var module in assemblyDef.Modules)
        {
            // Check defined types
            foreach (var type in module.Types)
            {
                if (!hasIL2CPP && type.FullName.StartsWith("Il2Cpp"))
                    hasIL2CPP = true;

                if (!hasMono && type.FullName.Contains("ScheduleOne"))
                    hasMono = true;
            }

            // Check referenced types
            foreach (var typeRef in module.GetTypeReferences())
            {
                if (!hasS1API && typeRef.Namespace.StartsWith("S1API"))
                    hasS1API = true;

                if (!hasIL2CPP && typeRef.Namespace.StartsWith("Il2Cpp"))
                    hasIL2CPP = true;

                if (!hasMono && typeRef.Namespace.Contains("ScheduleOne"))
                    hasMono = true;
            }

            if (hasIL2CPP || hasMono)
                break;
        }

        if (hasIL2CPP)
            return Backend.IL2CPP;
        if (hasMono)
            return Backend.Mono;
        return hasS1API ? Backend.S1API : Backend.Unknown;
    }

    public bool isCompatible(MelonMod mod, ref string backend)
    {
        var modBackend = DetermineBackend(mod);
        backend = modBackend.ToString();

        if (modBackend == Backend.Unknown)
            return false;

        var isGameIL2CPP = MelonUtils.IsGameIl2Cpp();

        if (modBackend == Backend.IL2CPP && isGameIL2CPP)
            return true;
        if (modBackend == Backend.Mono && !isGameIL2CPP)
            return true;
        if (modBackend == Backend.S1API)
            return true;

        return false;
    }
}

internal enum Backend
{
    Unknown,
    IL2CPP,
    Mono,
    S1API
}