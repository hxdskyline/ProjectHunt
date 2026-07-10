using System.Collections.Generic;
using UnityEngine;

namespace ProjectHunt.Data
{
    public static class HammerCharacterFactory
    {
        private static readonly Dictionary<string, CharacterConfig> Cache = new Dictionary<string, CharacterConfig>();

        public static CharacterConfig GetHammerVariant(CharacterConfig baseConfig)
        {
            if (baseConfig == null)
            {
                return null;
            }

            if (IsHammerVariant(baseConfig))
            {
                return baseConfig;
            }

            var cacheKey = string.IsNullOrWhiteSpace(baseConfig.id)
                ? baseConfig.GetInstanceID().ToString()
                : baseConfig.id;
            if (Cache.TryGetValue(cacheKey, out var cached) && cached != null)
            {
                return cached;
            }

            var hammerConfig = ScriptableObject.CreateInstance<CharacterConfig>();
            hammerConfig.name = $"{baseConfig.name}_HammerRuntime";
            hammerConfig.id = $"{baseConfig.id}_hammer";
            hammerConfig.displayName = GetHammerDisplayName(baseConfig.roleType);
            hammerConfig.roleType = baseConfig.roleType;
            hammerConfig.isHammerVariant = true;
            hammerConfig.baseCharacterId = baseConfig.id;
            hammerConfig.resourceId = $"{baseConfig.resourceId}_hammer";
            hammerConfig.defaultAttackAction = baseConfig.defaultAttackAction;
            hammerConfig.moveAction = baseConfig.moveAction;
            hammerConfig.visualScale = baseConfig.visualScale;
            hammerConfig.yOffset = baseConfig.yOffset;
            hammerConfig.defaultWeaponType = WeaponType.MeteorHammer;
            hammerConfig.attackTempo = baseConfig.attackTempo;
            hammerConfig.attackRangeType = baseConfig.attackRangeType;
            hammerConfig.spawnSlot = baseConfig.spawnSlot;
            hammerConfig.isPlayable = baseConfig.isPlayable;

            Cache[cacheKey] = hammerConfig;
            return hammerConfig;
        }

        public static bool IsHammerVariant(CharacterConfig config)
        {
            if (config == null)
            {
                return false;
            }

            if (config.isHammerVariant)
            {
                return true;
            }

            return !string.IsNullOrWhiteSpace(config.resourceId) &&
                   config.resourceId.EndsWith("_hammer");
        }

        public static string GetHammerDisplayName(RoleType roleType)
        {
            switch (roleType)
            {
                case RoleType.Archer:
                    return "\u9524\u5f13\u624b";
                case RoleType.Assassin:
                    return "\u9524\u523a\u5ba2";
                default:
                    return "\u9524\u5251\u58eb";
            }
        }
    }
}
