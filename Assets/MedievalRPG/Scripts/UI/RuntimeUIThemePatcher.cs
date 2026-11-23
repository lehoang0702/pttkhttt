using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;
using MedievalRPG.Combat;
using MedievalRPG.Visuals;

namespace MedievalRPG.UI
{
    /// <summary>
    /// Đồng bộ tự động phông chữ tiếng Việt rõ nét (UiSans) và tự động nới/co giãn
    /// các khung chữ dễ tràn (Hướng dẫn, Quiz, Tooltip, Cài đặt, Màn kết thúc) ngay khi
    /// scene tải trong Play Mode hoặc bản build, kể cả khi scene chưa kịp rebuild trong Editor.
    /// </summary>
    public static class RuntimeUIThemePatcher
    {
        public const string VietnameseGlyphs =
            " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~" +
            "ÀÁÂÃÈÉÊÌÍÒÓÔÕÙÚÝàáâãèéêìíòóôõùúýĂăĐđĨĩŨũƠơƯư" +
            "ẠạẢảẤấẦầẨẩẪẫẬậẮắẰằẲẳẴẵẶặẸẹẺẻẼẽẾếỀềỂểỄễỆệỈỉỊịỌọỎỏỐốỒồỔổỖỗỘộỚớỜờỞởỠỡỢợỤụỦủỨứỪừỬửỮữỰựỲỳỴỵỶỷỸỹ–—•×÷°%";

