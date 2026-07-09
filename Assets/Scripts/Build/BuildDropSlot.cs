using UnityEngine;
using UnityEngine.EventSystems;

namespace ProjectHunt.Build
{
    public sealed class BuildDropSlot : MonoBehaviour, IDropHandler
    {
        public BuildSelectionController selectionController;
        public BuildCharacterSlotView characterSlotView;

        public void OnDrop(PointerEventData eventData)
        {
            if (selectionController == null || characterSlotView == null)
            {
                return;
            }

            selectionController.SelectCharacter(characterSlotView);
        }
    }
}
