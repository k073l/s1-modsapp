using System.Reflection;
using MelonLoader;
using MelonLoader.Utils;
using Mono.Cecil;

namespace ModsApp.Managers;

public class ModManager
{
    private readonly Dictionary<string, MelonMod> _mods = new();
    private readonly MelonLogger.Instance _logger;

    private static readonly Dictionary<MelonMod, ModDependencyInfo> DependencyCache = new();
    private static readonly Dictionary<MelonMod, Backend> BackendCache = new();

    private static FieldInfo _cachedFileField;
    private static PropertyInfo _cachedFileProperty;
    private static FieldInfo _cachedFilePathField;
    private static PropertyInfo _cachedFilePathProperty;
    private static FieldInfo _cachedDirectFilePathField;
    private static PropertyInfo _cachedDirectFilePathProperty;
    private static bool _reflectionCacheInitialized;

    private List<InactiveModInfo> _inactiveModsCache;

    public ModManager(MelonLogger.Instance logger)
    {
        _logger = logger;
        LoadMods();
    }

    public int ModCount => _mods.Count;

    public IEnumerable<MelonMod> GetAllMods() =>
        _mods.Values.OrderBy(m => m.Info.Name, StringComparer.OrdinalIgnoreCase);

    public MelonMod GetMod(string name) => _mods.TryGetValue(name, out var mod) ? mod : null;

    public MelonMod GetModByAssemblyName(string assemblyName) =>
        _mods.Values.FirstOrDefault(m =>
            m.MelonAssembly?.Assembly?.GetName().Name
                .Equals(assemblyName, StringComparison.OrdinalIgnoreCase) == true);

