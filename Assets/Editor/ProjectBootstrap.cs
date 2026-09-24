using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game2048.Editor
{
    /// <summary>
    /// Generates Game.unity, Tile.prefab and player settings from code so the scene never has to be
    /// wired by hand. Menu: 2048 / Setup Project. Batch: -executeMethod Game2048.Editor.ProjectBootstrap.SetupProject
    /// </summary>
    public static class ProjectBootstrap
    {
        public const string ScenePath = "Assets/Scenes/Game.unity";
        public const string PrefabPath = "Assets/Prefabs/Tile.prefab";
        public const string BundleId = "com.gvnai.game2048";

        private const string FontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
        private const string RoundedSpritePath = "Assets/Sprites/RoundedRect.png";
        private const int RoundedTextureSize = 96;
        private const int RoundedRadius = 24;

        // Canvas reference resolution (portrait phone).
        private const float RefWidth = 1080f;
        private const float RefHeight = 1920f;

        private const int BoardSize = Core.BoardModel.DefaultSize;

        private static TMP_FontAsset _font;
        private static Sprite _rounded;

        [MenuItem("2048/Setup Project")]
        public static void SetupProject()
        {
            ConfigurePlayerSettings();
            BuildSceneAndPrefab();
            Debug.Log("[2048] Project setup complete: " + ScenePath);
        }

        [MenuItem("2048/Rebuild Scene")]
        public static void BuildSceneAndPrefab()
        {
            _font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (_font == null)
                throw new FileNotFoundException("TMP Essential Resources missing. Use Window > TextMeshPro > Import TMP Essential Resources.", FontPath);
            Directory.CreateDirectory("Assets/Prefabs");
            Directory.CreateDirectory("Assets/Scenes");
            Directory.CreateDirectory("Assets/Sprites");

            _rounded = BuildRoundedSprite();
            BuildTilePrefab();
            BuildScene();
            AssetDatabase.SaveAssets();
        }

        public static void ConfigurePlayerSettings()
        {
            PlayerSettings.companyName = "gvnai";
            PlayerSettings.productName = "2048";
            PlayerSettings.bundleVersion = "1.0.0";

            foreach (var target in new[] { NamedBuildTarget.iOS, NamedBuildTarget.Android, NamedBuildTarget.Standalone })
                PlayerSettings.SetApplicationIdentifier(target, BundleId);

            // Portrait only on mobile.
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;

            // iOS
            PlayerSettings.iOS.buildNumber = "1";
            PlayerSettings.iOS.requiresFullScreen = true;

            // Android: IL2CPP + ARM64 (required by Google Play, runs on Apple silicon emulators).
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.bundleVersionCode = 1;

            // Desktop: Mono (Windows can be built from macOS), portrait-ish resizable window.
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.defaultScreenWidth = 540;
            PlayerSettings.defaultScreenHeight = 960;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = false;

            // Input System package only (0 = old, 1 = new, 2 = both). Takes effect after editor restart.
            var playerSettings = Unsupported.GetSerializedAssetInterfaceSingleton("PlayerSettings");
            var so = new SerializedObject(playerSettings);
            var handler = so.FindProperty("activeInputHandler");
            if (handler != null && handler.intValue != 1)
            {
                handler.intValue = 1;
                so.ApplyModifiedPropertiesWithoutUndo();
                Debug.Log("[2048] Active Input Handling set to Input System Package (restart the editor if it is open).");
            }

            AssetDatabase.SaveAssets();
        }

        // ---------------------------------------------------------------- prefab

        // ---------------------------------------------------------------- sprite

        /// <summary>White rounded rectangle without outline, 9-sliced so any size keeps crisp corners.</summary>
        private static Sprite BuildRoundedSprite()
        {
            const int size = RoundedTextureSize;
            const float radius = RoundedRadius;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                // Distance outside the rounded rect (negative inside), 1px anti-aliased edge.
                float px = Mathf.Max(Mathf.Abs(x + 0.5f - size * 0.5f) - (size * 0.5f - radius), 0f);
                float py = Mathf.Max(Mathf.Abs(y + 0.5f - size * 0.5f) - (size * 0.5f - radius), 0f);
                float distance = Mathf.Sqrt(px * px + py * py) - radius;
                byte alpha = (byte)Mathf.RoundToInt(Mathf.Clamp01(0.5f - distance) * 255f);
                pixels[y * size + x] = new Color32(255, 255, 255, alpha);
            }
            texture.SetPixels32(pixels);
            File.WriteAllBytes(RoundedSpritePath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(RoundedSpritePath, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(RoundedSpritePath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spriteBorder = new Vector4(RoundedRadius + 2, RoundedRadius + 2, RoundedRadius + 2, RoundedRadius + 2);
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(RoundedSpritePath);
        }

        // ---------------------------------------------------------------- prefab

        private static void BuildTilePrefab()
        {
            var root = NewUI("Tile", null);
            var image = root.gameObject.AddComponent<Image>();
            StyleRounded(image, TileColors.TileBackground(2));
            image.raycastTarget = false;
            root.sizeDelta = new Vector2(220f, 220f);

            var label = NewText("Label", root, "2", 110f, TileColors.DarkText, TextAlignmentOptions.Center);
            Stretch(label.rectTransform, 12f, 12f, 12f, 12f);
            label.fontStyle = FontStyles.Bold;
            label.enableAutoSizing = true;
            label.fontSizeMin = 20f;
            label.fontSizeMax = 110f;

            var view = root.gameObject.AddComponent<TileView>();
            SetRef(view, "background", image);
            SetRef(view, "label", label);

            PrefabUtility.SaveAsPrefabAsset(root.gameObject, PrefabPath);
            Object.DestroyImmediate(root.gameObject);
        }

        // ---------------------------------------------------------------- scene

        private static void BuildScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Load assets after NewScene: opening a scene unloads objects that were only held in memory.
            _rounded = AssetDatabase.LoadAssetAtPath<Sprite>(RoundedSpritePath);
            var tilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath).GetComponent<TileView>();

            // Camera only clears the background; all content is Screen Space Overlay UI.
            var cameraGo = new GameObject("Main Camera");
            cameraGo.tag = "MainCamera";
            var camera = cameraGo.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = TileColors.Background;
            camera.orthographic = true;
            camera.cullingMask = 0;

            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<InputSystemUIInputModule>();

            // Canvas
            var canvasRect = NewUI("Canvas", null);
            var canvas = canvasRect.gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasRect.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(RefWidth, RefHeight);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            canvasRect.gameObject.AddComponent<GraphicRaycaster>();

            var background = NewUI("Background", canvasRect);
            Stretch(background);
            var bgImage = background.gameObject.AddComponent<Image>();
            bgImage.color = TileColors.Background;
            bgImage.raycastTarget = false;

            var safeArea = NewUI("SafeArea", canvasRect);
            Stretch(safeArea);
            safeArea.gameObject.AddComponent<SafeAreaFitter>();

            var content = NewUI("Content", safeArea);
            Stretch(content, 48f, 48f, 48f, 48f);

            // Header: title + score boxes
            var header = NewUI("Header", content);
            TopBand(header, 0f, 200f);

            var title = NewText("Title", header, "2048", 150f, TileColors.DarkText, TextAlignmentOptions.MidlineLeft);
            title.fontStyle = FontStyles.Bold;
            Anchor(title.rectTransform, new Vector2(0f, 0f), new Vector2(0.45f, 1f));

            var bestText = ScoreBox("Best", header, "BEST", 1f);
            var scoreText = ScoreBox("Score", header, "SCORE", 1f - 0.25f);

            // Toolbar: hint + buttons
            var toolbar = NewUI("Toolbar", content);
            TopBand(toolbar, 230f, 120f);

            var hint = NewText("Hint", toolbar, "Join the tiles,\nget to <b>2048!</b>", 36f, TileColors.DarkText, TextAlignmentOptions.MidlineLeft);
            Anchor(hint.rectTransform, new Vector2(0f, 0f), new Vector2(0.45f, 1f));

            var undoButton = NewButton("UndoButton", toolbar, "Undo");
            Anchor((RectTransform)undoButton.transform, new Vector2(0.5f, 0f), new Vector2(0.72f, 1f));
            var restartButton = NewButton("RestartButton", toolbar, "Restart");
            Anchor((RectTransform)restartButton.transform, new Vector2(0.75f, 0f), new Vector2(1f, 1f));

            // Board area: square board fitted into the remaining space
            var boardArea = NewUI("BoardArea", content);
            Stretch(boardArea, 0f, 0f, 0f, 400f);

            var board = NewUI("Board", boardArea);
            board.gameObject.AddComponent<SquareFitter>();
            var boardImage = board.gameObject.AddComponent<Image>();
            StyleRounded(boardImage, TileColors.Board, 1.4f);
            boardImage.raycastTarget = false;

            var gridRect = NewUI("Grid", board);
            Stretch(gridRect);
            var grid = gridRect.gameObject.AddComponent<GridLayoutGroup>();
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment = TextAnchor.UpperLeft;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = BoardSize;
            for (int i = 0; i < BoardSize * BoardSize; i++)
            {
                var cell = NewUI("Cell" + i, gridRect);
                var cellImage = cell.gameObject.AddComponent<Image>();
                StyleRounded(cellImage, TileColors.EmptyCell);
                cellImage.raycastTarget = false;
            }

            var tiles = NewUI("Tiles", board);
            Stretch(tiles);

            var boardView = board.gameObject.AddComponent<BoardView>();
            SetRef(boardView, "grid", grid);
            SetRef(boardView, "tileLayer", tiles);
            SetRef(boardView, "tilePrefab", tilePrefab);

            // Result overlay on top of the board
            var overlay = NewUI("ResultOverlay", board);
            Stretch(overlay);
            var overlayImage = overlay.gameObject.AddComponent<Image>();
            StyleRounded(overlayImage, TileColors.OverlayLost, 1.4f);

            var overlayTitle = NewText("Title", overlay, "Game Over", 120f, TileColors.DarkText, TextAlignmentOptions.Center);
            overlayTitle.fontStyle = FontStyles.Bold;
            Anchor(overlayTitle.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 0.8f));

            var overlayButtons = NewUI("Buttons", overlay);
            Anchor(overlayButtons, new Vector2(0.1f, 0.28f), new Vector2(0.9f, 0.42f));
            var row = overlayButtons.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.childAlignment = TextAnchor.MiddleCenter;
            row.spacing = 40f;
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = true;

            var continueButton = NewButton("ContinueButton", overlayButtons, "Continue");
            continueButton.gameObject.AddComponent<LayoutElement>().preferredWidth = 320f;
            var overlayRestart = NewButton("RestartButton", overlayButtons, "Restart");
            overlayRestart.gameObject.AddComponent<LayoutElement>().preferredWidth = 320f;
            overlay.gameObject.SetActive(false);

            // UI + game objects
            var ui = canvasRect.gameObject.AddComponent<GameUI>();
            SetRef(ui, "scoreText", scoreText);
            SetRef(ui, "bestText", bestText);
            SetRef(ui, "restartButton", restartButton);
            SetRef(ui, "undoButton", undoButton);
            SetRef(ui, "overlay", overlay.gameObject);
            SetRef(ui, "overlayBackground", overlayImage);
            SetRef(ui, "overlayTitle", overlayTitle);
            SetRef(ui, "overlayContinueButton", continueButton);
            SetRef(ui, "overlayRestartButton", overlayRestart);

            var managerGo = new GameObject("GameManager");
            var input = managerGo.AddComponent<InputHandler>();
            var manager = managerGo.AddComponent<GameManager>();
            SetRef(manager, "boardView", boardView);
            SetRef(manager, "ui", ui);
            SetRef(manager, "input", input);

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }

        // ---------------------------------------------------------------- helpers

        private static TMP_Text ScoreBox(string name, RectTransform parent, string caption, float rightAnchor)
        {
            var box = NewUI(name + "Box", parent);
            Anchor(box, new Vector2(rightAnchor - 0.22f, 0.1f), new Vector2(rightAnchor, 0.9f));
            var image = box.gameObject.AddComponent<Image>();
            StyleRounded(image, TileColors.ScoreBox);
            image.raycastTarget = false;

            var label = NewText("Caption", box, caption, 30f, TileColors.ScoreLabel, TextAlignmentOptions.Center);
            label.fontStyle = FontStyles.Bold;
            Anchor(label.rectTransform, new Vector2(0f, 0.58f), new Vector2(1f, 0.95f));

            var value = NewText("Value", box, "0", 56f, Color.white, TextAlignmentOptions.Center);
            value.fontStyle = FontStyles.Bold;
            value.enableAutoSizing = true;
            value.fontSizeMin = 20f;
            value.fontSizeMax = 56f;
            Anchor(value.rectTransform, new Vector2(0.05f, 0.08f), new Vector2(0.95f, 0.62f));
            return value;
        }

        private static Button NewButton(string name, RectTransform parent, string text)
        {
            var rect = NewUI(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            StyleRounded(image, TileColors.Button);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.disabledColor = new Color(1f, 1f, 1f, 0.4f);
            button.colors = colors;

            var label = NewText("Label", rect, text, 44f, TileColors.LightText, TextAlignmentOptions.Center);
            label.fontStyle = FontStyles.Bold;
            Stretch(label.rectTransform, 8f, 8f, 8f, 8f);
            return button;
        }

        private static TextMeshProUGUI NewText(string name, RectTransform parent, string text, float size, Color color,
            TextAlignmentOptions alignment)
        {
            var rect = NewUI(name, parent);
            var tmp = rect.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.font = _font;
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = alignment;
            tmp.raycastTarget = false;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            return tmp;
        }

        private static RectTransform NewUI(string name, RectTransform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            var rect = (RectTransform)go.transform;
            if (parent != null) rect.SetParent(parent, false);
            return rect;
        }

        /// <param name="cornerScale">Higher = smaller corners (Image.pixelsPerUnitMultiplier).</param>
        private static void StyleRounded(Image image, Color color, float cornerScale = 2f)
        {
            image.sprite = _rounded;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = cornerScale;
            image.color = color;
        }

        private static void Stretch(RectTransform rect, float left = 0f, float bottom = 0f, float right = 0f, float top = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        private static void Anchor(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        /// <summary>Full-width band anchored to the top at <paramref name="top"/> with a fixed height.</summary>
        private static void TopBand(RectTransform rect, float top, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -top);
            rect.sizeDelta = new Vector2(0f, height);
        }

        private static void SetRef(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            var property = so.FindProperty(field);
            if (property == null) throw new System.MissingFieldException(target.GetType().Name, field);
            property.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
