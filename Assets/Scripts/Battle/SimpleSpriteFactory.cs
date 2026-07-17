using UnityEngine;

namespace ProjectHunt.Battle
{
    public static class SimpleSpriteFactory
    {
        private static Sprite _meteorHammerSprite;
        private static Sprite _holyCupSprite;
        private static Sprite _giantKeySprite;
        private static Sprite _whitePixelSprite;
        private static Sprite _hitSparkSprite;
        private static Sprite _heartIconSprite;
        private static Sprite _crossedSwordsIconSprite;
        private static Sprite _prohibitedIconSprite;

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

        public static Sprite GetHolyCupSprite()
        {
            if (_holyCupSprite != null)
            {
                return _holyCupSprite;
            }

            var tex = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            var clear = new Color(0f, 0f, 0f, 0f);
            var bowl = new Color32(226, 184, 98, 255);
            var stem = new Color32(176, 126, 60, 255);
            var fill = new Color32(255, 116, 86, 255);

            for (var y = 0; y < 16; y++)
            {
                for (var x = 0; x < 16; x++)
                {
                    tex.SetPixel(x, y, clear);
                }
            }

            // Texture coordinates grow upward, so the bowl belongs above the stem.
            for (var y = 7; y <= 11; y++)
            {
                for (var x = 4; x <= 11; x++)
                {
                    if (y == 7 || y == 11 || x == 4 || x == 11)
                    {
                        tex.SetPixel(x, y, bowl);
                    }
                    else
                    {
                        tex.SetPixel(x, y, fill);
                    }
                }
            }

            tex.SetPixel(7, 6, bowl);
            tex.SetPixel(8, 6, bowl);
            tex.SetPixel(7, 5, stem);
            tex.SetPixel(8, 5, stem);
            for (var x = 6; x <= 9; x++)
            {
                tex.SetPixel(x, 4, stem);
            }
            for (var x = 5; x <= 10; x++)
            {
                tex.SetPixel(x, 3, bowl);
            }
            tex.Apply();

            _holyCupSprite = Sprite.Create(
                tex,
                new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f),
                16f,
                0,
                SpriteMeshType.FullRect);

            return _holyCupSprite;
        }

        public static Sprite GetGiantKeySprite()
        {
            if (_giantKeySprite != null)
            {
                return _giantKeySprite;
            }

            var tex = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            var clear = new Color(0f, 0f, 0f, 0f);
            var metal = new Color32(211, 176, 86, 255);
            var dark = new Color32(126, 96, 42, 255);

            for (var y = 0; y < 16; y++)
            {
                for (var x = 0; x < 16; x++)
                {
                    tex.SetPixel(x, y, clear);
                }
            }

            for (var y = 4; y <= 11; y++)
            {
                for (var x = 2; x <= 7; x++)
                {
                    var dx = x - 4.5f;
                    var dy = y - 7.5f;
                    if (dx * dx + dy * dy <= 8f)
                    {
                        tex.SetPixel(x, y, metal);
                    }
                }
            }

            tex.SetPixel(4, 7, clear);
            tex.SetPixel(5, 7, clear);
            tex.SetPixel(4, 8, clear);
            tex.SetPixel(5, 8, clear);

            for (var x = 7; x <= 13; x++)
            {
                tex.SetPixel(x, 7, metal);
                tex.SetPixel(x, 8, metal);
            }

            tex.SetPixel(12, 9, dark);
            tex.SetPixel(13, 9, metal);
            tex.SetPixel(12, 10, dark);
            tex.SetPixel(13, 10, metal);
            tex.SetPixel(10, 5, Color.white);
            tex.Apply();

            _giantKeySprite = Sprite.Create(
                tex,
                new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f),
                16f,
                0,
                SpriteMeshType.FullRect);

            return _giantKeySprite;
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

        public static Sprite GetHeartIconSprite()
        {
            if (_heartIconSprite != null)
            {
                return _heartIconSprite;
            }

            var pixels = new[]
            {
                "................",
                "................",
                "...RR....RR.....",
                "..RDDR..RDDR....",
                ".RDDDDRRDDDDR...",
                ".RDDDDDDDDDDR...",
                ".RDDDDDDDDDDR...",
                "..RDDDDDDDDR....",
                "...RDDDDDDR.....",
                "....RDDDDR......",
                ".....RDDR.......",
                "......RR........",
                "................",
                "................",
                "................",
                "................",
            };
            _heartIconSprite = CreatePixelIcon(pixels, new Color32(245, 72, 82, 255), new Color32(150, 35, 48, 255));
            return _heartIconSprite;
        }

        public static Sprite GetCrossedSwordsIconSprite()
        {
            if (_crossedSwordsIconSprite != null)
            {
                return _crossedSwordsIconSprite;
            }

            var pixels = new[]
            {
                "................",
                "..L.........L...",
                "...L.......L....",
                "....L.....L.....",
                ".....L...L......",
                "......L.L.......",
                ".......L........",
                "......L.L.......",
                ".....L...L......",
                "....L.....L.....",
                "...H.......H....",
                "..HHH.....HHH...",
                "...H.......H....",
                "................",
                "................",
                "................",
            };
            _crossedSwordsIconSprite = CreatePixelIcon(pixels, new Color32(226, 226, 218, 255), new Color32(188, 132, 56, 255));
            return _crossedSwordsIconSprite;
        }

        public static Sprite GetProhibitedIconSprite()
        {
            if (_prohibitedIconSprite != null)
            {
                return _prohibitedIconSprite;
            }

            const int size = 32;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;
            var red = new Color32(238, 66, 58, 255);
            var dark = new Color32(92, 20, 22, 255);
            var center = new Vector2(15.5f, 15.5f);
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var distance = Vector2.Distance(new Vector2(x, y), center);
                    var ring = distance >= 11f && distance <= 14f;
                    var slash = Mathf.Abs((x + y) - 31f) <= 2f && distance <= 14f;
                    var shadow = distance >= 14f && distance <= 15f;
                    texture.SetPixel(x, y, ring || slash ? red : shadow ? dark : Color.clear);
                }
            }
            texture.Apply();
            _prohibitedIconSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                size,
                0,
                SpriteMeshType.FullRect);
            return _prohibitedIconSprite;
        }

        private static Sprite CreatePixelIcon(string[] pixels, Color light, Color dark)
        {
            var height = pixels.Length;
            var width = pixels[0].Length;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;
            for (var y = 0; y < height; y++)
            {
                var row = pixels[height - 1 - y];
                for (var x = 0; x < width; x++)
                {
                    texture.SetPixel(x, y, row[x] == 'L' || row[x] == 'R'
                        ? light
                        : row[x] == 'H' || row[x] == 'D' ? dark : Color.clear);
                }
            }
            texture.Apply();

            return Sprite.Create(
                texture,
                new Rect(0, 0, width, height),
                new Vector2(0.5f, 0.5f),
                16f,
                0,
                SpriteMeshType.FullRect);
        }
    }
}
