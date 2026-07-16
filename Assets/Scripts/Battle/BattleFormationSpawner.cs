using System;
using System.Collections.Generic;
using ProjectHunt.Data;
using ProjectHunt.Flow;
using UnityEngine;

namespace ProjectHunt.Battle
{
    public sealed class BattleFormationSpawner : MonoBehaviour
    {
        [Serializable]
        public sealed class CharacterPrefabBinding
        {
            public CharacterConfig config;
            public GameObject prefab;
        }

        [Serializable]
        public sealed class BossPrefabBinding
        {
            public BossConfig config;
            public GameObject prefab;
        }

        [Header("Context")]
        public DemoGameContext gameContext;
        public BattleSceneReferences sceneReferences;
        public BattleDirector battleDirector;

        [Header("Prefab Bindings")]
        public List<CharacterPrefabBinding> characterBindings = new List<CharacterPrefabBinding>();
        public List<BossPrefabBinding> bossBindings = new List<BossPrefabBinding>();

        private readonly List<GameObject> _spawnedPlayers = new List<GameObject>();
        private GameObject _spawnedBoss;
        private BossConfig _battle03BossConfig;
        private BossConfig _battle05BossConfig;

        public IReadOnlyList<GameObject> SpawnedPlayers => _spawnedPlayers;
        public GameObject SpawnedBoss => _spawnedBoss;

        public void SpawnCurrentBattle()
        {
            if (gameContext == null)
            {
                gameContext = DemoGameContext.Instance;
            }

            if (gameContext == null || gameContext.defaultBattleFormation == null)
            {
                Debug.LogError("BattleFormationSpawner is missing DemoGameContext or default battle formation.");
                return;
            }

            if (sceneReferences == null)
            {
                Debug.LogError("BattleFormationSpawner is missing BattleSceneReferences.");
                return;
            }

            ClearSpawnedUnits();

            var formation = gameContext.defaultBattleFormation;
            Debug.Log($"[BattleFormation] phase={gameContext.runState.phase}, spawning players at: front={sceneReferences.playerFrontPoint.position}, mid={sceneReferences.playerMidPoint.position}, back={sceneReferences.playerBackPoint.position}");
            var battleRoster = new List<CharacterConfig>
            {
                ResolveBattleCharacter(formation.frontCharacter),
                ResolveBattleCharacter(formation.midCharacter),
                ResolveBattleCharacter(formation.backCharacter),
            };
            battleRoster.Sort((left, right) => GetBattlePositionOrder(left).CompareTo(GetBattlePositionOrder(right)));
            var positions = new[]
            {
                sceneReferences.playerFrontPoint,
                sceneReferences.playerMidPoint,
                sceneReferences.playerBackPoint,
            };
            for (var i = 0; i < battleRoster.Count && i < positions.Length; i++)
            {
                SpawnCharacter(battleRoster[i], positions[i], sceneReferences.playerTeamRoot);
            }

            var bossConfig = ResolveBattleBoss(formation.boss, battleDirector == null || !battleDirector.UsesPreBossWaves);
            if (bossConfig != null)
            {
                SpawnBoss(bossConfig, sceneReferences.bossPoint, sceneReferences.bossRoot);
            }
        }

        public void SpawnDelayedBoss()
        {
            if (_spawnedBoss != null || gameContext == null || gameContext.defaultBattleFormation == null || sceneReferences == null)
            {
                return;
            }

            var bossConfig = ResolveBattleBoss(gameContext.defaultBattleFormation.boss, true);
            if (bossConfig != null)
            {
                SpawnBoss(bossConfig, sceneReferences.bossPoint, sceneReferences.bossRoot);
            }
        }

        public void ClearSpawnedUnits()
        {
            for (var i = 0; i < _spawnedPlayers.Count; i++)
            {
                if (_spawnedPlayers[i] != null)
                {
                    Destroy(_spawnedPlayers[i]);
                }
            }

            _spawnedPlayers.Clear();

            if (_spawnedBoss != null)
            {
                Destroy(_spawnedBoss);
                _spawnedBoss = null;
            }
        }

        private void SpawnCharacter(CharacterConfig config, Transform spawnPoint, Transform parentRoot)
        {
            if (config == null || spawnPoint == null || parentRoot == null)
            {
                return;
            }

            var prefab = FindCharacterPrefab(config);
            var instance = prefab != null
                ? Instantiate(prefab, spawnPoint.position, Quaternion.identity, parentRoot)
                : CreateFallbackUnit($"{config.id}_Fallback", spawnPoint.position, parentRoot);

            instance.name = $"{config.id}_Instance";
            var controller = EnsureCombatUnit(instance);
            controller.Setup(config, battleDirector);
            _spawnedPlayers.Add(instance);
        }

