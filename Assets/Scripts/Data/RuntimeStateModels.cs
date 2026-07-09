using System;

namespace ProjectHunt.Data
{
    [Serializable]
    public sealed class BuildSelectionState
    {
        public bool hasClaimedMeteorHammer;
        public CharacterConfig selectedCharacter;
        public bool isSelectionConfirmed;

        public void Reset()
        {
            hasClaimedMeteorHammer = false;
            selectedCharacter = null;
            isSelectionConfirmed = false;
        }
    }

    [Serializable]
    public sealed class DemoRunState
    {
        public GamePhase phase = GamePhase.MainMenu;
        public bool isBattle01Complete;
        public bool isDropSpawned;
        public bool isDropClaimed;
        public bool isBattle02Started;
        public bool isBattle02Complete;

        public void Reset()
        {
            phase = GamePhase.MainMenu;
            isBattle01Complete = false;
            isDropSpawned = false;
            isDropClaimed = false;
            isBattle02Started = false;
            isBattle02Complete = false;
        }
    }
}
