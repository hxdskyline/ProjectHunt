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
            var isBattle02 = IsBattle02Active();
            Debug.Log($"[BattleFormation] isBattle02={isBattle02}, spawning players at: front={sceneReferences.playerFrontPoint.position}, mid={sceneReferences.playerMidPoint.position}, back={sceneReferences.playerBackPoint.position}");
            SpawnCharacter(ResolveBattleCharacter(formation.frontCharacter, isBattle02), sceneReferences.playerFrontPoint, sceneReferences.playerTeamRoot);
            SpawnCharacter(ResolveBattleCharacter(formation.midCharacter, isBattle02), sceneReferences.playerMidPoint, sceneReferences.playerTeamRoot);
            SpawnCharacter(ResolveBattleCharacter(formation.backCharacter, isBattle02), sceneReferences.playerBackPoint, sceneReferences.playerTeamRoot);
            if (!isBattle02)
            {
                SpawnBoss(formation.boss, sceneReferences.bossPoint, sceneReferences.bossRoot);
            }
            else
            {
                Debug.Log("[BattleFormation] Battle02: skipped boss spawn.");
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

        private void SpawnBoss(BossConfig config, Transform spawnPoint, Transform parentRoot)
        {
            if (config == null || spawnPoint == null || parentRoot == null)
            {
                return;
            }

            var prefab = FindBossPrefab(config);
            _spawnedBoss = prefab != null
                ? Instantiate(prefab, spawnPoint.position, Quaternion.identity, parentRoot)
                : CreateFallbackUnit($"{config.id}_Fallback", spawnPoint.position, parentRoot);

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

        private CharacterConfig ResolveBattleCharacter(CharacterConfig baseConfig, bool isBattle02)
        {
            if (baseConfig == null || !isBattle02 || gameContext == null)
            {
                return baseConfig;
            }

            var selectedBase = gameContext.buildSelection.selectedCharacter;
            var selectedHammer = gameContext.buildSelection.selectedHammerCharacter;
            if (selectedBase == null || selectedHammer == null)
            {
                return baseConfig;
            }

            var isSelectedBase = baseConfig == selectedBase ||
                                 (!string.IsNullOrWhiteSpace(baseConfig.id) &&
                                  baseConfig.id == selectedBase.id);
            return isSelectedBase ? selectedHammer : baseConfig;
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
