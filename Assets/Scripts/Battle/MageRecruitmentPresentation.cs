using System.Collections;
using ProjectHunt.Build;
using ProjectHunt.Data;
using ProjectHunt.Flow;
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
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(_dialogueCanvas.transform, false);
            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0f);
            panelRect.pivot = new Vector2(0.5f, 0f);
            panelRect.anchoredPosition = new Vector2(0f, 7f);
            panelRect.sizeDelta = new Vector2(820f, 112f);
            var panelImage = panel.GetComponent<Image>();
            panelImage.sprite = SimpleSpriteFactory.GetWhitePixelSprite();
            panelImage.color = new Color(0.04f, 0.04f, 0.06f, 0.9f);

            var line = CreateText(panel.transform, "Text", new Vector2(820f, 112f), 28, Vector2.zero);
            line.text = "\u6cd5\u5e08\uff1a\u8fd9\u4e00\u5e26\u88ab\u4e00\u4e2a\u62b1\u7740\u5de8\u5927\u94a5\u5319\u7684\u602a\u7269\u5360\u9886\u4e86\uff0c\u5feb\u968f\u6211\u53bb\u6d88\u706d\u5b83\uff01";
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
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            var panel = CreateImage(_canvas.transform, "Panel", new Vector2(900f, 410f), new Color(0.05f, 0.06f, 0.1f, 0.96f));
            _selectionPanel = panel;
            panel.GetComponent<Outline>().effectColor = new Color(0.42f, 0.75f, 1f, 0.8f);
            _title = CreateText(panel.transform, "Title", new Vector2(800f, 54f), 30, new Vector2(0f, 150f));
            _line = CreateText(panel.transform, "Line", new Vector2(800f, 80f), 22, new Vector2(0f, 84f));
            _buttonRoot = new GameObject("Buttons", typeof(RectTransform)).transform;
            _buttonRoot.SetParent(panel.transform, false);
        }

        private void ShowReplacement()
        {
            _title.text = "\u7b2c 1 \u6b65\uff1a\u6cd5\u5e08\u52a0\u5165\uff0c\u66ff\u6362\u8c01\u4e0b\u573a\uff1f";
            _line.text = "\u9009\u62e9\u4e00\u540d\u961f\u5458\u66ff\u6362\uff0c\u6216\u9009\u62e9\u4e0d\u66ff\u6362\u3002";
            ClearButtons();
            var formation = _context.defaultBattleFormation;
            AddButton(formation.frontCharacter, -270f);
            AddButton(formation.midCharacter, -90f);
            AddButton(formation.backCharacter, 90f);
            AddButton("\u4e0d\u66ff\u6362", 270f, () => Complete(null, RewardType.None));
        }

        private void ShowRewardChoice(CharacterConfig replaced)
        {
            Debug.Log($"[MageRecruitment] Replacing {replaced.displayName}; showing equipment choice.");
            _title.text = "\u7b2c 2 \u6b65\uff1a\u6cd5\u5e08\u4f7f\u7528\u54ea\u4ef6\u7269\u54c1\uff1f";
            _line.text = "\u53ef\u7a7a\u624b\u53c2\u6218\uff0c\u4e5f\u53ef\u4f7f\u7528\u6d41\u661f\u9524\u6216\u9152\u795e\u5723\u676f\u3002";
            ClearButtons();
            if (_context.buildSelection.hasClaimedMeteorHammer)
                AddButton("\u63a5\u89e6\u6d41\u661f\u9524", -210f, () => ShowDiscoverReward(replaced, RewardType.MeteorHammer));
            AddButton("\u7a7a\u624b\u52a0\u5165", 0f, () => ShowDiscoverReward(replaced, RewardType.None));
            if (_context.buildSelection.hasClaimedHolyCup)
                AddButton("\u63a5\u89e6\u9152\u795e\u5723\u676f", 210f, () => ShowDiscoverReward(replaced, RewardType.HolyCup));
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
                () => ReturnToRewardChoice(replaced),
                () => Complete(replaced, reward));
        }

        private void ReturnToRewardChoice(CharacterConfig replaced)
        {
            if (_selectionPanel != null)
            {
                _selectionPanel.SetActive(true);
            }

            ShowRewardChoice(replaced);
        }

        private void Complete(CharacterConfig replaced, RewardType reward)
        {
            var replacedId = replaced != null ? replaced.id : null;
            Debug.Log($"[MageRecruitment] Confirmed replacement={replacedId ?? "none"}, mage reward={reward}.");
            _flow.RecruitMage(replacedId, reward);
            Destroy(_sceneMage);
            Destroy(gameObject);
        }

        private void AddButton(CharacterConfig config, float x)
        {
            AddButton("\u66ff\u6362" + config.displayName, x, () => ShowRewardChoice(config));
        }

        private void AddButton(string label, float x, UnityEngine.Events.UnityAction action)
        {
            var go = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(_buttonRoot, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(x, -92f);
            rect.sizeDelta = new Vector2(180f, 54f);
            var image = go.GetComponent<Image>();
            image.sprite = SimpleSpriteFactory.GetWhitePixelSprite();
            image.color = new Color(0.95f, 0.55f, 0.16f, 1f);
            go.GetComponent<Button>().onClick.AddListener(action);
            var text = CreateText(go.transform, "Text", new Vector2(170f, 48f), 20, Vector2.zero);
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
