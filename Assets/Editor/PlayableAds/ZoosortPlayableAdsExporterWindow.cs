using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Sprites;
using UnityEngine;

namespace Editor.PlayableAds
{
    public sealed class ZoosortPlayableAdsExporterWindow : EditorWindow
    {
        private const string DefaultTemplateAssetPath = "Assets/Editor/PlayableAds/Templates/Zoosort_TutorialFull.template.html";
        private const string DefaultOutputDirectoryRelativePath = "PlayableAds";
        private const int MaxHtmlSizeBytes = 5 * 1024 * 1024;
        private const string EditorPrefsKeyPrefix = "ZoosortPlayableAdsExporter.";

        private const string StoreUrl = "https://play.google.com/store/apps/details?id=com.multicast.catsort";

        private string _outputDirectoryRelativePath = DefaultOutputDirectoryRelativePath;
        private bool _openOutputFolderAfterExport = true;
        private bool _enableDebugOverlay;

        private TextAsset _htmlTemplate;

        // Reference layout capture (Unity scene)
        private int _designWidth = 1080;
        private int _designHeight = 1920;
        private RectTransform _referenceCanvasRectTransform;

        private RectTransform _level1Column1RectTransform;
        private RectTransform _level1Column2RectTransform;
        private RectTransform _level1Column3RectTransform;
        private RectTransform _level1Column4RectTransform;

        private RectTransform _slot1RectTransform;
        private RectTransform _slot2RectTransform;
        private RectTransform _slot3RectTransform;
        private RectTransform _slot4RectTransform;

        private RectTransform _downloadButtonRectTransform;
        private RectTransform _winButtonRectTransform;
        private RectTransform _winTextRectTransform;

        private RectTransform _hintsPanelRectTransform;

        private RectTransform _fingerRectTransform;
        private RectTransform _fingerTipMarkerRectTransform;

        private CapturedLayout _capturedLayout;

        private Sprite _backgroundSprite;
        private Sprite _columnSpriteCapacity4;
        private Sprite _tutorialFingerSprite;

        private Sprite _downloadButtonSprite;
        private Sprite _level2ButtonSprite;
        private Sprite _level3ButtonSprite;

        private Sprite _chipBaseSprite;
        private Sprite _hintsBackgroundSprite;
        private Vector2 _chipDesiredSize;

        private List<ImageSpriteEntry> _imageCategorySprites = new List<ImageSpriteEntry>();

        private Vector2 _scrollPosition;
        private string _lastExportReport;

        public string OutputDirectoryRelativePath => _outputDirectoryRelativePath;

        [MenuItem("Tools/Playable Ads/ZooSort Export (Tutorial Full)...")]
        public static void Open()
        {
            ZoosortPlayableAdsExporterWindow window = GetWindow<ZoosortPlayableAdsExporterWindow>("ZooSort Playable Export");
            window.minSize = new Vector2(760f, 620f);
        }

        private void OnEnable()
        {
            LoadStateFromEditorPrefs();
            EnsureDefaultTemplateAssigned();
        }

        private void OnDisable()
        {
            SaveStateToEditorPrefs();
        }

        private void OnGUI()
        {
            EnsureDefaultTemplateAssigned();
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField("ZooSort Playable Export (AppLovin single HTML)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Требования:\n" +
                "- Single HTML (один файл .html)\n" +
                "- Все ассеты должны быть инлайн (data-uri base64)\n" +
                "- Лимит веса: 5MB\n",
                MessageType.Info);

            EditorGUI.BeginChangeCheck();

            DrawTemplateSection();
            DrawAssetsSection();
            DrawReferenceLayoutSection();
            DrawOutputSection();
            DrawExportSection();
            DrawReportSection();

            if (EditorGUI.EndChangeCheck())
            {
                SaveStateToEditorPrefs();
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawTemplateSection()
        {
            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField("Template", EditorStyles.boldLabel);
            _htmlTemplate = (TextAsset)EditorGUILayout.ObjectField("HTML Template (TextAsset)", _htmlTemplate, typeof(TextAsset), false);
            EditorGUILayout.HelpBox(
                "В шаблоне должен быть плейсхолдер:\n" +
                "- /*__ASSETS__*/ (будет заменён на `const ASSETS = {...};`)\n" +
                "- /*__STORE_URL__*/ (будет заменён на строковый URL)\n",
                MessageType.None);
        }

        private void EnsureDefaultTemplateAssigned()
        {
            if (_htmlTemplate != null)
            {
                return;
            }

            _htmlTemplate = AssetDatabase.LoadAssetAtPath<TextAsset>(DefaultTemplateAssetPath);
        }

        private void DrawAssetsSection()
        {
            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField("Sprites", EditorStyles.boldLabel);

            _backgroundSprite = (Sprite)EditorGUILayout.ObjectField("Background", _backgroundSprite, typeof(Sprite), false);
            _columnSpriteCapacity4 = (Sprite)EditorGUILayout.ObjectField("Column (Capacity 4)", _columnSpriteCapacity4, typeof(Sprite), false);
            _tutorialFingerSprite = (Sprite)EditorGUILayout.ObjectField("Tutorial Finger", _tutorialFingerSprite, typeof(Sprite), false);

            EditorGUILayout.Space(6f);
            _downloadButtonSprite = (Sprite)EditorGUILayout.ObjectField("Button: Download (bottom)", _downloadButtonSprite, typeof(Sprite), false);
            _level2ButtonSprite = (Sprite)EditorGUILayout.ObjectField("Button: Level 2", _level2ButtonSprite, typeof(Sprite), false);
            _level3ButtonSprite = (Sprite)EditorGUILayout.ObjectField("Button: Level 3 (store)", _level3ButtonSprite, typeof(Sprite), false);

            EditorGUILayout.Space(6f);
            _chipBaseSprite = (Sprite)EditorGUILayout.ObjectField("Chip Base (container for word/image)", _chipBaseSprite, typeof(Sprite), false);
            _hintsBackgroundSprite = (Sprite)EditorGUILayout.ObjectField("Hints Background", _hintsBackgroundSprite, typeof(Sprite), false);
            _chipDesiredSize = EditorGUILayout.Vector2Field("Desired Chip Size (W,H)", _chipDesiredSize);
            EditorGUILayout.HelpBox("Если (0,0) — размер берётся из спрайта Chip Base (natural size). Значения в 'design units' (как на Canvas 1080x1920).", MessageType.None);

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Image category sprites (from Level2.json)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Это спрайты для itemId, которые являются путями Resources (например `Textures/ImageCategories/Fish/...`).\n" +
                "Кнопка Sync добавит/обновит список нужных путей по Level2.json, дальше ты просто проставляешь Sprite.",
                MessageType.None);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Sync from Level2.json", GUILayout.Height(22f)))
                {
                    SyncImageCategorySpritesFromLevel2JsonSafe();
                    SaveStateToEditorPrefs();
                }
            }

            DrawImageCategorySpritesList();
        }

