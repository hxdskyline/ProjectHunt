using ProjectHunt.Flow;
using UnityEngine;

namespace ProjectHunt.UI
{
    public sealed class SceneFlowButtonAction : MonoBehaviour
    {
        public enum ActionType
        {
            StartRun = 0,
            ReturnToMainMenu = 1,
        }

        public ActionType actionType;

        public void Execute()
        {
            var flow = FindObjectOfType<DemoFlowController>();
            if (flow == null && DemoGameContext.Instance != null)
            {
                flow = DemoGameContext.Instance.GetComponent<DemoFlowController>();
            }

            if (flow == null)
            {
                Debug.LogError("SceneFlowButtonAction could not find DemoFlowController.");
                return;
            }

            switch (actionType)
            {
                case ActionType.ReturnToMainMenu:
                    flow.ReturnToMainMenu();
                    break;
                default:
                    flow.StartNewRun();
                    break;
            }
        }
    }
}
