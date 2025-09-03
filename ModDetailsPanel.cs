using System.Linq;
using UnityEngine;
using S1API.UI;
using MelonLoader;
using UnityEngine.UI;
using System.Collections.Generic;

namespace ModsApp
{
    public class ModDetailsPanel
    {
        private readonly Transform _parent;
        private readonly ModManager _modManager;
        private readonly UITheme _theme;
        private readonly MelonLogger.Instance _logger;

        private RectTransform _detailsContent;
        private Dictionary<string, object> _modifiedPreferences = new Dictionary<string, object>();

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
                layout.padding = new RectOffset(12, 12, 12, 12);
                layout.childControlWidth = true;
                layout.childForceExpandHeight = false;
            }

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
            _modifiedPreferences.Clear();

            var headerCard = CreateInfoCard($"{UIHelper.SanitizeName(mod.Info.Name)}_HeaderCard");
            CreateModHeader(mod, headerCard);

            var prefsCard = CreateInfoCard($"{UIHelper.SanitizeName(mod.Info.Name)}_PrefsCard");
            CreatePreferencesSection(mod, prefsCard);

            // Add apply/reset buttons if there are preferences
            var categories = _modManager.GetPreferencesForMod(mod).ToList();
            if (categories.Count > 0)
            {
                var actionsCard = CreateInfoCard($"{UIHelper.SanitizeName(mod.Info.Name)}_ActionsCard");
                CreateActionButtons(mod, actionsCard);
            }

