using System.Collections.Generic;
using UnityEngine;

namespace ProjectHunt.UI
{
    public static class ExtractedArtLibrary
    {
        private static readonly Dictionary<string, Sprite> Sprites = new Dictionary<string, Sprite>();

        public static Sprite LoadUi(string name)
        {
            return Load("Art/UI/" + name);
        }

        public static Sprite LoadEnvironment(string name)
        {
            return Load("Art/Environment/" + name);
        }

        private static Sprite Load(string resourcePath)
        {
            if (Sprites.TryGetValue(resourcePath, out var cached))
            {
                return cached;
            }

            var texture = Resources.Load<Texture2D>(resourcePath);
            if (texture == null)
            {
                return null;
            }

            texture.filterMode = FilterMode.Point;
            var sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                32f,
                0,
                SpriteMeshType.FullRect);
            sprite.name = resourcePath;
            Sprites[resourcePath] = sprite;
            return sprite;
        }
    }
}
