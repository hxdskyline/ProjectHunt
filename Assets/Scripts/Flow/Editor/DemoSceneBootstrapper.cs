using System.Collections.Generic;
using ProjectHunt.Battle;
using ProjectHunt.Build;
using ProjectHunt.Data;
using ProjectHunt.EditorTools;
using ProjectHunt.UI;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ProjectHunt.Flow.Editor
{
    public static class DemoSceneBootstrapper
    {
        private const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";
        private const string BattleScenePath = "Assets/Scenes/BattleScene.unity";
        private const string BuildScenePath = "Assets/Scenes/BuildScene.unity";
        private const string ResultScenePath = "Assets/Scenes/ResultScene.unity";

        [MenuItem("Project Hunt/Bootstrap/Build Demo Scenes")]
        public static void BuildDemoScenes()
        {
            DemoConfigBootstrapper.CreateDemoConfigAssets();

            SetupMainMenuScene();
            SetupBattleScene();
            SetupBuildScene();
            SetupResultScene();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Project Hunt", "Demo scenes bootstrapped.", "OK");
        }

        private static void SetupMainMenuScene()
        {
            var scene = EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);
            ClearScene(scene);

            var context = CreateContextAndFlow(out var flow);
            context.runState.phase = GamePhase.MainMenu;

            var canvas = CreateCanvas("MainMenuCanvas");
            EnsureEventSystem();

            CreateCenteredLabel(canvas.transform, "Title", "Target Hunt Demo", new Vector2(0f, 120f), 36);
            var startButton = CreateButton(canvas.transform, "StartButton", "Start Hunt", new Vector2(0f, -20f), new Vector2(260f, 70f));
            var startAction = startButton.gameObject.AddComponent<SceneFlowButtonAction>();
            startAction.actionType = SceneFlowButtonAction.ActionType.StartRun;
            EditorSceneManager.MarkSceneDirty(scene);
            UnityEventTools.AddPersistentListener(startButton.onClick, startAction.Execute);
            EditorSceneManager.SaveScene(scene);
        }

        private static void SetupBattleScene()
        {
            var scene = EditorSceneManager.OpenScene(BattleScenePath, OpenSceneMode.Single);
            ClearScene(scene);

            var context = CreateContextAndFlow(out var flow);
            var refsGo = new GameObject("BattleSceneReferences");
            var refs = refsGo.AddComponent<BattleSceneReferences>();

            refs.playerTeamRoot = new GameObject("PlayerTeamRoot").transform;
            refs.playerTeamRoot.SetParent(refsGo.transform);
            refs.bossRoot = new GameObject("BossRoot").transform;
            refs.bossRoot.SetParent(refsGo.transform);
            refs.dropRoot = new GameObject("DropRoot").transform;
            refs.dropRoot.SetParent(refsGo.transform);

            var spawnRoot = new GameObject("SpawnPoints").transform;
            refs.playerFrontPoint = CreatePoint(spawnRoot, "PlayerFrontPoint", new Vector3(-4.5f, -1.5f, 0f));
            refs.playerMidPoint = CreatePoint(spawnRoot, "PlayerMidPoint", new Vector3(-5.8f, -1.5f, 0f));
            refs.playerBackPoint = CreatePoint(spawnRoot, "PlayerBackPoint", new Vector3(-7.1f, -1.5f, 0f));
            refs.bossPoint = CreatePoint(spawnRoot, "BossPoint", new Vector3(4.7f, -1.4f, 0f));
            refs.dropPoint = CreatePoint(spawnRoot, "DropPoint", new Vector3(3.8f, -2.6f, 0f));

            var battleDirectorGo = new GameObject("BattleDirector");
            var battleDirector = battleDirectorGo.AddComponent<BattleDirector>();
            battleDirector.gameContext = context;
            battleDirector.flowController = flow;
            battleDirector.sceneReferences = refs;

            var spawnerGo = new GameObject("BattleFormationSpawner");
            var spawner = spawnerGo.AddComponent<BattleFormationSpawner>();
            spawner.gameContext = context;
            spawner.sceneReferences = refs;
            spawner.battleDirector = battleDirector;

            battleDirector.formationSpawner = spawner;

            var cameraGo = new GameObject("Main Camera");
            var camera = cameraGo.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5.2f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.12f, 0.12f, 0.18f);
            cameraGo.tag = "MainCamera";
            cameraGo.transform.position = new Vector3(0f, 0f, -10f);

            var canvas = CreateCanvas("BattleCanvas");
            var bossHpSlider = CreateSlider(canvas.transform, "BossHpBar", new Vector2(0f, -40f), new Vector2(540f, 30f));
            var bossHpText = CreateCenteredLabel(canvas.transform, "BossHpText", "Boss", new Vector2(0f, -10f), 22);
            var dropHint = CreateCenteredLabel(canvas.transform, "DropHint", "Click to Claim Meteor Hammer", new Vector2(0f, 220f), 20);
            dropHint.gameObject.SetActive(false);

            var hpView = bossHpSlider.gameObject.AddComponent<BossHpBarView>();
            hpView.slider = bossHpSlider;
            hpView.label = bossHpText;

            battleDirector.bossHpBarView = hpView;
            battleDirector.dropHintText = dropHint;

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void SetupBuildScene()
        {
            var scene = EditorSceneManager.OpenScene(BuildScenePath, OpenSceneMode.Single);
            ClearScene(scene);

            var context = CreateContextAndFlow(out var flow);
            context.runState.phase = GamePhase.Build;

            var canvas = CreateCanvas("BuildCanvas");
            EnsureEventSystem();

            CreateCenteredLabel(canvas.transform, "BuildTitle", "Choose a Wielder", new Vector2(0f, 220f), 32);

            var controllerGo = new GameObject("BuildSelectionController");
            controllerGo.transform.SetParent(canvas.transform, false);
            var controller = controllerGo.AddComponent<BuildSelectionController>();
            controller.flowController = flow;
            controller.gameContext = context;

            var slotConfigs = new[]
            {
                AssetDatabase.LoadAssetAtPath<CharacterConfig>("Assets/Data/Characters/CD_Player_Swordsman.asset"),
                AssetDatabase.LoadAssetAtPath<CharacterConfig>("Assets/Data/Characters/CD_Player_Brawler.asset"),
                AssetDatabase.LoadAssetAtPath<CharacterConfig>("Assets/Data/Characters/CD_Player_Archer.asset"),
            };

            var slotPositions = new[] { -320f, 0f, 320f };
            var slotViews = new List<BuildCharacterSlotView>();
            for (var i = 0; i < slotConfigs.Length; i++)
            {
                var slotRoot = CreatePanel(canvas.transform, $"Slot_{slotConfigs[i].displayName}", new Vector2(slotPositions[i], 10f), new Vector2(220f, 260f));
                var slotView = slotRoot.AddComponent<BuildCharacterSlotView>();
                slotView.characterConfig = slotConfigs[i];
                slotView.selectedHighlight = CreatePanel(slotRoot.transform, "SelectedHighlight", Vector2.zero, new Vector2(220f, 260f), new Color(0.95f, 0.85f, 0.2f, 0.18f)).gameObject;
                slotView.selectedHighlight.SetActive(false);
                slotView.weaponPreview = CreateImage(slotRoot.transform, "WeaponPreview", new Vector2(0f, -60f), new Vector2(64f, 64f));
                slotView.weaponPreview.enabled = false;

                CreateCenteredLabel(slotRoot.transform, "Name", slotConfigs[i].displayName, new Vector2(0f, 90f), 24);
                CreateCenteredLabel(slotRoot.transform, "ResourceName", slotConfigs[i].resourceId, new Vector2(0f, 50f), 16);

                var dropSlot = slotRoot.AddComponent<BuildDropSlot>();
                dropSlot.selectionController = controller;
                dropSlot.characterSlotView = slotView;

                slotViews.Add(slotView);
            }

            controller.characterSlots = slotViews;

            var dragItem = CreateImage(canvas.transform, "MeteorHammerDragItem", new Vector2(0f, -180f), new Vector2(96f, 96f));
            dragItem.color = Color.white;
            dragItem.sprite = SimpleSpriteFactory.GetMeteorHammerSprite();
            var dragCanvasGroup = dragItem.gameObject.AddComponent<CanvasGroup>();
            dragCanvasGroup.blocksRaycasts = true;
            var dragLogic = dragItem.gameObject.AddComponent<BuildDragItem>();
            dragLogic.selectionController = controller;
            controller.dragItemVisual = dragItem;

            var confirmButton = CreateButton(canvas.transform, "ConfirmButton", "Start Next Battle", new Vector2(0f, -290f), new Vector2(280f, 70f));
            controller.confirmButton = confirmButton;
            UnityEventTools.AddPersistentListener(confirmButton.onClick, controller.ConfirmSelection);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void SetupResultScene()
        {
            var scene = EditorSceneManager.OpenScene(ResultScenePath, OpenSceneMode.Single);
            ClearScene(scene);

            var context = CreateContextAndFlow(out var flow);
            context.runState.phase = GamePhase.Result;

            var canvas = CreateCanvas("ResultCanvas");
            EnsureEventSystem();

            CreateCenteredLabel(canvas.transform, "ResultTitle", "Weapon Claimed", new Vector2(0f, 100f), 34);
            var restartButton = CreateButton(canvas.transform, "RestartButton", "Restart", new Vector2(0f, -40f), new Vector2(220f, 70f));
            var restartAction = restartButton.gameObject.AddComponent<SceneFlowButtonAction>();
            restartAction.actionType = SceneFlowButtonAction.ActionType.ReturnToMainMenu;
            UnityEventTools.AddPersistentListener(restartButton.onClick, restartAction.Execute);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static DemoGameContext CreateContextAndFlow(out DemoFlowController flowController)
        {
            var contextGo = new GameObject("DemoGameContext");
            var context = contextGo.AddComponent<DemoGameContext>();
            context.defaultBattleFormation =
                AssetDatabase.LoadAssetAtPath<BattleFormationConfig>("Assets/Data/RuntimeTemplates/FD_DefaultBattleFormation.asset");

            flowController = contextGo.AddComponent<DemoFlowController>();
            flowController.gameContext = context;
            return context;
        }

        private static void ClearScene(Scene scene)
        {
            var roots = scene.GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                Object.DestroyImmediate(roots[i]);
            }
        }

        private static Canvas CreateCanvas(string name)
        {
            var go = new GameObject(name);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            go.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindObjectOfType<EventSystem>() != null)
            {
                return;
            }

            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();
        }

        private static Text CreateCenteredLabel(Transform parent, string name, string text, Vector2 anchoredPos, int fontSize)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(600f, 60f);
            rect.anchoredPosition = anchoredPos;

            var uiText = go.AddComponent<Text>();
            uiText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            uiText.fontSize = fontSize;
            uiText.alignment = TextAnchor.MiddleCenter;
            uiText.color = Color.white;
            uiText.text = text;
            return uiText;
        }

        private static Button CreateButton(Transform parent, string name, string text, Vector2 anchoredPos, Vector2 size)
        {
            var go = CreatePanel(parent, name, anchoredPos, size, new Color(0.2f, 0.2f, 0.2f, 0.95f)).gameObject;
            var button = go.AddComponent<Button>();
            var label = CreateCenteredLabel(go.transform, "Label", text, Vector2.zero, 22);
            label.rectTransform.sizeDelta = size;
            return button;
        }

        private static Image CreateImage(Transform parent, string name, Vector2 anchoredPos, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPos;
            return go.AddComponent<Image>();
        }

        private static Image CreatePanel(Transform parent, string name, Vector2 anchoredPos, Vector2 size)
        {
            return CreatePanel(parent, name, anchoredPos, size, new Color(0.1f, 0.1f, 0.1f, 0.75f));
        }

        private static Image CreatePanel(Transform parent, string name, Vector2 anchoredPos, Vector2 size, Color color)
        {
            var panel = CreateImage(parent, name, anchoredPos, size);
            panel.color = color;
            return panel;
        }

        private static Transform CreatePoint(Transform parent, string name, Vector3 position)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.position = position;
            return go.transform;
        }

        private static Slider CreateSlider(Transform parent, string name, Vector2 anchoredPos, Vector2 size)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            var rect = root.AddComponent<RectTransform>();
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPos;

            var background = CreateImage(root.transform, "Background", Vector2.zero, size);
            background.color = new Color(0.2f, 0.2f, 0.2f, 1f);

            var fillArea = new GameObject("FillArea");
            fillArea.transform.SetParent(root.transform, false);
            var fillAreaRect = fillArea.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = new Vector2(0f, 0f);
            fillAreaRect.anchorMax = new Vector2(1f, 1f);
            fillAreaRect.offsetMin = new Vector2(5f, 5f);
            fillAreaRect.offsetMax = new Vector2(-5f, -5f);

            var fill = CreateImage(fillArea.transform, "Fill", Vector2.zero, size - new Vector2(10f, 10f));
            fill.color = new Color(0.8f, 0.15f, 0.15f, 1f);

            var slider = root.AddComponent<Slider>();
            slider.fillRect = fill.rectTransform;
            slider.targetGraphic = fill;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 100f;
            slider.value = 100f;
            return slider;
        }
    }
}
