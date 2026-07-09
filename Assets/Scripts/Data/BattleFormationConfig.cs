using UnityEngine;

namespace ProjectHunt.Data
{
    [CreateAssetMenu(
        fileName = "FD_BattleFormation",
        menuName = "Project Hunt/Data/Battle Formation Config")]
    public sealed class BattleFormationConfig : ScriptableObject
    {
        public string id;
        public CharacterConfig frontCharacter;
        public CharacterConfig midCharacter;
        public CharacterConfig backCharacter;
        public BossConfig boss;
    }
}
