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
            yield return Say("\u52c7\u58eb\u4eec\uff0c\u8bf7\u5e2e\u5e2e\u6211\u4eec\uff01", 1.55f);
            yield return Say("\u6211\u4eec\u7684\u6751\u5b50\uff0c\u88ab\u4e00\u4e2a\u7231\u559d\u9152\u7684\u602a\u7269\u7ed9\u70e7\u4e86\u3002", 1.1f);
            yield return Say("\u90a3\u5bb6\u4f19\u5f80\u5317\u8fb9\u53bb\u4e86\uff0c\u6211\u7ed9\u4f60\u4eec\u5e26\u8def\u3002", 1.3f);
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
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;
            var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(_dialogueCanvas.transform, false);
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, -88f);
            rect.sizeDelta = new Vector2(980f, 184f);
            var image = panel.GetComponent<Image>();
            image.sprite = SimpleSpriteFactory.GetWhitePixelSprite();
            image.color = new Color(0.035f, 0.04f, 0.06f, 0.97f);
            var outline = panel.AddComponent<Outline>();
            outline.effectColor = new Color(0.88f, 0.58f, 0.2f, 0.95f);
            outline.effectDistance = new Vector2(4f, -4f);

            var speakerTag = new GameObject("SpeakerTag", typeof(RectTransform), typeof(Image));
            speakerTag.transform.SetParent(panel.transform, false);
            var tagRect = speakerTag.GetComponent<RectTransform>();
            tagRect.anchorMin = tagRect.anchorMax = new Vector2(0f, 1f);
            tagRect.pivot = new Vector2(0f, 1f);
            tagRect.anchoredPosition = new Vector2(28f, -20f);
            tagRect.sizeDelta = new Vector2(142f, 48f);
            var tagImage = speakerTag.GetComponent<Image>();
            tagImage.sprite = SimpleSpriteFactory.GetWhitePixelSprite();
            tagImage.color = new Color(0.78f, 0.4f, 0.12f, 1f);

            var speakerTextObject = new GameObject("SpeakerName", typeof(RectTransform), typeof(Text));
            speakerTextObject.transform.SetParent(speakerTag.transform, false);
            var speakerText = speakerTextObject.GetComponent<Text>();
            speakerText.font = BuildUiRuntimeStyle.GetChineseFont();
            speakerText.fontSize = 26;
            speakerText.fontStyle = FontStyle.Bold;
            speakerText.alignment = TextAnchor.MiddleCenter;
            speakerText.color = new Color(1f, 0.94f, 0.78f, 1f);
            speakerText.text = "\u94c1\u5320";
            speakerText.rectTransform.anchorMin = Vector2.zero;
            speakerText.rectTransform.anchorMax = Vector2.one;
            speakerText.rectTransform.offsetMin = Vector2.zero;
            speakerText.rectTransform.offsetMax = Vector2.zero;
            var textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(panel.transform, false);
            _line = textGo.GetComponent<Text>();
            _line.font = BuildUiRuntimeStyle.GetChineseFont();
            _line.fontSize = 29;
            _line.alignment = TextAnchor.MiddleLeft;
            _line.color = Color.white;
            _line.rectTransform.anchorMin = Vector2.zero;
            _line.rectTransform.anchorMax = Vector2.one;
            _line.rectTransform.offsetMin = new Vector2(42f, 20f);
            _line.rectTransform.offsetMax = new Vector2(-42f, -72f);
            _line.horizontalOverflow = HorizontalWrapMode.Wrap;
            _line.verticalOverflow = VerticalWrapMode.Truncate;
        }

        private IEnumerator Say(string line, float duration)
        {
            _line.text = line;
            yield return new WaitForSeconds(duration);
        }
    }
}
