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
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            _dialogueRoot = new GameObject("DialoguePanel", typeof(RectTransform), typeof(Image));
            _dialogueRoot.transform.SetParent(canvasGo.transform, false);
            var panelRect = _dialogueRoot.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0f);
            panelRect.anchorMax = new Vector2(0.5f, 0f);
            panelRect.pivot = new Vector2(0.5f, 0f);
            panelRect.anchoredPosition = new Vector2(0f, 7f);
            panelRect.sizeDelta = new Vector2(820f, 112f);
            var panel = _dialogueRoot.GetComponent<Image>();
            panel.sprite = SimpleSpriteFactory.GetWhitePixelSprite();
            panel.color = new Color(0.04f, 0.04f, 0.06f, 0.9f);
            var outline = _dialogueRoot.AddComponent<Outline>();
            outline.effectColor = new Color(0.72f, 0.56f, 0.28f, 0.7f);
            outline.effectDistance = new Vector2(1f, -1f);
            outline.enabled = false;

            _speakerText = CreateText("Speaker", new Vector2(760f, 34f), 25);
            _speakerText.transform.SetParent(_dialogueRoot.transform, false);
            _speakerText.rectTransform.anchoredPosition = new Vector2(0f, 33f);
            _speakerText.color = new Color(1f, 0.84f, 0.48f, 1f);
            _speakerText.text = "铁匠";

            _speakerText.gameObject.SetActive(false);

            _lineText = CreateText("Line", new Vector2(820f, 112f), 28);
            _lineText.transform.SetParent(_dialogueRoot.transform, false);
            _lineText.rectTransform.anchoredPosition = Vector2.zero;
            _lineText.alignment = TextAnchor.MiddleCenter;
            _lineText.color = Color.white;
        }

        private IEnumerator ShowLine(string line, float duration)
        {
            _dialogueRoot.SetActive(true);
            _lineText.text = "\u94c1\u5320\uff1a" + line;
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
