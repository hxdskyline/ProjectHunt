using System.Collections;
using ProjectHunt.Battle;
using ProjectHunt.Build;
using ProjectHunt.Data;
using ProjectHunt.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ProjectHunt.Flow
{
    public sealed class EndingSceneController : MonoBehaviour
    {
        private DemoGameContext _gameContext;
        private DemoFlowController _flow;
        private Canvas _canvas;
        private Image _fadeOverlay;
        private GameObject _blacksmith;
        private GameObject _workbench;
        private GameObject _forge;
        private GameObject _dialoguePanel;
        private Text _dialogueText;
        private GameObject[] _items;
        private GameObject[] _playerUnits;

        private void Start()
        {
            _gameContext = DemoGameContext.Instance;
            _flow = DemoFlowController.Instance;
            StartCoroutine(PlayEndingCutscene());
        }

        private IEnumerator PlayEndingCutscene()
        {
            CreateScene();
            yield return new WaitForSeconds(0.5f);

            yield return BlacksmithWalkToWorkbench();

            yield return PlaceItem(0, "流星锤");
            yield return new WaitForSeconds(0.4f);

            yield return PlaceItem(1, "酒神圣杯");
            yield return new WaitForSeconds(0.4f);

            yield return PlaceItem(2, "巨人钥匙");
            yield return new WaitForSeconds(0.6f);

            yield return ShowDialogue("下次回来，它们就不是现在这个样子了。", 2.0f);

            yield return FadeToBlack(1.0f);
            yield return new WaitForSeconds(0.3f);

            ReturnToMainMenu();
        }

        private void CreateScene()
        {
            var cameraGo = new GameObject("EndingCamera");
            var camera = cameraGo.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5.2f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.12f, 0.08f, 0.05f);
            cameraGo.tag = "MainCamera";
            cameraGo.transform.position = new Vector3(0f, 0f, -10f);

            var canvasGo = new GameObject("EndingCanvas");
            _canvas = canvasGo.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            canvasGo.AddComponent<GraphicRaycaster>();

            CreateVillageBackground();

            _forge = CreateForge();
            _workbench = CreateWorkbench();

            _playerUnits = CreatePlayerTeam();

            _blacksmith = CreateBlacksmith();

            _items = new GameObject[3];
            for (var i = 0; i < 3; i++)
            {
                _items[i] = CreateItem(i);
                _items[i].SetActive(false);
            }

            _fadeOverlay = CreateFullScreenImage(_canvas.transform, "FadeOverlay", Color.clear);
            _fadeOverlay.raycastTarget = false;

            CreateDialogueUI();
        }

        private void CreateVillageBackground()
        {
            var bg = new GameObject("VillageBackground", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(_canvas.transform, false);
            var rect = bg.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var image = bg.GetComponent<Image>();
            image.sprite = SimpleSpriteFactory.GetWhitePixelSprite();
            image.color = new Color(0.15f, 0.1f, 0.07f, 1f);
            image.raycastTarget = false;

            var ground = new GameObject("Ground", typeof(RectTransform), typeof(Image));
            ground.transform.SetParent(_canvas.transform, false);
            var groundRect = ground.GetComponent<RectTransform>();
            groundRect.anchorMin = new Vector2(0f, 0f);
            groundRect.anchorMax = new Vector2(1f, 0.35f);
            groundRect.offsetMin = Vector2.zero;
            groundRect.offsetMax = Vector2.zero;
            var groundImage = ground.GetComponent<Image>();
            groundImage.sprite = SimpleSpriteFactory.GetWhitePixelSprite();
            groundImage.color = new Color(0.12f, 0.08f, 0.05f, 1f);
            groundImage.raycastTarget = false;

            var glow = new GameObject("ForgeGlow", typeof(RectTransform), typeof(Image));
            glow.transform.SetParent(_canvas.transform, false);
            var glowRect = glow.GetComponent<RectTransform>();
            glowRect.anchorMin = new Vector2(0.6f, 0.25f);
            glowRect.anchorMax = new Vector2(0.8f, 0.55f);
            glowRect.offsetMin = Vector2.zero;
            glowRect.offsetMax = Vector2.zero;
            var glowImage = glow.GetComponent<Image>();
            glowImage.sprite = SimpleSpriteFactory.GetWhitePixelSprite();
            glowImage.color = new Color(0.9f, 0.4f, 0.1f, 0.08f);
            glowImage.raycastTarget = false;
        }

        private GameObject CreateForge()
        {
            var forge = new GameObject("Forge", typeof(RectTransform), typeof(Image));
            forge.transform.SetParent(_canvas.transform, false);
            var rect = forge.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.62f, 0.28f);
            rect.anchorMax = new Vector2(0.72f, 0.52f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var image = forge.GetComponent<Image>();
            image.sprite = SimpleSpriteFactory.GetWhitePixelSprite();
            image.color = new Color(0.25f, 0.15f, 0.1f, 1f);
            image.raycastTarget = false;

            var fire = new GameObject("ForgeFire", typeof(RectTransform), typeof(Image));
            fire.transform.SetParent(forge.transform, false);
            var fireRect = fire.GetComponent<RectTransform>();
            fireRect.anchorMin = new Vector2(0.2f, 0.7f);
            fireRect.anchorMax = new Vector2(0.8f, 1.1f);
            fireRect.offsetMin = Vector2.zero;
            fireRect.offsetMax = Vector2.zero;
            var fireImage = fire.GetComponent<Image>();
            fireImage.sprite = SimpleSpriteFactory.GetWhitePixelSprite();
            fireImage.color = new Color(1f, 0.5f, 0.1f, 0.6f);
            fireImage.raycastTarget = false;

            StartCoroutine(AnimateForgeFire(fireImage));
            return forge;
        }

        private IEnumerator AnimateForgeFire(Image fireImage)
        {
            if (fireImage == null) yield break;

            var baseColor = new Color(1f, 0.5f, 0.1f, 0.6f);
            while (true)
            {
                var flicker = 0.85f + Mathf.Sin(Time.time * 3.5f) * 0.15f;
                fireImage.color = new Color(baseColor.r, baseColor.g * flicker, baseColor.b, baseColor.a * flicker);
                yield return null;
            }
        }

        private GameObject CreateWorkbench()
        {
            var bench = new GameObject("Workbench", typeof(RectTransform), typeof(Image));
            bench.transform.SetParent(_canvas.transform, false);
            var rect = bench.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.55f, 0.28f);
            rect.anchorMax = new Vector2(0.78f, 0.38f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var image = bench.GetComponent<Image>();
            image.sprite = SimpleSpriteFactory.GetWhitePixelSprite();
            image.color = new Color(0.3f, 0.2f, 0.12f, 1f);
            image.raycastTarget = false;

            var top = new GameObject("WorkbenchTop", typeof(RectTransform), typeof(Image));
            top.transform.SetParent(bench.transform, false);
            var topRect = top.GetComponent<RectTransform>();
            topRect.anchorMin = new Vector2(-0.02f, 0.85f);
            topRect.anchorMax = new Vector2(1.02f, 1.15f);
            topRect.offsetMin = Vector2.zero;
            topRect.offsetMax = Vector2.zero;
            var topImage = top.GetComponent<Image>();
            topImage.sprite = SimpleSpriteFactory.GetWhitePixelSprite();
            topImage.color = new Color(0.35f, 0.24f, 0.15f, 1f);
            topImage.raycastTarget = false;

            return bench;
        }

        private GameObject[] CreatePlayerTeam()
        {
            if (_gameContext == null || _gameContext.defaultBattleFormation == null)
            {
                return new GameObject[0];
            }

            var formation = _gameContext.defaultBattleFormation;
            var configs = new[]
            {
                ResolveCharacter(formation.frontCharacter),
                ResolveCharacter(formation.midCharacter),
                ResolveCharacter(formation.backCharacter),
            };

            var units = new GameObject[3];
            var positions = new[]
            {
                new Vector2(0.18f, 0.42f),
                new Vector2(0.12f, 0.38f),
                new Vector2(0.06f, 0.44f),
            };

            for (var i = 0; i < 3; i++)
            {
                if (configs[i] == null) continue;

                var unitGo = new GameObject($"Player_{i}", typeof(RectTransform), typeof(Image));
                unitGo.transform.SetParent(_canvas.transform, false);
                var rect = unitGo.GetComponent<RectTransform>();
                rect.anchorMin = positions[i];
                rect.anchorMax = positions[i] + new Vector2(0.06f, 0.12f);
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;

                var image = unitGo.GetComponent<Image>();
                image.sprite = GetPortraitSprite(configs[i]);
                image.color = new Color(1f, 1f, 1f, 0.85f);
                image.raycastTarget = false;
                image.preserveAspect = true;

                units[i] = unitGo;
            }

            return units;
        }

        private CharacterConfig ResolveCharacter(CharacterConfig baseConfig)
        {
            if (baseConfig == null || _gameContext == null) return baseConfig;

            var resolved = baseConfig;
            if (_gameContext.runState.hasRecruitedMage && baseConfig.id == _gameContext.runState.mageReplacedCharacterId)
            {
                return MageCharacterFactory.GetMageVariant(_gameContext.runState.mageRewardType);
            }

            resolved = ApplyVariant(resolved, _gameContext.buildSelection.selectedHammerTargetId, _gameContext.buildSelection.selectedHammerCharacter);
            resolved = ApplyVariant(resolved, _gameContext.buildSelection.selectedCupTargetId, _gameContext.buildSelection.selectedCupCharacter);
            resolved = ApplyVariant(resolved, _gameContext.buildSelection.selectedKeyTargetId, _gameContext.buildSelection.selectedKeyCharacter);
            return resolved;
        }

        private static CharacterConfig ApplyVariant(CharacterConfig config, string targetId, CharacterConfig variant)
        {
            if (config == null || string.IsNullOrWhiteSpace(targetId) || variant == null) return config;
            return config.id == targetId || config.baseCharacterId == targetId ? variant : config;
        }

        private GameObject CreateBlacksmith()
        {
            var bs = new GameObject("Blacksmith", typeof(RectTransform), typeof(Image));
            bs.transform.SetParent(_canvas.transform, false);
            var rect = bs.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.48f, 0.3f);
            rect.anchorMax = new Vector2(0.56f, 0.52f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var image = bs.GetComponent<Image>();
            image.sprite = GetPortraitSprite(GetBlacksmithConfig());
            image.color = Color.white;
            image.raycastTarget = false;
            image.preserveAspect = true;

            return bs;
        }

        private CharacterConfig GetBlacksmithConfig()
        {
            var config = ScriptableObject.CreateInstance<CharacterConfig>();
            config.id = "blacksmith";
            config.resourceId = "human_militia";
            config.displayName = "铁匠";
            return config;
        }

        private GameObject CreateItem(int index)
        {
            var names = new[] { "MeteorHammerItem", "HolyCupItem", "GiantKeyItem" };
            var item = new GameObject(names[index], typeof(RectTransform), typeof(Image));
            item.transform.SetParent(_canvas.transform, false);

            var rect = item.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.62f + index * 0.05f, 0.32f);
            rect.anchorMax = new Vector2(0.66f + index * 0.05f, 0.38f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var image = item.GetComponent<Image>();
            image.sprite = index switch
            {
                0 => SimpleSpriteFactory.GetMeteorHammerSprite(),
                1 => SimpleSpriteFactory.GetHolyCupSprite(),
                2 => SimpleSpriteFactory.GetGiantKeySprite(),
                _ => SimpleSpriteFactory.GetWhitePixelSprite(),
            };
            image.color = Color.white;
            image.raycastTarget = false;
            image.preserveAspect = true;

            return item;
        }

        private void CreateDialogueUI()
        {
            _dialoguePanel = new GameObject("DialoguePanel", typeof(RectTransform), typeof(Image));
            _dialoguePanel.transform.SetParent(_canvas.transform, false);
            var rect = _dialoguePanel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.15f, 0.02f);
            rect.anchorMax = new Vector2(0.85f, 0.15f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var image = _dialoguePanel.GetComponent<Image>();
            image.sprite = SimpleSpriteFactory.GetWhitePixelSprite();
            image.color = new Color(0.04f, 0.04f, 0.06f, 0.9f);
            image.raycastTarget = false;

            var textGo = new GameObject("DialogueText", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(_dialoguePanel.transform, false);
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(20f, 10f);
            textRect.offsetMax = new Vector2(-20f, -10f);

            _dialogueText = textGo.GetComponent<Text>();
            _dialogueText.font = BuildUiRuntimeStyle.GetChineseFont();
            _dialogueText.fontSize = 28;
            _dialogueText.alignment = TextAnchor.MiddleCenter;
            _dialogueText.color = Color.white;
            _dialogueText.text = string.Empty;

            _dialoguePanel.SetActive(false);
        }

        private IEnumerator BlacksmithWalkToWorkbench()
        {
            if (_blacksmith == null) yield break;

            var start = new Vector2(0.35f, 0.38f);
            var end = new Vector2(0.52f, 0.38f);
            var rect = _blacksmith.GetComponent<RectTransform>();

            var elapsed = 0f;
            var duration = 1.2f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                rect.anchorMin = Vector2.Lerp(start, end, t);
                rect.anchorMax = rect.anchorMin + new Vector2(0.08f, 0.22f);
                yield return null;
            }
        }

        private IEnumerator PlaceItem(int index, string itemName)
        {
            if (index < 0 || index >= _items.Length) yield break;

            _items[index].SetActive(true);

            var itemRect = _items[index].GetComponent<RectTransform>();
            var startPos = itemRect.anchoredPosition;
            var startAnchor = itemRect.anchorMin;

            yield return ShowDialogue($"铁匠把{itemName}放到了工作台上。", 0.8f);
        }

        private IEnumerator ShowDialogue(string text, float duration)
        {
            if (_dialoguePanel != null)
            {
                _dialoguePanel.SetActive(true);
            }

            if (_dialogueText != null)
            {
                _dialogueText.text = text;
            }

            yield return new WaitForSeconds(duration);

            if (_dialoguePanel != null)
            {
                _dialoguePanel.SetActive(false);
            }
        }

        private IEnumerator FadeToBlack(float duration)
        {
            if (_fadeOverlay == null) yield break;

            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                _fadeOverlay.color = new Color(0f, 0f, 0f, t);
                yield return null;
            }

            _fadeOverlay.color = Color.black;
        }

        private void ReturnToMainMenu()
        {
            if (_flow != null)
            {
                _flow.ReturnToMainMenu();
            }
            else
            {
                SceneManager.LoadScene("MainMenu");
            }
        }

        private static Sprite GetPortraitSprite(CharacterConfig config)
        {
            if (config == null) return null;

            return PixelAnimationLibrary.GetFirstFrameSprite(
                config.resourceId,
                "idle",
                "stand",
                "walk",
                config.defaultAttackAction);
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
    }
}
