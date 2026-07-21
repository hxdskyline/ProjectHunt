using ProjectHunt.Data;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectHunt.Build
{
    public sealed class BuildCharacterSlotView : MonoBehaviour
    {
        [Header("Data")]
        public CharacterConfig characterConfig;

        [Header("UI")]
        public GameObject selectedHighlight;
        public Image portraitImage;
        public Text nameText;
        public Text effectText;
        public Button assignButton;
        public Text assignButtonText;
        public Image cardBackground;
        public Color normalCardColor = new Color(0.1f, 0.1f, 0.1f, 0.85f);
        public Color selectedCardColor = new Color(0.1f, 0.1f, 0.1f, 0.85f);
        public Color normalButtonColor = new Color(0.95f, 0.55f, 0.16f, 0.98f);
        public Color selectedButtonColor = new Color(0.95f, 0.55f, 0.16f, 0.98f);

        private static readonly Dictionary<int, Sprite> CroppedPortraitCache = new Dictionary<int, Sprite>();
        private BuildSelectionController _selectionController;

        private void Awake()
        {
            AutoBindIfNeeded();
        }

        public void Bind(BuildSelectionController selectionController)
        {
            _selectionController = selectionController;
            AutoBindIfNeeded();
            EnsureAssignButton();
            ApplyLayout();
        }

        public void SetSelected(bool isSelected)
        {
            if (selectedHighlight != null)
            {
                selectedHighlight.SetActive(false);
            }

            if (cardBackground != null)
            {
                cardBackground.color = normalCardColor;
            }

            var buttonGraphic = assignButton != null ? assignButton.targetGraphic as Graphic : null;
            if (buttonGraphic != null)
            {
                buttonGraphic.color = normalButtonColor;
            }
        }

        public void SetPortrait(Sprite sprite, CharacterConfig portraitConfig = null)
        {
            if (portraitImage == null)
            {
                return;
            }

            var croppedSprite = GetCroppedPortraitSprite(sprite);
            portraitImage.sprite = croppedSprite != null ? croppedSprite : sprite;
            portraitImage.enabled = sprite != null;
            portraitImage.preserveAspect = true;
            ApplyPortraitSizing(portraitImage.sprite, portraitConfig != null ? portraitConfig : characterConfig);
        }

        public void SetTexts(string displayName, string effectDescription, string buttonText)
        {
            if (nameText != null)
            {
                nameText.font = BuildUiRuntimeStyle.GetChineseFont();
                nameText.text = displayName;
            }

            if (effectText != null)
            {
                effectText.font = BuildUiRuntimeStyle.GetChineseFont();
                effectText.text = effectDescription;
            }

            if (assignButtonText != null)
            {
                assignButtonText.font = BuildUiRuntimeStyle.GetChineseFont();
                assignButtonText.text = buttonText;
            }
        }

        public void AssignToCharacter()
        {
            if (_selectionController != null)
            {
                _selectionController.ShowDiscoverPanel(this);
            }
        }

        private void AutoBindIfNeeded()
        {
            if (selectedHighlight == null)
            {
                var selected = transform.Find("SelectedHighlight");
                if (selected != null)
                {
                    selectedHighlight = selected.gameObject;
                }
            }

            if (portraitImage == null)
            {
                var portrait = transform.Find("WeaponPreview");
                if (portrait != null)
                {
                    portraitImage = portrait.GetComponent<Image>();
                }
            }

            if (nameText == null)
            {
                var label = transform.Find("Name");
                if (label != null)
                {
                    nameText = label.GetComponent<Text>();
                }
            }

            if (effectText == null)
            {
                var effect = transform.Find("ResourceName");
                if (effect != null)
                {
                    effectText = effect.GetComponent<Text>();
                }
            }

            if (cardBackground == null)
            {
                cardBackground = GetComponent<Image>();
            }
        }

        private void EnsureAssignButton()
        {
            if (assignButton == null)
            {
                var buttonTransform = transform.Find("AssignButton");
                if (buttonTransform != null)
                {
                    assignButton = buttonTransform.GetComponent<Button>();
                    assignButtonText = buttonTransform.GetComponentInChildren<Text>();
                }
            }

            if (assignButton == null)
            {
                CreateAssignButton();
            }

            if (assignButton == null)
            {
                return;
            }

            assignButton.onClick.RemoveAllListeners();
            assignButton.onClick.AddListener(AssignToCharacter);
        }

        private void CreateAssignButton()
        {
            var buttonGo = new GameObject("AssignButton", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonGo.transform.SetParent(transform, false);

            var rect = buttonGo.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, -98f);
            rect.sizeDelta = new Vector2(168f, 42f);

            var image = buttonGo.GetComponent<Image>();
            image.color = normalButtonColor;

            assignButton = buttonGo.GetComponent<Button>();
            assignButton.targetGraphic = image;

            var textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(buttonGo.transform, false);
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            assignButtonText = textGo.GetComponent<Text>();
            assignButtonText.alignment = TextAnchor.MiddleCenter;
            assignButtonText.font = BuildUiRuntimeStyle.GetChineseFont();
            assignButtonText.fontSize = 18;
            assignButtonText.color = Color.white;
        }

        private void ApplyLayout()
        {
            if (portraitImage != null)
            {
                var portraitRect = portraitImage.rectTransform;
                portraitRect.anchoredPosition = new Vector2(-235f, -8f);
            }

            if (nameText != null)
            {
                var nameRect = nameText.rectTransform;
                nameRect.anchoredPosition = new Vector2(105f, 92f);
                nameRect.sizeDelta = new Vector2(430f, 48f);
                nameText.fontSize = 30;
                nameText.alignment = TextAnchor.MiddleCenter;
            }

            if (effectText != null)
            {
                var effectRect = effectText.rectTransform;
                effectRect.anchoredPosition = new Vector2(105f, 28f);
                effectRect.sizeDelta = new Vector2(430f, 76f);
                effectText.fontSize = 24;
                effectText.alignment = TextAnchor.MiddleCenter;
                effectText.horizontalOverflow = HorizontalWrapMode.Wrap;
                effectText.verticalOverflow = VerticalWrapMode.Overflow;
            }

            if (assignButton != null)
            {
                var buttonRect = assignButton.GetComponent<RectTransform>();
                buttonRect.anchoredPosition = new Vector2(105f, -91f);
                buttonRect.sizeDelta = new Vector2(330f, 64f);
            }

            if (assignButtonText != null)
            {
                assignButtonText.fontSize = 24;
            }
        }

        private void ApplyPortraitSizing(Sprite sprite, CharacterConfig sizingConfig)
        {
            if (portraitImage == null || sprite == null || sizingConfig == null)
            {
                return;
            }

            var scaleFactor = GetPortraitScaleFactor(sizingConfig);
            var size = sprite.rect.size * scaleFactor;
            portraitImage.rectTransform.sizeDelta = size;
        }

        private static float GetPortraitScaleFactor(CharacterConfig sizingConfig)
        {
            var visualScale = Mathf.Max(0.01f, sizingConfig.visualScale);
            const float uiPixelsPerWorldUnit = 4.2f;
            return visualScale * uiPixelsPerWorldUnit * 0.5f;
        }

        public static Sprite GetCroppedPortraitSprite(Sprite sprite)
        {
            if (sprite == null)
            {
                return null;
            }

            var key = sprite.GetInstanceID();
            if (CroppedPortraitCache.TryGetValue(key, out var cached))
            {
                return cached;
            }

            var texture = sprite.texture;
            if (texture == null)
            {
                return sprite;
            }

            var sourceRect = sprite.rect;
            var minX = int.MaxValue;
            var minY = int.MaxValue;
            var maxX = int.MinValue;
            var maxY = int.MinValue;

            for (var y = 0; y < sourceRect.height; y++)
            {
                for (var x = 0; x < sourceRect.width; x++)
                {
                    var pixel = texture.GetPixel((int)sourceRect.x + x, (int)sourceRect.y + y);
                    if (pixel.a <= 0f)
                    {
                        continue;
                    }

                    if (x < minX) minX = x;
                    if (y < minY) minY = y;
                    if (x > maxX) maxX = x;
                    if (y > maxY) maxY = y;
                }
            }

            if (minX == int.MaxValue)
            {
                CroppedPortraitCache[key] = sprite;
                return sprite;
            }

            var croppedRect = new Rect(
                sourceRect.x + minX,
                sourceRect.y + minY,
                maxX - minX + 1,
                maxY - minY + 1);

            var croppedSprite = Sprite.Create(
                texture,
                croppedRect,
                new Vector2(0.5f, 0f),
                sprite.pixelsPerUnit,
                0,
                SpriteMeshType.FullRect);

            CroppedPortraitCache[key] = croppedSprite;
            return croppedSprite;
        }
    }
}
