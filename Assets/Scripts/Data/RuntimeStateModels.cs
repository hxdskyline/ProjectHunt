using System;

namespace ProjectHunt.Data
{
    [Serializable]
    public sealed class BuildSelectionState
    {
        public bool hasClaimedMeteorHammer;
        public bool hasClaimedHolyCup;
        public bool hasClaimedGiantKey;
        public CharacterConfig selectedCharacter;
        public CharacterConfig selectedHammerCharacter;
        public CharacterConfig selectedCupCharacter;
        public CharacterConfig selectedKeyCharacter;
        public string selectedHammerTargetId;
        public string selectedCupTargetId;
        public string selectedKeyTargetId;
        public bool isSelectionConfirmed;
        public RewardType pendingRewardType;

        public void Reset()
        {
            hasClaimedMeteorHammer = false;
            hasClaimedHolyCup = false;
            hasClaimedGiantKey = false;
            selectedCharacter = null;
            selectedHammerCharacter = null;
            selectedCupCharacter = null;
            selectedKeyCharacter = null;
            selectedHammerTargetId = null;
            selectedCupTargetId = null;
            selectedKeyTargetId = null;
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
            hasClaimedHolyCup = other.hasClaimedHolyCup;
            hasClaimedGiantKey = other.hasClaimedGiantKey;
            selectedCharacter = other.selectedCharacter;
            selectedHammerCharacter = other.selectedHammerCharacter;
            selectedCupCharacter = other.selectedCupCharacter;
            selectedKeyCharacter = other.selectedKeyCharacter;
            selectedHammerTargetId = other.selectedHammerTargetId;
            selectedCupTargetId = other.selectedCupTargetId;
            selectedKeyTargetId = other.selectedKeyTargetId;
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
        public bool isBlacksmithValidationStarted;
        public bool isBlacksmithValidationComplete;
        public bool isBattle03Started;
        public bool isBattle03Complete;
        public bool isDrop02Spawned;
        public bool isDrop02Claimed;
        public bool isBattle04Started;
        public bool isBattle04Complete;
        public bool isBattle05Started;
        public bool isBattle05Complete;
        public bool isDrop03Spawned;
        public bool isDrop03Claimed;
        public bool isBattle06Started;
        public bool isBattle06Complete;
        public bool hasRecruitedMage;
        public bool isMageValidationStarted;
        public string mageReplacedCharacterId;
        public RewardType mageRewardType;

        public void Reset()
        {
            phase = GamePhase.MainMenu;
            isBattle01Complete = false;
            isDropSpawned = false;
            isDropClaimed = false;
            isBattle02Started = false;
            isBattle02Complete = false;
            isBlacksmithValidationStarted = false;
            isBlacksmithValidationComplete = false;
            isBattle03Started = false;
            isBattle03Complete = false;
            isDrop02Spawned = false;
            isDrop02Claimed = false;
            isBattle04Started = false;
            isBattle04Complete = false;
            isBattle05Started = false;
            isBattle05Complete = false;
            isDrop03Spawned = false;
            isDrop03Claimed = false;
            isBattle06Started = false;
            isBattle06Complete = false;
            hasRecruitedMage = false;
            isMageValidationStarted = false;
            mageReplacedCharacterId = null;
            mageRewardType = RewardType.None;
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
            isBlacksmithValidationStarted = other.isBlacksmithValidationStarted;
            isBlacksmithValidationComplete = other.isBlacksmithValidationComplete;
            isBattle03Started = other.isBattle03Started;
            isBattle03Complete = other.isBattle03Complete;
            isDrop02Spawned = other.isDrop02Spawned;
            isDrop02Claimed = other.isDrop02Claimed;
            isBattle04Started = other.isBattle04Started;
            isBattle04Complete = other.isBattle04Complete;
            isBattle05Started = other.isBattle05Started;
            isBattle05Complete = other.isBattle05Complete;
            isDrop03Spawned = other.isDrop03Spawned;
            isDrop03Claimed = other.isDrop03Claimed;
            isBattle06Started = other.isBattle06Started;
            isBattle06Complete = other.isBattle06Complete;
            hasRecruitedMage = other.hasRecruitedMage;
            isMageValidationStarted = other.isMageValidationStarted;
            mageReplacedCharacterId = other.mageReplacedCharacterId;
            mageRewardType = other.mageRewardType;
        }
    }
}
