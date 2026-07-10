using UnityEngine;

namespace ProjectHunt.Battle
{
    public static class SimpleSpriteFactory
    {
        private static Sprite _meteorHammerSprite;
        private static Sprite _fireGlandSprite;
        private static Sprite _whitePixelSprite;
        private static Sprite _hitSparkSprite;

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

        public static Sprite GetWhitePixelSprite()
        {
            if (_whitePixelSprite != null)
            {
                return _whitePixelSprite;
            }

            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();

            _whitePixelSprite = Sprite.Create(
                tex,
                new Rect(0, 0, 1, 1),
                new Vector2(0.5f, 0.5f),
                1f,
                0,
                SpriteMeshType.FullRect);

            return _whitePixelSprite;
        }

        public static Sprite GetFireGlandSprite()
        {
            if (_fireGlandSprite != null)
            {
                return _fireGlandSprite;
            }

            var tex = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            var clear = new Color(0f, 0f, 0f, 0f);
            var dark = new Color32(104, 24, 12, 255);
            var core = new Color32(240, 88, 32, 255);
            var glow = new Color32(255, 190, 76, 255);

            for (var y = 0; y < 16; y++)
            {
                for (var x = 0; x < 16; x++)
                {
                    tex.SetPixel(x, y, clear);
                }
            }

            for (var y = 3; y <= 12; y++)
            {
                for (var x = 3; x <= 12; x++)
                {
                    var dx = x - 7.5f;
                    var dy = y - 8f;
                    var distance = dx * dx * 0.9f + dy * dy;
                    if (distance <= 18f)
                    {
                        tex.SetPixel(x, y, dark);
                    }

                    if (distance <= 11f)
                    {
                        tex.SetPixel(x, y, core);
                    }

                    if (distance <= 5.5f)
                    {
                        tex.SetPixel(x, y, glow);
                    }
                }
            }

            tex.SetPixel(7, 8, Color.white);
            tex.SetPixel(8, 8, Color.white);
            tex.Apply();

            _fireGlandSprite = Sprite.Create(
                tex,
                new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f),
                16f,
                0,
                SpriteMeshType.FullRect);

            return _fireGlandSprite;
        }

        public static Sprite GetHitSparkSprite()
        {
            if (_hitSparkSprite != null)
            {
                return _hitSparkSprite;
            }

            var tex = new Texture2D(8, 8, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            var clear = new Color(0f, 0f, 0f, 0f);
            var bright = new Color32(255, 243, 176, 255);
            var hot = new Color32(255, 196, 90, 255);

            for (var y = 0; y < 8; y++)
            {
                for (var x = 0; x < 8; x++)
                {
                    tex.SetPixel(x, y, clear);
                }
            }

            for (var i = 1; i <= 6; i++)
            {
                tex.SetPixel(3, i, bright);
                tex.SetPixel(4, i, bright);
                tex.SetPixel(i, 3, bright);
                tex.SetPixel(i, 4, bright);
            }

            tex.SetPixel(2, 2, hot);
            tex.SetPixel(5, 2, hot);
            tex.SetPixel(2, 5, hot);
            tex.SetPixel(5, 5, hot);
            tex.SetPixel(3, 3, Color.white);
            tex.SetPixel(4, 3, Color.white);
            tex.SetPixel(3, 4, Color.white);
            tex.SetPixel(4, 4, Color.white);
            tex.Apply();

            _hitSparkSprite = Sprite.Create(
                tex,
                new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f),
                8f,
                0,
                SpriteMeshType.FullRect);

            return _hitSparkSprite;
        }
    }
}
