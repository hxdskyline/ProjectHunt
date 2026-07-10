using System.Collections.Generic;
using UnityEngine;

namespace ProjectHunt.Data
{
    public static class FireGlandCharacterFactory
    {
        private static readonly Dictionary<string, CharacterConfig> Cache = new Dictionary<string, CharacterConfig>();

        public static CharacterConfig GetFireVariant(CharacterConfig baseConfig)
        {
            if (baseConfig == null)
            {
                return null;
            }

            var cacheKey = GetRoleCacheKey(baseConfig);

            if (Cache.TryGetValue(cacheKey, out var cached) && cached != null)
            {
                return cached;
            }

            var fireConfig = ScriptableObject.CreateInstance<CharacterConfig>();
            fireConfig.name = $"{cacheKey}_Runtime";
            fireConfig.id = cacheKey;
            fireConfig.displayName = FireGlandRules.GetVariantDisplayName(baseConfig.roleType);
            fireConfig.roleType = baseConfig.roleType;
            fireConfig.isHammerVariant = false;
            fireConfig.baseCharacterId = string.IsNullOrWhiteSpace(baseConfig.baseCharacterId) ? baseConfig.id : baseConfig.baseCharacterId;
            fireConfig.resourceId = GetRoleResourceId(baseConfig);
            fireConfig.defaultAttackAction = baseConfig.defaultAttackAction;
            fireConfig.moveAction = baseConfig.moveAction;
            fireConfig.visualScale = baseConfig.visualScale;
            fireConfig.yOffset = baseConfig.yOffset;
            fireConfig.defaultWeaponType = baseConfig.defaultWeaponType;
            fireConfig.attackTempo = baseConfig.attackTempo;
            fireConfig.attackRangeType = baseConfig.attackRangeType;
            fireConfig.spawnSlot = baseConfig.spawnSlot;
            fireConfig.isPlayable = baseConfig.isPlayable;

            Cache[cacheKey] = fireConfig;
            return fireConfig;
        }

        private static string GetRoleCacheKey(CharacterConfig baseConfig)
        {
            switch (baseConfig.roleType)
            {
                case RoleType.Archer:
                    return "player_archer_fire";
                case RoleType.Assassin:
                    return "player_assassin_fire";
                default:
                    return "player_swordsman_fire";
            }
        }

        private static string GetRoleResourceId(CharacterConfig baseConfig)
        {
            switch (baseConfig.roleType)
            {
                case RoleType.Archer:
                    return "longbowman_fire";
                case RoleType.Assassin:
                    return "assassin_fire";
                default:
                    return "human_swordsman_fire";
            }
        }
    }
}