    public Assembly GetAssemblyByAssemblyName(string assemblyName) =>
        AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name.Equals(assemblyName, StringComparison.OrdinalIgnoreCase));

    public List<InactiveModInfo> GetInactiveMods()
    {
        if (_inactiveModsCache != null) return _inactiveModsCache;
        ModFolderScanner.Scan(out _, out var inactive, out _);
        _inactiveModsCache = inactive;
        return _inactiveModsCache;
    }

    public void InvalidateScanCache() => _inactiveModsCache = null;

    private void LoadMods()
    {
        foreach (var mod in MelonMod.RegisteredMelons)
            _mods[mod.Info.Name] = mod;
    }

    #region Preferences

    public IEnumerable<MelonPreferences_Category> GetPreferencesForMod(MelonMod mod)
    {
        var modName = mod.Info.Name;
        var categories = new List<MelonPreferences_Category>();

        try
        {
            foreach (var catObj in MelonPreferences.Categories)
            {
                var category = ExtractCategoryFromObject(catObj);
                if (category != null &&
                    IsCategoryForMod(category, modName, mod.MelonAssembly?.Assembly?.GetName().Name))
                    categories.Add(category);
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
            if (!allModNames.Any(n =>
                    IsCategoryForMod(category, n, GetMod(n)?.MelonAssembly?.Assembly?.GetName().Name)))
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
            if (!allModNames.Any(n =>
                    IsCategoryForMod(category, n, GetMod(n)?.MelonAssembly?.Assembly?.GetName().Name)))
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
        return type.GetProperty("Value")?.GetValue(catObj) as MelonPreferences_Category;
    }

    private bool IsCategoryForMod(MelonPreferences_Category category, string modName, string assemblyName = null)
    {
        var namesToMatch = string.IsNullOrEmpty(assemblyName)
            ? new[] { modName }
            : new[] { modName, assemblyName };

        foreach (var name in namesToMatch)
        {
            if (MatchesModName(category.Identifier, name)) return true;
            if (MatchesModName(category.DisplayName, name)) return true;
        }

        EnsureReflectionCacheInitialized();

        try
        {
            var filePath = TryGetCategoryFilePath(category);
            if (!string.IsNullOrEmpty(filePath))
                foreach (var name in namesToMatch)
                    if (MatchesModNameInFilePath(filePath, name))
                        return true;
        }
        catch (Exception ex)
        {
            MelonDebug.Error($"Could not check file path for category matching {modName}: {ex.Message}");
        }

        return false;
    }

    internal static bool MatchesModName(string text, string modName)
    {
        if (string.IsNullOrEmpty(text)) return false;
        return GetModNameVariants(modName).Any(v =>
            text.IndexOf(v, StringComparison.OrdinalIgnoreCase) >= 0);
    }

    private bool MatchesModNameInFilePath(string filePath, string modName)
    {
        if (MatchesModName(Path.GetFileNameWithoutExtension(filePath), modName)) return true;
        var dir = Path.GetDirectoryName(filePath);
        while (!string.IsNullOrEmpty(dir))
        {
            if (MatchesModName(Path.GetFileName(dir), modName)) return true;
            dir = Path.GetDirectoryName(dir);
        }

        return false;
    }

    private void EnsureReflectionCacheInitialized()
    {
        if (_reflectionCacheInitialized) return;
        try
        {
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
        }

        _reflectionCacheInitialized = true;
    }

    private string TryGetCategoryFilePath(MelonPreferences_Category category)
    {
        var fileObj = _cachedFileField?.GetValue(category) ?? _cachedFileProperty?.GetValue(category);
        if (fileObj != null)
        {
            if (_cachedFilePathField == null && _cachedFilePathProperty == null)
            {
                _cachedFilePathField = fileObj.GetType().GetField("FilePath",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                _cachedFilePathProperty = fileObj.GetType().GetProperty("FilePath",
                    BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            }

            var path = _cachedFilePathField?.GetValue(fileObj) as string
                       ?? _cachedFilePathProperty?.GetValue(fileObj) as string;
            if (!string.IsNullOrEmpty(path)) return path;
        }

        return _cachedDirectFilePathField?.GetValue(category) as string
               ?? _cachedDirectFilePathProperty?.GetValue(category) as string;
    }

    internal static IEnumerable<string> GetModNameVariants(string modName)
    {
        if (string.IsNullOrWhiteSpace(modName)) yield break;
        yield return modName;
        yield return modName.Replace(" ", string.Empty);
        yield return modName.Replace(" ", "_");
        yield return modName.Replace(" ", "-");
        var normalized = new string(modName.Where(char.IsLetterOrDigit).ToArray());
        if (!string.IsNullOrEmpty(normalized)) yield return normalized;
    }

    #endregion

    #region Dependencies

    public ModDependencyInfo GetModDependencies(MelonMod mod)
    {
        if (DependencyCache.TryGetValue(mod, out var cached)) return cached;

        var result = new ModDependencyInfo();
        if (mod?.MelonAssembly?.Assembly == null) return result;

        var assemblyLocation = mod.MelonAssembly.Assembly.Location;
        if (string.IsNullOrWhiteSpace(assemblyLocation) || !File.Exists(assemblyLocation))
            return result;

        var loadedAssemblyNames = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a != null).Select(a => a.GetName().Name)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var environmentAssemblies = GetEnvironmentAssemblies();
        var refs = ReadAssemblyReferences(assemblyLocation);
        var required = refs.Where(r => !environmentAssemblies.Contains(r)).ToList();

        result.Required = required;
        result.Missing = required.Where(r => !loadedAssemblyNames.Contains(r)).ToList();
        result.Optional = mod.OptionalDependencies?.AssemblyNames?
            .Where(x => !string.IsNullOrWhiteSpace(x)).ToList() ?? [];

        DependencyCache[mod] = result;
        return result;
    }

    public List<MelonMod> GetDependants(string dllPath)
    {
        var targetName = ReadAssemblyName(dllPath);
        var result = new List<MelonMod>();
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var loadedByName = BuildLoadedByNameMap();
        var refMap = BuildReferenceMap(loadedByName);

        CollectDependants(targetName, loadedByName, refMap, result, visited);
        return result;
    }

    public List<MelonMod> GetCascadingDependants(IEnumerable<string> dllPaths)
    {
        var disabling = dllPaths.Select(ReadAssemblyName).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var result = new List<MelonMod>();
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var loadedByName = BuildLoadedByNameMap();
        var refMap = BuildReferenceMap(loadedByName);

        foreach (var target in disabling)
            CollectDependants(target, loadedByName, refMap, result, visited);

        return result
            .Where(m => !disabling.Contains(ReadAssemblyName(m.MelonAssembly.Assembly.Location)))
            .Distinct().ToList();
    }

    private Dictionary<string, MelonMod> BuildLoadedByNameMap() =>
        MelonMod.RegisteredMelons
            .Where(m => m?.MelonAssembly?.Assembly?.Location != null)
            .ToDictionary(
                m => ReadAssemblyName(m.MelonAssembly.Assembly.Location),
                m => m,
                StringComparer.OrdinalIgnoreCase);

    private Dictionary<string, HashSet<string>> BuildReferenceMap(Dictionary<string, MelonMod> loadedByName)
    {
        var map = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var kv in loadedByName)
        {
            var loc = kv.Value.MelonAssembly.Assembly.Location;
            // Use DependencyCache refs if available to avoid Cecil re-reads
            if (DependencyCache.TryGetValue(kv.Value, out var cached))
            {
                map[kv.Key] = cached.Required.ToHashSet(StringComparer.OrdinalIgnoreCase);
            }
            else
            {
                map[kv.Key] = ReadAssemblyReferences(loc);
            }
        }

        return map;
    }

    private static void CollectDependants(
        string targetName,
        Dictionary<string, MelonMod> loadedByName,
        Dictionary<string, HashSet<string>> refMap,
        List<MelonMod> result,
        HashSet<string> visited)
    {
        if (!visited.Add(targetName)) return;
        foreach (var kv in refMap)
        {
            if (!kv.Value.Contains(targetName)) continue;
            if (!loadedByName.TryGetValue(kv.Key, out var dependant)) continue;
            if (result.Contains(dependant)) continue;
            result.Add(dependant);
            CollectDependants(kv.Key, loadedByName, refMap, result, visited);
        }
    }

    private static HashSet<string> ReadAssemblyReferences(string filePath)
    {
        try
        {
            var def = AssemblyDefinition.ReadAssembly(filePath, new ReaderParameters { ReadWrite = false });
            return def.MainModule.AssemblyReferences
                .Select(r => r.Name).Where(n => !string.IsNullOrWhiteSpace(n))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }
        catch
        {
            return [];
        }
    }

    private static string ReadAssemblyName(string filePath)
    {
        try
        {
            var def = AssemblyDefinition.ReadAssembly(filePath, new ReaderParameters { ReadWrite = false });
            return def.Name.Name;
        }
        catch
        {
            return Path.GetFileNameWithoutExtension(filePath.Replace(ModToggleManager.InactiveExtension, ""));
        }
    }

    #endregion

    #region Environment / Backend

    private static readonly string[] NonEnvironmentFolders =
    [
        MelonEnvironment.UserLibsDirectory,
        MelonEnvironment.PluginsDirectory,
        MelonEnvironment.ModsDirectory
    ];

    private static bool IsEnvironmentAssembly(Assembly asm, HashSet<Assembly> modAssemblies)
    {
        if (asm == null || modAssemblies.Contains(asm)) return false;
        string location;
        try
        {
            location = asm.Location;
        }
        catch (NotSupportedException)
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(location)) return true;
        return !NonEnvironmentFolders.Any(f => location.StartsWith(f, StringComparison.OrdinalIgnoreCase));
    }

    private static HashSet<string> GetEnvironmentAssemblies()
    {
        var modAssemblies = MelonMod.RegisteredMelons
            .Where(m => m?.MelonAssembly?.Assembly != null)
            .Select(m => m.MelonAssembly.Assembly).ToHashSet();

        return AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => IsEnvironmentAssembly(a, modAssemblies))
            .Select(a => a.GetName().Name).Where(n => !string.IsNullOrWhiteSpace(n))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private Backend DetermineBackend(MelonMod mod)
    {
        if (BackendCache.TryGetValue(mod, out var cached)) return cached;

        var assemblyDef = AssemblyDefinition.ReadAssembly(mod.MelonAssembly.Assembly.Location);
        var hasIL2CPP = false;
        var hasMono = false;
        var hasS1API = false;

        foreach (var module in assemblyDef.Modules)
        {
            foreach (var type in module.Types)
            {
                if (!hasIL2CPP && type.FullName.StartsWith("Il2Cpp")) hasIL2CPP = true;
                if (!hasMono && type.FullName.Contains("ScheduleOne")) hasMono = true;
            }

            foreach (var typeRef in module.GetTypeReferences())
            {
                if (!hasS1API && typeRef.Namespace.StartsWith("S1API")) hasS1API = true;
                if (!hasIL2CPP && typeRef.Namespace.StartsWith("Il2Cpp")) hasIL2CPP = true;
                if (!hasMono && typeRef.Namespace.Contains("ScheduleOne")) hasMono = true;
            }

            if (hasIL2CPP || hasMono) break;
        }

        var result = hasIL2CPP ? Backend.IL2CPP
            : hasMono ? Backend.Mono
            : hasS1API ? Backend.S1API
            : Backend.Unknown;

        BackendCache[mod] = result;
        return result;
    }

    public bool isCompatible(MelonMod mod, ref string backend)
    {
        var modBackend = DetermineBackend(mod);
        backend = modBackend.ToString();
        if (modBackend == Backend.Unknown) return false;
        var isIL2CPP = MelonUtils.IsGameIl2Cpp();
        return modBackend == Backend.S1API
               || (modBackend == Backend.IL2CPP && isIL2CPP)
               || (modBackend == Backend.Mono && !isIL2CPP);
    }

    #endregion

    #region Docs

    public string GetChangelog(MelonMod mod, out string filepath)
    {
        filepath = null;
        if (mod?.MelonAssembly?.Assembly == null || string.IsNullOrWhiteSpace(mod.MelonAssembly.Assembly.Location))
            return null;
        var dir = Directory.GetParent(mod.MelonAssembly.Assembly.Location);
        if (dir == null || dir.Name == MelonEnvironment.ModsDirectory || !dir.Exists) return null;
        return FindDocFile(dir.FullName, "CHANGELOG", out filepath);
    }

    public string GetReadme(MelonMod mod, out string filepath)
    {
        filepath = null;
        if (mod?.MelonAssembly?.Assembly == null || string.IsNullOrWhiteSpace(mod.MelonAssembly.Assembly.Location))
            return null;
        var dir = Directory.GetParent(mod.MelonAssembly.Assembly.Location);
        if (dir == null || dir.Name == MelonEnvironment.ModsDirectory || !dir.Exists) return null;
        return FindDocFile(dir.FullName, "README", out filepath);
    }

    private static string FindDocFile(string dirPath, string fileName, out string filepath)
    {
        var extensions = new[] { ".txt", ".md", ".rst" };
        var file = Directory.EnumerateFiles(dirPath)
            .FirstOrDefault(f => extensions.Contains(Path.GetExtension(f).ToLower())
                                 && Path.GetFileNameWithoutExtension(f)
                                     .Equals(fileName, StringComparison.OrdinalIgnoreCase));
        filepath = file;
        return file == null ? null : File.ReadAllText(file);
    }

    #endregion
}

internal enum Backend
{
    Unknown,
    IL2CPP,
    Mono,
    S1API
}

public class ModDependencyInfo
{
    public List<string> Required { get; set; } = [];
    public List<string> Missing { get; set; } = [];
    public List<string> Optional { get; set; } = [];
}