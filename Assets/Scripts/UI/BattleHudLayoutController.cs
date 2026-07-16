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
        private Image[] _relicIcons;
        private GameObject[] _partyCards;
        private Outline[] _partyCardOutlines;
        private GameObject[] _partyStatBars;
        private Text[] _partyHpValues;
        private Text[] _partyAttackValues;
        private RectTransform[] _selectionEffects;
        private Coroutine[] _selectionEffectRoutines;
        private Image[] _cooldownMasks;
        private Outline[] _relicOutlines;
        private RewardType[] _relicRewards;
        private CombatUnitController[] _partyUnits;
        private Coroutine[] _partyCooldowns;
        private CharacterConfig[] _partyDefaultConfigs;
        private RewardType[] _partyEquippedRewards;
        private Image[] _trailNodes;
        private RectTransform _trailProgress;
        private Text _stageTitleLabel;
        private Text _waveCountLabel;
        private AudioSource _musicSource;
        // Decorative props are separate from the future static battle-background layer.
        private GameObject _environmentProps;
        private int _activeWave;
        private int _totalWaves;
        private int _waveEnemyTotal;
        private int _selectedPartyIndex = -1;
        private const float RelicCooldownDuration = 6f;
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
        }

        public static void NotifyWaveStarted(int waveIndex, int totalWaves, int enemyTotal)
        {
            Instance?.SetWaveStarted(waveIndex, totalWaves, enemyTotal);
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
            CreateRelicBar();
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
            _partyCards = new GameObject[3];
            _partyCardOutlines = new Outline[3];
            _partyStatBars = new GameObject[3];
            _partyHpValues = new Text[3];
            _partyAttackValues = new Text[3];
            _cooldownMasks = new Image[3];
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

                CreatePartyStatBar(card.transform, i);

                var interaction = card.AddComponent<PartyCardInteraction>();
                interaction.Initialize(this, i);
            }
        }

        private void CreateRelicBar()
        {
            _relicIcons = new Image[6];
            _relicOutlines = new Outline[6];
            _relicRewards = new RewardType[6];
            _selectionEffects = new RectTransform[6];
            _selectionEffectRoutines = new Coroutine[6];
            var startX = 0.12f;
            const float step = 0.15f;
            for (var i = 0; i < _relicIcons.Length; i++)
            {
                var slot = CreatePanel(_root, "RelicSlot_" + i, new Vector2(startX + i * step, 0.075f), new Vector2(92f, 92f), new Color(0.06f, 0.07f, 0.1f, 1f));
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

                var interaction = slot.AddComponent<RelicSlotInteraction>();
                interaction.Initialize(this, i);
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
            SetEncounterBackdrop(3, true);
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

        private void SetEncounterBackdrop(int index, bool isBoss)
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
            CreateEnvironmentProps(index, isBoss);
        }

        private void CreateEnvironmentProps(int index, bool isBoss)
        {
            if (_environmentProps != null)
            {
                Destroy(_environmentProps);
            }

            _environmentProps = new GameObject("BattleEnvironmentProps");
            _environmentProps.AddComponent<BattleEnvironmentPropParallax>();
            var themeName = isBoss
                ? "bg_lava_texture"
                : index == 0 ? "bg_village_mill" : index == 1 ? "bg_winter_mill" : "bg_graveyard_tree";
            var sprite = ExtractedArtLibrary.LoadEnvironment(themeName);
            if (sprite == null)
            {
                return;
            }

            if (isBoss)
            {
                var bossPropPositions = new[] { -5.2f, -1.75f, 1.75f, 5.2f };
                for (var i = 0; i < bossPropPositions.Length; i++)
                {
                    CreateEnvironmentProp(
                        themeName + "_" + i,
                        sprite,
                        new Vector3(bossPropPositions[i], -1.05f, 4f),
                        2.15f,
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
                _partyPortraits[i].sprite = PixelAnimationLibrary.GetFirstFrameSprite(config.resourceId, "walk", "attack", "idle");
            }
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

        public void DropRelic(int relicIndex, Vector2 screenPosition)
        {
            if (!IsUsableRelic(relicIndex))
            {
                return;
            }

            var partyIndex = FindPartyCardAt(screenPosition);
            if (partyIndex < 0)
            {
                partyIndex = FindBattleUnitAt(screenPosition);
            }

            if (partyIndex >= 0)
            {
                TryAssignRelic(partyIndex, _relicRewards[relicIndex]);
            }
        }

        private void TryAssignRelic(int partyIndex, RewardType rewardType)
        {
            if (!IsValidPartyIndex(partyIndex) || rewardType == RewardType.None)
            {
                return;
            }

            var unit = _partyUnits[partyIndex];
            if (unit == null || !unit.IsAlive || unit.characterConfig == null)
            {
                return;
            }

            ResetCurrentRelicHolder(rewardType, partyIndex);
            ResetPartyToDefault(partyIndex);
            _selectedPartyIndex = -1;
            RefreshSelectionVisuals();
            _partyCooldowns[partyIndex] = StartCoroutine(TemporaryRelicRoutine(partyIndex, unit, rewardType));
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

            _partyEquippedRewards[partyIndex] = rewardType;
            unit.ApplyTemporaryCharacterConfig(temporaryConfig);
            RefreshPartyPortrait(partyIndex, temporaryConfig);
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

            if (unit != null && unit.IsAlive)
            {
                unit.ApplyTemporaryCharacterConfig(defaultConfig);
                RefreshPartyPortrait(partyIndex, defaultConfig);
            }

            _partyEquippedRewards[partyIndex] = RewardType.None;
            cooldownMask.fillAmount = 0f;
            cooldownMask.gameObject.SetActive(false);
            _partyCooldowns[partyIndex] = null;
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
                _partyPortraits[partyIndex].sprite = PixelAnimationLibrary.GetFirstFrameSprite(config.resourceId, "walk", "attack", "idle");
            }
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
                   _relicRewards[relicIndex] != RewardType.None;
        }

        private void RefreshSelectionVisuals()
        {
            for (var i = 0; i < _partyCardOutlines.Length; i++)
            {
                _partyCardOutlines[i].effectColor = i == _selectedPartyIndex ? SelectedCardOutline : NormalCardOutline;
                _partyStatBars[i].SetActive(i == _selectedPartyIndex);
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
                    _partyStatBars[i].SetActive(i == _selectedPartyIndex);
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

        private sealed class PartyCardInteraction : MonoBehaviour, IPointerClickHandler
        {
            private BattleHudLayoutController _owner;
            private int _partyIndex;

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
                }
            }

            public void OnEndDrag(PointerEventData eventData)
            {
                if (_dragVisual == null)
                {
                    return;
                }

                _owner?.DropRelic(_relicIndex, eventData.position);
                Destroy(_dragCanvasObject);
                _dragVisual = null;
                _dragCanvasObject = null;
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
    }
}
