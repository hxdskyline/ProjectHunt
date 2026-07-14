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

    public static class HolyCupCharacterFactory
    {
        private static readonly Dictionary<string, CharacterConfig> Cache = new Dictionary<string, CharacterConfig>();

        public static CharacterConfig GetCupVariant(CharacterConfig baseConfig)
        {
            if (baseConfig == null)
            {
                return null;
            }

            var cacheKey = GetIdentityKey(baseConfig) + "_cup";
            if (Cache.TryGetValue(cacheKey, out var cached) && cached != null)
            {
                return cached;
            }

            var cupConfig = ScriptableObject.CreateInstance<CharacterConfig>();
            cupConfig.name = cacheKey + "_Runtime";
            cupConfig.id = cacheKey;
            cupConfig.displayName = HolyCupRules.GetVariantDisplayName(baseConfig.roleType);
            cupConfig.roleType = baseConfig.roleType;
            cupConfig.isHammerVariant = false;
            cupConfig.baseCharacterId = string.IsNullOrWhiteSpace(baseConfig.baseCharacterId) ? baseConfig.id : baseConfig.baseCharacterId;
            cupConfig.resourceId = GetCupResourceId(baseConfig);
            cupConfig.defaultAttackAction = baseConfig.defaultAttackAction;
            cupConfig.moveAction = baseConfig.moveAction;
            cupConfig.visualScale = baseConfig.visualScale;
            cupConfig.yOffset = baseConfig.yOffset;
            cupConfig.defaultWeaponType = baseConfig.defaultWeaponType;
            cupConfig.attackTempo = baseConfig.attackTempo;
            cupConfig.attackRangeType = baseConfig.attackRangeType;
            cupConfig.spawnSlot = baseConfig.spawnSlot;
            cupConfig.isPlayable = baseConfig.isPlayable;
            if (baseConfig.roleType == RoleType.Archer)
            {
                // Catapult frames are wider than the archer frames; preserve on-screen height.
                cupConfig.visualScale = baseConfig.visualScale * (32f / 45f);
            }
            Cache[cacheKey] = cupConfig;
            return cupConfig;
        }

        private static string GetIdentityKey(CharacterConfig config)
        {
            return config != null && !string.IsNullOrWhiteSpace(config.id)
                ? config.id
                : config != null ? config.GetInstanceID().ToString() : "unknown";
        }

        private static string GetCupResourceId(CharacterConfig baseConfig)
        {
            switch (baseConfig.roleType)
            {
                case RoleType.Archer:
                    return "catapult";
                case RoleType.Assassin:
                    return "assassin_cup";
                default:
                    return "human_swordsman_cup";
            }
        }
    }

    public static class GiantKeyCharacterFactory
    {
        private static readonly Dictionary<string, CharacterConfig> Cache = new Dictionary<string, CharacterConfig>();

        public static CharacterConfig GetKeyVariant(CharacterConfig baseConfig)
        {
            if (baseConfig == null)
            {
                return null;
            }

            var cacheKey = GetIdentityKey(baseConfig) + "_key";
            if (Cache.TryGetValue(cacheKey, out var cached) && cached != null)
            {
                return cached;
            }

            var keyConfig = ScriptableObject.CreateInstance<CharacterConfig>();
            keyConfig.name = cacheKey + "_Runtime";
            keyConfig.id = cacheKey;
            keyConfig.displayName = GiantKeyRules.GetVariantDisplayName(baseConfig.roleType);
            keyConfig.roleType = baseConfig.roleType;
            keyConfig.isHammerVariant = false;
            keyConfig.baseCharacterId = string.IsNullOrWhiteSpace(baseConfig.baseCharacterId) ? baseConfig.id : baseConfig.baseCharacterId;
            keyConfig.resourceId = GetKeyResourceId(baseConfig);
            keyConfig.defaultAttackAction = baseConfig.defaultAttackAction;
            keyConfig.moveAction = baseConfig.moveAction;
            keyConfig.visualScale = baseConfig.visualScale;
            keyConfig.yOffset = baseConfig.yOffset;
            keyConfig.defaultWeaponType = baseConfig.defaultWeaponType;
            keyConfig.attackTempo = baseConfig.attackTempo;
            keyConfig.attackRangeType = baseConfig.attackRangeType;
            keyConfig.spawnSlot = baseConfig.spawnSlot;
            keyConfig.isPlayable = baseConfig.isPlayable;
            Cache[cacheKey] = keyConfig;
            return keyConfig;
        }

        private static string GetIdentityKey(CharacterConfig config)
        {
            return config != null && !string.IsNullOrWhiteSpace(config.id)
                ? config.id
                : config != null ? config.GetInstanceID().ToString() : "unknown";
        }

        private static string GetKeyResourceId(CharacterConfig baseConfig)
        {
            switch (baseConfig.roleType)
            {
                case RoleType.Archer:
                    return "longbowman_key";
                case RoleType.Assassin:
                    return "assassin_key";
                case RoleType.Mage:
                    // Mage key art is still a placeholder; keep the blue mage body visible.
                    return "mage_blue";
                default:
                    return "human_swordsman_key";
            }
        }
    }

    public static class MageCharacterFactory
    {
        public static CharacterConfig GetMageVariant(RewardType rewardType)
        {
            var config = ScriptableObject.CreateInstance<CharacterConfig>();
            config.id = "mage_" + rewardType.ToString().ToLowerInvariant();
            config.baseCharacterId = "mage";
            config.displayName = rewardType == RewardType.MeteorHammer ? "锤法师" : rewardType == RewardType.HolyCup ? "酒法师" : "法师";
            config.roleType = RoleType.Mage;
            config.resourceId = "mage_blue";
            config.defaultAttackAction = "attack";
            config.moveAction = "walk";
            config.visualScale = 2.5f;
            config.yOffset = -0.15f;
            config.defaultWeaponType = WeaponType.Bow;
            config.attackTempo = AttackTempo.Medium;
            config.attackRangeType = AttackRangeType.RangedLine;
            config.spawnSlot = SpawnSlot.Back;
            return config;
        }
    }
}
