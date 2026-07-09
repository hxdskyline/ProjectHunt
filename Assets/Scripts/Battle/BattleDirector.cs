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

        private readonly List<CombatUnitController> _players = new List<CombatUnitController>();
        private CombatUnitController _boss;

        public bool IsBattleResolved { get; private set; }
        public bool IsBattle02 => flowController != null && flowController.IsBattle02();
        public CharacterConfig CurrentMeteorHammerUser =>
            gameContext != null && gameContext.buildSelection.isSelectionConfirmed
                ? gameContext.buildSelection.selectedCharacter
                : null;

        private void Start()
        {
            if (gameContext == null)
            {
                gameContext = DemoGameContext.Instance;
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

            formationSpawner.SpawnCurrentBattle();
            CollectUnits();
            StartCombat();
        }

        public void ResolveAttack(CombatUnitController attacker)
        {
            if (attacker == null || !attacker.IsAlive || IsBattleResolved)
            {
                return;
            }

            if (attacker.team == CombatUnitController.TeamType.Player)
            {
                if (_boss != null && _boss.IsAlive)
                {
                    _boss.ApplyDamage(GetDamage(attacker));
                }
            }
            else
            {
                for (var i = 0; i < _players.Count; i++)
                {
                    var player = _players[i];
                    if (player != null && player.IsAlive)
                    {
                        player.ApplyDamage(GetDamage(attacker));
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
            if (unit == _boss)
            {
                StartCoroutine(HandleBossDeath());
            }
        }

        private void CollectUnits()
        {
            _players.Clear();
            _boss = null;

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

            _boss?.StopCombat();

            yield return new WaitForSeconds(0.75f);

            var isBattle02 = flowController != null && flowController.IsBattle02();
            if (isBattle02)
            {
                flowController?.CompleteBattle02();
                yield break;
            }

            flowController?.CompleteBattle01();
            SpawnDrop();
        }

        private void SpawnDrop()
        {
            if (sceneReferences == null || sceneReferences.dropRoot == null || sceneReferences.dropPoint == null)
            {
                return;
            }

            GameObject instance;
            if (dropPrefab != null)
            {
                instance = Instantiate(dropPrefab, sceneReferences.dropPoint.position, Quaternion.identity, sceneReferences.dropRoot);
            }
            else
            {
                instance = CreateFallbackDrop(sceneReferences.dropRoot, sceneReferences.dropPoint.position);
            }

            var claim = instance.GetComponent<DropClaimController>();
            if (claim == null)
            {
                claim = instance.AddComponent<DropClaimController>();
            }

            claim.flowController = flowController;

            if (dropHintText != null)
            {
                dropHintText.gameObject.SetActive(true);
                dropHintText.text = "Click to Claim Meteor Hammer";
            }
        }

        private GameObject CreateFallbackDrop(Transform parent, Vector3 position)
        {
            var go = new GameObject("MeteorHammerDrop");
            go.transform.SetParent(parent);
            go.transform.position = position;
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = SimpleSpriteFactory.GetMeteorHammerSprite();
            renderer.sortingOrder = 10;
            go.transform.localScale = Vector3.one * 1.5f;
            return go;
        }

        private static int GetDamage(CombatUnitController attacker)
        {
            if (attacker.bossConfig != null)
            {
                return 6;
            }

            if (attacker.UsesMeteorHammerOverride)
            {
                return 16;
            }

            switch (attacker.characterConfig.attackTempo)
            {
                case AttackTempo.Fast:
                    return 8;
                case AttackTempo.Slow:
                    return 10;
                default:
                    return 12;
            }
        }
    }
}
