using System.Linq;
using UnityEngine;
using S1API.UI;
using MelonLoader;
using UnityEngine.UI;

namespace ModsApp
{
    public class ModDetailsPanel
    {
        private readonly Transform _parent;
        private readonly ModManager _modManager;
        private readonly UITheme _theme;
        private readonly MelonLogger.Instance _logger;
        
        private RectTransform _detailsContent;

        public ModDetailsPanel(Transform parent, ModManager modManager, UITheme theme, MelonLogger.Instance logger)
        {
            _parent = parent;
            _modManager = modManager;
            _theme = theme;
            _logger = logger;
        }

        public void Initialize()
        {
            var rightPanel = UIFactory.Panel("ModDetailsPanel", _parent, _theme.BgCard,
                new Vector2(0.36f, 0.05f), new Vector2(0.98f, 0.82f));
            UIHelper.ForceRectToAnchors(rightPanel.GetComponent<RectTransform>(),
                new Vector2(0.36f, 0.05f), new Vector2(0.98f, 0.82f),
                Vector2.zero, Vector2.zero);
            
            UIHelper.AddBorderEffect(rightPanel, _theme.AccentPrimary);
            
            _detailsContent = UIFactory.ScrollableVerticalList("ModDetailsContent", rightPanel.transform, out _);
            UIHelper.ForceRectToAnchors(_detailsContent, Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero, new Vector2(0.5f, 1f));
            UIHelper.SetupLayoutGroup(_detailsContent.gameObject, 6, true, new RectOffset(12, 12, 12, 12));
            
            var layout = _detailsContent.GetComponent<VerticalLayoutGroup>();
            if (layout != null)
            {
                layout.spacing = 6;
                layout.padding = new RectOffset(12,12,12,12);
                layout.childControlWidth = true;
                layout.childForceExpandHeight = false;
            }
            // LayoutRebuilder.ForceRebuildLayoutImmediate(_detailsContent);
            UIFactory.FitContentHeight(_detailsContent);
            
            UIHelper.DumpRect("ModDetailsPanel", rightPanel.GetComponent<RectTransform>());
            UIHelper.DumpRect("ModDetailsContent", _detailsContent);
        }

        public void ShowWelcome()
        {
            if (_detailsContent == null) return;

            UIFactory.ClearChildren(_detailsContent);

            var welcomeCard = CreateInfoCard("WelcomeCard");
            CreateWelcomeContent(welcomeCard);

            var statsCard = CreateInfoCard("StatsCard");
            CreateStatsContent(statsCard);

            UIHelper.RefreshLayout(_detailsContent);
        }

        public void ShowModDetails(MelonMod mod)
        {
            if (_detailsContent == null) return;

            UIFactory.ClearChildren(_detailsContent);

            var headerCard = CreateInfoCard($"{UIHelper.SanitizeName(mod.Info.Name)}_HeaderCard");
            CreateModHeader(mod, headerCard);

            var prefsCard = CreateInfoCard($"{UIHelper.SanitizeName(mod.Info.Name)}_PrefsCard");
            CreatePreferencesSection(mod, prefsCard);

            UIHelper.RefreshLayout(_detailsContent);
        }

        private GameObject CreateInfoCard(string name)
        {
            var card = UIFactory.Panel(name, _detailsContent, _theme.BgSecondary);
            UIHelper.SetupLayoutGroup(card, 4, true, new RectOffset(12, 12, 4, 4)); // changed from 8, 8
            
            var layoutElement = card.GetOrAddComponent<UnityEngine.UI.LayoutElement>();
            layoutElement.preferredHeight = -1;
            layoutElement.minHeight = 0;
            layoutElement.flexibleHeight = 0;
            
            LayoutRebuilder.ForceRebuildLayoutImmediate(card.GetComponent<RectTransform>());
            MelonLogger.Msg($"Created card '{name}' height: {card.GetComponent<RectTransform>().rect.height}");
            
            return card;
        }

