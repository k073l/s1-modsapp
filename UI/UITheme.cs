using ModsApp.UI;
using UnityEngine;

namespace ModsApp
{
    public class UITheme
    {
        public readonly Color BgPrimary = new Color(0.12f, 0.12f, 0.15f, 1f);
        public readonly Color BgSecondary = new Color(0.16f, 0.16f, 0.20f, 1f);
        public readonly Color BgCard = new Color(0.20f, 0.20f, 0.24f, 1f);
        public readonly Color AccentPrimary = new Color(0.26f, 0.59f, 0.98f, 1f);
        public readonly Color AccentSecondary = new Color(0.15f, 0.15f, 0.18f, 1f);
        public readonly Color TextPrimary = new Color(0.95f, 0.95f, 0.95f, 1f);
        public readonly Color TextSecondary = new Color(0.70f, 0.70f, 0.70f, 1f);
        public readonly Color SuccessColor = new Color(0.20f, 0.80f, 0.20f, 1f);
        public readonly Color WarningColor = new Color(0.95f, 0.75f, 0.20f, 1f);
        
        private const int _sizeTiny = 10;
        private const int _sizeSmall = 12;
        private const int _sizeStandard = 14;
        private const int _sizeMedium = 16;
        private const int _sizeLarge = 20;

        private float _textScale = 1.0f;

        public int SizeTiny => Mathf.RoundToInt(_sizeTiny * _textScale);
        public int SizeSmall => Mathf.RoundToInt(_sizeSmall * _textScale);
        public int SizeStandard => Mathf.RoundToInt(_sizeStandard * _textScale);
        public int SizeMedium => Mathf.RoundToInt(_sizeMedium * _textScale);
        public int SizeLarge => Mathf.RoundToInt(_sizeLarge * _textScale);

        public void SetTextScale(TextSizeProfile profile)
        {
            _textScale = profile switch
            {
                TextSizeProfile.ExtraSmall => 0.75f,
                TextSizeProfile.Small => 0.85f,
                TextSizeProfile.Normal => 1.0f,
                TextSizeProfile.Large => 1.25f,
                TextSizeProfile.ExtraLarge => 1.5f,
                _ => 1.0f
            };
        }

        public UITheme()
        {
            SetTextScale(ModsApp.TextSizeProfileEntry.Value);
        }
    }
}