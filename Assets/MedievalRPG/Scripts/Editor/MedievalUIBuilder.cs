using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;
using MedievalRPG.Combat;
using MedievalRPG.UI;

namespace MedievalRPG.EditorTools
{
    /// <summary>
    /// Trình khởi tạo tài nguyên UI, Font Tiếng Việt SDF và dựng sẵn Canvas uGUI cho cả hai Scene:
    /// - Assets/MedievalRPG/Scenes/MainMenu.unity
    /// - Assets/MedievalRPG/Scenes/MedievalBattleDemo.unity
    /// </summary>
    [InitializeOnLoad]
    public static class MedievalUIBuilder
    {
        private const string UI_SPRITE_ROOT = "Assets/MedievalRPG/Sprites/UI";
        private const string FONT_ROOT      = "Assets/MedievalRPG/Fonts";
        private const string MAIN_MENU_PATH = "Assets/MedievalRPG/Scenes/MainMenu.unity";
        private const string BATTLE_PATH    = "Assets/MedievalRPG/Scenes/MedievalBattleDemo.unity";
        private const string INPUT_ACTIONS  = "Assets/Settings/InputSystem_Actions.inputactions";

        static MedievalUIBuilder()
        {
            EditorApplication.delayCall += EnsureBuiltOnFirstLoad;
        }

        private static void EnsureBuiltOnFirstLoad()
        {
            if (Application.isBatchMode || Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            if (File.Exists($"{FONT_ROOT}/Montserrat-SemiBold SDF.asset") &&
                File.Exists("Assets/TextMesh Pro/Resources/TMP Settings.asset"))
            {
                return;
            }

            SceneSetup[] previousSetup = EditorSceneManager.GetSceneManagerSetup();
            BuildAllUI();
            if (previousSetup != null && previousSetup.Length > 0)
            {
                EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
            }
        }

        private const string VIETNAMESE_CHARS =
            " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~" +
            "ÀÁÂÃÈÉÊÌÍÒÓÔÕÙÚÝàáâãèéêìíòóôõùúýĂăĐđĨĩŨũƠơƯư" +
            "ẠạẢảẤấẦầẨẩẪẫẬậẮắẰằẲẳẴẵẶặẸẹẺẻẼẽẾếỀềỂểỄễỆệỈỉỊịỌọỎỏỐốỒồỔổỖỗỘộỚớỜờỞởỠỡỢợỤụỦủỨứỪừỬửỮữỰựỲỳỴỵỶỷỸỹ–—•×÷°%";

        private struct UIAssets
        {
            public Sprite panelDarkGold;
            public Sprite panelInset;
            public Sprite buttonFill;
            public Sprite buttonFrame;
            public Sprite headerRibbon;
            public Sprite barFrame;
            public Sprite barFill;
            public Sprite portraitFrame;
            public Sprite badgePill;

            public Sprite iconSword;
            public Sprite iconMagic;
            public Sprite iconArrow;
            public Sprite iconHeart;
            public Sprite iconShield;
            public Sprite iconScroll;
            public Sprite iconCrown;
            public Sprite iconSkull;
            public Sprite iconGear;
            public Sprite iconPlay;
            public Sprite iconBook;
            public Sprite iconExit;
            public Sprite iconMana;
            public Sprite iconTurnArrow;

            public Sprite portKnight;
            public Sprite portMage;
            public Sprite portArcher;
            public Sprite portGoblin;
            public Sprite portSkeleton;
            public Sprite portDarkKnight;

            public TMP_FontAsset fontDisplay;
            public TMP_FontAsset fontBold;
            public TMP_FontAsset fontBody;
        }

        [MenuItem("MedievalRPG/Rebuild All Game UI")]
        public static void BuildAllUI()
        {
            EnsureTMPEssentials();
            ConfigureAllUISprites();
            MedievalRPGAssetSetup.EnsureCombatDataSetup();
            if (!File.Exists("Assets/MedievalRPG/Resources/MedievalAudioBank.asset"))
            {
                MedievalAudioGenerator.GenerateAllAudioAndBank();
            }

            UIAssets assets = LoadAndCreateAssets();

            BuildMainMenuScene(assets);
            BuildBattleScene(assets);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[MedievalUIBuilder] Hoàn tất xây dựng giao diện UI cho MainMenu & MedievalBattleDemo!");
        }

        private static void EnsureTMPEssentials()
        {
            if (!File.Exists("Assets/TextMesh Pro/Resources/TMP Settings.asset"))
            {
                Debug.Log("[MedievalUIBuilder] Đang nhập TextMeshPro Essential Resources...");
                TMP_PackageResourceImporter.ImportResources(true, false, false);
                AssetDatabase.Refresh();
            }
        }

        private static void ConfigureAllUISprites()
        {
            var borderMap = new Dictionary<string, Vector4>
            {
                { "UI_Panel_DarkGold.png", new Vector4(12, 12, 12, 12) },
                { "UI_Panel_Inset.png",    new Vector4(8, 8, 8, 8) },
                { "UI_Button_Fill.png",    new Vector4(8, 8, 8, 8) },
                { "UI_Button_Frame.png",   new Vector4(8, 8, 8, 8) },
                { "UI_Header_Ribbon.png",  new Vector4(18, 8, 18, 8) },
                { "UI_Bar_Frame.png",      new Vector4(6, 4, 6, 4) },
                { "UI_Bar_Fill.png",       new Vector4(2, 2, 2, 2) },
                { "UI_Portrait_Frame.png", new Vector4(10, 10, 10, 10) },
                { "UI_Badge_Pill.png",     new Vector4(8, 8, 8, 8) },
            };

            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { UI_SPRITE_ROOT });
            bool reimported = false;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string fileName = Path.GetFileName(path);
                if (AssetImporter.GetAtPath(path) is TextureImporter importer)
                {
                    bool dirty = false;
                    if (importer.textureType != TextureImporterType.Sprite)
                    {
                        importer.textureType = TextureImporterType.Sprite;
                        dirty = true;
                    }
                    if (importer.spriteImportMode != SpriteImportMode.Single)
                    {
                        importer.spriteImportMode = SpriteImportMode.Single;
                        dirty = true;
                    }
                    if (importer.filterMode != FilterMode.Point)
                    {
                        importer.filterMode = FilterMode.Point;
                        dirty = true;
                    }
                    if (importer.textureCompression != TextureImporterCompression.Uncompressed)
                    {
                        importer.textureCompression = TextureImporterCompression.Uncompressed;
                        dirty = true;
                    }
                    if (importer.mipmapEnabled)
                    {
                        importer.mipmapEnabled = false;
                        dirty = true;
                    }

                    if (borderMap.TryGetValue(fileName, out Vector4 border) && importer.spriteBorder != border)
                    {
                        importer.spriteBorder = border;
                        dirty = true;
                    }

                    if (dirty)
                    {
                        importer.SaveAndReimport();
                        reimported = true;
                    }
                }
            }

            if (reimported) AssetDatabase.Refresh();
        }

        private static TMP_FontAsset CreateOrLoadVietnameseFontAsset(string fontFileName, string assetFileName)
        {
            string fontPath = $"{FONT_ROOT}/{fontFileName}";
            string assetPath = $"{FONT_ROOT}/{assetFileName}";

            if (AssetImporter.GetAtPath(fontPath) is TrueTypeFontImporter fontImporter)
            {
                if (!fontImporter.includeFontData)
                {
                    fontImporter.includeFontData = true;
                    fontImporter.SaveAndReimport();
                }
            }

            Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(fontPath);
            if (sourceFont == null)
            {
                Debug.LogError($"[MedievalUIBuilder] Không tìm thấy font: {fontPath}");
                return TMP_Settings.defaultFontAsset;
            }

            TMP_FontAsset existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
            if (existing != null && existing.atlasTextures != null && existing.atlasTextures.Length > 0 && existing.atlasTextures[0] != null)
            {
                existing.TryAddCharacters(VIETNAMESE_CHARS, out _);
                EditorUtility.SetDirty(existing);
                return existing;
            }

            if (File.Exists(assetPath))
            {
                AssetDatabase.DeleteAsset(assetPath);
            }

            TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(
                sourceFont, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
            if (fontAsset == null)
            {
                Debug.LogError($"[MedievalUIBuilder] Không tạo được TMP_FontAsset từ {fontPath}");
                return TMP_Settings.defaultFontAsset;
            }

            AssetDatabase.CreateAsset(fontAsset, assetPath);

            string baseName = Path.GetFileNameWithoutExtension(assetFileName);
            if (fontAsset.atlasTextures != null)
            {
                for (int i = 0; i < fontAsset.atlasTextures.Length; i++)
                {
                    Texture2D tex = fontAsset.atlasTextures[i];
                    if (tex == null) continue;
                    tex.name = $"{baseName} Atlas {i}";
                    AssetDatabase.AddObjectToAsset(tex, fontAsset);
                }
            }

            if (fontAsset.material != null)
            {
                fontAsset.material.name = $"{baseName} Material";
                AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
            }

            var so = new SerializedObject(fontAsset);
            var clearProp = so.FindProperty("m_ClearDynamicDataOnBuild");
            if (clearProp != null) clearProp.boolValue = false;
            so.ApplyModifiedPropertiesWithoutUndo();

            // Nạp sẵn bộ chữ ASCII + Tiếng Việt có dấu vào Atlas
            bool allAdded = fontAsset.TryAddCharacters(VIETNAMESE_CHARS, out string missing);
            if (!allAdded && !string.IsNullOrEmpty(missing))
            {
                Debug.LogWarning($"[MedievalUIBuilder] Font {fontFileName} thiếu ký tự: {missing}");
            }

            EditorUtility.SetDirty(fontAsset);
            AssetDatabase.SaveAssets();
            return fontAsset;
        }

        private static UIAssets LoadAndCreateAssets()
        {
            UIAssets a = new UIAssets
            {
                panelDarkGold  = AssetDatabase.LoadAssetAtPath<Sprite>($"{UI_SPRITE_ROOT}/UI_Panel_DarkGold.png"),
                panelInset     = AssetDatabase.LoadAssetAtPath<Sprite>($"{UI_SPRITE_ROOT}/UI_Panel_Inset.png"),
                buttonFill     = AssetDatabase.LoadAssetAtPath<Sprite>($"{UI_SPRITE_ROOT}/UI_Button_Fill.png"),
                buttonFrame    = AssetDatabase.LoadAssetAtPath<Sprite>($"{UI_SPRITE_ROOT}/UI_Button_Frame.png"),
                headerRibbon   = AssetDatabase.LoadAssetAtPath<Sprite>($"{UI_SPRITE_ROOT}/UI_Header_Ribbon.png"),
                barFrame       = AssetDatabase.LoadAssetAtPath<Sprite>($"{UI_SPRITE_ROOT}/UI_Bar_Frame.png"),
                barFill        = AssetDatabase.LoadAssetAtPath<Sprite>($"{UI_SPRITE_ROOT}/UI_Bar_Fill.png"),
                portraitFrame  = AssetDatabase.LoadAssetAtPath<Sprite>($"{UI_SPRITE_ROOT}/UI_Portrait_Frame.png"),
                badgePill      = AssetDatabase.LoadAssetAtPath<Sprite>($"{UI_SPRITE_ROOT}/UI_Badge_Pill.png"),

                iconSword      = AssetDatabase.LoadAssetAtPath<Sprite>($"{UI_SPRITE_ROOT}/Icons/Icon_Sword.png"),
                iconMagic      = AssetDatabase.LoadAssetAtPath<Sprite>($"{UI_SPRITE_ROOT}/Icons/Icon_Magic.png"),
                iconArrow      = AssetDatabase.LoadAssetAtPath<Sprite>($"{UI_SPRITE_ROOT}/Icons/Icon_Arrow.png"),
                iconHeart      = AssetDatabase.LoadAssetAtPath<Sprite>($"{UI_SPRITE_ROOT}/Icons/Icon_Heart.png"),
                iconShield     = AssetDatabase.LoadAssetAtPath<Sprite>($"{UI_SPRITE_ROOT}/Icons/Icon_Shield.png"),
                iconScroll     = AssetDatabase.LoadAssetAtPath<Sprite>($"{UI_SPRITE_ROOT}/Icons/Icon_Scroll.png"),
                iconCrown      = AssetDatabase.LoadAssetAtPath<Sprite>($"{UI_SPRITE_ROOT}/Icons/Icon_Crown.png"),
                iconSkull      = AssetDatabase.LoadAssetAtPath<Sprite>($"{UI_SPRITE_ROOT}/Icons/Icon_Skull.png"),
                iconGear       = AssetDatabase.LoadAssetAtPath<Sprite>($"{UI_SPRITE_ROOT}/Icons/Icon_Gear.png"),
                iconPlay       = AssetDatabase.LoadAssetAtPath<Sprite>($"{UI_SPRITE_ROOT}/Icons/Icon_Play.png"),
                iconBook       = AssetDatabase.LoadAssetAtPath<Sprite>($"{UI_SPRITE_ROOT}/Icons/Icon_Book.png"),
                iconExit       = AssetDatabase.LoadAssetAtPath<Sprite>($"{UI_SPRITE_ROOT}/Icons/Icon_Exit.png"),
                iconMana       = AssetDatabase.LoadAssetAtPath<Sprite>($"{UI_SPRITE_ROOT}/Icons/Icon_Mana.png"),
                iconTurnArrow  = AssetDatabase.LoadAssetAtPath<Sprite>($"{UI_SPRITE_ROOT}/Icons/Icon_TurnArrow.png"),

                portKnight     = AssetDatabase.LoadAssetAtPath<Sprite>($"{UI_SPRITE_ROOT}/Portraits/Portrait_Knight.png"),
                portMage       = AssetDatabase.LoadAssetAtPath<Sprite>($"{UI_SPRITE_ROOT}/Portraits/Portrait_Mage.png"),
                portArcher     = AssetDatabase.LoadAssetAtPath<Sprite>($"{UI_SPRITE_ROOT}/Portraits/Portrait_Archer.png"),
                portGoblin     = AssetDatabase.LoadAssetAtPath<Sprite>($"{UI_SPRITE_ROOT}/Portraits/Portrait_Goblin.png"),
                portSkeleton   = AssetDatabase.LoadAssetAtPath<Sprite>($"{UI_SPRITE_ROOT}/Portraits/Portrait_Skeleton.png"),
                portDarkKnight = AssetDatabase.LoadAssetAtPath<Sprite>($"{UI_SPRITE_ROOT}/Portraits/Portrait_DarkKnight.png"),

                fontDisplay    = CreateOrLoadVietnameseFontAsset("STIXTwoText-Bold.otf", "STIXTwoText-Bold SDF.asset"),
                fontBold       = CreateOrLoadVietnameseFontAsset("Montserrat-Bold.otf", "Montserrat-Bold SDF.asset"),
                fontBody       = CreateOrLoadVietnameseFontAsset("Montserrat-SemiBold.otf", "Montserrat-SemiBold SDF.asset"),
            };

            if (TMP_Settings.instance != null && a.fontBody != null)
            {
                if (TMP_Settings.fallbackFontAssets == null)
                {
                    // TMP_Settings fallback list handled cleanly
                }
                else if (!TMP_Settings.fallbackFontAssets.Contains(a.fontBody))
                {
                    TMP_Settings.fallbackFontAssets.Add(a.fontBody);
                    EditorUtility.SetDirty(TMP_Settings.instance);
                }
            }

            return a;
        }

        // =====================================================================
        // SCENE 1: MAIN MENU
        // =====================================================================
        private static void BuildMainMenuScene(UIAssets a)
        {
            Scene scene = EditorSceneManager.OpenScene(MAIN_MENU_PATH, OpenSceneMode.Single);

            // Xóa các đối tượng UI cũ nếu chạy rebuild nhiều lần
            DestroyExistingByName("MainMenuCanvas");
            DestroyExistingByName("EventSystem");
            DestroyExistingByName("MenuBackdrop");
            DestroyExistingByName("Global Light 2D");

            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                mainCam.backgroundColor = new Color(0.08f, 0.11f, 0.15f, 1f);
                mainCam.orthographic = true;
                mainCam.orthographicSize = 3.5f;
                if (mainCam.GetComponent<UniversalAdditionalCameraData>() == null)
                {
                    mainCam.gameObject.AddComponent<UniversalAdditionalCameraData>();
                }
            }

            // Thêm Global Light 2D để các Sprite dùng URP Lit Material hiển thị sáng rõ
            GameObject lightGo = new GameObject("Global Light 2D");
            Light2D globalLight = lightGo.AddComponent<Light2D>();
            globalLight.lightType = Light2D.LightType.Global;
            globalLight.color = new Color(1f, 0.96f, 0.92f, 1f);
            globalLight.intensity = 1.05f;

            // Sửa GameObject MainMenuManager (trước đây trỏ sai GUID script)
            GameObject managerGo = GameObject.Find("MainMenuManager");
            if (managerGo == null) managerGo = new GameObject("MainMenuManager");
            RemoveMissingComponents(managerGo);
            MainMenuManager menuMgr = managerGo.GetComponent<MainMenuManager>();
            if (menuMgr == null) menuMgr = managerGo.AddComponent<MainMenuManager>();
            menuMgr.battleSceneName = "MedievalBattleDemo";

            // Dựng nền rừng sinh động + nhân vật minh họa trên Main Menu
            BuildMenuWorldBackdrop();

            EnsureEventSystem();
            Canvas canvas = CreateRootCanvas("MainMenuCanvas", mainCam);

            // Lớp phủ tối nhẹ để chữ nổi bật trên nền rừng
            UnityEngine.UI.Image scrim = CreateImage("BackdropScrim", canvas.transform, null, new Color(0.045f, 0.06f, 0.09f, 0.42f));
            StretchFull(scrim.rectTransform);
            scrim.raycastTarget = false;

            // --- Header / Title ---
            GameObject titleGroup = CreateRect("TitleGroup", canvas.transform);
            RectTransform titleRt = titleGroup.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0.5f, 1f);
            titleRt.anchorMax = new Vector2(0.5f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.anchoredPosition = new Vector2(0f, -54f);
            titleRt.sizeDelta = new Vector2(880f, 190f);

            UnityEngine.UI.Image tagPill = CreateImage("GenreRibbon", titleGroup.transform, a.headerRibbon, Color.white);
            tagPill.type = UnityEngine.UI.Image.Type.Sliced;
            tagPill.pixelsPerUnitMultiplier = 0.7f;
            tagPill.raycastTarget = false;
            RectTransform tagRt = tagPill.rectTransform;
            tagRt.anchorMin = new Vector2(0.5f, 1f);
            tagRt.anchorMax = new Vector2(0.5f, 1f);
            tagRt.pivot = new Vector2(0.5f, 1f);
            tagRt.anchoredPosition = new Vector2(0f, 0f);
            tagRt.sizeDelta = new Vector2(580f, 44f);

            TextMeshProUGUI tagTMP = CreateTMP("GenreLabel", tagPill.transform, "GAME NHẬP VAI GIÁO DỤC • CHIẾN THUẬT THEO LƯỢT",
                a.fontBold, 16f, MedievalUITheme.GoldSoft, TextAlignmentOptions.Center);
            StretchFull(tagTMP.rectTransform);

            TextMeshProUGUI mainTitle = CreateTMP("GameTitle", titleGroup.transform, "CHIẾN BINH TRI THỨC",
                a.fontDisplay, 68f, MedievalUITheme.GoldSoft, TextAlignmentOptions.Center);
            mainTitle.outlineWidth = 0.24f;
            mainTitle.outlineColor = new Color32(9, 12, 18, 255);
            RectTransform mtRt = mainTitle.rectTransform;
            mtRt.anchorMin = new Vector2(0.5f, 1f);
            mtRt.anchorMax = new Vector2(0.5f, 1f);
            mtRt.pivot = new Vector2(0.5f, 1f);
            mtRt.anchoredPosition = new Vector2(0f, -52f);
            mtRt.sizeDelta = new Vector2(880f, 88f);

            TextMeshProUGUI subTitle = CreateTMP("SubTitle", titleGroup.transform, "MEDIEVAL TURN-BASED RPG",
                a.fontBold, 21f, MedievalUITheme.IvoryText, TextAlignmentOptions.Center);
            subTitle.characterSpacing = 6f;
            RectTransform stRt = subTitle.rectTransform;
            stRt.anchorMin = new Vector2(0.5f, 1f);
            stRt.anchorMax = new Vector2(0.5f, 1f);
            stRt.pivot = new Vector2(0.5f, 1f);
            stRt.anchoredPosition = new Vector2(0f, -142f);
            stRt.sizeDelta = new Vector2(680f, 36f);

            // --- Menu Card Trung Tâm ---
            UnityEngine.UI.Image menuCard = CreateImage("MenuCommandCard", canvas.transform, a.panelDarkGold, Color.white);
            menuCard.type = UnityEngine.UI.Image.Type.Sliced;
            menuCard.pixelsPerUnitMultiplier = 0.65f;
            menuCard.raycastTarget = false;
            RectTransform mcRt = menuCard.rectTransform;
            mcRt.anchorMin = new Vector2(0.5f, 0.5f);
            mcRt.anchorMax = new Vector2(0.5f, 0.5f);
            mcRt.pivot = new Vector2(0.5f, 0.5f);
            mcRt.anchoredPosition = new Vector2(0f, -66f);
            mcRt.sizeDelta = new Vector2(470f, 444f);

            VerticalLayoutGroup vlg = menuCard.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(32, 32, 26, 26);
            vlg.spacing = 12f;
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            var btnStart = CreateStyledMenuButton("StartButton", menuCard.transform, a, a.iconPlay,
                "BẮT ĐẦU TRẬN CHIẾN", MedievalUITheme.BtnPrimaryGold, 68f, 23f);
            var btnEquip = CreateStyledMenuButton("EquipmentButton", menuCard.transform, a, a.iconCrown,
                "KHO TRANG BỊ", MedievalUITheme.BtnSkill, 58f, 20f);
            var btnGuide = CreateStyledMenuButton("InstructionsButton", menuCard.transform, a, a.iconBook,
                "HƯỚNG DẪN CÁCH CHƠI", MedievalUITheme.BtnNeutralSlate, 56f, 20f);
            var btnSettings = CreateStyledMenuButton("SettingsButton", menuCard.transform, a, a.iconGear,
                "CÀI ĐẶT", MedievalUITheme.BtnNeutralSlate, 56f, 20f);
            var btnQuit = CreateStyledMenuButton("QuitButton", menuCard.transform, a, a.iconExit,
                "THOÁT GAME", MedievalUITheme.BtnDangerSlate, 54f, 19f);

            // Footer hint
            TextMeshProUGUI footer = CreateTMP("FooterHint", canvas.transform,
                "Học mà chơi • Trả lời đúng câu hỏi tri thức để tung chiêu mạnh mẽ!",
                a.fontBody, 17f, MedievalUITheme.SlateMuted, TextAlignmentOptions.Center);
            RectTransform ftRt = footer.rectTransform;
            ftRt.anchorMin = new Vector2(0.5f, 0f);
            ftRt.anchorMax = new Vector2(0.5f, 0f);
            ftRt.pivot = new Vector2(0.5f, 0f);
            ftRt.anchoredPosition = new Vector2(0f, 24f);
            ftRt.sizeDelta = new Vector2(880f, 34f);

            // --- Modal Hướng Dẫn ---
            GameObject guideModal = CreateModalOverlay("InstructionsModal", canvas.transform, a, new Vector2(860f, 540f),
                a.iconBook, "HƯỚNG DẪN CHIẾN ĐẤU", out Transform guideContent, out UnityEngine.UI.Button closeGuideBtn, "ĐÃ HIỂU");
            BuildInstructionsContent(guideContent, a);
            guideModal.SetActive(false);

            // --- Modal Cài Đặt ---
            GameObject settingsModal = CreateModalOverlay("SettingsModal", canvas.transform, a, new Vector2(620f, 510f),
                a.iconGear, "CÀI ĐẶT HỆ THỐNG", out Transform settContent, out UnityEngine.UI.Button closeSettBtn, "LƯU & ĐÓNG");
            BuildSettingsControls(settContent, a,
                out UnityEngine.UI.Slider musicSlider, out TextMeshProUGUI musicValue,
                out UnityEngine.UI.Slider sfxSlider, out TextMeshProUGUI sfxValue,
                out UnityEngine.UI.Toggle quizToggle, out UnityEngine.UI.Toggle fastToggle);
            settingsModal.SetActive(false);

            // --- Modal Kho Trang Bị (dùng chung với màn chiến đấu) ---
            EquipmentModalUI menuEquipModal = CreateEquipmentModal(canvas.transform, a);
            menuEquipModal.gameObject.SetActive(false);

            // --- Fade Overlay ---
            CanvasGroup fadeOverlay = CreateFadeOverlay(canvas.transform);

            // Gán tham chiếu cho MainMenuManager
            menuMgr.ConfigureCanvasUI(
                canvas, fadeOverlay,
                btnStart, btnGuide, btnSettings, btnQuit,
                guideModal, closeGuideBtn,
                settingsModal, closeSettBtn,
                musicSlider, musicValue, sfxSlider, sfxValue, quizToggle, fastToggle);
            menuMgr.ConfigureEquipmentUI(btnEquip, menuEquipModal);

            EditorUtility.SetDirty(menuMgr);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void BuildMenuWorldBackdrop()
        {
            GameObject root = new GameObject("MenuBackdrop");
            MainMenuBackdrop backdrop = root.AddComponent<MainMenuBackdrop>();

            Sprite sky = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/MedievalRPG/Environment/Forest/Layer_03.png");
            Sprite mid = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/MedievalRPG/Environment/Forest/Layer_02.png");
            Sprite fg  = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/MedievalRPG/Environment/Forest/Layer_01.png");

            GameObject skyGo = CreateWorldSprite("Menu_Sky", root.transform, sky, new Vector3(0f, 0.5f, 5f), new Vector3(2.4f, 1.9f, 1f), -10);
            GameObject midGo = CreateWorldSprite("Menu_Midground", root.transform, mid, new Vector3(0f, 1.0f, 3f), Vector3.one * 1.85f, -5);
            GameObject fgGo  = CreateWorldSprite("Menu_Foreground", root.transform, fg, new Vector3(0f, 0.85f, 1f), Vector3.one * 1.75f, -1);
            TileForestSideExtensions(midGo, "_SideTile");
            TileForestSideExtensions(fgGo, "_SideTile");

            Sprite kSpr = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/MedievalRPG/Characters/Knight/Frames/Idle/Knight_Idle_0.png");
            Sprite mSpr = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/MedievalRPG/Characters/Mage/Frames/Idle/Mage_Idle_0.png");
            Sprite aSpr = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/MedievalRPG/Characters/Archer/Frames/Idle/Archer_Idle_0.png");
            Sprite gSpr = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/MedievalRPG/Characters/Enemies/Enemy_01/Frames/Idle/Enemy_Idle_0.png");

            GameObject kGo = CreateWorldSprite("Showcase_Knight", root.transform, kSpr, new Vector3(-3.65f, -1.40f, 0f), Vector3.one, 1);
            GameObject mGo = CreateWorldSprite("Showcase_Mage",   root.transform, mSpr, new Vector3(-4.85f, -1.25f, 0f), Vector3.one, 1);
            GameObject aGo = CreateWorldSprite("Showcase_Archer", root.transform, aSpr, new Vector3(-4.20f, -0.95f, 0f), Vector3.one, 0);
            GameObject gGo = CreateWorldSprite("Showcase_Goblin", root.transform, gSpr, new Vector3(4.05f, -1.35f, 0f), Vector3.one, 1);
            if (gGo != null)
            {
                var sr = gGo.GetComponent<SpriteRenderer>();
                if (sr != null) sr.flipX = true;
            }

            backdrop.Configure(
                skyGo.transform, midGo.transform, fgGo.transform,
                new[] { kGo.transform, mGo.transform, aGo.transform, gGo.transform });
        }

        private static void TileForestSideExtensions(GameObject centerObj, string suffix)
        {
            if (centerObj == null) return;
            SpriteRenderer sr = centerObj.GetComponent<SpriteRenderer>();
            if (sr == null || sr.sprite == null) return;
            float localWidth = sr.sprite.bounds.size.x;
            if (localWidth <= 0.1f) return;

            for (int dir = -1; dir <= 1; dir += 2)
            {
                string tileName = $"{centerObj.name}{suffix}_{(dir < 0 ? "L" : "R")}";
                Transform oldChild = centerObj.transform.Find(tileName);
                if (oldChild != null) UnityEngine.Object.DestroyImmediate(oldChild.gameObject);
                GameObject oldGlobal = GameObject.Find(tileName);
                if (oldGlobal != null) UnityEngine.Object.DestroyImmediate(oldGlobal);

                GameObject tile = new GameObject(tileName);
                tile.transform.SetParent(centerObj.transform, false);
                tile.transform.localPosition = new Vector3(dir * localWidth, 0f, 0f);
                tile.transform.localScale = Vector3.one;
                SpriteRenderer tileSr = tile.AddComponent<SpriteRenderer>();
                tileSr.sprite = sr.sprite;
                tileSr.sortingLayerID = sr.sortingLayerID;
                tileSr.sortingOrder = sr.sortingOrder;
                tileSr.sharedMaterial = sr.sharedMaterial;
            }
        }

        private static GameObject CreateWorldSprite(string name, Transform parent, Sprite sprite, Vector3 pos, Vector3 scale, int sortOrder)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.localScale = scale;
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = sortOrder;
            return go;
        }

