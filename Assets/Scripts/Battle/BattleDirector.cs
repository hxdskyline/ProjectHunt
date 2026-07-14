using System.Collections;
using System.Collections.Generic;
using ProjectHunt.Data;
using ProjectHunt.Flow;
using ProjectHunt.UI;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectHunt.Battle
{
    public sealed class BattleDirector : MonoBehaviour
    {
        public sealed class AttackTargetSnapshot
        {
            public CombatUnitController target;
            public Vector3 worldPosition;
        }

        [Header("Core")]
        public DemoGameContext gameContext;
        public DemoFlowController flowController;
        public BattleSceneReferences sceneReferences;
        public BattleFormationSpawner formationSpawner;

        [Header("UI")]
        public BossHpBarView bossHpBarView;
        public Text dropHintText;

        [Header("Drop")]
        public GameObject dropPrefab;

        [Header("Combat Tuning")]
        public int bossDamage = 8;
        public int fastPlayerDamage = 4;
        public int mediumPlayerDamage = 6;
        public int slowPlayerDamage = 5;
        public int meteorHammerDamage = 10;
        public float bossDeathPause = 2.0f;
        public float playerEntryOffsetX = 4.0f;
        public float bossEntryOffsetX = 4.5f;
        public float entryMoveSpeed = 4.0f;
        public float arrowFlightDuration = 0.5f;
        public float arrowArcHeight = 1.15f;
        public float meteorHammerArcherArcHeight = 1.85f;
        public float assassinBladeFlightDuration = 0.32f;
        public float assassinBladeArcHeight = 0.12f;
        public float hitSparkDuration = 0.18f;
        public Vector3 meteorHammerDropStart = new Vector3(5.83f, -1.57f, 0f);
        public Vector3 meteorHammerDropTarget = new Vector3(3.25f, -2.04f, 0f);
        public float meteorHammerDropDuration = 0.8f;
        public float meteorHammerDropArcHeight = 1.35f;
        public float meteorHammerDropSpinSpeed = 720f;

        [Header("Burning Village Waves")]
        public int waveEnemyDamage = 1;
        public int waveEnemyHp = 16;
        public float waveSpawnDelay = 0.75f;
        public float waveEntryOffsetX = 4.8f;
        public Vector3[] waveEnemySlots = new[]
        {
            new Vector3(2.4f, -1.18f, 0f),
            new Vector3(3.3f, -1.5f, 0f),
            new Vector3(4.2f, -1.82f, 0f),
        };
        private readonly List<CombatUnitController> _players = new List<CombatUnitController>();
        private readonly List<CombatUnitController> _enemies = new List<CombatUnitController>();
        private CombatUnitController _boss;
        private BossConfig _waveEnemyConfig;
        private Transform _waveEnemyRoot;
        private int _currentWaveIndex;
        private bool _battle02CombatStarted;
        public bool IsBattleResolved { get; private set; }
        public bool IsBattle02 =>
            (flowController != null && flowController.IsBattle02()) ||
            (gameContext != null &&
             (gameContext.runState.phase == GamePhase.Battle02 || gameContext.runState.isBattle02Started));
        public bool IsBattle03 =>
            (flowController != null && flowController.IsBattle03()) ||
            (gameContext != null &&
             (gameContext.runState.phase == GamePhase.Battle03 || gameContext.runState.isBattle03Started));
        public bool IsBlacksmithValidation =>
            (flowController != null && flowController.IsBlacksmithValidation()) ||
            (gameContext != null &&
             (gameContext.runState.phase == GamePhase.BlacksmithValidation ||
              gameContext.runState.isBlacksmithValidationStarted));
        public bool IsBattle04 =>
            (flowController != null && flowController.IsBattle04()) ||
            (gameContext != null &&
             (gameContext.runState.phase == GamePhase.Battle04 || gameContext.runState.isBattle04Started));
        public bool IsBattle05 =>
            (flowController != null && flowController.IsBattle05()) ||
            (gameContext != null &&
             (gameContext.runState.phase == GamePhase.Battle05 || gameContext.runState.isBattle05Started));
        public bool IsBattle06 =>
            (flowController != null && flowController.IsBattle06()) ||
            (gameContext != null &&
             (gameContext.runState.phase == GamePhase.Battle06 || gameContext.runState.isBattle06Started));
        public CharacterConfig CurrentMeteorHammerUser =>
            gameContext != null && gameContext.buildSelection.isSelectionConfirmed
                ? gameContext.buildSelection.selectedCharacter
                : null;

        private void Start()
        {
            if (gameContext == null || gameContext.gameObject == null)
            {
                gameContext = DemoGameContext.Instance;
            }

            if (flowController == null || flowController.gameObject == null)
            {
                flowController = DemoFlowController.Instance;
            }
            if (flowController == null)
            {
                flowController = FindObjectOfType<DemoFlowController>();
            }

            if (formationSpawner == null)
            {
                formationSpawner = FindObjectOfType<BattleFormationSpawner>();
            }

            if (dropHintText != null)
            {
                dropHintText.gameObject.SetActive(false);
                var rect = dropHintText.rectTransform;
                rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, 178f);
            }

            StartCoroutine(BootstrapBattle());
        }

        private IEnumerator BootstrapBattle()
        {
            yield return null;

            if (formationSpawner == null)
            {
                yield break;
            }

            Debug.Log($"[BattleDirector] phase={gameContext?.runState.phase}, IsBattle02={IsBattle02}, IsBlacksmithValidation={IsBlacksmithValidation}, IsBattle03={IsBattle03}, IsBattle04={IsBattle04}, IsBattle05={IsBattle05}, IsBattle06={IsBattle06}");
            formationSpawner.SpawnCurrentBattle();
            CollectUnits();
            yield return StartCoroutine(PlayBattleEntrance());

            if (IsBattle02 || IsBlacksmithValidation || IsBattle04 || IsBattle06)
            {
                if (bossHpBarView != null)
                {
                    bossHpBarView.gameObject.SetActive(false);
                }

                _battle02CombatStarted = false;
                StartCoroutine(RunWaveStage(IsBattle04 ? 2 : IsBlacksmithValidation ? 2 : 3));
                yield break;
            }

            Debug.Log($"[BattleDirector] Entering phase {gameContext?.runState.phase}.");
            StartCombat();
        }

        public AttackTargetSnapshot CaptureAttackTarget(CombatUnitController attacker)
        {
            if (attacker == null)
            {
                return null;
            }

            if (attacker.team == CombatUnitController.TeamType.Player)
            {
                var target = _boss != null && _boss.IsAlive ? _boss : GetFirstAliveEnemy();
                if (target == null)
                {
                    return null;
                }

                return new AttackTargetSnapshot
                {
                    target = target,
                    worldPosition = target.transform.position,
                };
            }

            return null;
        }

        public void ResolveAttack(
            CombatUnitController attacker,
            int attackImpactIndex = 0,
            AttackTargetSnapshot targetSnapshot = null,
            string attackAction = null)
        {
            if (attacker == null || !attacker.IsAlive || IsBattleResolved)
            {
                return;
            }

            if (attacker.team == CombatUnitController.TeamType.Player)
            {
                var lockedTarget = targetSnapshot != null ? targetSnapshot.target : null;
                var currentTarget = _boss != null && _boss.IsAlive ? _boss : GetFirstAliveEnemy();
                var resolvedTarget = ShouldUseProjectile(attacker)
                    ? lockedTarget != null ? lockedTarget : currentTarget
                    : currentTarget;

                if (resolvedTarget != null)
                {
                    attacker.PlayAttackFeedback();

                    if (ShouldUseProjectile(attacker))
                    {
                        var fallbackPosition = targetSnapshot != null
                            ? targetSnapshot.worldPosition
                            : resolvedTarget.transform.position;
                        SpawnProjectile(attacker, resolvedTarget, fallbackPosition, GetDamage(attacker), attackImpactIndex);
                    }
                    else
                    {
                        if (!resolvedTarget.IsAlive)
                        {
                            return;
                        }

                        SpawnHitSpark(resolvedTarget.transform.position + new Vector3(0f, 0.45f, 0f), 0.6f);
                        resolvedTarget.ApplyDamage(GetDamage(attacker));

                        if (ShouldApplyAssassinStun(attacker))
                        {
                            resolvedTarget.TryApplyStun(MeteorHammerRules.AssassinStunDuration);
                        }
                    }
                }
            }
            else
            {
                attacker.PlayAttackFeedback();
                Debug.Log($"[ResolveAttack] enemy={attacker.gameObject.name} attacking {_players.Count} players.");

                for (var i = 0; i < _players.Count; i++)
                {
                    var player = _players[i];
                    if (player != null && player.IsAlive)
                    {
                        if (ShouldBlockBossDamage(player))
                        {
                            SpawnHitSpark(player.transform.position + new Vector3(0f, 0.3f, 0f), 0.42f);
                            player.ShowStatusText("\u683c\u6321", new Color(1f, 0.95f, 0.6f, 1f));
                            continue;
                        }

                        SpawnHitSpark(player.transform.position + new Vector3(0f, 0.3f, 0f), 0.52f);
                        var isLichFreeze = attacker.bossConfig != null &&
                                           attacker.bossConfig.resourceId == "boss_lich" &&
                                           attackAction == "cast";
                        // Wave enemies are verification pressure only; their damage is always fixed.
                        var dmg = _enemies.Contains(attacker)
                            ? waveEnemyDamage
                            : GetBossDamageAgainst(player);
                        if (isLichFreeze)
                        {
                            dmg = Mathf.CeilToInt(dmg * 1.25f);
                            player.TryApplyStun(0.7f);
                            player.ShowStatusText("冻结", new Color(0.55f, 0.84f, 1f, 1f));
                        }
                        Debug.Log($"[ResolveAttack] enemy hits {player.gameObject.name} for {dmg} (hp {player.currentHp}/{player.maxHp})");
                        player.ApplyDamage(dmg);
                    }
                }
            }
        }

        public void NotifyHpChanged(CombatUnitController unit)
        {
            if (unit == _boss && bossHpBarView != null)
            {
                bossHpBarView.SetValue(_boss.currentHp, _boss.GetMaxHp());
            }
        }

        public void NotifyUnitDied(CombatUnitController unit)
        {
            if (unit != null && _enemies.Contains(unit))
            {
                StartCoroutine(HandleWaveEnemyDeath(unit));
                return;
            }

            if (unit == _boss)
            {
                StartCoroutine(HandleBossDeath());
            }
        }

        private void CollectUnits()
        {
            _players.Clear();
            _boss = null;
            _enemies.Clear();

            if (sceneReferences?.playerTeamRoot != null)
            {
                _players.AddRange(sceneReferences.playerTeamRoot.GetComponentsInChildren<CombatUnitController>());
            }

            if (sceneReferences?.bossRoot != null)
            {
                _boss = sceneReferences.bossRoot.GetComponentInChildren<CombatUnitController>();
            }

            if (_boss != null && bossHpBarView != null)
            {
                var name = _boss.bossConfig != null ? _boss.bossConfig.displayName : "Boss";
                bossHpBarView.SetBossName(name);
                bossHpBarView.SetValue(_boss.currentHp, _boss.GetMaxHp());
            }
        }

        private void StartCombat()
        {
            IsBattleResolved = false;

            for (var i = 0; i < _players.Count; i++)
            {
                if (_players[i] != null)
                {
                    _players[i].BeginCombat();
                }
            }

            if (_boss != null)
            {
                _boss.BeginCombat();
            }
        }

        private void StartPlayerCombat()
        {
            IsBattleResolved = false;
            for (var i = 0; i < _players.Count; i++)
            {
                if (_players[i] != null)
                {
                    _players[i].BeginCombat();
                }
            }
        }

        private IEnumerator RunWaveStage(int waveCount)
        {
            _currentWaveIndex = 0;
            while (_currentWaveIndex < waveCount && !IsBattleResolved)
            {
                yield return StartCoroutine(SpawnWave(_currentWaveIndex));

                while (GetFirstAliveEnemy() != null && !IsBattleResolved)
                {
                    yield return null;
                }

                _currentWaveIndex++;
                yield return new WaitForSeconds(0.5f);
            }

            if (IsBattleResolved)
            {
                yield break;
            }

            IsBattleResolved = true;
            for (var i = 0; i < _players.Count; i++)
            {
                _players[i]?.StopCombat();
            }

            if (IsBattle04)
            {
                if (gameContext != null && gameContext.runState.isMageValidationStarted)
                {
                    flowController?.CompleteBattle04();
                }
                else
                {
                    MageRecruitmentPresentation.Show(flowController, gameContext);
                }
                yield break;
            }

            if (IsBlacksmithValidation)
            {
                yield return new WaitForSeconds(0.65f);
                flowController?.CompleteBlacksmithValidation();
                yield break;
            }

            if (IsBattle06)
            {
                yield return new WaitForSeconds(0.8f);
                flowController?.CompleteBattle06();
                yield break;
            }

            if (IsBattle02)
            {
                BlacksmithRescuePresentation.Show(flowController);
                yield break;
            }

            yield return new WaitForSeconds(0.65f);
            flowController?.CompleteBattle02();
        }

        private IEnumerator SpawnWave(int waveIndex)
        {
            _enemies.Clear();
            EnsureWaveEnemyConfig(waveIndex);
            EnsureWaveEnemyRoot();

            for (var i = 0; i < waveEnemySlots.Length; i++)
            {
                var targetPosition = waveEnemySlots[i];
                var spawnPosition = targetPosition + Vector3.right * waveEntryOffsetX;
                var enemy = CreateWaveEnemy(spawnPosition, waveIndex, i);
                if (enemy != null)
                {
                    _enemies.Add(enemy);
                }
            }

            // Start the team as soon as the wave exists, so incoming enemies can be hit.
            if (!_battle02CombatStarted)
            {
                StartPlayerCombat();
                _battle02CombatStarted = true;
            }

            for (var i = 0; i < _enemies.Count; i++)
            {
                if (_enemies[i] != null)
                {
                    StartCoroutine(MoveWaveEnemyIntoCombat(_enemies[i], waveEnemySlots[i]));
                }
            }

            yield return new WaitForSeconds(waveSpawnDelay);
        }

        private IEnumerator MoveWaveEnemyIntoCombat(CombatUnitController enemy, Vector3 targetPosition)
        {
            if (enemy == null)
            {
                yield break;
            }

            yield return StartCoroutine(enemy.MoveToPosition(targetPosition, entryMoveSpeed));
            if (!IsBattleResolved && enemy != null && enemy.IsAlive)
            {
                enemy.BeginCombat();
            }
        }

        private CombatUnitController CreateWaveEnemy(Vector3 spawnPosition, int waveIndex, int enemyIndex)
        {
            var root = _waveEnemyRoot != null ? _waveEnemyRoot : transform;
            var go = new GameObject($"BurningMonster_{waveIndex + 1}_{enemyIndex + 1}");
            go.transform.SetParent(root, false);
            go.transform.position = spawnPosition;
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = 5;
            renderer.flipX = true;
            go.AddComponent<PixelUnitAnimator>();
            var controller = go.AddComponent<CombatUnitController>();
            controller.Setup(_waveEnemyConfig, this);
            return controller;
        }

        private void EnsureWaveEnemyConfig(int waveIndex)
        {
            if (_waveEnemyConfig != null && !IsBattle06)
            {
                return;
            }

            _waveEnemyConfig = ScriptableObject.CreateInstance<BossConfig>();
            var iceStage = IsBattle04;
            var finalStage = IsBattle06;
            var finalResourceId = waveIndex == 0 ? "undead_bone_warrior" :
                                  waveIndex == 1 ? "undead_dread_guard" : "skeleton_large";
            _waveEnemyConfig.id = finalStage ? "final_" + finalResourceId : iceStage ? "icefield_snow_golem" : "burning_village_monster";
            _waveEnemyConfig.displayName = finalStage ? "最终验证敌人" : iceStage ? "雪傀儡" : "燃烧小怪";
            _waveEnemyConfig.resourceId = finalStage ? finalResourceId : iceStage ? "snow_golem" : "scarling_bandit";
            _waveEnemyConfig.mainAttackAction = finalStage && waveIndex > 0 ? "attack_1" : "attack";
            _waveEnemyConfig.moveAction = "walk";
            _waveEnemyConfig.deathAction = string.Empty;
            _waveEnemyConfig.weaponType = WeaponType.Sword;
            _waveEnemyConfig.attackRangeType = AttackRangeType.MeleeShort;
            _waveEnemyConfig.attackTempo = AttackTempo.Medium;
            _waveEnemyConfig.maxHp = finalStage ? waveEnemyHp + 10 + waveIndex * 6 : iceStage ? waveEnemyHp + 7 : waveEnemyHp;
            _waveEnemyConfig.dropWeaponType = WeaponType.Sword;
            _waveEnemyConfig.visualScale = finalStage ? 2.35f : iceStage ? 2.5f : 2.4f;
            _waveEnemyConfig.yOffset = 0f;
        }

        private void EnsureWaveEnemyRoot()
        {
            if (_waveEnemyRoot != null)
            {
                return;
            }

            var rootGo = new GameObject("BurningVillageEnemyRoot");
            var parent = sceneReferences != null ? sceneReferences.transform : transform;
            rootGo.transform.SetParent(parent, false);
            _waveEnemyRoot = rootGo.transform;
        }

        private CombatUnitController GetFirstAliveEnemy()
        {
            for (var i = 0; i < _enemies.Count; i++)
            {
                if (_enemies[i] != null && _enemies[i].IsAlive)
                {
                    return _enemies[i];
                }
            }

            return null;
        }

        private IEnumerator HandleWaveEnemyDeath(CombatUnitController unit)
        {
            if (unit == null)
            {
                yield break;
            }

            unit.StopCombat();
            _enemies.Remove(unit);
            yield return new WaitForSeconds(0.08f);
            if (unit != null)
            {
                Destroy(unit.gameObject);
            }
        }

        private IEnumerator HandleBossDeath()
        {
            if (IsBattleResolved)
            {
                yield break;
            }

            IsBattleResolved = true;

            for (var i = 0; i < _players.Count; i++)
            {
                _players[i]?.StopCombat();
            }

            // Boss death animation is already triggered in CombatUnitController.ApplyDamage.
            // Do not stop the boss animator here, or the death clip gets cleared immediately.
            var deathDuration = _boss != null && _boss.bossConfig != null
                ? _boss.GetActionDuration(_boss.bossConfig.deathAction)
                : 0f;
            yield return new WaitForSeconds(Mathf.Max(bossDeathPause, deathDuration));

            if (_boss != null)
            {
                Destroy(_boss.gameObject);
                _boss = null;
            }

            if (IsBattle05)
            {
                flowController?.CompleteBattle05();
                SpawnDrop(RewardType.GiantKey);
                yield break;
            }

            if (IsBattle03)
            {
                flowController?.CompleteBattle03();
                SpawnDrop(RewardType.HolyCup);
                yield break;
            }

            if (IsBattle02)
            {
                flowController?.CompleteBattle02();
                yield break;
            }

            if (IsBattle04)
            {
                flowController?.CompleteBattle04();
                yield break;
            }

            if (IsBattle06)
            {
                flowController?.CompleteBattle06();
                yield break;
            }

            flowController?.CompleteBattle01();
            SpawnDrop(RewardType.MeteorHammer);
        }

        private void SpawnDrop(RewardType rewardType)
        {
            if (sceneReferences == null || sceneReferences.dropRoot == null)
            {
                return;
            }

            GameObject instance;
            if (dropPrefab != null)
            {
                instance = Instantiate(dropPrefab, meteorHammerDropStart, Quaternion.identity, sceneReferences.dropRoot);
            }
            else
            {
                instance = CreateFallbackDrop(sceneReferences.dropRoot, meteorHammerDropStart, rewardType);
            }

            var claim = instance.GetComponent<DropClaimController>();
            if (claim == null)
            {
                claim = instance.AddComponent<DropClaimController>();
            }

            claim.flowController = flowController;
            claim.rewardType = rewardType;
            claim.RebuildPresentation();
            EnsureDropClickable(instance);
            claim.SetInteractable(false);

            StartCoroutine(AnimateDrop(instance.transform, claim, rewardType));

            if (dropHintText != null)
            {
                dropHintText.gameObject.SetActive(false);
            }
        }

        private IEnumerator PlayBattleEntrance()
        {
            var targetPositions = new List<Vector3>(_players.Count);
            for (var i = 0; i < _players.Count; i++)
            {
                if (_players[i] == null)
                {
                    targetPositions.Add(Vector3.zero);
                    continue;
                }

                var target = _players[i].transform.position;
                targetPositions.Add(target);
                _players[i].transform.position = target + Vector3.left * playerEntryOffsetX;
            }

            Vector3 bossTargetPosition = Vector3.zero;
            if (_boss != null)
            {
                bossTargetPosition = _boss.transform.position;
                _boss.transform.position = bossTargetPosition + Vector3.right * bossEntryOffsetX;
            }

            for (var i = 0; i < _players.Count; i++)
            {
                if (_players[i] != null)
                {
                    StartCoroutine(_players[i].MoveToPosition(targetPositions[i], entryMoveSpeed));
                }
            }

            if (_boss != null)
            {
                StartCoroutine(_boss.MoveToPosition(bossTargetPosition, entryMoveSpeed));
            }

            var moving = true;
            while (moving)
            {
                moving = false;

                for (var i = 0; i < _players.Count; i++)
                {
                    if (_players[i] != null && Vector3.Distance(_players[i].transform.position, targetPositions[i]) > 0.02f)
                    {
                        moving = true;
                        break;
                    }
                }

                if (!moving && _boss != null && Vector3.Distance(_boss.transform.position, bossTargetPosition) > 0.02f)
                {
                    moving = true;
                }

                yield return null;
            }

            yield return new WaitForSeconds(0.1f);
        }

        private GameObject CreateFallbackDrop(Transform parent, Vector3 position, RewardType rewardType)
        {
            var go = new GameObject(rewardType switch
            {
                RewardType.HolyCup => "HolyCupDrop",
                RewardType.GiantKey => "GiantKeyDrop",
                _ => "MeteorHammerDrop",
            });
            go.transform.SetParent(parent);
            go.transform.position = position;
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = rewardType switch
            {
                RewardType.HolyCup => SimpleSpriteFactory.GetHolyCupSprite(),
                RewardType.GiantKey => SimpleSpriteFactory.GetGiantKeySprite(),
                _ => SimpleSpriteFactory.GetMeteorHammerSprite(),
            };
            renderer.sortingOrder = 10;
            go.transform.localScale = Vector3.one * 1.5f;
            return go;
        }

        private void SpawnProjectile(CombatUnitController attacker, CombatUnitController target, Vector3 fallbackTargetWorldPosition, int damage, int attackImpactIndex)
        {
            if (attacker == null)
            {
                return;
            }

            var projectileRoot = sceneReferences != null ? sceneReferences.projectileRoot : null;
            if (projectileRoot == null)
            {
                var fallbackRoot = new GameObject("ProjectileRoot");
                projectileRoot = fallbackRoot.transform;
                if (sceneReferences != null)
                {
                    sceneReferences.projectileRoot = projectileRoot;
                }
            }

            var isMeteorHammerArcher = IsMeteorHammerArcher(attacker);
            var isHolyCupCatapult = IsHolyCupCatapult(attacker);
            var isAssassinHammerProjectile = ShouldUseAssassinHammerProjectile(attacker, attackImpactIndex);
            var isMeteorHammerAssassin = IsMeteorHammerAssassin(attacker);
            var isGiantKeyProjectile = IsGiantKeyProjectile(attacker);
            var isAssassinBlade = (IsAssassinProjectile(attacker) || isMeteorHammerAssassin) &&
                                  !isAssassinHammerProjectile;
            var go = new GameObject(
                isMeteorHammerArcher || isAssassinHammerProjectile ? "MeteorHammerProjectile" :
                isHolyCupCatapult ? "CatapultStoneProjectile" :
                isGiantKeyProjectile ? "GiantKeyProjectile" :
                isAssassinBlade ? "AssassinBladeProjectile" :
                "ArrowProjectile");
            go.transform.SetParent(projectileRoot, false);
            var renderer = go.AddComponent<SpriteRenderer>();
            var controller = go.AddComponent<ArrowProjectileController>();
            controller.spriteRenderer = renderer;

            var sprite = isMeteorHammerArcher || isAssassinHammerProjectile
                ? SimpleSpriteFactory.GetMeteorHammerSprite()
                : isHolyCupCatapult
                    ? PixelAnimationLibrary.GetFirstFrameSprite("catapult_ball", "idle")
                : isGiantKeyProjectile
                    ? SimpleSpriteFactory.GetGiantKeySprite()
                : isAssassinBlade
                    ? ExternalSpriteLibrary.GetLongbowArrowSprite()
                    : ExternalSpriteLibrary.GetLongbowArrowSprite();
            if (sprite == null)
            {
                sprite = SimpleSpriteFactory.GetWhitePixelSprite();
                go.transform.localScale = new Vector3(0.35f, 0.08f, 1f);
            }
            else if (isMeteorHammerArcher)
            {
                go.transform.localScale = Vector3.one * 0.75f;
            }
            else if (isHolyCupCatapult)
            {
                go.transform.localScale = Vector3.one * 1.55f;
            }
            else if (isAssassinHammerProjectile)
            {
                go.transform.localScale = Vector3.one * 0.6f;
            }
            else if (isAssassinBlade)
            {
                go.transform.localScale = Vector3.one * 0.7f;
            }
            else if (isGiantKeyProjectile)
            {
                go.transform.localScale = Vector3.one * 0.82f;
            }
            else
            {
                go.transform.localScale = Vector3.one * 0.9f;
            }

            controller.duration = isAssassinBlade ? assassinBladeFlightDuration : isHolyCupCatapult ? arrowFlightDuration + 0.18f : arrowFlightDuration;
            controller.arcHeight = isAssassinBlade
                ? 0f
                : isMeteorHammerArcher
                    ? meteorHammerArcherArcHeight
                    : isHolyCupCatapult
                        ? meteorHammerArcherArcHeight + 0.55f
                    : isGiantKeyProjectile && attacker.characterConfig.roleType == RoleType.Archer
                        ? meteorHammerArcherArcHeight + 0.35f
                    : arrowArcHeight;
            controller.isMeteorHammerProjectile = isMeteorHammerArcher || isHolyCupCatapult;
            controller.areaImpactRadius = isMeteorHammerArcher
                ? MeteorHammerRules.ArcherImpactRadius
                : isHolyCupCatapult ? 1.55f : 0f;
            controller.alignToVelocity = !isAssassinBlade && !isAssassinHammerProjectile && !isGiantKeyProjectile;
            controller.spinSpeed = isMeteorHammerArcher || isAssassinHammerProjectile || isGiantKeyProjectile ? -720f : 0f;
            controller.returnToSender = isGiantKeyProjectile && attacker.characterConfig.roleType == RoleType.Assassin;
            controller.animationFrames = isAssassinBlade
                ? ExternalSpriteLibrary.GetBallistaArrowFrames()
                : isHolyCupCatapult ? PixelAnimationLibrary.GetClip("catapult_ball", "idle")?.frames : null;
            controller.animationFps = isAssassinBlade || isHolyCupCatapult ? 12f : 0f;
            controller.staticEulerAngles = isAssassinBlade ? new Vector3(0f, 180f, 0f) : Vector3.zero;

            var launchPosition = attacker.GetProjectileLaunchPosition();
            var targetBasePosition = target != null && target.IsAlive
                ? target.transform.position
                : fallbackTargetWorldPosition;
            var targetPosition = targetBasePosition + new Vector3(0f, isAssassinBlade || isAssassinHammerProjectile ? 0.2f : 0.35f, 0f);
            if (isAssassinBlade || isAssassinHammerProjectile ||
                (isGiantKeyProjectile && attacker.characterConfig.roleType == RoleType.Assassin))
            {
                launchPosition = GetAssassinLaunchPosition(launchPosition, attackImpactIndex);
                targetPosition.y = launchPosition.y;
            }

            controller.Launch(
                launchPosition,
                targetPosition,
                sprite,
                damage,
                target,
                this);
        }

        private static Vector3 GetAssassinLaunchPosition(Vector3 defaultLaunchPosition, int attackImpactIndex)
        {
            if (attackImpactIndex <= 0)
            {
                defaultLaunchPosition.y = -1.566f;
                return defaultLaunchPosition;
            }

            defaultLaunchPosition.x = -1.5f;
            defaultLaunchPosition.y = -1.579f;
            return defaultLaunchPosition;
        }

        private IEnumerator AnimateDrop(Transform dropTransform, DropClaimController claimController, RewardType rewardType)
        {
            var elapsed = 0f;
            var start = meteorHammerDropStart;
            var end = meteorHammerDropTarget;

            while (elapsed < meteorHammerDropDuration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, meteorHammerDropDuration));
                var position = Vector3.Lerp(start, end, t);
                position.y += Mathf.Sin(t * Mathf.PI) * meteorHammerDropArcHeight;
                dropTransform.position = position;
                dropTransform.rotation = Quaternion.Euler(0f, 0f, meteorHammerDropSpinSpeed * elapsed);
                yield return null;
            }

            dropTransform.position = end;
            dropTransform.rotation = Quaternion.identity;
            Debug.Log($"[Drop] {rewardType} landed at {end} and is now interactable.");

            if (rewardType == RewardType.HolyCup)
            {
                // The blacksmith inspects the cup before it enters the familiar claim flow.
                HolyCupInspectionPresentation.Play(claimController);
                yield break;
            }

            claimController.SetInteractable(true);

            if (dropHintText != null)
            {
                dropHintText.gameObject.SetActive(true);
                dropHintText.text = rewardType switch
                {
                    RewardType.HolyCup => "\u70b9\u51fb\u62fe\u53d6\u201c\u9152\u795e\u5723\u676f\u201d",
                    RewardType.GiantKey => "\u70b9\u51fb\u62fe\u53d6\u201c\u5de8\u4eba\u94a5\u5319\u201d",
                    _ => "\u70b9\u51fb\u62fe\u53d6\u201c\u54e5\u5e03\u6797\u6d41\u661f\u9524\u201d",
                };
                var rect = dropHintText.rectTransform;
                rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, 178f);
            }
        }

        private static void EnsureDropClickable(GameObject dropObject)
        {
            if (dropObject == null)
            {
                return;
            }

            var collider2D = dropObject.GetComponent<Collider2D>();
            if (collider2D == null)
            {
                var box = dropObject.AddComponent<BoxCollider2D>();
                box.isTrigger = false;

                var spriteRenderer = dropObject.GetComponent<SpriteRenderer>();
                if (spriteRenderer != null && spriteRenderer.sprite != null)
                {
                    box.size = spriteRenderer.sprite.bounds.size;
                    box.offset = spriteRenderer.sprite.bounds.center;
                }
                else
                {
                    box.size = new Vector2(1.2f, 1.2f);
                }
            }

            Debug.Log($"[Drop] Clickable collider ready on {dropObject.name}.");
        }

        public void SpawnHitSpark(Vector3 worldPosition, float scale = 0.6f)
        {
            StartCoroutine(HitSparkRoutine(worldPosition, scale));
        }

        public void ApplyPlayerAreaDamage(Vector3 center, float radius, int damage)
        {
            if (_boss != null && _boss.IsAlive && Vector3.Distance(_boss.transform.position, center) <= radius)
            {
                SpawnHitSpark(_boss.transform.position + new Vector3(0f, 0.38f, 0f), 0.9f);
                _boss.ApplyDamage(damage);
            }

            for (var i = _enemies.Count - 1; i >= 0; i--)
            {
                var enemy = _enemies[i];
                if (enemy != null && enemy.IsAlive && Vector3.Distance(enemy.transform.position, center) <= radius)
                {
                    SpawnHitSpark(enemy.transform.position + new Vector3(0f, 0.38f, 0f), 0.72f);
                    enemy.ApplyDamage(damage);
                }
            }
        }

        private IEnumerator HitSparkRoutine(Vector3 worldPosition, float scale)
        {
            var root = sceneReferences != null && sceneReferences.projectileRoot != null
                ? sceneReferences.projectileRoot
                : transform;
            var spark = new GameObject("HitSpark");
            spark.transform.SetParent(root, false);
            spark.transform.position = worldPosition;

            var renderer = spark.AddComponent<SpriteRenderer>();
            renderer.sprite = SimpleSpriteFactory.GetHitSparkSprite();
            renderer.sortingOrder = 25;
            renderer.color = Color.white;

            var startScale = Vector3.one * Mathf.Max(0.01f, scale);
            var endScale = startScale * 1.75f;
            var elapsed = 0f;

            while (elapsed < hitSparkDuration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, hitSparkDuration));
                spark.transform.localScale = Vector3.Lerp(startScale, endScale, t);
                spark.transform.rotation = Quaternion.Euler(0f, 0f, t * 90f);

                var color = renderer.color;
                color.a = 1f - t;
                renderer.color = color;
                yield return null;
            }

            Destroy(spark);
        }

        private int GetDamage(CombatUnitController attacker)
        {
            if (attacker.bossConfig != null)
            {
                return bossDamage;
            }

            if (IsMeteorHammerArcher(attacker))
            {
                return slowPlayerDamage + MeteorHammerRules.ArcherAreaDamageBonus;
            }

            if (IsHolyCupCatapult(attacker))
            {
                return slowPlayerDamage + 3;
            }

            if (IsMeteorHammerSwordsman(attacker))
            {
                return mediumPlayerDamage + Mathf.RoundToInt(MeteorHammerRules.SwordsmanBonusDamage);
            }

            switch (attacker.characterConfig.attackTempo)
            {
                case AttackTempo.Fast:
                    return fastPlayerDamage;
                case AttackTempo.Slow:
                    return slowPlayerDamage;
                default:
                    return mediumPlayerDamage;
            }
        }

        private bool ShouldUseProjectile(CombatUnitController attacker)
        {
            return IsMeteorHammerArcher(attacker) ||
                   IsMeteorHammerAssassin(attacker) ||
                   IsHolyCupCatapult(attacker) ||
                   IsGiantKeyProjectile(attacker) ||
                   IsNormalArcherProjectile(attacker) ||
                   IsAssassinProjectile(attacker);
        }

        private int GetBossDamageAgainst(CombatUnitController target)
        {
            if (target == null || target.characterConfig == null)
            {
                return bossDamage;
            }

            float modifier;
            switch (target.characterConfig.roleType)
            {
                case RoleType.Assassin:
                    modifier = 0.5f;
                    break;
                case RoleType.Archer:
                    modifier = 0.1f;
                    break;
                default:
                    modifier = 1.0f;
                    break;
            }

            return Mathf.Max(1, Mathf.RoundToInt(bossDamage * modifier));
        }

        private bool ShouldApplyAssassinStun(CombatUnitController attacker)
        {
            return attacker != null &&
                   attacker.UsesMeteorHammerOverride &&
                   attacker.characterConfig != null &&
                   MeteorHammerRules.IsMeteorHammerAssassin(attacker.characterConfig) &&
                   Random.value <= MeteorHammerRules.AssassinStunChance;
        }

        private bool ShouldBlockBossDamage(CombatUnitController target)
        {
            return target != null &&
                   target.UsesMeteorHammerOverride &&
                   target.characterConfig != null &&
                   MeteorHammerRules.IsMeteorHammerSwordsman(target.characterConfig) &&
                   Random.value <= MeteorHammerRules.SwordsmanBlockChance;
        }

        private bool IsMeteorHammerArcher(CombatUnitController attacker)
        {
            return attacker != null &&
                   attacker.UsesMeteorHammerOverride &&
                   attacker.characterConfig != null &&
                   MeteorHammerRules.IsMeteorHammerArcher(attacker.characterConfig);
        }

        private static bool IsNormalArcherProjectile(CombatUnitController attacker)
        {
            return attacker != null &&
                   attacker.team == CombatUnitController.TeamType.Player &&
                   attacker.characterConfig != null &&
                   attacker.characterConfig.roleType == RoleType.Archer &&
                   !attacker.UsesMeteorHammerOverride;
        }

        private static bool IsAssassinProjectile(CombatUnitController attacker)
        {
            return attacker != null &&
                   attacker.team == CombatUnitController.TeamType.Player &&
                   attacker.characterConfig != null &&
                   attacker.characterConfig.roleType == RoleType.Assassin &&
                   !attacker.UsesMeteorHammerOverride;
        }

        private bool IsMeteorHammerSwordsman(CombatUnitController attacker)
        {
            return attacker != null &&
                   attacker.UsesMeteorHammerOverride &&
                   attacker.characterConfig != null &&
                   MeteorHammerRules.IsMeteorHammerSwordsman(attacker.characterConfig);
        }

        private static bool IsHolyCupCatapult(CombatUnitController attacker)
        {
            return attacker != null &&
                   attacker.team == CombatUnitController.TeamType.Player &&
                   attacker.characterConfig != null &&
                   attacker.characterConfig.roleType == RoleType.Archer &&
                   attacker.characterConfig.resourceId == "catapult";
        }

        private static bool IsGiantKeyProjectile(CombatUnitController attacker)
        {
            return attacker != null && attacker.team == CombatUnitController.TeamType.Player &&
                   attacker.characterConfig != null &&
                   (attacker.characterConfig.roleType == RoleType.Archer ||
                    attacker.characterConfig.roleType == RoleType.Assassin ||
                    attacker.characterConfig.roleType == RoleType.Mage) &&
                   !string.IsNullOrWhiteSpace(attacker.characterConfig.id) &&
                   attacker.characterConfig.id.EndsWith("_key");
        }

        private bool IsMeteorHammerAssassin(CombatUnitController attacker)
        {
            return attacker != null &&
                   attacker.UsesMeteorHammerOverride &&
                   attacker.characterConfig != null &&
                   MeteorHammerRules.IsMeteorHammerAssassin(attacker.characterConfig);
        }

        private bool ShouldUseAssassinHammerProjectile(CombatUnitController attacker, int attackImpactIndex)
        {
            return IsMeteorHammerAssassin(attacker) && attackImpactIndex > 0;
        }
    }
}

