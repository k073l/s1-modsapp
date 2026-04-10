using ModsApp.Helpers.Registries;
using ModsApp.Managers;

namespace ModsApp.UI.Search;

public class SearchManager
{
    internal const int MaxSearchResults = 50;
    public static float SimilarityThreshold => SettingsRegistry.SearchSimilarityThreshold?.Value ?? 0.6f;
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
        var queryLower = query.ToLowerInvariant();

        foreach (var mod in _modManager.GetAllMods())
        {
            var categories = _modManager.GetPreferencesForMod(mod).ToList();
            foreach (var category in categories)
            {
                var categoryKey = $"{mod.Info.Name}.{category.Identifier}";

                var categoryName = category.DisplayName ?? category.Identifier;
                var categoryScore = GetWordMatchScore(categoryName, queryLower);
                var categoryIdScore = GetWordMatchScore(category.Identifier, queryLower);
                var bestCategoryScore = Math.Max(categoryScore, categoryIdScore);

                if (bestCategoryScore >= SimilarityThreshold)
                {
                    if (!addedCategories.Contains(categoryKey))
                    {
                        results.Add(new SearchResult(mod, category, null, bestCategoryScore));
                        addedCategories.Add(categoryKey);
                    }
                }

                if (category.Entries == null) continue;
                foreach (var entry in category.Entries)
                {
                    var entryKey = $"{categoryKey}.{entry.Identifier}";
                    var entryName = entry.DisplayName ?? entry.Identifier;
                    var entryDesc = entry.Description ?? "";

                    var nameScore = GetWordMatchScore(entryName, queryLower);
                    var descScore = GetWordMatchScore(entryDesc, queryLower);
                    var bestEntryScore = Math.Max(nameScore, descScore);

                    if (bestEntryScore >= SimilarityThreshold)
                    {
                        if (!addedCategories.Contains(categoryKey))
                        {
                            results.Add(new SearchResult(mod, category, null, bestCategoryScore));
                            addedCategories.Add(categoryKey);
                        }

                        if (!addedEntries.Contains(entryKey))
                        {
                            results.Add(new SearchResult(mod, category, entry, bestEntryScore));
                            addedEntries.Add(entryKey);
                        }
                    }
                }
            }

            if (results.Count >= MaxSearchResults)
                break;
        }

        return results
            .OrderByDescending(r => r.Score)
            .Take(MaxSearchResults)
            .ToList();
    }

    private static float GetWordMatchScore(string text, string query)
    {
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(query))
            return 0f;

        var exactMatch = text.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
        if (exactMatch)
            return 1f;

        if (SimilarityThreshold >= 1f)
            return 0f;

        var words = text.Split(' ', '-', '_');
        var bestScore = 0f;

        foreach (var word in words)
        {
            var wordLower = word.ToLowerInvariant();
            var score = Levenshtein.Similarity(wordLower, query);
            if (score > bestScore)
                bestScore = score;
        }

        return bestScore >= SimilarityThreshold ? bestScore : 0f;
    }
}