using UnityEngine;

namespace ProjectHunt.Build
{
    public static class BuildUiRuntimeStyle
    {
        private static Font _cachedFont;

        public static Font GetChineseFont()
        {
            if (_cachedFont != null)
            {
                return _cachedFont;
            }

            _cachedFont = Font.CreateDynamicFontFromOSFont(
                new[]
                {
                    "Microsoft YaHei UI",
                    "Microsoft YaHei",
                    "SimHei",
                    "Noto Sans CJK SC",
                    "Arial Unicode MS",
                    "Arial",
                },
                28);

            return _cachedFont;
        }
    }
}