            UIHelper.RefreshLayout(_detailsContent);
        }

        private GameObject CreateInfoCard(string name)
        {
            var card = UIFactory.Panel(name, _detailsContent, _theme.BgSecondary);

            // Setup VerticalLayoutGroup
            var vlg = card.GetOrAddComponent<VerticalLayoutGroup>();
            vlg.spacing = 8;
            vlg.padding = new RectOffset(12, 12, 8, 8);
            vlg.childControlWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childAlignment = TextAnchor.UpperLeft;

            // Add ContentSizeFitter to properly size the card
            var csf = card.GetOrAddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            // Add LayoutElement for parent layout
            var layoutElement = card.GetOrAddComponent<LayoutElement>();
            layoutElement.preferredHeight = -1;
            layoutElement.minHeight = 0;
            layoutElement.flexibleHeight = 0;
            layoutElement.flexibleWidth = 1;

            return card;
        }

        private void CreateWelcomeContent(GameObject card)
        {
            var title = UIFactory.Text("WelcomeTitle", "Mods Manager", card.transform, 20, TextAnchor.UpperLeft,
                FontStyle.Bold);
            title.color = _theme.TextPrimary;

            var desc = UIFactory.Text("WelcomeDesc", "Select a mod from the list to view its details and preferences.",
                card.transform, 14);
            desc.color = _theme.TextSecondary;
        }

        private void CreateStatsContent(GameObject card)
        {
            var title = UIFactory.Text("StatsTitle", "Statistics", card.transform, 16, TextAnchor.UpperLeft,
                FontStyle.Bold);
            title.color = _theme.TextPrimary;

            var count = UIFactory.Text("ModCount", $"Total Mods: {_modManager.ModCount}", card.transform, 14);
            count.color = _theme.TextSecondary;
        }

        private void CreateModHeader(MelonMod mod, GameObject card)
        {
            // Container for title + badge
            var headerContainer = UIFactory.Panel("HeaderContainer", card.transform, Color.clear);

            // Add LayoutElement to prevent expansion issues
            var headerLayout = headerContainer.GetOrAddComponent<LayoutElement>();
            headerLayout.preferredHeight = 28;
            headerLayout.flexibleHeight = 0;

            var hLayout = headerContainer.AddComponent<HorizontalLayoutGroup>();
            hLayout.spacing = 8;
            hLayout.childAlignment = TextAnchor.MiddleLeft;
            hLayout.childForceExpandWidth = false;
            hLayout.childForceExpandHeight = false;
            hLayout.childControlWidth = true;
            hLayout.childControlHeight = true;

            // Title
            var title = UIFactory.Text("ModTitle", mod.Info.Name, headerContainer.transform, 20, TextAnchor.MiddleLeft,
                FontStyle.Bold);
            title.color = _theme.TextPrimary;

            // Backend badge
            string backendName = "";
            bool compatible = _modManager.isCompatible(mod, ref backendName);

            var badge = UIFactory.Panel("BackendBadge", headerContainer.transform,
                compatible ? _theme.SuccessColor : _theme.WarningColor);

            var badgeLayout = badge.GetOrAddComponent<LayoutElement>();
            badgeLayout.minWidth = 50;
            badgeLayout.preferredWidth = 60;
            badgeLayout.preferredHeight = 22;
            badgeLayout.flexibleWidth = 0;
            badgeLayout.flexibleHeight = 0;

            // Padding for text inside badge
            var badgePadding = badge.AddComponent<HorizontalLayoutGroup>();
            badgePadding.childAlignment = TextAnchor.MiddleCenter;
            badgePadding.childForceExpandWidth = true;
            badgePadding.childForceExpandHeight = true;
            badgePadding.padding = new RectOffset(6, 6, 2, 2);

            var badgeText = UIFactory.Text("BackendText", backendName, badge.transform, 12, TextAnchor.MiddleCenter,
                FontStyle.Bold);
            badgeText.color = Color.white;

            var author = UIFactory.Text("ModAuthor", $"by: {mod.Info.Author}", card.transform, 14);
            author.color = _theme.TextSecondary;

            var version = UIFactory.Text("ModVersion", $"v. {mod.Info.Version}", card.transform, 14);
            version.color = _theme.TextSecondary;
        }

        private void CreatePreferencesSection(MelonMod mod, GameObject card)
        {
            var header = UIFactory.Text("PrefsHeader", "Preferences", card.transform, 16, TextAnchor.UpperLeft,
                FontStyle.Bold);
            header.color = _theme.TextPrimary;

            var categories = _modManager.GetPreferencesForMod(mod).ToList();

            if (categories.Count == 0)
            {
                var noPrefs = UIFactory.Text("NoPrefs", "No preferences available for this mod.", card.transform, 12,
                    TextAnchor.UpperLeft, FontStyle.Italic);
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
            var categoryPanel = UIFactory.Panel($"{UIHelper.SanitizeName(category.Identifier)}_Category",
                parent.transform,
                new Color(_theme.BgCard.r - 0.02f, _theme.BgCard.g - 0.02f, _theme.BgCard.b - 0.02f, 1f));

            // Setup layout for category panel
            var vlg = categoryPanel.GetOrAddComponent<VerticalLayoutGroup>();
            vlg.spacing = 4;
            vlg.padding = new RectOffset(8, 8, 6, 6);
            vlg.childControlWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childAlignment = TextAnchor.UpperLeft;

            // Add ContentSizeFitter for proper sizing
            var csf = categoryPanel.GetOrAddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            // Add LayoutElement for parent layout
            var layoutElement = categoryPanel.GetOrAddComponent<LayoutElement>();
            layoutElement.preferredHeight = -1;
            layoutElement.flexibleHeight = 0;
            layoutElement.flexibleWidth = 1;

            string categoryTitle = string.IsNullOrWhiteSpace(category.DisplayName)
                ? category.Identifier
                : category.DisplayName;
            var title = UIFactory.Text($"{UIHelper.SanitizeName(category.Identifier)}_Title", $"{categoryTitle}",
                categoryPanel.transform, 14, TextAnchor.UpperLeft, FontStyle.Bold);
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
            string entryKey = $"{categoryId}.{entry.Identifier}";

            var entryContainer = UIFactory.Panel(
                $"{UIHelper.SanitizeName(categoryId)}_{UIHelper.SanitizeName(entryName)}_Container",
                parent.transform, Color.clear);

            var entryLayout = entryContainer.GetOrAddComponent<LayoutElement>();
            entryLayout.preferredHeight = 30;
            entryLayout.flexibleHeight = 0;
            entryLayout.flexibleWidth = 1;

            var hLayout = entryContainer.AddComponent<HorizontalLayoutGroup>();
            hLayout.spacing = 8;
            hLayout.childAlignment = TextAnchor.MiddleLeft;
            hLayout.childForceExpandWidth = false; // ❗ don’t stretch
            hLayout.childForceExpandHeight = false;
            hLayout.childControlHeight = true;

            // Label
            var label = UIFactory.Text(
                $"{UIHelper.SanitizeName(categoryId)}_{UIHelper.SanitizeName(entryName)}_Label",
                $"{entryName} ({entry.Identifier})", entryContainer.transform, 14, TextAnchor.MiddleLeft);
            label.color = _theme.TextSecondary;

            var labelLayout = label.gameObject.GetOrAddComponent<LayoutElement>();
            labelLayout.minWidth = 60;
            labelLayout.flexibleWidth = 0; // auto-size to content

            // Input
            CreatePreferenceInput(entry, entryContainer, entryKey);
        }

        private void CreatePreferenceInput(MelonPreferences_Entry entry, GameObject parent, string entryKey)
        {
            var currentValue = _modifiedPreferences.ContainsKey(entryKey)
                ? _modifiedPreferences[entryKey]
                : entry.BoxedValue;

            if (entry.BoxedValue is bool boolValue)
            {
                CreateBooleanInput(entry, parent, entryKey,
                    _modifiedPreferences.ContainsKey(entryKey) ? (bool)_modifiedPreferences[entryKey] : boolValue);
            }
            else if (entry.BoxedValue is int intValue)
            {
                CreateIntegerInput(entry, parent, entryKey,
                    _modifiedPreferences.ContainsKey(entryKey) ? (int)_modifiedPreferences[entryKey] : intValue);
            }
            else if (entry.BoxedValue is float floatValue)
            {
                CreateFloatInput(entry, parent, entryKey,
                    _modifiedPreferences.ContainsKey(entryKey) ? (float)_modifiedPreferences[entryKey] : floatValue);
            }
            else if (entry.BoxedValue is string stringValue)
            {
                CreateStringInput(entry, parent, entryKey,
                    _modifiedPreferences.ContainsKey(entryKey) ? (string)_modifiedPreferences[entryKey] : stringValue);
            }
            else
            {
                // Fallback for unknown types - show as read-only text
                var valueText = UIFactory.Text($"{entryKey}_Value", currentValue?.ToString() ?? "null",
                    parent.transform, 11);
                valueText.color = _theme.TextPrimary;
                valueText.fontStyle = FontStyle.Italic;
            }
        }

        private InputField CreateInputField(GameObject parent, string name, string initialValue,
            InputField.ContentType contentType, int minWidth = 80)
        {
            MelonLogger.Msg($"[UI] Creating InputField: {name}, initial='{initialValue}', type={contentType}");

            var inputObj = new GameObject(name);
            inputObj.transform.SetParent(parent.transform, false);

            // Background
            var bg = inputObj.AddComponent<Image>();
            bg.color = new Color(1f, 1f, 1f, 0.9f);

            var inputField = inputObj.AddComponent<InputField>();
            inputField.contentType = contentType;
            inputField.transition = Selectable.Transition.ColorTint;
            
            var font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (font == null)
                MelonLogger.Msg("[UI][WARN] Arial font missing!");

            // Text child
            var textGO = new GameObject("Text");
            textGO.transform.SetParent(inputObj.transform, false);
            textGO.AddComponent<RectTransform>();
            var text = textGO.AddComponent<Text>();
            text.font = font;
            text.text = initialValue ?? "";
            text.fontSize = 14;
            text.alignment = TextAnchor.MiddleLeft;
            text.fontStyle = FontStyle.Normal;
            text.color = Color.black;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;

            var textRT = text.GetComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = new Vector2(6, 2);
            textRT.offsetMax = new Vector2(-6, -2);

            inputField.textComponent = text;
            inputField.text = initialValue ?? "";
            MelonLogger.Msg($"[UI] Assigned textComponent with font={font?.name}");

            // Placeholder
            var placeholderGO = new GameObject("Placeholder");
            placeholderGO.transform.SetParent(inputObj.transform, false);
            placeholderGO.AddComponent<RectTransform>();
            var placeholder = placeholderGO.AddComponent<Text>();
            placeholder.font = font;
            placeholder.fontSize = 14;
            placeholder.alignment = TextAnchor.MiddleLeft;
            placeholder.color = new Color(0.5f, 0.5f, 0.5f, 0.75f);
            placeholder.text = "Enter value...";

            var phRT = placeholder.GetComponent<RectTransform>();
            phRT.anchorMin = Vector2.zero;
            phRT.anchorMax = Vector2.one;
            phRT.offsetMin = new Vector2(6, 2);
            phRT.offsetMax = new Vector2(-6, -2);

            inputField.placeholder = placeholder;
            MelonLogger.Msg($"[UI] Assigned placeholder with font={font?.name}");

            // Caret setup
            inputField.caretBlinkRate = 0.6f;
            inputField.customCaretColor = true;
            inputField.caretColor = Color.black;

            // Layout
            var layout = inputObj.GetOrAddComponent<LayoutElement>();
            layout.minWidth = minWidth;
            layout.preferredWidth = Mathf.Max(minWidth, (initialValue?.Length ?? 1) * 12);

            return inputField;
        }

        private void CreateBooleanInput(MelonPreferences_Entry entry, GameObject parent, string entryKey,
            bool currentValue)
        {
            var toggleObj = new GameObject($"{entryKey}_Toggle");
            toggleObj.transform.SetParent(parent.transform, false);

            var bg = toggleObj.AddComponent<Image>();
            bg.color = Color.white;

            var toggle = toggleObj.AddComponent<Toggle>();
            toggle.targetGraphic = bg;

            // Square background
            var rt = toggleObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(20, 20);

            // Checkmark child
            var checkmarkGO = new GameObject("Checkmark");
            checkmarkGO.transform.SetParent(toggleObj.transform, false);
            var checkmarkImg = checkmarkGO.AddComponent<Image>();
            checkmarkImg.color = Color.black;

            var crt = checkmarkGO.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0.2f, 0.2f);
            crt.anchorMax = new Vector2(0.8f, 0.8f);
            crt.offsetMin = crt.offsetMax = Vector2.zero;

            toggle.graphic = checkmarkImg;
            toggle.isOn = currentValue;

            toggle.onValueChanged.AddListener(value =>
            {
                _modifiedPreferences[entryKey] = value;
                _logger.Msg($"Modified preference {entryKey}: {value}");
            });

            var layout = toggleObj.GetOrAddComponent<LayoutElement>();
            layout.minWidth = 20;
            layout.minHeight = 20;
            layout.preferredWidth = 20;
            layout.preferredHeight = 20;
        }

        private void CreateIntegerInput(MelonPreferences_Entry entry, GameObject parent, string entryKey,
            int currentValue)
        {
            var input = CreateInputField(parent, $"{entryKey}_Input", currentValue.ToString(),
                InputField.ContentType.IntegerNumber, 50);

            input.onEndEdit.AddListener(value =>
            {
                if (int.TryParse(value, out int intValue))
                {
                    _modifiedPreferences[entryKey] = intValue;
                    _logger.Msg($"Modified preference {entryKey}: {intValue}");
                }
                else input.text = currentValue.ToString();
            });
        }

        private void CreateFloatInput(MelonPreferences_Entry entry, GameObject parent, string entryKey,
            float currentValue)
        {
            var input = CreateInputField(parent, $"{entryKey}_Input", currentValue.ToString("0.##"),
                InputField.ContentType.DecimalNumber, 60);

            input.onEndEdit.AddListener(value =>
            {
                if (float.TryParse(value, out float floatValue))
                {
                    _modifiedPreferences[entryKey] = floatValue;
                    _logger.Msg($"Modified preference {entryKey}: {floatValue}");
                }
                else input.text = currentValue.ToString("0.##");
            });
        }

        private void CreateStringInput(MelonPreferences_Entry entry, GameObject parent, string entryKey,
            string currentValue)
        {
            var input = CreateInputField(parent, $"{entryKey}_Input", currentValue ?? "",
                InputField.ContentType.Standard, 100);

            input.onEndEdit.AddListener(value =>
            {
                _modifiedPreferences[entryKey] = value;
                _logger.Msg($"Modified preference {entryKey}: {value}");
            });
        }


        private void CreateActionButtons(MelonMod mod, GameObject card)
        {
            var buttonContainer = UIFactory.Panel("ButtonContainer", card.transform, Color.clear);


            var buttonLayout = buttonContainer.GetOrAddComponent<LayoutElement>();
            buttonLayout.preferredHeight = 35;
            buttonLayout.flexibleHeight = 0;


            var hLayout = buttonContainer.AddComponent<HorizontalLayoutGroup>();
            hLayout.spacing = 8;
            hLayout.childAlignment = TextAnchor.MiddleCenter;
            hLayout.childForceExpandWidth = false;
            hLayout.childForceExpandHeight = false;
            hLayout.childControlWidth = true;
            hLayout.childControlHeight = true;


            var (applyObj, applyButton, _) = UIFactory.RoundedButtonWithLabel(
                "ApplyButton", "Apply Changes", buttonContainer.transform,
                _theme.AccentPrimary, 120, 30, 14, Color.white);


            applyButton.onClick.AddListener(() => ApplyPreferenceChanges(mod));


            var (resetObj, resetButton, _) = UIFactory.RoundedButtonWithLabel(
                "ResetButton", "Reset", buttonContainer.transform,
                _theme.WarningColor, 80, 30, 14, Color.white);


            resetButton.onClick.AddListener(() => ResetPreferenceChanges(mod));


            UpdateButtonStates(applyButton, resetButton);
        }

        private void ApplyPreferenceChanges(MelonMod mod)
        {
            foreach (var kvp in _modifiedPreferences)
            {
                var parts = kvp.Key.Split('.');
                if (parts.Length == 2)
                {
                    var categoryId = parts[0];
                    var entryId = parts[1];

                    // Find the preference entry and update it
                    var categories = _modManager.GetPreferencesForMod(mod).ToList();
                    var category = categories.FirstOrDefault(c => c.Identifier == categoryId);
                    var entry = category?.Entries?.FirstOrDefault(e => e.Identifier == entryId);

                    if (entry != null)
                    {
                        try
                        {
                            entry.BoxedValue = kvp.Value;
                            _logger.Msg($"Applied preference change: {kvp.Key} = {kvp.Value}");
                        }
                        catch (System.Exception ex)
                        {
                            _logger.Error($"Failed to apply preference {kvp.Key}: {ex.Message}");
                        }
                    }
                }
            }

            // Save preferences
            MelonPreferences.Save();
            _modifiedPreferences.Clear();

            // Refresh the display
            ShowModDetails(mod);

            _logger.Msg("Preferences applied and saved successfully");
        }

        private void ResetPreferenceChanges(MelonMod mod)
        {
            _modifiedPreferences.Clear();
            ShowModDetails(mod);
            _logger.Msg("Preference changes reset");
        }

        private void UpdateButtonStates(Button applyButton, Button resetButton)
        {
            var hasChanges = _modifiedPreferences.Count > 0;
            applyButton.interactable = hasChanges;
            resetButton.interactable = hasChanges;
        }
    }
}