        private static void BuildInstructionsContent(Transform parent, UIAssets a)
        {
            VerticalLayoutGroup vlg = parent.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 14f;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            CreateRuleRow(parent, a, a.iconSword, MedievalUITheme.BtnAttack, "1. Đội Hình 3 Anh Hùng",
                "• Hiệp Sĩ (Knight): Máu cao, giáp dày, bảo vệ đồng đội.\n• Pháp Sư (Mage): Sát thương phép & Hồi máu.\n• Cung Thủ (Archer): Tốc độ nhanh, tỉ lệ chí mạng cao.");

            CreateRuleRow(parent, a, a.iconScroll, MedievalUITheme.GoldPrimary, "2. Chiến Đấu & Tri Thức",
                "Khi chọn Tấn Công hoặc Kỹ Năng, bạn cần trả lời 1 câu hỏi trắc nghiệm.\n• ĐÚNG: Xuất chiêu + thưởng sát thương (Chuỗi >= 3 tăng +20% lực đánh!).\n• SAI: Anh hùng mất lượt hành động.");

            CreateRuleRow(parent, a, a.iconShield, MedievalUITheme.BtnDefend, "3. Bản Đồ 6 Đợt, Rớt Đồ & Phím Tắt",
                "• 6 đợt leo thang: Quái yếu → Thành đàn → Mạnh hơn → Tinh Anh → Boss. Càng đi xa tỉ lệ rớt đồ (Thường → Huyền Thoại) càng cao!\n• Bấm vào rương rơi dưới đất hoặc [E] để nhặt đồ, [I] mở Kho Trang Bị cho cả 3 Class, [1-5] hành động, [Tab] đổi mục tiêu.");
        }

        private static void CreateRuleRow(Transform parent, UIAssets a, Sprite icon, Color accent, string title, string body)
        {
            UnityEngine.UI.Image card = CreateImage("RuleCard", parent, a.panelInset, Color.white);
            card.type = UnityEngine.UI.Image.Type.Sliced;
            card.pixelsPerUnitMultiplier = 0.7f;
            card.raycastTarget = false;
            LayoutElement le = card.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 104f;

            HorizontalLayoutGroup hlg = card.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(16, 16, 12, 12);
            hlg.spacing = 16f;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;

            UnityEngine.UI.Image badge = CreateImage("IconBox", card.transform, a.badgePill, accent);
            badge.type = UnityEngine.UI.Image.Type.Sliced;
            badge.pixelsPerUnitMultiplier = 0.7f;
            badge.raycastTarget = false;
            badge.rectTransform.sizeDelta = new Vector2(56f, 56f);

            UnityEngine.UI.Image ic = CreateImage("Icon", badge.transform, icon, Color.white);
            ic.preserveAspect = true;
            ic.raycastTarget = false;
            ic.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            ic.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            ic.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            ic.rectTransform.sizeDelta = new Vector2(36f, 36f);

            GameObject txtCol = CreateRect("TextCol", card.transform);
            RectTransform txtRt = txtCol.GetComponent<RectTransform>();
            txtRt.sizeDelta = new Vector2(690f, 82f);

            TextMeshProUGUI titleTMP = CreateTMP("Title", txtCol.transform, title, a.fontBold, 19f, MedievalUITheme.GoldSoft, TextAlignmentOptions.TopLeft);
            titleTMP.rectTransform.anchorMin = new Vector2(0f, 1f);
            titleTMP.rectTransform.anchorMax = new Vector2(1f, 1f);
            titleTMP.rectTransform.pivot = new Vector2(0f, 1f);
            titleTMP.rectTransform.anchoredPosition = Vector2.zero;
            titleTMP.rectTransform.sizeDelta = new Vector2(0f, 26f);

            TextMeshProUGUI bodyTMP = CreateTMP("Body", txtCol.transform, body, a.fontBody, 15.5f, MedievalUITheme.IvoryText, TextAlignmentOptions.TopLeft);
            bodyTMP.textWrappingMode = TextWrappingModes.Normal;
            bodyTMP.rectTransform.anchorMin = new Vector2(0f, 0f);
            bodyTMP.rectTransform.anchorMax = new Vector2(1f, 1f);
            bodyTMP.rectTransform.offsetMin = Vector2.zero;
            bodyTMP.rectTransform.offsetMax = new Vector2(0f, -26f);
        }

