using System;
using System.Collections;
using ProjectHunt.Build;
using ProjectHunt.Data;
using ProjectHunt.Flow;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectHunt.Battle
{
    public sealed class DropClaimController : MonoBehaviour
    {
        [Header("Flow")]
        public DemoFlowController flowController;
        public RewardType rewardType = RewardType.MeteorHammer;

        [Header("Optional Visuals")]
        public GameObject dropVisualRoot;

        private bool _isClaimed;
        private bool _isInteractable = true;
        private DropClaimPresentationController _presentationController;

        private void Awake()
        {
            ResolveFlowController();
            _presentationController = DropClaimPresentationController.Create(this);

            Debug.Log($"[Drop] Awake on {name}. Flow controller found: {flowController != null}");
        }

        public void ClaimDrop()
        {
            Debug.Log($"[Drop] ClaimDrop called on {name}. isClaimed={_isClaimed}, isInteractable={_isInteractable}");

            if (_isClaimed || !_isInteractable)
            {
                Debug.LogWarning($"[Drop] Claim blocked on {name}. isClaimed={_isClaimed}, isInteractable={_isInteractable}");
                return;
            }

            BeginClaimSequence();
        }

        public void PlayAutoClaimSequence()
        {
            if (_isClaimed)
            {
                return;
            }

            BeginClaimSequence();
        }

        private void BeginClaimSequence()
        {
            _isClaimed = true;
            BattleSfx.PlayWeaponDiscovery();
            var controller = _presentationController != null ? _presentationController : DropClaimPresentationController.Create(this);
            _presentationController = controller;
            controller.PlayClaimSequence(FinalizeClaim);
        }

        private void OnMouseUpAsButton()
        {
            Debug.Log($"[Drop] OnMouseUpAsButton fired on {name}.");
            ClaimDrop();
        }

        public void SetInteractable(bool isInteractable)
        {
            _isInteractable = isInteractable;
            Debug.Log($"[Drop] SetInteractable({isInteractable}) on {name}.");

            if (isInteractable)
            {
                if (_presentationController == null)
                {
                    _presentationController = DropClaimPresentationController.Create(this);
                }

                _presentationController.ShowPrompt();
            }
            else if (_presentationController != null)
            {
                _presentationController.HidePrompt();
            }
        }

        public void RebuildPresentation()
        {
            if (_presentationController != null)
            {
                Destroy(_presentationController.gameObject);
            }

            _presentationController = DropClaimPresentationController.Create(this);
            if (_isInteractable)
            {
                _presentationController.ShowPrompt();
            }
        }

        private void FinalizeClaim()
        {
            if (dropVisualRoot != null)
            {
                dropVisualRoot.SetActive(false);
            }
            else
            {
                gameObject.SetActive(false);
            }

            if (flowController == null)
            {
                ResolveFlowController();
            }

            if (flowController != null)
            {
                Debug.Log("[Drop] Claim presentation finished. Loading build scene.");
                if (rewardType == RewardType.HolyCup)
                {
                    flowController.ClaimHolyCup();
                }
                else if (rewardType == RewardType.GiantKey)
                {
                    flowController.ClaimGiantKey();
                }
                else
                {
                    flowController.ClaimMeteorHammer();
                }
                return;
            }

            Debug.LogError("DropClaimController could not find DemoFlowController.");
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

            var context = DemoGameContext.Instance;
            if (context == null)
            {
                return;
            }

            flowController = context.GetComponent<DemoFlowController>();
            if (flowController == null)
            {
                flowController = context.gameObject.AddComponent<DemoFlowController>();
                flowController.gameContext = context;
                Debug.Log("[Drop] Added missing DemoFlowController onto DemoGameContext.");
            }
        }
    }

    public sealed class DropClaimPresentationController : MonoBehaviour
    {
        private const float ArrowBounceAmplitude = 0.22f;
        private const float ArrowBounceSpeed = 3.4f;

        private Transform _dropTransform;
        private Camera _worldCamera;
        private Canvas _overlayCanvas;
        private RectTransform _overlayRoot;
        private Image _claimPanel;
        private Image _claimGlow;
        private Image _centerHammerImage;
        private Text _claimText;
        private Image _fullscreenDim;
        private SpriteRenderer _arrowRenderer;
        private readonly Vector3 _arrowBaseOffset = new(0f, 1.1f, 0f);
        private Coroutine _arrowRoutine;
        private RewardType _rewardType = RewardType.MeteorHammer;

        public static DropClaimPresentationController Create(DropClaimController dropClaimController)
        {
            var host = new GameObject("DropClaimPresentationController");
            var controller = host.AddComponent<DropClaimPresentationController>();
            controller.Initialize(dropClaimController);
            return controller;
        }

        public void Initialize(DropClaimController dropClaimController)
        {
            _dropTransform = dropClaimController != null ? dropClaimController.transform : null;
            _rewardType = dropClaimController != null ? dropClaimController.rewardType : RewardType.MeteorHammer;
            _worldCamera = Camera.main != null ? Camera.main : FindObjectOfType<Camera>();
            CreateArrow();
            CreateOverlay();
        }

        public void ShowPrompt()
        {
            if (_arrowRenderer != null)
            {
                _arrowRenderer.enabled = true;
            }

            if (_arrowRoutine != null)
            {
                StopCoroutine(_arrowRoutine);
            }

            _arrowRoutine = StartCoroutine(ArrowBounceRoutine());
        }

        public void HidePrompt()
        {
            if (_arrowRoutine != null)
            {
                StopCoroutine(_arrowRoutine);
                _arrowRoutine = null;
            }

            if (_arrowRenderer != null)
            {
                _arrowRenderer.enabled = false;
            }
        }

        public void PlayClaimSequence(Action onComplete)
        {
            HidePrompt();
            StartCoroutine(ClaimSequenceRoutine(onComplete));
        }

        private IEnumerator ClaimSequenceRoutine(Action onComplete)
        {
            if (_dropTransform == null)
            {
                onComplete?.Invoke();
                Destroy(gameObject);
                yield break;
            }

            EnsureOverlayVisible(true);
            _centerHammerImage.gameObject.SetActive(true);
            _claimPanel.gameObject.SetActive(true);
            _claimGlow.gameObject.SetActive(true);
            _claimText.gameObject.SetActive(true);

            _claimPanel.color = new Color(0.05f, 0.05f, 0.08f, 0f);
            _claimGlow.color = new Color(1f, 0.8f, 0.32f, 0f);
            _claimText.color = new Color(1f, 0.93f, 0.8f, 0f);
            _centerHammerImage.color = Color.white;
            _fullscreenDim.color = new Color(0f, 0f, 0f, 0f);

            var hammerRect = _centerHammerImage.rectTransform;
            var panelRect = _claimPanel.rectTransform;
            var startScreen = RectTransformUtility.WorldToScreenPoint(_worldCamera, _dropTransform.position);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _overlayRoot,
                startScreen,
                _overlayCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _overlayCanvas.worldCamera,
                out var startLocalPoint);
            hammerRect.anchoredPosition = startLocalPoint;
            hammerRect.localScale = Vector3.one * 0.9f;

            // Keep the ritual centered inside the portrait gameplay viewport rather than
            // the full 9:16 screen, leaving the route and party HUD unobstructed.
            var endLocalPoint = new Vector2(0f, 265f);
            panelRect.localScale = Vector3.one * 0.92f;

            const float introDuration = 0.6f;
            var elapsed = 0f;
            while (elapsed < introDuration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / introDuration);
                var ease = Mathf.SmoothStep(0f, 1f, t);
                _fullscreenDim.color = new Color(0f, 0f, 0f, 0.26f * ease);
                _claimPanel.color = new Color(0.05f, 0.05f, 0.08f, 0.9f * ease);
                _claimGlow.color = new Color(1f, 0.8f, 0.32f, 0.42f * ease);
                panelRect.localScale = Vector3.Lerp(Vector3.one * 0.92f, Vector3.one, ease);
                hammerRect.anchoredPosition = Vector2.Lerp(startLocalPoint, endLocalPoint, ease);
                hammerRect.localScale = Vector3.Lerp(Vector3.one * 0.9f, Vector3.one * 1.18f, ease);
                hammerRect.localEulerAngles = new Vector3(0f, 0f, Mathf.Lerp(0f, 540f, t));
                yield return null;
            }

            hammerRect.anchoredPosition = endLocalPoint;
            hammerRect.localScale = Vector3.one * 1.12f;
            hammerRect.localEulerAngles = Vector3.zero;

            const float focusDuration = 0.95f;
            elapsed = 0f;
            while (elapsed < focusDuration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / focusDuration);
                _claimText.color = new Color(1f, 0.93f, 0.8f, Mathf.Clamp01(t * 2f));
                _claimGlow.rectTransform.localScale = Vector3.one * (1f + Mathf.Sin(t * Mathf.PI * 2f) * 0.04f);
                hammerRect.localScale = Vector3.one * (1.12f + Mathf.Sin(t * Mathf.PI * 2f) * 0.04f);
                yield return null;
            }

            const float outroDuration = 0.25f;
            elapsed = 0f;
            while (elapsed < outroDuration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / outroDuration);
                var alpha = 1f - t;
                _fullscreenDim.color = new Color(0f, 0f, 0f, 0.26f * alpha);
                _claimPanel.color = new Color(0.05f, 0.05f, 0.08f, 0.9f * alpha);
                _claimGlow.color = new Color(1f, 0.8f, 0.32f, 0.42f * alpha);
                _claimText.color = new Color(1f, 0.93f, 0.8f, alpha);
                _centerHammerImage.color = new Color(1f, 1f, 1f, alpha);
                yield return null;
            }

            onComplete?.Invoke();
            Destroy(gameObject);
        }

        private void CreateArrow()
        {
            if (_dropTransform == null)
            {
                return;
            }

            var arrowGo = new GameObject("DropHintArrow");
            arrowGo.transform.SetParent(_dropTransform, false);
            arrowGo.transform.localPosition = _arrowBaseOffset;
            arrowGo.transform.localScale = Vector3.one * 0.9f;
            arrowGo.transform.localEulerAngles = new Vector3(0f, 0f, 180f);

            _arrowRenderer = arrowGo.AddComponent<SpriteRenderer>();
            _arrowRenderer.sprite = CreateArrowSprite();
            _arrowRenderer.sortingOrder = 15;
            _arrowRenderer.color = new Color(1f, 0.8f, 0.28f, 0.95f);
            _arrowRenderer.enabled = false;
        }

        private void CreateOverlay()
        {
            var canvasGo = new GameObject("DropClaimOverlay", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            _overlayCanvas = canvasGo.GetComponent<Canvas>();
            _overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _overlayCanvas.sortingOrder = 200;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;

            _overlayRoot = canvasGo.GetComponent<RectTransform>();
            _overlayRoot.anchorMin = Vector2.zero;
            _overlayRoot.anchorMax = Vector2.one;
            _overlayRoot.offsetMin = Vector2.zero;
            _overlayRoot.offsetMax = Vector2.zero;

            _fullscreenDim = CreateFullScreenImage(_overlayRoot, "Dim", new Color(0f, 0f, 0f, 0f));
            _claimGlow = CreateAnchoredImage(_overlayRoot, "Glow", new Vector2(820f, 600f), new Color(1f, 0.8f, 0.32f, 0f));
            _claimGlow.rectTransform.anchoredPosition = new Vector2(0f, 225f);
            _claimGlow.preserveAspect = false;

            _claimPanel = CreateAnchoredImage(_overlayRoot, "Panel", new Vector2(720f, 500f), new Color(0.05f, 0.05f, 0.08f, 0f));
            _claimPanel.rectTransform.anchoredPosition = new Vector2(0f, 205f);
            _claimPanel.preserveAspect = false;
            var claimBorder = _claimPanel.gameObject.AddComponent<Outline>();
            claimBorder.effectColor = new Color(1f, 0.82f, 0.42f, 0.92f);
            claimBorder.effectDistance = new Vector2(5f, -5f);

            _centerHammerImage = CreateAnchoredImage(_overlayRoot, "RewardIcon", new Vector2(230f, 230f), Color.white);
            _centerHammerImage.sprite = GetRewardSprite();

            _claimText = CreateText(_overlayRoot, "ClaimText", new Vector2(660f, 74f), 38, GetClaimText());
            _claimText.rectTransform.anchoredPosition = new Vector2(0f, 25f);
            _claimText.alignment = TextAnchor.MiddleCenter;

            EnsureOverlayVisible(false);
        }

        private void EnsureOverlayVisible(bool isVisible)
        {
            if (_overlayCanvas != null)
            {
                _overlayCanvas.gameObject.SetActive(isVisible);
            }
        }

        private IEnumerator ArrowBounceRoutine()
        {
            var arrowTransform = _arrowRenderer != null ? _arrowRenderer.transform : null;
            if (arrowTransform == null)
            {
                yield break;
            }

            var elapsed = 0f;
            while (true)
            {
                elapsed += Time.deltaTime;
                var bounce = Mathf.Sin(elapsed * ArrowBounceSpeed) * ArrowBounceAmplitude;
                arrowTransform.localPosition = _arrowBaseOffset + Vector3.up * bounce;
                var c = _arrowRenderer.color;
                c.a = 0.72f + (Mathf.Sin(elapsed * ArrowBounceSpeed) * 0.5f + 0.5f) * 0.28f;
                _arrowRenderer.color = c;
                yield return null;
            }
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
            image.color = color;
            image.sprite = SimpleSpriteFactory.GetWhitePixelSprite();
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
            image.color = color;
            image.sprite = SimpleSpriteFactory.GetWhitePixelSprite();
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
            text.color = Color.white;
            text.text = content;
            return text;
        }

        private static Sprite CreateArrowSprite()
        {
            var tex = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            var clear = new Color(0f, 0f, 0f, 0f);
            var arrow = new Color32(255, 210, 95, 255);

            for (var y = 0; y < 16; y++)
            {
                for (var x = 0; x < 16; x++)
                {
                    tex.SetPixel(x, y, clear);
                }
            }

            for (var y = 2; y < 8; y++)
            {
                tex.SetPixel(7, y, arrow);
                tex.SetPixel(8, y, arrow);
            }

            for (var row = 0; row < 6; row++)
            {
                var minX = 3 + row;
                var maxX = 12 - row;
                for (var x = minX; x <= maxX; x++)
                {
                    tex.SetPixel(x, 8 + row, arrow);
                }
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 16f);
        }

        private string GetClaimText()
        {
            return _rewardType switch
            {
                RewardType.HolyCup => "\u83b7\u5f97\u795e\u5668\uff1a\u9152\u795e\u5723\u676f",
                RewardType.GiantKey => "\u83b7\u5f97\u795e\u5668\uff1a\u5de8\u4eba\u94a5\u5319",
                _ => "\u83b7\u5f97\u795e\u5668\uff1a\u6d41\u661f\u9524",
            };
        }

        private Sprite GetRewardSprite()
        {
            return _rewardType switch
            {
                RewardType.HolyCup => SimpleSpriteFactory.GetHolyCupSprite(),
                RewardType.GiantKey => SimpleSpriteFactory.GetGiantKeySprite(),
                _ => SimpleSpriteFactory.GetMeteorHammerSprite(),
            };
        }
    }
}
