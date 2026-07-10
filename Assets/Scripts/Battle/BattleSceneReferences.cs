using UnityEngine;

namespace ProjectHunt.Battle
{
    public sealed class BattleSceneReferences : MonoBehaviour
    {
        [Header("Roots")]
        public Transform playerTeamRoot;
        public Transform bossRoot;
        public Transform dropRoot;
        public Transform projectileRoot;

        [Header("Spawn Points")]
        public Transform playerFrontPoint;
        public Transform playerMidPoint;
        public Transform playerBackPoint;
        public Transform bossPoint;
        public Transform dropPoint;
    }
}