        private void DrawReferenceLayoutSection()
        {
            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField("Reference Layout (optional, recommended)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Чтобы позиции/размеры НЕ 'ехали', можно снять лейаут из референс-сцены.\n" +
                "Собери UI в Unity на Canvas 1080x1920 (портрет), pivot у спрайтов по центру.\n" +
                "Нажми Capture Layout — и экспорт будет использовать эти числа.\n",
                MessageType.None);

            _designWidth = EditorGUILayout.IntField("Design Width", _designWidth);
            _designHeight = EditorGUILayout.IntField("Design Height", _designHeight);
            _referenceCanvasRectTransform = (RectTransform)EditorGUILayout.ObjectField("Reference Canvas (RectTransform)", _referenceCanvasRectTransform, typeof(RectTransform), true);

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Columns", EditorStyles.boldLabel);
            _level1Column1RectTransform = (RectTransform)EditorGUILayout.ObjectField("Level 1 Column 1", _level1Column1RectTransform, typeof(RectTransform), true);
            _level1Column2RectTransform = (RectTransform)EditorGUILayout.ObjectField("Level 1 Column 2", _level1Column2RectTransform, typeof(RectTransform), true);
            _level1Column3RectTransform = (RectTransform)EditorGUILayout.ObjectField("Level 1 Column 3", _level1Column3RectTransform, typeof(RectTransform), true);
            _level1Column4RectTransform = (RectTransform)EditorGUILayout.ObjectField("Level 1 Column 4", _level1Column4RectTransform, typeof(RectTransform), true);

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Slots (4, any one column)", EditorStyles.boldLabel);
            _slot1RectTransform = (RectTransform)EditorGUILayout.ObjectField("Slot 1", _slot1RectTransform, typeof(RectTransform), true);
            _slot2RectTransform = (RectTransform)EditorGUILayout.ObjectField("Slot 2", _slot2RectTransform, typeof(RectTransform), true);
            _slot3RectTransform = (RectTransform)EditorGUILayout.ObjectField("Slot 3", _slot3RectTransform, typeof(RectTransform), true);
            _slot4RectTransform = (RectTransform)EditorGUILayout.ObjectField("Slot 4", _slot4RectTransform, typeof(RectTransform), true);

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("UI", EditorStyles.boldLabel);
            _downloadButtonRectTransform = (RectTransform)EditorGUILayout.ObjectField("Download Button", _downloadButtonRectTransform, typeof(RectTransform), true);
            _winTextRectTransform = (RectTransform)EditorGUILayout.ObjectField("Win Text (YOU WIN)", _winTextRectTransform, typeof(RectTransform), true);
            _winButtonRectTransform = (RectTransform)EditorGUILayout.ObjectField("Win Button (Level 2/3)", _winButtonRectTransform, typeof(RectTransform), true);
            _hintsPanelRectTransform = (RectTransform)EditorGUILayout.ObjectField("Hints Panel (for size/position)", _hintsPanelRectTransform, typeof(RectTransform), true);

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Finger", EditorStyles.boldLabel);
            _fingerRectTransform = (RectTransform)EditorGUILayout.ObjectField("Finger Sprite", _fingerRectTransform, typeof(RectTransform), true);
            _fingerTipMarkerRectTransform = (RectTransform)EditorGUILayout.ObjectField("Finger Tip Marker (empty)", _fingerTipMarkerRectTransform, typeof(RectTransform), true);

            EditorGUILayout.Space(6f);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Capture Layout From Scene", GUILayout.Height(28f)))
                {
                    CaptureLayoutFromSceneSafe();
                }

                if (GUILayout.Button("Clear Captured Layout", GUILayout.Height(28f)))
                {
                    _capturedLayout = null;
                    EditorPrefs.DeleteKey(GetPrefsKey("capturedLayoutJson"));
                }
            }

            if (_capturedLayout != null)
            {
                EditorGUILayout.Space(4f);
                EditorGUILayout.HelpBox(
                    $"Captured layout: design={_capturedLayout.designWidth}x{_capturedLayout.designHeight}\n" +
                    $"SlotsY(bottom->top): [{string.Join(", ", _capturedLayout.slotOffsetsYFromBottom)}]\n" +
                    $"Finger tip offset: ({_capturedLayout.fingerTipOffsetX:0.0}, {_capturedLayout.fingerTipOffsetY:0.0})",
                    MessageType.Info);
            }
        }

        private void CaptureLayoutFromSceneSafe()
        {
            try
            {
                CaptureLayoutFromScene();
                SaveStateToEditorPrefs();
            }
            catch (Exception exception)
            {
                SetReportError($"Capture Layout exception:\n{exception}");
            }
        }

        private void CaptureLayoutFromScene()
        {
            if (_referenceCanvasRectTransform == null)
            {
                throw new InvalidOperationException("Reference Canvas is not assigned.");
            }

            if (_designWidth <= 0 || _designHeight <= 0)
            {
                throw new InvalidOperationException("Design Width/Height must be > 0.");
            }

            ValidateRectTransformAssigned(_level1Column1RectTransform, "Level 1 Column 1");
            ValidateRectTransformAssigned(_level1Column2RectTransform, "Level 1 Column 2");
            ValidateRectTransformAssigned(_level1Column3RectTransform, "Level 1 Column 3");
            ValidateRectTransformAssigned(_level1Column4RectTransform, "Level 1 Column 4");

            ValidateRectTransformAssigned(_slot1RectTransform, "Slot 1");
            ValidateRectTransformAssigned(_slot2RectTransform, "Slot 2");
            ValidateRectTransformAssigned(_slot3RectTransform, "Slot 3");
            ValidateRectTransformAssigned(_slot4RectTransform, "Slot 4");

            ValidateRectTransformAssigned(_downloadButtonRectTransform, "Download Button");
            ValidateRectTransformAssigned(_winTextRectTransform, "Win Text");
            ValidateRectTransformAssigned(_winButtonRectTransform, "Win Button");
            ValidateRectTransformAssigned(_hintsPanelRectTransform, "Hints Panel");

            ValidateRectTransformAssigned(_fingerRectTransform, "Finger Sprite");
            ValidateRectTransformAssigned(_fingerTipMarkerRectTransform, "Finger Tip Marker");

            Vector2 l1c1 = GetCanvasLocalPosition(_referenceCanvasRectTransform, _level1Column1RectTransform);
            Vector2 l1c2 = GetCanvasLocalPosition(_referenceCanvasRectTransform, _level1Column2RectTransform);
            Vector2 l1c3 = GetCanvasLocalPosition(_referenceCanvasRectTransform, _level1Column3RectTransform);
            Vector2 l1c4 = GetCanvasLocalPosition(_referenceCanvasRectTransform, _level1Column4RectTransform);

            Vector2 slot1 = GetCanvasLocalPosition(_referenceCanvasRectTransform, _slot1RectTransform);
            Vector2 slot2 = GetCanvasLocalPosition(_referenceCanvasRectTransform, _slot2RectTransform);
            Vector2 slot3 = GetCanvasLocalPosition(_referenceCanvasRectTransform, _slot3RectTransform);
            Vector2 slot4 = GetCanvasLocalPosition(_referenceCanvasRectTransform, _slot4RectTransform);

            // Compute slot offsets relative to one of the columns (use level1 column1)
            Vector2 baseColumn = l1c1;
            float[] slotOffsetsYFromBottom = new[]
            {
                slot1.y - baseColumn.y,
                slot2.y - baseColumn.y,
                slot3.y - baseColumn.y,
                slot4.y - baseColumn.y
            };
            Array.Sort(slotOffsetsYFromBottom); // bottom->top if pivot is centered and y-up in canvas local

            RectLayout downloadButton = RectLayout.FromRectTransform(_referenceCanvasRectTransform, _downloadButtonRectTransform);
            RectLayout winText = RectLayout.FromRectTransform(_referenceCanvasRectTransform, _winTextRectTransform);
            RectLayout winButton = RectLayout.FromRectTransform(_referenceCanvasRectTransform, _winButtonRectTransform);
            RectLayout hintsPanel = RectLayout.FromRectTransform(_referenceCanvasRectTransform, _hintsPanelRectTransform);

            RectLayout fingerLayout = RectLayout.FromRectTransform(_referenceCanvasRectTransform, _fingerRectTransform);
            Vector2 fingerCenter = GetCanvasLocalPosition(_referenceCanvasRectTransform, _fingerRectTransform);
            Vector2 tip = GetCanvasLocalPosition(_referenceCanvasRectTransform, _fingerTipMarkerRectTransform);
            Vector2 fingerTipOffset = tip - fingerCenter;

            var level1ColumnRects = new[]
            {
                RectLayout.FromRectTransform(_referenceCanvasRectTransform, _level1Column1RectTransform),
                RectLayout.FromRectTransform(_referenceCanvasRectTransform, _level1Column2RectTransform),
                RectLayout.FromRectTransform(_referenceCanvasRectTransform, _level1Column3RectTransform),
                RectLayout.FromRectTransform(_referenceCanvasRectTransform, _level1Column4RectTransform)
            };

            _capturedLayout = new CapturedLayout
            {
                designWidth = _designWidth,
                designHeight = _designHeight,
                columns = new[] { l1c1, l1c2, l1c3, l1c4 },
                level1ColumnRects = level1ColumnRects,
                slotOffsetsYFromBottom = slotOffsetsYFromBottom,
                finger = fingerLayout,
                downloadButton = downloadButton,
                winText = winText,
                winButton = winButton,
                hintsPanel = hintsPanel,
                fingerTipOffsetX = fingerTipOffset.x,
                fingerTipOffsetY = fingerTipOffset.y
            };

            EditorPrefs.SetString(GetPrefsKey("capturedLayoutJson"), JsonUtility.ToJson(_capturedLayout));
        }

