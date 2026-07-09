using System;
using System.Collections.Generic;
using System.IO;
using ProjectHunt.Data;
using UnityEngine;

namespace ProjectHunt.Battle
{
    public static class PixelAnimationLibrary
    {
        public sealed class ActionClip
        {
            public string name;
            public float frameDuration;
            public Sprite[] frames;

            public float Duration => frames == null ? 0f : frames.Length * frameDuration;
        }

        private static readonly Dictionary<string, Dictionary<string, ActionClip>> ResourceCache =
            new Dictionary<string, Dictionary<string, ActionClip>>();

        public static ActionClip GetClip(string resourceId, string actionName, float fps = 8f)
        {
            if (string.IsNullOrWhiteSpace(resourceId) || string.IsNullOrWhiteSpace(actionName))
            {
                return null;
            }

            if (!ResourceCache.TryGetValue(resourceId, out var actions))
            {
                actions = LoadResource(resourceId, fps);
                ResourceCache[resourceId] = actions;
            }

            if (actions != null && actions.TryGetValue(actionName, out var clip))
            {
                return clip;
            }

            return null;
        }

        private static Dictionary<string, ActionClip> LoadResource(string resourceId, float fps)
        {
            var resourcePath = Path.Combine(Application.dataPath, "bundle", "units", resourceId);
            var indexPath = Path.Combine(resourcePath, "index.json");
            if (!File.Exists(indexPath))
            {
                Debug.LogWarning($"PixelAnimationLibrary could not find index.json for {resourceId}");
                return new Dictionary<string, ActionClip>();
            }

            var json = File.ReadAllText(indexPath);
            var root = MiniJson.Deserialize(json) as Dictionary<string, object>;
            if (root == null || !root.TryGetValue("actions", out var actionsObj))
            {
                return new Dictionary<string, ActionClip>();
            }

            var actionsDict = actionsObj as Dictionary<string, object>;
            var result = new Dictionary<string, ActionClip>();
            if (actionsDict == null)
            {
                return result;
            }

            foreach (var pair in actionsDict)
            {
                var actionData = pair.Value as Dictionary<string, object>;
                if (actionData == null || !actionData.TryGetValue("frames", out var framesObj))
                {
                    continue;
                }

                var frameList = framesObj as List<object>;
                if (frameList == null || frameList.Count == 0)
                {
                    continue;
                }

                var sprites = new List<Sprite>(frameList.Count);
                for (var i = 0; i < frameList.Count; i++)
                {
                    var fileName = frameList[i] as string;
                    var sprite = LoadSprite(Path.Combine(resourcePath, fileName));
                    if (sprite != null)
                    {
                        sprites.Add(sprite);
                    }
                }

                result[pair.Key] = new ActionClip
                {
                    name = pair.Key,
                    frameDuration = fps <= 0f ? 0.125f : 1f / fps,
                    frames = sprites.ToArray(),
                };
            }

            return result;
        }

        private static Sprite LoadSprite(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return null;
            }

            var bytes = File.ReadAllBytes(filePath);
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            if (!texture.LoadImage(bytes))
            {
                UnityEngine.Object.Destroy(texture);
                return null;
            }

            texture.name = Path.GetFileNameWithoutExtension(filePath);
            return Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                16f,
                0,
                SpriteMeshType.FullRect);
        }
    }
}
