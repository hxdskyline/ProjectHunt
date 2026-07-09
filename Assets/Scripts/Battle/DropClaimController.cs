using ProjectHunt.Flow;
using UnityEngine;

namespace ProjectHunt.Battle
{
    public sealed class DropClaimController : MonoBehaviour
    {
        [Header("Flow")]
        public DemoFlowController flowController;

        [Header("Optional Visuals")]
        public GameObject dropVisualRoot;

        private bool _isClaimed;

        private void Awake()
        {
            if (flowController == null)
            {
                flowController = FindObjectOfType<DemoFlowController>();
            }
        }

        public void ClaimDrop()
        {
            if (_isClaimed)
            {
                return;
            }

            _isClaimed = true;

            if (dropVisualRoot != null)
            {
                dropVisualRoot.SetActive(false);
            }
            else
            {
                gameObject.SetActive(false);
            }

            if (flowController != null)
            {
                flowController.ClaimMeteorHammer();
            }
            else
            {
                Debug.LogError("DropClaimController could not find DemoFlowController.");
            }
        }

        private void OnMouseUpAsButton()
        {
            ClaimDrop();
        }
    }
}
