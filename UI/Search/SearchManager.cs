using ModsApp.Managers;

namespace ModsApp.UI.Search;

public class SearchManager
{
    internal const int MaxSearchResults = 50;
    private readonly ModManager _modManager;

    public SearchManager(ModManager modManager)
    {
        _modManager = modManager;
    }

    public IReadOnlyList<SearchResult> Search(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return new List<SearchResult>();

        var results = new List<SearchResult>();
        var addedCategories = new HashSet<string>();
        var addedEntries = new HashSet<string>();

        foreach (var mod in _modManager.GetAllMods())
        {
            var categories = _modManager.GetPreferencesForMod(mod).ToList();
            foreach (var category in categories)
            {
                var categoryKey = $"{mod.Info.Name}.{category.Identifier}";

                var categoryName = category.DisplayName ?? category.Identifier;
                var categoryMatches = categoryName.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                      category.Identifier.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;

                if (categoryMatches)
                {
                    if (!addedCategories.Contains(categoryKey))
                    {
                        results.Add(new SearchResult(mod, category, null));
                        addedCategories.Add(categoryKey);
                    }
                }

                if (category.Entries == null) continue;
                foreach (var entry in category.Entries)
                {
                    var entryKey = $"{categoryKey}.{entry.Identifier}";
                    var entryName = entry.DisplayName ?? entry.Identifier;
                    var entryDesc = entry.Description ?? "";

                    var matchesName = entryName.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
                    var matchesDesc = entryDesc.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;

                    if (matchesName || matchesDesc)
                    {
                        if (!addedCategories.Contains(categoryKey))
                        {
                            results.Add(new SearchResult(mod, category, null));
                            addedCategories.Add(categoryKey);
                        }

                        if (!addedEntries.Contains(entryKey))
                        {
                            results.Add(new SearchResult(mod, category, entry));
                            addedEntries.Add(entryKey);
                        }
                    }
                }
            }

            if (results.Count >= MaxSearchResults)
                break;
        }

        return results;
    }
}