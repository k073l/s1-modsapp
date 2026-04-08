using MelonLoader;

namespace ModsApp.UI.Search;

public struct SearchResult
{
    public MelonMod Mod;
    public MelonPreferences_Category Category;
    public MelonPreferences_Entry Entry;

    public SearchResult(MelonMod mod, MelonPreferences_Category category, MelonPreferences_Entry entry)
    {
        Mod = mod;
        Category = category;
        Entry = entry;
    }
}