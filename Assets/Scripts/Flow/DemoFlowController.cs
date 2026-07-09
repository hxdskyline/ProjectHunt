using ProjectHunt.Data;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectHunt.Flow
{
    public sealed class DemoFlowController : MonoBehaviour
    {
        [Header("Context")]
        public DemoGameContext gameContext;

        [Header("Scene Names")]
        public string mainMenuSceneName = "MainMenu";
        public string battleSceneName = "BattleScene";
        public string buildSceneName = "BuildScene";
        public string resultSceneName = "ResultScene";

        private void Awake()
        {
            if (gameContext == null)
            {
                gameContext = DemoGameContext.Instance;
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
            gameContext.runState.isDropClaimed = true;
            gameContext.runState.phase = GamePhase.Build;
            SceneManager.LoadScene(buildSceneName);
        }

        public void ConfirmBuildSelection(CharacterConfig selectedCharacter)
        {
            EnsureContext();
            gameContext.buildSelection.selectedCharacter = selectedCharacter;
            gameContext.buildSelection.isSelectionConfirmed = selectedCharacter != null;
            gameContext.runState.phase = GamePhase.Battle02;
            gameContext.runState.isBattle02Started = true;
            SceneManager.LoadScene(battleSceneName);
        }

        public void CompleteBattle02()
        {
            EnsureContext();
            gameContext.runState.isBattle02Complete = true;
            gameContext.runState.phase = GamePhase.Result;
            SceneManager.LoadScene(resultSceneName);
        }

        public bool IsBattle02()
        {
            EnsureContext();
            return gameContext.runState.phase == GamePhase.Battle02 || gameContext.runState.isBattle02Started;
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
