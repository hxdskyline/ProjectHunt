using System.Collections;
using ProjectHunt.Build;
using ProjectHunt.Flow;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectHunt.Battle
{
    /// <summary>Automatic rescue beat after the burning-village validation waves.</summary>
    public sealed class BlacksmithRescuePresentation : MonoBehaviour
    {
        private DemoFlowController _flow;
        private GameObject _blacksmith;
        private GameObject _dialogueCanvas;
        private Text _line;

        public static void Show(DemoFlowController flow)
        {
            ProjectHunt.UI.BattleHudLayoutController.NotifyRescuePhase();
            var host = new GameObject("BlacksmithRescuePresentation");
            var presentation = host.AddComponent<BlacksmithRescuePresentation>();
            presentation._flow = flow;
            presentation.StartCoroutine(presentation.PlayRoutine());
        }

        private IEnumerator PlayRoutine()
        {
            _blacksmith = new GameObject("RescuedBlacksmithPlaceholder");
            var renderer = _blacksmith.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = 9;
            _blacksmith.transform.position = new Vector3(5.7f, -1.5f, 0f);
            _blacksmith.transform.localEulerAngles = new Vector3(0f, 180f, 0f);
            _blacksmith.transform.localScale = Vector3.one * 2.2f;
            var animator = _blacksmith.AddComponent<PixelUnitAnimator>();
            animator.Configure("human_militia");
            animator.PlayLoop("walk");
            var destination = new Vector3(2.55f, -1.5f, 0f);
            while (Vector3.Distance(_blacksmith.transform.position, destination) > 0.02f)
            {
                _blacksmith.transform.position = Vector3.MoveTowards(
                    _blacksmith.transform.position,
                    destination,
                    3.4f * Time.deltaTime);
                yield return null;
            }

            CreateDialogue();
            yield return Say("\u8fd9\u6751\u5b50\uff0c\u88ab\u4e00\u4e2a\u7231\u559d\u9152\u7684\u602a\u7269\u70e7\u4e86\u3002", 1.55f);
            yield return Say("\u90a3\u5bb6\u4f19\u5f80\u5317\u8fb9\u53bb\u4e86\u3002", 1.1f);
            yield return Say("\u5982\u679c\u4f60\u4eec\u8fd8\u8981\u8ffd\uff0c\u6211\u8ddf\u4f60\u4eec\u4e00\u8d77\u53bb\u3002", 1.3f);
            Destroy(_blacksmith);
            Destroy(_dialogueCanvas);
            _flow?.CompleteBattle02();
            Destroy(gameObject);
        }

        private void CreateDialogue()
        {
            _dialogueCanvas = new GameObject("BlacksmithDialogue", typeof(Canvas), typeof(CanvasScaler));
            var canvas = _dialogueCanvas.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 180;
            var scaler = _dialogueCanvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(_dialogueCanvas.transform, false);
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 7f);
            rect.sizeDelta = new Vector2(820f, 112f);
            var image = panel.GetComponent<Image>();
            image.sprite = SimpleSpriteFactory.GetWhitePixelSprite();
            image.color = new Color(0.04f, 0.04f, 0.06f, 0.9f);
            var textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(panel.transform, false);
            _line = textGo.GetComponent<Text>();
            _line.font = BuildUiRuntimeStyle.GetChineseFont();
            _line.fontSize = 28;
            _line.alignment = TextAnchor.MiddleCenter;
            _line.color = Color.white;
            _line.rectTransform.anchorMin = Vector2.zero;
            _line.rectTransform.anchorMax = Vector2.one;
            _line.rectTransform.offsetMin = Vector2.zero;
            _line.rectTransform.offsetMax = Vector2.zero;
        }

        private IEnumerator Say(string line, float duration)
        {
            _line.text = "\u94c1\u5320\uff1a" + line;
            yield return new WaitForSeconds(duration);
        }
    }
}