        private static int GetBattlePositionOrder(CharacterConfig config)
        {
            if (config == null)
            {
                return int.MaxValue;
            }

            return config.roleType switch
            {
                RoleType.Swordsman => 0,
                RoleType.Assassin => 1,
                RoleType.Mage => 2,
                RoleType.Archer => 3,
                _ => int.MaxValue,
            };
        }

        private void SpawnBoss(BossConfig config, Transform spawnPoint, Transform parentRoot)
        {
            if (config == null || spawnPoint == null || parentRoot == null)
            {
                return;
            }

            var spawnPosition = spawnPoint.position;
            if (IsBattle03Active())
            {
                spawnPosition = new Vector3(4.65f, 0f, 0f);
            }

            var prefab = FindBossPrefab(config);
            _spawnedBoss = prefab != null
                ? Instantiate(prefab, spawnPosition, Quaternion.identity, parentRoot)
                : CreateFallbackUnit($"{config.id}_Fallback", spawnPosition, parentRoot);

            _spawnedBoss.name = $"{config.id}_Instance";
            var controller = EnsureCombatUnit(_spawnedBoss);
            controller.Setup(config, battleDirector);

            var bossRenderer = _spawnedBoss.GetComponent<SpriteRenderer>();
            if (bossRenderer != null)
            {
                bossRenderer.flipX = true;
            }
        }

        private bool IsBattle02Active()
        {
            if (battleDirector != null && battleDirector.IsBattle02)
            {
                return true;
            }

            return gameContext != null &&
                   (gameContext.runState.phase == GamePhase.Battle02 || gameContext.runState.isBattle02Started);
        }

        private bool IsBattle03Active()
        {
            if (battleDirector != null && battleDirector.IsBattle03)
            {
                return true;
            }

            return gameContext != null &&
                   (gameContext.runState.phase == GamePhase.Battle03 || gameContext.runState.isBattle03Started);
        }

        private bool IsBattle04Active()
        {
            if (battleDirector != null && battleDirector.IsBattle04)
            {
                return true;
            }

            return gameContext != null &&
                   (gameContext.runState.phase == GamePhase.Battle04 || gameContext.runState.isBattle04Started);
        }

        private bool IsBattle05Active()
        {
            if (battleDirector != null && battleDirector.IsBattle05)
            {
                return true;
            }

            return gameContext != null &&
                   (gameContext.runState.phase == GamePhase.Battle05 || gameContext.runState.isBattle05Started);
        }

        private bool IsBattle06Active()
        {
            if (battleDirector != null && battleDirector.IsBattle06)
            {
                return true;
            }

            return gameContext != null &&
                   (gameContext.runState.phase == GamePhase.Battle06 || gameContext.runState.isBattle06Started);
        }

        private CharacterConfig ResolveBattleCharacter(CharacterConfig baseConfig)
        {
            if (baseConfig == null || gameContext == null)
            {
                return baseConfig;
            }

            if (gameContext.runState.hasRecruitedMage &&
                baseConfig.id == gameContext.runState.mageReplacedCharacterId)
            {
                return MageCharacterFactory.GetMageVariant(gameContext.runState.mageRewardType);
            }

            var resolved = baseConfig;
            resolved = ApplyRewardVariant(
                resolved,
                gameContext.buildSelection.selectedHammerTargetId,
                gameContext.buildSelection.selectedHammerCharacter);
            resolved = ApplyRewardVariant(
                resolved,
                gameContext.buildSelection.selectedCupTargetId,
                gameContext.buildSelection.selectedCupCharacter);
            resolved = ApplyRewardVariant(
                resolved,
                gameContext.buildSelection.selectedKeyTargetId,
                gameContext.buildSelection.selectedKeyCharacter);
            return resolved;
        }

        private static CharacterConfig ApplyRewardVariant(CharacterConfig currentConfig, string targetId, CharacterConfig rewardVariant)
        {
            if (currentConfig == null || string.IsNullOrWhiteSpace(targetId) || rewardVariant == null)
            {
                return currentConfig;
            }

            var currentId = currentConfig.id;
            var baseId = currentConfig.baseCharacterId;
            return currentId == targetId || baseId == targetId
                ? rewardVariant
                : currentConfig;
        }

