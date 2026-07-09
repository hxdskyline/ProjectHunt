using UnityEngine;

namespace ProjectHunt.Battle
{
    public static class SimpleSpriteFactory
    {
        private static Sprite _meteorHammerSprite;

        public static Sprite GetMeteorHammerSprite()
        {
            if (_meteorHammerSprite != null)
            {
                return _meteorHammerSprite;
            }

            var tex = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            var clear = new Color(0, 0, 0, 0);
            var dark = new Color32(60, 60, 70, 255);
            var metal = new Color32(160, 160, 170, 255);
            var chain = new Color32(110, 90, 70, 255);

            for (var y = 0; y < 16; y++)
            {
                for (var x = 0; x < 16; x++)
                {
                    tex.SetPixel(x, y, clear);
                }
            }

            for (var x = 2; x <= 7; x++)
            {
                tex.SetPixel(x, 8, chain);
            }

            for (var y = 7; y <= 9; y++)
            {
                tex.SetPixel(8, y, chain);
            }

            for (var y = 5; y <= 11; y++)
            {
                for (var x = 9; x <= 14; x++)
                {
                    if ((x - 11) * (x - 11) + (y - 8) * (y - 8) <= 9)
                    {
                        tex.SetPixel(x, y, metal);
                    }
                }
            }

            tex.SetPixel(11, 8, dark);
            tex.Apply();

            _meteorHammerSprite = Sprite.Create(
                tex,
                new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f),
                16f,
                0,
                SpriteMeshType.FullRect);

            return _meteorHammerSprite;
        }
    }
}
