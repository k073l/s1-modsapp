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

    public bool HasUnassignedPreferences()
    {
        var allModNames = _mods.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var catObj in MelonPreferences.Categories)
        {
            var category = ExtractCategoryFromObject(catObj);
            if (category == null) continue;

            var isAssigned = allModNames.Any(modName =>
                IsCategoryForMod(category, modName));

            if (!isAssigned)
                return true;
        }

        return false;
    }

    public IEnumerable<MelonPreferences_Category> GetUnassignedPreferences()
    {
        var allModNames = _mods.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var unassigned = new List<MelonPreferences_Category>();

        foreach (var catObj in MelonPreferences.Categories)
        {
            var category = ExtractCategoryFromObject(catObj);
            if (category == null) continue;

            bool isAssigned = allModNames.Any(modName =>
                IsCategoryForMod(category, modName));

            if (!isAssigned)
                unassigned.Add(category);
        }

        return unassigned;
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
        if (MatchesModName(category.Identifier, modName))
            return true;

        if (MatchesModName(category.DisplayName, modName))
            return true;

        EnsureReflectionCacheInitialized();

        try
        {
            var filePath = TryGetCategoryFilePath(category);
            if (!string.IsNullOrEmpty(filePath) &&
                MatchesModNameInFilePath(filePath, modName))
            {
                return true;
            }
        }
        catch (Exception ex)
        {
            MelonDebug.Error($"Could not check file path for category matching {modName}: {ex.Message}");
        }

        return false;
    }


    private static bool MatchesModName(string text, string modName)
    {
        if (string.IsNullOrEmpty(text))
            return false;

        foreach (var variant in GetModNameVariants(modName))
        {
            if (text.IndexOf(variant, StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        }

        return false;
    }

    private void EnsureReflectionCacheInitialized()
    {
        if (_reflectionCacheInitialized)
            return;

        try
        {
            _cachedFileField = typeof(MelonPreferences_Category).GetField(
                "File", BindingFlags.NonPublic | BindingFlags.Instance);

            _cachedFileProperty = typeof(MelonPreferences_Category).GetProperty(
                "File", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

            _cachedDirectFilePathField = typeof(MelonPreferences_Category).GetField(
                "FilePath", BindingFlags.NonPublic | BindingFlags.Instance);

            _cachedDirectFilePathProperty = typeof(MelonPreferences_Category).GetProperty(
                "FilePath", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        }
        catch
        {
            /* Ignore reflection failures */
        }

        _reflectionCacheInitialized = true;
    }

    private string TryGetCategoryFilePath(MelonPreferences_Category category)
    {
        object fileObj = _cachedFileField?.GetValue(category)
                         ?? _cachedFileProperty?.GetValue(category);

        if (fileObj != null)
        {
            if (_cachedFilePathField == null && _cachedFilePathProperty == null)
            {
                _cachedFilePathField = fileObj.GetType().GetField(
                    "FilePath", BindingFlags.NonPublic | BindingFlags.Instance);

                _cachedFilePathProperty = fileObj.GetType().GetProperty(
                    "FilePath", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            }

            var path = _cachedFilePathField?.GetValue(fileObj) as string
                       ?? _cachedFilePathProperty?.GetValue(fileObj) as string;

            if (!string.IsNullOrEmpty(path))
                return path;
        }

        return _cachedDirectFilePathField?.GetValue(category) as string
               ?? _cachedDirectFilePathProperty?.GetValue(category) as string;
    }

    private bool MatchesModNameInFilePath(string filePath, string modName)
    {
        var fileName = Path.GetFileNameWithoutExtension(filePath);
        if (MatchesModName(fileName, modName))
            return true;

        var currentDir = Path.GetDirectoryName(filePath);
        while (!string.IsNullOrEmpty(currentDir))
        {
            var directoryName = Path.GetFileName(currentDir);
            if (MatchesModName(directoryName, modName))
                return true;

            currentDir = Path.GetDirectoryName(currentDir);
        }

        return false;
    }

    // fully qualified type name since UE would crash the runtime when loading it otherwise
    private static System.Collections.Generic.IEnumerable<string> GetModNameVariants(string modName)
    {
        if (string.IsNullOrWhiteSpace(modName))
            yield break;

        yield return modName;

        var noSpaces = modName.Replace(" ", string.Empty);
        yield return noSpaces;

        yield return modName.Replace(" ", "_");
        yield return modName.Replace(" ", "-");

        // aggressive normalized form
        var normalized = new string(
            modName
                .Where(char.IsLetterOrDigit)
                .ToArray()
        );

        if (!string.IsNullOrEmpty(normalized))
            yield return normalized;
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