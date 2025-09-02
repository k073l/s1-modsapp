using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MelonLoader;
using Mono.Cecil;

namespace ModsApp
{
    public class ModManager
    {
        private readonly Dictionary<string, MelonMod> _mods = new();
        private readonly MelonLogger.Instance _logger;

        public ModManager(MelonLogger.Instance logger)
        {
            _logger = logger;
            LoadMods();
        }

        public int ModCount => _mods.Count;
        public IEnumerable<MelonMod> GetAllMods() => _mods.Values.OrderBy(m => m.Info.Name, StringComparer.OrdinalIgnoreCase);
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
            if (type?.IsGenericType == true && type.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
            {
                var valueProp = type.GetProperty("Value");
                return valueProp?.GetValue(catObj) as MelonPreferences_Category;
            }

            return null;
        }

        private bool IsCategoryForMod(MelonPreferences_Category category, string modName)
        {
            return (!string.IsNullOrEmpty(category.Identifier) && 
                    category.Identifier.IndexOf(modName, StringComparison.OrdinalIgnoreCase) >= 0) ||
                   (!string.IsNullOrEmpty(category.DisplayName) && 
                    category.DisplayName.IndexOf(modName, StringComparison.OrdinalIgnoreCase) >= 0);
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

                // Check referenced types for S1API
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
}