using ProjectHunt.Data;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectHunt.Flow
{
    public sealed class DemoFlowController : MonoBehaviour
    {
        public static DemoFlowController Instance { get; private set; }

        [Header("Context")]
        public DemoGameContext gameContext;

        [Header("Scene Names")]
        public string mainMenuSceneName = "MainMenu";
        public string battleSceneName = "BattleScene";
        public string buildSceneName = "BuildScene";
        public string resultSceneName = "ResultScene";

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                var oldInstance = Instance;
                Instance = this;
                DontDestroyOnLoad(gameObject);
                Destroy(oldInstance.gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (gameContext == null)
            {
                gameContext = DemoGameContext.Instance;
            }

            if (gameContext != null)
            {
                gameContext.flowController = this;
            }
        }

        public void StartNewRun()
        {
            EnsureContext();
            gameContext.ResetRun();
            gameContext.runState.phase = GamePhase.Battle01;
            SceneManager.LoadScene(battleSceneName);
        }

        public void ReturnToMainMenu()
        {
            EnsureContext();
            gameContext.runState.phase = GamePhase.MainMenu;
            SceneManager.LoadScene(mainMenuSceneName);
        }

        public void CompleteBattle01()
        {
            EnsureContext();
            gameContext.runState.isBattle01Complete = true;
            gameContext.runState.isDropSpawned = true;
            gameContext.runState.phase = GamePhase.Drop;
        }

        public void ClaimMeteorHammer()
        {
            EnsureContext();
            gameContext.buildSelection.hasClaimedMeteorHammer = true;
            gameContext.buildSelection.pendingRewardType = RewardType.MeteorHammer;
            gameContext.runState.isDropClaimed = true;
            gameContext.runState.phase = GamePhase.Build;
            SceneManager.LoadScene(buildSceneName);
        }

        public void ConfirmBuildSelection(CharacterConfig selectedCharacter)
        {
            EnsureContext();
            gameContext.buildSelection.selectedCharacter = selectedCharacter;
            gameContext.buildSelection.selectedHammerCharacter = HammerCharacterFactory.GetHammerVariant(selectedCharacter);
            gameContext.buildSelection.MarkUnitDiscovered(gameContext.buildSelection.selectedHammerCharacter != null
                ? gameContext.buildSelection.selectedHammerCharacter.id
                : null);
            gameContext.buildSelection.selectedHammerTargetId = selectedCharacter != null ? selectedCharacter.id : null;
            gameContext.buildSelection.isSelectionConfirmed = selectedCharacter != null;
            gameContext.buildSelection.hasClaimedMeteorHammer = selectedCharacter != null;
            gameContext.buildSelection.pendingRewardType = RewardType.None;
            StartBattlePhase(GamePhase.Battle02);
        }

        public void CompleteBattle02()
        {
            EnsureContext();
            gameContext.runState.isBattle02Complete = true;
            gameContext.runState.isBattle02Started = false;
            StartBattlePhase(GamePhase.Battle03);
        }

        public void CompleteBattle03()
        {
            EnsureContext();
            gameContext.runState.isBattle03Complete = true;
            gameContext.runState.isBattle03Started = false;
            gameContext.runState.isDrop02Spawned = true;
            gameContext.runState.phase = GamePhase.Drop02;
        }

        public void ClaimHolyCup()
        {
            EnsureContext();
            gameContext.buildSelection.hasClaimedHolyCup = true;
            gameContext.buildSelection.pendingRewardType = RewardType.HolyCup;
            gameContext.runState.isDrop02Claimed = true;
            gameContext.runState.phase = GamePhase.Build02;
            SceneManager.LoadScene(buildSceneName);
        }

        public void ConfirmHolyCupSelection(CharacterConfig selectedCharacter)
        {
            EnsureContext();
            gameContext.buildSelection.selectedCharacter = selectedCharacter;
            gameContext.buildSelection.selectedCupCharacter = HolyCupCharacterFactory.GetCupVariant(selectedCharacter);
            gameContext.buildSelection.MarkUnitDiscovered(gameContext.buildSelection.selectedCupCharacter != null
                ? gameContext.buildSelection.selectedCupCharacter.id
                : null);
            gameContext.buildSelection.selectedCupTargetId = selectedCharacter != null ? selectedCharacter.id : null;
            gameContext.buildSelection.pendingRewardType = RewardType.None;
            StartBattlePhase(GamePhase.Battle04);
        }

        public void CompleteBattle04()
        {
            EnsureContext();
            gameContext.runState.isBattle04Complete = true;
            gameContext.runState.isBattle04Started = false;
            gameContext.runState.isMageValidationStarted = false;
            StartBattlePhase(GamePhase.Battle05);
        }

        public void RecruitMage(string replacedCharacterId, RewardType rewardType)
        {
            EnsureContext();
            gameContext.runState.hasRecruitedMage = !string.IsNullOrWhiteSpace(replacedCharacterId);
            gameContext.runState.mageReplacedCharacterId = replacedCharacterId;
            gameContext.runState.mageRewardType = rewardType;
            gameContext.buildSelection.MarkUnitDiscovered(MageCharacterFactory.GetMageVariant(rewardType).id);
            gameContext.runState.isMageValidationStarted = true;
            StartBattlePhase(GamePhase.Battle04);
        }

        public void CompleteBattle05()
        {
            EnsureContext();
            gameContext.runState.isBattle05Complete = true;
            gameContext.runState.isBattle05Started = false;
            gameContext.runState.isDrop03Spawned = true;
            gameContext.runState.phase = GamePhase.Drop03;
        }

        public void ClaimGiantKey()
        {
            EnsureContext();
            gameContext.buildSelection.hasClaimedGiantKey = true;
            gameContext.buildSelection.pendingRewardType = RewardType.GiantKey;
            gameContext.runState.isDrop03Claimed = true;
            gameContext.runState.phase = GamePhase.Build03;
            SceneManager.LoadScene(buildSceneName);
        }

        public void ConfirmGiantKeySelection(CharacterConfig selectedCharacter)
        {
            EnsureContext();
            gameContext.buildSelection.selectedCharacter = selectedCharacter;
            gameContext.buildSelection.selectedKeyCharacter = GiantKeyCharacterFactory.GetKeyVariant(selectedCharacter);
            gameContext.buildSelection.MarkUnitDiscovered(gameContext.buildSelection.selectedKeyCharacter != null
                ? gameContext.buildSelection.selectedKeyCharacter.id
                : null);
            gameContext.buildSelection.selectedKeyTargetId = selectedCharacter != null ? selectedCharacter.id : null;
            gameContext.buildSelection.pendingRewardType = RewardType.None;
            StartBattlePhase(GamePhase.Battle06);
        }

        public void CompleteBattle06()
        {
            EnsureContext();
            gameContext.runState.isBattle06Complete = true;
            gameContext.runState.isBattle06Started = false;
            gameContext.runState.phase = GamePhase.Ending;
            SceneManager.LoadScene(resultSceneName);
        }

        public bool IsBattle02()
        {
            return IsBattlePhase(GamePhase.Battle02, gameContext != null && gameContext.runState.isBattle02Started);
        }

        public bool IsBattle03()
        {
            return IsBattlePhase(GamePhase.Battle03, gameContext != null && gameContext.runState.isBattle03Started);
        }

        public bool IsBattle04()
        {
            return IsBattlePhase(GamePhase.Battle04, gameContext != null && gameContext.runState.isBattle04Started);
        }

        public bool IsBattle05()
        {
            return IsBattlePhase(GamePhase.Battle05, gameContext != null && gameContext.runState.isBattle05Started);
        }

        public bool IsBattle06()
        {
            return IsBattlePhase(GamePhase.Battle06, gameContext != null && gameContext.runState.isBattle06Started);
        }

        private bool IsBattlePhase(GamePhase phase, bool startedFlag)
        {
            EnsureContext();
            return gameContext.runState.phase == phase || startedFlag;
        }

        private void StartBattlePhase(GamePhase phase)
        {
            EnsureContext();
            gameContext.runState.phase = phase;
            SetBattleStartedFlag(phase, true);
            Debug.Log($"[Flow] Start battle phase: {phase}, loading={battleSceneName}");
            SceneManager.LoadScene(battleSceneName);
        }

        private void SetBattleStartedFlag(GamePhase phase, bool value)
        {
            switch (phase)
            {
                case GamePhase.Battle02:
                    gameContext.runState.isBattle02Started = value;
                    break;
                case GamePhase.Battle03:
                    gameContext.runState.isBattle03Started = value;
                    break;
                case GamePhase.Battle04:
                    gameContext.runState.isBattle04Started = value;
                    break;
                case GamePhase.Battle05:
                    gameContext.runState.isBattle05Started = value;
                    break;
                case GamePhase.Battle06:
                    gameContext.runState.isBattle06Started = value;
                    break;
            }
        }

        private void EnsureContext()
        {
            if (gameContext == null)
            {
                gameContext = DemoGameContext.Instance;
            }

            if (gameContext == null)
            {
                var go = new GameObject("DemoGameContext");
                gameContext = go.AddComponent<DemoGameContext>();
            }

            if (gameContext == null)
            {
                Debug.LogError("DemoFlowController could not find or create DemoGameContext.");
            }
        }
    }
}
