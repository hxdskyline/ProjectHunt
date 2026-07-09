using UnityEngine;

namespace ProjectHunt.Data
{
    [CreateAssetMenu(
        fileName = "WD_Weapon",
        menuName = "Project Hunt/Data/Weapon Config")]
    public sealed class WeaponConfig : ScriptableObject
    {
        [Header("Identity")]
        public string id;
        public string displayName;
        public WeaponType weaponType;

        [Header("Behavior")]
        public AttackBehaviorType attackBehaviorType;
        public AttackRangeType rangeType;
        public AttackTempo tempo;

        [Header("Rules")]
        public bool canEquipInBuild;
        public bool isBossDrop;
    }
}
