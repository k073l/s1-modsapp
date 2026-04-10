using MelonLoader;

namespace ModsApp.UI.Search;

public struct SearchResult
{
    public MelonMod Mod;
    public MelonPreferences_Category Category;
    public MelonPreferences_Entry Entry;
    public float Score;

    public SearchResult(MelonMod mod, MelonPreferences_Category category, MelonPreferences_Entry entry,
        float score = 0f)
    {
        Mod = mod;
        Category = category;
        Entry = entry;
        Score = score;
    }
}