        private void CreateWelcomeContent(GameObject card)
        {
            var title = UIFactory.Text("WelcomeTitle", "Mods Manager", card.transform, 20, TextAnchor.UpperLeft, FontStyle.Bold);
            title.color = _theme.TextPrimary;
            
            var desc = UIFactory.Text("WelcomeDesc", "Select a mod from the list to view its details and preferences.", card.transform, 14);
            desc.color = _theme.TextSecondary;
        }

        private void CreateStatsContent(GameObject card)
        {
            var title = UIFactory.Text("StatsTitle", "Statistics", card.transform, 16, TextAnchor.UpperLeft, FontStyle.Bold);
            title.color = _theme.TextPrimary;
            
            var count = UIFactory.Text("ModCount", $"Total Mods: {_modManager.ModCount}", card.transform, 14);
            count.color = _theme.TextSecondary;
        }

        private void CreateModHeader(MelonMod mod, GameObject card)
        {
            var title = UIFactory.Text("ModTitle", mod.Info.Name, card.transform, 18, TextAnchor.UpperLeft, FontStyle.Bold);
            title.color = _theme.TextPrimary;
            
            var author = UIFactory.Text("ModAuthor", $"{mod.Info.Author}", card.transform, 12);
            author.color = _theme.TextSecondary;
            
            var version = UIFactory.Text("ModVersion", $"Version {mod.Info.Version}", card.transform, 12);
            version.color = _theme.TextSecondary;
        }

        private void CreatePreferencesSection(MelonMod mod, GameObject card)
        {
            var header = UIFactory.Text("PrefsHeader", "Preferences", card.transform, 16, TextAnchor.UpperLeft, FontStyle.Bold);
            header.color = _theme.TextPrimary;

            var categories = _modManager.GetPreferencesForMod(mod).ToList();

            if (categories.Count == 0)
            {
                var noPrefs = UIFactory.Text("NoPrefs", "No preferences available for this mod.", card.transform, 12, TextAnchor.UpperLeft, FontStyle.Italic);
                noPrefs.color = _theme.TextSecondary;
                return;
            }

            foreach (var category in categories)
            {
                CreateCategorySection(category, card);
            }
        }

        private void CreateCategorySection(MelonPreferences_Category category, GameObject parent)
        {
            var categoryPanel = UIFactory.Panel($"{UIHelper.SanitizeName(category.Identifier)}_Category", parent.transform, 
                new Color(_theme.BgCard.r - 0.02f, _theme.BgCard.g - 0.02f, _theme.BgCard.b - 0.02f, 1f));
            
            UIHelper.SetupLayoutGroup(categoryPanel, 3, true, new RectOffset(8, 8, 6, 6));

            string categoryTitle = string.IsNullOrWhiteSpace(category.DisplayName) ? category.Identifier : category.DisplayName;
            var title = UIFactory.Text($"{UIHelper.SanitizeName(category.Identifier)}_Title", $"{categoryTitle}", categoryPanel.transform, 14, TextAnchor.UpperLeft, FontStyle.Bold);
            title.color = new Color(_theme.AccentPrimary.r, _theme.AccentPrimary.g, _theme.AccentPrimary.b, 0.9f);

            if (category.Entries?.Count > 0)
            {
                foreach (var entry in category.Entries)
                {
                    CreatePreferenceEntry(entry, categoryPanel, category.Identifier);
                }
            }
        }

        private void CreatePreferenceEntry(MelonPreferences_Entry entry, GameObject parent, string categoryId)
        {
            string entryName = entry.DisplayName ?? entry.Identifier ?? "Entry";
            string valueStr = entry.BoxedValue?.ToString() ?? "null";
            
            var entryText = UIFactory.Text($"{UIHelper.SanitizeName(categoryId)}_{UIHelper.SanitizeName(entryName)}", 
                $"  • {entryName}: {valueStr}", parent.transform, 11);
            entryText.color = _theme.TextSecondary;
        }
    }
}
