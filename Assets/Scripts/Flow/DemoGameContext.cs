using UnityEngine;
using ProjectHunt.Data;

namespace ProjectHunt.Flow
{
    /// <summary>
    /// Shared runtime container for the current demo run.
    /// Keep this intentionally small for the prototype.
    /// </summary>
    public sealed class DemoGameContext : MonoBehaviour
    {
        public static DemoGameContext Instance { get; private set; }

        [Header("Static Config")]
        public BattleFormationConfig defaultBattleFormation;

        [Header("Runtime State")]
        public BuildSelectionState buildSelection = new BuildSelectionState();
        public DemoRunState runState = new DemoRunState();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void ResetRun()
        {
            buildSelection.Reset();
            runState.Reset();
        }
    }
}