        private BossConfig ResolveBattleBoss(BossConfig defaultBoss, bool includeDelayedBoss)
        {
            // Battle 02 is the burning-village validation stage: three small-enemy waves,
            // not a boss fight. BattleDirector owns those wave spawns.
            if (IsBattle02Active())
            {
                return null;
            }

            if (IsBattle04Active())
            {
                return null;
            }

            if (IsBattle06Active())
            {
                return null;
            }

            if (!includeDelayedBoss)
            {
                return null;
            }

            if (IsBattle05Active())
            {
                return GetBattle05BossConfig();
            }

            if (IsBattle03Active())
            {
                return GetBattle03BossConfig();
            }

            return defaultBoss;
        }

        private BossConfig GetBattle03BossConfig()
        {
            if (_battle03BossConfig != null)
            {
                return _battle03BossConfig;
            }

            _battle03BossConfig = ScriptableObject.CreateInstance<BossConfig>();
            _battle03BossConfig.id = "battle03_drunk_ogre_boss";
            _battle03BossConfig.displayName = "酒鬼食人魔";
            _battle03BossConfig.resourceId = "ogre_boss";
            _battle03BossConfig.mainAttackAction = "attack";
            _battle03BossConfig.moveAction = "walk";
            _battle03BossConfig.deathAction = "death";
            _battle03BossConfig.weaponType = WeaponType.Sword;
            _battle03BossConfig.attackRangeType = AttackRangeType.MeleeShort;
            _battle03BossConfig.attackTempo = AttackTempo.Medium;
            _battle03BossConfig.maxHp = 132;
            _battle03BossConfig.dropWeaponType = WeaponType.Sword;
            _battle03BossConfig.visualScale = 2.9f;
            _battle03BossConfig.yOffset = -0.05f;
            return _battle03BossConfig;
        }

        private BossConfig GetBattle05BossConfig()
        {
            if (_battle05BossConfig != null)
            {
                return _battle05BossConfig;
            }

            _battle05BossConfig = ScriptableObject.CreateInstance<BossConfig>();
            _battle05BossConfig.id = "battle05_giant_key_boss";
            _battle05BossConfig.displayName = "冰骑士";
            _battle05BossConfig.resourceId = "boss_lich";
            _battle05BossConfig.mainAttackAction = "attack";
            _battle05BossConfig.specialAttackAction = "cast";
            _battle05BossConfig.normalAttacksBetweenSpecial = 2;
            _battle05BossConfig.moveAction = "walk";
            _battle05BossConfig.deathAction = "death";
            _battle05BossConfig.weaponType = WeaponType.Sword;
            _battle05BossConfig.attackRangeType = AttackRangeType.MeleeShort;
            _battle05BossConfig.attackTempo = AttackTempo.Slow;
            _battle05BossConfig.maxHp = 176;
            _battle05BossConfig.dropWeaponType = WeaponType.Sword;
            _battle05BossConfig.visualScale = 1.75f;
            _battle05BossConfig.yOffset = -0.35f;
            return _battle05BossConfig;
        }

        private GameObject FindCharacterPrefab(CharacterConfig config)
        {
            for (var i = 0; i < characterBindings.Count; i++)
            {
                var binding = characterBindings[i];
                if (binding != null && binding.config == config)
                {
                    return binding.prefab;
                }
            }

            return null;
        }

        private GameObject FindBossPrefab(BossConfig config)
        {
            for (var i = 0; i < bossBindings.Count; i++)
            {
                var binding = bossBindings[i];
                if (binding != null && binding.config == config)
                {
                    return binding.prefab;
                }
            }

            return null;
        }

        private static GameObject CreateFallbackUnit(string objectName, Vector3 position, Transform parent)
        {
            var go = new GameObject(objectName);
            go.transform.SetParent(parent);
            go.transform.position = position;
            go.AddComponent<SpriteRenderer>().sortingOrder = 5;
            go.AddComponent<PixelUnitAnimator>();
            return go;
        }

        private static CombatUnitController EnsureCombatUnit(GameObject target)
        {
            var controller = target.GetComponent<CombatUnitController>();
            if (controller == null)
            {
                controller = target.AddComponent<CombatUnitController>();
            }

            if (target.GetComponent<SpriteRenderer>() == null)
            {
                target.AddComponent<SpriteRenderer>().sortingOrder = 5;
            }

            if (target.GetComponent<PixelUnitAnimator>() == null)
            {
                target.AddComponent<PixelUnitAnimator>();
            }

            return controller;
        }
    }
}
