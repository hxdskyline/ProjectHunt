using System.Collections;
using ProjectHunt.Build;
using ProjectHunt.Data;
using ProjectHunt.Flow;
using ProjectHunt.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProjectHunt.Battle
{
    /// <summary>Demo-specific mage rescue and equipment-validation choice.</summary>
    public sealed class MageRecruitmentPresentation : MonoBehaviour
    {
        private DemoFlowController _flow;
        private DemoGameContext _context;
        private Text _title;
        private Text _line;
        private Transform _buttonRoot;
        private GameObject _sceneMage;
        private GameObject _dialogueCanvas;
        private Canvas _canvas;
        private GameObject _selectionPanel;
        private DiscoverUnitPresentationController _discoverPresentation;

        public static void Show(DemoFlowController flow, DemoGameContext context)
        {
            ProjectHunt.UI.BattleHudLayoutController.NotifyRescuePhase();
            var host = new GameObject("MageRecruitmentPresentation");
            var presentation = host.AddComponent<MageRecruitmentPresentation>();
            presentation._flow = flow;
            presentation._context = context;
            presentation.StartCoroutine(presentation.EnterMageRoutine());
        }

        private IEnumerator EnterMageRoutine()
        {
            _sceneMage = new GameObject("RescuedMage");
            var renderer = _sceneMage.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = 9;
            _sceneMage.transform.position = new Vector3(6.2f, -1.28f, 0f);
            _sceneMage.transform.localEulerAngles = new Vector3(0f, 180f, 0f);
            _sceneMage.transform.localScale = Vector3.one * 2.5f;
            var animator = _sceneMage.AddComponent<PixelUnitAnimator>();
            animator.Configure("mage_blue");
            animator.PlayLoop("walk");

            var destination = new Vector3(3.75f, -1.28f, 0f);
            while (_sceneMage != null && Vector3.Distance(_sceneMage.transform.position, destination) > 0.02f)
            {
                _sceneMage.transform.position = Vector3.MoveTowards(
                    _sceneMage.transform.position,
                    destination,
                    3.25f * Time.deltaTime);
                yield return null;
            }

            yield return PlayArrivalDialogue();
            CreateUi();
            ShowReplacement();
        }

        private IEnumerator PlayArrivalDialogue()
        {
            _dialogueCanvas = new GameObject("MageArrivalDialogue", typeof(Canvas), typeof(CanvasScaler));
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
            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = new Vector2(0f, 205f);
            panelRect.sizeDelta = new Vector2(980f, 184f);
            var panelImage = panel.GetComponent<Image>();
            panelImage.sprite = SimpleSpriteFactory.GetWhitePixelSprite();
            panelImage.color = new Color(0.035f, 0.04f, 0.06f, 0.97f);
            var outline = panel.AddComponent<Outline>();
            outline.effectColor = new Color(0.88f, 0.58f, 0.2f, 0.95f);
            outline.effectDistance = new Vector2(4f, -4f);

            var speakerTag = CreateImage(
                panel.transform,
                "SpeakerTag",
                new Vector2(142f, 48f),
                new Color(0.78f, 0.4f, 0.12f, 1f));
            var tagRect = speakerTag.GetComponent<RectTransform>();
            tagRect.anchorMin = tagRect.anchorMax = new Vector2(0f, 1f);
            tagRect.pivot = new Vector2(0f, 1f);
            tagRect.anchoredPosition = new Vector2(28f, -20f);
            speakerTag.GetComponent<Outline>().enabled = false;
            var speaker = CreateText(speakerTag.transform, "SpeakerName", new Vector2(142f, 48f), 26, Vector2.zero);
            speaker.fontStyle = FontStyle.Bold;
            speaker.color = new Color(1f, 0.94f, 0.78f, 1f);
            speaker.text = "法师";

            var line = CreateText(panel.transform, "Text", new Vector2(896f, 92f), 29, new Vector2(0f, -26f));
            line.alignment = TextAnchor.MiddleLeft;
            line.horizontalOverflow = HorizontalWrapMode.Wrap;
            line.verticalOverflow = VerticalWrapMode.Truncate;
            line.text = "这一带被一个抱着巨大钥匙的怪物占领了，快随我去消灭它！";
            yield return new WaitForSeconds(2.7f);
            Destroy(_dialogueCanvas);
            _dialogueCanvas = null;
        }

        private void CreateUi()
        {
            EnsureEventSystem();
            var canvasGo = new GameObject(
                "MageRecruitmentCanvas",
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            _canvas = canvasGo.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 210;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;

            var dim = CreateImage(_canvas.transform, "Dim", new Vector2(1080f, 1920f), new Color(0.015f, 0.02f, 0.035f, 0.74f));
            dim.GetComponent<Outline>().enabled = false;
            var panel = CreateImage(_canvas.transform, "Panel", new Vector2(940f, 760f), new Color(0.05f, 0.06f, 0.1f, 0.98f));
            _selectionPanel = panel;
            panel.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 190f);
            panel.GetComponent<Outline>().effectColor = new Color(0.42f, 0.75f, 1f, 0.8f);
            panel.GetComponent<Outline>().effectDistance = new Vector2(4f, -4f);
            _title = CreateText(panel.transform, "Title", new Vector2(840f, 70f), 38, new Vector2(0f, 290f));
            _line = CreateText(panel.transform, "Line", new Vector2(820f, 108f), 25, new Vector2(0f, 205f));
            _buttonRoot = new GameObject("Buttons", typeof(RectTransform)).transform;
            _buttonRoot.SetParent(panel.transform, false);
            var buttonRootRect = (RectTransform)_buttonRoot;
            buttonRootRect.anchorMin = Vector2.zero;
            buttonRootRect.anchorMax = Vector2.one;
            buttonRootRect.offsetMin = Vector2.zero;
            buttonRootRect.offsetMax = Vector2.zero;
        }

        private void ShowReplacement()
        {
            _title.text = "\u6cd5\u5e08\u52a0\u5165\uff0c\u66ff\u6362\u8c01\u4e0b\u573a\uff1f";
            _line.text = "\u9009\u62e9\u4e00\u540d\u961f\u5458\u66ff\u6362\uff0c\u6216\u9009\u62e9\u4e0d\u66ff\u6362\u3002";
            ClearButtons();
            var formation = _context.defaultBattleFormation;
            AddButton(formation.frontCharacter, -215f, 70f);
            AddButton(formation.midCharacter, 215f, 70f);
            AddButton(formation.backCharacter, -215f, -65f);
            AddButton("\u4e0d\u66ff\u6362", 215f, -65f, () => Complete(null, RewardType.None));
        }

        private void ShowDiscoverReward(CharacterConfig replaced, RewardType reward)
        {
            if (_canvas == null)
            {
                return;
            }

            _selectionPanel.SetActive(false);
            if (_discoverPresentation == null)
            {
                _discoverPresentation = DiscoverUnitPresentationController.Create(_canvas);
                _discoverPresentation.transform.SetParent(transform, false);
            }

            var mage = MageCharacterFactory.GetMageVariant(RewardType.None);
            var result = MageCharacterFactory.GetMageVariant(reward);
            _discoverPresentation.PlayDiscoverSequence(
                mage,
                result,
                reward,
                ReturnToReplacementChoice,
                () => Complete(replaced, reward));
        }

        private void ReturnToReplacementChoice()
        {
            if (_selectionPanel != null)
            {
                _selectionPanel.SetActive(true);
            }

            ShowReplacement();
        }

        private void Complete(CharacterConfig replaced, RewardType reward)
        {
            var replacedId = replaced != null ? replaced.id : null;
            Debug.Log($"[MageRecruitment] Confirmed replacement={replacedId ?? "none"}, mage reward={reward}.");
            _flow.RecruitMage(replacedId, reward);
            Destroy(_sceneMage);
            Destroy(gameObject);
        }

        private void AddButton(CharacterConfig config, float x, float y)
        {
            AddButton("\u66ff\u6362" + config.displayName, x, y, () => ShowDiscoverReward(config, RewardType.None));
        }

        private void AddButton(string label, float x, float y, UnityEngine.Events.UnityAction action)
        {
            var go = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(_buttonRoot, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(360f, 92f);
            var image = go.GetComponent<Image>();
            image.sprite = ExtractedArtLibrary.LoadUi("button_primary") ?? SimpleSpriteFactory.GetWhitePixelSprite();
            image.color = Color.white;
            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 0.76f, 0.3f, 0.9f);
            outline.effectDistance = new Vector2(3f, -3f);
            go.GetComponent<Button>().onClick.AddListener(action);
            var text = CreateText(go.transform, "Text", new Vector2(330f, 76f), 27, Vector2.zero);
            text.text = label;
        }

        private void ClearButtons()
        {
            for (var i = _buttonRoot.childCount - 1; i >= 0; i--)
            {
                var button = _buttonRoot.GetChild(i).gameObject;
                // Destroy is deferred until frame end; hide first so choice groups never overlap.
                button.SetActive(false);
                Destroy(button);
            }
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindObjectOfType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        private static GameObject CreateImage(Transform parent, string name, Vector2 size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Outline));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            var image = go.GetComponent<Image>();
            image.sprite = SimpleSpriteFactory.GetWhitePixelSprite();
            image.color = color;
            return go;
        }

        private static Text CreateText(Transform parent, string name, Vector2 size, int fontSize, Vector2 position)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.font = BuildUiRuntimeStyle.GetChineseFont();
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.rectTransform.sizeDelta = size;
            text.rectTransform.anchoredPosition = position;
            return text;
        }
    }
}