        private static TMP_FontAsset _regularFont;
        private static TMP_FontAsset _boldFont;
        private static TMP_FontAsset _italicFont;
        private static TMP_FontAsset _boldItalicFont;
        private static bool _hooked;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            _regularFont = null;
            _boldFont = null;
            _italicFont = null;
            _boldItalicFont = null;
            if (_hooked)
            {
                SceneManager.sceneLoaded -= HandleSceneLoaded;
                _hooked = false;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterSceneHook()
        {
            if (_hooked) return;
            _hooked = true;
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ApplyToActiveScene();
        }

        public static bool EnsureRuntimeUiSansFonts(out TMP_FontAsset regular, out TMP_FontAsset bold)
        {
            if (_regularFont == null || _boldFont == null)
            {
                _regularFont    = CreateRuntimeFont("MedievalFonts/UiSans-Regular",    "UiSans-Regular SDF Runtime",    false);
                _boldFont       = CreateRuntimeFont("MedievalFonts/UiSans-Bold",       "UiSans-Bold SDF Runtime",       true);
                _italicFont     = CreateRuntimeFont("MedievalFonts/UiSans-Italic",     "UiSans-Italic SDF Runtime",     false);
                _boldItalicFont = CreateRuntimeFont("MedievalFonts/UiSans-BoldItalic", "UiSans-BoldItalic SDF Runtime", true);

                LinkWeightTable(_regularFont, _regularFont, _italicFont, _boldFont, _boldItalicFont);
                LinkWeightTable(_boldFont,    _boldFont,    _boldItalicFont, _boldFont, _boldItalicFont);

                if (_regularFont != null && TMP_Settings.instance != null && TMP_Settings.fallbackFontAssets != null)
                {
                    if (!TMP_Settings.fallbackFontAssets.Contains(_regularFont))
                    {
                        TMP_Settings.fallbackFontAssets.Insert(0, _regularFont);
                    }
                    if (_boldFont != null && !TMP_Settings.fallbackFontAssets.Contains(_boldFont))
                    {
                        TMP_Settings.fallbackFontAssets.Insert(Mathf.Min(1, TMP_Settings.fallbackFontAssets.Count), _boldFont);
                    }
                }
            }

            regular = _regularFont;
            bold = _boldFont;
            return regular != null && bold != null;
        }

        private static TMP_FontAsset CreateRuntimeFont(string resourcePath, string assetName, bool isAlreadyBold)
        {
            Font src = Resources.Load<Font>(resourcePath);
            if (src == null) return null;

            TMP_FontAsset fa = TMP_FontAsset.CreateFontAsset(
                src, 64, 7, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
            if (fa == null) return null;

            fa.name = assetName;
            fa.normalStyle = 0f;
            fa.normalSpacingOffset = 0f;
            fa.boldStyle = isAlreadyBold ? 0f : 0.45f;
            fa.boldSpacing = isAlreadyBold ? 0f : 4f;
            fa.TryAddCharacters(VietnameseGlyphs, out _);
            return fa;
        }

        private static void LinkWeightTable(
            TMP_FontAsset target,
            TMP_FontAsset regular,
            TMP_FontAsset italic,
            TMP_FontAsset bold,
            TMP_FontAsset boldItalic)
        {
            if (target == null) return;
            TMP_FontWeightPair[] table = target.fontWeightTable;
            if (table != null && table.Length >= 8)
            {
                table[0] = new TMP_FontWeightPair { regularTypeface = regular, italicTypeface = italic };
                table[4] = new TMP_FontWeightPair { regularTypeface = regular, italicTypeface = italic };
                table[7] = new TMP_FontWeightPair { regularTypeface = bold,    italicTypeface = boldItalic };
            }
        }

        public static void ApplyToActiveScene()
        {
            if (!EnsureRuntimeUiSansFonts(out TMP_FontAsset regular, out TMP_FontAsset bold))
            {
                return;
            }

            TMP_Text[] allTexts = UnityEngine.Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include);
            for (int i = 0; i < allTexts.Length; i++)
            {
                TMP_Text t = allTexts[i];
                if (t == null) continue;
                string fName = t.font != null ? t.font.name : "";
                if (fName.StartsWith("UiSans", StringComparison.Ordinal)) continue;

                bool useBold = fName.IndexOf("Bold", StringComparison.OrdinalIgnoreCase) >= 0
                    || fName.IndexOf("STIX", StringComparison.OrdinalIgnoreCase) >= 0
                    || (t.fontStyle & FontStyles.Bold) != 0;
                t.font = useBold ? bold : regular;
            }

            foreach (var eq in UnityEngine.Object.FindObjectsByType<EquipmentModalUI>(FindObjectsInactive.Include))
                eq.OverrideFonts(bold, regular);
            foreach (var sh in UnityEngine.Object.FindObjectsByType<ShopModalUI>(FindObjectsInactive.Include))
                sh.OverrideFonts(bold, regular);
            foreach (var st in UnityEngine.Object.FindObjectsByType<StageSelectModalUI>(FindObjectsInactive.Include))
                st.OverrideFonts(bold, regular);
            foreach (var nb in UnityEngine.Object.FindObjectsByType<MistakeNotebookModalUI>(FindObjectsInactive.Include))
                nb.OverrideFonts(bold, regular);
            foreach (var ach in UnityEngine.Object.FindObjectsByType<AchievementsModalUI>(FindObjectsInactive.Include))
                ach.OverrideFonts(bold, regular);
            foreach (var ft in UnityEngine.Object.FindObjectsByType<FloatingTextManager>(FindObjectsInactive.Include))
                ft.OverrideFont(bold);
            foreach (var road in UnityEngine.Object.FindObjectsByType<ForestRoadController>(FindObjectsInactive.Include))
                road.OverrideFont(bold);
            foreach (var camp in UnityEngine.Object.FindObjectsByType<CampHubManager>(FindObjectsInactive.Include))
                camp.worldLabelFont = bold;
            foreach (var bs in UnityEngine.Object.FindObjectsByType<BattleSystem>(FindObjectsInactive.Include))
                bs.lootLabelFont = bold;

            PatchCanvasLayouts();
        }

        private static void PatchCanvasLayouts()
        {
            Canvas[] canvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include);
            for (int c = 0; c < canvases.Length; c++)
            {
                Transform root = canvases[c].transform;
                PatchInstructionsModal(FindChildRecursive(root, "InstructionsModal"));
                PatchQuizCard(FindChildRecursive(root, "QuizCard"));
                PatchSettingsModal(FindChildRecursive(root, "SettingsModal"), 700f, 750f);
                PatchSettingsModal(FindChildRecursive(root, "CampSettingsModal"), 700f, 750f);
                PatchSettingsModal(FindChildRecursive(root, "PauseSettingsModal"), 780f, 800f);

                Transform tut = FindChildRecursive(root, "TutorialCard");
                if (tut is RectTransform tutRt && tutRt.sizeDelta.y < 450f)
                {
                    tutRt.sizeDelta = new Vector2(840f, 460f);
                    Transform stepBody = FindChildRecursive(tut, "StepBody");
                    if (stepBody != null && stepBody.TryGetComponent<TextMeshProUGUI>(out var sbTmp))
                    {
                        sbTmp.enableAutoSizing = true;
                        sbTmp.fontSizeMin = 14f;
                        sbTmp.fontSizeMax = 18f;
                    }
                }

                Transform tip = FindChildRecursive(root, "StatusHoverTooltip");
                if (tip is RectTransform tipRt && tipRt.sizeDelta.y < 200f)
                {
                    tipRt.sizeDelta = new Vector2(380f, 210f);
                    Transform tipBody = FindChildRecursive(tip, "TooltipBody");
                    if (tipBody != null && tipBody.TryGetComponent<TextMeshProUGUI>(out var tbTmp))
                    {
                        tbTmp.enableAutoSizing = true;
                        tbTmp.fontSizeMin = 11.5f;
                        tbTmp.fontSizeMax = 14f;
                    }
                }

                PatchEndCard(FindChildRecursive(root, "VictoryModal"));
                PatchEndCard(FindChildRecursive(root, "GameOverModal"));
            }
        }

