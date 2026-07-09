using ProjectHunt.Data;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectHunt.Build
{
    public sealed class BuildCharacterSlotView : MonoBehaviour
    {
        [Header("Data")]
        public CharacterConfig characterConfig;

        [Header("UI")]
        public GameObject selectedHighlight;
        public Image weaponPreview;

        public void SetSelected(bool isSelected)
        {
            if (selectedHighlight != null)
            {
                selectedHighlight.SetActive(isSelected);
            }
        }

        public void SetWeaponPreview(Sprite sprite, bool enabled)
        {
            if (weaponPreview == null)
            {
                return;
            }

            weaponPreview.sprite = sprite;
            weaponPreview.enabled = enabled;
        }
    }
}
