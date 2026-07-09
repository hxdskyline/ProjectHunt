using System.Collections.Generic;
using ProjectHunt.Data;
using ProjectHunt.Flow;
using ProjectHunt.Battle;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProjectHunt.Build
{
    public sealed class BuildSelectionController : MonoBehaviour
    {
        [Header("Flow")]
        public DemoFlowController flowController;
        public DemoGameContext gameContext;

        [Header("Slots")]
        public List<BuildCharacterSlotView> characterSlots = new List<BuildCharacterSlotView>();

        [Header("UI")]
        public Button confirmButton;
        public Image dragItemVisual;
        public Sprite meteorHammerSprite;

        private CharacterConfig _selectedCharacter;
        private Vector3 _dragStartPosition;
        private Transform _dragStartParent;

        private void Awake()
        {
            if (flowController == null)
            {
                flowController = FindObjectOfType<DemoFlowController>();
            }

            if (gameContext == null)
            {
                gameContext = DemoGameContext.Instance;
            }
        }

        private void Start()
        {
            if (meteorHammerSprite == null && dragItemVisual != null)
            {
                meteorHammerSprite = SimpleSpriteFactory.GetMeteorHammerSprite();
                dragItemVisual.sprite = meteorHammerSprite;
            }

            ResetView();
        }

        public void ResetView()
        {
            _selectedCharacter = null;

            for (var i = 0; i < characterSlots.Count; i++)
            {
                if (characterSlots[i] == null)
                {
                    continue;
                }

                characterSlots[i].SetSelected(false);
                characterSlots[i].SetWeaponPreview(null, false);
            }

            RefreshConfirmButton();
        }

        public void SelectCharacter(BuildCharacterSlotView slot)
        {
            if (slot == null || slot.characterConfig == null)
            {
                return;
            }

            _selectedCharacter = slot.characterConfig;

            for (var i = 0; i < characterSlots.Count; i++)
            {
                var current = characterSlots[i];
                if (current == null)
                {
                    continue;
                }

                var isSelected = current == slot;
                current.SetSelected(isSelected);
                current.SetWeaponPreview(meteorHammerSprite, isSelected && meteorHammerSprite != null);
            }

            RefreshConfirmButton();
        }

        public void ConfirmSelection()
        {
            if (_selectedCharacter == null || flowController == null)
            {
                return;
            }

            flowController.ConfirmBuildSelection(_selectedCharacter);
        }

        public void CacheDragStart(RectTransform dragItem)
        {
            if (dragItem == null)
            {
                return;
            }

            _dragStartPosition = dragItem.position;
            _dragStartParent = dragItem.parent;
        }

        public void RestoreDragStart(RectTransform dragItem)
        {
            if (dragItem == null)
            {
                return;
            }

            dragItem.SetParent(_dragStartParent);
            dragItem.position = _dragStartPosition;
        }

        private void RefreshConfirmButton()
        {
            if (confirmButton != null)
            {
                confirmButton.interactable = _selectedCharacter != null;
            }
        }
    }
}