        private static void PatchInstructionsModal(Transform modalRoot)
        {
            if (modalRoot == null) return;
            Transform modalCard = FindChildRecursive(modalRoot, "ModalCard");
            if (modalCard is RectTransform cardRt)
            {
                if (cardRt.sizeDelta.y >= 640f) return;
                cardRt.sizeDelta = new Vector2(960f, 660f);
            }

            Transform content = FindChildRecursive(modalRoot, "Content");
            if (content == null) return;

            if (content.TryGetComponent<VerticalLayoutGroup>(out var contentVlg))
            {
                contentVlg.spacing = 12f;
                contentVlg.childControlWidth = true;
                contentVlg.childControlHeight = true;
                contentVlg.childForceExpandWidth = true;
                contentVlg.childForceExpandHeight = false;
            }

            for (int i = 0; i < content.childCount; i++)
            {
                Transform row = content.GetChild(i);
                if (!row.name.StartsWith("RuleCard", StringComparison.Ordinal)) continue;

                if (row.TryGetComponent<LayoutElement>(out var rowLe))
                {
                    rowLe.preferredHeight = -1f;
                    rowLe.minHeight = 84f;
                }

                if (row.TryGetComponent<HorizontalLayoutGroup>(out var hlg))
                {
                    hlg.padding = new RectOffset(18, 18, 14, 14);
                    hlg.spacing = 16f;
                    hlg.childAlignment = TextAnchor.MiddleLeft;
                    hlg.childControlWidth = true;
                    hlg.childControlHeight = true;
                    hlg.childForceExpandWidth = false;
                    hlg.childForceExpandHeight = false;
                }

                Transform iconBox = row.Find("IconBox");
                if (iconBox != null)
                {
                    LayoutElement ibLe = iconBox.GetComponent<LayoutElement>() ?? iconBox.gameObject.AddComponent<LayoutElement>();
                    ibLe.minWidth = 56f;
                    ibLe.preferredWidth = 56f;
                    ibLe.minHeight = 56f;
                    ibLe.preferredHeight = 56f;
                }

                Transform txtCol = row.Find("TextCol");
                if (txtCol != null)
                {
                    LayoutElement tcLe = txtCol.GetComponent<LayoutElement>() ?? txtCol.gameObject.AddComponent<LayoutElement>();
                    tcLe.flexibleWidth = 1f;

                    VerticalLayoutGroup colVlg = txtCol.GetComponent<VerticalLayoutGroup>() ?? txtCol.gameObject.AddComponent<VerticalLayoutGroup>();
                    colVlg.spacing = 5f;
                    colVlg.childAlignment = TextAnchor.UpperLeft;
                    colVlg.childControlWidth = true;
                    colVlg.childControlHeight = true;
                    colVlg.childForceExpandWidth = true;
                    colVlg.childForceExpandHeight = false;

                    Transform tTitle = txtCol.Find("Title");
                    if (tTitle != null && tTitle.TryGetComponent<TextMeshProUGUI>(out var titleTmp))
                    {
                        titleTmp.rectTransform.offsetMin = Vector2.zero;
                        titleTmp.rectTransform.offsetMax = Vector2.zero;
                        titleTmp.fontSize = 18.5f;
                    }
                    Transform tBody = txtCol.Find("Body");
                    if (tBody != null && tBody.TryGetComponent<TextMeshProUGUI>(out var bodyTmp))
                    {
                        bodyTmp.rectTransform.offsetMin = Vector2.zero;
                        bodyTmp.rectTransform.offsetMax = Vector2.zero;
                        bodyTmp.fontSize = 15.5f;
                        bodyTmp.textWrappingMode = TextWrappingModes.Normal;
                        bodyTmp.overflowMode = TextOverflowModes.Overflow;
                    }
                }
            }

            if (content is RectTransform contentRt)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(contentRt);
            }
        }

