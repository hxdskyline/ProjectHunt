using UnityEngine;

namespace ProjectHunt.Data
{
    [CreateAssetMenu(
        fileName = "CD_Character",
        menuName = "Project Hunt/Data/Character Config")]
    public sealed class CharacterConfig : ScriptableObject
    {
        [Header("Identity")]
        public string id;
        public string displayName;
        public RoleType roleType;
        public bool isHammerVariant;
        public string baseCharacterId;

        [Header("Art")]
        public string resourceId;
        public string defaultAttackAction = "attack";
        public string moveAction = "walk";
        public float visualScale = 3f;
        public float yOffset = 0f;

        [Header("Combat")]
        public WeaponType defaultWeaponType;
        public AttackTempo attackTempo;
        public AttackRangeType attackRangeType;
        public SpawnSlot spawnSlot;
        public bool isPlayable = true;
    }
}
