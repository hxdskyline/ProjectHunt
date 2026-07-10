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
                // Keep this scene-local instance, destroy the old persistent one so scene references stay valid.
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
            gameContext.buildSelection.isSelectionConfirmed = selectedCharacter != null;
            gameContext.buildSelection.hasClaimedMeteorHammer = selectedCharacter != null;
            gameContext.buildSelection.pendingRewardType = RewardType.None;
            gameContext.runState.phase = GamePhase.Battle02;
            gameContext.runState.isBattle02Started = true;
            Debug.Log($"[Flow] ConfirmBuildSelection: phase={gameContext.runState.phase}, isBattle02Started={gameContext.runState.isBattle02Started}, loading={battleSceneName}");
            SceneManager.LoadScene(battleSceneName);
        }

        public void CompleteBattle02()
        {
            EnsureContext();
            gameContext.runState.isBattle02Complete = true;
            gameContext.runState.isBattle02Started = false;
            gameContext.runState.isDrop02Spawned = true;
            gameContext.runState.phase = GamePhase.Drop02;
        }

        public void ClaimFireGland()
        {
            EnsureContext();
            gameContext.buildSelection.hasClaimedFireGland = true;
            gameContext.buildSelection.pendingRewardType = RewardType.FireGland;
            gameContext.runState.isDrop02Claimed = true;
            gameContext.runState.phase = GamePhase.Build02;
            SceneManager.LoadScene(buildSceneName);
        }

        public void ConfirmFireGlandSelection(CharacterConfig selectedCharacter)
        {
            EnsureContext();
            gameContext.buildSelection.selectedCharacter = selectedCharacter;
            gameContext.buildSelection.selectedFireCharacter = FireGlandCharacterFactory.GetFireVariant(selectedCharacter);
            gameContext.buildSelection.pendingRewardType = RewardType.None;
            gameContext.runState.phase = GamePhase.Result;
            SceneManager.LoadScene(resultSceneName);
        }

        public bool IsBattle02()
        {
            EnsureContext();
            var result = gameContext.runState.phase == GamePhase.Battle02 || gameContext.runState.isBattle02Started;
            Debug.Log($"[Flow] IsBattle02={result} (phase={gameContext.runState.phase}, isBattle02Started={gameContext.runState.isBattle02Started})");
            return result;
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
