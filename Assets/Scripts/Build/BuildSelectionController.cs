using System;
using System.Collections;
using System.Collections.Generic;
using ProjectHunt.Battle;
using ProjectHunt.Data;
using ProjectHunt.Flow;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectHunt.Build
{
    public sealed class BuildSelectionController : MonoBehaviour
    {
        [Header("Flow")]
        public DemoFlowController flowController;
        public DemoGameContext gameContext;

        [Header("Slots")]
        public List<BuildCharacterSlotView> characterSlots = new();

        [Header("UI")]
        public Button confirmButton;
        public Image dragItemVisual;
        public Sprite meteorHammerSprite;
        public Sprite fireGlandSprite;
        public Text titleText;

        private CharacterConfig _selectedCharacter;
        private Vector3 _dragStartPosition;
        private Transform _dragStartParent;
        private DiscoverUnitPresentationController _discoverPresentationController;
        private RewardType CurrentRewardType => gameContext != null ? gameContext.buildSelection.pendingRewardType : RewardType.None;

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
            if (!ValidateBuildScene())
            {
                return;
            }

            AutoBindUi();

            if (meteorHammerSprite == null)
            {
                meteorHammerSprite = SimpleSpriteFactory.GetMeteorHammerSprite();
            }

            if (fireGlandSprite == null)
            {
                fireGlandSprite = SimpleSpriteFactory.GetHolyCupSprite();
            }

            if (dragItemVisual != null)
            {
                dragItemVisual.sprite = GetRewardSprite(CurrentRewardType);
                dragItemVisual.gameObject.SetActive(false);
            }

            HideCarryoverHintTexts();
            ResetView();
        }

        public void ResetView()
        {
            _selectedCharacter = null;
            ResolveFlowController();
            SyncSlotsWithCurrentRoster();

            if (titleText != null)
            {
                titleText.text = GetBuildTitle(CurrentRewardType);
            }

            if (dragItemVisual != null)
            {
                dragItemVisual.gameObject.SetActive(false);
            }

            if (confirmButton != null)
            {
                confirmButton.interactable = false;
                confirmButton.gameObject.SetActive(false);
            }

            for (var i = 0; i < characterSlots.Count; i++)
            {
                var slot = characterSlots[i];
                if (slot == null)
                {
                    continue;
                }

                slot.Bind(this);
                slot.SetSelected(false);
                slot.SetPortrait(GetPortraitSprite(GetDisplayCharacter(slot.characterConfig)));
                slot.SetTexts(
                    GetDisplayCharacter(slot.characterConfig) != null ? GetDisplayCharacter(slot.characterConfig).displayName : "\u89d2\u8272",
                    GetEffectDescription(CurrentRewardType, slot.characterConfig != null ? slot.characterConfig.roleType : RoleType.Swordsman),
                    GetAssignButtonText(CurrentRewardType, GetDisplayCharacter(slot.characterConfig) != null ? GetDisplayCharacter(slot.characterConfig).displayName : "\u89d2\u8272"));
            }

            ArrangeCharacterSlots();
        }

        public void SelectCharacter(BuildCharacterSlotView slot)
        {
            if (slot == null || slot.characterConfig == null)
            {
                return;
            }

            _selectedCharacter = slot.characterConfig;
        }

        public void ConfirmSelection()
        {
            if (_selectedCharacter == null || flowController == null)
            {
                return;
            }

            flowController.ConfirmBuildSelection(_selectedCharacter);
        }

        public void ShowDiscoverPanel(BuildCharacterSlotView slot)
        {
            if (slot == null || slot.characterConfig == null)
            {
                return;
            }

            var sourceCharacter = GetDisplayCharacter(slot.characterConfig);
            _selectedCharacter = sourceCharacter;
            var resultCharacter = ResolveRewardCharacter(slot.characterConfig, CurrentRewardType);
            SetSelectionUiVisible(false);
            EnsureDiscoverPresentation();
            _discoverPresentationController.PlayDiscoverSequence(
                sourceCharacter,
                resultCharacter,
                CurrentRewardType,
                HandleDiscoverBack,
                ConfirmDiscoveredUnit);
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

        private void ConfirmDiscoveredUnit()
        {
            ResolveFlowController();
            if (_selectedCharacter == null || flowController == null)
            {
                Debug.LogError("[Build] Could not confirm discovered unit.");
                return;
            }

            Debug.Log($"[Build] Discover confirm for {_selectedCharacter.displayName}.");
            if (CurrentRewardType == RewardType.HolyCup)
            {
                flowController.ConfirmHolyCupSelection(_selectedCharacter);
                return;
            }

            if (CurrentRewardType == RewardType.GiantKey)
            {
                flowController.ConfirmGiantKeySelection(_selectedCharacter);
                return;
            }

            flowController.ConfirmBuildSelection(_selectedCharacter);
        }

        private void HandleDiscoverBack()
        {
            SetSelectionUiVisible(true);
            _selectedCharacter = null;
        }

        private void SetSelectionUiVisible(bool isVisible)
        {
            if (titleText != null)
            {
                titleText.gameObject.SetActive(isVisible);
            }

            for (var i = 0; i < characterSlots.Count; i++)
            {
                if (characterSlots[i] != null)
                {
                    characterSlots[i].gameObject.SetActive(isVisible);
                }
            }

            if (dragItemVisual != null)
            {
                dragItemVisual.gameObject.SetActive(false);
            }
        }

        private void AutoBindUi()
        {
            if (titleText == null)
            {
                var titleObject = GameObject.Find("BuildTitle");
                if (titleObject != null)
                {
                    titleText = titleObject.GetComponent<Text>();
                }
            }

            if (titleText != null)
            {
                titleText.font = BuildUiRuntimeStyle.GetChineseFont();
                titleText.fontSize = 22;
                titleText.alignment = TextAnchor.MiddleCenter;
                titleText.horizontalOverflow = HorizontalWrapMode.Overflow;
                titleText.verticalOverflow = VerticalWrapMode.Overflow;
                titleText.lineSpacing = 1f;

                var rect = titleText.rectTransform;
                rect.anchoredPosition = new Vector2(0f, 205f);
                rect.sizeDelta = new Vector2(820f, 48f);
            }
        }

        private bool ValidateBuildScene()
        {
            if (Camera.main == null && UnityEngine.Object.FindObjectOfType<Camera>() == null)
            {
                Debug.LogError("[Build] BuildScene is missing a camera. Please regenerate the BuildScene resources.");
                return false;
            }

            if (UnityEngine.Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                Debug.LogError("[Build] BuildScene is missing an EventSystem. Please regenerate the BuildScene resources.");
                return false;
            }

            if (titleText == null)
            {
                var titleObject = GameObject.Find("BuildTitle");
                if (titleObject != null)
                {
                    titleText = titleObject.GetComponent<Text>();
                }
            }

            if (titleText == null)
            {
                Debug.LogError("[Build] BuildScene is missing BuildTitle. Please regenerate the BuildScene resources.");
                return false;
            }

            if (characterSlots == null)
            {
                characterSlots = new List<BuildCharacterSlotView>();
            }

            characterSlots.RemoveAll(slot => slot == null);
            if (characterSlots.Count == 0)
            {
                Debug.LogError("[Build] BuildScene has no character slots assigned. Please regenerate the BuildScene resources.");
                return false;
            }

            return true;
        }

        private static void HideCarryoverHintTexts()
        {
            var texts = UnityEngine.Object.FindObjectsOfType<Text>(true);
            for (var i = 0; i < texts.Length; i++)
            {
                var text = texts[i];
                if (text != null &&
                    (text.text.Contains("\u54e5\u5e03\u6797\u6d41\u661f\u9524") ||
                     text.text.Contains("\u9152\u795e\u5723\u676f") ||
                     text.text.Contains("\u5de8\u4eba\u94a5\u5319")))
                {
                    text.gameObject.SetActive(false);
                }
            }
        }

        private CharacterConfig GetDisplayCharacter(CharacterConfig baseCharacter)
        {
            if (baseCharacter == null || gameContext == null)
            {
                return baseCharacter;
            }

            var resolved = baseCharacter;

            if (CurrentRewardType == RewardType.HolyCup || CurrentRewardType == RewardType.GiantKey)
            {
                resolved = ApplyRewardVariant(
                    resolved,
                    gameContext.buildSelection.selectedHammerTargetId,
                    gameContext.buildSelection.selectedHammerCharacter);
            }

            if (CurrentRewardType == RewardType.GiantKey)
            {
                resolved = ApplyRewardVariant(
                    resolved,
                    gameContext.buildSelection.selectedCupTargetId,
                    gameContext.buildSelection.selectedCupCharacter);
            }

            return resolved;
        }

        private CharacterConfig ResolveRewardCharacter(CharacterConfig baseCharacter, RewardType rewardType)
        {
            if (baseCharacter == null)
            {
                return null;
            }

            switch (rewardType)
            {
                case RewardType.HolyCup:
                    return HolyCupCharacterFactory.GetCupVariant(GetDisplayCharacter(baseCharacter));
                case RewardType.GiantKey:
                    return GiantKeyCharacterFactory.GetKeyVariant(GetDisplayCharacter(baseCharacter));
                case RewardType.MeteorHammer:
                default:
                    return HammerCharacterFactory.GetHammerVariant(baseCharacter);
            }
        }

        private string GetBuildTitle(RewardType rewardType)
        {
            return rewardType switch
            {
                RewardType.HolyCup => "\u628a\u5723\u676f\u4ea4\u7ed9\u8c01\uff1f",
                RewardType.GiantKey => "\u628a\u5de8\u4eba\u94a5\u5319\u4ea4\u7ed9\u8c01\uff1f",
                _ => "\u628a\u6d41\u661f\u9524\u4ea4\u7ed9\u8c01\uff1f",
            };
        }

        private static string GetEffectDescription(RewardType rewardType, RoleType roleType)
        {
            return rewardType switch
            {
                RewardType.HolyCup => HolyCupRules.GetEffectDescription(roleType),
                RewardType.GiantKey => GiantKeyRules.GetEffectDescription(roleType),
                _ => MeteorHammerRules.GetEffectDescription(roleType),
            };
        }

        private static string GetAssignButtonText(RewardType rewardType, string displayName)
        {
            return rewardType switch
            {
                RewardType.HolyCup => HolyCupRules.GetAssignButtonText(displayName),
                RewardType.GiantKey => GiantKeyRules.GetAssignButtonText(displayName),
                _ => MeteorHammerRules.GetAssignButtonText(displayName),
            };
        }

        private Sprite GetRewardSprite(RewardType rewardType)
        {
            return rewardType switch
            {
                RewardType.HolyCup => fireGlandSprite,
                RewardType.GiantKey => SimpleSpriteFactory.GetGiantKeySprite(),
                _ => meteorHammerSprite,
            };
        }

        private void ArrangeCharacterSlots()
        {
            var anchoredXs = new[] { -225f, 0f, 225f };
            for (var i = 0; i < characterSlots.Count && i < anchoredXs.Length; i++)
            {
                var slot = characterSlots[i];
                if (slot == null || slot.transform is not RectTransform rect)
                {
                    continue;
                }

                rect.anchoredPosition = new Vector2(anchoredXs[i], 10f);
            }
        }

        private void SyncSlotsWithCurrentRoster()
        {
            if (gameContext == null || gameContext.defaultBattleFormation == null)
            {
                return;
            }

            var formation = gameContext.defaultBattleFormation;
            var roster = new[]
            {
                ResolveRosterCharacter(formation.frontCharacter),
                ResolveRosterCharacter(formation.midCharacter),
                ResolveRosterCharacter(formation.backCharacter),
            };

            for (var i = 0; i < characterSlots.Count && i < roster.Length; i++)
            {
                if (characterSlots[i] != null)
                {
                    characterSlots[i].characterConfig = roster[i];
                }
            }
        }

        private CharacterConfig ResolveRosterCharacter(CharacterConfig baseCharacter)
        {
            if (baseCharacter == null || gameContext == null)
            {
                return baseCharacter;
            }

            var runState = gameContext.runState;
            if (runState.hasRecruitedMage && baseCharacter.id == runState.mageReplacedCharacterId)
            {
                return MageCharacterFactory.GetMageVariant(runState.mageRewardType);
            }

            var resolved = baseCharacter;
            resolved = ApplyRewardVariant(
                resolved,
                gameContext.buildSelection.selectedHammerTargetId,
                gameContext.buildSelection.selectedHammerCharacter);
            resolved = ApplyRewardVariant(
                resolved,
                gameContext.buildSelection.selectedCupTargetId,
                gameContext.buildSelection.selectedCupCharacter);
            return resolved;
        }

        private static CharacterConfig ApplyRewardVariant(CharacterConfig currentConfig, string targetId, CharacterConfig rewardVariant)
        {
            if (currentConfig == null || string.IsNullOrWhiteSpace(targetId) || rewardVariant == null)
            {
                return currentConfig;
            }

            return currentConfig.id == targetId || currentConfig.baseCharacterId == targetId
                ? rewardVariant
                : currentConfig;
        }

        private void ResolveFlowController()
        {
            if (flowController != null)
            {
                return;
            }

            flowController = FindObjectOfType<DemoFlowController>();
            if (flowController != null)
            {
                return;
            }

            if (gameContext == null)
            {
                gameContext = DemoGameContext.Instance;
            }

            if (gameContext == null)
            {
                return;
            }

            flowController = gameContext.GetComponent<DemoFlowController>();
            if (flowController == null)
            {
                flowController = gameContext.gameObject.AddComponent<DemoFlowController>();
                flowController.gameContext = gameContext;
                Debug.Log("[Build] Added missing DemoFlowController onto DemoGameContext.");
            }
        }

        private void EnsureDiscoverPresentation()
        {
            if (_discoverPresentationController != null)
            {
                return;
            }

            var canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                return;
            }

            _discoverPresentationController = DiscoverUnitPresentationController.Create(canvas);
        }

        private static Sprite GetPortraitSprite(CharacterConfig config)
        {
            if (config == null)
            {
                return null;
            }

            return PixelAnimationLibrary.GetFirstFrameSprite(
                config.resourceId,
                "idle",
                "stand",
                "walk",
                config.defaultAttackAction);
        }
    }

    public sealed class DiscoverUnitPresentationController : MonoBehaviour
    {
        private Canvas _canvas;
        private RectTransform _root;
        private Image _dim;
        private Image _panel;
        private Text _titleText;
        private Image _leftUnitImage;
        private Image _rightHammerImage;
        private Image _centerResultImage;
        private Image _glowImage;
        private Text _resultNameText;
        private Button _backButton;
        private Button _confirmButton;
        private Image _previewProjectileImage;
        private Image _impactFlashImage;
        private Action _onBack;
        private Action _onConfirm;
        private CharacterConfig _hammerCharacter;
        private RewardType _rewardType;

        public static DiscoverUnitPresentationController Create(Canvas canvas)
        {
            var go = new GameObject("DiscoverUnitPresentationController");
            var controller = go.AddComponent<DiscoverUnitPresentationController>();
            controller.Initialize(canvas);
            return controller;
        }

        public void Initialize(Canvas canvas)
        {
            _canvas = canvas;
            CreateOverlay();
        }

        public void PlayDiscoverSequence(CharacterConfig baseCharacter, CharacterConfig hammerCharacter, RewardType rewardType, Action onBack, Action onConfirm)
        {
            _hammerCharacter = hammerCharacter;
            _rewardType = rewardType;
            _onBack = onBack;
            _onConfirm = onConfirm;
            gameObject.SetActive(true);
            _root.gameObject.SetActive(true);

            _titleText.text = "\u53d1\u73b0\u65b0\u5175\u79cd";
            _resultNameText.text = hammerCharacter != null ? hammerCharacter.displayName : string.Empty;
            _leftUnitImage.sprite = GetPortraitSprite(baseCharacter);
            _leftUnitImage.enabled = _leftUnitImage.sprite != null;
            _rightHammerImage.sprite = GetRewardSprite(rewardType);
            _rightHammerImage.enabled = _rightHammerImage.sprite != null;
            _centerResultImage.sprite = GetPortraitSprite(hammerCharacter);
            if (_centerResultImage.sprite == null)
            {
                _centerResultImage.sprite = GetPortraitSprite(baseCharacter);
            }
            _centerResultImage.enabled = _centerResultImage.sprite != null;
            _previewProjectileImage.gameObject.SetActive(false);
            _impactFlashImage.gameObject.SetActive(false);

            StopAllCoroutines();
            StartCoroutine(PlayRoutine());
        }

        private IEnumerator PlayRoutine()
        {
            SetButtonsVisible(false);

            _dim.color = new Color(0f, 0f, 0f, 0.26f);
            _panel.color = new Color(0.08f, 0.08f, 0.12f, 0.95f);
            _glowImage.color = new Color(1f, 0.8f, 0.32f, 0f);
            _centerResultImage.color = new Color(1f, 1f, 1f, 0f);
            _resultNameText.color = new Color(1f, 0.94f, 0.82f, 0f);

            var leftRect = _leftUnitImage.rectTransform;
            var rightRect = _rightHammerImage.rectTransform;
            var resultRect = _centerResultImage.rectTransform;
            var leftStart = new Vector2(-112f, -8f);
            var rightStart = new Vector2(112f, -8f);
            var center = new Vector2(0f, -4f);

            leftRect.anchoredPosition = leftStart;
            rightRect.anchoredPosition = rightStart;
            resultRect.anchoredPosition = center;
            leftRect.localScale = Vector3.one;
            rightRect.localScale = Vector3.one * 0.9f;
            resultRect.localScale = Vector3.one * 0.76f;
            rightRect.localEulerAngles = Vector3.zero;

            if (_rewardType == RewardType.None)
            {
                _leftUnitImage.enabled = false;
                _rightHammerImage.enabled = false;
                yield return PlayRevealRoutine(resultRect);
                yield return new WaitForSeconds(0.18f);
                yield return PlayAttackPreviewRoutine();
                yield return new WaitForSeconds(0.12f);
                SetButtonsVisible(true);
                yield break;
            }

            yield return new WaitForSeconds(0.25f);

            const float mergeDuration = 0.48f;
            var elapsed = 0f;
            while (elapsed < mergeDuration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / mergeDuration);
                var ease = Mathf.SmoothStep(0f, 1f, t);
                leftRect.anchoredPosition = Vector2.Lerp(leftStart, center, ease);
                rightRect.anchoredPosition = Vector2.Lerp(rightStart, center, ease);
                leftRect.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 1.08f, ease);
                rightRect.localScale = Vector3.Lerp(Vector3.one * 0.9f, Vector3.one * 1.05f, ease);
                rightRect.localEulerAngles = new Vector3(0f, 0f, Mathf.Lerp(0f, -270f, t));
                yield return null;
            }

            _leftUnitImage.enabled = false;
            _rightHammerImage.enabled = false;

            yield return PlayRevealRoutine(resultRect);

            yield return new WaitForSeconds(0.18f);
            yield return PlayAttackPreviewRoutine();
            yield return new WaitForSeconds(0.12f);

            SetButtonsVisible(true);
        }

        private IEnumerator PlayRevealRoutine(RectTransform resultRect)
        {
            const float revealDuration = 0.55f;
            var elapsed = 0f;
            while (elapsed < revealDuration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / revealDuration);
                var ease = Mathf.SmoothStep(0f, 1f, t);
                _glowImage.color = new Color(1f, 0.8f, 0.32f, 0.4f * ease);
                _glowImage.rectTransform.localScale = Vector3.one * (0.92f + ease * 0.18f);
                _centerResultImage.color = new Color(1f, 1f, 1f, ease);
                resultRect.localScale = Vector3.Lerp(Vector3.one * 0.76f, Vector3.one, ease);
                _resultNameText.color = new Color(1f, 0.94f, 0.82f, Mathf.Clamp01((t - 0.25f) / 0.75f));
                yield return null;
            }
        }

        private IEnumerator PlayAttackPreviewRoutine()
        {
            if (_hammerCharacter == null)
            {
                yield break;
            }

            var attackClip = PixelAnimationLibrary.GetClip(_hammerCharacter.resourceId, _hammerCharacter.defaultAttackAction);
            var idleSprite = GetPortraitSprite(_hammerCharacter);
            var resultRect = _centerResultImage.rectTransform;
            var basePosition = resultRect.anchoredPosition;
            var baseScale = resultRect.localScale;

            if (attackClip == null || attackClip.frames == null || attackClip.frames.Length == 0)
            {
                yield break;
            }

            for (var frameIndex = 0; frameIndex < attackClip.frames.Length; frameIndex++)
            {
                var frame = attackClip.frames[frameIndex];
                if (frame != null)
                {
                    _centerResultImage.sprite = frame;
                }

                var normalized = attackClip.frames.Length <= 1 ? 1f : frameIndex / (float)(attackClip.frames.Length - 1);
                var lunge = Mathf.Sin(normalized * Mathf.PI) * 10f;
                resultRect.anchoredPosition = basePosition + new Vector2(lunge, 0f);
                resultRect.localScale = baseScale * (1f + Mathf.Sin(normalized * Mathf.PI) * 0.05f);

                if (ShouldSpawnProjectileAtFrame(_hammerCharacter, frameIndex))
                {
                    yield return StartCoroutine(PlayProjectilePreviewRoutine(_hammerCharacter, basePosition, frameIndex));
                }
                else
                {
                    yield return new WaitForSeconds(attackClip.frameDuration);
                }
            }

            _centerResultImage.sprite = idleSprite;
            resultRect.anchoredPosition = basePosition;
            resultRect.localScale = baseScale;
        }

        private IEnumerator PlayProjectilePreviewRoutine(CharacterConfig config, Vector2 basePosition, int attackFrameIndex)
        {
            var projectileSprite = GetProjectileSprite(config, attackFrameIndex);
            if (projectileSprite == null)
            {
                yield return new WaitForSeconds(0.08f);
                yield break;
            }

            _previewProjectileImage.gameObject.SetActive(true);
            _previewProjectileImage.sprite = projectileSprite;
            _previewProjectileImage.color = Color.white;
            _previewProjectileImage.rectTransform.localEulerAngles = Vector3.zero;

            var start = basePosition + GetProjectileStartOffset(config, attackFrameIndex);
            var end = basePosition + new Vector2(215f, 18f);
            var duration = GetProjectileDuration(config, attackFrameIndex);
            var arcHeight = GetProjectileArcHeight(config, attackFrameIndex);
            var elapsed = 0f;
            var frames = GetProjectileAnimationFrames(config, attackFrameIndex);
            var frameTimer = 0f;
            var frameIndex = 0;

            _previewProjectileImage.rectTransform.anchoredPosition = start;
            _previewProjectileImage.rectTransform.sizeDelta = GetProjectileSize(config, attackFrameIndex);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                frameTimer += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, duration));
                var position = Vector2.Lerp(start, end, t);
                position.y += Mathf.Sin(t * Mathf.PI) * arcHeight;
                _previewProjectileImage.rectTransform.anchoredPosition = position;

                if (frames != null && frames.Length > 0)
                {
                    if (frameTimer >= 1f / 12f)
                    {
                        frameTimer = 0f;
                        frameIndex = (frameIndex + 1) % frames.Length;
                    }

                    if (frames[frameIndex] != null)
                    {
                        _previewProjectileImage.sprite = frames[frameIndex];
                    }
                }

                if (UsesSpinningProjectile(config, attackFrameIndex))
                {
                    _previewProjectileImage.rectTransform.localEulerAngles = new Vector3(0f, 0f, -720f * elapsed);
                }
                else if (UsesFlippedProjectile(config, attackFrameIndex))
                {
                    _previewProjectileImage.rectTransform.localEulerAngles = new Vector3(0f, 180f, 0f);
                }

                yield return null;
            }

            _previewProjectileImage.gameObject.SetActive(false);
            yield return PlayImpactFlashRoutine(end, config);
        }

        private IEnumerator PlayImpactFlashRoutine(Vector2 impactPosition, CharacterConfig config)
        {
            _impactFlashImage.gameObject.SetActive(true);
            _impactFlashImage.rectTransform.anchoredPosition = impactPosition;
            _impactFlashImage.rectTransform.sizeDelta = config != null && config.roleType == RoleType.Archer
                ? new Vector2(96f, 96f)
                : new Vector2(72f, 72f);

            var elapsed = 0f;
            const float duration = 0.22f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                var ease = Mathf.Sin(t * Mathf.PI);
                _impactFlashImage.color = new Color(1f, 0.86f, 0.46f, 0.55f * (1f - t));
                _impactFlashImage.rectTransform.localScale = Vector3.one * (0.75f + ease * 0.55f);
                yield return null;
            }

            _impactFlashImage.gameObject.SetActive(false);
        }

        private void CreateOverlay()
        {
            var rootGo = new GameObject("DiscoverUnitOverlay", typeof(RectTransform));
            rootGo.transform.SetParent(_canvas.transform, false);
            _root = rootGo.GetComponent<RectTransform>();
            _root.anchorMin = Vector2.zero;
            _root.anchorMax = Vector2.one;
            _root.offsetMin = Vector2.zero;
            _root.offsetMax = Vector2.zero;

            _dim = CreateFullScreenImage(_root, "Dim", new Color(0f, 0f, 0f, 0.26f));
            _panel = CreateAnchoredImage(_root, "Panel", new Vector2(470f, 400f), new Color(0.08f, 0.08f, 0.12f, 0.95f));
            var panelOutline = _panel.gameObject.AddComponent<Outline>();
            panelOutline.effectColor = new Color(1f, 0.82f, 0.42f, 0.9f);
            panelOutline.effectDistance = new Vector2(2f, -2f);

            _glowImage = CreateAnchoredImage(_root, "Glow", new Vector2(260f, 240f), new Color(1f, 0.8f, 0.32f, 0f));
            _titleText = CreateText(_root, "Title", new Vector2(280f, 36f), 26, "\u53d1\u73b0\u65b0\u5175\u79cd");
            _titleText.rectTransform.anchoredPosition = new Vector2(0f, 145f);

            _leftUnitImage = CreateAnchoredImage(_root, "BaseUnit", new Vector2(150f, 150f), Color.white);
            _rightHammerImage = CreateAnchoredImage(_root, "Hammer", new Vector2(92f, 92f), Color.white);
            _centerResultImage = CreateAnchoredImage(_root, "ResultUnit", new Vector2(180f, 180f), new Color(1f, 1f, 1f, 0f));

            _previewProjectileImage = CreateAnchoredImage(_root, "ProjectilePreview", new Vector2(68f, 68f), Color.white);
            _previewProjectileImage.gameObject.SetActive(false);
            _previewProjectileImage.raycastTarget = false;

            _impactFlashImage = CreateAnchoredImage(_root, "ImpactFlash", new Vector2(72f, 72f), new Color(1f, 0.86f, 0.46f, 0f));
            _impactFlashImage.sprite = SimpleSpriteFactory.GetHitSparkSprite();
            _impactFlashImage.gameObject.SetActive(false);
            _impactFlashImage.raycastTarget = false;

            _resultNameText = CreateText(_root, "ResultName", new Vector2(260f, 40f), 24, string.Empty);
            _resultNameText.rectTransform.anchoredPosition = new Vector2(0f, -120f);

            _backButton = CreateButton(_root, "BackButton", new Vector2(-78f, -172f), new Vector2(132f, 40f), "\u8fd4\u56de", HandleBack);
            _confirmButton = CreateButton(_root, "ConfirmButton", new Vector2(78f, -172f), new Vector2(132f, 40f), "\u786e\u8ba4", HandleConfirm);

            _root.gameObject.SetActive(false);
        }

        private void HandleBack()
        {
            _root.gameObject.SetActive(false);
            _onBack?.Invoke();
        }

        private void HandleConfirm()
        {
            _root.gameObject.SetActive(false);
            _onConfirm?.Invoke();
        }

        private void SetButtonsVisible(bool isVisible)
        {
            _backButton.gameObject.SetActive(isVisible);
            _confirmButton.gameObject.SetActive(isVisible);
        }

        private static bool ShouldSpawnProjectileAtFrame(CharacterConfig config, int frameIndex)
        {
            if (config == null)
            {
                return false;
            }

            if (config.roleType == RoleType.Archer)
            {
                return frameIndex == 8;
            }

            if (config.roleType == RoleType.Assassin && config.isHammerVariant)
            {
                return frameIndex == 3 || frameIndex == 7;
            }

            return false;
        }

        private static Sprite GetProjectileSprite(CharacterConfig config, int attackFrameIndex)
        {
            if (config == null)
            {
                return null;
            }

            if (config.roleType == RoleType.Archer && config.isHammerVariant)
            {
                return SimpleSpriteFactory.GetMeteorHammerSprite();
            }

            if (config.roleType == RoleType.Archer && config.resourceId == "catapult")
            {
                return PixelAnimationLibrary.GetFirstFrameSprite("catapult_ball", "idle");
            }

            if (config.roleType == RoleType.Archer)
            {
                return ExternalSpriteLibrary.GetLongbowArrowSprite();
            }

            if (config.roleType == RoleType.Assassin && config.isHammerVariant)
            {
                if (attackFrameIndex <= 3)
                {
                    var bladeFrames = ExternalSpriteLibrary.GetBallistaArrowFrames();
                    return bladeFrames != null && bladeFrames.Length > 0 ? bladeFrames[0] : null;
                }

                return SimpleSpriteFactory.GetMeteorHammerSprite();
            }

            if (config.roleType == RoleType.Assassin)
            {
                var frames = ExternalSpriteLibrary.GetBallistaArrowFrames();
                return frames != null && frames.Length > 0 ? frames[0] : null;
            }

            return null;
        }

        private static Sprite[] GetProjectileAnimationFrames(CharacterConfig config, int attackFrameIndex)
        {
            if (config != null && config.roleType == RoleType.Archer && config.resourceId == "catapult")
            {
                return PixelAnimationLibrary.GetClip("catapult_ball", "idle")?.frames;
            }

            if (config != null && config.roleType == RoleType.Assassin && (!config.isHammerVariant || attackFrameIndex <= 3))
            {
                return ExternalSpriteLibrary.GetBallistaArrowFrames();
            }

            return null;
        }

        private static Vector2 GetProjectileStartOffset(CharacterConfig config, int attackFrameIndex)
        {
            if (config == null)
            {
                return new Vector2(34f, 6f);
            }

            if (config.roleType == RoleType.Archer)
            {
                return new Vector2(38f, 10f);
            }

            if (config.roleType == RoleType.Assassin)
            {
                if (config.isHammerVariant)
                {
                    return attackFrameIndex <= 3 ? new Vector2(36f, 2f) : new Vector2(42f, 4f);
                }

                return new Vector2(36f, 2f);
            }

            return new Vector2(34f, 6f);
        }

        private static Vector2 GetProjectileSize(CharacterConfig config, int attackFrameIndex)
        {
            if (config == null)
            {
                return new Vector2(64f, 64f);
            }

            if (config.roleType == RoleType.Archer && config.isHammerVariant)
            {
                return new Vector2(64f, 64f);
            }

            if (config.roleType == RoleType.Archer && config.resourceId == "catapult")
            {
                return new Vector2(58f, 58f);
            }

            if (config.roleType == RoleType.Archer)
            {
                return new Vector2(52f, 24f);
            }

            if (config.roleType == RoleType.Assassin && config.isHammerVariant)
            {
                return attackFrameIndex <= 3 ? new Vector2(56f, 24f) : new Vector2(56f, 56f);
            }

            return new Vector2(56f, 24f);
        }

        private static float GetProjectileDuration(CharacterConfig config, int attackFrameIndex)
        {
            if (config != null && config.roleType == RoleType.Assassin)
            {
                return attackFrameIndex <= 3 ? 0.32f : 0.5f;
            }

            return 0.5f;
        }

        private static float GetProjectileArcHeight(CharacterConfig config, int attackFrameIndex)
        {
            if (config == null)
            {
                return 0f;
            }

            if (config.roleType == RoleType.Assassin)
            {
                return 0f;
            }

            if (config.roleType == RoleType.Archer)
            {
                return config.isHammerVariant ? 74f : config.resourceId == "catapult" ? 88f : 58f;
            }

            return config.isHammerVariant ? 54f : 0f;
        }

        private static bool UsesSpinningProjectile(CharacterConfig config, int attackFrameIndex)
        {
            return config != null &&
                   config.isHammerVariant &&
                   config.roleType != RoleType.Assassin
                   || (config != null && config.roleType == RoleType.Assassin && config.isHammerVariant && attackFrameIndex > 3);
        }

        private static bool UsesFlippedProjectile(CharacterConfig config, int attackFrameIndex)
        {
            return config != null &&
                   config.roleType == RoleType.Assassin &&
                   (!config.isHammerVariant || attackFrameIndex <= 3);
        }

        private static Image CreateFullScreenImage(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var image = go.GetComponent<Image>();
            image.sprite = SimpleSpriteFactory.GetWhitePixelSprite();
            image.color = color;
            return image;
        }

        private static Image CreateAnchoredImage(Transform parent, string name, Vector2 size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;

            var image = go.GetComponent<Image>();
            image.sprite = SimpleSpriteFactory.GetWhitePixelSprite();
            image.color = color;
            image.preserveAspect = true;
            return image;
        }

        private static Text CreateText(Transform parent, string name, Vector2 size, int fontSize, string content)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;

            var text = go.GetComponent<Text>();
            text.font = BuildUiRuntimeStyle.GetChineseFont();
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.text = content;
            text.color = Color.white;
            return text;
        }

        private static Button CreateButton(Transform parent, string name, Vector2 position, Vector2 size, string label, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            var image = go.GetComponent<Image>();
            image.color = new Color(0.95f, 0.55f, 0.16f, 0.98f);

            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(onClick);

            var text = CreateText(go.transform, "Text", size, 20, label);
            text.rectTransform.anchoredPosition = Vector2.zero;
            return button;
        }

        private static Sprite GetPortraitSprite(CharacterConfig config)
        {
            if (config == null)
            {
                return null;
            }

            return PixelAnimationLibrary.GetFirstFrameSprite(
                config.resourceId,
                "idle",
                "stand",
                "walk",
                config.defaultAttackAction);
        }

        private static Sprite GetRewardSprite(RewardType rewardType)
        {
            return rewardType switch
            {
                RewardType.None => SimpleSpriteFactory.GetHitSparkSprite(),
                RewardType.HolyCup => SimpleSpriteFactory.GetHolyCupSprite(),
                RewardType.GiantKey => SimpleSpriteFactory.GetGiantKeySprite(),
                _ => SimpleSpriteFactory.GetMeteorHammerSprite(),
            };
        }
    }
}
