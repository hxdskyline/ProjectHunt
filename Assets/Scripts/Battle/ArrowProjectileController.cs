using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectHunt.Battle
{
    public sealed class ArrowProjectileController : MonoBehaviour
    {
        public SpriteRenderer spriteRenderer;
        public float duration = 0.45f;
        public float arcHeight = 1.2f;
        public float rotationOffset = -20f;
        public bool isMeteorHammerProjectile;
        public float areaImpactRadius;
        public bool alignToVelocity = true;
        public float spinSpeed;
        public float spinAccelerationDelay = -1f;
        public float spinAccelerationMultiplier = 1f;
        public Sprite[] animationFrames;
        public float animationFps = 12f;
        public Vector3 staticEulerAngles;
        public bool returnToSender;
        public bool pierceAllEnemiesByX;
        public CombatUnitController attacker;
        public bool appliesHammerAssassinStun;

        public void Launch(
            Vector3 startPosition,
            Vector3 targetPosition,
            Sprite sprite,
            int damage,
            CombatUnitController target,
            BattleDirector director)
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            if (spriteRenderer == null)
            {
                spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            }

            spriteRenderer.sprite = sprite;
            spriteRenderer.sortingOrder = 12;
            transform.position = startPosition;
            transform.rotation = Quaternion.Euler(staticEulerAngles);
            StartCoroutine(Fly(startPosition, targetPosition, damage, target, director));
        }

        private IEnumerator Fly(
            Vector3 startPosition,
            Vector3 targetPosition,
            int damage,
            CombatUnitController target,
            BattleDirector director)
        {
            var elapsed = 0f;
            var previousX = startPosition.x;
            var outwardHits = pierceAllEnemiesByX ? new HashSet<int>() : null;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, duration));
                var position = Vector3.Lerp(startPosition, targetPosition, t);
                position.y += Mathf.Sin(t * Mathf.PI) * arcHeight;
                transform.position = position;
                if (pierceAllEnemiesByX && director != null)
                {
                    director.ApplyPlayerCrossingDamage(previousX, position.x, damage, outwardHits);
                    previousX = position.x;
                }
                UpdateAnimationFrame(elapsed);

                if (spinSpeed != 0f)
                {
                    transform.rotation = Quaternion.Euler(
                        staticEulerAngles.x,
                        staticEulerAngles.y,
                        staticEulerAngles.z + GetAccumulatedSpin(elapsed));
                }
                else if (alignToVelocity)
                {
                    var tangent = (targetPosition - startPosition).normalized;
                    var angle = Mathf.Atan2(tangent.y + Mathf.Cos(t * Mathf.PI) * (arcHeight * 0.45f), tangent.x) * Mathf.Rad2Deg;
                    transform.rotation = Quaternion.Euler(
                        staticEulerAngles.x,
                        staticEulerAngles.y,
                        staticEulerAngles.z + angle + rotationOffset);
                }
                yield return null;
            }

            transform.position = targetPosition;
            if (!pierceAllEnemiesByX && director != null && !director.IsBattleResolved)
            {
                if (isMeteorHammerProjectile && areaImpactRadius > 0f)
                {
                    BattleSfx.PlayImpact(true, true);
                    director.SpawnHitSpark(targetPosition, 0.85f);
                    director.ApplyPlayerAreaDamage(targetPosition, areaImpactRadius, damage);
                }
                else if (target != null && target.IsAlive)
                {
                    BattleSfx.PlayImpact(false, spinSpeed != 0f);
                    director.SpawnHitSpark(targetPosition, 0.5f);
                    target.ApplyDamage(damage);
                    if (appliesHammerAssassinStun && target.IsAlive)
                    {
                        director.TryApplyHammerAssassinStun(attacker, target);
                    }
                }
                else
                {
                    BattleSfx.PlayImpact(false, false);
                    director.SpawnHitSpark(targetPosition, 0.35f);
                }
            }

            if (returnToSender)
            {
                yield return StartCoroutine(FlyBack(targetPosition, startPosition, damage, director));
            }

            Destroy(gameObject);
        }

        private IEnumerator FlyBack(
            Vector3 startPosition,
            Vector3 endPosition,
            int damage,
            BattleDirector director)
        {
            var elapsed = 0f;
            var previousX = startPosition.x;
            var returnHits = pierceAllEnemiesByX ? new HashSet<int>() : null;
            var returnDuration = Mathf.Max(0.12f, duration * 0.8f);
            while (elapsed < returnDuration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / returnDuration);
                transform.position = Vector3.Lerp(startPosition, endPosition, t);
                if (pierceAllEnemiesByX && director != null)
                {
                    director.ApplyPlayerCrossingDamage(previousX, transform.position.x, damage, returnHits);
                    previousX = transform.position.x;
                }
                if (spinSpeed != 0f)
                {
                    var returnSpinSpeed = spinAccelerationDelay >= 0f
                        ? spinSpeed * spinAccelerationMultiplier
                        : spinSpeed;
                    transform.Rotate(0f, 0f, returnSpinSpeed * Time.deltaTime);
                }
                yield return null;
            }
        }

        private float GetAccumulatedSpin(float elapsed)
        {
            if (spinAccelerationDelay < 0f || spinAccelerationMultiplier <= 1f || elapsed <= spinAccelerationDelay)
            {
                return spinSpeed * elapsed;
            }

            var normalSpin = spinSpeed * spinAccelerationDelay;
            var acceleratedSpin = spinSpeed * spinAccelerationMultiplier * (elapsed - spinAccelerationDelay);
            return normalSpin + acceleratedSpin;
        }

        private void UpdateAnimationFrame(float elapsed)
        {
            if (spriteRenderer == null || animationFrames == null || animationFrames.Length == 0 || animationFps <= 0f)
            {
                return;
            }

            var frameIndex = Mathf.FloorToInt(elapsed * animationFps) % animationFrames.Length;
            var frame = animationFrames[frameIndex];
            if (frame != null)
            {
                spriteRenderer.sprite = frame;
            }
        }
    }
}
