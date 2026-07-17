using System.Collections;
using ProjectHunt.Battle;
using ProjectHunt.Build;
using ProjectHunt.Data;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProjectHunt.UI
{
    /// <summary>
    /// Keeps the battle camera in the central gameplay band and owns the surrounding campaign HUD.
    /// The world simulation remains unchanged; this component only changes presentation layout.
    /// </summary>
    public sealed class BattleHudLayoutController : MonoBehaviour
    {
        private enum MusicMode
        {
            Battle,
            Boss,
            Treasure,
        }

        // This is the red-box area from the approved layout, expressed in normalized screen space.
        private static readonly Rect GameplayViewport = new Rect(0.012f, 0.40f, 0.976f, 0.41f);

        private BattleDirector _director;
        private Transform _root;
        private Image[] _partyPortraits;
        private Image[] _partyCompositeWeaponOverlays;
        private Image[] _relicIcons;
        private RectTransform[] _relicSlotRects;
        private GameObject[] _relicPowerConnections;
        private Image[] _relicPowerLines;
        private Image[][] _relicPowerDots;
        private GameObject _relicDragGuideConnection;
        private Image _relicDragGuideLine;
        private Image[] _relicDragGuideDots;
        private int _relicDragGuideTargetPartyIndex = -1;
        private int _relicDragGuideHoverPartyIndex = -1;
        private GameObject[] _partyCards;
        private Outline[] _partyCardOutlines;
        private GameObject[] _partyStatBars;
        private Text[] _partyHpValues;
        private Text[] _partyAttackValues;
        private GameObject[] _newUnitTipBars;
        private CharacterConfig[] _pendingDiscoverBaseConfigs;
        private CharacterConfig[] _pendingDiscoverConfigs;
        private RewardType[] _pendingDiscoverRewards;
        private RectTransform[] _selectionEffects;
        private Coroutine[] _selectionEffectRoutines;
        private Image[] _cooldownMasks;
        private Image[] _relicCooldownMasks;
        private Coroutine[] _relicCooldownRoutines;
        private bool[] _relicCoolingDown;
        private Image[] _relicProhibitedIcons;
        private Outline[] _relicOutlines;
        private RewardType[] _relicRewards;
        private CombatUnitController[] _partyUnits;
        private Coroutine[] _partyCooldowns;
        private CharacterConfig[] _partyDefaultConfigs;
        private RewardType[] _partyEquippedRewards;
        private RewardType _draggedRelicReward = RewardType.None;
        private Image[] _trailNodes;
        private RectTransform _trailProgress;
        private Text _stageTitleLabel;
        private Text _waveCountLabel;
        private AudioSource _musicSource;
        // Decorative props are separate from the future static battle-background layer.
        private GameObject _environmentProps;
        private bool _environmentPropsAreBoss;
        private GameObject _relicDragTipRoot;
        private RectTransform _relicDragTipArrow;
        private Coroutine _relicDragTipRoutine;
        private DiscoverUnitPresentationController _discoverPresentation;
        private GameObject _partyCompositionButton;
        private GameObject _partyCompositionMenu;
        private Toggle[] _partyCompositionToggles;
        private bool[] _partyCompositionSelection;
        private Button _partyCompositionConfirmButton;
        private Image _partyCompositionConfirmImage;
        private CharacterConfig[] _partyCompositionOriginals;
        private int _activeWave;
        private int _totalWaves;
        private int _waveEnemyTotal;
        private int _selectedPartyIndex = -1;
        private const float RelicCooldownDuration = 6f;
        private const float RelicEnergyFlowSpeed = 0.216f;
        private const float RelicGuideFlowSpeed = 0.72f;
        private static readonly Color RelicEnergyLineColor = new Color(0.62f, 0.72f, 0.78f, 1f);
        private static readonly Color RelicEnergyDotColor = new Color(0.72f, 0.82f, 0.87f, 1f);
        private static readonly Color NormalCardOutline = new Color(0.5f, 0.5f, 0.58f, 1f);
        private static readonly Color SelectedCardOutline = new Color(0.28f, 0.75f, 1f, 1f);
        private static readonly Color NormalRelicOutline = new Color(0.42f, 0.43f, 0.5f, 0.9f);
        private static readonly Color ActiveRelicOutline = new Color(1f, 0.72f, 0.2f, 1f);

        public static BattleHudLayoutController Instance { get; private set; }

        public static void Ensure(BattleDirector director)
        {
            if (director == null)
            {
                return;
            }

            var existing = FindObjectOfType<BattleHudLayoutController>();
            if (existing != null)
            {
                existing.Initialize(director);
                return;
            }

            var go = new GameObject("BattleHudLayout");
            var controller = go.AddComponent<BattleHudLayoutController>();
            controller.Initialize(director);
        }

        private void Initialize(BattleDirector director)
        {
            if (_root != null)
            {
                return;
            }

            _director = director;
            Instance = this;
            ApplyGameplayViewport();
            HideLegacyBossBar();
            MoveDropHintIntoGameplayBand();
            CreateHudCanvas();
            StartCoroutine(RefreshHudRoutine());
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Update()
        {
            RefreshSelectedPartyStats();
            RefreshRelicPowerConnections();
            RefreshPartyCompositionButton();
        }

        public static void NotifyWaveStarted(int waveIndex, int totalWaves, int enemyTotal)
        {
            Instance?.SetWaveStarted(waveIndex, totalWaves, enemyTotal);
        }

        public static void PrepareEntranceBackdrop(bool startsWithBoss)
        {
            Instance?.SetEncounterBackdrop(startsWithBoss ? 3 : 0, startsWithBoss);
        }

        public static void NotifyWaveEnemyDefeated(int remainingEnemies)
        {
            Instance?.SetWaveEnemyCount(remainingEnemies);
        }

        public static void NotifyWaveCleared(int waveIndex, int totalWaves)
        {
            Instance?.SetWaveCleared(waveIndex, totalWaves);
        }

        public static void NotifyBossIncoming()
        {
            Instance?.SetBossIncoming();
        }

        public static IEnumerator PlayBossBackdropEntrance()
        {
            if (Instance == null)
            {
                yield break;
            }

            yield return Instance.StartCoroutine(Instance.BossBackdropEntranceRoutine());
        }

        public static void NotifyRouteCompleted()
        {
            Instance?.SetRouteCompleted();
        }

        public static void NotifyTreasurePhase()
        {
            Instance?.PlayMusic(MusicMode.Treasure);
        }

        public static void NotifyRescuePhase()
        {
            Instance?.PlayMusic(MusicMode.Battle);
        }

        private void ApplyGameplayViewport()
        {
            var battleCamera = Camera.main;
            if (battleCamera != null)
            {
                battleCamera.rect = GameplayViewport;
            }
        }

        private void HideLegacyBossBar()
        {
            if (_director == null || _director.bossHpBarView == null)
            {
                return;
            }

            _director.bossHpBarView.gameObject.SetActive(false);
            if (_director.bossHpBarView.label != null)
            {
                _director.bossHpBarView.label.gameObject.SetActive(false);
            }
        }

        private void MoveDropHintIntoGameplayBand()
        {
            if (_director == null || _director.dropHintText == null)
            {
                return;
            }

            var hint = _director.dropHintText.rectTransform;
            hint.anchorMin = new Vector2(0.5f, GameplayViewport.yMax - 0.015f);
            hint.anchorMax = hint.anchorMin;
            hint.anchoredPosition = Vector2.zero;
            hint.sizeDelta = new Vector2(760f, 56f);
        }

        private void CreateHudCanvas()
        {
            var canvasGo = new GameObject("CampaignHudCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;
            EnsureEventSystem();

            CreateHudClearLayer(canvasGo.transform);

            _root = new GameObject("CampaignHud", typeof(RectTransform)).transform;
            _root.SetParent(canvasGo.transform, false);
            var rootRect = (RectTransform)_root;
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            CreateTopStatus();
            CreateLeftControls();
            CreatePartyCards();
            CreatePartyCompositionUi();
            CreateRelicBar();
            CreateRelicPowerConnections();
            CreateMusic();
        }

        private static void CreateHudClearLayer(Transform canvasRoot)
        {
            // The battle camera only clears the central gameplay viewport. These opaque panels
            // clear the surrounding HUD area every frame so moving overlay UI cannot leave trails.
            var clearRoot = new GameObject("HudClearLayer", typeof(RectTransform)).transform;
            clearRoot.SetParent(canvasRoot, false);
            var clearRect = (RectTransform)clearRoot;
            clearRect.anchorMin = Vector2.zero;
            clearRect.anchorMax = Vector2.one;
            clearRect.offsetMin = Vector2.zero;
            clearRect.offsetMax = Vector2.zero;

            var color = new Color(0.055f, 0.055f, 0.09f, 1f);
            CreateStretchPanel(
                clearRoot,
                "TopClear",
                new Vector2(0f, GameplayViewport.yMax),
                Vector2.one,
                color);
            CreateStretchPanel(
                clearRoot,
                "BottomClear",
                Vector2.zero,
                new Vector2(1f, GameplayViewport.yMin),
                color);
            CreateStretchPanel(
                clearRoot,
                "LeftClear",
                new Vector2(0f, GameplayViewport.yMin),
                new Vector2(GameplayViewport.xMin, GameplayViewport.yMax),
                color);
            CreateStretchPanel(
                clearRoot,
                "RightClear",
                new Vector2(GameplayViewport.xMax, GameplayViewport.yMin),
                new Vector2(1f, GameplayViewport.yMax),
                color);
            clearRoot.SetAsFirstSibling();
        }

        private static void CreateStretchPanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Color color)
        {
            var panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var image = panel.GetComponent<Image>();
            image.sprite = SimpleSpriteFactory.GetWhitePixelSprite();
            image.color = color;
            image.raycastTarget = false;
        }

        private void CreateTopStatus()
        {
            CreatePanel(_root, "HuntMark", new Vector2(0.09f, 0.955f), new Vector2(130f, 58f), new Color(0.09f, 0.28f, 0.40f, 0.96f));
            CreateLabel(_root, "HuntMarkText", "✦", new Vector2(0.09f, 0.955f), new Vector2(120f, 55f), 34, new Color(0.8f, 0.94f, 1f, 1f));

            var map = CreatePanel(_root, "StageTrail", new Vector2(0.5f, 0.95f), new Vector2(530f, 112f), new Color(0.19f, 0.28f, 0.23f, 0.98f));
            CreateOutline(map, new Color(0.67f, 0.55f, 0.31f, 0.95f), new Vector2(4f, -4f));
            var encounterType = _director != null && _director.UsesPreBossWaves ? "苦战" : "救援";
            CreateLabel(map.transform, "StageTitle", "[" + encounterType + "] 贫瘠森林", new Vector2(0.5f, 0.77f), new Vector2(480f, 42f), 26, new Color(1f, 0.68f, 0.12f, 1f));

            _trailNodes = new Image[4];
            for (var i = 0; i < _trailNodes.Length; i++)
            {
                var node = CreatePanel(
                    map.transform,
                    "TrailNode_" + i,
                    new Vector2(0.18f + i * 0.21f, 0.34f),
                    new Vector2(46f, 46f),
                    i == 0
                        ? new Color(0.16f, 0.55f, 0.95f, 1f)
                        : new Color(0.28f, 0.22f, 0.16f, 1f));
                CreateOutline(node, new Color(0.05f, 0.06f, 0.08f, 0.9f), new Vector2(3f, -3f));
                _trailNodes[i] = node.GetComponent<Image>();
            }

            _stageTitleLabel = map.transform.Find("StageTitle").GetComponent<Text>();
            var progress = CreatePanel(map.transform, "TrailProgress", new Vector2(0.18f, 0.34f), new Vector2(334f, 10f), new Color(0.78f, 0.55f, 0.25f, 1f));
            _trailProgress = progress.GetComponent<RectTransform>();
            _trailProgress.pivot = new Vector2(0f, 0.5f);
            _trailProgress.sizeDelta = new Vector2(0f, 10f);
            _trailProgress.SetSiblingIndex(1);
            var waveCountBackground = CreatePanel(
                map.transform,
                "WaveCountBackground",
                new Vector2(0.5f, 0.5f),
                new Vector2(280f, 38f),
                new Color(0.035f, 0.04f, 0.06f, 0.96f));
            waveCountBackground.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -83.5f);
            CreateOutline(waveCountBackground, new Color(0.32f, 0.34f, 0.4f, 0.95f), new Vector2(2f, -2f));
            _waveCountLabel = CreateLabel(
                waveCountBackground.transform,
                "WaveCount",
                "",
                new Vector2(0.5f, 0.5f),
                new Vector2(264f, 32f),
                22,
                new Color(0.92f, 0.9f, 0.78f, 1f));
            waveCountBackground.transform.SetAsLastSibling();

            var legacyStageCount = _root.Find("StageCount");
            if (legacyStageCount != null)
            {
                Destroy(legacyStageCount.gameObject);
            }

            for (var i = 0; i < _trailNodes.Length; i++)
            {
                _trailNodes[i].transform.SetAsLastSibling();
            }
        }

        private void CreateLeftControls()
        {
            var root = CreatePanel(_root, "BattleControls", new Vector2(0.055f, 0.76f), new Vector2(62f, 186f), new Color(0.12f, 0.12f, 0.17f, 0.86f));
            CreateOutline(root, new Color(0.42f, 0.38f, 0.42f, 0.95f), new Vector2(3f, -3f));
            CreateLabel(root.transform, "Pause", "Ⅱ", new Vector2(0.5f, 0.79f), new Vector2(60f, 48f), 34, Color.white);
            CreateLabel(root.transform, "Speed", "x1", new Vector2(0.5f, 0.47f), new Vector2(60f, 40f), 20, Color.white);
            CreateLabel(root.transform, "Stats", "▥\n统计", new Vector2(0.5f, 0.16f), new Vector2(62f, 52f), 16, new Color(0.82f, 0.86f, 0.9f, 1f));
        }

        private void CreatePartyCards()
        {
            _partyPortraits = new Image[3];
            _partyCompositeWeaponOverlays = new Image[3];
            _partyCards = new GameObject[3];
            _partyCardOutlines = new Outline[3];
            _partyStatBars = new GameObject[3];
            _partyHpValues = new Text[3];
            _partyAttackValues = new Text[3];
            _newUnitTipBars = new GameObject[3];
            _pendingDiscoverBaseConfigs = new CharacterConfig[3];
            _pendingDiscoverConfigs = new CharacterConfig[3];
            _pendingDiscoverRewards = new RewardType[3];
            _cooldownMasks = new Image[3];
            _relicProhibitedIcons = new Image[3];
            _partyUnits = new CombatUnitController[3];
            _partyCooldowns = new Coroutine[3];
            _partyDefaultConfigs = new CharacterConfig[3];
            _partyEquippedRewards = new RewardType[3];
            var xPositions = new[] { 0.22f, 0.5f, 0.78f };
            for (var i = 0; i < _partyPortraits.Length; i++)
            {
                var card = CreatePanel(_root, "PartyCard_" + i, new Vector2(xPositions[i], 0.245f), new Vector2(212f, 214f), new Color(0.055f, 0.06f, 0.09f, 0.98f));
                _partyCards[i] = card;
                _partyCardOutlines[i] = CreateOutline(card, NormalCardOutline, new Vector2(5f, -5f));
                var inset = CreatePanel(card.transform, "Inset", new Vector2(0.5f, 0.52f), new Vector2(184f, 184f), new Color(0.12f, 0.13f, 0.18f, 1f));
                var portrait = CreateImage(inset.transform, "Portrait", new Vector2(0.5f, 0.55f), new Vector2(150f, 150f), null, Color.white);
                portrait.preserveAspect = true;
                _partyPortraits[i] = portrait;
                var compositeWeapon = CreateImage(
                    inset.transform,
                    "CompositeWeapon",
                    new Vector2(0.78f, 0.78f),
                    new Vector2(48f, 48f),
                    null,
                    Color.white);
                compositeWeapon.preserveAspect = true;
                compositeWeapon.raycastTarget = false;
                compositeWeapon.gameObject.SetActive(false);
                _partyCompositeWeaponOverlays[i] = compositeWeapon;

                var cooldownMask = CreateImage(
                    inset.transform,
                    "CooldownMask",
                    new Vector2(0.5f, 0.5f),
                    new Vector2(184f, 184f),
                    SimpleSpriteFactory.GetWhitePixelSprite(),
                    new Color(0.08f, 0.45f, 0.72f, 0.48f));
                cooldownMask.type = Image.Type.Filled;
                cooldownMask.fillMethod = Image.FillMethod.Radial360;
                cooldownMask.fillOrigin = (int)Image.Origin360.Top;
                cooldownMask.fillClockwise = true;
                cooldownMask.fillAmount = 0f;
                cooldownMask.raycastTarget = false;
                cooldownMask.gameObject.SetActive(false);
                _cooldownMasks[i] = cooldownMask;

                var prohibitedIcon = CreateImage(
                    inset.transform,
                    "RelicProhibitedIcon",
                    new Vector2(0.5f, 0.5f),
                    new Vector2(112f, 112f),
                    SimpleSpriteFactory.GetProhibitedIconSprite(),
                    Color.white);
                prohibitedIcon.preserveAspect = true;
                prohibitedIcon.raycastTarget = false;
                prohibitedIcon.gameObject.SetActive(false);
                _relicProhibitedIcons[i] = prohibitedIcon;

                CreatePartyStatBar(card.transform, i);
                CreateNewUnitTipBar(card.transform, i);

                var interaction = card.AddComponent<PartyCardInteraction>();
                interaction.Initialize(this, i);
            }

            CreateRelicDragTip();
        }

        private void CreatePartyCompositionUi()
        {
            _partyCompositionButton = CreatePanel(
                _root,
                "PartyCompositionButton",
                new Vector2(0.925f, 0.245f),
                new Vector2(66f, 66f),
                new Color(0.12f, 0.48f, 0.72f, 1f));
            CreateOutline(_partyCompositionButton, new Color(0.58f, 0.88f, 1f, 1f), new Vector2(4f, -4f));
            CreateLabel(
                _partyCompositionButton.transform,
                "Plus",
                "+",
                new Vector2(0.5f, 0.52f),
                new Vector2(60f, 60f),
                48,
                Color.white);
            var openButton = _partyCompositionButton.AddComponent<Button>();
            _partyCompositionButton.GetComponent<Image>().raycastTarget = true;
            openButton.targetGraphic = _partyCompositionButton.GetComponent<Image>();
            openButton.onClick.AddListener(OpenPartyCompositionMenu);
            _partyCompositionButton.SetActive(false);

            _partyCompositionMenu = new GameObject("PartyCompositionMenu", typeof(RectTransform), typeof(Image));
            _partyCompositionMenu.transform.SetParent(_root, false);
            var blockerRect = _partyCompositionMenu.GetComponent<RectTransform>();
            blockerRect.anchorMin = Vector2.zero;
            blockerRect.anchorMax = Vector2.one;
            blockerRect.offsetMin = Vector2.zero;
            blockerRect.offsetMax = Vector2.zero;
            var blocker = _partyCompositionMenu.GetComponent<Image>();
            blocker.sprite = SimpleSpriteFactory.GetWhitePixelSprite();
            blocker.color = new Color(0.015f, 0.018f, 0.03f, 0.82f);
            blocker.raycastTarget = true;

            var panel = CreatePanel(
                _partyCompositionMenu.transform,
                "Panel",
                new Vector2(0.5f, 0.5f),
                new Vector2(820f, 780f),
                new Color(0.055f, 0.065f, 0.095f, 1f));
            CreateOutline(panel, new Color(0.38f, 0.68f, 0.88f, 1f), new Vector2(6f, -6f));
            CreateLabel(panel.transform, "Title", "调整出战队伍", new Vector2(0.5f, 0.91f), new Vector2(720f, 72f), 42, Color.white);
            CreateLabel(
                panel.transform,
                "Hint",
                "选择三名出战角色",
                new Vector2(0.5f, 0.83f),
                new Vector2(700f, 46f),
                25,
                new Color(0.72f, 0.8f, 0.9f, 1f));

            _partyCompositionToggles = new Toggle[4];
            _partyCompositionSelection = new bool[4];
            var roleNames = new[] { "剑士", "刺客", "弓手", "法师" };
            for (var i = 0; i < _partyCompositionToggles.Length; i++)
            {
                var row = CreatePanel(
                    panel.transform,
                    "CharacterOption_" + i,
                    new Vector2(0.5f, 0.70f - i * 0.14f),
                    new Vector2(680f, 92f),
                    new Color(0.1f, 0.11f, 0.16f, 1f));
                CreateOutline(row, new Color(0.28f, 0.31f, 0.4f, 1f), new Vector2(2f, -2f));
                CreateLabel(row.transform, "RoleName", roleNames[i], new Vector2(0.37f, 0.5f), new Vector2(320f, 70f), 32, Color.white);

                var toggleBox = CreatePanel(
                    row.transform,
                    "ToggleBox",
                    new Vector2(0.86f, 0.5f),
                    new Vector2(58f, 58f),
                    new Color(0.04f, 0.045f, 0.07f, 1f));
                var checkmark = CreateLabel(
                    toggleBox.transform,
                    "Checkmark",
                    "✓",
                    new Vector2(0.5f, 0.5f),
                    new Vector2(54f, 54f),
                    38,
                    new Color(0.25f, 0.85f, 1f, 1f));
                var toggle = row.AddComponent<Toggle>();
                toggle.targetGraphic = toggleBox.GetComponent<Image>();
                toggle.graphic = checkmark;
                var capturedIndex = i;
                toggle.onValueChanged.AddListener(value => HandlePartyCompositionToggle(capturedIndex, value));
                row.GetComponent<Image>().raycastTarget = true;
                _partyCompositionToggles[i] = toggle;
            }

            var cancel = CreateMenuButton(panel.transform, "Cancel", "取消", new Vector2(0.32f, 0.09f), new Color(0.28f, 0.3f, 0.36f, 1f));
            cancel.onClick.AddListener(ClosePartyCompositionMenu);
            _partyCompositionConfirmButton = CreateMenuButton(
                panel.transform,
                "Confirm",
                "确定",
                new Vector2(0.68f, 0.09f),
                new Color(0.95f, 0.52f, 0.14f, 1f));
            _partyCompositionConfirmImage = _partyCompositionConfirmButton.targetGraphic as Image;
            _partyCompositionConfirmButton.onClick.AddListener(ConfirmPartyComposition);
            _partyCompositionMenu.SetActive(false);
        }

        private static Button CreateMenuButton(Transform parent, string name, string label, Vector2 anchor, Color color)
        {
            var go = CreatePanel(parent, name, anchor, new Vector2(230f, 72f), color);
            go.GetComponent<Image>().raycastTarget = true;
            var button = go.AddComponent<Button>();
            button.targetGraphic = go.GetComponent<Image>();
            CreateLabel(go.transform, "Label", label, new Vector2(0.5f, 0.5f), new Vector2(210f, 64f), 30, Color.white);
            return button;
        }

        private void RefreshPartyCompositionButton()
        {
            if (_partyCompositionButton == null || _director == null || _director.gameContext == null)
            {
                return;
            }

            _partyCompositionButton.SetActive(_director.gameContext.runState.hasRecruitedMage);
        }

        private void OpenPartyCompositionMenu()
        {
            if (_director == null || _director.gameContext == null || !_director.gameContext.runState.hasRecruitedMage)
            {
                return;
            }

            BattleSfx.PlayUiClick(true);
            var formation = _director.gameContext.defaultBattleFormation;
            if (formation == null)
            {
                return;
            }

            _partyCompositionOriginals = new[]
            {
                formation.frontCharacter,
                formation.midCharacter,
                formation.backCharacter,
            };
            var replacedId = _director.gameContext.runState.mageReplacedCharacterId;
            for (var i = 0; i < 3; i++)
            {
                _partyCompositionSelection[i] = _partyCompositionOriginals[i] != null &&
                                                _partyCompositionOriginals[i].id != replacedId;
            }
            _partyCompositionSelection[3] = !string.IsNullOrWhiteSpace(replacedId);

            for (var i = 0; i < _partyCompositionToggles.Length; i++)
            {
                _partyCompositionToggles[i].SetIsOnWithoutNotify(_partyCompositionSelection[i]);
            }
            RefreshPartyCompositionConfirm();
            _partyCompositionMenu.SetActive(true);
            _partyCompositionMenu.transform.SetAsLastSibling();
        }

        private void HandlePartyCompositionToggle(int index, bool isSelected)
        {
            if (index < 0 || index >= _partyCompositionSelection.Length)
            {
                return;
            }

            if (isSelected && CountPartyCompositionSelection() >= 3)
            {
                _partyCompositionToggles[index].SetIsOnWithoutNotify(false);
                return;
            }

            BattleSfx.PlayUiClick();
            _partyCompositionSelection[index] = isSelected;
            RefreshPartyCompositionConfirm();
        }

        private int CountPartyCompositionSelection()
        {
            var count = 0;
            for (var i = 0; i < _partyCompositionSelection.Length; i++)
            {
                if (_partyCompositionSelection[i])
                {
                    count++;
                }
            }
            return count;
        }

        private void RefreshPartyCompositionConfirm()
        {
            var canConfirm = CountPartyCompositionSelection() >= 3;
            _partyCompositionConfirmButton.interactable = canConfirm;
            if (_partyCompositionConfirmImage != null)
            {
                _partyCompositionConfirmImage.color = canConfirm
                    ? new Color(0.95f, 0.52f, 0.14f, 1f)
                    : new Color(0.28f, 0.29f, 0.33f, 1f);
            }
        }

        private void ConfirmPartyComposition()
        {
            if (CountPartyCompositionSelection() < 3 || _director == null || _director.gameContext == null)
            {
                return;
            }

            var replacedId = string.Empty;
            if (_partyCompositionSelection[3])
            {
                for (var i = 0; i < 3; i++)
                {
                    if (!_partyCompositionSelection[i] && _partyCompositionOriginals[i] != null)
                    {
                        replacedId = _partyCompositionOriginals[i].id;
                        break;
                    }
                }
            }

            _director.gameContext.runState.mageReplacedCharacterId = string.IsNullOrWhiteSpace(replacedId) ? null : replacedId;
            for (var i = 0; i < _partyDefaultConfigs.Length; i++)
            {
                if (_partyCooldowns[i] != null)
                {
                    StopCoroutine(_partyCooldowns[i]);
                    _partyCooldowns[i] = null;
                }
                if (_cooldownMasks[i] != null)
                {
                    _cooldownMasks[i].fillAmount = 0f;
                    _cooldownMasks[i].gameObject.SetActive(false);
                }
                _partyDefaultConfigs[i] = null;
                _partyEquippedRewards[i] = RewardType.None;
                _partyUnits[i] = null;
            }
            _director.RefreshPlayerParty();
            RefreshPartyCards();
            RefreshRelicPowerConnections();
            BattleSfx.PlayUiClick(true);
            ClosePartyCompositionMenu();
        }

        private void ClosePartyCompositionMenu()
        {
            BattleSfx.PlayUiClick();
            if (_partyCompositionMenu != null)
            {
                _partyCompositionMenu.SetActive(false);
            }
        }

        private void CreateRelicDragTip()
        {
            _relicDragTipRoot = new GameObject("RelicDragTip", typeof(RectTransform));
            _relicDragTipRoot.transform.SetParent(_root, false);
            var rootRect = _relicDragTipRoot.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            var tipPanel = CreatePanel(
                _relicDragTipRoot.transform,
                "TipTextBackground",
                new Vector2(0.5f, 0.145f),
                new Vector2(610f, 58f),
                new Color(0.035f, 0.04f, 0.06f, 0.96f));
            CreateOutline(tipPanel, new Color(1f, 0.68f, 0.18f, 0.95f), new Vector2(3f, -3f));
            CreateLabel(
                tipPanel.transform,
                "TipText",
                "\u53ef\u62d6\u62fd\u6b66\u5668\uff0c\u968f\u65f6\u4ea4\u7ed9\u5176\u4ed6\u89d2\u8272\u3002",
                new Vector2(0.5f, 0.5f),
                new Vector2(570f, 48f),
                24,
                new Color(1f, 0.92f, 0.72f, 1f));

            var arrow = CreateLabel(
                _relicDragTipRoot.transform,
                "TipArrow",
                "\u25bc",
                new Vector2(0.12f, 0.125f),
                new Vector2(62f, 62f),
                42,
                new Color(1f, 0.68f, 0.18f, 1f));
            _relicDragTipArrow = arrow.rectTransform;
            _relicDragTipRoot.SetActive(false);
        }

        private void CreateRelicBar()
        {
            _relicIcons = new Image[6];
            _relicSlotRects = new RectTransform[6];
            _relicOutlines = new Outline[6];
            _relicRewards = new RewardType[6];
            _selectionEffects = new RectTransform[6];
            _selectionEffectRoutines = new Coroutine[6];
            _relicCooldownMasks = new Image[6];
            _relicCooldownRoutines = new Coroutine[6];
            _relicCoolingDown = new bool[6];
            var startX = 0.12f;
            const float step = 0.15f;
            for (var i = 0; i < _relicIcons.Length; i++)
            {
                var slot = CreatePanel(_root, "RelicSlot_" + i, new Vector2(startX + i * step, 0.075f), new Vector2(92f, 92f), new Color(0.06f, 0.07f, 0.1f, 1f));
                _relicSlotRects[i] = slot.GetComponent<RectTransform>();
                _relicOutlines[i] = CreateOutline(slot, NormalRelicOutline, new Vector2(3f, -3f));
                _relicIcons[i] = CreateImage(slot.transform, "Icon", new Vector2(0.5f, 0.5f), new Vector2(66f, 66f), null, Color.white);
                _relicIcons[i].preserveAspect = true;

                var selectionEffect = CreatePanel(
                    slot.transform,
                    "RelicSelectionEffect",
                    new Vector2(0.5f, 0.5f),
                    new Vector2(104f, 104f),
                    new Color(1f, 0.66f, 0.16f, 0.12f));
                selectionEffect.GetComponent<Image>().raycastTarget = false;
                CreateOutline(selectionEffect, new Color(1f, 0.68f, 0.18f, 0.95f), new Vector2(4f, -4f));
                selectionEffect.SetActive(false);
                _selectionEffects[i] = selectionEffect.GetComponent<RectTransform>();

                var cooldownMask = CreateImage(
                    slot.transform,
                    "RelicCooldownMask",
                    new Vector2(0.5f, 0.5f),
                    new Vector2(92f, 92f),
                    SimpleSpriteFactory.GetWhitePixelSprite(),
                    new Color(0.08f, 0.45f, 0.72f, 0.55f));
                cooldownMask.type = Image.Type.Filled;
                cooldownMask.fillMethod = Image.FillMethod.Radial360;
                cooldownMask.fillOrigin = (int)Image.Origin360.Top;
                cooldownMask.fillClockwise = true;
                cooldownMask.fillAmount = 0f;
                cooldownMask.raycastTarget = false;
                cooldownMask.gameObject.SetActive(false);
                _relicCooldownMasks[i] = cooldownMask;

                var interaction = slot.AddComponent<RelicSlotInteraction>();
                interaction.Initialize(this, i);
            }
        }

        private void CreateRelicPowerConnections()
        {
            _relicPowerConnections = new GameObject[_relicIcons.Length];
            _relicPowerLines = new Image[_relicIcons.Length];
            _relicPowerDots = new Image[_relicIcons.Length][];
            for (var i = 0; i < _relicIcons.Length; i++)
            {
                var connection = new GameObject("RelicPowerConnection_" + i, typeof(RectTransform));
                connection.transform.SetParent(_root, false);
                connection.transform.SetSiblingIndex(0);
                var connectionRect = connection.GetComponent<RectTransform>();
                connectionRect.anchorMin = Vector2.zero;
                connectionRect.anchorMax = Vector2.one;
                connectionRect.offsetMin = Vector2.zero;
                connectionRect.offsetMax = Vector2.zero;

                var line = CreateImage(
                    connection.transform,
                    "EnergyLine",
                    new Vector2(0.5f, 0.5f),
                    new Vector2(100f, 6f),
                    SimpleSpriteFactory.GetWhitePixelSprite(),
                    new Color(RelicEnergyLineColor.r, RelicEnergyLineColor.g, RelicEnergyLineColor.b, 0.4f));
                line.raycastTarget = false;
                _relicPowerLines[i] = line;

                _relicPowerDots[i] = new Image[5];
                for (var dotIndex = 0; dotIndex < _relicPowerDots[i].Length; dotIndex++)
                {
                    var dot = CreateImage(
                        connection.transform,
                        "FlowDot_" + dotIndex,
                        new Vector2(0.5f, 0.5f),
                        new Vector2(13f, 13f),
                        SimpleSpriteFactory.GetWhitePixelSprite(),
                        new Color(RelicEnergyDotColor.r, RelicEnergyDotColor.g, RelicEnergyDotColor.b, 0.4f));
                    dot.raycastTarget = false;
                    _relicPowerDots[i][dotIndex] = dot;
                }

                connection.SetActive(false);
                _relicPowerConnections[i] = connection;
            }

            CreateRelicDragGuideConnection();
        }

        private void CreateRelicDragGuideConnection()
        {
            _relicDragGuideConnection = new GameObject("RelicDragGuideConnection", typeof(RectTransform));
            _relicDragGuideConnection.transform.SetParent(_relicDragTipRoot.transform, false);
            _relicDragGuideConnection.transform.SetAsFirstSibling();
            var rect = _relicDragGuideConnection.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            _relicDragGuideLine = CreateImage(
                _relicDragGuideConnection.transform,
                "GuideEnergyLine",
                new Vector2(0.5f, 0.5f),
                new Vector2(100f, 6f),
                SimpleSpriteFactory.GetWhitePixelSprite(),
                new Color(1f, 0.62f, 0.15f, 0.42f));
            _relicDragGuideLine.raycastTarget = false;

            _relicDragGuideDots = new Image[5];
            for (var i = 0; i < _relicDragGuideDots.Length; i++)
            {
                _relicDragGuideDots[i] = CreateImage(
                    _relicDragGuideConnection.transform,
                    "GuideFlowDot_" + i,
                    new Vector2(0.5f, 0.5f),
                    new Vector2(13f, 13f),
                    SimpleSpriteFactory.GetWhitePixelSprite(),
                    new Color(1f, 0.86f, 0.32f, 0.95f));
                _relicDragGuideDots[i].raycastTarget = false;
            }
            _relicDragGuideConnection.SetActive(false);
        }

        private void RefreshRelicPowerConnections()
        {
            if (_relicPowerConnections == null || _partyEquippedRewards == null || _root == null)
            {
                return;
            }

            for (var relicIndex = 0; relicIndex < _relicPowerConnections.Length; relicIndex++)
            {
                var tutorialActive = _relicDragTipRoot != null && _relicDragTipRoot.activeSelf;
                var holderIndex = -1;
                var reward = _relicRewards[relicIndex];
                for (var partyIndex = 0; partyIndex < _partyEquippedRewards.Length; partyIndex++)
                {
                    if (reward != RewardType.None && _partyEquippedRewards[partyIndex] == reward)
                    {
                        holderIndex = partyIndex;
                        break;
                    }
                }

                var shouldShow = !tutorialActive && holderIndex >= 0 && _relicSlotRects[relicIndex] != null &&
                                 _partyCards[holderIndex] != null;
                _relicPowerConnections[relicIndex].SetActive(shouldShow);
                if (!shouldShow)
                {
                    continue;
                }

                var relicPosition = (Vector2)_root.InverseTransformPoint(
                    _relicSlotRects[relicIndex].TransformPoint(Vector3.zero));
                var partyRect = _partyCards[holderIndex].GetComponent<RectTransform>();
                var partyPosition = (Vector2)_root.InverseTransformPoint(partyRect.TransformPoint(Vector3.zero));
                var direction = partyPosition - relicPosition;
                var connectionAlpha = reward != RewardType.None && reward == _draggedRelicReward ? 1f : 0.4f;
                _relicPowerLines[relicIndex].color = new Color(
                    RelicEnergyLineColor.r,
                    RelicEnergyLineColor.g,
                    RelicEnergyLineColor.b,
                    connectionAlpha);
                var lineRect = _relicPowerLines[relicIndex].rectTransform;
                lineRect.anchoredPosition = relicPosition + direction * 0.5f;
                lineRect.sizeDelta = new Vector2(direction.magnitude, 6f);
                lineRect.localEulerAngles = new Vector3(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);

                for (var dotIndex = 0; dotIndex < _relicPowerDots[relicIndex].Length; dotIndex++)
                {
                    var flow = Mathf.Repeat(Time.unscaledTime * RelicEnergyFlowSpeed + dotIndex / 5f, 1f);
                    _relicPowerDots[relicIndex][dotIndex].color =
                        new Color(RelicEnergyDotColor.r, RelicEnergyDotColor.g, RelicEnergyDotColor.b, connectionAlpha);
                    var dotRect = _relicPowerDots[relicIndex][dotIndex].rectTransform;
                    dotRect.anchoredPosition = Vector2.Lerp(relicPosition, partyPosition, flow);
                    dotRect.localScale = Vector3.one * (0.72f + Mathf.Sin(flow * Mathf.PI) * 0.45f);
                }
            }

            RefreshRelicDragGuideConnection();
        }

        private void RefreshRelicDragGuideConnection()
        {
            if (_relicDragGuideConnection == null || _relicDragTipRoot == null ||
                !_relicDragTipRoot.activeSelf || _partyCards == null)
            {
                if (_relicDragGuideConnection != null)
                {
                    _relicDragGuideConnection.SetActive(false);
                }
                return;
            }

            var hammerIndex = GetRelicIndex(RewardType.MeteorHammer);
            if (hammerIndex < 0 || _relicSlotRects[hammerIndex] == null)
            {
                _relicDragGuideConnection.SetActive(false);
                return;
            }

            var holderIndex = -1;
            for (var i = 0; i < _partyEquippedRewards.Length; i++)
            {
                if (_partyEquippedRewards[i] == RewardType.MeteorHammer)
                {
                    holderIndex = i;
                    break;
                }
            }

            var relicPosition = (Vector2)_root.InverseTransformPoint(
                _relicSlotRects[hammerIndex].TransformPoint(Vector3.zero));
            if (_relicDragGuideTargetPartyIndex < 0)
            {
                var nearestDistance = float.MaxValue;
                for (var i = 0; i < _partyCards.Length; i++)
                {
                    if (i == holderIndex || _partyCards[i] == null || _partyUnits[i] == null || !_partyUnits[i].IsAlive)
                    {
                        continue;
                    }

                    var cardRect = _partyCards[i].GetComponent<RectTransform>();
                    var cardPosition = (Vector2)_root.InverseTransformPoint(cardRect.TransformPoint(Vector3.zero));
                    var distance = Vector2.SqrMagnitude(cardPosition - relicPosition);
                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        _relicDragGuideTargetPartyIndex = i;
                    }
                }
            }

            var targetIndex = _relicDragGuideHoverPartyIndex >= 0
                ? _relicDragGuideHoverPartyIndex
                : _relicDragGuideTargetPartyIndex;
            if (targetIndex < 0 || targetIndex >= _partyCards.Length || _partyCards[targetIndex] == null ||
                _partyUnits[targetIndex] == null || !_partyUnits[targetIndex].IsAlive)
            {
                _relicDragGuideConnection.SetActive(false);
                return;
            }

            _relicDragGuideConnection.SetActive(true);
            var targetRect = _partyCards[targetIndex].GetComponent<RectTransform>();
            var targetPosition = (Vector2)_root.InverseTransformPoint(targetRect.TransformPoint(Vector3.zero));
            var direction = targetPosition - relicPosition;
            _relicDragGuideLine.color = new Color(1f, 0.62f, 0.15f, 0.42f);
            var lineRect = _relicDragGuideLine.rectTransform;
            lineRect.anchoredPosition = relicPosition + direction * 0.5f;
            lineRect.sizeDelta = new Vector2(direction.magnitude, 6f);
            lineRect.localEulerAngles = new Vector3(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);

            for (var i = 0; i < _relicDragGuideDots.Length; i++)
            {
                var flow = Mathf.Repeat(Time.unscaledTime * RelicGuideFlowSpeed + i / 5f, 1f);
                var dot = _relicDragGuideDots[i];
                dot.color = new Color(1f, 0.86f, 0.32f, 0.95f);
                dot.rectTransform.anchoredPosition = Vector2.Lerp(relicPosition, targetPosition, flow);
                dot.rectTransform.localScale = Vector3.one * (0.72f + Mathf.Sin(flow * Mathf.PI) * 0.45f);
            }
        }

        private IEnumerator RefreshHudRoutine()
        {
            yield return null;
            yield return null;

            RefreshPartyCards();
            RefreshRelicIcons();
        }

        private void SetWaveStarted(int waveIndex, int totalWaves, int enemyTotal)
        {
            _activeWave = waveIndex;
            _totalWaves = Mathf.Max(1, totalWaves);
            _waveEnemyTotal = Mathf.Max(1, enemyTotal);
            if (_trailProgress != null)
            {
                _trailProgress.gameObject.SetActive(true);
            }

            SetWaveEnemyCount(enemyTotal);
            RefreshTrail(0f);
            SetEncounterBackdrop(waveIndex, false);
            PlayMusic(MusicMode.Battle);
        }

        private void SetWaveEnemyCount(int remainingEnemies)
        {
            if (_waveCountLabel == null)
            {
                return;
            }

            var defeated = Mathf.Clamp(_waveEnemyTotal - remainingEnemies, 0, _waveEnemyTotal);
            _waveCountLabel.text = "\u5c0f\u602a " + defeated + "/" + _waveEnemyTotal;
            RefreshTrail(defeated / (float)_waveEnemyTotal);
        }

        private void SetWaveCleared(int waveIndex, int totalWaves)
        {
            _activeWave = waveIndex;
            _totalWaves = Mathf.Max(1, totalWaves);
            RefreshTrail(1f);
        }

        private void SetBossIncoming()
        {
            if (_waveCountLabel != null)
            {
                _waveCountLabel.text = string.Empty;
            }
            if (_stageTitleLabel != null)
            {
                _stageTitleLabel.text = "Boss\u6765\u88ad";
                _stageTitleLabel.color = new Color(1f, 0.57f, 0.22f, 1f);
            }

            RefreshTrail(1f, true);
        }

        private IEnumerator BossBackdropEntranceRoutine()
        {
            SetBossIncoming();
            var departingProps = _environmentProps;
            var departingParallax = departingProps != null
                ? departingProps.GetComponent<BattleEnvironmentPropParallax>()
                : null;
            if (departingParallax != null)
            {
                departingParallax.enabled = false;
            }

            SetEncounterBackdrop(3, true, true);
            if (_environmentProps == null)
            {
                if (departingProps != null)
                {
                    Destroy(departingProps);
                }
                PlayMusic(MusicMode.Boss);
                yield break;
            }

            var parallax = _environmentProps.GetComponent<BattleEnvironmentPropParallax>();
            if (parallax != null)
            {
                parallax.enabled = false;
            }

            var targetPosition = _environmentProps.transform.position;
            var startPosition = targetPosition + Vector3.right * 13.5f;
            _environmentProps.transform.position = startPosition;
            var departingStartPosition = departingProps != null ? departingProps.transform.position : Vector3.zero;
            var departingTargetPosition = departingStartPosition + Vector3.left * 13.5f;
            const float duration = 4.4f / 1.3f;
            var elapsed = 0f;
            while (elapsed < duration && _environmentProps != null)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                _environmentProps.transform.position = Vector3.Lerp(
                    startPosition,
                    targetPosition,
                    Mathf.SmoothStep(0f, 1f, t));
                if (departingProps != null)
                {
                    departingProps.transform.position = Vector3.Lerp(
                        departingStartPosition,
                        departingTargetPosition,
                        Mathf.SmoothStep(0f, 1f, t));
                }
                yield return null;
            }

            if (_environmentProps != null)
            {
                _environmentProps.transform.position = targetPosition;
            }
            if (departingProps != null)
            {
                Destroy(departingProps);
            }
            PlayMusic(MusicMode.Boss);
        }

        private void SetRouteCompleted()
        {
            PlayMusic(MusicMode.Battle);
            if (_waveCountLabel != null)
            {
                _waveCountLabel.text = "\u533a\u57df\u6e05\u5b8c";
            }
            RefreshTrail(1f);
            if (_trailNodes != null && _trailNodes.Length > 0)
            {
                // Validation routes end in a rescue/reward beat, not a Boss warning.
                _trailNodes[_trailNodes.Length - 1].color = new Color(0.2f, 0.72f, 0.36f, 1f);
            }
        }

        private void RefreshTrail(float waveProgress, bool bossIncoming = false)
        {
            if (_trailNodes == null || _trailNodes.Length == 0)
            {
                return;
            }

            var completed = Mathf.Clamp(_activeWave + (waveProgress >= 1f ? 1 : 0), 0, _totalWaves);
            for (var i = 0; i < _trailNodes.Length; i++)
            {
                if (bossIncoming && i == _trailNodes.Length - 1)
                {
                    _trailNodes[i].color = new Color(0.95f, 0.34f, 0.11f, 1f);
                }
                else if (i < completed)
                {
                    _trailNodes[i].color = new Color(0.76f, 0.55f, 0.26f, 1f);
                }
                else if (i == _activeWave && !bossIncoming)
                {
                    _trailNodes[i].color = new Color(0.16f, 0.55f, 0.95f, 1f);
                }
                else
                {
                    _trailNodes[i].color = new Color(0.28f, 0.22f, 0.16f, 1f);
                }
            }

            if (_trailProgress != null)
            {
                var totalProgress = Mathf.Clamp01((_activeWave + waveProgress) / Mathf.Max(1f, _totalWaves));
                _trailProgress.sizeDelta = new Vector2(334f * totalProgress, 10f);
            }
        }

        private void SetEncounterBackdrop(int index, bool isBoss, bool preserveExistingProps = false)
        {
            var battleCamera = Camera.main;
            if (battleCamera == null)
            {
                return;
            }

            var colors = new[]
            {
                new Color(0.12f, 0.12f, 0.18f),
                new Color(0.08f, 0.16f, 0.18f),
                new Color(0.17f, 0.12f, 0.18f),
                new Color(0.24f, 0.07f, 0.08f),
            };
            battleCamera.backgroundColor = colors[Mathf.Clamp(isBoss ? 3 : index, 0, colors.Length - 1)];
            CreateEnvironmentProps(isBoss, preserveExistingProps);
        }

        private void CreateEnvironmentProps(bool isBoss, bool preserveExistingProps = false)
        {
            // The three minor encounters share one persistent midground. Only the Boss
            // transition replaces it with the existing Boss-specific prop arrangement.
            if (!isBoss && _environmentProps != null && !_environmentPropsAreBoss)
            {
                return;
            }

            if (_environmentProps != null && !preserveExistingProps)
            {
                Destroy(_environmentProps);
            }

            _environmentProps = new GameObject("BattleEnvironmentProps");
            _environmentPropsAreBoss = isBoss;
            var environmentParallax = _environmentProps.AddComponent<BattleEnvironmentPropParallax>();
            environmentParallax.enabled = !isBoss;
            var themeName = isBoss ? "bg_lava_texture" : "bg_village_mill";
            var sprite = ExtractedArtLibrary.LoadEnvironment(themeName);
            if (sprite == null)
            {
                return;
            }

            if (isBoss)
            {
                // Lava tiles are 64 px wide at 32 PPU. At 1.75 scale each tile is
                // exactly 3.5 world units wide, matching the center-to-center spacing.
                const int bossPropCount = 10;
                for (var i = 0; i < bossPropCount; i++)
                {
                    CreateEnvironmentProp(
                        themeName + "_" + i,
                        sprite,
                        new Vector3(-5.25f + i * 3.5f, -1.91f, 4f),
                        1.75f,
                        new Color(1f, 1f, 1f, 0.32f));
                }
                return;
            }

            var propPositions = new[]
            {
                new Vector3(-5.3f, 2.41f, 4f),
                new Vector3(0.3f, 3.65f, 4f),
                new Vector3(5.4f, 1.89f, 4f),
            };
            for (var i = 0; i < propPositions.Length; i++)
            {
                CreateEnvironmentProp(
                    themeName + "_" + i,
                    sprite,
                    propPositions[i],
                    i == 2 ? 1.3f : i == 1 ? 2.2f : 1.7f,
                    new Color(1f, 1f, 1f, 0.58f));
            }
        }

        private void CreateEnvironmentProp(string name, Sprite sprite, Vector3 position, float scale, Color color)
        {
            var decoration = new GameObject(name);
            decoration.transform.SetParent(_environmentProps.transform, false);
            decoration.transform.position = position;
            decoration.transform.localScale = Vector3.one * scale;
            var renderer = decoration.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = -90;
        }

        private void CreateMusic()
        {
            var listener = FindObjectOfType<AudioListener>();
            if (listener == null && Camera.main != null)
            {
                Camera.main.gameObject.AddComponent<AudioListener>();
            }

            _musicSource = gameObject.AddComponent<AudioSource>();
            _musicSource.loop = true;
            _musicSource.volume = 0.08f;
            _musicSource.clip = CreateMusicLoop(MusicMode.Battle);
            _musicSource.Play();
        }

        private void PlayMusic(MusicMode mode)
        {
            if (_musicSource == null)
            {
                return;
            }

            var desired = GetMusicClipName(mode);
            if (_musicSource.clip != null && _musicSource.clip.name == desired)
            {
                return;
            }

            _musicSource.clip = CreateMusicLoop(mode);
            _musicSource.Play();
        }

        private static AudioClip CreateMusicLoop(MusicMode mode)
        {
            var packagedClip = Resources.Load<AudioClip>("Audio/ProjectHunt/" + GetMusicClipName(mode));
            if (packagedClip != null)
            {
                return packagedClip;
            }

            const int sampleRate = 22050;
            const int seconds = 4;
            var samples = new float[sampleRate * seconds];
            var pattern = mode == MusicMode.Boss
                ? new[] { 82.41f, 98f, 73.42f, 92.5f }
                : mode == MusicMode.Treasure
                    ? new[] { 392f, 493.88f, 587.33f, 783.99f }
                    : new[] { 220f, 261.63f, 293.66f, 261.63f };
            for (var i = 0; i < samples.Length; i++)
            {
                var time = i / (float)sampleRate;
                var step = Mathf.FloorToInt(time * 2f) % pattern.Length;
                var noteTime = time % 0.5f;
                var envelope = Mathf.Clamp01(1f - noteTime * (mode == MusicMode.Treasure ? 1.25f : 1.7f));
                var wave = Mathf.Sin(time * pattern[step] * Mathf.PI * 2f);
                var harmony = mode == MusicMode.Treasure
                    ? Mathf.Sin(time * pattern[step] * 1.5f * Mathf.PI * 2f) * 0.35f
                    : 0f;
                samples[i] = (wave + harmony) * envelope * (mode == MusicMode.Boss ? 0.18f : 0.12f);
            }

            var clip = AudioClip.Create(GetMusicClipName(mode), samples.Length, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static string GetMusicClipName(MusicMode mode)
        {
            return mode == MusicMode.Boss
                ? "snd_music_final_battle_1"
                : mode == MusicMode.Treasure
                    ? "snd_music_village_choices_1"
                    : "snd_music_village_battle_1";
        }

        private void RefreshPartyCards()
        {
            if (_director == null || _director.formationSpawner == null || _partyPortraits == null)
            {
                return;
            }

            var players = _director.formationSpawner.SpawnedPlayers;
            for (var i = 0; i < _partyPortraits.Length; i++)
            {
                if (i >= players.Count || players[i] == null)
                {
                    continue;
                }

                var controller = players[i].GetComponent<CombatUnitController>();
                if (controller == null || controller.characterConfig == null)
                {
                    continue;
                }

                var config = controller.characterConfig;
                _partyUnits[i] = controller;
                if (_partyDefaultConfigs[i] == null)
                {
                    _partyDefaultConfigs[i] = CreateDefaultCharacterConfig(config);
                    _partyEquippedRewards[i] = GetEquippedReward(config);
                }
                var portraitSprite = PixelAnimationLibrary.GetFirstFrameSprite(config.resourceId, "walk", "attack", "idle");
                _partyPortraits[i].sprite = BuildCharacterSlotView.GetCroppedPortraitSprite(portraitSprite);
                RefreshPartyCompositeWeaponOverlay(i, config);
            }

            ApplyStoredPartyCardOrder();
        }

        private void RefreshRelicIcons()
        {
            if (_relicIcons == null || _director == null || _director.gameContext == null)
            {
                return;
            }

            var selection = _director.gameContext.buildSelection;
            var sprites = new[]
            {
                selection.hasClaimedMeteorHammer ? SimpleSpriteFactory.GetMeteorHammerSprite() : null,
                selection.hasClaimedHolyCup ? SimpleSpriteFactory.GetHolyCupSprite() : null,
                selection.hasClaimedGiantKey ? SimpleSpriteFactory.GetGiantKeySprite() : null,
            };
            var rewards = new[] { RewardType.MeteorHammer, RewardType.HolyCup, RewardType.GiantKey };
            for (var i = 0; i < _relicIcons.Length; i++)
            {
                _relicIcons[i].sprite = i < sprites.Length ? sprites[i] : null;
                _relicIcons[i].color = _relicIcons[i].sprite != null ? Color.white : new Color(0f, 0f, 0f, 0f);
                _relicRewards[i] = i < rewards.Length && sprites[i] != null ? rewards[i] : RewardType.None;
            }

            ApplyStoredRelicSlotOrder();

            RefreshRelicDragTip();
        }

        private void RefreshRelicDragTip()
        {
            if (_relicDragTipRoot == null || _director == null || _director.gameContext == null)
            {
                return;
            }

            var selection = _director.gameContext.buildSelection;
            var shouldShow = selection.hasClaimedMeteorHammer && !selection.hasCompletedRelicDragTip;
            _relicDragTipRoot.SetActive(shouldShow);
            if (shouldShow && _relicDragTipRoutine == null)
            {
                _relicDragTipRoutine = StartCoroutine(AnimateRelicDragTipArrow());
            }
            else if (!shouldShow && _relicDragTipRoutine != null)
            {
                StopCoroutine(_relicDragTipRoutine);
                _relicDragTipRoutine = null;
            }
            if (!shouldShow)
            {
                _relicDragGuideTargetPartyIndex = -1;
                _relicDragGuideHoverPartyIndex = -1;
            }
        }

        private IEnumerator AnimateRelicDragTipArrow()
        {
            var baseY = _relicDragTipArrow.anchoredPosition.y;
            while (_relicDragTipRoot != null && _relicDragTipRoot.activeSelf)
            {
                UpdateRelicDragTipArrowAnchor();
                var offset = Mathf.Sin(Time.unscaledTime * 5f) * 9f;
                _relicDragTipArrow.anchoredPosition = new Vector2(0f, baseY + offset);
                yield return null;
            }

            if (_relicDragTipArrow != null)
            {
                _relicDragTipArrow.anchoredPosition = new Vector2(0f, baseY);
            }
            _relicDragTipRoutine = null;
        }

        private void UpdateRelicDragTipArrowAnchor()
        {
            if (_relicDragTipArrow == null || _relicSlotRects == null)
            {
                return;
            }

            var hammerIndex = GetRelicIndex(RewardType.MeteorHammer);
            if (hammerIndex < 0 || hammerIndex >= _relicSlotRects.Length || _relicSlotRects[hammerIndex] == null)
            {
                return;
            }

            var hammerAnchorX = _relicSlotRects[hammerIndex].anchorMin.x;
            var arrowAnchor = new Vector2(hammerAnchorX, 0.125f);
            _relicDragTipArrow.anchorMin = arrowAnchor;
            _relicDragTipArrow.anchorMax = arrowAnchor;
        }

        private void CompleteRelicDragTip()
        {
            if (_director == null || _director.gameContext == null)
            {
                return;
            }

            _director.gameContext.buildSelection.hasCompletedRelicDragTip = true;
            RefreshRelicDragTip();
        }

        public void TogglePartySelection(int partyIndex)
        {
            if (!IsValidPartyIndex(partyIndex))
            {
                return;
            }

            _selectedPartyIndex = _selectedPartyIndex == partyIndex ? -1 : partyIndex;
            RefreshSelectionVisuals();
        }

        public void ClickRelic(int relicIndex)
        {
            if (_selectedPartyIndex < 0 || !IsUsableRelic(relicIndex))
            {
                return;
            }

            TryAssignRelic(_selectedPartyIndex, _relicRewards[relicIndex]);
        }

        public bool DropRelic(int relicIndex, Vector2 screenPosition)
        {
            if (!IsUsableRelic(relicIndex))
            {
                return false;
            }

            var partyIndex = FindPartyCardAt(screenPosition);
            if (partyIndex < 0)
            {
                partyIndex = FindBattleUnitAt(screenPosition);
            }

            if (partyIndex >= 0)
            {
                return TryAssignRelic(partyIndex, _relicRewards[relicIndex]);
            }

            return false;
        }

        private bool TryAssignRelic(int partyIndex, RewardType rewardType)
        {
            if (!IsValidPartyIndex(partyIndex) || rewardType == RewardType.None || IsRelicCoolingDown(rewardType))
            {
                return false;
            }

            if (_partyEquippedRewards[partyIndex] == rewardType)
            {
                return false;
            }

            var unit = _partyUnits[partyIndex];
            if (unit == null || !unit.IsAlive || unit.characterConfig == null)
            {
                return false;
            }

            ResetCurrentRelicHolder(rewardType, partyIndex);
            ResetPartyToDefault(partyIndex);
            _selectedPartyIndex = -1;
            RefreshSelectionVisuals();
            StartRelicCooldown(rewardType);
            StartCoroutine(PlayRelicTossAboveUnit(unit, rewardType));
            _partyCooldowns[partyIndex] = StartCoroutine(TemporaryRelicRoutine(partyIndex, unit, rewardType));
            return true;
        }

        private IEnumerator PlayRelicTossAboveUnit(CombatUnitController unit, RewardType rewardType)
        {
            if (unit == null)
            {
                yield break;
            }

            var sprite = rewardType == RewardType.MeteorHammer
                ? SimpleSpriteFactory.GetMeteorHammerSprite()
                : rewardType == RewardType.HolyCup
                    ? SimpleSpriteFactory.GetHolyCupSprite()
                    : SimpleSpriteFactory.GetGiantKeySprite();
            if (sprite == null)
            {
                yield break;
            }

            var bodyRenderer = unit.GetComponent<SpriteRenderer>();
            var headOffset = bodyRenderer != null && bodyRenderer.sprite != null
                ? bodyRenderer.bounds.max.y - unit.transform.position.y + 0.18f
                : 1.35f;
            if (unit.characterConfig != null && unit.characterConfig.roleType == RoleType.Assassin)
            {
                // Assassin frames have extra transparent/pivot space that inflates renderer bounds.
                headOffset = 1.45f;
            }
            var tossObject = new GameObject("RelicCombatToss_" + rewardType);
            var tossRenderer = tossObject.AddComponent<SpriteRenderer>();
            tossRenderer.sprite = sprite;
            tossRenderer.sortingOrder = 32;

            var spriteSize = sprite.bounds.size;
            var largestDimension = Mathf.Max(0.01f, spriteSize.x, spriteSize.y);
            tossObject.transform.localScale = Vector3.one * (0.62f / largestDimension);

            const float duration = 0.65f;
            const float arcHeight = 0.85f;
            var elapsed = 0f;
            while (elapsed < duration && unit != null)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                var headPosition = unit.transform.position + Vector3.up * headOffset;
                tossObject.transform.position = headPosition + Vector3.up * (Mathf.Sin(t * Mathf.PI) * arcHeight);
                tossObject.transform.localEulerAngles = new Vector3(0f, 0f, -360f * t);
                yield return null;
            }

            Destroy(tossObject);
        }

        private void StartRelicCooldown(RewardType rewardType)
        {
            var relicIndex = GetRelicIndex(rewardType);
            if (relicIndex < 0)
            {
                return;
            }

            if (_relicCooldownRoutines[relicIndex] != null)
            {
                StopCoroutine(_relicCooldownRoutines[relicIndex]);
            }
            _relicCooldownRoutines[relicIndex] = StartCoroutine(RelicCooldownRoutine(relicIndex));
        }

        private IEnumerator RelicCooldownRoutine(int relicIndex)
        {
            _relicCoolingDown[relicIndex] = true;
            var mask = _relicCooldownMasks[relicIndex];
            mask.fillAmount = 1f;
            mask.gameObject.SetActive(true);
            RefreshSelectionVisuals();

            var elapsed = 0f;
            while (elapsed < RelicCooldownDuration)
            {
                elapsed += Time.deltaTime;
                mask.fillAmount = 1f - Mathf.Clamp01(elapsed / RelicCooldownDuration);
                yield return null;
            }

            mask.fillAmount = 0f;
            mask.gameObject.SetActive(false);
            _relicCoolingDown[relicIndex] = false;
            _relicCooldownRoutines[relicIndex] = null;
            RefreshSelectionVisuals();
        }

        private bool IsRelicCoolingDown(RewardType rewardType)
        {
            var relicIndex = GetRelicIndex(rewardType);
            return relicIndex >= 0 && _relicCoolingDown[relicIndex];
        }

        private int GetRelicIndex(RewardType rewardType)
        {
            if (_relicRewards == null || rewardType == RewardType.None)
            {
                return -1;
            }

            for (var i = 0; i < _relicRewards.Length; i++)
            {
                if (_relicRewards[i] == rewardType)
                {
                    return i;
                }
            }

            return -1;
        }

        private void SetRelicDragState(RewardType rewardType, bool isDragging)
        {
            if (_relicProhibitedIcons == null || _partyEquippedRewards == null)
            {
                return;
            }

            _draggedRelicReward = isDragging ? rewardType : RewardType.None;
            if (!isDragging || rewardType != RewardType.MeteorHammer)
            {
                _relicDragGuideHoverPartyIndex = -1;
            }

            for (var i = 0; i < _relicProhibitedIcons.Length; i++)
            {
                if (_relicProhibitedIcons[i] != null)
                {
                    _relicProhibitedIcons[i].gameObject.SetActive(
                        isDragging && _partyEquippedRewards[i] == rewardType);
                }
            }
        }

        private void UpdateRelicDragGuideHover(RewardType rewardType, Vector2 screenPosition)
        {
            _relicDragGuideHoverPartyIndex = -1;
            if (rewardType != RewardType.MeteorHammer || _relicDragTipRoot == null ||
                !_relicDragTipRoot.activeSelf)
            {
                return;
            }

            var hoveredPartyIndex = FindPartyCardAt(screenPosition);
            if (!IsValidPartyIndex(hoveredPartyIndex) ||
                _partyEquippedRewards[hoveredPartyIndex] == rewardType)
            {
                return;
            }

            var unit = _partyUnits[hoveredPartyIndex];
            if (unit != null && unit.IsAlive && unit.characterConfig != null)
            {
                _relicDragGuideHoverPartyIndex = hoveredPartyIndex;
            }
        }

        private void ShowNewUnitDiscoveryTip(
            int partyIndex,
            CharacterConfig baseConfig,
            CharacterConfig discoveredConfig,
            RewardType rewardType)
        {
            if (_director == null || _director.gameContext == null || discoveredConfig == null ||
                _director.gameContext.buildSelection.HasDiscoveredUnit(discoveredConfig.id))
            {
                return;
            }

            _pendingDiscoverBaseConfigs[partyIndex] = baseConfig;
            _pendingDiscoverConfigs[partyIndex] = discoveredConfig;
            _pendingDiscoverRewards[partyIndex] = rewardType;
            _newUnitTipBars[partyIndex].SetActive(true);
            _partyStatBars[partyIndex].SetActive(false);
        }

        private void OpenPendingUnitDiscovery(int partyIndex)
        {
            var discoveredConfig = _pendingDiscoverConfigs[partyIndex];
            if (discoveredConfig == null || _root == null)
            {
                return;
            }

            if (_discoverPresentation == null)
            {
                _discoverPresentation = DiscoverUnitPresentationController.Create(_root.GetComponentInParent<Canvas>());
            }

            _discoverPresentation.PlayDiscoverAcknowledgement(
                _pendingDiscoverBaseConfigs[partyIndex],
                discoveredConfig,
                _pendingDiscoverRewards[partyIndex],
                () => CompletePendingUnitDiscovery(discoveredConfig.id));
        }

        private void CompletePendingUnitDiscovery(string discoveredUnitId)
        {
            if (_director != null && _director.gameContext != null)
            {
                _director.gameContext.buildSelection.MarkUnitDiscovered(discoveredUnitId);
            }

            for (var i = 0; i < _pendingDiscoverConfigs.Length; i++)
            {
                if (_pendingDiscoverConfigs[i] != null && _pendingDiscoverConfigs[i].id == discoveredUnitId)
                {
                    _pendingDiscoverBaseConfigs[i] = null;
                    _pendingDiscoverConfigs[i] = null;
                    _pendingDiscoverRewards[i] = RewardType.None;
                    _newUnitTipBars[i].SetActive(false);
                }
            }
            RefreshSelectionVisuals();
        }

        private IEnumerator TemporaryRelicRoutine(int partyIndex, CombatUnitController unit, RewardType rewardType)
        {
            var defaultConfig = _partyDefaultConfigs[partyIndex] ?? CreateDefaultCharacterConfig(unit.characterConfig);
            _partyDefaultConfigs[partyIndex] = defaultConfig;
            var temporaryConfig = CreateTemporaryRelicVariant(defaultConfig, rewardType);
            if (temporaryConfig == null)
            {
                _partyCooldowns[partyIndex] = null;
                yield break;
            }

            PersistRelicAssignment(defaultConfig, temporaryConfig, rewardType);
            _partyEquippedRewards[partyIndex] = rewardType;
            unit.ApplyTemporaryCharacterConfig(temporaryConfig);
            RefreshPartyPortrait(partyIndex, temporaryConfig);
            ShowNewUnitDiscoveryTip(partyIndex, defaultConfig, temporaryConfig, rewardType);
            var cooldownMask = _cooldownMasks[partyIndex];
            cooldownMask.fillAmount = 1f;
            cooldownMask.gameObject.SetActive(true);
            PlayRelicSelectionEffect(rewardType);
            BattleSfx.PlayClaim();

            var elapsed = 0f;
            while (elapsed < RelicCooldownDuration && unit != null && unit.IsAlive)
            {
                elapsed += Time.deltaTime;
                cooldownMask.fillAmount = 1f - Mathf.Clamp01(elapsed / RelicCooldownDuration);
                yield return null;
            }

            cooldownMask.fillAmount = 0f;
            cooldownMask.gameObject.SetActive(false);
            _partyCooldowns[partyIndex] = null;
        }

        private void PersistRelicAssignment(
            CharacterConfig baseConfig,
            CharacterConfig relicConfig,
            RewardType rewardType)
        {
            if (_director == null || _director.gameContext == null || baseConfig == null || relicConfig == null)
            {
                return;
            }

            var selection = _director.gameContext.buildSelection;
            var runState = _director.gameContext.runState;

            // Moving a relic resets its previous owner, and the recipient can only hold one relic.
            var previousRole = GetPersistentRelicHolderRole(selection, runState, rewardType);
            if (previousRole.HasValue)
            {
                ClearPersistentRelicsForRole(selection, runState, previousRole.Value);
            }
            ClearPersistentRelicsForRole(selection, runState, baseConfig.roleType);

            if (baseConfig.roleType == RoleType.Mage)
            {
                runState.mageRewardType = rewardType;
                return;
            }

            switch (rewardType)
            {
                case RewardType.MeteorHammer:
                    selection.selectedCharacter = baseConfig;
                    selection.selectedHammerCharacter = relicConfig;
                    selection.selectedHammerTargetId = baseConfig.id;
                    selection.isSelectionConfirmed = true;
                    break;
                case RewardType.HolyCup:
                    selection.selectedCupCharacter = relicConfig;
                    selection.selectedCupTargetId = baseConfig.id;
                    break;
                case RewardType.GiantKey:
                    selection.selectedKeyCharacter = relicConfig;
                    selection.selectedKeyTargetId = baseConfig.id;
                    break;
            }
        }

        private static RoleType? GetPersistentRelicHolderRole(
            BuildSelectionState selection,
            DemoRunState runState,
            RewardType rewardType)
        {
            if (runState.hasRecruitedMage && runState.mageRewardType == rewardType)
            {
                return RoleType.Mage;
            }

            var config = rewardType == RewardType.MeteorHammer
                ? selection.selectedHammerCharacter
                : rewardType == RewardType.HolyCup
                    ? selection.selectedCupCharacter
                    : selection.selectedKeyCharacter;
            return config != null ? config.roleType : (RoleType?)null;
        }

        private static void ClearPersistentRelicsForRole(
            BuildSelectionState selection,
            DemoRunState runState,
            RoleType roleType)
        {
            if (selection.selectedHammerCharacter != null &&
                selection.selectedHammerCharacter.roleType == roleType)
            {
                selection.selectedCharacter = null;
                selection.selectedHammerCharacter = null;
                selection.selectedHammerTargetId = null;
                selection.isSelectionConfirmed = false;
            }

            if (selection.selectedCupCharacter != null && selection.selectedCupCharacter.roleType == roleType)
            {
                selection.selectedCupCharacter = null;
                selection.selectedCupTargetId = null;
            }

            if (selection.selectedKeyCharacter != null && selection.selectedKeyCharacter.roleType == roleType)
            {
                selection.selectedKeyCharacter = null;
                selection.selectedKeyTargetId = null;
            }

            if (roleType == RoleType.Mage)
            {
                runState.mageRewardType = RewardType.None;
            }
        }

        private void ResetCurrentRelicHolder(RewardType rewardType, int newHolderIndex)
        {
            for (var i = 0; i < _partyEquippedRewards.Length; i++)
            {
                if (i != newHolderIndex && _partyEquippedRewards[i] == rewardType)
                {
                    ResetPartyToDefault(i);
                }
            }
        }

        private void ResetPartyToDefault(int partyIndex)
        {
            if (!IsValidPartyIndex(partyIndex))
            {
                return;
            }

            if (_partyCooldowns[partyIndex] != null)
            {
                StopCoroutine(_partyCooldowns[partyIndex]);
                _partyCooldowns[partyIndex] = null;
            }

            var defaultConfig = _partyDefaultConfigs[partyIndex] ??
                                CreateDefaultCharacterConfig(_partyUnits[partyIndex].characterConfig);
            _partyDefaultConfigs[partyIndex] = defaultConfig;
            _partyEquippedRewards[partyIndex] = RewardType.None;
            if (_partyUnits[partyIndex].IsAlive && defaultConfig != null)
            {
                _partyUnits[partyIndex].ApplyTemporaryCharacterConfig(defaultConfig);
                RefreshPartyPortrait(partyIndex, defaultConfig);
            }

            if (_cooldownMasks[partyIndex] != null)
            {
                _cooldownMasks[partyIndex].fillAmount = 0f;
                _cooldownMasks[partyIndex].gameObject.SetActive(false);
            }
        }

        private void PlayRelicSelectionEffect(RewardType rewardType)
        {
            var relicIndex = FindRelicIndex(rewardType);
            if (relicIndex < 0 || _selectionEffects[relicIndex] == null)
            {
                return;
            }

            if (_selectionEffectRoutines[relicIndex] != null)
            {
                StopCoroutine(_selectionEffectRoutines[relicIndex]);
            }

            _selectionEffects[relicIndex].gameObject.SetActive(false);
            _selectionEffectRoutines[relicIndex] = StartCoroutine(PlayRelicSelectionEffectRoutine(relicIndex));
        }

        private IEnumerator PlayRelicSelectionEffectRoutine(int relicIndex)
        {
            var effect = _selectionEffects[relicIndex];
            effect.gameObject.SetActive(true);
            effect.localEulerAngles = Vector3.zero;
            var elapsed = 0f;
            const float duration = 0.5f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                effect.localEulerAngles = new Vector3(0f, 0f, -360f * elapsed / duration);
                yield return null;
            }

            effect.localEulerAngles = Vector3.zero;
            effect.gameObject.SetActive(false);
            _selectionEffectRoutines[relicIndex] = null;
        }

        private int FindRelicIndex(RewardType rewardType)
        {
            if (_relicRewards == null)
            {
                return -1;
            }

            for (var i = 0; i < _relicRewards.Length; i++)
            {
                if (_relicRewards[i] == rewardType)
                {
                    return i;
                }
            }

            return -1;
        }

        private static CharacterConfig CreateTemporaryRelicVariant(CharacterConfig currentConfig, RewardType rewardType)
        {
            if (currentConfig == null)
            {
                return null;
            }

            if (currentConfig.roleType == RoleType.Mage)
            {
                return MageCharacterFactory.GetMageVariant(rewardType);
            }

            var baseConfig = ScriptableObject.CreateInstance<CharacterConfig>();
            baseConfig.id = string.IsNullOrWhiteSpace(currentConfig.baseCharacterId)
                ? currentConfig.id
                : currentConfig.baseCharacterId;
            baseConfig.displayName = currentConfig.displayName;
            baseConfig.roleType = currentConfig.roleType;
            baseConfig.resourceId = currentConfig.roleType == RoleType.Swordsman
                ? "human_swordsman"
                : currentConfig.roleType == RoleType.Assassin ? "assassin" : "longbowman";
            baseConfig.defaultAttackAction = "attack";
            baseConfig.moveAction = "walk";
            baseConfig.visualScale = currentConfig.visualScale;
            baseConfig.yOffset = currentConfig.yOffset;
            baseConfig.defaultWeaponType = currentConfig.defaultWeaponType;
            baseConfig.attackTempo = currentConfig.attackTempo;
            baseConfig.attackRangeType = currentConfig.attackRangeType;
            baseConfig.spawnSlot = currentConfig.spawnSlot;
            baseConfig.isPlayable = true;

            return rewardType == RewardType.MeteorHammer
                ? HammerCharacterFactory.GetHammerVariant(baseConfig)
                : rewardType == RewardType.HolyCup
                    ? HolyCupCharacterFactory.GetCupVariant(baseConfig)
                    : GiantKeyCharacterFactory.GetKeyVariant(baseConfig);
        }

        private static RewardType GetEquippedReward(CharacterConfig config)
        {
            if (config == null)
            {
                return RewardType.None;
            }

            if (HammerCharacterFactory.IsHammerVariant(config) || config.id == "mage_meteorhammer")
            {
                return RewardType.MeteorHammer;
            }

            if (config.resourceId == "catapult" ||
                (!string.IsNullOrWhiteSpace(config.id) && config.id.EndsWith("_cup")) ||
                config.id == "mage_holycup")
            {
                return RewardType.HolyCup;
            }

            if ((!string.IsNullOrWhiteSpace(config.id) && config.id.EndsWith("_key")) ||
                config.id == "mage_giantkey")
            {
                return RewardType.GiantKey;
            }

            return RewardType.None;
        }

        private static CharacterConfig CreateDefaultCharacterConfig(CharacterConfig currentConfig)
        {
            if (currentConfig == null)
            {
                return null;
            }

            if (currentConfig.roleType == RoleType.Mage)
            {
                return MageCharacterFactory.GetMageVariant(RewardType.None);
            }

            var config = ScriptableObject.CreateInstance<CharacterConfig>();
            config.id = string.IsNullOrWhiteSpace(currentConfig.baseCharacterId)
                ? currentConfig.id
                : currentConfig.baseCharacterId;
            config.baseCharacterId = config.id;
            config.displayName = currentConfig.roleType == RoleType.Swordsman
                ? "剑士"
                : currentConfig.roleType == RoleType.Assassin ? "刺客" : "弓手";
            config.roleType = currentConfig.roleType;
            config.resourceId = currentConfig.roleType == RoleType.Swordsman
                ? "human_swordsman"
                : currentConfig.roleType == RoleType.Assassin ? "assassin" : "longbowman";
            config.defaultAttackAction = "attack";
            config.moveAction = "walk";
            config.visualScale = currentConfig.roleType == RoleType.Swordsman
                ? 3f
                : currentConfig.roleType == RoleType.Assassin ? 2.6f : 2.4f;
            config.yOffset = 0f;
            config.defaultWeaponType = currentConfig.roleType == RoleType.Archer
                ? WeaponType.Bow
                : WeaponType.Sword;
            config.attackTempo = currentConfig.roleType == RoleType.Assassin
                ? AttackTempo.Fast
                : currentConfig.roleType == RoleType.Archer ? AttackTempo.Slow : AttackTempo.Medium;
            config.attackRangeType = currentConfig.roleType == RoleType.Swordsman
                ? AttackRangeType.MeleeShort
                : AttackRangeType.RangedLine;
            config.spawnSlot = currentConfig.roleType == RoleType.Swordsman
                ? SpawnSlot.Front
                : currentConfig.roleType == RoleType.Assassin ? SpawnSlot.Mid : SpawnSlot.Back;
            config.isPlayable = true;
            return config;
        }

        private void RefreshPartyPortrait(int partyIndex, CharacterConfig config)
        {
            if (IsValidPartyIndex(partyIndex) && config != null)
            {
                var portraitSprite = PixelAnimationLibrary.GetFirstFrameSprite(config.resourceId, "walk", "attack", "idle");
                _partyPortraits[partyIndex].sprite = BuildCharacterSlotView.GetCroppedPortraitSprite(portraitSprite);
                RefreshPartyCompositeWeaponOverlay(partyIndex, config);
            }
        }

        private void RefreshPartyCompositeWeaponOverlay(int partyIndex, CharacterConfig config)
        {
            if (_partyCompositeWeaponOverlays == null || partyIndex < 0 ||
                partyIndex >= _partyCompositeWeaponOverlays.Length || _partyCompositeWeaponOverlays[partyIndex] == null)
            {
                return;
            }

            var overlay = _partyCompositeWeaponOverlays[partyIndex];
            var reward = GetEquippedReward(config);
            // Hammer variants already have dedicated painted portrait frames.
            if (reward == RewardType.None || reward == RewardType.MeteorHammer)
            {
                overlay.gameObject.SetActive(false);
                return;
            }

            overlay.sprite = reward == RewardType.HolyCup
                ? SimpleSpriteFactory.GetHolyCupSprite()
                : SimpleSpriteFactory.GetGiantKeySprite();
            overlay.gameObject.SetActive(overlay.sprite != null);
            var rect = overlay.rectTransform;
            rect.localEulerAngles = Vector3.zero;
            rect.localScale = Vector3.one;
            rect.anchorMin = rect.anchorMax = new Vector2(0.78f, 0.78f);
            rect.sizeDelta = new Vector2(48f, 48f);

            if (config != null && config.roleType == RoleType.Swordsman && reward == RewardType.HolyCup)
            {
                // Match the bell knight's cup-on-head silhouette used in battle.
                rect.anchorMin = rect.anchorMax = new Vector2(0.54f, 0.67f);
                rect.sizeDelta = new Vector2(58f, 58f);
                rect.localEulerAngles = new Vector3(0f, 0f, 180f);
                rect.localScale = new Vector3(0.5719084f, 0.29298f, 1f);
            }
            else if (config != null && config.roleType == RoleType.Swordsman && reward == RewardType.GiantKey)
            {
                // Keep the winding key visually behind the swordsman body in the composite icon.
                rect.anchorMin = rect.anchorMax = new Vector2(0.43f, 0.55f);
                rect.sizeDelta = new Vector2(82f, 82f);
                overlay.transform.SetSiblingIndex(_partyPortraits[partyIndex].transform.GetSiblingIndex());
            }
            else
            {
                overlay.transform.SetSiblingIndex(_partyPortraits[partyIndex].transform.GetSiblingIndex() + 1);
            }
        }

        private static string GetPartyCardIdentity(CharacterConfig config)
        {
            if (config == null)
            {
                return string.Empty;
            }

            return string.IsNullOrWhiteSpace(config.baseCharacterId) ? config.id : config.baseCharacterId;
        }

        private void ApplyStoredPartyCardOrder()
        {
            if (_director == null || _director.gameContext == null || _partyCards == null)
            {
                return;
            }

            var order = _director.gameContext.buildSelection.partyCardOrder;
            var slotX = new[] { 0.22f, 0.5f, 0.78f };
            var assigned = new bool[_partyCards.Length];
            var occupiedSlots = new bool[_partyCards.Length];
            if (order != null)
            {
                for (var slot = 0; slot < slotX.Length && slot < order.Count; slot++)
                {
                    for (var cardIndex = 0; cardIndex < _partyUnits.Length; cardIndex++)
                    {
                        if (assigned[cardIndex] || _partyUnits[cardIndex] == null ||
                            GetPartyCardIdentity(_partyUnits[cardIndex].characterConfig) != order[slot])
                        {
                            continue;
                        }

                        SetCardAnchorX(cardIndex, slotX[slot]);
                        assigned[cardIndex] = true;
                        occupiedSlots[slot] = true;
                        break;
                    }
                }
            }

            var nextSlot = 0;
            for (var cardIndex = 0; cardIndex < _partyCards.Length; cardIndex++)
            {
                if (assigned[cardIndex])
                {
                    continue;
                }
                while (nextSlot < occupiedSlots.Length && occupiedSlots[nextSlot])
                {
                    nextSlot++;
                }
                if (nextSlot < slotX.Length)
                {
                    SetCardAnchorX(cardIndex, slotX[nextSlot]);
                    occupiedSlots[nextSlot] = true;
                }
            }

            SavePartyCardOrder();
        }

        private void SetCardAnchorX(int cardIndex, float anchorX)
        {
            var rect = _partyCards[cardIndex].GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(anchorX, rect.anchorMin.y);
        }

        private void ApplyStoredRelicSlotOrder()
        {
            if (_director == null || _director.gameContext == null || _relicSlotRects == null)
            {
                return;
            }

            var order = _director.gameContext.buildSelection.relicSlotOrder;
            const float startX = 0.12f;
            const float step = 0.15f;
            var assigned = new bool[_relicSlotRects.Length];
            var occupiedSlots = new bool[_relicSlotRects.Length];
            if (order != null)
            {
                for (var slot = 0; slot < order.Count && slot < occupiedSlots.Length; slot++)
                {
                    if (order[slot] == RewardType.None)
                    {
                        continue;
                    }
                    for (var relicIndex = 0; relicIndex < _relicRewards.Length; relicIndex++)
                    {
                        if (!assigned[relicIndex] && _relicRewards[relicIndex] == order[slot])
                        {
                            SetRelicAnchorX(relicIndex, startX + slot * step);
                            assigned[relicIndex] = true;
                            occupiedSlots[slot] = true;
                            break;
                        }
                    }
                }
            }

            var nextSlot = 0;
            for (var relicIndex = 0; relicIndex < _relicSlotRects.Length; relicIndex++)
            {
                if (assigned[relicIndex])
                {
                    continue;
                }
                while (nextSlot < occupiedSlots.Length && occupiedSlots[nextSlot])
                {
                    nextSlot++;
                }
                if (nextSlot < occupiedSlots.Length)
                {
                    SetRelicAnchorX(relicIndex, startX + nextSlot * step);
                    occupiedSlots[nextSlot] = true;
                }
            }

            SaveRelicSlotOrder();
        }

        private void SetRelicAnchorX(int relicIndex, float anchorX)
        {
            var rect = _relicSlotRects[relicIndex];
            rect.anchorMin = rect.anchorMax = new Vector2(anchorX, rect.anchorMin.y);
        }

        private bool TryReorderPartyCard(int sourceIndex, Vector2 screenPosition)
        {
            var targetIndex = FindPartyCardAt(screenPosition);
            if (!IsValidPartyIndex(sourceIndex) || targetIndex < 0 || targetIndex == sourceIndex)
            {
                return false;
            }

            SwapAnchors(_partyCards[sourceIndex].GetComponent<RectTransform>(), _partyCards[targetIndex].GetComponent<RectTransform>());
            _selectedPartyIndex = -1;
            RefreshSelectionVisuals();
            SavePartyCardOrder();
            BattleSfx.PlayUiClick(true);
            return true;
        }

        private bool TryReorderRelicSlot(int sourceIndex, Vector2 screenPosition)
        {
            var targetIndex = FindRelicSlotAt(screenPosition);
            if (sourceIndex < 0 || targetIndex < 0 || targetIndex == sourceIndex)
            {
                return false;
            }

            SwapAnchors(_relicSlotRects[sourceIndex], _relicSlotRects[targetIndex]);
            SaveRelicSlotOrder();
            BattleSfx.PlayUiClick(true);
            return true;
        }

        private static void SwapAnchors(RectTransform first, RectTransform second)
        {
            var firstMin = first.anchorMin;
            var firstMax = first.anchorMax;
            first.anchorMin = second.anchorMin;
            first.anchorMax = second.anchorMax;
            second.anchorMin = firstMin;
            second.anchorMax = firstMax;
        }

        private void SavePartyCardOrder()
        {
            if (_director == null || _director.gameContext == null)
            {
                return;
            }

            var indices = new[] { 0, 1, 2 };
            System.Array.Sort(indices, (left, right) =>
                _partyCards[left].GetComponent<RectTransform>().anchorMin.x.CompareTo(
                    _partyCards[right].GetComponent<RectTransform>().anchorMin.x));
            var order = _director.gameContext.buildSelection.partyCardOrder;
            order.Clear();
            for (var i = 0; i < indices.Length; i++)
            {
                order.Add(_partyUnits[indices[i]] != null
                    ? GetPartyCardIdentity(_partyUnits[indices[i]].characterConfig)
                    : string.Empty);
            }
        }

        private void SaveRelicSlotOrder()
        {
            if (_director == null || _director.gameContext == null)
            {
                return;
            }

            var indices = new[] { 0, 1, 2, 3, 4, 5 };
            System.Array.Sort(indices, (left, right) =>
                _relicSlotRects[left].anchorMin.x.CompareTo(_relicSlotRects[right].anchorMin.x));
            var order = _director.gameContext.buildSelection.relicSlotOrder;
            order.Clear();
            for (var i = 0; i < indices.Length; i++)
            {
                order.Add(_relicRewards[indices[i]]);
            }
        }

        private int FindRelicSlotAt(Vector2 screenPosition)
        {
            for (var i = 0; i < _relicSlotRects.Length; i++)
            {
                if (RectTransformUtility.RectangleContainsScreenPoint(_relicSlotRects[i], screenPosition))
                {
                    return i;
                }
            }
            return -1;
        }

        private int FindPartyCardAt(Vector2 screenPosition)
        {
            for (var i = 0; i < _partyCards.Length; i++)
            {
                var rect = _partyCards[i].GetComponent<RectTransform>();
                if (RectTransformUtility.RectangleContainsScreenPoint(rect, screenPosition))
                {
                    return i;
                }
            }
            return -1;
        }

        private int FindBattleUnitAt(Vector2 screenPosition)
        {
            var battleCamera = Camera.main;
            if (battleCamera == null)
            {
                return -1;
            }

            var bestIndex = -1;
            var bestDistance = 72f;
            for (var i = 0; i < _partyUnits.Length; i++)
            {
                var unit = _partyUnits[i];
                if (unit == null || !unit.IsAlive)
                {
                    continue;
                }

                var unitScreenPosition = (Vector2)battleCamera.WorldToScreenPoint(unit.transform.position + new Vector3(0f, 0.45f, 0f));
                var distance = Vector2.Distance(screenPosition, unitScreenPosition);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestIndex = i;
                }
            }
            return bestIndex;
        }

        private bool IsValidPartyIndex(int partyIndex)
        {
            return _partyUnits != null && partyIndex >= 0 && partyIndex < _partyUnits.Length && _partyUnits[partyIndex] != null;
        }

        private bool IsUsableRelic(int relicIndex)
        {
            return _relicRewards != null && relicIndex >= 0 && relicIndex < _relicRewards.Length &&
                   _relicRewards[relicIndex] != RewardType.None &&
                   (_relicCoolingDown == null || !_relicCoolingDown[relicIndex]);
        }

        private void RefreshSelectionVisuals()
        {
            for (var i = 0; i < _partyCardOutlines.Length; i++)
            {
                _partyCardOutlines[i].effectColor = i == _selectedPartyIndex ? SelectedCardOutline : NormalCardOutline;
                _partyStatBars[i].SetActive(i == _selectedPartyIndex && _pendingDiscoverConfigs[i] == null);
            }

            for (var i = 0; i < _relicOutlines.Length; i++)
            {
                _relicOutlines[i].effectColor = _selectedPartyIndex >= 0 && IsUsableRelic(i)
                    ? ActiveRelicOutline
                    : NormalRelicOutline;
            }
        }

        private void CreatePartyStatBar(Transform card, int partyIndex)
        {
            var statBar = CreatePanel(
                card,
                "SelectedStats",
                new Vector2(0.5f, 1f),
                new Vector2(196f, 42f),
                new Color(0.035f, 0.04f, 0.06f, 0.97f));
            statBar.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 30f);
            CreateOutline(statBar, SelectedCardOutline, new Vector2(2f, -2f));

            var heart = CreateImage(
                statBar.transform,
                "HeartIcon",
                new Vector2(0.09f, 0.5f),
                new Vector2(28f, 28f),
                SimpleSpriteFactory.GetHeartIconSprite(),
                Color.white);
            heart.preserveAspect = true;
            _partyHpValues[partyIndex] = CreateLabel(
                statBar.transform,
                "HealthValue",
                "0/0",
                new Vector2(0.31f, 0.5f),
                new Vector2(78f, 32f),
                19,
                Color.white);

            var swords = CreateImage(
                statBar.transform,
                "AttackIcon",
                new Vector2(0.62f, 0.5f),
                new Vector2(30f, 30f),
                SimpleSpriteFactory.GetCrossedSwordsIconSprite(),
                Color.white);
            swords.preserveAspect = true;
            _partyAttackValues[partyIndex] = CreateLabel(
                statBar.transform,
                "AttackValue",
                "0",
                new Vector2(0.82f, 0.5f),
                new Vector2(52f, 32f),
                19,
                Color.white);

            statBar.SetActive(false);
            _partyStatBars[partyIndex] = statBar;
        }

        private void CreateNewUnitTipBar(Transform card, int partyIndex)
        {
            var tipBar = CreatePanel(
                card,
                "NewUnitDiscoveryTip",
                new Vector2(0.5f, 1f),
                new Vector2(216f, 48f),
                new Color(0.12f, 0.32f, 0.48f, 0.98f));
            tipBar.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 33f);
            var image = tipBar.GetComponent<Image>();
            image.raycastTarget = true;
            CreateOutline(tipBar, new Color(0.45f, 0.82f, 1f, 1f), new Vector2(3f, -3f));
            var button = tipBar.AddComponent<Button>();
            button.targetGraphic = image;
            var capturedIndex = partyIndex;
            button.onClick.AddListener(() => OpenPendingUnitDiscovery(capturedIndex));
            CreateLabel(
                tipBar.transform,
                "TipText",
                "\u53d1\u73b0\u65b0\u5175\u79cd\uff01\u70b9\u51fb\u4e86\u89e3\u3002",
                new Vector2(0.5f, 0.5f),
                new Vector2(204f, 40f),
                20,
                Color.white);
            tipBar.SetActive(false);
            _newUnitTipBars[partyIndex] = tipBar;
        }

        private void RefreshSelectedPartyStats()
        {
            if (_partyStatBars == null)
            {
                return;
            }

            if (_selectedPartyIndex >= 0 &&
                (!IsValidPartyIndex(_selectedPartyIndex) || !_partyUnits[_selectedPartyIndex].IsAlive))
            {
                _selectedPartyIndex = -1;
                RefreshSelectionVisuals();
            }

            for (var i = 0; i < _partyStatBars.Length; i++)
            {
                if (_partyStatBars[i] != null)
                {
                    _partyStatBars[i].SetActive(i == _selectedPartyIndex && _pendingDiscoverConfigs[i] == null);
                }
            }

            if (_selectedPartyIndex < 0)
            {
                return;
            }

            var unit = _partyUnits[_selectedPartyIndex];
            _partyHpValues[_selectedPartyIndex].text = unit.currentHp + "/" + unit.maxHp;
            _partyAttackValues[_selectedPartyIndex].text = _director != null
                ? _director.GetDisplayedPlayerDamage(unit).ToString()
                : "0";
        }

        private static GameObject CreatePanel(Transform parent, string name, Vector2 anchor, Vector2 size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            var image = go.GetComponent<Image>();
            image.sprite = SimpleSpriteFactory.GetWhitePixelSprite();
            image.color = color;
            image.raycastTarget = false;
            return go;
        }

        private static Image CreateImage(Transform parent, string name, Vector2 anchor, Vector2 size, Sprite sprite, Color color)
        {
            var go = CreatePanel(parent, name, anchor, size, color);
            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            return image;
        }

        private static Text CreateLabel(Transform parent, string name, string text, Vector2 anchor, Vector2 size, int fontSize, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            var label = go.GetComponent<Text>();
            label.font = BuildUiRuntimeStyle.GetChineseFont();
            label.fontSize = fontSize;
            label.alignment = TextAnchor.MiddleCenter;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.text = text;
            label.color = color;
            label.raycastTarget = false;
            return label;
        }

        private static Outline CreateOutline(GameObject target, Color color, Vector2 distance)
        {
            var outline = target.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = distance;
            return outline;
        }

        private static void EnsureEventSystem()
        {
            if (FindObjectOfType<EventSystem>() != null)
            {
                return;
            }

            var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            DontDestroyOnLoad(eventSystem);
        }

        private sealed class PartyCardInteraction : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
        {
            private BattleHudLayoutController _owner;
            private int _partyIndex;
            private GameObject _dragCanvasObject;
            private RectTransform _dragVisual;

            public void Initialize(BattleHudLayoutController owner, int partyIndex)
            {
                _owner = owner;
                _partyIndex = partyIndex;
                GetComponent<Image>().raycastTarget = true;
            }

            public void OnPointerClick(PointerEventData eventData)
            {
                BattleSfx.PlayUiClick();
                _owner?.TogglePartySelection(_partyIndex);
            }

            public void OnBeginDrag(PointerEventData eventData)
            {
                if (_owner == null || _owner._partyPortraits[_partyIndex].sprite == null)
                {
                    return;
                }

                _dragCanvasObject = new GameObject("PartyCardDragCanvas", typeof(Canvas), typeof(CanvasScaler));
                var canvas = _dragCanvasObject.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.overrideSorting = true;
                canvas.sortingOrder = 500;
                var scaler = _dragCanvasObject.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080f, 1920f);
                scaler.matchWidthOrHeight = 1f;

                var visual = new GameObject("PartyCardDragVisual", typeof(RectTransform), typeof(Image), typeof(Outline), typeof(CanvasGroup));
                visual.transform.SetParent(_dragCanvasObject.transform, false);
                _dragVisual = visual.GetComponent<RectTransform>();
                _dragVisual.sizeDelta = new Vector2(176f, 176f);
                var image = visual.GetComponent<Image>();
                image.sprite = _owner._partyPortraits[_partyIndex].sprite;
                image.preserveAspect = true;
                image.raycastTarget = false;
                var outline = visual.GetComponent<Outline>();
                outline.effectColor = SelectedCardOutline;
                outline.effectDistance = new Vector2(5f, -5f);
                visual.GetComponent<CanvasGroup>().blocksRaycasts = false;
                OnDrag(eventData);
            }

            public void OnDrag(PointerEventData eventData)
            {
                if (_dragVisual != null)
                {
                    _dragVisual.position = eventData.position;
                }
            }

            public void OnEndDrag(PointerEventData eventData)
            {
                if (_dragVisual == null)
                {
                    return;
                }

                _owner?.TryReorderPartyCard(_partyIndex, eventData.position);
                Destroy(_dragCanvasObject);
                _dragCanvasObject = null;
                _dragVisual = null;
            }

            private void OnDisable()
            {
                if (_dragCanvasObject != null)
                {
                    Destroy(_dragCanvasObject);
                    _dragCanvasObject = null;
                    _dragVisual = null;
                }
            }
        }

        private sealed class RelicSlotInteraction : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
        {
            private BattleHudLayoutController _owner;
            private int _relicIndex;
            private RectTransform _dragVisual;
            private GameObject _dragCanvasObject;

            public void Initialize(BattleHudLayoutController owner, int relicIndex)
            {
                _owner = owner;
                _relicIndex = relicIndex;
                GetComponent<Image>().raycastTarget = true;
            }

            public void OnPointerClick(PointerEventData eventData)
            {
                BattleSfx.PlayUiClick();
                _owner?.ClickRelic(_relicIndex);
            }

            public void OnBeginDrag(PointerEventData eventData)
            {
                if (_owner == null || !_owner.IsUsableRelic(_relicIndex))
                {
                    return;
                }

                var source = _owner._relicIcons[_relicIndex];
                _owner.SetRelicDragState(_owner._relicRewards[_relicIndex], true);
                _dragCanvasObject = new GameObject(
                    "RelicDragCanvas",
                    typeof(Canvas),
                    typeof(CanvasScaler),
                    typeof(GraphicRaycaster));
                var dragCanvas = _dragCanvasObject.GetComponent<Canvas>();
                dragCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                dragCanvas.overrideSorting = true;
                dragCanvas.sortingOrder = 500;
                var scaler = _dragCanvasObject.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080f, 1920f);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 1f;

                var go = new GameObject("RelicDragVisual", typeof(RectTransform), typeof(Image), typeof(Outline), typeof(CanvasGroup));
                go.transform.SetParent(_dragCanvasObject.transform, false);
                _dragVisual = go.GetComponent<RectTransform>();
                _dragVisual.sizeDelta = new Vector2(112f, 112f);
                var background = go.GetComponent<Image>();
                background.sprite = SimpleSpriteFactory.GetWhitePixelSprite();
                background.color = new Color(0.035f, 0.04f, 0.065f, 1f);
                background.raycastTarget = false;
                var outline = go.GetComponent<Outline>();
                outline.effectColor = new Color(1f, 0.68f, 0.18f, 1f);
                outline.effectDistance = new Vector2(4f, -4f);

                var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                iconGo.transform.SetParent(go.transform, false);
                var iconRect = iconGo.GetComponent<RectTransform>();
                iconRect.anchorMin = iconRect.anchorMax = new Vector2(0.5f, 0.5f);
                iconRect.sizeDelta = new Vector2(82f, 82f);
                var image = iconGo.GetComponent<Image>();
                image.sprite = source.sprite;
                image.preserveAspect = true;
                image.raycastTarget = false;
                go.GetComponent<CanvasGroup>().blocksRaycasts = false;
                OnDrag(eventData);
            }

            public void OnDrag(PointerEventData eventData)
            {
                if (_dragVisual != null)
                {
                    _dragVisual.position = eventData.position;
                    _owner?.UpdateRelicDragGuideHover(_owner._relicRewards[_relicIndex], eventData.position);
                }
            }

            public void OnEndDrag(PointerEventData eventData)
            {
                if (_dragVisual == null)
                {
                    return;
                }

                var draggedReward = _owner != null ? _owner._relicRewards[_relicIndex] : RewardType.None;
                var reordered = _owner != null && _owner.TryReorderRelicSlot(_relicIndex, eventData.position);
                var assigned = !reordered && _owner != null && _owner.DropRelic(_relicIndex, eventData.position);
                _owner?.SetRelicDragState(draggedReward, false);
                if (assigned)
                {
                    _owner.CompleteRelicDragTip();
                }
                Destroy(_dragCanvasObject);
                _dragVisual = null;
                _dragCanvasObject = null;
            }

            private void OnDisable()
            {
                if (_dragCanvasObject != null)
                {
                    if (_owner != null && _owner.IsUsableRelic(_relicIndex))
                    {
                        _owner.SetRelicDragState(_owner._relicRewards[_relicIndex], false);
                    }
                    Destroy(_dragCanvasObject);
                    _dragCanvasObject = null;
                    _dragVisual = null;
                }
            }
        }
    }
}