        private static void ValidateRectTransformAssigned(RectTransform rectTransform, string label)
        {
            if (rectTransform == null)
            {
                throw new InvalidOperationException($"{label} is not assigned.");
            }
        }

        private static Vector2 GetCanvasLocalPosition(RectTransform canvasRectTransform, RectTransform targetRectTransform)
        {
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, targetRectTransform.position);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRectTransform, screen, null, out Vector2 localPoint);
            return localPoint;
        }

        private void DrawOutputSection()
        {
            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField("Output", EditorStyles.boldLabel);

            string projectRootPath = GetProjectRootPath();
            _outputDirectoryRelativePath = EditorGUILayout.TextField("Output Directory (relative)", _outputDirectoryRelativePath);
            _openOutputFolderAfterExport = EditorGUILayout.ToggleLeft("Open output folder after export", _openOutputFolderAfterExport);
            _enableDebugOverlay = EditorGUILayout.ToggleLeft("Enable debug overlay (export stamp + outlines)", _enableDebugOverlay);

            string root = string.IsNullOrWhiteSpace(_outputDirectoryRelativePath) ? "PlayableAds" : _outputDirectoryRelativePath.Trim();
            string outputDirectoryFullPath = Path.GetFullPath(Path.Combine(projectRootPath, root));
            string tutorialFullFileName = "TutorialFull.html";
            string tutorialShortFileName = "TutorialShort.html";
            string tutorialMediumFileName = "TutorialMedium.html";

            bool looksLikeOldSubfolder =
                root.Replace('\\', '/').EndsWith("/TutorialFull", StringComparison.OrdinalIgnoreCase) ||
                root.Replace('\\', '/').EndsWith("/TutorialShort", StringComparison.OrdinalIgnoreCase) ||
                root.Replace('\\', '/').EndsWith("/TutorialMedium", StringComparison.OrdinalIgnoreCase);
            if (looksLikeOldSubfolder)
            {
                EditorGUILayout.HelpBox(
                    "Похоже, Output Directory указывает на старую структуру (PlayableAds/TutorialFull).\n" +
                    "Сейчас экспорт делает 3 файла в ОДНУ папку, поэтому лучше поставить Output Directory = `PlayableAds`.",
                    MessageType.Warning);
                if (GUILayout.Button("Set Output Directory = PlayableAds", GUILayout.Height(22f)))
                {
                    _outputDirectoryRelativePath = "PlayableAds";
                }
            }

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(Path.Combine(outputDirectoryFullPath, tutorialFullFileName));
            EditorGUILayout.LabelField(Path.Combine(outputDirectoryFullPath, tutorialShortFileName));
            EditorGUILayout.LabelField(Path.Combine(outputDirectoryFullPath, tutorialMediumFileName));

            EditorGUILayout.Space(6f);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Open Output Folder", GUILayout.Height(22f)))
                {
                    Directory.CreateDirectory(outputDirectoryFullPath);
                    EditorUtility.RevealInFinder(outputDirectoryFullPath);
                }

                if (GUILayout.Button("Reset Defaults", GUILayout.Height(22f)))
                {
                    _outputDirectoryRelativePath = DefaultOutputDirectoryRelativePath;
                }
            }
        }

        private void DrawExportSection()
        {
            EditorGUILayout.Space(12f);
            EditorGUILayout.LabelField("Export", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Export 3 scenarios (TutorialFull/Short/Medium)", GUILayout.Height(36f)))
                {
                    TryExportAllScenariosSafe();
                }
            }
        }

        private void TryExportSafe()
        {
            try
            {
                string root = NormalizeOutputRootDirectory(DefaultOutputDirectoryRelativePath);
                TryExportSingleScenario("TutorialFull", Path.Combine(root, "TutorialFull.html"));
            }
            catch (Exception exception)
            {
                SetReportError($"Export exception:\n{exception}");
            }
        }

        private void TryExportAllScenariosSafe()
        {
            try
            {
                TryExportAllScenarios();
            }
            catch (Exception exception)
            {
                SetReportError($"Export exception:\n{exception}");
            }
        }

        private void DrawReportSection()
        {
            if (string.IsNullOrWhiteSpace(_lastExportReport))
            {
                return;
            }

            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField("Last export", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(_lastExportReport, MessageType.None);
        }

        private void TryExportAllScenarios()
        {
            string root = string.IsNullOrWhiteSpace(_outputDirectoryRelativePath) ? "PlayableAds" : _outputDirectoryRelativePath.Trim();
            root = NormalizeOutputRootDirectory(root);

            // Persist normalized root so user doesn't keep exporting into old subfolder.
            _outputDirectoryRelativePath = root;

            string fullPath = TryExportSingleScenario("TutorialFull", Path.Combine(root, "TutorialFull.html"));
            string shortPath = TryExportSingleScenario("TutorialShort", Path.Combine(root, "TutorialShort.html"));
            string mediumPath = TryExportSingleScenario("TutorialMedium", Path.Combine(root, "TutorialMedium.html"));

            if (string.IsNullOrWhiteSpace(fullPath) || string.IsNullOrWhiteSpace(shortPath) || string.IsNullOrWhiteSpace(mediumPath))
            {
                // Error already reported inside TryExportSingleScenario
                return;
            }

            _lastExportReport =
                "OK\n" +
                $"- Exported: TutorialFull, TutorialShort, TutorialMedium\n" +
                $"- Output folder: {Path.GetDirectoryName(fullPath)}\n" +
                $"- TutorialFull: {fullPath}\n" +
                $"- TutorialShort: {shortPath}\n" +
                $"- TutorialMedium: {mediumPath}\n";

            SaveStateToEditorPrefs();

            if (_openOutputFolderAfterExport)
            {
                try
                {
                    EditorUtility.RevealInFinder(Path.GetDirectoryName(fullPath));
                }
                catch
                {
                    // ignore
                }
            }
        }

        private string TryExportSingleScenario(string scenarioName, string outputFileRelativePath)
        {
            _lastExportReport = string.Empty;

            if (_htmlTemplate == null || string.IsNullOrWhiteSpace(_htmlTemplate.text))
            {
                SetReportError("HTML Template is not assigned (TextAsset).");
                return string.Empty;
            }

            var missing = GetMissingRequiredSprites(scenarioName);
            if (missing.Count > 0)
            {
                SetReportError("Missing required sprites:\n- " + string.Join("\n- ", missing));
                return string.Empty;
            }

            // Если ссылки на референс-сцену уже назначены, автозахватываем layout перед экспортом.
            // Это решает кейсы, когда лейаут ещё не был захвачен (или в EditorPrefs лежит старый/битый JSON).
            TryAutoCaptureLayoutBeforeExportSafe();

            string template = _htmlTemplate.text;
            if (!template.Contains("/*__ASSETS__*/", StringComparison.Ordinal))
            {
                SetReportError("Template does not contain required placeholder: /*__ASSETS__*/");
                return string.Empty;
            }
            if (!template.Contains("/*__MODE__*/", StringComparison.Ordinal))
            {
                SetReportError("Template does not contain required placeholder: /*__MODE__*/");
                return string.Empty;
            }

            var assets = BuildAssetsForScenario(scenarioName);

            string assetsJs = BuildAssetsJsObjectDeclaration(assets);
            string resultHtml = template.Replace("/*__ASSETS__*/", assetsJs, StringComparison.Ordinal);
            resultHtml = resultHtml.Replace("/*__STORE_URL__*/", StoreUrl, StringComparison.Ordinal);
            resultHtml = resultHtml.Replace("/*__MODE__*/", scenarioName, StringComparison.Ordinal);
            if (resultHtml.Contains("/*__LAYOUT__*/", StringComparison.Ordinal))
            {
                string layoutJs = BuildLayoutJsObjectDeclaration(_capturedLayout);
                resultHtml = resultHtml.Replace("/*__LAYOUT__*/", layoutJs, StringComparison.Ordinal);
            }
            if (resultHtml.Contains("/*__LEVELS_DATA__*/", StringComparison.Ordinal))
            {
                string levelsJs = BuildLevelsDataJsObjectDeclaration();
                resultHtml = resultHtml.Replace("/*__LEVELS_DATA__*/", levelsJs, StringComparison.Ordinal);
            }
            if (resultHtml.Contains("/*__CHIP_SIZE_OVERRIDES__*/", StringComparison.Ordinal))
            {
                string overridesJs = BuildChipOverridesJs();
                resultHtml = resultHtml.Replace("/*__CHIP_SIZE_OVERRIDES__*/", overridesJs, StringComparison.Ordinal);
            }
            resultHtml = resultHtml.Replace("/*__DEBUG__*/", _enableDebugOverlay ? "true" : "false", StringComparison.Ordinal);
            resultHtml = resultHtml.Replace("/*__EXPORT_STAMP__*/", _enableDebugOverlay ? DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss 'UTC'") : string.Empty, StringComparison.Ordinal);

            int bytes = Encoding.UTF8.GetByteCount(resultHtml);
            if (bytes > MaxHtmlSizeBytes)
            {
                SetReportError($"Export blocked: HTML size is {FormatBytes(bytes)} (limit {FormatBytes(MaxHtmlSizeBytes)}). " +
                               "Нужно уменьшить ассеты (размер/формат/количество).");
                return string.Empty;
            }

            string projectRootPath = GetProjectRootPath();
            string outputFileFullPath = Path.GetFullPath(Path.Combine(projectRootPath, outputFileRelativePath));
            string outputDirectoryFullPath = Path.GetDirectoryName(outputFileFullPath);

            try
            {
                Directory.CreateDirectory(outputDirectoryFullPath);
                File.WriteAllText(outputFileFullPath, resultHtml, Encoding.UTF8);
            }
            catch (Exception exception)
            {
                SetReportError($"Failed to write file:\n{exception}");
                return string.Empty;
            }

            // Do not override _lastExportReport for multi-export; caller will set final report.
            return outputFileFullPath;
        }

        private void TryAutoCaptureLayoutBeforeExportSafe()
        {
            try
            {
                TryAutoCaptureLayoutBeforeExport();
            }
            catch
            {
                // ignore: export can still run with fallback layout
            }
        }

        private void TryAutoCaptureLayoutBeforeExport()
        {
            if (_referenceCanvasRectTransform == null)
            {
                return;
            }

            // Only if all required scene refs are assigned.
            if (_level1Column1RectTransform == null ||
                _level1Column2RectTransform == null ||
                _level1Column3RectTransform == null ||
                _level1Column4RectTransform == null ||
                _slot1RectTransform == null ||
                _slot2RectTransform == null ||
                _slot3RectTransform == null ||
                _slot4RectTransform == null ||
                _downloadButtonRectTransform == null ||
                _winTextRectTransform == null ||
                _winButtonRectTransform == null ||
                _hintsPanelRectTransform == null ||
                _fingerRectTransform == null ||
                _fingerTipMarkerRectTransform == null)
            {
                return;
            }

            CaptureLayoutFromScene();
        }

        private static string NormalizeOutputRootDirectory(string root)
        {
            if (string.IsNullOrWhiteSpace(root))
            {
                return "PlayableAds";
            }

            string normalized = root.Trim().Replace('\\', '/');
            bool endsWithOldScenarioSubfolder =
                normalized.EndsWith("/TutorialFull", StringComparison.OrdinalIgnoreCase) ||
                normalized.EndsWith("/TutorialShort", StringComparison.OrdinalIgnoreCase) ||
                normalized.EndsWith("/TutorialMedium", StringComparison.OrdinalIgnoreCase);

            if (!endsWithOldScenarioSubfolder)
            {
                return root.Trim();
            }

            string parent = Path.GetDirectoryName(normalized.Replace('/', Path.DirectorySeparatorChar));
            if (string.IsNullOrWhiteSpace(parent))
            {
                return "PlayableAds";
            }

            // Convert back to relative path style
            return parent.Replace(Path.DirectorySeparatorChar, '/');
        }

        private Dictionary<string, string> BuildAssetsForScenario(string scenarioName)
        {
            // Always needed (all scenarios)
            var assets = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["backgroundSprite"] = EncodeSpriteAsPngDataUri(_backgroundSprite),
                // Важно: у колонки часто есть "1px seam" из-за атлас-паддинга/билинейной фильтрации при скейле.
                // Чистим внешний бордер по альфе, как и для пальца.
                ["columnSpriteCapacity4"] = EncodeSpriteAsPngDataUri(_columnSpriteCapacity4, true),
                ["tutorialFingerSprite"] = EncodeSpriteAsPngDataUri(_tutorialFingerSprite, true),
                ["downloadButtonSprite"] = EncodeSpriteAsPngDataUri(_downloadButtonSprite),
                ["chipBaseSprite"] = EncodeSpriteAsPngDataUri(_chipBaseSprite),
                ["hintsBackgroundSprite"] = EncodeSpriteAsPngDataUri(_hintsBackgroundSprite),
            };

            // For the first test, keep 3 scenarios but export identical logic:
            // Level1 (tutor) -> Level2 -> Store.
            assets["level2ButtonSprite"] = EncodeSpriteAsPngDataUri(_level2ButtonSprite);
            assets["level3ButtonSprite"] = EncodeSpriteAsPngDataUri(_level3ButtonSprite);

            // Image-category sprites (resourcesPath -> png data uri)
            if (_imageCategorySprites != null)
            {
                for (int i = 0; i < _imageCategorySprites.Count; i++)
                {
                    var entry = _imageCategorySprites[i];
                    if (entry == null || string.IsNullOrWhiteSpace(entry.resourcesPath) || entry.sprite == null)
                    {
                        continue;
                    }

                    // Key must be stable and JS-safe.
                    // We'll use a prefix and escape slashes.
                    string key = "img_" + entry.resourcesPath.Replace("/", "_", StringComparison.Ordinal);
                    if (!assets.ContainsKey(key))
                    {
                        assets[key] = EncodeSpriteAsPngDataUri(entry.sprite);
                    }
                }
            }

            return assets;
        }

        private List<string> GetMissingRequiredSprites(string scenarioName)
        {
            var missing = new List<string>();
            if (_backgroundSprite == null) missing.Add("Background");
            if (_columnSpriteCapacity4 == null) missing.Add("Column (Capacity 4)");
            if (_tutorialFingerSprite == null) missing.Add("Tutorial Finger");
            if (_downloadButtonSprite == null) missing.Add("Button: Download (bottom)");
            if (_chipBaseSprite == null) missing.Add("Chip Base (container for word/image)");
            if (_hintsBackgroundSprite == null) missing.Add("Hints Background");
            if (_level2ButtonSprite == null) missing.Add("Button: Level 2");
            if (_level3ButtonSprite == null) missing.Add("Button: Level 3 (store)");

            if (_imageCategorySprites != null)
            {
                for (int i = 0; i < _imageCategorySprites.Count; i++)
                {
                    var entry = _imageCategorySprites[i];
                    if (entry == null || string.IsNullOrWhiteSpace(entry.resourcesPath))
                    {
                        continue;
                    }
                    if (entry.sprite == null)
                    {
                        missing.Add($"Image: {entry.resourcesPath}");
                    }
                }
            }

            return missing;
        }

        private static string BuildAssetsJsObjectDeclaration(Dictionary<string, string> assets)
        {
            var stringBuilder = new StringBuilder(assets.Count * 256);
            // ES5-safe object literal:
            // - keys are quoted to avoid any identifier restrictions (unicode/special chars)
            // - no trailing comma (older WebViews may choke)
            stringBuilder.AppendLine("var ASSETS = {");
            int index = 0;
            foreach (var kv in assets)
            {
                stringBuilder.Append("  ");
                stringBuilder.Append('"');
                stringBuilder.Append(EscapeJsString(kv.Key));
                stringBuilder.Append("\": ");
                stringBuilder.Append('"');
                stringBuilder.Append(EscapeJsString(kv.Value));
                stringBuilder.Append('"');
                index++;
                stringBuilder.AppendLine(index < assets.Count ? "," : string.Empty);
            }
            stringBuilder.AppendLine("};");
            return stringBuilder.ToString();
        }

        private static string EscapeJsString(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            return value
                .Replace("\\", "\\\\", StringComparison.Ordinal)
                .Replace("\"", "\\\"", StringComparison.Ordinal);
        }

        private static string EncodeSpriteAsPngDataUri(Sprite sprite)
        {
            return EncodeSpriteAsPngDataUri(sprite, false);
        }

        private static string EncodeSpriteAsPngDataUri(Sprite sprite, bool clearOuterBorder)
        {
            Texture2D readable = null;
            try
            {
                readable = ExtractSpriteToRgba32(sprite);
                if (readable == null)
                {
                    throw new InvalidOperationException($"Failed to extract sprite '{sprite?.name ?? "<null>"}' to RGBA32 texture.");
                }

                // Special case: some sprites (notably the tutorial finger) may contain non-zero-alpha pixels
                // at the outermost edges due to atlas padding / import quirks. This produces straight "frame"
                // lines when scaled. Clearing outer border fixes it.
                if (clearOuterBorder)
                {
                    ClearOuterBorderAlpha(readable, 2);
                }

                byte[] pngBytes = readable.EncodeToPNG();
                if (pngBytes == null || pngBytes.Length <= 0)
                {
                    throw new InvalidOperationException($"Failed to encode sprite '{sprite.name}' to PNG.");
                }

                string base64 = Convert.ToBase64String(pngBytes);
                return "data:image/png;base64," + base64;
            }
            finally
            {
                if (readable != null)
                {
                    UnityEngine.Object.DestroyImmediate(readable);
                }
            }
        }

        private static Texture2D ExtractSpriteToRgba32(Sprite sprite)
        {
            if (sprite == null || sprite.texture == null)
            {
                return null;
            }

            Texture2D sourceTexture = sprite.texture;
            Rect rect = sprite.textureRect;
            int width = Mathf.Max(1, Mathf.RoundToInt(rect.width));
            int height = Mathf.Max(1, Mathf.RoundToInt(rect.height));

            // Важно:
            // - EncodeToPNG не поддерживает compressed форматы
            // - многие UI-спрайты могут лежать в атласе, поэтому всегда вырезаем textureRect
            RenderTexture renderTexture = null;
            RenderTexture previous = null;
            try
            {
                renderTexture = RenderTexture.GetTemporary(sourceTexture.width, sourceTexture.height, 0, RenderTextureFormat.ARGB32);
                Graphics.Blit(sourceTexture, renderTexture);

                previous = RenderTexture.active;
                RenderTexture.active = renderTexture;

                var cropped = new Texture2D(width, height, TextureFormat.RGBA32, false);
                cropped.ReadPixels(new Rect(rect.x, rect.y, rect.width, rect.height), 0, 0);
                cropped.Apply(false, false);

                Texture2D rotated = ApplyPackingRotationIfNeeded(sprite, cropped);
                ApplyAlphaBleedToTransparentPixels(rotated, 2);
                return rotated;
            }
            finally
            {
                RenderTexture.active = previous;
                if (renderTexture != null)
                {
                    RenderTexture.ReleaseTemporary(renderTexture);
                }
            }
        }

        private static Texture2D ApplyPackingRotationIfNeeded(Sprite sprite, Texture2D texture)
        {
            if (sprite == null || texture == null)
            {
                return texture;
            }

            // Если спрайт не packed — rotation будет None.
            var rotation = sprite.packingRotation;
            if (rotation == SpritePackingRotation.None)
            {
                return texture;
            }

            // Мы будем пересобирать пиксели в новый Texture2D и уничтожать исходный.
            Color32[] src = texture.GetPixels32();
            int w = texture.width;
            int h = texture.height;

            Texture2D result;
            Color32[] dst;

            switch (rotation)
            {
                case SpritePackingRotation.FlipHorizontal:
                    result = new Texture2D(w, h, TextureFormat.RGBA32, false);
                    dst = new Color32[src.Length];
                    for (int y = 0; y < h; y++)
                    {
                        for (int x = 0; x < w; x++)
                        {
                            dst[y * w + x] = src[y * w + (w - 1 - x)];
                        }
                    }
                    result.SetPixels32(dst);
                    result.Apply(false, false);
                    UnityEngine.Object.DestroyImmediate(texture);
                    return result;

                case SpritePackingRotation.FlipVertical:
                    result = new Texture2D(w, h, TextureFormat.RGBA32, false);
                    dst = new Color32[src.Length];
                    for (int y = 0; y < h; y++)
                    {
                        for (int x = 0; x < w; x++)
                        {
                            dst[y * w + x] = src[(h - 1 - y) * w + x];
                        }
                    }
                    result.SetPixels32(dst);
                    result.Apply(false, false);
                    UnityEngine.Object.DestroyImmediate(texture);
                    return result;

                case SpritePackingRotation.Rotate180:
                    result = new Texture2D(w, h, TextureFormat.RGBA32, false);
                    dst = new Color32[src.Length];
                    for (int y = 0; y < h; y++)
                    {
                        for (int x = 0; x < w; x++)
                        {
                            dst[y * w + x] = src[(h - 1 - y) * w + (w - 1 - x)];
                        }
                    }
                    result.SetPixels32(dst);
                    result.Apply(false, false);
                    UnityEngine.Object.DestroyImmediate(texture);
                    return result;

            }

            return texture;
        }

        /// <summary>
        /// Заполняет RGB у полностью прозрачных пикселей цветом ближайших непрозрачных соседей (alpha остаётся 0).
        /// Это уменьшает "чёрные/тёмные рамки" при билинейной фильтрации и масштабировании (аналог Unity "Alpha Is Transparency").
        /// </summary>
        private static void ApplyAlphaBleedToTransparentPixels(Texture2D texture, int iterations)
        {
            if (texture == null || iterations <= 0)
            {
                return;
            }

            Color32[] pixels = texture.GetPixels32();
            int w = texture.width;
            int h = texture.height;
            if (pixels == null || pixels.Length != w * h)
            {
                return;
            }

            const byte alphaThreshold = 2; // <= 1..2 считаем полностью прозрачным

            // Iterative dilation: each pass fills transparent pixel RGB from any 4-neighbour with alpha>threshold.
            var tmp = new Color32[pixels.Length];
            for (int it = 0; it < iterations; it++)
            {
                Array.Copy(pixels, tmp, pixels.Length);
                bool changed = false;

                for (int y = 0; y < h; y++)
                {
                    int row = y * w;
                    for (int x = 0; x < w; x++)
                    {
                        int idx = row + x;
                        var p = pixels[idx];
                        if (p.a > alphaThreshold)
                        {
                            continue;
                        }

                        // look for neighbour with alpha
                        bool found = false;
                        Color32 n = default;

                        if (x > 0)
                        {
                            var left = pixels[idx - 1];
                            if (left.a > alphaThreshold) { n = left; found = true; }
                        }
                        if (!found && x < w - 1)
                        {
                            var right = pixels[idx + 1];
                            if (right.a > alphaThreshold) { n = right; found = true; }
                        }
                        if (!found && y > 0)
                        {
                            var down = pixels[idx - w];
                            if (down.a > alphaThreshold) { n = down; found = true; }
                        }
                        if (!found && y < h - 1)
                        {
                            var up = pixels[idx + w];
                            if (up.a > alphaThreshold) { n = up; found = true; }
                        }

                        if (!found)
                        {
                            continue;
                        }

                        tmp[idx] = new Color32(n.r, n.g, n.b, p.a); // keep original alpha (near-zero)
                        changed = true;
                    }
                }

                if (!changed)
                {
                    break;
                }

                var swap = pixels;
                pixels = tmp;
                tmp = swap;
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, false);
        }

        private static void ClearOuterBorderAlpha(Texture2D texture, int borderPixels)
        {
            if (texture == null || borderPixels <= 0)
            {
                return;
            }

            int w = texture.width;
            int h = texture.height;
            if (w <= 0 || h <= 0)
            {
                return;
            }

            borderPixels = Mathf.Clamp(borderPixels, 1, Mathf.Min(w / 2, h / 2));
            Color32[] pixels = texture.GetPixels32();
            if (pixels == null || pixels.Length != w * h)
            {
                return;
            }

            void ClearPixel(int x, int y)
            {
                int idx = y * w + x;
                var p = pixels[idx];
                pixels[idx] = new Color32(p.r, p.g, p.b, 0);
            }

            // Top & bottom bands
            for (int y = 0; y < borderPixels; y++)
            {
                for (int x = 0; x < w; x++) ClearPixel(x, y);
            }
            for (int y = h - borderPixels; y < h; y++)
            {
                for (int x = 0; x < w; x++) ClearPixel(x, y);
            }

            // Left & right bands
            for (int y = borderPixels; y < h - borderPixels; y++)
            {
                for (int x = 0; x < borderPixels; x++) ClearPixel(x, y);
                for (int x = w - borderPixels; x < w; x++) ClearPixel(x, y);
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, false);
        }

        private static string GetProjectRootPath()
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        }

        private void LoadStateFromEditorPrefs()
        {
            _outputDirectoryRelativePath = EditorPrefs.GetString(GetPrefsKey("outputDir"), DefaultOutputDirectoryRelativePath);
            _openOutputFolderAfterExport = EditorPrefs.GetBool(GetPrefsKey("openFolderAfterExport"), true);
            _enableDebugOverlay = EditorPrefs.GetBool(GetPrefsKey("enableDebugOverlay"), false);

            _htmlTemplate = LoadAssetFromPrefs<TextAsset>("templateGuid");

            // Scene refs (RectTransforms) — persisted via GlobalObjectId
            _referenceCanvasRectTransform = LoadSceneObjectFromPrefs<RectTransform>("refCanvasGoid");
            _level1Column1RectTransform = LoadSceneObjectFromPrefs<RectTransform>("l1c1Goid");
            _level1Column2RectTransform = LoadSceneObjectFromPrefs<RectTransform>("l1c2Goid");
            _level1Column3RectTransform = LoadSceneObjectFromPrefs<RectTransform>("l1c3Goid");
            _level1Column4RectTransform = LoadSceneObjectFromPrefs<RectTransform>("l1c4Goid");
            _slot1RectTransform = LoadSceneObjectFromPrefs<RectTransform>("slot1Goid");
            _slot2RectTransform = LoadSceneObjectFromPrefs<RectTransform>("slot2Goid");
            _slot3RectTransform = LoadSceneObjectFromPrefs<RectTransform>("slot3Goid");
            _slot4RectTransform = LoadSceneObjectFromPrefs<RectTransform>("slot4Goid");
            _downloadButtonRectTransform = LoadSceneObjectFromPrefs<RectTransform>("downloadBtnGoid");
            _winTextRectTransform = LoadSceneObjectFromPrefs<RectTransform>("winTextGoid");
            _winButtonRectTransform = LoadSceneObjectFromPrefs<RectTransform>("winBtnGoid");
            _hintsPanelRectTransform = LoadSceneObjectFromPrefs<RectTransform>("hintsPanelGoid");
            _fingerRectTransform = LoadSceneObjectFromPrefs<RectTransform>("fingerGoid");
            _fingerTipMarkerRectTransform = LoadSceneObjectFromPrefs<RectTransform>("fingerTipGoid");

            _backgroundSprite = LoadAssetFromPrefs<Sprite>("backgroundGuid");
            _columnSpriteCapacity4 = LoadAssetFromPrefs<Sprite>("column4Guid");
            _tutorialFingerSprite = LoadAssetFromPrefs<Sprite>("fingerGuid");

            _downloadButtonSprite = LoadAssetFromPrefs<Sprite>("btnDownloadGuid");
            _level2ButtonSprite = LoadAssetFromPrefs<Sprite>("btnLevel2Guid");
            _level3ButtonSprite = LoadAssetFromPrefs<Sprite>("btnLevel3Guid");

            _chipBaseSprite = LoadAssetFromPrefs<Sprite>("chipBaseGuid");
            _hintsBackgroundSprite = LoadAssetFromPrefs<Sprite>("hintsBgGuid");
            _chipDesiredSize = ReadVector2FromPrefs("chipDesiredSize");

            string layoutJson = EditorPrefs.GetString(GetPrefsKey("capturedLayoutJson"), string.Empty);
            if (!string.IsNullOrWhiteSpace(layoutJson))
            {
                try
                {
                    _capturedLayout = JsonUtility.FromJson<CapturedLayout>(layoutJson);
                }
                catch
                {
                    _capturedLayout = null;
                }
            }

            LoadImageCategorySpritesFromPrefs();
            SyncImageCategorySpritesFromLevel2JsonSafe(); // keep list in sync with current Level2.json
        }

        private void SaveStateToEditorPrefs()
        {
            EditorPrefs.SetString(GetPrefsKey("outputDir"), string.IsNullOrWhiteSpace(_outputDirectoryRelativePath) ? DefaultOutputDirectoryRelativePath : _outputDirectoryRelativePath);
            EditorPrefs.SetBool(GetPrefsKey("openFolderAfterExport"), _openOutputFolderAfterExport);
            EditorPrefs.SetBool(GetPrefsKey("enableDebugOverlay"), _enableDebugOverlay);

            SaveAssetToPrefs("templateGuid", _htmlTemplate);

            // Scene refs (RectTransforms) — persisted via GlobalObjectId
            SaveSceneObjectToPrefs("refCanvasGoid", _referenceCanvasRectTransform);
            SaveSceneObjectToPrefs("l1c1Goid", _level1Column1RectTransform);
            SaveSceneObjectToPrefs("l1c2Goid", _level1Column2RectTransform);
            SaveSceneObjectToPrefs("l1c3Goid", _level1Column3RectTransform);
            SaveSceneObjectToPrefs("l1c4Goid", _level1Column4RectTransform);
            SaveSceneObjectToPrefs("slot1Goid", _slot1RectTransform);
            SaveSceneObjectToPrefs("slot2Goid", _slot2RectTransform);
            SaveSceneObjectToPrefs("slot3Goid", _slot3RectTransform);
            SaveSceneObjectToPrefs("slot4Goid", _slot4RectTransform);
            SaveSceneObjectToPrefs("downloadBtnGoid", _downloadButtonRectTransform);
            SaveSceneObjectToPrefs("winTextGoid", _winTextRectTransform);
            SaveSceneObjectToPrefs("winBtnGoid", _winButtonRectTransform);
            SaveSceneObjectToPrefs("hintsPanelGoid", _hintsPanelRectTransform);
            SaveSceneObjectToPrefs("fingerGoid", _fingerRectTransform);
            SaveSceneObjectToPrefs("fingerTipGoid", _fingerTipMarkerRectTransform);

            SaveAssetToPrefs("backgroundGuid", _backgroundSprite);
            SaveAssetToPrefs("column4Guid", _columnSpriteCapacity4);
            SaveAssetToPrefs("fingerGuid", _tutorialFingerSprite);

            SaveAssetToPrefs("btnDownloadGuid", _downloadButtonSprite);
            SaveAssetToPrefs("btnLevel2Guid", _level2ButtonSprite);
            SaveAssetToPrefs("btnLevel3Guid", _level3ButtonSprite);

            SaveAssetToPrefs("chipBaseGuid", _chipBaseSprite);
            SaveAssetToPrefs("hintsBgGuid", _hintsBackgroundSprite);
            WriteVector2ToPrefs("chipDesiredSize", _chipDesiredSize);

            SaveImageCategorySpritesToPrefs();
        }

        private static string GetPrefsKey(string suffix)
        {
            return EditorPrefsKeyPrefix + suffix;
        }

        private static void SaveSceneObjectToPrefs(string prefsSuffix, UnityEngine.Object sceneObject)
        {
            string prefsKey = GetPrefsKey(prefsSuffix);
            if (sceneObject == null)
            {
                EditorPrefs.DeleteKey(prefsKey);
                return;
            }

            GlobalObjectId globalObjectId = GlobalObjectId.GetGlobalObjectIdSlow(sceneObject);
            EditorPrefs.SetString(prefsKey, globalObjectId.ToString());
        }

        private static T LoadSceneObjectFromPrefs<T>(string prefsSuffix) where T : UnityEngine.Object
        {
            string prefsKey = GetPrefsKey(prefsSuffix);
            string globalObjectIdString = EditorPrefs.GetString(prefsKey, string.Empty);
            if (string.IsNullOrWhiteSpace(globalObjectIdString))
            {
                return null;
            }

            if (!GlobalObjectId.TryParse(globalObjectIdString, out GlobalObjectId globalObjectId))
            {
                return null;
            }

            UnityEngine.Object obj = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(globalObjectId);
            return obj as T;
        }

        private static void WriteVector2ToPrefs(string prefsSuffix, Vector2 value)
        {
            EditorPrefs.SetFloat(GetPrefsKey(prefsSuffix + ".x"), value.x);
            EditorPrefs.SetFloat(GetPrefsKey(prefsSuffix + ".y"), value.y);
        }

        private static Vector2 ReadVector2FromPrefs(string prefsSuffix)
        {
            float x = EditorPrefs.GetFloat(GetPrefsKey(prefsSuffix + ".x"), 0f);
            float y = EditorPrefs.GetFloat(GetPrefsKey(prefsSuffix + ".y"), 0f);
            return new Vector2(x, y);
        }

        private static void SaveAssetToPrefs(string prefsSuffix, UnityEngine.Object asset)
        {
            string prefsKey = GetPrefsKey(prefsSuffix);
            if (asset == null)
            {
                EditorPrefs.DeleteKey(prefsKey);
                return;
            }

            string assetPath = AssetDatabase.GetAssetPath(asset);
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                EditorPrefs.DeleteKey(prefsKey);
                return;
            }

            string guid = AssetDatabase.AssetPathToGUID(assetPath);
            if (string.IsNullOrWhiteSpace(guid))
            {
                EditorPrefs.DeleteKey(prefsKey);
                return;
            }

            EditorPrefs.SetString(prefsKey, guid);
        }

        private static T LoadAssetFromPrefs<T>(string prefsSuffix) where T : UnityEngine.Object
        {
            string prefsKey = GetPrefsKey(prefsSuffix);
            string guid = EditorPrefs.GetString(prefsKey, string.Empty);
            if (string.IsNullOrWhiteSpace(guid))
            {
                return null;
            }

            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                return null;
            }

            return AssetDatabase.LoadAssetAtPath<T>(assetPath);
        }

        private void SetReportError(string message)
        {
            _lastExportReport = "ERROR\n" + message;
        }

        private static string FormatBytes(int bytes)
        {
            if (bytes < 1024)
            {
                return $"{bytes} B";
            }

            if (bytes < 1024 * 1024)
            {
                return $"{bytes / 1024f:0.0} KB";
            }

            return $"{bytes / (1024f * 1024f):0.00} MB";
        }

        private static string BuildLayoutJsObjectDeclaration(CapturedLayout layout)
        {
            if (layout == null)
            {
                return "const LAYOUT = null;";
            }

            // Keep it simple and predictable. Numbers are in Unity canvas local coords (center origin, Y up).
            string json = JsonUtility.ToJson(layout);
            return "const LAYOUT = " + json + ";";
        }

        private string BuildChipOverridesJs()
        {
            // Values are in design units. (0,0) means ignore.
            string w = $"{_chipDesiredSize.x:0.###}";
            string h = $"{_chipDesiredSize.y:0.###}";
            return
                "const CHIP_BASE_SIZE_OVERRIDE = {\n" +
                $"  w: {w},\n" +
                $"  h: {h},\n" +
                "};";
        }

        private static string BuildLevelsDataJsObjectDeclaration()
        {
            var level1 = LoadLevelForPlayable("Assets/Resources/LevelsData/Level1.json");
            var level2 = LoadLevelForPlayable("Assets/Resources/LevelsData/Level2.json");

            var data = new PlayableLevelsData
            {
                levels = new[]
                {
                    level1,
                    level2
                }
            };

            string json = JsonUtility.ToJson(data);
            return "const LEVELS_DATA = " + json + ";";
        }

        private static PlayableLevel LoadLevelForPlayable(string assetPath)
        {
            var textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(assetPath);
            if (textAsset == null || string.IsNullOrWhiteSpace(textAsset.text))
            {
                throw new InvalidOperationException($"Level json not found or empty: {assetPath}");
            }

            var wrapper = JsonUtility.FromJson<LevelsJsonWrapperFull>(textAsset.text);
            if (wrapper == null || wrapper.levels == null || wrapper.levels.Length <= 0 || wrapper.levels[0] == null)
            {
                throw new InvalidOperationException($"Level json has unexpected format (no levels[0]): {assetPath}");
            }

            var src = wrapper.levels[0];
            var level = new PlayableLevel
            {
                columns = src.columns,
                categoryBindings = src.categoryBindings
            };
            return level;
        }

        [Serializable]
        private sealed class PlayableLevelsData
        {
            public PlayableLevel[] levels;
        }

        [Serializable]
        private sealed class PlayableLevel
        {
            public ColumnJsonFull[] columns;
            public CategoryBindingJson[] categoryBindings;
        }

        [Serializable]
        private sealed class LevelsJsonWrapperFull
        {
            public PlayableLevelFull[] levels;
        }

        [Serializable]
        private sealed class PlayableLevelFull
        {
            public ColumnJsonFull[] columns;
            public CategoryBindingJson[] categoryBindings;
        }

        [Serializable]
        private sealed class ColumnJsonFull
        {
            public int capacity;
            public int[] chipsTopToBottom;
            public string[] itemIdsTopToBottom;
            public bool hasTutorIdentificator;
            public int tutorObjectId;
        }

        [Serializable]
        private sealed class CategoryBindingJson
        {
            public int chipCategory;
            public int archetype;
            public string categoryId;
            public string displayName;
        }

        [Serializable]
        private sealed class CapturedLayout
        {
            public int designWidth;
            public int designHeight;
            public Vector2[] columns;
            public RectLayout[] level1ColumnRects;
            public float[] slotOffsetsYFromBottom;
            public RectLayout finger;
            public RectLayout downloadButton;
            public RectLayout winText;
            public RectLayout winButton;
            public RectLayout hintsPanel;
            public float fingerTipOffsetX;
            public float fingerTipOffsetY;
        }

        [Serializable]
        private struct RectLayout
        {
            public float centerX;
            public float centerY;
            public float width;
            public float height;

            public static RectLayout FromRectTransform(RectTransform canvasRectTransform, RectTransform rectTransform)
            {
                Vector2 center = GetCanvasLocalPosition(canvasRectTransform, rectTransform);

                // Важно: rectTransform.rect не учитывает localScale/scale родителей.
                // Чтобы получить реальный визуальный размер (как на сцене), берём world corners
                // и переводим их в canvas local space.
                var corners = new Vector3[4];
                rectTransform.GetWorldCorners(corners);

                Vector2 c0 = WorldToCanvasLocal(canvasRectTransform, corners[0]); // bottom-left
                Vector2 c2 = WorldToCanvasLocal(canvasRectTransform, corners[2]); // top-right

                float width = Mathf.Abs(c2.x - c0.x);
                float height = Mathf.Abs(c2.y - c0.y);
                return new RectLayout
                {
                    centerX = center.x,
                    centerY = center.y,
                    width = width,
                    height = height
                };
            }
        }

        [Serializable]
        private sealed class ImageSpriteEntry
        {
            public string resourcesPath;
            public Sprite sprite;
        }

        [Serializable]
        private sealed class ImageSpriteEntryState
        {
            public string resourcesPath;
            public string spriteGuid;
        }

        [Serializable]
        private sealed class ImageSpriteEntriesState
        {
            public ImageSpriteEntryState[] entries;
        }

        private void DrawImageCategorySpritesList()
        {
            if (_imageCategorySprites == null)
            {
                _imageCategorySprites = new List<ImageSpriteEntry>();
            }

            if (_imageCategorySprites.Count <= 0)
            {
                EditorGUILayout.HelpBox("Пока не найдено ни одного image itemId в Level2.json.", MessageType.Info);
                return;
            }

            for (int i = 0; i < _imageCategorySprites.Count; i++)
            {
                var entry = _imageCategorySprites[i];
                if (entry == null)
                {
                    continue;
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(entry.resourcesPath ?? string.Empty, GUILayout.Width(420f));
                    entry.sprite = (Sprite)EditorGUILayout.ObjectField(entry.sprite, typeof(Sprite), false);
                }
            }
        }

        private void SyncImageCategorySpritesFromLevel2JsonSafe()
        {
            try
            {
                SyncImageCategorySpritesFromLevel2Json();
            }
            catch (Exception exception)
            {
                SetReportError($"Sync from Level2.json exception:\n{exception}");
            }
        }

        private void SyncImageCategorySpritesFromLevel2Json()
        {
            var requiredPaths = GetUniqueImageItemIdsFromLevelJsonAsset("Assets/Resources/LevelsData/Level2.json");
            if (requiredPaths.Count <= 0)
            {
                return;
            }

            if (_imageCategorySprites == null)
            {
                _imageCategorySprites = new List<ImageSpriteEntry>();
            }

            var existing = new Dictionary<string, ImageSpriteEntry>(StringComparer.Ordinal);
            for (int i = 0; i < _imageCategorySprites.Count; i++)
            {
                var e = _imageCategorySprites[i];
                if (e == null || string.IsNullOrWhiteSpace(e.resourcesPath))
                {
                    continue;
                }
                if (!existing.ContainsKey(e.resourcesPath))
                {
                    existing.Add(e.resourcesPath, e);
                }
            }

            var updated = new List<ImageSpriteEntry>(requiredPaths.Count);
            foreach (string path in requiredPaths)
            {
                if (existing.TryGetValue(path, out var entry))
                {
                    updated.Add(entry);
                    continue;
                }

                updated.Add(new ImageSpriteEntry
                {
                    resourcesPath = path,
                    sprite = null
                });
            }

            _imageCategorySprites = updated;
        }

        private static HashSet<string> GetUniqueImageItemIdsFromLevelJsonAsset(string assetPath)
        {
            var results = new HashSet<string>(StringComparer.Ordinal);
            var levelText = AssetDatabase.LoadAssetAtPath<TextAsset>(assetPath);
            if (levelText == null || string.IsNullOrWhiteSpace(levelText.text))
            {
                return results;
            }

            var wrapper = JsonUtility.FromJson<LevelsJsonWrapper>(levelText.text);
            if (wrapper == null || wrapper.levels == null || wrapper.levels.Length <= 0)
            {
                return results;
            }

            var level = wrapper.levels[0];
            if (level == null || level.columns == null)
            {
                return results;
            }

            for (int c = 0; c < level.columns.Length; c++)
            {
                var col = level.columns[c];
                if (col == null || col.itemIdsTopToBottom == null)
                {
                    continue;
                }

                for (int i = 0; i < col.itemIdsTopToBottom.Length; i++)
                {
                    string itemId = col.itemIdsTopToBottom[i];
                    if (string.IsNullOrWhiteSpace(itemId))
                    {
                        continue;
                    }

                    // Heuristic: image category uses Resources path (e.g. Textures/ImageCategories/...)
                    if (itemId.Contains("/", StringComparison.Ordinal) && !itemId.Contains(" ", StringComparison.Ordinal))
                    {
                        results.Add(itemId);
                    }
                }
            }

            return results;
        }

        [Serializable]
        private sealed class LevelsJsonWrapper
        {
            public LevelJson[] levels;
        }

        [Serializable]
        private sealed class LevelJson
        {
            public ColumnJson[] columns;
        }

        [Serializable]
        private sealed class ColumnJson
        {
            public string[] itemIdsTopToBottom;
        }

        private void SaveImageCategorySpritesToPrefs()
        {
            if (_imageCategorySprites == null || _imageCategorySprites.Count <= 0)
            {
                EditorPrefs.DeleteKey(GetPrefsKey("imageCategorySpritesJson"));
                return;
            }

            var state = new ImageSpriteEntriesState
            {
                entries = new ImageSpriteEntryState[_imageCategorySprites.Count]
            };

            for (int i = 0; i < _imageCategorySprites.Count; i++)
            {
                var entry = _imageCategorySprites[i];
                string guid = null;
                if (entry != null && entry.sprite != null)
                {
                    string assetPath = AssetDatabase.GetAssetPath(entry.sprite);
                    if (!string.IsNullOrWhiteSpace(assetPath))
                    {
                        guid = AssetDatabase.AssetPathToGUID(assetPath);
                    }
                }

                state.entries[i] = new ImageSpriteEntryState
                {
                    resourcesPath = entry != null ? entry.resourcesPath : string.Empty,
                    spriteGuid = guid
                };
            }

            EditorPrefs.SetString(GetPrefsKey("imageCategorySpritesJson"), JsonUtility.ToJson(state));
        }

        private void LoadImageCategorySpritesFromPrefs()
        {
            _imageCategorySprites = new List<ImageSpriteEntry>();

            string json = EditorPrefs.GetString(GetPrefsKey("imageCategorySpritesJson"), string.Empty);
            if (string.IsNullOrWhiteSpace(json))
            {
                return;
            }

            ImageSpriteEntriesState state;
            try
            {
                state = JsonUtility.FromJson<ImageSpriteEntriesState>(json);
            }
            catch
            {
                return;
            }

            if (state == null || state.entries == null)
            {
                return;
            }

            for (int i = 0; i < state.entries.Length; i++)
            {
                var e = state.entries[i];
                if (e == null || string.IsNullOrWhiteSpace(e.resourcesPath))
                {
                    continue;
                }

                Sprite sprite = null;
                if (!string.IsNullOrWhiteSpace(e.spriteGuid))
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(e.spriteGuid);
                    if (!string.IsNullOrWhiteSpace(assetPath))
                    {
                        sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
                    }
                }

                _imageCategorySprites.Add(new ImageSpriteEntry
                {
                    resourcesPath = e.resourcesPath,
                    sprite = sprite
                });
            }
        }

        private static Vector2 WorldToCanvasLocal(RectTransform canvasRectTransform, Vector3 worldPosition)
        {
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, worldPosition);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRectTransform, screen, null, out Vector2 localPoint);
            return localPoint;
        }
    }
}



