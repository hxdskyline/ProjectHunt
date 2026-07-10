using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ProjectHunt.Battle
{
    public static class ExternalSpriteLibrary
    {
        private const string ExternalRoot = "E:\\SteamLibrary\\steamapps\\common\\The King is Watching\\_extracted_images";
        private static readonly Dictionary<string, Sprite> SpriteCache = new Dictionary<string, Sprite>();
        private static Sprite[] _ballistaArrowFrames;

        public static Sprite GetLongbowArrowSprite()
        {
            return GetSprite("sprites/image_32789_tex002_18x7.png");
        }

        public static Sprite GetAssassinBladeSprite()
        {
            return GetSprite("sprites/image_28773_tex020_75x75.png");
        }

        public static Sprite[] GetBallistaArrowFrames()
        {
            if (_ballistaArrowFrames != null)
            {
                return _ballistaArrowFrames;
            }

            _ballistaArrowFrames = new[]
            {
                GetSprite("sprites/image_32789_tex002_18x7.png"),
                GetSprite("sprites/image_33132_tex002_18x7.png"),
                GetSprite("sprites/image_32788_tex002_18x7.png"),
                GetSprite("sprites/image_33133_tex002_18x7.png"),
            };

            return _ballistaArrowFrames;
        }

        private static Sprite GetSprite(string relativePath)
        {
            if (SpriteCache.TryGetValue(relativePath, out var cached))
            {
                return cached;
            }

            var fullPath = Path.Combine(ExternalRoot, relativePath.Replace("/", "\\"));
            var sprite = PixelAnimationLibrary.LoadSpriteFromAbsolutePath(fullPath);
            if (sprite != null)
            {
                SpriteCache[relativePath] = sprite;
            }

            return sprite;
        }
    }
}
