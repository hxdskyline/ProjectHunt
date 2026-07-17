using System.Collections;
using System.Collections.Generic;
using ProjectHunt.Data;
using ProjectHunt.UI;
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
        public int maxHp;
        public bool IsAlive => currentHp > 0;
        public bool UsesMeteorHammerOverride => _usesMeteorHammerOverride;
        public bool IsStunned => _stunTimer > 0f;
        public bool IsWeakened => _weakenTimer > 0f;

        private PixelUnitAnimator _animator;
        private BattleDirector _director;
        private Coroutine _combatRoutine;
        private bool _usesMeteorHammerOverride;
        private SpriteRenderer _equippedWeaponRenderer;
        private UnitHpBarView _hpBarView;
        private SpriteRenderer _bodyRenderer;
        private Color _baseColor = Color.white;
        private Vector3 _baseVisualScale = Vector3.one;
        private Coroutine _hitFeedbackRoutine;
        private Coroutine _attackFeedbackRoutine;
        private Coroutine _stunRoutine;
        private GameObject _stunStars;
        private bool _resumeCombatAfterStun;
        private float _stunTimer;
        private float _keySwordsmanHasteTimer;
        private float _weakenTimer;
        private int _bossNormalAttackCount;

        private void Awake()
        {
            _animator = GetComponent<PixelUnitAnimator>();
            _bodyRenderer = GetComponent<SpriteRenderer>();
            if (_bodyRenderer != null)
            {
                _baseColor = _bodyRenderer.color;
            }
        }

        private void Update()
        {
            if (_stunTimer > 0f)
            {
                _stunTimer = Mathf.Max(0f, _stunTimer - Time.deltaTime);
            }
            if (_keySwordsmanHasteTimer > 0f)
            {
                _keySwordsmanHasteTimer = Mathf.Max(0f, _keySwordsmanHasteTimer - Time.deltaTime);
            }
            if (_weakenTimer > 0f)
            {
                _weakenTimer = Mathf.Max(0f, _weakenTimer - Time.deltaTime);
            }

            UpdateGiantKeyWindingVisual();
        }

        private void UpdateGiantKeyWindingVisual()
        {
            if (!IsGiantKeySwordsman() || _equippedWeaponRenderer == null || !_equippedWeaponRenderer.enabled)
            {
                return;
            }

            // Rotate into and out of the screen around its horizontal axis, like winding a key.
            var windingAngle = Mathf.Repeat(Time.time * 240f, 360f);
            _equippedWeaponRenderer.transform.localRotation = Quaternion.Euler(windingAngle, 0f, 0f);
        }

        private void OnDestroy()
        {
            CleanupHpBar();
        }

        public void Setup(CharacterConfig config, BattleDirector director)
        {
            characterConfig = config;
            bossConfig = null;
            team = TeamType.Player;
            maxHp = 50;
            currentHp = maxHp;
            _director = director;
            _animator.Configure(config.resourceId);
            _animator.PlayLoop(config.moveAction);
            _baseVisualScale = Vector3.one * Mathf.Max(0.01f, config.visualScale);
            transform.localScale = _baseVisualScale;

            var position = transform.position;
            position.y += config.yOffset;
            transform.position = position;

            _usesMeteorHammerOverride = HammerCharacterFactory.IsHammerVariant(config);
            SetupWeaponVisual();
            SetupHpBar();
        }

        public void ApplyTemporaryCharacterConfig(CharacterConfig config)
        {
            if (config == null || team != TeamType.Player || !IsAlive)
            {
                return;
            }

            characterConfig = config;
            _animator.Configure(config.resourceId);
            _baseVisualScale = Vector3.one * Mathf.Max(0.01f, config.visualScale);
            transform.localScale = _baseVisualScale;
            _usesMeteorHammerOverride = HammerCharacterFactory.IsHammerVariant(config);
            SetupWeaponVisual();
            _animator.PlayLoop(config.moveAction);
        }

        public void Setup(BossConfig config, BattleDirector director)
        {
            characterConfig = null;
            bossConfig = config;
            team = TeamType.Boss;
            maxHp = Mathf.Max(1, config.maxHp);
            currentHp = maxHp;
            _director = director;
            _animator.Configure(config.resourceId);
            _animator.PlayLoop(config.moveAction);
            _baseVisualScale = Vector3.one * Mathf.Max(0.01f, config.visualScale);
            transform.localScale = _baseVisualScale;
            var position = transform.position;
            position.y += config.yOffset;
            transform.position = position;
            _usesMeteorHammerOverride = false;
            SetupWeaponVisual();
            SetupHpBar();
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

            if (_animator != null)
            {
                _animator.Stop();
            }
        }

        public void ApplyDamage(int amount)
        {
            if (!IsAlive)
            {
                return;
            }

            PlayHitFeedback(amount);

            currentHp = Mathf.Max(0, currentHp - amount);
            if (_hpBarView != null)
            {
                _hpBarView.SetValue(currentHp);
            }
            _director?.NotifyHpChanged(this);

            if (currentHp <= 0)
            {
                var deathVolumeScale = _director != null && _director.IsWaveEnemy(this) ? 0.5f : 1f;
                BattleSfx.PlayDeath(bossConfig != null, deathVolumeScale);
                CleanupHpBar();
                StopCombat();
                if (bossConfig != null)
                {
                    _animator.PlayOnce(bossConfig.deathAction);
                }
                else if (characterConfig != null)
                {
                    StartCoroutine(HidePlayerAfterDeath());
                }

                _director?.NotifyUnitDied(this);
            }
        }

        public int RestoreHp(int amount)
        {
            if (!IsAlive || amount <= 0 || currentHp >= maxHp)
            {
                return 0;
            }

            var previousHp = currentHp;
            currentHp = Mathf.Min(maxHp, currentHp + amount);
            if (_hpBarView != null)
            {
                _hpBarView.SetValue(currentHp);
            }
            _director?.NotifyHpChanged(this);
            return currentHp - previousHp;
        }

        public float GetMaxHp()
        {
            return maxHp;
        }

        private IEnumerator HidePlayerAfterDeath()
        {
            // Player art has no reliable death clips yet; remove the defeated unit after hit feedback.
            yield return new WaitForSeconds(0.1f);
            if (this != null && gameObject != null)
            {
                gameObject.SetActive(false);
            }
        }

        public Vector3 GetProjectileLaunchPosition()
        {
            if (characterConfig != null && characterConfig.roleType == RoleType.Archer)
            {
                return transform.position + new Vector3(0.55f, 0.45f, 0f);
            }

            if (characterConfig != null && characterConfig.roleType == RoleType.Assassin)
            {
                return transform.position + new Vector3(0.42f, 0.34f, 0f);
            }

            return transform.position + new Vector3(0.35f, 0.2f, 0f);
        }

        public float GetActionDuration(string actionName)
        {
            return _animator != null ? _animator.GetDuration(actionName) : 0f;
        }

        public bool TryApplyStun(float duration, string label = "已眩晕", bool showStars = true)
        {
            if (!IsAlive || duration <= 0f)
            {
                return false;
            }

            _stunTimer = Mathf.Max(_stunTimer, duration);
            if (_combatRoutine != null)
            {
                StopCoroutine(_combatRoutine);
                _combatRoutine = null;
                _resumeCombatAfterStun = true;
            }
            if (_attackFeedbackRoutine != null)
            {
                StopCoroutine(_attackFeedbackRoutine);
                _attackFeedbackRoutine = null;
                transform.localScale = _baseVisualScale;
            }

            PlayStand();
            ShowCombatText(label, showStars
                ? new Color(1f, 0.82f, 0.2f, 1f)
                : new Color(0.55f, 0.84f, 1f, 1f));
            if (showStars && _stunStars == null)
            {
                _stunStars = CreateStunStars();
            }
            if (_stunRoutine == null)
            {
                _stunRoutine = StartCoroutine(StunRoutine());
            }
            return true;
        }

        private IEnumerator StunRoutine()
        {
            while (IsAlive && _stunTimer > 0f)
            {
                PlayStand();
                UpdateStunStars();
                yield return null;
            }

            if (_stunStars != null)
            {
                Destroy(_stunStars);
                _stunStars = null;
            }
            _stunRoutine = null;
            if (_resumeCombatAfterStun && IsAlive && _director != null && !_director.IsBattleResolved)
            {
                _resumeCombatAfterStun = false;
                BeginCombat();
            }
        }

        private GameObject CreateStunStars()
        {
            var root = new GameObject("StunStars");
            for (var i = 0; i < 3; i++)
            {
                var star = new GameObject("Star_" + i);
                star.transform.SetParent(root.transform, false);
                var text = star.AddComponent<TextMesh>();
                text.text = "★";
                text.fontSize = 28;
                text.characterSize = 0.08f;
                text.anchor = TextAnchor.MiddleCenter;
                text.alignment = TextAlignment.Center;
                text.color = new Color(1f, 0.78f, 0.12f, 1f);
                var renderer = star.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    renderer.sortingOrder = 32;
                }
            }
            return root;
        }

        private void UpdateStunStars()
        {
            if (_stunStars == null)
            {
                return;
            }

            _stunStars.transform.position = transform.position + Vector3.up * 1.35f;
            for (var i = 0; i < _stunStars.transform.childCount; i++)
            {
                var angle = Time.time * 5f + i * Mathf.PI * 2f / 3f;
                _stunStars.transform.GetChild(i).localPosition = new Vector3(
                    Mathf.Cos(angle) * 0.38f,
                    Mathf.Sin(angle) * 0.12f,
                    0f);
            }
        }

        public void ShowStatusText(string label, Color textColor)
        {
            StartCoroutine(FloatingStatusRoutine(label, textColor));
        }

        public void ShowCombatText(string label, Color textColor)
        {
            CreateFloatingCombatText(label, textColor);
        }

        public void ApplyWeaken(float duration)
        {
            if (!IsAlive || duration <= 0f)
            {
                return;
            }

            _weakenTimer = Mathf.Max(_weakenTimer, duration);
            ShowCombatText("削弱", new Color(0.72f, 0.82f, 1f, 1f));
        }

        public int ModifyOutgoingDamage(int damage)
        {
            var multiplier = IsWeakened ? HolyCupRules.WeakenedDamageMultiplier : 1f;
            return Mathf.Max(1, Mathf.RoundToInt(damage * multiplier));
        }

        public void PlayAttackFeedback()
        {
            if (_attackFeedbackRoutine != null)
            {
                StopCoroutine(_attackFeedbackRoutine);
            }

            _attackFeedbackRoutine = StartCoroutine(AttackFeedbackRoutine());
        }

        public void PlayMoveLoop()
        {
            _animator.playbackSpeed = 1f;
            if (characterConfig != null)
            {
                _animator.PlayLoop(characterConfig.moveAction);
            }
            else if (bossConfig != null)
            {
                _animator.PlayLoop(bossConfig.moveAction);
            }
        }

        public void PlayStand()
        {
            if (_animator == null)
            {
                return;
            }

            if (_animator.GetDuration("idle") > 0f)
            {
                _animator.PlayLoop("idle");
                return;
            }

            if (_animator.GetDuration("stand") > 0f)
            {
                _animator.PlayLoop("stand");
                return;
            }

            // Units without an authored idle use the first walk frame as a stable standing pose.
            var moveAction = characterConfig != null
                ? characterConfig.moveAction
                : bossConfig != null ? bossConfig.moveAction : null;
            _animator.PlayOnce(moveAction);
            _animator.Stop();
        }

        public void BeginWaveEffects()
        {
            _keySwordsmanHasteTimer = IsGiantKeySwordsman() ? 4f : 0f;
        }

        public IEnumerator MoveToPosition(Vector3 targetPosition, float moveSpeed)
        {
            PlayMoveLoop();

            var finalTargetPosition = targetPosition;

            while (this != null && transform != null && Vector3.Distance(transform.position, targetPosition) > 0.02f)
            {
                if (IsStunned)
                {
                    yield return null;
                    continue;
                }
                transform.position = Vector3.MoveTowards(
                    transform.position,
                    targetPosition,
                    moveSpeed * Time.deltaTime);
                yield return null;
            }

            if (this != null && transform != null)
            {
                transform.position = finalTargetPosition;
                PlayMoveLoop();
            }
        }

        private IEnumerator CombatLoop()
        {
            Debug.Log($"[CombatLoop] START: this={gameObject.name}, team={team}, alive={IsAlive}, hasConfig={(characterConfig != null ? characterConfig.resourceId : bossConfig?.resourceId)}");
            while (IsAlive && _director != null && !_director.IsBattleResolved)
            {
                while (IsStunned && IsAlive && _director != null && !_director.IsBattleResolved)
                {
                    PlayMoveLoop();
                    yield return null;
                }

                var attackAction = GetAttackAction();
                var moveAction = characterConfig != null ? characterConfig.moveAction : bossConfig.moveAction;
                var attackSpeedMultiplier = GetAttackSpeedMultiplier();
                var attackDuration = Mathf.Max(
                    0.1f,
                    _animator.GetDuration(attackAction) / attackSpeedMultiplier);
                var cooldown = GetAttackInterval();
                var impactSchedule = GetImpactSchedule(attackAction, attackDuration);
                var targetSnapshot = _director.CaptureAttackTarget(this);

                _animator.playbackSpeed = attackSpeedMultiplier;
                _animator.PlayOnce(attackAction);
                var previousImpactTime = 0f;
                for (var i = 0; i < impactSchedule.Count; i++)
                {
                    var impactTime = impactSchedule[i];
                    var elapsed = 0f;
                    while (elapsed < impactTime - previousImpactTime)
                    {
                        elapsed += Time.deltaTime;
                        yield return null;
                    }

                    if (IsAlive && !IsStunned && _director != null && !_director.IsBattleResolved)
                    {
                        Debug.Log($"[CombatLoop] {gameObject.name} impact #{i} -> ResolveAttack");
                        _director.ResolveAttack(this, i, targetSnapshot, attackAction);
                    }

                    previousImpactTime = impactTime;
                }

                var remaining = attackDuration - previousImpactTime;
                if (remaining > 0f)
                {
                    yield return new WaitForSeconds(remaining);
                }

                _animator.playbackSpeed = 1f;
                _animator.PlayLoop(moveAction);
                yield return new WaitForSeconds(cooldown);
            }
        }

        private string GetAttackAction()
        {
            if (characterConfig != null)
            {
                return characterConfig.defaultAttackAction;
            }

            if (bossConfig == null || string.IsNullOrWhiteSpace(bossConfig.specialAttackAction) ||
                bossConfig.normalAttacksBetweenSpecial <= 0)
            {
                return bossConfig != null ? bossConfig.mainAttackAction : string.Empty;
            }

            if (_bossNormalAttackCount >= bossConfig.normalAttacksBetweenSpecial)
            {
                _bossNormalAttackCount = 0;
                return bossConfig.specialAttackAction;
            }

            _bossNormalAttackCount++;
            return bossConfig.mainAttackAction;
        }

        private float GetAttackInterval()
        {
            var tempo = characterConfig != null ? characterConfig.attackTempo : bossConfig.attackTempo;
            var interval = 0.7f;
            switch (tempo)
            {
                case AttackTempo.Fast:
                    interval = 0.45f;
                    break;
                case AttackTempo.Slow:
                    interval = 1.0f;
                    break;
            }


            return interval / GetAttackSpeedMultiplier();
        }

        private float GetAttackSpeedMultiplier()
        {
            return IsGiantKeySwordsman() && _keySwordsmanHasteTimer > 0f ? 2f : 1f;
        }

        private bool IsGiantKeySwordsman()
        {
            return characterConfig != null &&
                   characterConfig.roleType == RoleType.Swordsman &&
                   !string.IsNullOrWhiteSpace(characterConfig.id) &&
                   characterConfig.id.EndsWith("_key");
        }

        private List<float> GetImpactSchedule(string attackAction, float attackDuration)
        {
            var schedule = new List<float>(2);

            if (characterConfig != null &&
                characterConfig.roleType == RoleType.Assassin &&
                attackAction == "attack" &&
                (characterConfig.resourceId == "assassin" || _usesMeteorHammerOverride))
            {
                const int totalFrames = 10;
                schedule.Add(attackDuration * (3f / totalFrames));
                schedule.Add(attackDuration * (8f / totalFrames));
                return schedule;
            }

            if (characterConfig != null &&
                characterConfig.id == "mage_none" &&
                attackAction == "attack")
            {
                // The beam is released on the fifth frame of the mage's seven-frame cast.
                schedule.Add(attackDuration * (4f / 7f));
                return schedule;
            }

            if (bossConfig == null)
            {
                schedule.Add(attackDuration * 0.5f);
                return schedule;
            }

            // For the current goblin boss strong slam, trigger damage exactly when
            // image_23744_tex020_60x60 appears.
            if (bossConfig.resourceId == "goblin_boss_wife" && attackAction == "slam_strong")
            {
                const int targetFrameIndex = 16;
                const int totalFrames = 23;
                schedule.Add(attackDuration * (targetFrameIndex / (float)totalFrames));
                return schedule;
            }

            var frameCount = _animator != null ? _animator.GetFrameCount(attackAction) : 0;
            if (frameCount >= 10)
            {
                var frameDuration = attackDuration / frameCount;
                schedule.Add(Mathf.Max(0f, attackDuration - frameDuration * 10));
                return schedule;
            }

            if (frameCount >= 2)
            {
                var frameDuration = attackDuration / frameCount;
                schedule.Add(Mathf.Max(0f, attackDuration - frameDuration * 2f));
                return schedule;
            }

            schedule.Add(attackDuration * 0.5f);
            return schedule;
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

            if (_equippedWeaponRenderer == null)
            {
                return;
            }

            _equippedWeaponRenderer.enabled = false;
            if (characterConfig == null || string.IsNullOrWhiteSpace(characterConfig.id))
            {
                return;
            }

            if (characterConfig.roleType == RoleType.Swordsman && characterConfig.id.EndsWith("_key"))
            {
                _equippedWeaponRenderer.sprite = SimpleSpriteFactory.GetGiantKeySprite();
                _equippedWeaponRenderer.sortingOrder = 4;
                _equippedWeaponRenderer.transform.localPosition = new Vector3(-0.2f, 0.2f, 0f);
                _equippedWeaponRenderer.transform.localEulerAngles = Vector3.zero;
                _equippedWeaponRenderer.transform.localScale = Vector3.one * 0.58f;
                _equippedWeaponRenderer.enabled = true;
            }
            else if (characterConfig.roleType == RoleType.Swordsman && characterConfig.id.EndsWith("_cup"))
            {
                _equippedWeaponRenderer.sprite = SimpleSpriteFactory.GetHolyCupSprite();
                _equippedWeaponRenderer.sortingOrder = 7;
                _equippedWeaponRenderer.transform.localPosition = new Vector3(0.0658f, 0.2585f, 0f);
                _equippedWeaponRenderer.transform.localEulerAngles = new Vector3(0f, 0f, 180f);
                _equippedWeaponRenderer.transform.localScale = new Vector3(0.5719084f, 0.29298f, 0.3f);
                _equippedWeaponRenderer.enabled = true;
            }
        }

        private void SetupHpBar()
        {
            if (_hpBarView == null)
            {
                var barRoot = new GameObject("UnitHpBar");
                barRoot.transform.SetParent(transform.parent, false);
                _hpBarView = barRoot.AddComponent<UnitHpBarView>();

                var background = new GameObject("Background");
                background.transform.SetParent(barRoot.transform, false);
                var backgroundRenderer = background.AddComponent<SpriteRenderer>();
                backgroundRenderer.sprite = SimpleSpriteFactory.GetWhitePixelSprite();
                backgroundRenderer.color = new Color(0f, 0f, 0f, 0.75f);
                backgroundRenderer.sortingOrder = 20;

                var fill = new GameObject("Fill");
                fill.transform.SetParent(barRoot.transform, false);
                var fillRenderer = fill.AddComponent<SpriteRenderer>();
                fillRenderer.sprite = SimpleSpriteFactory.GetWhitePixelSprite();
                fillRenderer.color = team == TeamType.Boss
                    ? new Color(0.82f, 0.2f, 0.2f, 1f)
                    : new Color(0.18f, 0.82f, 0.28f, 1f);
                fillRenderer.sortingOrder = 21;

                _hpBarView.backgroundRenderer = backgroundRenderer;
                _hpBarView.fillRenderer = fillRenderer;
                var bossBarHeight = bossConfig != null ? Mathf.Clamp(bossConfig.visualScale * 0.55f, 0.95f, 1.95f) : 1.95f;
                _hpBarView.worldOffset = team == TeamType.Boss
                    ? new Vector3(0f, bossBarHeight, 0f)
                    : new Vector3(0f, 1.45f, 0f);
                _hpBarView.backgroundWidth = team == TeamType.Boss ? 2.0f : 1.5f;
                _hpBarView.backgroundHeight = 0.16f;
                _hpBarView.fillWidth = team == TeamType.Boss ? 1.84f : 1.36f;
                _hpBarView.fillHeight = 0.10f;
            }

            _hpBarView.Bind(transform, maxHp);
        }

        private void CleanupHpBar()
        {
            if (_hpBarView == null)
            {
                return;
            }

            if (_hpBarView.gameObject != null)
            {
                Destroy(_hpBarView.gameObject);
            }

            _hpBarView = null;
        }

        private void PlayHitFeedback(int damage)
        {
            if (_hitFeedbackRoutine != null)
            {
                StopCoroutine(_hitFeedbackRoutine);
            }

            _hitFeedbackRoutine = StartCoroutine(HitFeedbackRoutine());
            FloatingDamageRoutine(damage);
        }

        private IEnumerator AttackFeedbackRoutine()
        {
            var elapsed = 0f;
            const float duration = 0.12f;
            var punchDirection = team == TeamType.Player ? 1f : -1f;
            var originalPosition = transform.position;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                var bump = Mathf.Sin(t * Mathf.PI);
                transform.localScale = _baseVisualScale * (1f + bump * 0.08f);
                transform.position = originalPosition + new Vector3(punchDirection * bump * 0.08f, 0f, 0f);
                yield return null;
            }

            transform.position = originalPosition;
            transform.localScale = _baseVisualScale;
            _attackFeedbackRoutine = null;
        }

        private IEnumerator HitFeedbackRoutine()
        {
            if (_bodyRenderer == null)
            {
                _bodyRenderer = GetComponent<SpriteRenderer>();
            }

            var originalPosition = transform.position;
            var elapsed = 0f;
            const float duration = 0.16f;
            var shakeDirection = team == TeamType.Player ? -1f : 1f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                var flash = Mathf.Sin(t * Mathf.PI);

                if (_bodyRenderer != null)
                {
                    _bodyRenderer.color = Color.Lerp(_baseColor, Color.white, flash);
                }

                transform.position = originalPosition + new Vector3(
                    shakeDirection * Mathf.Sin(t * Mathf.PI * 4f) * 0.08f,
                    Mathf.Sin(t * Mathf.PI * 2f) * 0.03f,
                    0f);

                yield return null;
            }

            transform.position = originalPosition;
            transform.localScale = _baseVisualScale;

            if (_bodyRenderer != null)
            {
                _bodyRenderer.color = _baseColor;
            }

            _hitFeedbackRoutine = null;
        }

        private void FloatingDamageRoutine(int damage)
        {
            CreateFloatingCombatText(damage.ToString(), new Color(1f, 0.92f, 0.72f, 1f));
        }

        private void CreateFloatingCombatText(string label, Color textColor)
        {
            var textGo = new GameObject("DamageText");
            textGo.transform.position = transform.position + new Vector3(0f, 1.1f, 0f);

            var textMesh = textGo.AddComponent<TextMesh>();
            textMesh.text = label;
            textMesh.fontSize = 36;
            textMesh.characterSize = 0.16f;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.color = textColor;

            var meshRenderer = textGo.GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                meshRenderer.sortingOrder = 30;
            }

            var floating = textGo.AddComponent<FloatingDamageText>();
            floating.Setup(textMesh);
        }

        private IEnumerator FloatingStatusRoutine(string label, Color textColor)
        {
            var textGo = new GameObject("StatusText");
            textGo.transform.SetParent(transform.parent, false);
            textGo.transform.position = transform.position + new Vector3(0f, 1.45f, 0f);

            var textMesh = textGo.AddComponent<TextMesh>();
            textMesh.text = label;
            textMesh.fontSize = 16;
            textMesh.characterSize = 0.08f;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.color = textColor;

            var meshRenderer = textGo.GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                meshRenderer.sortingOrder = 31;
            }

            var start = textGo.transform.position;
            var end = start + new Vector3(0f, 0.55f, 0f);
            var elapsed = 0f;
            const float duration = 0.4f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                textGo.transform.position = Vector3.Lerp(start, end, t);
                var color = textMesh.color;
                color.a = 1f - t;
                textMesh.color = color;
                yield return null;
            }

            Destroy(textGo);
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

    public sealed class FloatingDamageText : MonoBehaviour
    {
        private TextMesh _textMesh;
        private float _elapsed;
        private readonly float _duration = 0.45f;
        private Vector3 _start;
        private Vector3 _end;

        public void Setup(TextMesh textMesh)
        {
            _textMesh = textMesh;
            _elapsed = 0f;
            _start = transform.position;
            _end = _start + new Vector3(0f, 0.7f, 0f);
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            var t = Mathf.Clamp01(_elapsed / _duration);
            transform.position = Vector3.Lerp(_start, _end, t);
            transform.localScale = Vector3.one * (1f + Mathf.Sin(t * Mathf.PI) * 0.36f);

            if (_textMesh != null)
            {
                var color = _textMesh.color;
                color.a = 1f - t;
                _textMesh.color = color;
            }

            if (t >= 1f)
            {
                Destroy(gameObject);
            }
        }
    }
}
