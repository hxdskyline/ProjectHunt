using UnityEngine;

namespace ProjectHunt.Data
{
    [CreateAssetMenu(
        fileName = "BD_Boss",
        menuName = "Project Hunt/Data/Boss Config")]
    public sealed class BossConfig : ScriptableObject
    {
        [Header("Identity")]
        public string id;
        public string displayName;

        [Header("Art")]
        public string resourceId;
        public string mainAttackAction = "attack_round";
        public string specialAttackAction;
        [Min(0)] public int normalAttacksBetweenSpecial;
        public string moveAction = "walk";
        public string deathAction = "death";

        [Header("Combat")]
        public WeaponType weaponType = WeaponType.MeteorHammer;
        public AttackRangeType attackRangeType = AttackRangeType.SpinArea;
        public AttackTempo attackTempo = AttackTempo.Medium;
        public int maxHp = 100;
        public WeaponType dropWeaponType = WeaponType.MeteorHammer;
        public float visualScale = 3.8f;
        public float yOffset = 0f;
    }
}