        // =====================================================================
        // SCENE 2: MEDIEVAL BATTLE DEMO
        // =====================================================================
        private static void BuildBattleScene(UIAssets a)
        {
            Scene scene = EditorSceneManager.OpenScene(BATTLE_PATH, OpenSceneMode.Single);

            DestroyExistingByName("BattleUICanvas");
            DestroyExistingByName("EventSystem");

            BattleSystem battleSystem = UnityEngine.Object.FindAnyObjectByType<BattleSystem>();
            BattleUIManager uiMgr = UnityEngine.Object.FindAnyObjectByType<BattleUIManager>();
            if (battleSystem != null)
            {
                battleSystem.enableLegacyOnGUI = false;
                battleSystem.lootLabelFont = a.fontBold;
                WireBattleEncounterAndStations(battleSystem);
                EditorUtility.SetDirty(battleSystem);
            }
            if (uiMgr == null && battleSystem != null)
            {
                uiMgr = battleSystem.gameObject.AddComponent<BattleUIManager>();
            }

            EnsureEventSystem();
            Camera battleCam = Camera.main;
            if (battleCam != null && battleCam.GetComponent<UniversalAdditionalCameraData>() == null)
            {
                battleCam.gameObject.AddComponent<UniversalAdditionalCameraData>();
            }
            Canvas canvas = CreateRootCanvas("BattleUICanvas", battleCam);

            // --- Layer 0: World Unit HUDs & Floating Combat Text ---
            GameObject worldHudLayerGo = CreateRect("WorldHudLayer", canvas.transform);
            RectTransform worldHudRt = worldHudLayerGo.GetComponent<RectTransform>();
            StretchFull(worldHudRt);

            WorldUnitHUD[] worldHuds = new WorldUnitHUD[6];
            for (int i = 0; i < 6; i++)
            {
                worldHuds[i] = CreateWorldUnitHUD($"WorldUnitHUD_{i}", worldHudRt, a);
                worldHuds[i].gameObject.SetActive(false);
            }

            GameObject floatLayerGo = CreateRect("FloatingTextLayer", canvas.transform);
            RectTransform floatLayerRt = floatLayerGo.GetComponent<RectTransform>();
            StretchFull(floatLayerRt);
            FloatingTextManager ftManager = floatLayerGo.AddComponent<FloatingTextManager>();
            ftManager.Initialize(floatLayerRt, a.fontBold);

            // --- Layer 1: Top HUD Bar ---
            GameObject topBar = CreateRect("TopHUD", canvas.transform);
            RectTransform topRt = topBar.GetComponent<RectTransform>();
            topRt.anchorMin = new Vector2(0f, 1f);
            topRt.anchorMax = new Vector2(1f, 1f);
            topRt.pivot = new Vector2(0.5f, 1f);
            topRt.anchoredPosition = new Vector2(0f, -14f);
            topRt.sizeDelta = new Vector2(-32f, 120f);

            // Top-Left: 3 Party Cards
            GameObject partyRow = CreateRect("PartyStatusRow", topBar.transform);
            RectTransform prRt = partyRow.GetComponent<RectTransform>();
            prRt.anchorMin = new Vector2(0f, 1f);
            prRt.anchorMax = new Vector2(0f, 1f);
            prRt.pivot = new Vector2(0f, 1f);
            prRt.anchoredPosition = Vector2.zero;
            prRt.sizeDelta = new Vector2(666f, 112f);

            HorizontalLayoutGroup partyHlg = partyRow.AddComponent<HorizontalLayoutGroup>();
            partyHlg.spacing = 8f;
            partyHlg.childControlWidth = false;
            partyHlg.childControlHeight = false;
            partyHlg.childForceExpandWidth = false;
            partyHlg.childForceExpandHeight = false;

            PartyMemberCardUI[] partyCards = new PartyMemberCardUI[3];
            for (int i = 0; i < 3; i++)
            {
                partyCards[i] = CreatePartyMemberCard($"HeroCard_{i}", partyRow.transform, a);
            }

            // Top-Center: Turn Banner & Quiz Score Pill
            UnityEngine.UI.Image turnBox = CreateImage("TurnBannerCard", topBar.transform, a.headerRibbon, Color.white);
            turnBox.type = UnityEngine.UI.Image.Type.Sliced;
            turnBox.pixelsPerUnitMultiplier = 0.65f;
            turnBox.raycastTarget = false;
            RectTransform tbRt = turnBox.rectTransform;
            tbRt.anchorMin = new Vector2(0.5f, 1f);
            tbRt.anchorMax = new Vector2(0.5f, 1f);
            tbRt.pivot = new Vector2(0.5f, 1f);
            tbRt.anchoredPosition = Vector2.zero;
            tbRt.sizeDelta = new Vector2(328f, 106f);

            TextMeshProUGUI turnBannerTMP = CreateTMP("TurnBannerText", turnBox.transform, "CHUẨN BỊ CHIẾN ĐẤU",
                a.fontBold, 21f, MedievalUITheme.GoldSoft, TextAlignmentOptions.Center);
            turnBannerTMP.rectTransform.anchorMin = new Vector2(0f, 1f);
            turnBannerTMP.rectTransform.anchorMax = new Vector2(1f, 1f);
            turnBannerTMP.rectTransform.pivot = new Vector2(0.5f, 1f);
            turnBannerTMP.rectTransform.anchoredPosition = new Vector2(0f, -12f);
            turnBannerTMP.rectTransform.sizeDelta = new Vector2(-28f, 30f);

            TextMeshProUGUI turnSubTMP = CreateTMP("TurnSubText", turnBox.transform, "Hiệp 1  •  Sắp xếp đội hình",
                a.fontBody, 14.5f, MedievalUITheme.IvoryText, TextAlignmentOptions.Center);
            turnSubTMP.rectTransform.anchorMin = new Vector2(0f, 1f);
            turnSubTMP.rectTransform.anchorMax = new Vector2(1f, 1f);
            turnSubTMP.rectTransform.pivot = new Vector2(0.5f, 1f);
            turnSubTMP.rectTransform.anchoredPosition = new Vector2(0f, -43f);
            turnSubTMP.rectTransform.sizeDelta = new Vector2(-28f, 24f);

            UnityEngine.UI.Image quizStatPill = CreateImage("QuizStatsPill", turnBox.transform, a.badgePill, MedievalUITheme.DeepInset);
            quizStatPill.type = UnityEngine.UI.Image.Type.Sliced;
            quizStatPill.pixelsPerUnitMultiplier = 0.7f;
            quizStatPill.raycastTarget = false;
            RectTransform qspRt = quizStatPill.rectTransform;
            qspRt.anchorMin = new Vector2(0.5f, 0f);
            qspRt.anchorMax = new Vector2(0.5f, 0f);
            qspRt.pivot = new Vector2(0.5f, 0f);
            qspRt.anchoredPosition = new Vector2(0f, 10f);
            qspRt.sizeDelta = new Vector2(260f, 26f);

            TextMeshProUGUI quizStatsTMP = CreateTMP("QuizStatsText", quizStatPill.transform, "Đúng: 0/0  •  Chuỗi: 0",
                a.fontBold, 14f, MedievalUITheme.GoldPrimary, TextAlignmentOptions.Center);
            StretchFull(quizStatsTMP.rectTransform);

            // Dải Thứ Tự Lượt (Turn Order Strip) ngay dưới TurnBannerCard
            GameObject turnStripGo = CreateRect("TurnOrderStrip", topBar.transform);
            RectTransform tsRt = turnStripGo.GetComponent<RectTransform>();
            tsRt.anchorMin = new Vector2(0.5f, 1f);
            tsRt.anchorMax = new Vector2(0.5f, 1f);
            tsRt.pivot = new Vector2(0.5f, 1f);
            tsRt.anchoredPosition = new Vector2(0f, -110f);
            tsRt.sizeDelta = new Vector2(310f, 40f);

            HorizontalLayoutGroup tsHlg = turnStripGo.AddComponent<HorizontalLayoutGroup>();
            tsHlg.spacing = 6f;
            tsHlg.childAlignment = TextAnchor.MiddleCenter;
            tsHlg.childControlWidth = false;
            tsHlg.childControlHeight = false;

            var turnRoots = new GameObject[6];
            var turnFrames = new UnityEngine.UI.Image[6];
            var turnPorts = new UnityEngine.UI.Image[6];
            for (int i = 0; i < 6; i++)
            {
                UnityEngine.UI.Image frame = CreateImage($"TurnSlot_{i}", turnStripGo.transform, a.portraitFrame, MedievalUITheme.SlateBorder);
                frame.type = UnityEngine.UI.Image.Type.Sliced;
                frame.pixelsPerUnitMultiplier = 0.7f;
                frame.raycastTarget = false;
                frame.rectTransform.sizeDelta = i == 0 ? new Vector2(40f, 40f) : new Vector2(34f, 34f);

                UnityEngine.UI.Image port = CreateImage("Portrait", frame.transform, a.portKnight, Color.white);
                port.preserveAspect = true;
                port.raycastTarget = false;
                StretchWithPadding(port.rectTransform, 4f);

                turnRoots[i] = frame.gameObject;
                turnFrames[i] = frame;
                turnPorts[i] = port;
                frame.gameObject.SetActive(false);
            }

            // Top-Right: Settings Gear + Kho Đồ [I] + Wave/Drop Badge + Enemy Card + Multi-Enemy Tabs
            UnityEngine.UI.Button pauseOpenBtn = CreateIconSquareButton("PauseSettingsButton", topBar.transform, a, a.iconGear,
                MedievalUITheme.BtnNeutralSlate, new Vector2(48f, 46f));
            RectTransform pbRt = pauseOpenBtn.GetComponent<RectTransform>();
            pbRt.anchorMin = new Vector2(1f, 1f);
            pbRt.anchorMax = new Vector2(1f, 1f);
            pbRt.pivot = new Vector2(1f, 1f);
            pbRt.anchoredPosition = Vector2.zero;

            UnityEngine.UI.Button equipOpenBtn = CreateStyledMenuButton("OpenEquipmentButton", topBar.transform, a, null,
                "KHO ĐỒ [I] (0)", MedievalUITheme.BtnSkill, 46f, 12.5f);
            RectTransform eqbRt = equipOpenBtn.GetComponent<RectTransform>();
            eqbRt.anchorMin = new Vector2(1f, 1f);
            eqbRt.anchorMax = new Vector2(1f, 1f);
            eqbRt.pivot = new Vector2(1f, 1f);
            eqbRt.anchoredPosition = new Vector2(-54f, 0f);
            eqbRt.sizeDelta = new Vector2(118f, 46f);
            TextMeshProUGUI equipOpenLabelTMP = equipOpenBtn.GetComponentInChildren<TextMeshProUGUI>();

            UnityEngine.UI.Image wavePill = CreateImage("WaveBadgePill", topBar.transform, a.badgePill, MedievalUITheme.DeepInset);
            wavePill.type = UnityEngine.UI.Image.Type.Sliced;
            wavePill.pixelsPerUnitMultiplier = 0.7f;
            wavePill.raycastTarget = false;
            wavePill.rectTransform.anchorMin = new Vector2(1f, 1f);
            wavePill.rectTransform.anchorMax = new Vector2(1f, 1f);
            wavePill.rectTransform.pivot = new Vector2(1f, 1f);
            wavePill.rectTransform.anchoredPosition = new Vector2(0f, -54f);
            wavePill.rectTransform.sizeDelta = new Vector2(172f, 30f);
            TextMeshProUGUI waveBadgeTMP = CreateTMP("WaveBadgeText", wavePill.transform, "ĐỢT 1/6 • RỚT 35%",
                a.fontBold, 11.5f, MedievalUITheme.GoldSoft, TextAlignmentOptions.Center);
            StretchFull(waveBadgeTMP.rectTransform);

            EnemyStatusCardUI enemyCard = CreateEnemyStatusCard("EnemyStatusCard", topBar.transform, a);
            RectTransform ecRt = enemyCard.GetComponent<RectTransform>();
            ecRt.anchorMin = new Vector2(1f, 1f);
            ecRt.anchorMax = new Vector2(1f, 1f);
            ecRt.pivot = new Vector2(1f, 1f);
            ecRt.anchoredPosition = new Vector2(-182f, 0f);

            // Tab chọn đa quái khi có >= 2 kẻ địch
            GameObject enemyTabsRow = CreateRect("EnemyTabsRow", topBar.transform);
            RectTransform etRt = enemyTabsRow.GetComponent<RectTransform>();
            etRt.anchorMin = new Vector2(1f, 1f);
            etRt.anchorMax = new Vector2(1f, 1f);
            etRt.pivot = new Vector2(1f, 1f);
            etRt.anchoredPosition = new Vector2(-182f, -118f);
            etRt.sizeDelta = new Vector2(340f, 34f);

            HorizontalLayoutGroup etHlg = enemyTabsRow.AddComponent<HorizontalLayoutGroup>();
            etHlg.spacing = 6f;
            etHlg.childControlWidth = true;
            etHlg.childControlHeight = true;
            etHlg.childForceExpandWidth = true;
            etHlg.childForceExpandHeight = true;

            var enemyTabBtns = new UnityEngine.UI.Button[3];
            var enemyTabLbls = new TextMeshProUGUI[3];
            var enemyTabHpFills = new UnityEngine.UI.Image[3];
            var enemyTabBorders = new UnityEngine.UI.Image[3];
            for (int i = 0; i < 3; i++)
            {
                UnityEngine.UI.Image tabBg = CreateImage($"EnemyTab_{i}", enemyTabsRow.transform, a.panelInset, MedievalUITheme.ObsidianPanel);
                tabBg.type = UnityEngine.UI.Image.Type.Sliced;
                tabBg.pixelsPerUnitMultiplier = 0.7f;
                UnityEngine.UI.Button tabBtn = tabBg.gameObject.AddComponent<UnityEngine.UI.Button>();
                tabBtn.targetGraphic = tabBg;

                UnityEngine.UI.Image border = CreateImage("Border", tabBg.transform, a.buttonFrame, MedievalUITheme.SlateBorder);
                border.type = UnityEngine.UI.Image.Type.Sliced;
                border.pixelsPerUnitMultiplier = 0.7f;
                border.raycastTarget = false;
                StretchFull(border.rectTransform);

                UnityEngine.UI.Image hpFill = CreateImage("MiniHP", tabBg.transform, a.barFill, MedievalUITheme.HpCrimson);
                hpFill.type = UnityEngine.UI.Image.Type.Filled;
                hpFill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
                hpFill.raycastTarget = false;
                hpFill.rectTransform.anchorMin = new Vector2(0f, 0f);
                hpFill.rectTransform.anchorMax = new Vector2(1f, 0f);
                hpFill.rectTransform.pivot = new Vector2(0.5f, 0f);
                hpFill.rectTransform.offsetMin = new Vector2(4f, 3f);
                hpFill.rectTransform.offsetMax = new Vector2(-4f, 8f);

                TextMeshProUGUI lbl = CreateTMP("Label", tabBg.transform, $"Quái {i + 1}",
                    a.fontBold, 11.5f, MedievalUITheme.IvoryText, TextAlignmentOptions.Center);
                lbl.enableAutoSizing = true;
                lbl.fontSizeMin = 8.5f;
                lbl.fontSizeMax = 11.5f;
                lbl.textWrappingMode = TextWrappingModes.NoWrap;
                lbl.overflowMode = TextOverflowModes.Truncate;
                lbl.rectTransform.anchorMin = Vector2.zero;
                lbl.rectTransform.anchorMax = Vector2.one;
                lbl.rectTransform.offsetMin = new Vector2(6f, 7f);
                lbl.rectTransform.offsetMax = new Vector2(-6f, -2f);

                enemyTabBtns[i] = tabBtn;
                enemyTabLbls[i] = lbl;
                enemyTabHpFills[i] = hpFill;
                enemyTabBorders[i] = border;
                tabBg.gameObject.SetActive(false);
            }
            enemyTabsRow.SetActive(false);

            // --- Layer 2: Bottom Command Deck (Dialogue + 5 Action Buttons) ---
            UnityEngine.UI.Image bottomDeck = CreateImage("BottomCommandDeck", canvas.transform, a.panelDarkGold, Color.white);
            bottomDeck.type = UnityEngine.UI.Image.Type.Sliced;
            bottomDeck.pixelsPerUnitMultiplier = 0.65f;
            RectTransform bdRt = bottomDeck.rectTransform;
            bdRt.anchorMin = new Vector2(0f, 0f);
            bdRt.anchorMax = new Vector2(1f, 0f);
            bdRt.pivot = new Vector2(0.5f, 0f);
            bdRt.offsetMin = new Vector2(64f, 16f);
            bdRt.offsetMax = new Vector2(-64f, 228f);

            // Left: Dialogue & Active Hero Spotlight
            GameObject leftDiagCol = CreateRect("DialogueColumn", bottomDeck.transform);
            RectTransform ldRt = leftDiagCol.GetComponent<RectTransform>();
            ldRt.anchorMin = new Vector2(0f, 0f);
            ldRt.anchorMax = new Vector2(0.43f, 1f);
            ldRt.offsetMin = new Vector2(18f, 16f);
            ldRt.offsetMax = new Vector2(-8f, -16f);

            GameObject heroHeader = CreateRect("ActiveHeroHeader", leftDiagCol.transform);
            RectTransform hhRt = heroHeader.GetComponent<RectTransform>();
            hhRt.anchorMin = new Vector2(0f, 1f);
            hhRt.anchorMax = new Vector2(1f, 1f);
            hhRt.pivot = new Vector2(0.5f, 1f);
            hhRt.anchoredPosition = Vector2.zero;
            hhRt.sizeDelta = new Vector2(0f, 44f);

            UnityEngine.UI.Image heroPortFrame = CreateImage("ActiveHeroPortraitFrame", heroHeader.transform, a.portraitFrame, Color.white);
            heroPortFrame.type = UnityEngine.UI.Image.Type.Sliced;
            heroPortFrame.pixelsPerUnitMultiplier = 0.65f;
            heroPortFrame.raycastTarget = false;
            heroPortFrame.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            heroPortFrame.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            heroPortFrame.rectTransform.pivot = new Vector2(0f, 0.5f);
            heroPortFrame.rectTransform.anchoredPosition = Vector2.zero;
            heroPortFrame.rectTransform.sizeDelta = new Vector2(44f, 44f);

            UnityEngine.UI.Image activeHeroPortrait = CreateImage("ActiveHeroPortrait", heroPortFrame.transform, a.portKnight, Color.white);
            activeHeroPortrait.preserveAspect = true;
            activeHeroPortrait.raycastTarget = false;
            StretchWithPadding(activeHeroPortrait.rectTransform, 6f);

            TextMeshProUGUI activeHeroBannerTMP = CreateTMP("ActiveHeroStatsText", heroHeader.transform, "Đang chuẩn bị đội hình...",
                a.fontBold, 17.5f, MedievalUITheme.GoldSoft, TextAlignmentOptions.MidlineLeft);
            activeHeroBannerTMP.rectTransform.anchorMin = new Vector2(0f, 0f);
            activeHeroBannerTMP.rectTransform.anchorMax = new Vector2(1f, 1f);
            activeHeroBannerTMP.rectTransform.offsetMin = new Vector2(56f, 0f);
            activeHeroBannerTMP.rectTransform.offsetMax = Vector2.zero;

            UnityEngine.UI.Image diagBox = CreateImage("DialogueInsetBox", leftDiagCol.transform, a.panelInset, Color.white);
            diagBox.type = UnityEngine.UI.Image.Type.Sliced;
            diagBox.pixelsPerUnitMultiplier = 0.65f;
            diagBox.raycastTarget = false;
            RectTransform dbRt = diagBox.rectTransform;
            dbRt.anchorMin = new Vector2(0f, 0f);
            dbRt.anchorMax = new Vector2(1f, 1f);
            dbRt.offsetMin = new Vector2(0f, 30f);
            dbRt.offsetMax = new Vector2(0f, -50f);

            TextMeshProUGUI dialogueTMP = CreateTMP("DialogueText", diagBox.transform, "Sẵn sàng bước vào trận chiến!",
                a.fontBody, 20f, MedievalUITheme.IvoryText, TextAlignmentOptions.MidlineLeft);
            dialogueTMP.textWrappingMode = TextWrappingModes.Normal;
            StretchWithPadding(dialogueTMP.rectTransform, 14f);

            TextMeshProUGUI skillPreviewTMP = CreateTMP("SkillPreviewText", leftDiagCol.transform,
                "Di chuột vào nút hành động để xem chi tiết hiệu ứng & chi phí MP.",
                a.fontBody, 14.5f, MedievalUITheme.SlateMuted, TextAlignmentOptions.MidlineLeft);
            skillPreviewTMP.rectTransform.anchorMin = new Vector2(0f, 0f);
            skillPreviewTMP.rectTransform.anchorMax = new Vector2(1f, 0f);
            skillPreviewTMP.rectTransform.pivot = new Vector2(0.5f, 0f);
            skillPreviewTMP.rectTransform.anchoredPosition = Vector2.zero;
            skillPreviewTMP.rectTransform.sizeDelta = new Vector2(0f, 26f);

            // Right: 5 Action Command Cards
            GameObject rightActCol = CreateRect("ActionCommandColumn", bottomDeck.transform);
            RectTransform raRt = rightActCol.GetComponent<RectTransform>();
            raRt.anchorMin = new Vector2(0.43f, 0f);
            raRt.anchorMax = new Vector2(1f, 1f);
            raRt.offsetMin = new Vector2(8f, 16f);
            raRt.offsetMax = new Vector2(-18f, -16f);

            HorizontalLayoutGroup actHlg = rightActCol.AddComponent<HorizontalLayoutGroup>();
            actHlg.spacing = 10f;
            actHlg.childControlWidth = true;
            actHlg.childControlHeight = true;
            actHlg.childForceExpandWidth = true;
            actHlg.childForceExpandHeight = true;

            var actBtns = new UnityEngine.UI.Button[5];
            var actTitles = new TextMeshProUGUI[5];
            var actCosts = new TextMeshProUGUI[5];
            var actIcons = new UnityEngine.UI.Image[5];
            var actFeedbacks = new UIButtonFeedback[5];

            string[] defaultTitles = { "Tấn Công", "Kỹ Năng 1", "Kỹ Năng 2", "Túi Đồ", "Phòng Thủ" };
            Sprite[] defaultIcons = { a.iconSword, a.iconMagic, a.iconMagic, a.iconHeart, a.iconShield };
            Color[] defaultColors =
            {
                MedievalUITheme.BtnAttack,
                MedievalUITheme.BtnSkill,
                MedievalUITheme.BtnSkill,
                MedievalUITheme.BtnHeal,
                MedievalUITheme.BtnDefend
            };

            for (int i = 0; i < 5; i++)
            {
                CreateActionCardButton($"ActionBtn_{i}", rightActCol.transform, a, i + 1,
                    defaultTitles[i], defaultIcons[i], defaultColors[i],
                    out actBtns[i], out actTitles[i], out actCosts[i], out actIcons[i], out actFeedbacks[i]);
            }

            // Locked overlay trên thanh hành động khi không phải lượt người chơi
            UnityEngine.UI.Image lockedOverlay = CreateImage("ActionLockedOverlay", rightActCol.transform, null,
                new Color(0.043f, 0.055f, 0.082f, 0.94f));
            LayoutElement lockLe = lockedOverlay.gameObject.AddComponent<LayoutElement>();
            lockLe.ignoreLayout = true;
            StretchFull(lockedOverlay.rectTransform);

            UnityEngine.UI.Image lockPill = CreateImage("LockedPill", lockedOverlay.transform, a.badgePill, MedievalUITheme.ObsidianPanel);
            lockPill.type = UnityEngine.UI.Image.Type.Sliced;
            lockPill.pixelsPerUnitMultiplier = 0.65f;
            lockPill.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            lockPill.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            lockPill.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            lockPill.rectTransform.sizeDelta = new Vector2(460f, 52f);

            TextMeshProUGUI lockedTMP = CreateTMP("LockedStatusText", lockPill.transform, "Đang chuẩn bị trận đấu...",
                a.fontBold, 19f, MedievalUITheme.GoldSoft, TextAlignmentOptions.Center);
            StretchFull(lockedTMP.rectTransform);

            // --- Popover Túi Vật Phẩm (trên đáy phải) ---
            UnityEngine.UI.Image itemBagImg = CreateImage("ItemBagPanel", canvas.transform, a.panelDarkGold, Color.white);
            itemBagImg.type = UnityEngine.UI.Image.Type.Sliced;
            itemBagImg.pixelsPerUnitMultiplier = 0.65f;
            RectTransform bagRt = itemBagImg.rectTransform;
            bagRt.anchorMin = new Vector2(1f, 0f);
            bagRt.anchorMax = new Vector2(1f, 0f);
            bagRt.pivot = new Vector2(1f, 0f);
            bagRt.anchoredPosition = new Vector2(-72f, 236f);
            bagRt.sizeDelta = new Vector2(440f, 248f);

            TextMeshProUGUI bagTitle = CreateTMP("BagTitle", itemBagImg.transform, "TÚI VẬT PHẨM CHIẾN ĐẤU [1-3]",
                a.fontBold, 15.5f, MedievalUITheme.GoldSoft, TextAlignmentOptions.MidlineLeft);
            bagTitle.rectTransform.anchorMin = new Vector2(0f, 1f);
            bagTitle.rectTransform.anchorMax = new Vector2(1f, 1f);
            bagTitle.rectTransform.pivot = new Vector2(0.5f, 1f);
            bagTitle.rectTransform.offsetMin = new Vector2(16f, -38f);
            bagTitle.rectTransform.offsetMax = new Vector2(-84f, -8f);

            UnityEngine.UI.Button closeBagBtn = CreateStyledMenuButton("CloseBagBtn", itemBagImg.transform, a, null,
                "ĐÓNG", MedievalUITheme.BtnDangerSlate, 30f, 12.5f);
            RectTransform cbbRt = closeBagBtn.GetComponent<RectTransform>();
            cbbRt.anchorMin = new Vector2(1f, 1f);
            cbbRt.anchorMax = new Vector2(1f, 1f);
            cbbRt.pivot = new Vector2(1f, 1f);
            cbbRt.anchoredPosition = new Vector2(-12f, -9f);
            cbbRt.sizeDelta = new Vector2(68f, 28f);

            GameObject bagListGo = CreateRect("BagItemsList", itemBagImg.transform);
            RectTransform blRt = bagListGo.GetComponent<RectTransform>();
            blRt.anchorMin = Vector2.zero;
            blRt.anchorMax = Vector2.one;
            blRt.offsetMin = new Vector2(14f, 14f);
            blRt.offsetMax = new Vector2(-14f, -44f);
            VerticalLayoutGroup blVlg = bagListGo.AddComponent<VerticalLayoutGroup>();
            blVlg.spacing = 8f;
            blVlg.childControlWidth = true;
            blVlg.childControlHeight = true;
            blVlg.childForceExpandWidth = true;
            blVlg.childForceExpandHeight = true;

            var bagBtns = new UnityEngine.UI.Button[3];
            var bagTitles = new TextMeshProUGUI[3];
            var bagCounts = new TextMeshProUGUI[3];
            string[] itemNames = { "[1] Thảo Dược Máu (+55 HP)", "[2] Bình Mana (+30 MP)", "[3] Lông Phượng Hoàng (Hồi Sinh)" };
            Sprite[] itemIcons = { a.iconHeart, a.iconMana, a.iconCrown };
            Color[] itemColors = { MedievalUITheme.BtnHeal, MedievalUITheme.BtnDefend, MedievalUITheme.BtnPrimaryGold };

            for (int i = 0; i < 3; i++)
            {
                UnityEngine.UI.Image rowImg = CreateImage($"ItemRow_{i}", bagListGo.transform, a.buttonFill, itemColors[i]);
                rowImg.type = UnityEngine.UI.Image.Type.Sliced;
                rowImg.pixelsPerUnitMultiplier = 0.65f;
                UnityEngine.UI.Button rowBtn = rowImg.gameObject.AddComponent<UnityEngine.UI.Button>();
                rowBtn.targetGraphic = rowImg;
                rowBtn.transition = Selectable.Transition.None;
                UIButtonFeedback fb = rowImg.gameObject.AddComponent<UIButtonFeedback>();
                fb.SetBaseColor(itemColors[i]);

                UnityEngine.UI.Image border = CreateImage("Frame", rowImg.transform, a.buttonFrame, Color.white);
                border.type = UnityEngine.UI.Image.Type.Sliced;
                border.pixelsPerUnitMultiplier = 0.65f;
                border.raycastTarget = false;
                StretchFull(border.rectTransform);

                UnityEngine.UI.Image ic = CreateImage("Icon", rowImg.transform, itemIcons[i], Color.white);
                ic.preserveAspect = true;
                ic.raycastTarget = false;
                ic.rectTransform.anchorMin = new Vector2(0f, 0.5f);
                ic.rectTransform.anchorMax = new Vector2(0f, 0.5f);
                ic.rectTransform.pivot = new Vector2(0f, 0.5f);
                ic.rectTransform.anchoredPosition = new Vector2(12f, 0f);
                ic.rectTransform.sizeDelta = new Vector2(28f, 28f);

                TextMeshProUGUI iTitle = CreateTMP("Title", rowImg.transform, itemNames[i],
                    a.fontBold, 15f, MedievalUITheme.IvoryText, TextAlignmentOptions.MidlineLeft);
                iTitle.rectTransform.anchorMin = Vector2.zero;
                iTitle.rectTransform.anchorMax = Vector2.one;
                iTitle.rectTransform.offsetMin = new Vector2(48f, 0f);
                iTitle.rectTransform.offsetMax = new Vector2(-64f, 0f);

                TextMeshProUGUI iCount = CreateTMP("Count", rowImg.transform, "x1",
                    a.fontBold, 16f, MedievalUITheme.GoldSoft, TextAlignmentOptions.MidlineRight);
                iCount.rectTransform.anchorMin = new Vector2(1f, 0f);
                iCount.rectTransform.anchorMax = new Vector2(1f, 1f);
                iCount.rectTransform.pivot = new Vector2(1f, 0.5f);
                iCount.rectTransform.offsetMin = new Vector2(-58f, 0f);
                iCount.rectTransform.offsetMax = new Vector2(-14f, 0f);

                bagBtns[i] = rowBtn;
                bagTitles[i] = iTitle;
                bagCounts[i] = iCount;
            }
            itemBagImg.gameObject.SetActive(false);

            // --- Nút Nổi Nhặt Tất Cả Đồ Rơi Trên Chiến Trường [E] ---
            UnityEngine.UI.Button collectLootBtn = CreateStyledMenuButton("CollectAllLootButton", canvas.transform, a, a.iconCrown,
                "NHẶT TẤT CẢ [E] (1)", MedievalUITheme.BtnPrimaryGold, 48f, 15.5f);
            RectTransform clRt = collectLootBtn.GetComponent<RectTransform>();
            clRt.anchorMin = new Vector2(1f, 0f);
            clRt.anchorMax = new Vector2(1f, 0f);
            clRt.pivot = new Vector2(1f, 0f);
            clRt.anchoredPosition = new Vector2(-68f, 240f);
            clRt.sizeDelta = new Vector2(262f, 48f);
            TextMeshProUGUI collectLootLbl = collectLootBtn.GetComponentInChildren<TextMeshProUGUI>();
            collectLootBtn.gameObject.SetActive(false);

            // --- Banner Toast Khi Nhặt Được Trang Bị ---
            UnityEngine.UI.Image lootToastImg = CreateImage("LootPickupToast", canvas.transform, a.panelInset, MedievalUITheme.ObsidianPanel);
            lootToastImg.type = UnityEngine.UI.Image.Type.Sliced;
            lootToastImg.pixelsPerUnitMultiplier = 0.65f;
            lootToastImg.raycastTarget = false;
            RectTransform ltRt = lootToastImg.rectTransform;
            ltRt.anchorMin = new Vector2(0.5f, 1f);
            ltRt.anchorMax = new Vector2(0.5f, 1f);
            ltRt.pivot = new Vector2(0.5f, 1f);
            ltRt.anchoredPosition = new Vector2(0f, -176f);
            ltRt.sizeDelta = new Vector2(560f, 64f);

            UnityEngine.UI.Image lootToastBorder = CreateImage("ToastBorder", lootToastImg.transform, a.buttonFrame, MedievalUITheme.GoldSoft);
            lootToastBorder.type = UnityEngine.UI.Image.Type.Sliced;
            lootToastBorder.pixelsPerUnitMultiplier = 0.65f;
            lootToastBorder.raycastTarget = false;
            StretchFull(lootToastBorder.rectTransform);

            TextMeshProUGUI lootToastTitle = CreateTMP("ToastTitle", lootToastImg.transform, "NHẶT ĐƯỢC TRANG BỊ!",
                a.fontBold, 15.5f, MedievalUITheme.GoldSoft, TextAlignmentOptions.Center);
            lootToastTitle.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            lootToastTitle.rectTransform.anchorMax = new Vector2(1f, 1f);
            lootToastTitle.rectTransform.offsetMin = new Vector2(14f, 0f);
            lootToastTitle.rectTransform.offsetMax = new Vector2(-14f, -6f);

            TextMeshProUGUI lootToastDetail = CreateTMP("ToastDetail", lootToastImg.transform, "+15 ATK • Bấm [I] để mở Kho Đồ",
                a.fontBody, 13f, MedievalUITheme.IvoryText, TextAlignmentOptions.Center);
            lootToastDetail.rectTransform.anchorMin = new Vector2(0f, 0f);
            lootToastDetail.rectTransform.anchorMax = new Vector2(1f, 0.5f);
            lootToastDetail.rectTransform.offsetMin = new Vector2(14f, 6f);
            lootToastDetail.rectTransform.offsetMax = new Vector2(-14f, 0f);
            lootToastImg.gameObject.SetActive(false);

            // --- Layer 3: Quiz Modal ---
            QuizModalUI quizModal = CreateQuizModal(canvas.transform, a);
            quizModal.gameObject.SetActive(false);

            // --- Layer 3.5: Kho Đồ & Trang Bị Modal ---
            EquipmentModalUI battleEquipModal = CreateEquipmentModal(canvas.transform, a);
            battleEquipModal.gameObject.SetActive(false);

            // --- Layer 4: Pause & Settings Modal ---
            GameObject pauseModal = CreateModalOverlay("PauseSettingsModal", canvas.transform, a, new Vector2(620f, 595f),
                a.iconGear, "TẠM DỪNG & CÀI ĐẶT", out Transform pauseContent, out UnityEngine.UI.Button resumeBtn, "TIẾP TỤC");
            BuildSettingsControls(pauseContent, a,
                out UnityEngine.UI.Slider pMusicSlider, out TextMeshProUGUI pMusicVal,
                out UnityEngine.UI.Slider pSfxSlider, out TextMeshProUGUI pSfxVal,
                out UnityEngine.UI.Toggle pQuizToggle, out UnityEngine.UI.Toggle pFastToggle);

            // Thêm nút Chơi lại & Về Menu trong Pause
            GameObject pauseExtraRow = CreateRect("PauseExtraButtons", pauseContent);
            LayoutElement peLe = pauseExtraRow.AddComponent<LayoutElement>();
            peLe.preferredHeight = 56f;
            HorizontalLayoutGroup peHlg = pauseExtraRow.AddComponent<HorizontalLayoutGroup>();
            peHlg.spacing = 12f;
            peHlg.childControlWidth = true;
            peHlg.childControlHeight = true;
            peHlg.childForceExpandWidth = true;
            peHlg.childForceExpandHeight = true;

            var pauseRestartBtn = CreateStyledMenuButton("PauseRestartButton", pauseExtraRow.transform, a, a.iconPlay,
                "CHƠI LẠI TRẬN NÀY", MedievalUITheme.BtnNeutralSlate, 54f, 17f);
            var pauseMenuBtn = CreateStyledMenuButton("PauseMainMenuButton", pauseExtraRow.transform, a, a.iconExit,
                "VỀ MENU CHÍNH", MedievalUITheme.BtnDangerSlate, 54f, 17f);

            pauseModal.SetActive(false);

            // --- Layer 5: Victory & Game Over Modals ---
            GameObject victoryModal = CreateEndScreenModal("VictoryModal", canvas.transform, a,
                a.iconCrown, "CHIẾN THẮNG VANG DỘI!", MedievalUITheme.GoldSoft,
                out TextMeshProUGUI winSummaryTMP,
                out UnityEngine.UI.Button winRestartBtn, out UnityEngine.UI.Button winMenuBtn,
                "CHƠI LẠI TRẬN NÀY", MedievalUITheme.BtnPrimaryGold);
            victoryModal.SetActive(false);

            GameObject gameOverModal = CreateEndScreenModal("GameOverModal", canvas.transform, a,
                a.iconSkull, "THẤT BẠI!", MedievalUITheme.DangerRed,
                out TextMeshProUGUI loseSummaryTMP,
                out UnityEngine.UI.Button loseRestartBtn, out UnityEngine.UI.Button loseMenuBtn,
                "THỬ LẠI NGAY", MedievalUITheme.BtnAttack);
            gameOverModal.SetActive(false);

            // --- Layer 6: Fade Overlay ---
            CanvasGroup fadeOverlay = CreateFadeOverlay(canvas.transform);

            if (uiMgr != null)
            {
                uiMgr.battleSystem = battleSystem;
                uiMgr.actionPanel = bottomDeck.gameObject;
                uiMgr.quizPanel = quizModal.gameObject;
                uiMgr.victoryScreen = victoryModal;
                uiMgr.gameOverScreen = gameOverModal;

                uiMgr.ConfigureCanvasUI(
                    canvas, fadeOverlay,
                    turnBannerTMP, turnSubTMP, quizStatsTMP, waveBadgeTMP,
                    dialogueTMP, activeHeroBannerTMP, skillPreviewTMP,
                    activeHeroPortrait,
                    turnRoots, turnFrames, turnPorts,
                    partyCards, enemyCard,
                    enemyTabsRow, enemyTabBtns, enemyTabLbls, enemyTabHpFills, enemyTabBorders,
                    worldHuds, worldHudRt, ftManager,
                    quizModal, pauseModal,
                    pMusicSlider, pMusicVal, pSfxSlider, pSfxVal, pQuizToggle, pFastToggle, pauseOpenBtn,
                    actBtns, actTitles, actCosts, actIcons, actFeedbacks,
                    lockedOverlay.gameObject, lockedTMP,
                    itemBagImg.gameObject, bagBtns, bagTitles, bagCounts, closeBagBtn,
                    winSummaryTMP, loseSummaryTMP,
                    a.portKnight, a.portMage, a.portArcher, a.portGoblin, a.portSkeleton, a.portDarkKnight,
                    a.iconSword, a.iconMagic, a.iconArrow, a.iconHeart, a.iconShield, a.iconMana, a.iconScroll);
                uiMgr.ConfigureLootAndEquipmentUI(
                    equipOpenBtn, equipOpenLabelTMP, battleEquipModal,
                    collectLootBtn, collectLootLbl,
                    lootToastImg.gameObject, lootToastBorder, lootToastTitle, lootToastDetail);

                // Gắn persistent UnityEvents cho các nút modal (hoạt động cả khi scene vừa tải)
                WirePersistentAction(resumeBtn, uiMgr.ResumeBattle);
                WirePersistentAction(pauseRestartBtn, uiMgr.RestartBattle);
                WirePersistentAction(pauseMenuBtn, uiMgr.ReturnToMainMenu);
                WirePersistentAction(winRestartBtn, uiMgr.RestartBattle);
                WirePersistentAction(winMenuBtn, uiMgr.ReturnToMainMenu);
                WirePersistentAction(loseRestartBtn, uiMgr.RestartBattle);
                WirePersistentAction(loseMenuBtn, uiMgr.ReturnToMainMenu);

                EditorUtility.SetDirty(uiMgr);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void WirePersistentAction(UnityEngine.UI.Button btn, UnityEngine.Events.UnityAction action)
        {
            if (btn == null) return;
            UnityEditor.Events.UnityEventTools.AddPersistentListener(btn.onClick, action);
            EditorUtility.SetDirty(btn);
        }

        // =====================================================================
        // CÁC HÀM DỰNG THÀNH PHẦN UI DÙNG CHUNG
        // =====================================================================
        private static PartyMemberCardUI CreatePartyMemberCard(string name, Transform parent, UIAssets a)
        {
            UnityEngine.UI.Image cardBg = CreateImage(name, parent, a.panelDarkGold, Color.white);
            cardBg.type = UnityEngine.UI.Image.Type.Sliced;
            cardBg.pixelsPerUnitMultiplier = 0.65f;
            cardBg.raycastTarget = false;
            cardBg.rectTransform.sizeDelta = new Vector2(216f, 112f);

            // Viền phát sáng khi tới lượt
            UnityEngine.UI.Image glowBorder = CreateImage("TurnGlowBorder", cardBg.transform, a.buttonFrame, MedievalUITheme.GoldSoft);
            glowBorder.type = UnityEngine.UI.Image.Type.Sliced;
            glowBorder.pixelsPerUnitMultiplier = 0.65f;
            glowBorder.raycastTarget = false;
            StretchWithPadding(glowBorder.rectTransform, -2f);
            glowBorder.enabled = false;

            // Viền xanh lá khi được chọn làm mục tiêu hồi phục / vật phẩm
            UnityEngine.UI.Image selectedBorder = CreateImage("SelectedAllyBorder", cardBg.transform, a.buttonFrame, MedievalUITheme.SuccessGreen);
            selectedBorder.type = UnityEngine.UI.Image.Type.Sliced;
            selectedBorder.pixelsPerUnitMultiplier = 0.65f;
            selectedBorder.raycastTarget = false;
            StretchWithPadding(selectedBorder.rectTransform, -2f);
            selectedBorder.enabled = false;

            // Khung chân dung bên trái
            UnityEngine.UI.Image portFrame = CreateImage("PortraitFrame", cardBg.transform, a.portraitFrame, Color.white);
            portFrame.type = UnityEngine.UI.Image.Type.Sliced;
            portFrame.pixelsPerUnitMultiplier = 0.65f;
            portFrame.raycastTarget = false;
            portFrame.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            portFrame.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            portFrame.rectTransform.pivot = new Vector2(0f, 0.5f);
            portFrame.rectTransform.anchoredPosition = new Vector2(10f, 0f);
            portFrame.rectTransform.sizeDelta = new Vector2(64f, 64f);

            UnityEngine.UI.Image portImg = CreateImage("PortraitImage", portFrame.transform, a.portKnight, Color.white);
            portImg.preserveAspect = true;
            portImg.raycastTarget = false;
            StretchWithPadding(portImg.rectTransform, 6f);

            // Icon hệ phái nhỏ ở góc khung chân dung
            UnityEngine.UI.Image classIcon = CreateImage("ClassIcon", portFrame.transform, a.iconSword, Color.white);
            classIcon.preserveAspect = true;
            classIcon.raycastTarget = false;
            classIcon.rectTransform.anchorMin = new Vector2(1f, 0f);
            classIcon.rectTransform.anchorMax = new Vector2(1f, 0f);
            classIcon.rectTransform.pivot = new Vector2(1f, 0f);
            classIcon.rectTransform.anchoredPosition = new Vector2(2f, -2f);
            classIcon.rectTransform.sizeDelta = new Vector2(22f, 22f);

            // Lớp phủ khi gục ngã
            UnityEngine.UI.Image deadOverlay = CreateImage("DeadOverlay", portFrame.transform, null, new Color(0.05f, 0.05f, 0.08f, 0.72f));
            deadOverlay.raycastTarget = false;
            StretchWithPadding(deadOverlay.rectTransform, 4f);
            deadOverlay.gameObject.SetActive(false);

            // Tên & Cấp độ
            TextMeshProUGUI nameTMP = CreateTMP("HeroName", cardBg.transform, "Hiệp Sĩ",
                a.fontBold, 16f, MedievalUITheme.IvoryText, TextAlignmentOptions.MidlineLeft);
            nameTMP.textWrappingMode = TextWrappingModes.NoWrap;
            nameTMP.rectTransform.anchorMin = new Vector2(0f, 1f);
            nameTMP.rectTransform.anchorMax = new Vector2(1f, 1f);
            nameTMP.rectTransform.pivot = new Vector2(0f, 1f);
            nameTMP.rectTransform.offsetMin = new Vector2(82f, -34f);
            nameTMP.rectTransform.offsetMax = new Vector2(-42f, -8f);

            TextMeshProUGUI lvTMP = CreateTMP("LevelText", cardBg.transform, "Lv.1",
                a.fontBody, 13.5f, MedievalUITheme.SlateMuted, TextAlignmentOptions.MidlineRight);
            lvTMP.rectTransform.anchorMin = new Vector2(1f, 1f);
            lvTMP.rectTransform.anchorMax = new Vector2(1f, 1f);
            lvTMP.rectTransform.pivot = new Vector2(1f, 1f);
            lvTMP.rectTransform.anchoredPosition = new Vector2(-10f, -8f);
            lvTMP.rectTransform.sizeDelta = new Vector2(44f, 24f);

            // Thanh HP
            CreateStatusBar("HPBar", cardBg.transform, a, new Vector2(82f, -38f), new Vector2(-10f, -62f),
                MedievalUITheme.HpCrimson, true,
                out UnityEngine.UI.Image hpFill, out UnityEngine.UI.Image hpTrail, out TextMeshProUGUI hpVal);

            // Thanh MP
            CreateStatusBar("MPBar", cardBg.transform, a, new Vector2(82f, -67f), new Vector2(-10f, -89f),
                MedievalUITheme.MpSapphire, false,
                out UnityEngine.UI.Image mpFill, out _, out TextMeshProUGUI mpVal);

            // Badge Đang Lượt (dưới chân dung)
            UnityEngine.UI.Image turnBadge = CreateImage("ActiveTurnBadge", cardBg.transform, a.badgePill, MedievalUITheme.GoldPrimary);
            turnBadge.type = UnityEngine.UI.Image.Type.Sliced;
            turnBadge.pixelsPerUnitMultiplier = 0.7f;
            turnBadge.raycastTarget = false;
            turnBadge.rectTransform.anchorMin = new Vector2(0f, 0f);
            turnBadge.rectTransform.anchorMax = new Vector2(0f, 0f);
            turnBadge.rectTransform.pivot = new Vector2(0f, 0f);
            turnBadge.rectTransform.anchoredPosition = new Vector2(10f, 7f);
            turnBadge.rectTransform.sizeDelta = new Vector2(64f, 18f);

            TextMeshProUGUI turnBadgeTxt = CreateTMP("TurnBadgeText", turnBadge.transform, "LƯỢT",
                a.fontBold, 11f, MedievalUITheme.DarkInkText, TextAlignmentOptions.Center);
            StretchFull(turnBadgeTxt.rectTransform);
            turnBadge.gameObject.SetActive(false);

            // Badge Trạng thái (THỦ / +GIÁP / GỤC NGÃ)
            UnityEngine.UI.Image statusBadge = CreateImage("StatusBadge", cardBg.transform, a.badgePill, MedievalUITheme.BtnDefend);
            statusBadge.type = UnityEngine.UI.Image.Type.Sliced;
            statusBadge.pixelsPerUnitMultiplier = 0.7f;
            statusBadge.raycastTarget = false;
            statusBadge.rectTransform.anchorMin = new Vector2(1f, 0f);
            statusBadge.rectTransform.anchorMax = new Vector2(1f, 0f);
            statusBadge.rectTransform.pivot = new Vector2(1f, 0f);
            statusBadge.rectTransform.anchoredPosition = new Vector2(-10f, 6f);
            statusBadge.rectTransform.sizeDelta = new Vector2(96f, 19f);

            TextMeshProUGUI statusTxt = CreateTMP("StatusBadgeText", statusBadge.transform, "THỦ -50%",
                a.fontBold, 11f, MedievalUITheme.IvoryText, TextAlignmentOptions.Center);
            StretchFull(statusTxt.rectTransform);
            statusBadge.gameObject.SetActive(false);

            PartyMemberCardUI card = cardBg.gameObject.AddComponent<PartyMemberCardUI>();
            card.Configure(cardBg, glowBorder, selectedBorder, portImg, classIcon, nameTMP, lvTMP,
                statusBadge.gameObject, statusTxt, turnBadge.gameObject, deadOverlay.gameObject,
                hpFill, hpTrail, hpVal, mpFill, mpVal);

            return card;
        }

        private static EnemyStatusCardUI CreateEnemyStatusCard(string name, Transform parent, UIAssets a)
        {
            UnityEngine.UI.Image cardBg = CreateImage(name, parent, a.panelDarkGold, Color.white);
            cardBg.type = UnityEngine.UI.Image.Type.Sliced;
            cardBg.pixelsPerUnitMultiplier = 0.65f;
            cardBg.raycastTarget = false;
            cardBg.rectTransform.sizeDelta = new Vector2(340f, 112f);

            UnityEngine.UI.Image glowBorder = CreateImage("EnemyTurnGlow", cardBg.transform, a.buttonFrame, MedievalUITheme.DangerRed);
            glowBorder.type = UnityEngine.UI.Image.Type.Sliced;
            glowBorder.pixelsPerUnitMultiplier = 0.65f;
            glowBorder.raycastTarget = false;
            StretchWithPadding(glowBorder.rectTransform, -2f);
            glowBorder.enabled = false;

            UnityEngine.UI.Image portFrame = CreateImage("PortraitFrame", cardBg.transform, a.portraitFrame, Color.white);
            portFrame.type = UnityEngine.UI.Image.Type.Sliced;
            portFrame.pixelsPerUnitMultiplier = 0.65f;
            portFrame.raycastTarget = false;
            portFrame.rectTransform.anchorMin = new Vector2(1f, 0.5f);
            portFrame.rectTransform.anchorMax = new Vector2(1f, 0.5f);
            portFrame.rectTransform.pivot = new Vector2(1f, 0.5f);
            portFrame.rectTransform.anchoredPosition = new Vector2(-12f, 0f);
            portFrame.rectTransform.sizeDelta = new Vector2(72f, 72f);

            UnityEngine.UI.Image portImg = CreateImage("PortraitImage", portFrame.transform, a.portGoblin, Color.white);
            portImg.preserveAspect = true;
            portImg.raycastTarget = false;
            StretchWithPadding(portImg.rectTransform, 6f);

            TextMeshProUGUI nameTMP = CreateTMP("EnemyName", cardBg.transform, "Goblin Raider",
                a.fontBold, 20f, new Color(1f, 0.58f, 0.58f, 1f), TextAlignmentOptions.MidlineLeft);
            nameTMP.enableAutoSizing = true;
            nameTMP.fontSizeMin = 13.5f;
            nameTMP.fontSizeMax = 20f;
            nameTMP.textWrappingMode = TextWrappingModes.NoWrap;
            nameTMP.overflowMode = TextOverflowModes.Truncate;
            nameTMP.rectTransform.anchorMin = new Vector2(0f, 1f);
            nameTMP.rectTransform.anchorMax = new Vector2(1f, 1f);
            nameTMP.rectTransform.pivot = new Vector2(0f, 1f);
            nameTMP.rectTransform.offsetMin = new Vector2(14f, -38f);
            nameTMP.rectTransform.offsetMax = new Vector2(-94f, -10f);

            CreateStatusBar("EnemyHPBar", cardBg.transform, a, new Vector2(14f, -44f), new Vector2(-94f, -74f),
                MedievalUITheme.HpCrimson, true,
                out UnityEngine.UI.Image hpFill, out UnityEngine.UI.Image hpTrail, out TextMeshProUGUI hpVal);
            hpVal.fontSize = 14.5f;

            TextMeshProUGUI statsTMP = CreateTMP("EnemyStats", cardBg.transform, "Cấp 1  •  ATK 14  •  DEF 8",
                a.fontBody, 13.5f, MedievalUITheme.SlateMuted, TextAlignmentOptions.MidlineLeft);
            statsTMP.rectTransform.anchorMin = new Vector2(0f, 0f);
            statsTMP.rectTransform.anchorMax = new Vector2(1f, 0f);
            statsTMP.rectTransform.pivot = new Vector2(0f, 0f);
            statsTMP.rectTransform.offsetMin = new Vector2(14f, 10f);
            statsTMP.rectTransform.offsetMax = new Vector2(-94f, 34f);

            UnityEngine.UI.Image deadBadge = CreateImage("DefeatedBadge", cardBg.transform, a.badgePill, MedievalUITheme.DangerPanel);
            deadBadge.type = UnityEngine.UI.Image.Type.Sliced;
            deadBadge.pixelsPerUnitMultiplier = 0.7f;
            deadBadge.raycastTarget = false;
            deadBadge.rectTransform.anchorMin = new Vector2(1f, 0f);
            deadBadge.rectTransform.anchorMax = new Vector2(1f, 0f);
            deadBadge.rectTransform.pivot = new Vector2(1f, 0f);
            deadBadge.rectTransform.anchoredPosition = new Vector2(-12f, 8f);
            deadBadge.rectTransform.sizeDelta = new Vector2(74f, 20f);
            TextMeshProUGUI dbTxt = CreateTMP("DeadTxt", deadBadge.transform, "HẠ GỤC",
                a.fontBold, 11f, MedievalUITheme.IvoryText, TextAlignmentOptions.Center);
            StretchFull(dbTxt.rectTransform);
            deadBadge.gameObject.SetActive(false);

            UnityEngine.UI.Image statusPill = CreateImage("EnemyStatusPill", cardBg.transform, a.badgePill, MedievalUITheme.BtnSkill);
            statusPill.type = UnityEngine.UI.Image.Type.Sliced;
            statusPill.pixelsPerUnitMultiplier = 0.7f;
            statusPill.raycastTarget = false;
            statusPill.rectTransform.anchorMin = new Vector2(0f, 0f);
            statusPill.rectTransform.anchorMax = new Vector2(0f, 0f);
            statusPill.rectTransform.pivot = new Vector2(0f, 0f);
            statusPill.rectTransform.anchoredPosition = new Vector2(14f, -14f);
            statusPill.rectTransform.sizeDelta = new Vector2(160f, 20f);
            TextMeshProUGUI statusTxt = CreateTMP("StatusTxt", statusPill.transform, "KHIÊN: 2",
                a.fontBold, 11f, MedievalUITheme.IvoryText, TextAlignmentOptions.Center);
            StretchFull(statusTxt.rectTransform);
            statusPill.gameObject.SetActive(false);

            EnemyStatusCardUI card = cardBg.gameObject.AddComponent<EnemyStatusCardUI>();
            card.Configure(glowBorder, portImg, nameTMP, statsTMP, hpFill, hpTrail, hpVal, deadBadge.gameObject,
                statusPill.gameObject, statusTxt);
            return card;
        }

        private static WorldUnitHUD CreateWorldUnitHUD(string name, Transform parent, UIAssets a)
        {
            GameObject root = CreateRect(name, parent);
            RectTransform rt = root.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(140f, 76f);
            CanvasGroup cg = root.AddComponent<CanvasGroup>();
            cg.interactable = false;
            cg.blocksRaycasts = false;

            // Vòng chọn mục tiêu khi người chơi nhắm quái
            UnityEngine.UI.Image reticle = CreateImage("TargetReticle", root.transform, a.buttonFrame, MedievalUITheme.GoldSoft);
            reticle.type = UnityEngine.UI.Image.Type.Sliced;
            reticle.pixelsPerUnitMultiplier = 0.7f;
            reticle.raycastTarget = false;
            reticle.rectTransform.anchorMin = new Vector2(0.5f, 0f);
            reticle.rectTransform.anchorMax = new Vector2(0.5f, 0f);
            reticle.rectTransform.pivot = new Vector2(0.5f, 0f);
            reticle.rectTransform.anchoredPosition = new Vector2(0f, 8f);
            reticle.rectTransform.sizeDelta = new Vector2(106f, 22f);
            reticle.gameObject.SetActive(false);

            // Pill dự báo hành động quái (Enemy Intent) hoặc trạng thái
            UnityEngine.UI.Image intentPill = CreateImage("IntentPill", root.transform, a.badgePill, MedievalUITheme.DangerPanel);
            intentPill.type = UnityEngine.UI.Image.Type.Sliced;
            intentPill.pixelsPerUnitMultiplier = 0.7f;
            intentPill.raycastTarget = false;
            intentPill.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            intentPill.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            intentPill.rectTransform.pivot = new Vector2(0.5f, 0f);
            intentPill.rectTransform.anchoredPosition = new Vector2(0f, 2f);
            intentPill.rectTransform.sizeDelta = new Vector2(126f, 20f);
            TextMeshProUGUI intentLabel = CreateTMP("IntentText", intentPill.transform, "ĐÁNH ~14",
                a.fontBold, 11f, MedievalUITheme.IvoryText, TextAlignmentOptions.Center);
            StretchFull(intentLabel.rectTransform);
            intentPill.gameObject.SetActive(false);

            // Mũi tên chỉ lượt phía trên đầu
            UnityEngine.UI.Image arrow = CreateImage("TurnArrow", root.transform, a.iconTurnArrow, Color.white);
            arrow.preserveAspect = true;
            arrow.raycastTarget = false;
            arrow.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            arrow.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            arrow.rectTransform.pivot = new Vector2(0.5f, 0f);
            arrow.rectTransform.anchoredPosition = new Vector2(0f, 24f);
            arrow.rectTransform.sizeDelta = new Vector2(24f, 24f);

            TextMeshProUGUI nameTxt = CreateTMP("UnitName", root.transform, "Hero",
                a.fontBold, 14f, MedievalUITheme.IvoryText, TextAlignmentOptions.Center);
            nameTxt.outlineWidth = 0.25f;
            nameTxt.outlineColor = new Color32(9, 12, 18, 255);
            nameTxt.rectTransform.anchorMin = new Vector2(0f, 1f);
            nameTxt.rectTransform.anchorMax = new Vector2(1f, 1f);
            nameTxt.rectTransform.pivot = new Vector2(0.5f, 1f);
            nameTxt.rectTransform.anchoredPosition = new Vector2(0f, -22f);
            nameTxt.rectTransform.sizeDelta = new Vector2(0f, 20f);

            // Thanh HP mini
            UnityEngine.UI.Image barBg = CreateImage("MiniBarFrame", root.transform, a.barFrame, Color.white);
            barBg.type = UnityEngine.UI.Image.Type.Sliced;
            barBg.pixelsPerUnitMultiplier = 0.7f;
            barBg.raycastTarget = false;
            barBg.rectTransform.anchorMin = new Vector2(0.5f, 0f);
            barBg.rectTransform.anchorMax = new Vector2(0.5f, 0f);
            barBg.rectTransform.pivot = new Vector2(0.5f, 0f);
            barBg.rectTransform.anchoredPosition = new Vector2(0f, 12f);
            barBg.rectTransform.sizeDelta = new Vector2(96f, 14f);

            UnityEngine.UI.Image hpFill = CreateImage("MiniHPFill", barBg.transform, a.barFill, MedievalUITheme.SuccessGreen);
            hpFill.type = UnityEngine.UI.Image.Type.Filled;
            hpFill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
            hpFill.raycastTarget = false;
            StretchWithPadding(hpFill.rectTransform, 3f);

            UnityEngine.UI.Image shieldBadge = CreateImage("DefendIcon", root.transform, a.iconShield, Color.white);
            shieldBadge.preserveAspect = true;
            shieldBadge.raycastTarget = false;
            shieldBadge.rectTransform.anchorMin = new Vector2(1f, 0f);
            shieldBadge.rectTransform.anchorMax = new Vector2(1f, 0f);
            shieldBadge.rectTransform.pivot = new Vector2(0f, 0.5f);
            shieldBadge.rectTransform.anchoredPosition = new Vector2(-14f, 19f);
            shieldBadge.rectTransform.sizeDelta = new Vector2(20f, 20f);
            shieldBadge.gameObject.SetActive(false);

            WorldUnitHUD hud = root.AddComponent<WorldUnitHUD>();
            hud.Configure(hpFill, nameTxt, arrow.gameObject, shieldBadge.gameObject, cg,
                reticle.gameObject, intentPill.gameObject, intentLabel, intentPill);
            return hud;
        }

        private static void WireBattleEncounterAndStations(BattleSystem bs)
        {
            if (bs == null) return;

            bs.knightPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/MedievalRPG/Prefabs/Knight.prefab");
            bs.magePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/MedievalRPG/Prefabs/Mage.prefab");
            bs.archerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/MedievalRPG/Prefabs/Archer.prefab");
            bs.enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/MedievalRPG/Prefabs/GoblinRaider.prefab");
            bs.skeletonPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/MedievalRPG/Prefabs/Skeleton.prefab");
            bs.bossPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/MedievalRPG/Prefabs/DarkKnightBoss.prefab");

            GameObject skyBg = GameObject.Find("Forest_Background_Sky");
            if (skyBg != null)
            {
                skyBg.transform.position = new Vector3(0f, 0.6f, 5f);
                skyBg.transform.localScale = new Vector3(2.4f, 1.9f, 1f);
            }
            GameObject midBg = GameObject.Find("Forest_Background_Midground");
            if (midBg != null)
            {
                midBg.transform.position = new Vector3(0f, 1.05f, 3f);
                midBg.transform.localScale = new Vector3(1.8f, 1.8f, 1f);
                TileForestSideExtensions(midBg, "_BattleTile");
            }
            GameObject fgBg = GameObject.Find("Forest_Foreground_Trees");
            if (fgBg != null)
            {
                fgBg.transform.position = new Vector3(0f, 0.90f, 1f);
                fgBg.transform.localScale = new Vector3(1.7f, 1.7f, 1f);
                TileForestSideExtensions(fgBg, "_BattleTile");
            }

            if (bs.knightBattleStation != null) bs.knightBattleStation.position = new Vector3(-1.45f, -1.20f, 0f);
            if (bs.mageBattleStation != null) bs.mageBattleStation.position = new Vector3(-3.05f, -1.00f, 0f);
            if (bs.archerBattleStation != null) bs.archerBattleStation.position = new Vector3(-4.15f, -1.40f, 0f);

            Transform s0 = bs.enemyBattleStation != null ? bs.enemyBattleStation : GameObject.Find("EnemyStation")?.transform;
            if (s0 == null) s0 = new GameObject("EnemyStation_0").transform;
            s0.position = new Vector3(1.65f, -1.26f, 0f);
            bs.enemyBattleStation = s0;

            Transform s1 = GameObject.Find("EnemyStation_1")?.transform ?? new GameObject("EnemyStation_1").transform;
            s1.position = new Vector3(3.15f, -0.78f, 0f);
            Transform s2 = GameObject.Find("EnemyStation_2")?.transform ?? new GameObject("EnemyStation_2").transform;
            s2.position = new Vector3(4.15f, -1.45f, 0f);

            bs.enemyBattleStations = new List<Transform> { s0, s1, s2 };

            bs.encounterData = AssetDatabase.LoadAssetAtPath<MedievalRPG.Data.EncounterData>(
                "Assets/MedievalRPG/ScriptableObjects/Encounters/ForestDemoEncounter.asset");

            bs.defaultWaves = new List<MedievalRPG.Data.BattleWave>
            {
                new MedievalRPG.Data.BattleWave
                {
                    waveTitle = "Đợt 1: Trinh Sát Rừng Sâu",
                    waveSubtitle = "Quái yếu đơn lẻ • Khởi động",
                    isBossWave = false,
                    enemyStatMultiplier = 0.80f,
                    enemyPrefabs = new List<GameObject> { bs.enemyPrefab }
                },
                new MedievalRPG.Data.BattleWave
                {
                    waveTitle = "Đợt 2: Đàn Goblin Phục Kích",
                    waveSubtitle = "Thành đàn 3 Goblin • Ưu tiên chiêu diện rộng",
                    isBossWave = false,
                    enemyStatMultiplier = 0.90f,
                    dropChanceBonus = 0.02f,
                    rarityLuckBonus = 0.03f,
                    enemyPrefabs = new List<GameObject> { bs.enemyPrefab, bs.enemyPrefab, bs.enemyPrefab }
                },
                new MedievalRPG.Data.BattleWave
                {
                    waveTitle = "Đợt 3: Vệ Binh Xương Khô",
                    waveSubtitle = "Quái mạnh hơn • Giáp cứng",
                    isBossWave = false,
                    enemyStatMultiplier = 1.00f,
                    dropChanceBonus = 0.05f,
                    rarityLuckBonus = 0.06f,
                    enemyPrefabs = new List<GameObject> { bs.enemyPrefab, bs.skeletonPrefab }
                },
                new MedievalRPG.Data.BattleWave
                {
                    waveTitle = "Đợt 4: Tiểu Đội Hầm Ngục",
                    waveSubtitle = "Đàn quái hạng nặng (3 con)",
                    isBossWave = false,
                    enemyStatMultiplier = 1.08f,
                    dropChanceBonus = 0.08f,
                    rarityLuckBonus = 0.10f,
                    enemyPrefabs = new List<GameObject> { bs.skeletonPrefab, bs.enemyPrefab, bs.skeletonPrefab }
                },
                new MedievalRPG.Data.BattleWave
                {
                    waveTitle = "Đợt 5: Cận Vệ Tinh Anh",
                    waveSubtitle = "Tinh Anh áp sát Boss • Rớt đồ Sử Thi cao",
                    isBossWave = false,
                    enemyStatMultiplier = 1.25f,
                    dropChanceBonus = 0.12f,
                    rarityLuckBonus = 0.16f,
                    enemyPrefabs = new List<GameObject> { bs.skeletonPrefab, bs.skeletonPrefab }
                },
                new MedievalRPG.Data.BattleWave
                {
                    waveTitle = "Đợt 6: Thủ Lĩnh Bóng Đêm",
                    waveSubtitle = "BOSS CUỐI MAP • Chắc chắn rớt 2 trang bị cao cấp",
                    isBossWave = true,
                    enemyStatMultiplier = 1.05f,
                    rarityLuckBonus = 0.15f,
                    enemyPrefabs = new List<GameObject> { bs.bossPrefab }
                }
            };
        }

        // =====================================================================
        // MODAL KHO ĐỒ & TRANG BỊ (DÙNG CHUNG CHO MAIN MENU & BATTLE SCENE)
        // =====================================================================
        private static EquipmentModalUI CreateEquipmentModal(Transform canvasParent, UIAssets a)
        {
            UnityEngine.UI.Image scrim = CreateImage("EquipmentModalOverlay", canvasParent, null, MedievalUITheme.ScrimDark);
            StretchFull(scrim.rectTransform);

            UnityEngine.UI.Image card = CreateImage("EquipmentCard", scrim.transform, a.panelDarkGold, Color.white);
            card.type = UnityEngine.UI.Image.Type.Sliced;
            card.pixelsPerUnitMultiplier = 0.65f;
            RectTransform cRt = card.rectTransform;
            cRt.anchorMin = new Vector2(0.5f, 0.5f);
            cRt.anchorMax = new Vector2(0.5f, 0.5f);
            cRt.pivot = new Vector2(0.5f, 0.5f);
            cRt.sizeDelta = new Vector2(1140f, 650f);

            // Header bar
            UnityEngine.UI.Image headerIcon = CreateImage("HeaderIcon", card.transform, a.iconCrown, Color.white);
            headerIcon.preserveAspect = true;
            headerIcon.raycastTarget = false;
            headerIcon.rectTransform.anchorMin = new Vector2(0f, 1f);
            headerIcon.rectTransform.anchorMax = new Vector2(0f, 1f);
            headerIcon.rectTransform.pivot = new Vector2(0f, 1f);
            headerIcon.rectTransform.anchoredPosition = new Vector2(24f, -18f);
            headerIcon.rectTransform.sizeDelta = new Vector2(36f, 36f);

            TextMeshProUGUI headerTitle = CreateTMP("HeaderTitle", card.transform, "KHO TRANG BỊ • 3 CLASS • 4 CẤP HIẾM",
                a.fontDisplay, 25f, MedievalUITheme.GoldSoft, TextAlignmentOptions.MidlineLeft);
            headerTitle.rectTransform.anchorMin = new Vector2(0f, 1f);
            headerTitle.rectTransform.anchorMax = new Vector2(0.75f, 1f);
            headerTitle.rectTransform.pivot = new Vector2(0f, 1f);
            headerTitle.rectTransform.offsetMin = new Vector2(70f, -56f);
            headerTitle.rectTransform.offsetMax = new Vector2(0f, -14f);

            UnityEngine.UI.Button closeBtn = CreateStyledMenuButton("CloseEquipModalBtn", card.transform, a, null,
                "ĐÓNG [I]", MedievalUITheme.BtnDangerSlate, 38f, 14f);
            RectTransform clRt = closeBtn.GetComponent<RectTransform>();
            clRt.anchorMin = new Vector2(1f, 1f);
            clRt.anchorMax = new Vector2(1f, 1f);
            clRt.pivot = new Vector2(1f, 1f);
            clRt.anchoredPosition = new Vector2(-22f, -18f);
            clRt.sizeDelta = new Vector2(116f, 38f);

            // --- CỘT TRÁI: CHỌN CLASS + 3 Ô ĐANG MẶC + TỔNG CHỈ SỐ ---
            UnityEngine.UI.Image leftCol = CreateImage("LeftLoadoutColumn", card.transform, a.panelInset, MedievalUITheme.DeepInset);
            leftCol.type = UnityEngine.UI.Image.Type.Sliced;
            leftCol.pixelsPerUnitMultiplier = 0.65f;
            RectTransform lRt = leftCol.rectTransform;
            lRt.anchorMin = new Vector2(0f, 0f);
            lRt.anchorMax = new Vector2(0.43f, 1f);
            lRt.offsetMin = new Vector2(22f, 22f);
            lRt.offsetMax = new Vector2(-8f, -68f);

            // 3 Class Tabs
            GameObject clsTabRow = CreateRect("ClassTabsRow", leftCol.transform);
            RectTransform ctRt = clsTabRow.GetComponent<RectTransform>();
            ctRt.anchorMin = new Vector2(0f, 1f);
            ctRt.anchorMax = new Vector2(1f, 1f);
            ctRt.pivot = new Vector2(0.5f, 1f);
            ctRt.offsetMin = new Vector2(14f, -58f);
            ctRt.offsetMax = new Vector2(-14f, -14f);
            HorizontalLayoutGroup ctHlg = clsTabRow.AddComponent<HorizontalLayoutGroup>();
            ctHlg.spacing = 8f;
            ctHlg.childControlWidth = true;
            ctHlg.childControlHeight = true;
            ctHlg.childForceExpandWidth = true;
            ctHlg.childForceExpandHeight = true;

            string[] clsNames = { "HIỆP SĨ", "PHÁP SƯ", "CUNG THỦ" };
            var clsTabs = new UnityEngine.UI.Button[3];
            var clsTabBgs = new UnityEngine.UI.Image[3];
            for (int i = 0; i < 3; i++)
            {
                UnityEngine.UI.Image bg = CreateImage($"ClassTab_{i}", clsTabRow.transform, a.buttonFill,
                    i == 0 ? MedievalUITheme.BtnPrimaryGold : MedievalUITheme.BtnNeutralSlate);
                bg.type = UnityEngine.UI.Image.Type.Sliced;
                bg.pixelsPerUnitMultiplier = 0.65f;
                UnityEngine.UI.Button b = bg.gameObject.AddComponent<UnityEngine.UI.Button>();
                b.targetGraphic = bg;
                TextMeshProUGUI lbl = CreateTMP("Label", bg.transform, clsNames[i], a.fontBold, 14f, MedievalUITheme.IvoryText, TextAlignmentOptions.Center);
                StretchFull(lbl.rectTransform);
                clsTabs[i] = b;
                clsTabBgs[i] = bg;
            }

            TextMeshProUGUI clsHeaderTMP = CreateTMP("SelectedClassHeader", leftCol.transform, "TRANG BỊ CỦA HIỆP SĨ",
                a.fontBold, 15.5f, MedievalUITheme.GoldSoft, TextAlignmentOptions.MidlineLeft);
            clsHeaderTMP.rectTransform.anchorMin = new Vector2(0f, 1f);
            clsHeaderTMP.rectTransform.anchorMax = new Vector2(1f, 1f);
            clsHeaderTMP.rectTransform.pivot = new Vector2(0.5f, 1f);
            clsHeaderTMP.rectTransform.offsetMin = new Vector2(16f, -92f);
            clsHeaderTMP.rectTransform.offsetMax = new Vector2(-16f, -66f);

            // 3 Ô Trang bị (Weapon, Armor, Accessory)
            GameObject slotsBox = CreateRect("EquippedSlotsBox", leftCol.transform);
            RectTransform sbRt = slotsBox.GetComponent<RectTransform>();
            sbRt.anchorMin = new Vector2(0f, 1f);
            sbRt.anchorMax = new Vector2(1f, 1f);
            sbRt.pivot = new Vector2(0.5f, 1f);
            sbRt.offsetMin = new Vector2(14f, -384f);
            sbRt.offsetMax = new Vector2(-14f, -98f);
            VerticalLayoutGroup sbVlg = slotsBox.AddComponent<VerticalLayoutGroup>();
            sbVlg.spacing = 10f;
            sbVlg.childControlWidth = true;
            sbVlg.childControlHeight = true;
            sbVlg.childForceExpandWidth = true;
            sbVlg.childForceExpandHeight = true;

            var sBorders = new UnityEngine.UI.Image[3];
            var sIcons = new UnityEngine.UI.Image[3];
            var sNames = new TextMeshProUGUI[3];
            var sStats = new TextMeshProUGUI[3];
            var sUnequips = new UnityEngine.UI.Button[3];
            string[] slotDefaultLabels = { "Vũ Khí: Trống", "Giáp: Trống", "Phụ Kiện: Trống" };
            Sprite[] slotDefaultIcons = { a.iconSword, a.iconShield, a.iconCrown };

            for (int i = 0; i < 3; i++)
            {
                UnityEngine.UI.Image slotCard = CreateImage($"EquipSlotCard_{i}", slotsBox.transform, a.panelInset, MedievalUITheme.ObsidianPanel);
                slotCard.type = UnityEngine.UI.Image.Type.Sliced;
                slotCard.pixelsPerUnitMultiplier = 0.65f;

                UnityEngine.UI.Image border = CreateImage("Border", slotCard.transform, a.buttonFrame, MedievalUITheme.SlateBorder);
                border.type = UnityEngine.UI.Image.Type.Sliced;
                border.pixelsPerUnitMultiplier = 0.65f;
                border.raycastTarget = false;
                StretchFull(border.rectTransform);

                UnityEngine.UI.Image iconBox = CreateImage("IconBox", slotCard.transform, a.badgePill, MedievalUITheme.DeepInset);
                iconBox.type = UnityEngine.UI.Image.Type.Sliced;
                iconBox.raycastTarget = false;
                iconBox.rectTransform.anchorMin = new Vector2(0f, 0.5f);
                iconBox.rectTransform.anchorMax = new Vector2(0f, 0.5f);
                iconBox.rectTransform.pivot = new Vector2(0f, 0.5f);
                iconBox.rectTransform.anchoredPosition = new Vector2(12f, 0f);
                iconBox.rectTransform.sizeDelta = new Vector2(52f, 52f);

                UnityEngine.UI.Image ic = CreateImage("Icon", iconBox.transform, slotDefaultIcons[i], Color.white);
                ic.preserveAspect = true;
                ic.raycastTarget = false;
                StretchWithPadding(ic.rectTransform, 8f);

                TextMeshProUGUI nameTxt = CreateTMP("ItemName", slotCard.transform, slotDefaultLabels[i],
                    a.fontBold, 14.5f, MedievalUITheme.SlateMuted, TextAlignmentOptions.MidlineLeft);
                nameTxt.enableAutoSizing = true;
                nameTxt.fontSizeMin = 11f;
                nameTxt.fontSizeMax = 14.5f;
                nameTxt.overflowMode = TextOverflowModes.Truncate;
                nameTxt.textWrappingMode = TextWrappingModes.NoWrap;
                nameTxt.rectTransform.anchorMin = new Vector2(0f, 0.5f);
                nameTxt.rectTransform.anchorMax = new Vector2(1f, 1f);
                nameTxt.rectTransform.offsetMin = new Vector2(74f, 2f);
                nameTxt.rectTransform.offsetMax = new Vector2(-74f, -8f);

                TextMeshProUGUI statTxt = CreateTMP("ItemStats", slotCard.transform, "Nhặt đồ trên bản đồ để trang bị",
                    a.fontBody, 12.5f, MedievalUITheme.IvoryText, TextAlignmentOptions.MidlineLeft);
                statTxt.enableAutoSizing = true;
                statTxt.fontSizeMin = 10f;
                statTxt.fontSizeMax = 12.5f;
                statTxt.overflowMode = TextOverflowModes.Truncate;
                statTxt.textWrappingMode = TextWrappingModes.NoWrap;
                statTxt.rectTransform.anchorMin = new Vector2(0f, 0f);
                statTxt.rectTransform.anchorMax = new Vector2(1f, 0.5f);
                statTxt.rectTransform.offsetMin = new Vector2(74f, 8f);
                statTxt.rectTransform.offsetMax = new Vector2(-74f, -2f);

                UnityEngine.UI.Button uneqBtn = CreateStyledMenuButton($"UnequipBtn_{i}", slotCard.transform, a, null,
                    "GỠ", MedievalUITheme.BtnDangerSlate, 34f, 12f);
                RectTransform uRt = uneqBtn.GetComponent<RectTransform>();
                uRt.anchorMin = new Vector2(1f, 0.5f);
                uRt.anchorMax = new Vector2(1f, 0.5f);
                uRt.pivot = new Vector2(1f, 0.5f);
                uRt.anchoredPosition = new Vector2(-10f, 0f);
                uRt.sizeDelta = new Vector2(56f, 34f);

                sBorders[i] = border;
                sIcons[i] = ic;
                sNames[i] = nameTxt;
                sStats[i] = statTxt;
                sUnequips[i] = uneqBtn;
            }

            // Khung Tổng Chỉ Số Cộng Thêm
            UnityEngine.UI.Image bonusBox = CreateImage("BonusSummaryBox", leftCol.transform, a.panelInset, MedievalUITheme.ObsidianPanel);
            bonusBox.type = UnityEngine.UI.Image.Type.Sliced;
            bonusBox.pixelsPerUnitMultiplier = 0.65f;
            bonusBox.raycastTarget = false;
            bonusBox.rectTransform.anchorMin = new Vector2(0f, 0f);
            bonusBox.rectTransform.anchorMax = new Vector2(1f, 0f);
            bonusBox.rectTransform.pivot = new Vector2(0.5f, 0f);
            bonusBox.rectTransform.offsetMin = new Vector2(14f, 76f);
            bonusBox.rectTransform.offsetMax = new Vector2(-14f, 152f);

            TextMeshProUGUI bonusTMP = CreateTMP("TotalBonusText", bonusBox.transform, "Tổng cộng thêm: Chưa mặc trang bị nào",
                a.fontBody, 13.5f, MedievalUITheme.SuccessGreen, TextAlignmentOptions.MidlineLeft);
            bonusTMP.textWrappingMode = TextWrappingModes.Normal;
            bonusTMP.rectTransform.anchorMin = Vector2.zero;
            bonusTMP.rectTransform.anchorMax = Vector2.one;
            bonusTMP.rectTransform.offsetMin = new Vector2(14f, 8f);
            bonusTMP.rectTransform.offsetMax = new Vector2(-14f, -8f);

            // Nút Tự Mặc Đồ Mạnh Nhất
            UnityEngine.UI.Button autoEquipBtn = CreateStyledMenuButton("AutoEquipBestButton", leftCol.transform, a, a.iconCrown,
                "TỰ MẶC ĐỒ MẠNH NHẤT (3 CLASS)", MedievalUITheme.BtnPrimaryGold, 50f, 15f);
            RectTransform aeRt = autoEquipBtn.GetComponent<RectTransform>();
            aeRt.anchorMin = new Vector2(0f, 0f);
            aeRt.anchorMax = new Vector2(1f, 0f);
            aeRt.pivot = new Vector2(0.5f, 0f);
            aeRt.offsetMin = new Vector2(14f, 14f);
            aeRt.offsetMax = new Vector2(-14f, 64f);

            // --- CỘT PHẢI: BỘ LỌC + DANH SÁCH KHO TRANG BỊ ---
            UnityEngine.UI.Image rightCol = CreateImage("RightInventoryColumn", card.transform, a.panelInset, MedievalUITheme.DeepInset);
            rightCol.type = UnityEngine.UI.Image.Type.Sliced;
            rightCol.pixelsPerUnitMultiplier = 0.65f;
            RectTransform rRt = rightCol.rectTransform;
            rRt.anchorMin = new Vector2(0.43f, 0f);
            rRt.anchorMax = new Vector2(1f, 1f);
            rRt.offsetMin = new Vector2(8f, 22f);
            rRt.offsetMax = new Vector2(-22f, -68f);

            TextMeshProUGUI invCountTMP = CreateTMP("InventoryCountTitle", rightCol.transform, "KHO TRANG BỊ (0 món)",
                a.fontBold, 15.5f, MedievalUITheme.GoldSoft, TextAlignmentOptions.MidlineLeft);
            invCountTMP.rectTransform.anchorMin = new Vector2(0f, 1f);
            invCountTMP.rectTransform.anchorMax = new Vector2(1f, 1f);
            invCountTMP.rectTransform.pivot = new Vector2(0.5f, 1f);
            invCountTMP.rectTransform.offsetMin = new Vector2(16f, -42f);
            invCountTMP.rectTransform.offsetMax = new Vector2(-16f, -12f);

            // 4 Filter Buttons (Tất cả / Hiệp Sĩ / Pháp Sư / Cung Thủ)
            GameObject filterRow = CreateRect("FilterTabsRow", rightCol.transform);
            RectTransform frRt = filterRow.GetComponent<RectTransform>();
            frRt.anchorMin = new Vector2(0f, 1f);
            frRt.anchorMax = new Vector2(1f, 1f);
            frRt.pivot = new Vector2(0.5f, 1f);
            frRt.offsetMin = new Vector2(14f, -84f);
            frRt.offsetMax = new Vector2(-14f, -48f);
            HorizontalLayoutGroup frHlg = filterRow.AddComponent<HorizontalLayoutGroup>();
            frHlg.spacing = 6f;
            frHlg.childControlWidth = true;
            frHlg.childControlHeight = true;
            frHlg.childForceExpandWidth = true;
            frHlg.childForceExpandHeight = true;

            string[] filtLabels = { "Tất Cả", "Hiệp Sĩ", "Pháp Sư", "Cung Thủ" };
            var filtBtns = new UnityEngine.UI.Button[4];
            var filtBgs = new UnityEngine.UI.Image[4];
            for (int i = 0; i < 4; i++)
            {
                UnityEngine.UI.Image bg = CreateImage($"FilterBtn_{i}", filterRow.transform, a.buttonFill,
                    i == 0 ? MedievalUITheme.BtnPrimaryGold : MedievalUITheme.BtnNeutralSlate);
                bg.type = UnityEngine.UI.Image.Type.Sliced;
                bg.pixelsPerUnitMultiplier = 0.65f;
                UnityEngine.UI.Button b = bg.gameObject.AddComponent<UnityEngine.UI.Button>();
                b.targetGraphic = bg;
                TextMeshProUGUI lbl = CreateTMP("Label", bg.transform, filtLabels[i], a.fontBold, 13f, MedievalUITheme.IvoryText, TextAlignmentOptions.Center);
                StretchFull(lbl.rectTransform);
                filtBtns[i] = b;
                filtBgs[i] = bg;
            }

            // ScrollRect chứa danh sách món đồ
            GameObject scrollGo = CreateRect("InventoryScrollView", rightCol.transform);
            RectTransform scRt = scrollGo.GetComponent<RectTransform>();
            scRt.anchorMin = Vector2.zero;
            scRt.anchorMax = Vector2.one;
            scRt.offsetMin = new Vector2(14f, 14f);
            scRt.offsetMax = new Vector2(-14f, -94f);
            UnityEngine.UI.Image scrollBg = scrollGo.AddComponent<UnityEngine.UI.Image>();
            scrollBg.color = new Color(0.04f, 0.05f, 0.08f, 0.72f);
            scrollGo.AddComponent<RectMask2D>();
            ScrollRect scrollRect = scrollGo.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.scrollSensitivity = 28f;

            GameObject contentGo = CreateRect("Content", scrollGo.transform);
            RectTransform contentRt = contentGo.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.anchoredPosition = Vector2.zero;
            contentRt.sizeDelta = Vector2.zero;

            VerticalLayoutGroup cVlg = contentGo.AddComponent<VerticalLayoutGroup>();
            cVlg.padding = new RectOffset(8, 8, 8, 8);
            cVlg.spacing = 8f;
            cVlg.childControlWidth = true;
            cVlg.childControlHeight = false;
            cVlg.childForceExpandWidth = true;
            cVlg.childForceExpandHeight = false;
            ContentSizeFitter csf = contentGo.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.content = contentRt;
            scrollRect.viewport = scRt;

            TextMeshProUGUI emptyHint = CreateTMP("EmptyHint", scrollGo.transform,
                "Kho đồ đang trống!\nHãy tiến sâu vào bản đồ 6 đợt để săn trang bị\nThường • Hiếm • Sử Thi • Huyền Thoại cho cả 3 Class.",
                a.fontBody, 15.5f, MedievalUITheme.SlateMuted, TextAlignmentOptions.Center);
            emptyHint.textWrappingMode = TextWrappingModes.Normal;
            StretchWithPadding(emptyHint.rectTransform, 32f);

            EquipmentModalUI modal = scrim.gameObject.AddComponent<EquipmentModalUI>();
            modal.Configure(
                a.panelInset, a.buttonFill, a.buttonFrame, a.badgePill,
                a.fontBold, a.fontBody,
                a.iconSword, a.iconMagic, a.iconArrow, a.iconShield, a.iconCrown,
                clsTabs, clsTabBgs, clsHeaderTMP, bonusTMP,
                sBorders, sIcons, sNames, sStats, sUnequips,
                filtBtns, filtBgs, invCountTMP, contentGo.transform, emptyHint.gameObject,
                autoEquipBtn, closeBtn);
            return modal;
        }

        private static void CreateStatusBar(
            string name, Transform parent, UIAssets a,
            Vector2 topOffsetMin, Vector2 topOffsetMax, Color fillColor, bool withTrail,
            out UnityEngine.UI.Image fillImage, out UnityEngine.UI.Image trailImage, out TextMeshProUGUI valueText)
        {
            UnityEngine.UI.Image frame = CreateImage(name, parent, a.barFrame, Color.white);
            frame.type = UnityEngine.UI.Image.Type.Sliced;
            frame.pixelsPerUnitMultiplier = 0.7f;
            frame.raycastTarget = false;

            RectTransform rt = frame.rectTransform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.offsetMin = new Vector2(topOffsetMin.x, topOffsetMax.y);
            rt.offsetMax = new Vector2(topOffsetMax.x, topOffsetMin.y);

            trailImage = null;
            if (withTrail)
            {
                trailImage = CreateImage("TrailFill", frame.transform, a.barFill, MedievalUITheme.HpTrailGold);
                trailImage.type = UnityEngine.UI.Image.Type.Filled;
                trailImage.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
                trailImage.raycastTarget = false;
                StretchWithPadding(trailImage.rectTransform, 3f);
            }

            fillImage = CreateImage("Fill", frame.transform, a.barFill, fillColor);
            fillImage.type = UnityEngine.UI.Image.Type.Filled;
            fillImage.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
            fillImage.raycastTarget = false;
            StretchWithPadding(fillImage.rectTransform, 3f);

            valueText = CreateTMP("ValueText", frame.transform, "100/100",
                a.fontBold, 12.5f, MedievalUITheme.IvoryText, TextAlignmentOptions.Center);
            valueText.outlineWidth = 0.22f;
            valueText.outlineColor = new Color32(9, 12, 18, 255);
            StretchFull(valueText.rectTransform);
        }

        private static void CreateActionCardButton(
            string name, Transform parent, UIAssets a, int hotkeyNumber,
            string title, Sprite icon, Color baseColor,
            out UnityEngine.UI.Button button, out TextMeshProUGUI titleTMP,
            out TextMeshProUGUI costTMP, out UnityEngine.UI.Image iconImg, out UIButtonFeedback feedback)
        {
            UnityEngine.UI.Image fillImg = CreateImage(name, parent, a.buttonFill, baseColor);
            fillImg.type = UnityEngine.UI.Image.Type.Sliced;
            fillImg.pixelsPerUnitMultiplier = 0.65f;

            button = fillImg.gameObject.AddComponent<UnityEngine.UI.Button>();
            button.targetGraphic = fillImg;
            button.transition = Selectable.Transition.None;

            feedback = fillImg.gameObject.AddComponent<UIButtonFeedback>();
            feedback.SetBaseColor(baseColor);

            // Khung viền pixel-art chồng lên trên nút
            UnityEngine.UI.Image border = CreateImage("Frame", fillImg.transform, a.buttonFrame, Color.white);
            border.type = UnityEngine.UI.Image.Type.Sliced;
            border.pixelsPerUnitMultiplier = 0.65f;
            border.raycastTarget = false;
            StretchFull(border.rectTransform);

            // Huy hiệu phím tắt [1..5] ở góc trên phải
            UnityEngine.UI.Image keyPill = CreateImage("HotkeyBadge", fillImg.transform, a.badgePill, MedievalUITheme.DeepInset);
            keyPill.type = UnityEngine.UI.Image.Type.Sliced;
            keyPill.pixelsPerUnitMultiplier = 0.7f;
            keyPill.raycastTarget = false;
            keyPill.rectTransform.anchorMin = new Vector2(1f, 1f);
            keyPill.rectTransform.anchorMax = new Vector2(1f, 1f);
            keyPill.rectTransform.pivot = new Vector2(1f, 1f);
            keyPill.rectTransform.anchoredPosition = new Vector2(-8f, -8f);
            keyPill.rectTransform.sizeDelta = new Vector2(26f, 22f);

            TextMeshProUGUI keyTMP = CreateTMP("Key", keyPill.transform, hotkeyNumber.ToString(),
                a.fontBold, 12.5f, MedievalUITheme.GoldSoft, TextAlignmentOptions.Center);
            StretchFull(keyTMP.rectTransform);

            // Biểu tượng hành động ở phần trên
            iconImg = CreateImage("Icon", fillImg.transform, icon, Color.white);
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;
            iconImg.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            iconImg.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            iconImg.rectTransform.pivot = new Vector2(0.5f, 1f);
            iconImg.rectTransform.anchoredPosition = new Vector2(0f, -20f);
            iconImg.rectTransform.sizeDelta = new Vector2(44f, 44f);

            // Tên kỹ năng
            titleTMP = CreateTMP("Title", fillImg.transform, title,
                a.fontBold, 17.5f, MedievalUITheme.IvoryText, TextAlignmentOptions.Center);
            titleTMP.textWrappingMode = TextWrappingModes.Normal;
            titleTMP.outlineWidth = 0.18f;
            titleTMP.outlineColor = new Color32(9, 12, 18, 255);
            titleTMP.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            titleTMP.rectTransform.anchorMax = new Vector2(1f, 0.5f);
            titleTMP.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            titleTMP.rectTransform.anchoredPosition = new Vector2(0f, -14f);
            titleTMP.rectTransform.sizeDelta = new Vector2(-16f, 46f);

            // Nhãn chi phí MP / Quiz ở đáy nút
            UnityEngine.UI.Image costBg = CreateImage("CostPill", fillImg.transform, a.badgePill, new Color(0.05f, 0.07f, 0.11f, 0.85f));
            costBg.type = UnityEngine.UI.Image.Type.Sliced;
            costBg.pixelsPerUnitMultiplier = 0.7f;
            costBg.raycastTarget = false;
            costBg.rectTransform.anchorMin = new Vector2(0f, 0f);
            costBg.rectTransform.anchorMax = new Vector2(1f, 0f);
            costBg.rectTransform.pivot = new Vector2(0.5f, 0f);
            costBg.rectTransform.anchoredPosition = new Vector2(0f, 10f);
            costBg.rectTransform.sizeDelta = new Vector2(-20f, 26f);

            costTMP = CreateTMP("CostText", costBg.transform, "Miễn phí",
                a.fontBody, 12.5f, MedievalUITheme.GoldSoft, TextAlignmentOptions.Center);
            StretchFull(costTMP.rectTransform);
        }

        private static QuizModalUI CreateQuizModal(Transform parent, UIAssets a)
        {
            UnityEngine.UI.Image scrim = CreateImage("QuizModalOverlay", parent, null, MedievalUITheme.ScrimDark);
            StretchFull(scrim.rectTransform);

            UnityEngine.UI.Image card = CreateImage("QuizCard", scrim.transform, a.panelDarkGold, Color.white);
            card.type = UnityEngine.UI.Image.Type.Sliced;
            card.pixelsPerUnitMultiplier = 0.65f;
            card.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            card.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            card.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            card.rectTransform.anchoredPosition = new Vector2(0f, 20f);
            card.rectTransform.sizeDelta = new Vector2(920f, 590f);

            // Header bar
            GameObject header = CreateRect("Header", card.transform);
            RectTransform hRt = header.GetComponent<RectTransform>();
            hRt.anchorMin = new Vector2(0f, 1f);
            hRt.anchorMax = new Vector2(1f, 1f);
            hRt.pivot = new Vector2(0.5f, 1f);
            hRt.anchoredPosition = new Vector2(0f, -18f);
            hRt.sizeDelta = new Vector2(-48f, 48f);

            UnityEngine.UI.Image scrollIcon = CreateImage("ScrollIcon", header.transform, a.iconScroll, Color.white);
            scrollIcon.preserveAspect = true;
            scrollIcon.raycastTarget = false;
            scrollIcon.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            scrollIcon.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            scrollIcon.rectTransform.pivot = new Vector2(0f, 0.5f);
            scrollIcon.rectTransform.sizeDelta = new Vector2(36f, 36f);

            TextMeshProUGUI headerTitle = CreateTMP("Title", header.transform, "THỬ THÁCH TRI THỨC",
                a.fontBold, 24f, MedievalUITheme.GoldSoft, TextAlignmentOptions.MidlineLeft);
            headerTitle.rectTransform.anchorMin = new Vector2(0f, 0f);
            headerTitle.rectTransform.anchorMax = new Vector2(0.6f, 1f);
            headerTitle.rectTransform.offsetMin = new Vector2(46f, 0f);
            headerTitle.rectTransform.offsetMax = Vector2.zero;

            UnityEngine.UI.Image bonusPill = CreateImage("BonusPill", header.transform, a.badgePill, MedievalUITheme.BtnPrimaryGold);
            bonusPill.type = UnityEngine.UI.Image.Type.Sliced;
            bonusPill.pixelsPerUnitMultiplier = 0.7f;
            bonusPill.raycastTarget = false;
            bonusPill.rectTransform.anchorMin = new Vector2(1f, 0.5f);
            bonusPill.rectTransform.anchorMax = new Vector2(1f, 0.5f);
            bonusPill.rectTransform.pivot = new Vector2(1f, 0.5f);
            bonusPill.rectTransform.anchoredPosition = Vector2.zero;
            bonusPill.rectTransform.sizeDelta = new Vector2(170f, 34f);

            TextMeshProUGUI bonusTMP = CreateTMP("BonusText", bonusPill.transform, "Thưởng +8 ST",
                a.fontBold, 15f, MedievalUITheme.IvoryText, TextAlignmentOptions.Center);
            StretchFull(bonusTMP.rectTransform);

            UnityEngine.UI.Image catPill = CreateImage("CategoryPill", header.transform, a.badgePill, MedievalUITheme.CategoryHistory);
            catPill.type = UnityEngine.UI.Image.Type.Sliced;
            catPill.pixelsPerUnitMultiplier = 0.7f;
            catPill.raycastTarget = false;
            catPill.rectTransform.anchorMin = new Vector2(1f, 0.5f);
            catPill.rectTransform.anchorMax = new Vector2(1f, 0.5f);
            catPill.rectTransform.pivot = new Vector2(1f, 0.5f);
            catPill.rectTransform.anchoredPosition = new Vector2(-182f, 0f);
            catPill.rectTransform.sizeDelta = new Vector2(150f, 34f);

            TextMeshProUGUI catTMP = CreateTMP("CategoryText", catPill.transform, "LỊCH SỬ",
                a.fontBold, 14f, MedievalUITheme.IvoryText, TextAlignmentOptions.Center);
            StretchFull(catTMP.rectTransform);

            // Pending action prompt
            TextMeshProUGUI pendingTMP = CreateTMP("PendingActionText", card.transform,
                "Anh hùng đang chuẩn bị xuất chiêu — Trả lời đúng để kích hoạt!",
                a.fontBody, 16.5f, MedievalUITheme.SlateMuted, TextAlignmentOptions.MidlineLeft);
            pendingTMP.rectTransform.anchorMin = new Vector2(0f, 1f);
            pendingTMP.rectTransform.anchorMax = new Vector2(1f, 1f);
            pendingTMP.rectTransform.pivot = new Vector2(0.5f, 1f);
            pendingTMP.rectTransform.anchoredPosition = new Vector2(0f, -72f);
            pendingTMP.rectTransform.sizeDelta = new Vector2(-48f, 28f);

            // Question Box
            UnityEngine.UI.Image qBox = CreateImage("QuestionInset", card.transform, a.panelInset, Color.white);
            qBox.type = UnityEngine.UI.Image.Type.Sliced;
            qBox.pixelsPerUnitMultiplier = 0.65f;
            qBox.raycastTarget = false;
            qBox.rectTransform.anchorMin = new Vector2(0f, 1f);
            qBox.rectTransform.anchorMax = new Vector2(1f, 1f);
            qBox.rectTransform.pivot = new Vector2(0.5f, 1f);
            qBox.rectTransform.anchoredPosition = new Vector2(0f, -108f);
            qBox.rectTransform.sizeDelta = new Vector2(-48f, 128f);

            TextMeshProUGUI qContentTMP = CreateTMP("QuestionContent", qBox.transform, "Nội dung câu hỏi?",
                a.fontBold, 24f, MedievalUITheme.IvoryText, TextAlignmentOptions.MidlineLeft);
            qContentTMP.textWrappingMode = TextWrappingModes.Normal;
            StretchWithPadding(qContentTMP.rectTransform, 18f);

            // 2x2 Options Grid
            GameObject gridGo = CreateRect("OptionsGrid", card.transform);
            RectTransform gridRt = gridGo.GetComponent<RectTransform>();
            gridRt.anchorMin = new Vector2(0f, 1f);
            gridRt.anchorMax = new Vector2(1f, 1f);
            gridRt.pivot = new Vector2(0.5f, 1f);
            gridRt.anchoredPosition = new Vector2(0f, -252f);
            gridRt.sizeDelta = new Vector2(-48f, 176f);

            GridLayoutGroup grid = gridGo.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(428f, 80f);
            grid.spacing = new Vector2(16f, 16f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;

            var optButtons = new UnityEngine.UI.Button[4];
            var optBgs = new UnityEngine.UI.Image[4];
            var optBadges = new UnityEngine.UI.Image[4];
            var optTexts = new TextMeshProUGUI[4];
            var optFeedbacks = new UIButtonFeedback[4];

            for (int i = 0; i < 4; i++)
            {
                char letter = (char)('A' + i);
                UnityEngine.UI.Image optBg = CreateImage($"Option_{letter}", gridGo.transform, a.buttonFill, MedievalUITheme.BtnNeutralSlate);
                optBg.type = UnityEngine.UI.Image.Type.Sliced;
                optBg.pixelsPerUnitMultiplier = 0.65f;

                UnityEngine.UI.Button btn = optBg.gameObject.AddComponent<UnityEngine.UI.Button>();
                btn.targetGraphic = optBg;
                btn.transition = Selectable.Transition.None;

                UIButtonFeedback fb = optBg.gameObject.AddComponent<UIButtonFeedback>();
                fb.SetBaseColor(MedievalUITheme.BtnNeutralSlate);

                UnityEngine.UI.Image frame = CreateImage("Frame", optBg.transform, a.buttonFrame, Color.white);
                frame.type = UnityEngine.UI.Image.Type.Sliced;
                frame.pixelsPerUnitMultiplier = 0.65f;
                frame.raycastTarget = false;
                StretchFull(frame.rectTransform);

                UnityEngine.UI.Image letterPill = CreateImage("LetterBadge", optBg.transform, a.badgePill, MedievalUITheme.GoldPrimary);
                letterPill.type = UnityEngine.UI.Image.Type.Sliced;
                letterPill.pixelsPerUnitMultiplier = 0.7f;
                letterPill.raycastTarget = false;
                letterPill.rectTransform.anchorMin = new Vector2(0f, 0.5f);
                letterPill.rectTransform.anchorMax = new Vector2(0f, 0.5f);
                letterPill.rectTransform.pivot = new Vector2(0f, 0.5f);
                letterPill.rectTransform.anchoredPosition = new Vector2(14f, 0f);
                letterPill.rectTransform.sizeDelta = new Vector2(44f, 44f);

                TextMeshProUGUI letterTMP = CreateTMP("Letter", letterPill.transform, letter.ToString(),
                    a.fontBold, 21f, MedievalUITheme.DarkInkText, TextAlignmentOptions.Center);
                StretchFull(letterTMP.rectTransform);

                TextMeshProUGUI ansTMP = CreateTMP("AnswerText", optBg.transform, $"Phương án {letter}",
                    a.fontBody, 20f, MedievalUITheme.IvoryText, TextAlignmentOptions.MidlineLeft);
                ansTMP.textWrappingMode = TextWrappingModes.Normal;
                ansTMP.rectTransform.anchorMin = Vector2.zero;
                ansTMP.rectTransform.anchorMax = Vector2.one;
                ansTMP.rectTransform.offsetMin = new Vector2(70f, 8f);
                ansTMP.rectTransform.offsetMax = new Vector2(-14f, -8f);

                optButtons[i] = btn;
                optBgs[i] = optBg;
                optBadges[i] = letterPill;
                optTexts[i] = ansTMP;
                optFeedbacks[i] = fb;
            }

            // Feedback Banner ở đáy Quiz Modal (hiển thị ngay sau khi chọn đáp án)
            UnityEngine.UI.Image fbContainer = CreateImage("FeedbackContainer", card.transform, a.buttonFill, MedievalUITheme.SuccessPanel);
            fbContainer.type = UnityEngine.UI.Image.Type.Sliced;
            fbContainer.pixelsPerUnitMultiplier = 0.65f;
            fbContainer.rectTransform.anchorMin = new Vector2(0f, 0f);
            fbContainer.rectTransform.anchorMax = new Vector2(1f, 0f);
            fbContainer.rectTransform.pivot = new Vector2(0.5f, 0f);
            fbContainer.rectTransform.anchoredPosition = new Vector2(0f, 18f);
            fbContainer.rectTransform.sizeDelta = new Vector2(-48f, 128f);

            UnityEngine.UI.Image fbBorder = CreateImage("Frame", fbContainer.transform, a.buttonFrame, Color.white);
            fbBorder.type = UnityEngine.UI.Image.Type.Sliced;
            fbBorder.pixelsPerUnitMultiplier = 0.65f;
            fbBorder.raycastTarget = false;
            StretchFull(fbBorder.rectTransform);

            TextMeshProUGUI fbTitleTMP = CreateTMP("FeedbackTitle", fbContainer.transform, "CHÍNH XÁC!",
                a.fontBold, 20f, MedievalUITheme.GoldSoft, TextAlignmentOptions.MidlineLeft);
            fbTitleTMP.rectTransform.anchorMin = new Vector2(0f, 1f);
            fbTitleTMP.rectTransform.anchorMax = new Vector2(1f, 1f);
            fbTitleTMP.rectTransform.pivot = new Vector2(0f, 1f);
            fbTitleTMP.rectTransform.offsetMin = new Vector2(18f, -38f);
            fbTitleTMP.rectTransform.offsetMax = new Vector2(-176f, -8f);

            TextMeshProUGUI fbExplTMP = CreateTMP("FeedbackExplanation", fbContainer.transform, "Giải thích kiến thức...",
                a.fontBody, 16.5f, MedievalUITheme.IvoryText, TextAlignmentOptions.TopLeft);
            fbExplTMP.textWrappingMode = TextWrappingModes.Normal;
            fbExplTMP.rectTransform.anchorMin = Vector2.zero;
            fbExplTMP.rectTransform.anchorMax = Vector2.one;
            fbExplTMP.rectTransform.offsetMin = new Vector2(18f, 18f);
            fbExplTMP.rectTransform.offsetMax = new Vector2(-176f, -40f);

            // Thanh tiến trình tự đóng ở mép dưới Feedback
            UnityEngine.UI.Image timerTrack = CreateImage("TimerTrack", fbContainer.transform, null, new Color(0f, 0f, 0f, 0.35f));
            timerTrack.raycastTarget = false;
            timerTrack.rectTransform.anchorMin = new Vector2(0f, 0f);
            timerTrack.rectTransform.anchorMax = new Vector2(1f, 0f);
            timerTrack.rectTransform.pivot = new Vector2(0.5f, 0f);
            timerTrack.rectTransform.anchoredPosition = new Vector2(0f, 4f);
            timerTrack.rectTransform.sizeDelta = new Vector2(-16f, 6f);

            UnityEngine.UI.Image timerFill = CreateImage("TimerFill", timerTrack.transform, a.barFill, MedievalUITheme.SuccessGreen);
            timerFill.type = UnityEngine.UI.Image.Type.Filled;
            timerFill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
            timerFill.raycastTarget = false;
            StretchFull(timerFill.rectTransform);

            UnityEngine.UI.Button continueBtn = CreateStyledMenuButton("ContinueButton", fbContainer.transform, a, null,
                "TIẾP TỤC [Space]", MedievalUITheme.BtnPrimaryGold, 48f, 15.5f);
            RectTransform cbRt = continueBtn.GetComponent<RectTransform>();
            cbRt.anchorMin = new Vector2(1f, 0.5f);
            cbRt.anchorMax = new Vector2(1f, 0.5f);
            cbRt.pivot = new Vector2(1f, 0.5f);
            cbRt.anchoredPosition = new Vector2(-16f, 4f);
            cbRt.sizeDelta = new Vector2(150f, 48f);

            fbContainer.gameObject.SetActive(false);

            QuizModalUI modal = scrim.gameObject.AddComponent<QuizModalUI>();
            modal.Configure(
                catPill, catTMP, bonusTMP, pendingTMP, qContentTMP,
                optButtons, optBgs, optBadges, optTexts, optFeedbacks,
                fbContainer.gameObject, fbContainer, fbTitleTMP, fbExplTMP, timerFill, continueBtn);

            return modal;
        }

        private static GameObject CreateModalOverlay(
            string name, Transform parent, UIAssets a, Vector2 cardSize,
            Sprite headerIcon, string headerTitle,
            out Transform contentContainer, out UnityEngine.UI.Button primaryCloseBtn, string closeLabel)
        {
            UnityEngine.UI.Image scrim = CreateImage(name, parent, null, MedievalUITheme.ScrimDark);
            StretchFull(scrim.rectTransform);

            UnityEngine.UI.Image card = CreateImage("ModalCard", scrim.transform, a.panelDarkGold, Color.white);
            card.type = UnityEngine.UI.Image.Type.Sliced;
            card.pixelsPerUnitMultiplier = 0.65f;
            card.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            card.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            card.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            card.rectTransform.sizeDelta = cardSize;

            // Header Ribbon
            UnityEngine.UI.Image ribbon = CreateImage("HeaderRibbon", card.transform, a.headerRibbon, Color.white);
            ribbon.type = UnityEngine.UI.Image.Type.Sliced;
            ribbon.pixelsPerUnitMultiplier = 0.65f;
            ribbon.raycastTarget = false;
            ribbon.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            ribbon.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            ribbon.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            ribbon.rectTransform.anchoredPosition = new Vector2(0f, -6f);
            ribbon.rectTransform.sizeDelta = new Vector2(Mathf.Min(460f, cardSize.x - 60f), 54f);

            if (headerIcon != null)
            {
                UnityEngine.UI.Image ic = CreateImage("HeaderIcon", ribbon.transform, headerIcon, Color.white);
                ic.preserveAspect = true;
                ic.raycastTarget = false;
                ic.rectTransform.anchorMin = new Vector2(0f, 0.5f);
                ic.rectTransform.anchorMax = new Vector2(0f, 0.5f);
                ic.rectTransform.pivot = new Vector2(0f, 0.5f);
                ic.rectTransform.anchoredPosition = new Vector2(20f, 0f);
                ic.rectTransform.sizeDelta = new Vector2(30f, 30f);
            }

            TextMeshProUGUI titleTMP = CreateTMP("HeaderTitle", ribbon.transform, headerTitle,
                a.fontBold, 22f, MedievalUITheme.GoldSoft, TextAlignmentOptions.Center);
            StretchFull(titleTMP.rectTransform);

            GameObject body = CreateRect("Content", card.transform);
            RectTransform bodyRt = body.GetComponent<RectTransform>();
            bodyRt.anchorMin = Vector2.zero;
            bodyRt.anchorMax = Vector2.one;
            bodyRt.offsetMin = new Vector2(28f, 92f);
            bodyRt.offsetMax = new Vector2(-28f, -44f);
            contentContainer = body.transform;

            primaryCloseBtn = CreateStyledMenuButton("PrimaryCloseButton", card.transform, a, null,
                closeLabel, MedievalUITheme.BtnPrimaryGold, 54f, 19f);
            RectTransform btnRt = primaryCloseBtn.GetComponent<RectTransform>();
            btnRt.anchorMin = new Vector2(0.5f, 0f);
            btnRt.anchorMax = new Vector2(0.5f, 0f);
            btnRt.pivot = new Vector2(0.5f, 0f);
            btnRt.anchoredPosition = new Vector2(0f, 22f);
            btnRt.sizeDelta = new Vector2(240f, 54f);

            return scrim.gameObject;
        }

        private static GameObject CreateEndScreenModal(
            string name, Transform parent, UIAssets a,
            Sprite emblemIcon, string headline, Color headlineColor,
            out TextMeshProUGUI summaryTMP,
            out UnityEngine.UI.Button restartBtn, out UnityEngine.UI.Button menuBtn,
            string restartLabel, Color restartColor)
        {
            UnityEngine.UI.Image scrim = CreateImage(name, parent, null, MedievalUITheme.ScrimDark);
            StretchFull(scrim.rectTransform);

            UnityEngine.UI.Image card = CreateImage("EndCard", scrim.transform, a.panelDarkGold, Color.white);
            card.type = UnityEngine.UI.Image.Type.Sliced;
            card.pixelsPerUnitMultiplier = 0.65f;
            card.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            card.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            card.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            card.rectTransform.sizeDelta = new Vector2(700f, 510f);

            UnityEngine.UI.Image emblem = CreateImage("EmblemIcon", card.transform, emblemIcon, Color.white);
            emblem.preserveAspect = true;
            emblem.raycastTarget = false;
            emblem.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            emblem.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            emblem.rectTransform.pivot = new Vector2(0.5f, 1f);
            emblem.rectTransform.anchoredPosition = new Vector2(0f, -24f);
            emblem.rectTransform.sizeDelta = new Vector2(64f, 64f);

            TextMeshProUGUI titleTMP = CreateTMP("EndTitle", card.transform, headline,
                a.fontDisplay, 38f, headlineColor, TextAlignmentOptions.Center);
            titleTMP.outlineWidth = 0.2f;
            titleTMP.outlineColor = new Color32(9, 12, 18, 255);
            titleTMP.rectTransform.anchorMin = new Vector2(0f, 1f);
            titleTMP.rectTransform.anchorMax = new Vector2(1f, 1f);
            titleTMP.rectTransform.pivot = new Vector2(0.5f, 1f);
            titleTMP.rectTransform.anchoredPosition = new Vector2(0f, -92f);
            titleTMP.rectTransform.sizeDelta = new Vector2(-40f, 52f);

            UnityEngine.UI.Image summaryBox = CreateImage("SummaryInset", card.transform, a.panelInset, Color.white);
            summaryBox.type = UnityEngine.UI.Image.Type.Sliced;
            summaryBox.pixelsPerUnitMultiplier = 0.65f;
            summaryBox.raycastTarget = false;
            summaryBox.rectTransform.anchorMin = Vector2.zero;
            summaryBox.rectTransform.anchorMax = Vector2.one;
            summaryBox.rectTransform.offsetMin = new Vector2(36f, 102f);
            summaryBox.rectTransform.offsetMax = new Vector2(-36f, -152f);

            summaryTMP = CreateTMP("SummaryText", summaryBox.transform, "",
                a.fontBody, 18f, MedievalUITheme.IvoryText, TextAlignmentOptions.MidlineLeft);
            summaryTMP.enableAutoSizing = true;
            summaryTMP.fontSizeMin = 14f;
            summaryTMP.fontSizeMax = 18f;
            summaryTMP.textWrappingMode = TextWrappingModes.Normal;
            StretchWithPadding(summaryTMP.rectTransform, 22f);

            GameObject btnRow = CreateRect("EndButtonRow", card.transform);
            RectTransform brRt = btnRow.GetComponent<RectTransform>();
            brRt.anchorMin = new Vector2(0f, 0f);
            brRt.anchorMax = new Vector2(1f, 0f);
            brRt.pivot = new Vector2(0.5f, 0f);
            brRt.anchoredPosition = new Vector2(0f, 24f);
            brRt.sizeDelta = new Vector2(-72f, 60f);

            HorizontalLayoutGroup hlg = btnRow.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 16f;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;

            restartBtn = CreateStyledMenuButton("RestartButton", btnRow.transform, a, a.iconPlay, restartLabel, restartColor, 58f, 18f);
            menuBtn = CreateStyledMenuButton("MainMenuButton", btnRow.transform, a, a.iconExit, "VỀ MENU CHÍNH", MedievalUITheme.BtnNeutralSlate, 58f, 18f);

            return scrim.gameObject;
        }

        private static void BuildSettingsControls(
            Transform parent, UIAssets a,
            out UnityEngine.UI.Slider musicSlider, out TextMeshProUGUI musicValTMP,
            out UnityEngine.UI.Slider sfxSlider, out TextMeshProUGUI sfxValTMP,
            out UnityEngine.UI.Toggle quizToggle, out UnityEngine.UI.Toggle fastToggle)
        {
            VerticalLayoutGroup vlg = parent.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 12f;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            CreateSliderRow("MusicVolumeRow", parent, a, "Nhạc nền (Music Volume)", out musicSlider, out musicValTMP);
            CreateSliderRow("SfxVolumeRow", parent, a, "Hiệu ứng âm thanh (SFX Volume)", out sfxSlider, out sfxValTMP);

            quizToggle = CreateToggleRow("QuizModeToggleRow", parent, a,
                "Bật câu hỏi Trắc nghiệm Giáo dục",
                "Yêu cầu trả lời đúng câu hỏi trước khi tung chiêu & nhận thưởng sát thương.");

            fastToggle = CreateToggleRow("FastSpeedToggleRow", parent, a,
                "Tốc độ chiến đấu nhanh (1.5x)",
                "Tăng tốc độ hoạt ảnh và chuyển lượt trong trận đấu.");
        }

        private static void CreateSliderRow(
            string name, Transform parent, UIAssets a, string title,
            out UnityEngine.UI.Slider slider, out TextMeshProUGUI valTMP)
        {
            UnityEngine.UI.Image row = CreateImage(name, parent, a.panelInset, Color.white);
            row.type = UnityEngine.UI.Image.Type.Sliced;
            row.pixelsPerUnitMultiplier = 0.65f;
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = 76f;

            TextMeshProUGUI lbl = CreateTMP("Label", row.transform, title,
                a.fontBold, 16.5f, MedievalUITheme.IvoryText, TextAlignmentOptions.MidlineLeft);
            lbl.rectTransform.anchorMin = new Vector2(0f, 1f);
            lbl.rectTransform.anchorMax = new Vector2(0.75f, 1f);
            lbl.rectTransform.pivot = new Vector2(0f, 1f);
            lbl.rectTransform.offsetMin = new Vector2(18f, -32f);
            lbl.rectTransform.offsetMax = new Vector2(0f, -6f);

            valTMP = CreateTMP("Percent", row.transform, "85%",
                a.fontBold, 16.5f, MedievalUITheme.GoldSoft, TextAlignmentOptions.MidlineRight);
            valTMP.rectTransform.anchorMin = new Vector2(0.75f, 1f);
            valTMP.rectTransform.anchorMax = new Vector2(1f, 1f);
            valTMP.rectTransform.pivot = new Vector2(1f, 1f);
            valTMP.rectTransform.offsetMin = new Vector2(0f, -32f);
            valTMP.rectTransform.offsetMax = new Vector2(-18f, -6f);

            slider = CreateStyledSlider("Slider", row.transform, a);
            RectTransform slRt = slider.GetComponent<RectTransform>();
            slRt.anchorMin = new Vector2(0f, 0f);
            slRt.anchorMax = new Vector2(1f, 0f);
            slRt.pivot = new Vector2(0.5f, 0f);
            slRt.anchoredPosition = new Vector2(0f, 12f);
            slRt.sizeDelta = new Vector2(-36f, 24f);
        }

        private static UnityEngine.UI.Slider CreateStyledSlider(string name, Transform parent, UIAssets a)
        {
            GameObject sliderGo = CreateRect(name, parent);
            UnityEngine.UI.Slider slider = sliderGo.AddComponent<UnityEngine.UI.Slider>();

            UnityEngine.UI.Image bg = CreateImage("Background", sliderGo.transform, a.barFrame, Color.white);
            bg.type = UnityEngine.UI.Image.Type.Sliced;
            bg.pixelsPerUnitMultiplier = 0.7f;
            bg.raycastTarget = false;
            StretchFull(bg.rectTransform);

            GameObject fillArea = CreateRect("Fill Area", sliderGo.transform);
            StretchWithPadding(fillArea.GetComponent<RectTransform>(), 4f);

            UnityEngine.UI.Image fill = CreateImage("Fill", fillArea.transform, a.barFill, MedievalUITheme.GoldPrimary);
            fill.type = UnityEngine.UI.Image.Type.Sliced;
            fill.pixelsPerUnitMultiplier = 0.7f;
            fill.raycastTarget = false;
            StretchFull(fill.rectTransform);

            GameObject handleArea = CreateRect("Handle Slide Area", sliderGo.transform);
            StretchFull(handleArea.GetComponent<RectTransform>());

            UnityEngine.UI.Image handle = CreateImage("Handle", handleArea.transform, a.badgePill, MedievalUITheme.GoldSoft);
            handle.type = UnityEngine.UI.Image.Type.Sliced;
            handle.pixelsPerUnitMultiplier = 0.7f;
            handle.rectTransform.sizeDelta = new Vector2(22f, 28f);

            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.direction = UnityEngine.UI.Slider.Direction.LeftToRight;
            return slider;
        }

        private static UnityEngine.UI.Toggle CreateToggleRow(string name, Transform parent, UIAssets a, string title, string subtitle)
        {
            UnityEngine.UI.Image row = CreateImage(name, parent, a.panelInset, Color.white);
            row.type = UnityEngine.UI.Image.Type.Sliced;
            row.pixelsPerUnitMultiplier = 0.65f;
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = 68f;

            UnityEngine.UI.Toggle toggle = row.gameObject.AddComponent<UnityEngine.UI.Toggle>();

            TextMeshProUGUI titleTMP = CreateTMP("Title", row.transform, title,
                a.fontBold, 16.5f, MedievalUITheme.IvoryText, TextAlignmentOptions.MidlineLeft);
            titleTMP.rectTransform.anchorMin = new Vector2(0f, 1f);
            titleTMP.rectTransform.anchorMax = new Vector2(1f, 1f);
            titleTMP.rectTransform.pivot = new Vector2(0f, 1f);
            titleTMP.rectTransform.offsetMin = new Vector2(18f, -34f);
            titleTMP.rectTransform.offsetMax = new Vector2(-78f, -8f);

            TextMeshProUGUI subTMP = CreateTMP("Subtitle", row.transform, subtitle,
                a.fontBody, 13.5f, MedievalUITheme.SlateMuted, TextAlignmentOptions.MidlineLeft);
            subTMP.rectTransform.anchorMin = new Vector2(0f, 0f);
            subTMP.rectTransform.anchorMax = new Vector2(1f, 0f);
            subTMP.rectTransform.pivot = new Vector2(0f, 0f);
            subTMP.rectTransform.offsetMin = new Vector2(18f, 8f);
            subTMP.rectTransform.offsetMax = new Vector2(-78f, 32f);

            UnityEngine.UI.Image boxBg = CreateImage("CheckBackground", row.transform, a.badgePill, MedievalUITheme.BtnNeutralSlate);
            boxBg.type = UnityEngine.UI.Image.Type.Sliced;
            boxBg.pixelsPerUnitMultiplier = 0.7f;
            boxBg.rectTransform.anchorMin = new Vector2(1f, 0.5f);
            boxBg.rectTransform.anchorMax = new Vector2(1f, 0.5f);
            boxBg.rectTransform.pivot = new Vector2(1f, 0.5f);
            boxBg.rectTransform.anchoredPosition = new Vector2(-18f, 0f);
            boxBg.rectTransform.sizeDelta = new Vector2(44f, 36f);

            UnityEngine.UI.Image checkmark = CreateImage("Checkmark", boxBg.transform, a.badgePill, MedievalUITheme.SuccessGreen);
            checkmark.type = UnityEngine.UI.Image.Type.Sliced;
            checkmark.pixelsPerUnitMultiplier = 0.7f;
            checkmark.raycastTarget = false;
            StretchWithPadding(checkmark.rectTransform, 5f);

            TextMeshProUGUI onTxt = CreateTMP("OnTxt", checkmark.transform, "BẬT",
                a.fontBold, 11.5f, MedievalUITheme.DarkInkText, TextAlignmentOptions.Center);
            StretchFull(onTxt.rectTransform);

            toggle.targetGraphic = boxBg;
            toggle.graphic = checkmark;
            return toggle;
        }

        private static UnityEngine.UI.Button CreateStyledMenuButton(
            string name, Transform parent, UIAssets a, Sprite icon,
            string text, Color baseColor, float height, float fontSize)
        {
            UnityEngine.UI.Image fill = CreateImage(name, parent, a.buttonFill, baseColor);
            fill.type = UnityEngine.UI.Image.Type.Sliced;
            fill.pixelsPerUnitMultiplier = 0.65f;

            LayoutElement le = fill.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = height;

            UnityEngine.UI.Button btn = fill.gameObject.AddComponent<UnityEngine.UI.Button>();
            btn.targetGraphic = fill;
            btn.transition = Selectable.Transition.None;

            UIButtonFeedback fb = fill.gameObject.AddComponent<UIButtonFeedback>();
            fb.SetBaseColor(baseColor);

            UnityEngine.UI.Image frame = CreateImage("Frame", fill.transform, a.buttonFrame, Color.white);
            frame.type = UnityEngine.UI.Image.Type.Sliced;
            frame.pixelsPerUnitMultiplier = 0.65f;
            frame.raycastTarget = false;
            StretchFull(frame.rectTransform);

            if (icon != null)
            {
                UnityEngine.UI.Image ic = CreateImage("Icon", fill.transform, icon, Color.white);
                ic.preserveAspect = true;
                ic.raycastTarget = false;
                ic.rectTransform.anchorMin = new Vector2(0f, 0.5f);
                ic.rectTransform.anchorMax = new Vector2(0f, 0.5f);
                ic.rectTransform.pivot = new Vector2(0f, 0.5f);
                ic.rectTransform.anchoredPosition = new Vector2(18f, 0f);
                ic.rectTransform.sizeDelta = new Vector2(height * 0.48f, height * 0.48f);
            }

            TextMeshProUGUI label = CreateTMP("Label", fill.transform, text,
                a.fontBold, fontSize, MedievalUITheme.IvoryText, TextAlignmentOptions.Center);
            label.outlineWidth = 0.18f;
            label.outlineColor = new Color32(9, 12, 18, 255);
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = new Vector2(icon != null ? 44f : 12f, 0f);
            label.rectTransform.offsetMax = new Vector2(-12f, 0f);

            return btn;
        }

        private static UnityEngine.UI.Button CreateIconSquareButton(
            string name, Transform parent, UIAssets a, Sprite icon, Color baseColor, Vector2 size)
        {
            UnityEngine.UI.Image fill = CreateImage(name, parent, a.buttonFill, baseColor);
            fill.type = UnityEngine.UI.Image.Type.Sliced;
            fill.pixelsPerUnitMultiplier = 0.65f;
            fill.rectTransform.sizeDelta = size;

            UnityEngine.UI.Button btn = fill.gameObject.AddComponent<UnityEngine.UI.Button>();
            btn.targetGraphic = fill;
            btn.transition = Selectable.Transition.None;

            UIButtonFeedback fb = fill.gameObject.AddComponent<UIButtonFeedback>();
            fb.SetBaseColor(baseColor);

            UnityEngine.UI.Image frame = CreateImage("Frame", fill.transform, a.buttonFrame, Color.white);
            frame.type = UnityEngine.UI.Image.Type.Sliced;
            frame.pixelsPerUnitMultiplier = 0.65f;
            frame.raycastTarget = false;
            StretchFull(frame.rectTransform);

            UnityEngine.UI.Image ic = CreateImage("Icon", fill.transform, icon, Color.white);
            ic.preserveAspect = true;
            ic.raycastTarget = false;
            StretchWithPadding(ic.rectTransform, 12f);

            return btn;
        }

        private static CanvasGroup CreateFadeOverlay(Transform parent)
        {
            UnityEngine.UI.Image fadeImg = CreateImage("ScreenFadeOverlay", parent, null, new Color(0.035f, 0.047f, 0.07f, 1f));
            fadeImg.raycastTarget = false;
            StretchFull(fadeImg.rectTransform);
            CanvasGroup cg = fadeImg.gameObject.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            cg.blocksRaycasts = false;
            return cg;
        }

        private static Canvas CreateRootCanvas(string name, Camera worldCam)
        {
            GameObject canvasGo = new GameObject(name);
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            if (worldCam != null)
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = worldCam;
                canvas.planeDistance = 5f;
                canvas.sortingOrder = 100;
            }
            else
            {
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }

            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        private static void EnsureEventSystem()
        {
            EventSystem es = UnityEngine.Object.FindAnyObjectByType<EventSystem>();
            GameObject esGo = es != null ? es.gameObject : new GameObject("EventSystem");
            if (es == null) es = esGo.AddComponent<EventSystem>();

            StandaloneInputModule oldModule = esGo.GetComponent<StandaloneInputModule>();
            if (oldModule != null) UnityEngine.Object.DestroyImmediate(oldModule);

            InputSystemUIInputModule uiModule = esGo.GetComponent<InputSystemUIInputModule>();
            if (uiModule == null) uiModule = esGo.AddComponent<InputSystemUIInputModule>();

            InputActionAsset actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(INPUT_ACTIONS);
            if (actions != null)
            {
                uiModule.actionsAsset = actions;
                var allRefs = AssetDatabase.LoadAllAssetsAtPath(INPUT_ACTIONS).OfType<InputActionReference>().ToArray();
                InputActionReference FindRef(string actionName) =>
                    allRefs.FirstOrDefault(r => r.action != null && r.action.actionMap != null && r.action.actionMap.name == "UI" && r.action.name == actionName);

                uiModule.point = FindRef("Point");
                uiModule.leftClick = FindRef("Click");
                uiModule.rightClick = FindRef("RightClick");
                uiModule.middleClick = FindRef("MiddleClick");
                uiModule.scrollWheel = FindRef("ScrollWheel");
                uiModule.move = FindRef("Navigate");
                uiModule.submit = FindRef("Submit");
                uiModule.cancel = FindRef("Cancel");
                uiModule.trackedDevicePosition = FindRef("TrackedDevicePosition");
                uiModule.trackedDeviceOrientation = FindRef("TrackedDeviceOrientation");
            }
            EditorUtility.SetDirty(esGo);
        }

        private static GameObject CreateRect(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static UnityEngine.UI.Image CreateImage(string name, Transform parent, Sprite sprite, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image));
            go.transform.SetParent(parent, false);
            UnityEngine.UI.Image img = go.GetComponent<UnityEngine.UI.Image>();
            img.sprite = sprite;
            img.color = color;
            return img;
        }

        private static TextMeshProUGUI CreateTMP(
            string name, Transform parent, string text,
            TMP_FontAsset font, float size, Color color, TextAlignmentOptions align)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
            if (font != null) tmp.font = font;
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = align;
            tmp.raycastTarget = false;
            tmp.richText = true;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            return tmp;
        }

        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void StretchWithPadding(RectTransform rt, float pad)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(pad, pad);
            rt.offsetMax = new Vector2(-pad, -pad);
        }

        private static void DestroyExistingByName(string objectName)
        {
            GameObject go = GameObject.Find(objectName);
            while (go != null)
            {
                UnityEngine.Object.DestroyImmediate(go);
                go = GameObject.Find(objectName);
            }
        }

        private static void RemoveMissingComponents(GameObject go)
        {
            if (go == null) return;
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
        }
    }
}
