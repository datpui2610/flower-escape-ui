using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FlowerEscape.EditorTools
{
    /// <summary>
    /// Dựng scene FlowerEscape_Gameplay từ 2 màn tĩnh (Main = Combat, BuyGear = Xếp gear):
    /// - 1 Canvas, CanvasScaler 450x1050 chế độ Expand (luôn thấy trọn khung thiết kế 9:21)
    /// - HUD neo theo góc (trái / giữa / phải), bảng gear neo đáy (đã bỏ quảng cáo), nền nối gương 2 bên cho màn rộng (3:4)
    /// - Safe Area, 2 trạng thái Combat / GearSetup có hiệu ứng chuyển
    /// Menu: Tools > Flower Escape > Build Gameplay Scene. Lần đầu mở project sẽ tự chạy nếu chưa có scene.
    /// </summary>
    [InitializeOnLoad]
    public static class FlowerEscapeSceneBuilder
    {
        public const string ScenePath = "Assets/Scenes/FlowerEscape_Gameplay.unity";

        private const float DesignWidth = 450f;
        private const float DesignHeight = 1050f;
        private const string AutoBuildKey = "FlowerEscape.AutoBuildTried.v2";

        // Bỏ banner quảng cáo: 2 bảng ở đáy hạ xuống đúng bằng chiều cao banner (107.46 trong khung 450x1050)
        private static readonly bool ShowAdBanner = false;
        private const float AdBannerHeight = 107.46f;

        // Tăng số này khi đổi bố cục -> scene tự dựng lại lần mở Unity kế tiếp
        private const int LayoutVersion = 2;
        private static string LayoutVersionKey => "FlowerEscape.LayoutVersion." + Application.dataPath;

        private static bool SceneOutdated => File.Exists(ScenePath) && EditorPrefs.GetInt(LayoutVersionKey, 1) < LayoutVersion;
        private const int UILayer = 5;

        private static int autoBuildTries;

        static FlowerEscapeSceneBuilder()
        {
            bool upToDate = File.Exists(ScenePath) && !SceneOutdated;
            if (upToDate || SessionState.GetBool(AutoBuildKey, false)) return;
            EditorApplication.delayCall += TryAutoBuild;
        }

        private static void TryAutoBuild()
        {
            bool rebuild = SceneOutdated;
            if (File.Exists(ScenePath) && !rebuild) return;

            bool editorBusy = EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode;
            bool spritesReady = AssetDatabase.LoadAssetAtPath<Sprite>(FlowerEscapeLayoutData.Layers[0].sprite) != null;
            if (editorBusy || !spritesReady)
            {
                // Chờ Unity import xong asset rồi thử lại
                if (++autoBuildTries < 200) EditorApplication.delayCall += TryAutoBuild;
                return;
            }

            SessionState.SetBool(AutoBuildKey, true);
            Build(askToSave: rebuild); // dựng lại thì hỏi lưu nếu scene đang mở có thay đổi
        }

        [MenuItem("Tools/Flower Escape/Build Gameplay Scene (9:21 + 3:4)", priority = 0)]
        private static void BuildFromMenu()
        {
            Build(askToSave: true);
        }

        [MenuItem("Tools/Flower Escape/Add Game View Sizes (9:21, 3:4)", priority = 1)]
        private static void AddGameViewSizesFromMenu()
        {
            GameViewSizeUtil.EnsureFlowerEscapeSizes();
        }

        public static void Build(bool askToSave)
        {
            if (askToSave && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateCamera();
            CreateEventSystem();

            // ---------- Canvas ----------
            RectTransform canvasRect = NewRect("Canvas - Flower Escape Gameplay", null);
            var canvas = canvasRect.gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasRect.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(DesignWidth, DesignHeight);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand; // 9:21 vừa khít, 3:4 rộng thêm 2 bên
            scaler.referencePixelsPerUnit = 100f;
            canvasRect.gameObject.AddComponent<GraphicRaycaster>();

            // Màu nền lót (chỉ lộ ra nếu màn cao hơn 9:21)
            Image fill = NewRect("BG Fill", canvasRect).gameObject.AddComponent<Image>();
            Stretch(fill.rectTransform);
            fill.color = new Color32(182, 188, 174, 255);
            fill.raycastTarget = false;

            // Nền: bản gốc ở giữa + 2 bản lật gương 2 bên để phủ màn rộng (3:4) mà không méo hình
            RectTransform bgGroup = Pinned("Background (nối gương 2 bên)", canvasRect, new Vector2(0.5f, 0.5f));

            // Mọi thứ còn lại nằm trong Safe Area
            RectTransform safeArea = NewRect("Safe Area", canvasRect);
            Stretch(safeArea);
            safeArea.gameObject.AddComponent<SafeArea>();

            RectTransform combatState = NewRect("State - Combat", safeArea);
            Stretch(combatState);
            var combatGroup = combatState.gameObject.AddComponent<CanvasGroup>();

            RectTransform gearState = NewRect("State - Gear Setup", safeArea);
            Stretch(gearState);
            var gearGroup = gearState.gameObject.AddComponent<CanvasGroup>();
            gearGroup.alpha = 0f;
            gearGroup.interactable = false;
            gearGroup.blocksRaycasts = false;

            // ---------- Nhóm neo ----------
            var groups = new Dictionary<string, RectTransform>
            {
                { "BG", bgGroup },
                // Combat
                { "Battle", Pinned("Battle Area (neo trên-giữa)", combatState, new Vector2(0.5f, 1f)) },
                { "CombatBoard", BoardRoot(BottomPinned("Combat Board (neo đáy-giữa)", combatState), "Board Root", new Vector2(188f, 820f)) },
                { "ResRight", Pinned("Resources (neo phải)", combatState, new Vector2(1f, 1f)) },
                // Xếp gear
                { "GearBoard", BoardRoot(BottomPinned("Gear Setup Board (neo đáy-giữa)", gearState), "Board Root", new Vector2(225f, 573f)) },
                // HUD dùng chung 2 trạng thái
                { "TopLeft", Pinned("HUD - Top Left", safeArea, new Vector2(0f, 1f)) },
                { "TopCenter", Pinned("HUD - Top Center", safeArea, new Vector2(0.5f, 1f)) },
                { "TopRight", Pinned("HUD - Top Right", safeArea, new Vector2(1f, 1f)) },
            };

            if (ShowAdBanner)
            {
                groups["Ad"] = Pinned("Ad Banner (neo đáy)", safeArea, new Vector2(0.5f, 0f));

                // Dải đen phía sau banner quảng cáo, kéo dài ra 2 bên và xuống dưới vùng safe area
                Image adStrip = NewRect("Ad Strip (full width)", groups["Ad"]).gameObject.AddComponent<Image>();
                PlaceDesign(adStrip.rectTransform, DesignWidth / 2f, 1096f, 4000f, 308f);
                adStrip.color = Color.black;
                adStrip.raycastTarget = false;
            }

            // ---------- Tính khung bao cho các nút gom nhiều lớp ----------
            var containerBounds = new Dictionary<string, Rect>();
            foreach (L layer in FlowerEscapeLayoutData.Layers)
            {
                if (string.IsNullOrEmpty(layer.container)) continue;
                var r = Rect.MinMaxRect(layer.cx - layer.w / 2f, layer.cy - layer.h / 2f, layer.cx + layer.w / 2f, layer.cy + layer.h / 2f);
                containerBounds[layer.container] = containerBounds.TryGetValue(layer.container, out Rect old)
                    ? Rect.MinMaxRect(Mathf.Min(old.xMin, r.xMin), Mathf.Min(old.yMin, r.yMin), Mathf.Max(old.xMax, r.xMax), Mathf.Max(old.yMax, r.yMax))
                    : r;
            }

            var containers = new Dictionary<string, RectTransform>();
            int missingSprites = 0;

            // ---------- Tạo từng lớp ảnh theo đúng thứ tự vẽ gốc ----------
            foreach (L layer in FlowerEscapeLayoutData.Layers)
            {
                if (layer.group == "Ad" && !ShowAdBanner) continue; // bỏ quảng cáo

                RectTransform parent = groups[layer.group];

                RectTransform containerRect = null;
                if (!string.IsNullOrEmpty(layer.container) && !containers.TryGetValue(layer.container, out containerRect))
                {
                    containerRect = CreateButtonContainer(layer.container, parent, containerBounds[layer.container]);
                    containers[layer.container] = containerRect;
                }

                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(layer.sprite);
                if (sprite == null)
                {
                    missingSprites++;
                    Debug.LogWarning($"[FlowerEscape] Không tìm thấy sprite: {layer.sprite}");
                }

                if (layer.group == "BG")
                {
                    // Bản giữa + 2 bản lật gương
                    CreateImage(layer.name, parent, sprite, layer.cx, layer.cy, layer.w, layer.h);
                    RectTransform left = CreateImage(layer.name + " (mirror L)", parent, sprite, layer.cx - layer.w, layer.cy, layer.w, layer.h);
                    left.localScale = new Vector3(-1f, 1f, 1f);
                    RectTransform right = CreateImage(layer.name + " (mirror R)", parent, sprite, layer.cx + layer.w, layer.cy, layer.w, layer.h);
                    right.localScale = new Vector3(-1f, 1f, 1f);
                    continue;
                }

                if (layer.flags == "stretchW")
                {
                    // Lót màu tím phía dưới thanh mua gear, phủ tới mép đáy màn hình (kể cả vùng ngoài safe area)
                    Image under = NewRect("Bar Bottom Fill (tím)", parent).gameObject.AddComponent<Image>();
                    PlaceDesign(under.rectTransform, DesignWidth / 2f, layer.cy + layer.h / 2f + 140f, 4000f, 320f);
                    under.color = new Color32(136, 100, 192, 255);
                    under.raycastTarget = false;
                }

                float width = layer.flags == "stretchW" ? 4000f : layer.w; // thanh tím đáy: kéo full bề ngang màn
                RectTransform rect = CreateImage(layer.name, parent, sprite, layer.cx, layer.cy, width, layer.h);
                if (containerRect != null) rect.SetParent(containerRect, true);
            }

            // ---------- Vùng chạm bảng gear ở Combat -> mở Xếp gear ----------
            RectTransform boardTap = NewRect("Btn_OpenGearSetup (chạm bảng gear)", groups["CombatBoard"]);
            PlaceDesign(boardTap, 188f, 766f, 348f, 352f);
            var boardTapImage = boardTap.gameObject.AddComponent<Image>();
            boardTapImage.color = new Color(1f, 1f, 1f, 0f);
            var boardTapButton = boardTap.gameObject.AddComponent<Button>();
            boardTapButton.transition = Selectable.Transition.None;

            // ---------- Controller + nối sự kiện ----------
            var controller = canvasRect.gameObject.AddComponent<GameplayStateController>();
            var so = new SerializedObject(controller);
            so.FindProperty("combatGroup").objectReferenceValue = combatGroup;
            so.FindProperty("gearSetupGroup").objectReferenceValue = gearGroup;
            so.FindProperty("combatBoard").objectReferenceValue = groups["CombatBoard"];
            so.FindProperty("gearSetupBoard").objectReferenceValue = groups["GearBoard"];
            var refresh = so.FindProperty("refreshTargets");
            string[] refreshNames = { "Btn_BuyGear1", "Btn_BuyGear2", "Btn_BuyGear3", "Btn_BuyGear4" };
            refresh.arraySize = 0;
            foreach (string n in refreshNames)
            {
                if (!containers.TryGetValue(n, out RectTransform rt)) continue;
                refresh.arraySize++;
                refresh.GetArrayElementAtIndex(refresh.arraySize - 1).objectReferenceValue = rt;
            }
            so.ApplyModifiedPropertiesWithoutUndo();

            UnityEventTools.AddPersistentListener(boardTapButton.onClick, new UnityAction(controller.ShowGearSetup));
            if (containers.TryGetValue("Btn_Start", out RectTransform start))
                UnityEventTools.AddPersistentListener(start.GetComponent<Button>().onClick, new UnityAction(controller.ShowCombat));
            if (containers.TryGetValue("Btn_Refresh", out RectTransform refreshButton))
                UnityEventTools.AddPersistentListener(refreshButton.GetComponent<Button>().onClick, new UnityAction(controller.PlayRefresh));

            // ---------- Lưu scene ----------
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings(ScenePath);
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            GameViewSizeUtil.EnsureFlowerEscapeSizes();

            EditorPrefs.SetInt(LayoutVersionKey, LayoutVersion);
            Selection.activeGameObject = canvasRect.gameObject;
            Debug.Log($"[FlowerEscape] Đã dựng {ScenePath}: {FlowerEscapeLayoutData.Layers.Length} lớp ảnh, {containers.Count} nút, thiếu sprite: {missingSprites}. " +
                      "Chọn Game view 'Flower Escape 9:21' hoặc '3:4' để kiểm tra. Khi Play: chạm bảng gear -> Xếp gear, START -> Combat, phím Space đổi trạng thái.");
        }

        // ================= helpers =================

        private static void CreateCamera()
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            var cam = go.AddComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color32(14, 20, 32, 255);
            go.transform.position = new Vector3(0f, 0f, -10f);
        }

        private static void CreateEventSystem()
        {
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            // Project dùng Input System mới -> dùng InputSystemUIInputModule nếu có
            System.Type module = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (module != null) go.AddComponent(module);
            else go.AddComponent<StandaloneInputModule>();
        }

        private static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = UILayer;
            if (parent != null) go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        /// <summary>Khung thiết kế 450x1050 ghim vào 1 điểm neo của màn hình (góc/cạnh).</summary>
        private static RectTransform Pinned(string name, Transform parent, Vector2 anchor)
        {
            RectTransform rect = NewRect(name, parent);
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.sizeDelta = new Vector2(DesignWidth, DesignHeight);
            rect.anchoredPosition = Vector2.zero;
            return rect;
        }

        /// <summary>Khung neo đáy; khi bỏ quảng cáo thì hạ xuống bằng chiều cao banner để sát đáy màn.</summary>
        private static RectTransform BottomPinned(string name, Transform parent)
        {
            RectTransform rect = Pinned(name, parent, new Vector2(0.5f, 0f));
            if (!ShowAdBanner) rect.anchoredPosition = new Vector2(0f, -AdBannerHeight);
            return rect;
        }

        /// <summary>Lớp con phủ kín khung, pivot đặt ở tâm bảng gear để hiệu ứng phóng to quanh tâm bảng.</summary>
        private static RectTransform BoardRoot(RectTransform pinned, string name, Vector2 designCenter)
        {
            RectTransform rect = NewRect(name, pinned);
            Stretch(rect);
            rect.pivot = new Vector2(designCenter.x / DesignWidth, 1f - designCenter.y / DesignHeight);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        /// <summary>Đặt theo toạ độ thiết kế: tâm (cx, cy) tính từ góc trên-trái khung 450x1050.</summary>
        private static void PlaceDesign(RectTransform rect, float cx, float cy, float w, float h)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(w, h);
            rect.anchoredPosition = new Vector2(cx, -cy);
        }

        private static RectTransform CreateImage(string name, Transform parent, Sprite sprite, float cx, float cy, float w, float h)
        {
            RectTransform rect = NewRect(name, parent);
            PlaceDesign(rect, cx, cy, w, h);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false; // chỉ vùng nút mới nhận chạm
            return rect;
        }

        private static RectTransform CreateButtonContainer(string name, Transform parent, Rect designBounds)
        {
            const float pad = 4f;
            RectTransform rect = NewRect(name, parent);
            PlaceDesign(rect, designBounds.center.x, designBounds.center.y, designBounds.width + pad * 2f, designBounds.height + pad * 2f);

            var hit = rect.gameObject.AddComponent<Image>();
            hit.color = new Color(1f, 1f, 1f, 0f); // trong suốt, chỉ để nhận chạm
            var button = rect.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = hit;
            rect.gameObject.AddComponent<UIPressFeedback>();
            return rect;
        }

        private static void AddSceneToBuildSettings(string path)
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            scenes.RemoveAll(s => s.path == path);
            scenes.Insert(0, new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
