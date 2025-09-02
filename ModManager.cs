using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MelonLoader;

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
    }
}