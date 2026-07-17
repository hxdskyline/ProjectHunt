using System.Collections;
using ProjectHunt.Build;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectHunt.Battle
{
    /// <summary>
    /// A short story beat between the holy-cup ground drop and the shared reward presentation.
    /// Human militia is deliberately a temporary blacksmith stand-in until bespoke art is available.
    /// </summary>
    public sealed class HolyCupInspectionPresentation : MonoBehaviour
    {
        private DropClaimController _drop;
        private GameObject _blacksmith;
        private PixelUnitAnimator _blacksmithAnimator;
        private Canvas _canvas;
        private GameObject _dialogueRoot;
        private Text _speakerText;
        private Text _lineText;

        public static void Play(DropClaimController drop)
        {
            if (drop == null)
            {
                return;
            }

            var host = new GameObject("HolyCupInspectionPresentation");
            var presentation = host.AddComponent<HolyCupInspectionPresentation>();
            presentation._drop = drop;
            presentation.StartCoroutine(presentation.PlayRoutine());
        }

        private IEnumerator PlayRoutine()
        {
            CreateDialogueUi();
            CreateBlacksmith();

            var cupPosition = _drop.transform.position;
            var inspectionPosition = cupPosition + new Vector3(-0.62f, 0f, 0f);
            var entryPosition = inspectionPosition + Vector3.left * 2.2f;
            _blacksmith.transform.position = entryPosition;
            _blacksmithAnimator.PlayLoop("walk");

            while (_blacksmith != null && Vector3.Distance(_blacksmith.transform.position, inspectionPosition) > 0.02f)
            {
                _blacksmith.transform.position = Vector3.MoveTowards(
                    _blacksmith.transform.position,
                    inspectionPosition,
                    3.4f * Time.deltaTime);
                yield return null;
            }

            if (_blacksmith != null)
            {
                _blacksmith.transform.position = inspectionPosition;
                _blacksmithAnimator.PlayLoop("walk");
            }

            yield return ShowLine("等一下。", 0.95f);

            // Picking up and turning the cup is conveyed through its position and rotation.
            _drop.transform.SetParent(_blacksmith.transform, true);
            _drop.transform.localPosition = new Vector3(0.36f, 0.25f, 0f);
            _drop.transform.localScale = Vector3.one * 0.85f;
            _drop.transform.localEulerAngles = new Vector3(0f, 0f, 180f);
            yield return new WaitForSeconds(0.38f);
            _drop.transform.localEulerAngles = new Vector3(0f, 0f, 0f);
            yield return ShowLine("哈哈哈哈。", 0.75f);
            yield return ShowLine("这食人魔，一直拿这东西喝酒。", 1.45f);
            yield return ShowLine("你们可别学他。", 1.15f);

            HideDialogue();
            _drop.transform.SetParent(null, true);
            _drop.PlayAutoClaimSequence();
            Destroy(_blacksmith);
            Destroy(_dialogueRoot);
            Destroy(gameObject);
        }

        private void CreateBlacksmith()
        {
            _blacksmith = new GameObject("BlacksmithPlaceholder");
            var renderer = _blacksmith.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = 9;
            _blacksmith.transform.localScale = Vector3.one * 2.35f;
            _blacksmithAnimator = _blacksmith.AddComponent<PixelUnitAnimator>();
            _blacksmithAnimator.Configure("human_militia");
        }

        private void CreateDialogueUi()
        {
            var canvasGo = new GameObject("HolyCupDialogueCanvas", typeof(Canvas), typeof(CanvasScaler));
            _canvas = canvasGo.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 180;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;

            _dialogueRoot = new GameObject("DialoguePanel", typeof(RectTransform), typeof(Image));
            _dialogueRoot.transform.SetParent(canvasGo.transform, false);
            var panelRect = _dialogueRoot.GetComponent<RectTransform>();
            panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = new Vector2(0f, -88f);
            panelRect.sizeDelta = new Vector2(980f, 184f);
            var panel = _dialogueRoot.GetComponent<Image>();
            panel.sprite = SimpleSpriteFactory.GetWhitePixelSprite();
            panel.color = new Color(0.035f, 0.04f, 0.06f, 0.97f);
            var outline = _dialogueRoot.AddComponent<Outline>();
            outline.effectColor = new Color(0.88f, 0.58f, 0.2f, 0.95f);
            outline.effectDistance = new Vector2(4f, -4f);

            var speakerTag = new GameObject("SpeakerTag", typeof(RectTransform), typeof(Image));
            speakerTag.transform.SetParent(_dialogueRoot.transform, false);
            var tagRect = speakerTag.GetComponent<RectTransform>();
            tagRect.anchorMin = tagRect.anchorMax = new Vector2(0f, 1f);
            tagRect.pivot = new Vector2(0f, 1f);
            tagRect.anchoredPosition = new Vector2(28f, -20f);
            tagRect.sizeDelta = new Vector2(142f, 48f);
            speakerTag.GetComponent<Image>().color = new Color(0.78f, 0.4f, 0.12f, 1f);

            _speakerText = CreateText("Speaker", new Vector2(142f, 48f), 26);
            _speakerText.transform.SetParent(speakerTag.transform, false);
            _speakerText.rectTransform.anchorMin = Vector2.zero;
            _speakerText.rectTransform.anchorMax = Vector2.one;
            _speakerText.rectTransform.offsetMin = Vector2.zero;
            _speakerText.rectTransform.offsetMax = Vector2.zero;
            _speakerText.fontStyle = FontStyle.Bold;
            _speakerText.color = new Color(1f, 0.94f, 0.78f, 1f);
            _speakerText.text = "铁匠";

            _lineText = CreateText("Line", new Vector2(896f, 92f), 29);
            _lineText.transform.SetParent(_dialogueRoot.transform, false);
            _lineText.rectTransform.anchorMin = Vector2.zero;
            _lineText.rectTransform.anchorMax = Vector2.one;
            _lineText.rectTransform.offsetMin = new Vector2(42f, 20f);
            _lineText.rectTransform.offsetMax = new Vector2(-42f, -72f);
            _lineText.alignment = TextAnchor.MiddleLeft;
            _lineText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _lineText.verticalOverflow = VerticalWrapMode.Truncate;
            _lineText.color = Color.white;
        }

        private IEnumerator ShowLine(string line, float duration)
        {
            _dialogueRoot.SetActive(true);
            _lineText.text = line;
            yield return new WaitForSeconds(duration);
        }

        private void HideDialogue()
        {
            if (_dialogueRoot != null)
            {
                _dialogueRoot.SetActive(false);
            }
        }

        private static Text CreateText(string name, Vector2 size, int fontSize)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            var text = go.GetComponent<Text>();
            text.font = BuildUiRuntimeStyle.GetChineseFont();
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.rectTransform.sizeDelta = size;
            return text;
        }
    }
}
