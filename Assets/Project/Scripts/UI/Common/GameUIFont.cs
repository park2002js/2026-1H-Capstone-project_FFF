using TMPro;

namespace FFF.UI.Common
{
    public static class GameUIFont
    {
        public static TMP_FontAsset Resolve(TMP_FontAsset fallback = null)
        {
            return TMP_Settings.defaultFontAsset != null
                ? TMP_Settings.defaultFontAsset
                : fallback;
        }

        public static void Apply(TextMeshProUGUI label, TMP_FontAsset fallback = null)
        {
            if (label == null)
                return;

            TMP_FontAsset font = Resolve(fallback);
            if (font != null)
                label.font = font;
        }
    }
}
