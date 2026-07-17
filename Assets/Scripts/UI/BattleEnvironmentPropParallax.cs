using System.Collections.Generic;
using ProjectHunt.Battle;
using UnityEngine;

namespace ProjectHunt.UI
{
    /// <summary>
    /// Moves decorative battlefield props with a subtle unit-driven parallax.
    /// These props are not the battle background; a future background layer can move independently.
    /// </summary>
    public sealed class BattleEnvironmentPropParallax : MonoBehaviour
    {
        public float playerFollowFactor = 0.2f;
        public float enemyFollowFactor = 0.055f;
        public float smoothing = 7f;

        private readonly List<CombatUnitController> _players = new List<CombatUnitController>();
        private readonly List<CombatUnitController> _enemies = new List<CombatUnitController>();
        private Vector3 _origin;
        private float _playerFurthestX;
        private float _enemyFurthestX;
        private int _playerSignature;
        private int _enemySignature;
        private bool _hasPlayerPosition;
        private bool _hasEnemyPosition;
        private float _accumulatedOffset;

        private void Awake()
        {
            _origin = transform.position;
        }

        private void LateUpdate()
        {
            CollectUnits();
            _accumulatedOffset += CalculateForwardTravel(
                _players,
                ref _playerFurthestX,
                ref _playerSignature,
                ref _hasPlayerPosition,
                true) * playerFollowFactor;
            _accumulatedOffset += CalculateFirstWaveEnemyTravel(
                _enemies,
                ref _enemyFurthestX,
                ref _enemySignature,
                ref _hasEnemyPosition) * enemyFollowFactor;
            // Props only travel left as combatants enter. Their target never moves backward.
            var target = _origin + Vector3.left * _accumulatedOffset;
            transform.position = Vector3.Lerp(
                transform.position,
                target,
                1f - Mathf.Exp(-smoothing * Time.deltaTime));
        }

        private void CollectUnits()
        {
            _players.Clear();
            _enemies.Clear();
            var units = FindObjectsOfType<CombatUnitController>();
            for (var i = 0; i < units.Length; i++)
            {
                var unit = units[i];
                if (unit == null || !unit.IsAlive)
                {
                    continue;
                }

                if (unit.team == CombatUnitController.TeamType.Player)
                {
                    _players.Add(unit);
                }
                else
                {
                    _enemies.Add(unit);
                }
            }
        }

        private static float CalculateForwardTravel(
            List<CombatUnitController> units,
            ref float furthestX,
            ref int signature,
            ref bool hasPosition,
            bool movesRight)
        {
            if (units.Count == 0)
            {
                hasPosition = false;
                signature = 0;
                return 0f;
            }

            var averageX = 0f;
            var currentSignature = units.Count;
            for (var i = 0; i < units.Count; i++)
            {
                averageX += units[i].transform.position.x;
                currentSignature ^= units[i].GetInstanceID();
            }
            averageX /= units.Count;

            if (!hasPosition || signature != currentSignature)
            {
                furthestX = averageX;
                signature = currentSignature;
                hasPosition = true;
                return 0f;
            }

            if (movesRight)
            {
                if (averageX <= furthestX)
                {
                    return 0f;
                }

                var travel = averageX - furthestX;
                furthestX = averageX;
                return travel;
            }

            if (averageX >= furthestX)
            {
                return 0f;
            }

            var enemyTravel = furthestX - averageX;
            furthestX = averageX;
            return enemyTravel;
        }

        private static float CalculateFirstWaveEnemyTravel(
            List<CombatUnitController> enemies,
            ref float furthestX,
            ref int signature,
            ref bool hasPosition)
        {
            CombatUnitController firstEnemy = null;
            for (var i = 0; i < enemies.Count; i++)
            {
                // Wave enemies are named BurningMonster_<wave>_<index>.
                // Only index 1 is allowed to drive the shared midground.
                if (enemies[i] != null && enemies[i].name.EndsWith("_1"))
                {
                    firstEnemy = enemies[i];
                    break;
                }
            }

            if (firstEnemy == null)
            {
                hasPosition = false;
                signature = 0;
                return 0f;
            }

            var currentX = firstEnemy.transform.position.x;
            var currentSignature = firstEnemy.GetInstanceID();
            if (!hasPosition || signature != currentSignature)
            {
                furthestX = currentX;
                signature = currentSignature;
                hasPosition = true;
                return 0f;
            }

            if (currentX >= furthestX)
            {
                return 0f;
            }

            var travel = furthestX - currentX;
            furthestX = currentX;
            return travel;
        }
    }
}
