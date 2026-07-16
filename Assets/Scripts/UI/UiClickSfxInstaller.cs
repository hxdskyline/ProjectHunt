using System.Collections;
using ProjectHunt.Battle;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProjectHunt.UI
{
    public sealed class UiClickSfxInstaller : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (FindObjectOfType<UiClickSfxInstaller>() != null)
            {
                return;
            }

            var host = new GameObject("UiClickSfxInstaller");
            DontDestroyOnLoad(host);
            host.AddComponent<UiClickSfxInstaller>();
        }

        private IEnumerator Start()
        {
            while (true)
            {
                var buttons = FindObjectsOfType<Button>(true);
                foreach (var button in buttons)
                {
                    if (button.GetComponent<UiClickSfxTarget>() == null)
                    {
                        button.gameObject.AddComponent<UiClickSfxTarget>();
                    }
                }

                yield return new WaitForSecondsRealtime(0.5f);
            }
        }
    }

    public sealed class UiClickSfxTarget : MonoBehaviour, IPointerClickHandler
    {
        public void OnPointerClick(PointerEventData eventData)
        {
            var button = GetComponent<Button>();
            if (button != null && button.IsInteractable())
            {
                BattleSfx.PlayUiClick();
            }
        }
    }
}
