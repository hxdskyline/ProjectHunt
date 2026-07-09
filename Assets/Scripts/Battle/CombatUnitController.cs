using System.Collections;
using System.Collections.Generic;
using ProjectHunt.Data;
using UnityEngine;

namespace ProjectHunt.Battle
{
    [RequireComponent(typeof(PixelUnitAnimator))]
    public sealed class CombatUnitController : MonoBehaviour
    {
        public enum TeamType
        {
            Player = 0,
            Boss = 1,
        }

        public TeamType team;
        public CharacterConfig characterConfig;
        public BossConfig bossConfig;
        public int currentHp;
        public bool IsAlive => currentHp > 0;
        public bool UsesMeteorHammerOverride => _usesMeteorHammerOverride;

        private PixelUnitAnimator _animator;
        private BattleDirector _director;
        private Coroutine _combatRoutine;
        private bool _usesMeteorHammerOverride;
        private SpriteRenderer _equippedWeaponRenderer;

        private void Awake()
        {
            _animator = GetComponent<PixelUnitAnimator>();
        }

        public void Setup(CharacterConfig config, BattleDirector director)
        {
            characterConfig = config;
            bossConfig = null;
            team = TeamType.Player;
            currentHp = 50;
            _director = director;
            _animator.Configure(config.resourceId);
            _animator.PlayLoop(config.moveAction);
            transform.localScale = Vector3.one * 3f;
            _usesMeteorHammerOverride = director != null &&
                                        director.IsBattle02 &&
                                        director.CurrentMeteorHammerUser == config;
            SetupWeaponVisual();
        }

        public void Setup(BossConfig config, BattleDirector director)
        {
            characterConfig = null;
            bossConfig = config;
            team = TeamType.Boss;
            currentHp = config.maxHp;
            _director = director;
            _animator.Configure(config.resourceId);
            _animator.PlayLoop(config.moveAction);
            transform.localScale = Vector3.one * 3.8f;
            _usesMeteorHammerOverride = false;
            SetupWeaponVisual();
        }

        public void BeginCombat()
        {
            if (_combatRoutine != null)
            {
                StopCoroutine(_combatRoutine);
            }

            _combatRoutine = StartCoroutine(CombatLoop());
        }

        public void StopCombat()
        {
            if (_combatRoutine != null)
            {
                StopCoroutine(_combatRoutine);
                _combatRoutine = null;
            }
        }

        public void ApplyDamage(int amount)
        {
            if (!IsAlive)
            {
                return;
            }

            currentHp = Mathf.Max(0, currentHp - amount);
            _director?.NotifyHpChanged(this);

            if (currentHp <= 0)
            {
                StopCombat();
                if (bossConfig != null)
                {
                    _animator.PlayOnce(bossConfig.deathAction);
                }
                else if (characterConfig != null)
                {
                    _animator.PlayLoop(characterConfig.moveAction);
                }

                _director?.NotifyUnitDied(this);
            }
        }

        public float GetMaxHp()
        {
            return bossConfig != null ? bossConfig.maxHp : 50f;
        }

        private IEnumerator CombatLoop()
        {
            while (IsAlive && _director != null && !_director.IsBattleResolved)
            {
                var attackAction = characterConfig != null ? characterConfig.defaultAttackAction : bossConfig.mainAttackAction;
                var moveAction = characterConfig != null ? characterConfig.moveAction : bossConfig.moveAction;
                var attackDuration = Mathf.Max(0.2f, _animator.GetDuration(attackAction));
                var cooldown = GetAttackInterval();

                _animator.PlayOnce(attackAction);
                if (_usesMeteorHammerOverride)
                {
                    StartCoroutine(PlayMeteorHammerEffect(attackDuration));
                }

                yield return new WaitForSeconds(attackDuration * 0.5f);

                if (IsAlive && _director != null && !_director.IsBattleResolved)
                {
                    _director.ResolveAttack(this);
                }

                yield return new WaitForSeconds(attackDuration * 0.5f);
                _animator.PlayLoop(moveAction);
                yield return new WaitForSeconds(cooldown);
            }
        }

        private float GetAttackInterval()
        {
            if (_usesMeteorHammerOverride)
            {
                return 0.7f;
            }

            var tempo = characterConfig != null ? characterConfig.attackTempo : bossConfig.attackTempo;
            switch (tempo)
            {
                case AttackTempo.Fast:
                    return 0.45f;
                case AttackTempo.Slow:
                    return 1.0f;
                default:
                    return 0.7f;
            }
        }

        private void SetupWeaponVisual()
        {
            if (_equippedWeaponRenderer == null)
            {
                var weaponGo = new GameObject("EquippedWeapon");
                weaponGo.transform.SetParent(transform, false);
                weaponGo.transform.localPosition = new Vector3(0.35f, 0.1f, 0f);
                _equippedWeaponRenderer = weaponGo.AddComponent<SpriteRenderer>();
                _equippedWeaponRenderer.sortingOrder = 6;
            }

            if (_usesMeteorHammerOverride)
            {
                _equippedWeaponRenderer.sprite = SimpleSpriteFactory.GetMeteorHammerSprite();
                _equippedWeaponRenderer.enabled = true;
                _equippedWeaponRenderer.transform.localScale = Vector3.one * 0.35f;
            }
            else
            {
                _equippedWeaponRenderer.enabled = false;
            }
        }

        private IEnumerator PlayMeteorHammerEffect(float duration)
        {
            var effectGo = new GameObject("MeteorHammerSpinEffect");
            effectGo.transform.SetParent(transform, false);
            var renderer = effectGo.AddComponent<SpriteRenderer>();
            renderer.sprite = SimpleSpriteFactory.GetMeteorHammerSprite();
            renderer.sortingOrder = 7;
            effectGo.transform.localScale = Vector3.one * 0.4f;

            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var angle = elapsed / Mathf.Max(0.01f, duration) * Mathf.PI * 4f;
                var radius = 0.75f;
                effectGo.transform.localPosition = new Vector3(
                    Mathf.Cos(angle) * radius,
                    Mathf.Sin(angle) * radius,
                    0f);
                yield return null;
            }

            Destroy(effectGo);
        }
    }
}
