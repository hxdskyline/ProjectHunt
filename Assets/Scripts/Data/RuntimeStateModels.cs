using System;

namespace ProjectHunt.Data
{
    [Serializable]
    public sealed class BuildSelectionState
    {
        public bool hasClaimedMeteorHammer;
        public bool hasClaimedFireGland;
        public CharacterConfig selectedCharacter;
        public CharacterConfig selectedHammerCharacter;
        public CharacterConfig selectedFireCharacter;
        public bool isSelectionConfirmed;
        public RewardType pendingRewardType;

        public void Reset()
        {
            hasClaimedMeteorHammer = false;
            hasClaimedFireGland = false;
            selectedCharacter = null;
            selectedHammerCharacter = null;
            selectedFireCharacter = null;
            isSelectionConfirmed = false;
            pendingRewardType = RewardType.None;
        }

        public void CopyFrom(BuildSelectionState other)
        {
            if (other == null)
            {
                return;
            }
            hasClaimedMeteorHammer = other.hasClaimedMeteorHammer;
            hasClaimedFireGland = other.hasClaimedFireGland;
            selectedCharacter = other.selectedCharacter;
            selectedHammerCharacter = other.selectedHammerCharacter;
            selectedFireCharacter = other.selectedFireCharacter;
            isSelectionConfirmed = other.isSelectionConfirmed;
            pendingRewardType = other.pendingRewardType;
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
        public bool isDrop02Spawned;
        public bool isDrop02Claimed;

        public void Reset()
        {
            phase = GamePhase.MainMenu;
            isBattle01Complete = false;
            isDropSpawned = false;
            isDropClaimed = false;
            isBattle02Started = false;
            isBattle02Complete = false;
            isDrop02Spawned = false;
            isDrop02Claimed = false;
        }

        public void CopyFrom(DemoRunState other)
        {
            if (other == null)
            {
                return;
            }
            phase = other.phase;
            isBattle01Complete = other.isBattle01Complete;
            isDropSpawned = other.isDropSpawned;
            isDropClaimed = other.isDropClaimed;
            isBattle02Started = other.isBattle02Started;
            isBattle02Complete = other.isBattle02Complete;
            isDrop02Spawned = other.isDrop02Spawned;
            isDrop02Claimed = other.isDrop02Claimed;
        }
    }
}