        private static void PatchQuizCard(Transform quizCard)
        {
            if (quizCard == null) return;
            if (quizCard is RectTransform qCardRt)
            {
                if (qCardRt.sizeDelta.y >= 640f) return;
                qCardRt.sizeDelta = new Vector2(1020f, 650f);
            }

            Transform qInset = quizCard.Find("QuestionInset");
            if (qInset is RectTransform qiRt && qiRt.sizeDelta.y < 150f)
            {
                qiRt.anchoredPosition = new Vector2(0f, -108f);
                qiRt.sizeDelta = new Vector2(-48f, 156f);
            }

            Transform qContent = FindChildRecursive(quizCard, "QuestionContent");
            if (qContent != null && qContent.TryGetComponent<TextMeshProUGUI>(out var qTmp))
            {
                qTmp.textWrappingMode = TextWrappingModes.Normal;
                qTmp.enableAutoSizing = true;
                qTmp.fontSizeMin = 15f;
                qTmp.fontSizeMax = 23f;
                qTmp.overflowMode = TextOverflowModes.Ellipsis;
            }

            Transform gridGo = quizCard.Find("OptionsGrid");
            if (gridGo is RectTransform gRt && gRt.sizeDelta.y < 180f)
            {
                gRt.anchoredPosition = new Vector2(0f, -278f);
                gRt.sizeDelta = new Vector2(-48f, 184f);
                if (gridGo.TryGetComponent<GridLayoutGroup>(out var glg))
                {
                    glg.cellSize = new Vector2(460f, 84f);
                }
            }

            for (int i = 0; i < 4; i++)
            {
                char letter = (char)('A' + i);
                Transform ans = FindChildRecursive(quizCard, $"Option_{letter}");
                Transform ansTxt = ans != null ? ans.Find("AnswerText") : null;
                if (ansTxt != null && ansTxt.TryGetComponent<TextMeshProUGUI>(out var aTmp))
                {
                    aTmp.textWrappingMode = TextWrappingModes.Normal;
                    aTmp.enableAutoSizing = true;
                    aTmp.fontSizeMin = 13.5f;
                    aTmp.fontSizeMax = 19f;
                    aTmp.overflowMode = TextOverflowModes.Ellipsis;
                }
            }

            Transform fbContainer = quizCard.Find("FeedbackContainer");
            if (fbContainer is RectTransform fbRt && fbRt.sizeDelta.y < 134f)
            {
                fbRt.sizeDelta = new Vector2(-48f, 138f);
            }
            Transform fbExpl = FindChildRecursive(quizCard, "FeedbackExplanation");
            if (fbExpl != null && fbExpl.TryGetComponent<TextMeshProUGUI>(out var fbTmp))
            {
                fbTmp.textWrappingMode = TextWrappingModes.Normal;
                fbTmp.enableAutoSizing = true;
                fbTmp.fontSizeMin = 12.5f;
                fbTmp.fontSizeMax = 16f;
                fbTmp.overflowMode = TextOverflowModes.Ellipsis;
            }
        }

        private static void PatchSettingsModal(Transform modalRoot, float width, float height)
        {
            if (modalRoot == null) return;
            Transform modalCard = FindChildRecursive(modalRoot, "ModalCard");
            if (modalCard is RectTransform cardRt && cardRt.sizeDelta.y < height)
            {
                cardRt.sizeDelta = new Vector2(width, height);
            }

            Transform content = FindChildRecursive(modalRoot, "Content");
            if (content == null) return;
            for (int i = 0; i < content.childCount; i++)
            {
                Transform row = content.GetChild(i);
                if (row.name.EndsWith("ToggleRow", StringComparison.Ordinal) && row.TryGetComponent<LayoutElement>(out var le))
                {
                    if (le.preferredHeight < 56f) le.preferredHeight = 56f;
                }
            }
        }

        private static void PatchEndCard(Transform modalRoot)
        {
            if (modalRoot == null) return;
            Transform endCard = FindChildRecursive(modalRoot, "EndCard");
            if (endCard is RectTransform rt && rt.sizeDelta.y < 550f)
            {
                rt.sizeDelta = new Vector2(840f, 560f);
            }
        }

        private static Transform FindChildRecursive(Transform root, string exactName)
        {
            if (root == null) return null;
            if (root.name == exactName) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindChildRecursive(root.GetChild(i), exactName);
                if (found != null) return found;
            }
            return null;
        }
    }
}
