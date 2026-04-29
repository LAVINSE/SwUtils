using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

namespace SWTools
{
    /// <summary>
    /// Unity Localization 패키지의 StringTableCollection을 대상으로
    /// Export / Import / Smart String 일괄 편집 / 유효성 검증 기능을 제공하는 통합 윈도우입니다.
    /// </summary>
    public class SWLocalizationToolsWindow : EditorWindow
    {
        #region 필드 - 공통
        private const string EXPORT_PATH_KEY = "SWTools.Localization.ExportPath";
        private const string EXPORT_FORMAT_KEY = "SWTools.Localization.ExportFormat";
        private const string IMPORT_SAVE_PATH_KEY = "SWTools.Localization.ImportSavePath";
        private const string LAST_TAB_KEY = "SWTools.Localization.LastTab";

        private int selectedTab = 0;
        private static readonly string[] tabNames = { "Export", "Import", "Smart String", "Validator" };

        // 공통: 선택된 컬렉션 / 로케일
        private StringTableCollection selectedCollection;
        private readonly List<Locale> selectedLocales = new();
        private Vector2 localeScroll;
        private bool showLocaleSelection = true;
        #endregion // 필드 - 공통

        #region 필드 - Export 탭
        private enum ExportFormat { TSV, CSV, JSON }

        private string exportPath = "Assets/Localization/Export";
        private ExportFormat exportFormat = ExportFormat.TSV;
        private bool exportIncludeEmptyEntries = false;
        private Vector2 exportPreviewScroll;
        private string cachedExportPreview;
        private bool exportPreviewDirty = true;
        #endregion // 필드 - Export 탭

        #region 필드 - Import 탭
        private enum ImportMode { CreateNew, UpdateExisting }

        private string importTsvData = "";
        private string importCollectionName = "NewLocalizationTable";
        private string importSavePath = "Assets/Localization";
        private Vector2 importTsvScroll;
        private ImportMode importMode = ImportMode.CreateNew;
        private StringTableCollection importExistingCollection;
        private bool importReplaceAllKeys = false;
        private bool importShowSettings = false;
        private string importKeyPrefix = "";
        private bool importEnableSmartString = false;
        private bool importShowPreview = false;
        private List<ImportEntry> importPreviewEntries = new();

        /// <summary>
        /// 가져오기 미리보기에 표시할 단일 로컬라이징 항목입니다.
        /// </summary>
        [Serializable]
        private class ImportEntry
        {
            /// <summary>로컬라이징 키입니다.</summary>
            public string key;
            /// <summary>로케일 코드별 번역 문자열입니다.</summary>
            public Dictionary<string, string> translations = new();
        }
        #endregion // 필드 - Import 탭

        #region 필드 - Smart String 탭
        private string smartSearchFilter = "";
        private bool smartFilterOnlyNonSmart = false;
        private bool smartFilterOnlySmart = false;
        private Vector2 smartKeyScroll;
        private readonly Dictionary<string, bool> smartKeySelections = new();
        #endregion // 필드 - Smart String 탭

        #region 필드 - Validator 탭
        private readonly List<ValidationResult> validationResults = new();
        private bool validatorShowErrors = true;
        private bool validatorShowWarnings = true;
        private bool validatorShowInfo = true;
        private bool validatorShowFilterOptions = false;
        private bool validatorGroupByCollection = false;
        private Vector2 validatorScroll;
        private readonly Dictionary<string, bool> validatorCollectionFoldouts = new();

        private enum ValidationLevel { Info, Warning, Error }

        /// <summary>
        /// 로컬라이징 검증 결과 한 항목입니다.
        /// </summary>
        private class ValidationResult
        {
            /// <summary>검증 메시지입니다.</summary>
            public string message;
            /// <summary>검증 심각도입니다.</summary>
            public ValidationLevel level;
            /// <summary>관련 로컬라이징 키입니다.</summary>
            public string key;
            /// <summary>관련 로케일 코드입니다.</summary>
            public string locale;
            /// <summary>관련 StringTableCollection 이름입니다.</summary>
            public string collectionName;
        }
        #endregion // 필드 - Validator 탭

        /// <summary>
        /// Localization Tools 창을 엽니다.
        /// </summary>
        [MenuItem("SWTools/Localization Tools %#l")]
        public static void ShowWindow()
        {
            SWLocalizationToolsWindow window = GetWindow<SWLocalizationToolsWindow>();
            SWEditorUtils.SetupWindow(window, "SW Localization", "d_BuildSettings.Web.Small", 520, 500);
            window.Show();
        }

        private void OnEnable()
        {
            exportPath = SWEditorUtils.LoadPref(EXPORT_PATH_KEY, exportPath);
            exportFormat = (ExportFormat)SWEditorUtils.LoadPref(EXPORT_FORMAT_KEY, 0);
            importSavePath = SWEditorUtils.LoadPref(IMPORT_SAVE_PATH_KEY, importSavePath);
            selectedTab = SWEditorUtils.LoadPref(LAST_TAB_KEY, 0);

            RefreshLocales();
        }

        private void OnDisable()
        {
            SWEditorUtils.SavePref(EXPORT_PATH_KEY, exportPath);
            SWEditorUtils.SavePref(EXPORT_FORMAT_KEY, (int)exportFormat);
            SWEditorUtils.SavePref(IMPORT_SAVE_PATH_KEY, importSavePath);
            SWEditorUtils.SavePref(LAST_TAB_KEY, selectedTab);
        }

        /// <summary>
        /// LocalizationSettings에서 사용 가능한 모든 Locale을 가져와 기본 선택 상태로 만듭니다.
        /// </summary>
        private void RefreshLocales()
        {
            selectedLocales.Clear();
            var availableLocales = LocalizationEditorSettings.GetLocales();
            if (availableLocales != null)
            {
                selectedLocales.AddRange(availableLocales);
            }
        }

        private void OnGUI()
        {
            int newTab = SWEditorUtils.DrawTabBar(selectedTab, tabNames);
            if (newTab != selectedTab)
            {
                selectedTab = newTab;
                exportPreviewDirty = true;
            }

            switch (selectedTab)
            {
                case 0: DrawExportTab(); break;
                case 1: DrawImportTab(); break;
                case 2: DrawSmartStringTab(); break;
                case 3: DrawValidatorTab(); break;
            }
        }

        #region 공통 UI
        /// <summary>
        /// 컬렉션 선택 ObjectField를 그립니다. 값이 바뀌면 onChanged 콜백이 호출됩니다.
        /// </summary>
        private StringTableCollection DrawCollectionField(string label, StringTableCollection current, Action onChanged = null)
        {
            EditorGUI.BeginChangeCheck();
            StringTableCollection result = EditorGUILayout.ObjectField(label, current, typeof(StringTableCollection), false) as StringTableCollection;
            if (EditorGUI.EndChangeCheck())
            {
                onChanged?.Invoke();
            }
            return result;
        }

        /// <summary>
        /// 로케일 다중 선택 UI를 접을 수 있는 형태로 그립니다.
        /// </summary>
        private void DrawLocaleSelection()
        {
            var availableLocales = LocalizationEditorSettings.GetLocales();
            if (availableLocales == null || availableLocales.Count == 0)
            {
                SWEditorUtils.DrawEmptyNotice("Localization Settings에서 Locale을 찾을 수 없습니다.", MessageType.Warning);
                return;
            }

            showLocaleSelection = EditorGUILayout.Foldout(showLocaleSelection,
                $"대상 언어 ({selectedLocales.Count}/{availableLocales.Count})", true);

            if (!showLocaleSelection) return;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("모두 선택", GUILayout.Height(18)))
            {
                selectedLocales.Clear();
                selectedLocales.AddRange(availableLocales);
                exportPreviewDirty = true;
            }
            if (GUILayout.Button("모두 해제", GUILayout.Height(18)))
            {
                selectedLocales.Clear();
                exportPreviewDirty = true;
            }
            if (GUILayout.Button("새로고침", GUILayout.Height(18)))
            {
                RefreshLocales();
                exportPreviewDirty = true;
            }
            EditorGUILayout.EndHorizontal();

            localeScroll = EditorGUILayout.BeginScrollView(localeScroll, GUILayout.MaxHeight(120));
            foreach (var locale in availableLocales)
            {
                bool isSelected = selectedLocales.Contains(locale);
                bool newSelected = EditorGUILayout.ToggleLeft(
                    $"{locale.LocaleName} ({locale.Identifier.Code})", isSelected);

                if (newSelected != isSelected)
                {
                    if (newSelected) selectedLocales.Add(locale);
                    else selectedLocales.Remove(locale);
                    exportPreviewDirty = true;
                }
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.EndVertical();
        }
        #endregion // 공통 UI

        #region Export 탭
        private void DrawExportTab()
        {
            SWEditorUtils.DrawHeader("Target Collection");

            selectedCollection = DrawCollectionField("Table Collection", selectedCollection, () => exportPreviewDirty = true);

            if (selectedCollection == null)
            {
                SWEditorUtils.DrawEmptyNotice("내보낼 String Table Collection을 선택해주세요.");
                return;
            }

            EditorGUILayout.Space(10);
            SWEditorUtils.DrawHeader("Export Settings");

            EditorGUI.BeginChangeCheck();
            exportFormat = (ExportFormat)EditorGUILayout.EnumPopup("파일 형식", exportFormat);
            exportIncludeEmptyEntries = EditorGUILayout.Toggle("빈 항목 포함", exportIncludeEmptyEntries);
            if (EditorGUI.EndChangeCheck()) exportPreviewDirty = true;

            EditorGUILayout.BeginHorizontal();
            exportPath = EditorGUILayout.TextField("내보내기 경로", exportPath);
            if (GUILayout.Button("찾기", GUILayout.Width(50)))
            {
                string path = EditorUtility.SaveFolderPanel("내보내기 폴더 선택", exportPath, "");
                if (!string.IsNullOrEmpty(path))
                {
                    exportPath = path.StartsWith(Application.dataPath)
                        ? "Assets" + path.Substring(Application.dataPath.Length)
                        : path;
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);
            DrawLocaleSelection();

            EditorGUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();
            using (new SWEditorUtils.GUIEnabledScope(selectedLocales.Count > 0))
            {
                if (GUILayout.Button("내보내기", GUILayout.Height(30)))
                {
                    ExportCollection();
                }
                if (GUILayout.Button("클립보드로 복사 (TSV)", GUILayout.Height(30)))
                {
                    CopyToClipboard();
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);
            SWEditorUtils.DrawHeader("Preview");
            DrawExportPreview();
        }

        private void DrawExportPreview()
        {
            if (selectedCollection == null || selectedLocales.Count == 0)
            {
                SWEditorUtils.DrawEmptyNotice("컬렉션과 언어를 선택하면 미리보기가 표시됩니다.", MessageType.None);
                return;
            }

            if (exportPreviewDirty || cachedExportPreview == null)
            {
                cachedExportPreview = GenerateExportData();
                exportPreviewDirty = false;
            }

            exportPreviewScroll = EditorGUILayout.BeginScrollView(exportPreviewScroll, GUILayout.MaxHeight(180));
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            var previewLines = cachedExportPreview.Split('\n').Take(8).ToArray();
            foreach (var line in previewLines)
            {
                string display = line.Length > 120 ? line.Substring(0, 117) + "..." : line;
                EditorGUILayout.LabelField(display, EditorStyles.miniLabel);
            }

            int totalLines = cachedExportPreview.Split('\n').Length;
            if (totalLines > 8)
            {
                EditorGUILayout.LabelField($"... (총 {totalLines}줄)", EditorStyles.miniLabel);
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// 선택된 형식(TSV/CSV/JSON)에 맞춰 직렬화된 데이터를 반환합니다.
        /// </summary>
        private string GenerateExportData()
        {
            if (exportFormat == ExportFormat.JSON)
            {
                return GenerateJsonExportData();
            }

            var sb = new StringBuilder();
            string separator = exportFormat == ExportFormat.CSV ? "," : "\t";

            // 헤더
            sb.Append("Key");
            foreach (var locale in selectedLocales.OrderBy(l => l.LocaleName))
            {
                sb.Append(separator);
                sb.Append(QuoteIfNeeded(locale.Identifier.Code, separator));
            }
            sb.AppendLine();

            var allKeys = selectedCollection.SharedData.Entries.Select(e => e.Key).OrderBy(k => k);

            foreach (var key in allKeys)
            {
                bool hasAnyValue = false;
                var rowData = new List<string> { QuoteIfNeeded(key, separator) };

                foreach (var locale in selectedLocales.OrderBy(l => l.LocaleName))
                {
                    var table = selectedCollection.GetTable(locale.Identifier) as StringTable;
                    var entry = table?.GetEntry(key);
                    var value = entry?.Value ?? "";
                    if (!string.IsNullOrEmpty(value)) hasAnyValue = true;
                    rowData.Add(QuoteIfNeeded(value, separator));
                }

                if (exportIncludeEmptyEntries || hasAnyValue)
                {
                    sb.AppendLine(string.Join(separator, rowData));
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// JSON 형식으로 직렬화합니다.
        /// </summary>
        private string GenerateJsonExportData()
        {
            var sb = new StringBuilder();
            sb.AppendLine("{");

            var allKeys = selectedCollection.SharedData.Entries.Select(e => e.Key).OrderBy(k => k).ToList();
            var orderedLocales = selectedLocales.OrderBy(l => l.LocaleName).ToList();

            for (int i = 0; i < allKeys.Count; i++)
            {
                string key = allKeys[i];
                var translations = new List<(string code, string value)>();
                bool hasAnyValue = false;

                foreach (var locale in orderedLocales)
                {
                    var table = selectedCollection.GetTable(locale.Identifier) as StringTable;
                    var entry = table?.GetEntry(key);
                    var value = entry?.Value ?? "";
                    if (!string.IsNullOrEmpty(value)) hasAnyValue = true;
                    translations.Add((locale.Identifier.Code, value));
                }

                if (!exportIncludeEmptyEntries && !hasAnyValue) continue;

                sb.Append("  ").Append(JsonEscape(key)).Append(": {");
                for (int j = 0; j < translations.Count; j++)
                {
                    if (j > 0) sb.Append(", ");
                    sb.Append(JsonEscape(translations[j].code)).Append(": ").Append(JsonEscape(translations[j].value));
                }
                sb.Append("}");
                if (i < allKeys.Count - 1) sb.Append(",");
                sb.AppendLine();
            }

            sb.AppendLine("}");
            return sb.ToString();
        }

        private string JsonEscape(string value)
        {
            if (value == null) return "\"\"";
            var sb = new StringBuilder(value.Length + 2);
            sb.Append('"');
            foreach (char c in value)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 0x20) sb.Append($"\\u{(int)c:X4}");
                        else sb.Append(c);
                        break;
                }
            }
            sb.Append('"');
            return sb.ToString();
        }

        private string QuoteIfNeeded(string value, string separator)
        {
            if (exportFormat == ExportFormat.CSV &&
                (value.Contains(",") || value.Contains("\"") || value.Contains("\n")))
            {
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            }
            return value;
        }

        private void ExportCollection()
        {
            if (selectedCollection == null || selectedLocales.Count == 0)
            {
                EditorUtility.DisplayDialog("오류", "컬렉션과 언어를 선택해주세요.", "확인");
                return;
            }

            try
            {
                var data = GenerateExportData();
                var fileName = $"{selectedCollection.name}_{DateTime.Now:yyyyMMdd_HHmmss}";
                var extension = exportFormat.ToString().ToLower();
                var fullPath = Path.Combine(exportPath, $"{fileName}.{extension}");

                string dir = Path.GetDirectoryName(fullPath);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

                File.WriteAllText(fullPath, data, Encoding.UTF8);
                AssetDatabase.Refresh();

                EditorUtility.DisplayDialog("완료", $"파일이 성공적으로 내보내졌습니다:\n{fullPath}", "확인");
                Debug.Log($"[SWLocalization] Export 완료: {fullPath}");
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("오류", $"내보내기 중 오류가 발생했습니다:\n{e.Message}", "확인");
                Debug.LogError($"[SWLocalization] Export 실패: {e}");
            }
        }

        private void CopyToClipboard()
        {
            if (selectedCollection == null || selectedLocales.Count == 0)
            {
                EditorUtility.DisplayDialog("오류", "컬렉션과 언어를 선택해주세요.", "확인");
                return;
            }

            EditorGUIUtility.systemCopyBuffer = GenerateExportData();
            EditorUtility.DisplayDialog("완료",
                "데이터가 클립보드에 복사되었습니다.\n스프레드시트에 붙여넣을 수 있습니다.", "확인");
        }
        #endregion // Export 탭

        #region Import 탭
        private void DrawImportTab()
        {
            SWEditorUtils.DrawHeader("TSV Input");
            EditorGUILayout.HelpBox(
                "스프레드시트에서 복사한 TSV 데이터를 붙여넣으세요.\n" +
                "첫 번째 열은 키, 나머지 열들은 각 언어별 번역입니다.", MessageType.Info);

            importTsvScroll = EditorGUILayout.BeginScrollView(importTsvScroll, GUILayout.Height(150));
            importTsvData = EditorGUILayout.TextArea(importTsvData, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(10);
            SWEditorUtils.DrawHeader("Import Mode");
            importMode = (ImportMode)EditorGUILayout.EnumPopup("모드", importMode);

            if (importMode == ImportMode.CreateNew)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("새 컬렉션 생성", EditorStyles.boldLabel);
                importCollectionName = EditorGUILayout.TextField("컬렉션 이름", importCollectionName);

                EditorGUILayout.BeginHorizontal();
                importSavePath = EditorGUILayout.TextField("저장 경로", importSavePath);
                if (GUILayout.Button("찾기", GUILayout.Width(50)))
                {
                    string picked = EditorUtility.SaveFolderPanel("저장 폴더 선택", importSavePath, "");
                    if (!string.IsNullOrEmpty(picked))
                    {
                        importSavePath = picked.StartsWith(Application.dataPath)
                            ? "Assets" + picked.Substring(Application.dataPath.Length)
                            : picked;
                    }
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
            }
            else
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("기존 컬렉션 업데이트", EditorStyles.boldLabel);
                importExistingCollection = EditorGUILayout.ObjectField(
                    "기존 컬렉션", importExistingCollection, typeof(StringTableCollection), false) as StringTableCollection;

                if (importExistingCollection != null)
                {
                    importReplaceAllKeys = EditorGUILayout.Toggle("모든 기존 키 삭제 후 교체", importReplaceAllKeys);
                    if (importReplaceAllKeys)
                    {
                        EditorGUILayout.HelpBox(
                            "주의: 기존의 모든 키가 삭제되고 새로운 키들로 완전히 교체됩니다.",
                            MessageType.Warning);
                    }
                    else
                    {
                        EditorGUILayout.HelpBox(
                            "TSV에 없는 기존 키는 삭제되고, 새 키가 추가되거나 기존 키의 값이 업데이트됩니다.",
                            MessageType.Info);
                    }
                }
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.Space(5);
            importShowSettings = EditorGUILayout.Foldout(importShowSettings, "고급 설정", true);
            if (importShowSettings)
            {
                EditorGUI.indentLevel++;
                importKeyPrefix = EditorGUILayout.TextField("키 접두사 (선택)", importKeyPrefix);
                importEnableSmartString = EditorGUILayout.Toggle("Smart String 활성화", importEnableSmartString);
                if (importEnableSmartString)
                {
                    EditorGUILayout.HelpBox(
                        "Smart String을 활성화하면 런타임에 동적 포맷팅이 가능합니다. (예: {0}, {name})",
                        MessageType.Info);
                }
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(5);
            DrawLocaleSelection();

            EditorGUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("미리보기", GUILayout.Height(30)))
            {
                PreviewImportData();
            }

            bool canGenerate = !string.IsNullOrEmpty(importTsvData) &&
                ((importMode == ImportMode.CreateNew && !string.IsNullOrEmpty(importCollectionName)) ||
                 (importMode == ImportMode.UpdateExisting && importExistingCollection != null));

            using (new SWEditorUtils.GUIEnabledScope(canGenerate))
            {
                string actionLabel = importMode == ImportMode.CreateNew ? "생성" : "업데이트";
                if (GUILayout.Button(actionLabel, GUILayout.Height(30)))
                {
                    GenerateLocalizationTables();
                }
            }
            EditorGUILayout.EndHorizontal();

            if (importShowPreview && importPreviewEntries.Count > 0)
            {
                EditorGUILayout.Space(10);
                SWEditorUtils.DrawHeader("Preview");
                DrawImportPreview();
            }
        }

        private void DrawImportPreview()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"총 {importPreviewEntries.Count}개 항목", EditorStyles.boldLabel);

            var languages = importPreviewEntries.FirstOrDefault()?.translations.Keys.ToList() ?? new List<string>();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("키", GUILayout.Width(150));
            foreach (var lang in languages)
            {
                EditorGUILayout.LabelField(lang, GUILayout.Width(100));
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(3);

            int previewCount = Mathf.Min(5, importPreviewEntries.Count);
            for (int i = 0; i < previewCount; i++)
            {
                var entry = importPreviewEntries[i];
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(entry.key, GUILayout.Width(150));
                foreach (var lang in languages)
                {
                    string value = entry.translations.TryGetValue(lang, out string v) ? v : "";
                    if (value.Length > 15) value = value.Substring(0, 12) + "...";
                    EditorGUILayout.LabelField(value, GUILayout.Width(100));
                }
                EditorGUILayout.EndHorizontal();
            }

            if (importPreviewEntries.Count > 5)
            {
                EditorGUILayout.LabelField($"... 그리고 {importPreviewEntries.Count - 5}개 더", EditorStyles.miniLabel);
            }

            EditorGUILayout.EndVertical();
        }

        private void PreviewImportData()
        {
            try
            {
                importPreviewEntries = ParseTSVData(importTsvData);
                importShowPreview = true;

                if (importPreviewEntries.Count == 0)
                {
                    EditorUtility.DisplayDialog("오류", "유효한 데이터를 찾을 수 없습니다.", "확인");
                    return;
                }
                Debug.Log($"[SWLocalization] 미리보기: {importPreviewEntries.Count}개 항목 파싱됨");
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("파싱 오류", $"TSV 파싱 중 오류:\n{e.Message}", "확인");
            }
        }

        /// <summary>
        /// TSV 문자열을 파싱합니다.
        /// </summary>
        private List<ImportEntry> ParseTSVData(string data)
        {
            var entries = new List<ImportEntry>();
            if (string.IsNullOrEmpty(data)) return entries;

            var lines = data.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length < 2) return entries;

            var headers = lines[0].Split('\t');
            if (headers.Length < 2) return entries;

            var languages = headers.Skip(1).ToArray();
            int expectedColumnCount = headers.Length;

            int currentLine = 1;
            while (currentLine < lines.Length)
            {
                string fullLine = lines[currentLine];
                int tabCount = fullLine.Count(c => c == '\t');

                while (tabCount < expectedColumnCount - 1 && currentLine + 1 < lines.Length)
                {
                    currentLine++;
                    fullLine += "\n" + lines[currentLine];
                    tabCount = fullLine.Count(c => c == '\t');
                }

                var columns = fullLine.Split('\t');
                if (columns.Length < 2 || string.IsNullOrEmpty(columns[0]))
                {
                    currentLine++;
                    continue;
                }

                var entry = new ImportEntry
                {
                    key = string.IsNullOrEmpty(importKeyPrefix) ? columns[0] : importKeyPrefix + columns[0]
                };

                for (int j = 1; j < columns.Length && j - 1 < languages.Length; j++)
                {
                    entry.translations[languages[j - 1]] = columns[j];
                }

                entries.Add(entry);
                currentLine++;
            }

            return entries;
        }

        /// <summary>
        /// 파싱된 엔트리를 실제 StringTableCollection에 반영합니다.
        /// </summary>
        private void GenerateLocalizationTables()
        {
            try
            {
                EditorUtility.DisplayProgressBar("Localization 처리 중", "데이터 파싱 중...", 0.1f);

                var entries = ParseTSVData(importTsvData);
                if (entries.Count == 0)
                {
                    EditorUtility.DisplayDialog("오류", "유효한 데이터를 찾을 수 없습니다.", "확인");
                    return;
                }

                EditorUtility.DisplayProgressBar("Localization 처리 중", "테이블 컬렉션 준비 중...", 0.3f);

                StringTableCollection collection;
                if (importMode == ImportMode.CreateNew)
                {
                    collection = GetOrCreateStringTableCollection(importCollectionName);
                }
                else
                {
                    collection = importExistingCollection;
                    if (importReplaceAllKeys)
                    {
                        EditorUtility.DisplayProgressBar("Localization 처리 중", "기존 키 삭제 중...", 0.4f);
                        ClearAllKeysFromCollection(collection);
                    }
                }

                EditorUtility.DisplayProgressBar("Localization 처리 중", "데이터 적용 중...", 0.5f);

                var tsvKeys = new HashSet<string>(entries.Select(e => e.key));

                foreach (var entry in entries)
                {
                    var sharedEntry = collection.SharedData.GetEntry(entry.key);
                    if (sharedEntry == null)
                    {
                        sharedEntry = collection.SharedData.AddKey(entry.key);
                    }

                    foreach (var locale in selectedLocales)
                    {
                        var table = collection.GetTable(locale.Identifier) as StringTable;
                        if (table == null)
                        {
                            try
                            {
                                collection.AddNewTable(locale.Identifier);
                                table = collection.GetTable(locale.Identifier) as StringTable;
                            }
                            catch (Exception e)
                            {
                                Debug.LogWarning($"[SWLocalization] 로케일 '{locale.LocaleName}' 테이블 생성 실패: {e.Message}");
                                continue;
                            }
                        }

                        if (table == null) continue;

                        string translation = FindTranslationForLocale(entry, locale);
                        if (string.IsNullOrEmpty(translation)) continue;

                        var existingEntry = table.GetEntry(entry.key);
                        if (existingEntry != null)
                        {
                            existingEntry.Value = translation;
                            if (importEnableSmartString) existingEntry.IsSmart = true;
                        }
                        else
                        {
                            var newEntry = table.AddEntry(sharedEntry.Key, translation);
                            if (importEnableSmartString && newEntry != null) newEntry.IsSmart = true;
                        }
                    }
                }

                if (importMode == ImportMode.UpdateExisting && !importReplaceAllKeys)
                {
                    EditorUtility.DisplayProgressBar("Localization 처리 중", "불필요한 키 삭제 중...", 0.7f);
                    RemoveKeysNotInTSV(collection, tsvKeys);
                }

                EditorUtility.DisplayProgressBar("Localization 처리 중", "저장 중...", 0.9f);

                EditorUtility.SetDirty(collection);
                EditorUtility.SetDirty(collection.SharedData);
                foreach (var locale in selectedLocales)
                {
                    var table = collection.GetTable(locale.Identifier);
                    if (table != null) EditorUtility.SetDirty(table);
                }
                AssetDatabase.SaveAssets();

                EditorUtility.ClearProgressBar();

                string collectionName = importMode == ImportMode.CreateNew ? importCollectionName : collection.name;
                string actionText = importMode == ImportMode.CreateNew ? "생성" : "업데이트";
                string replaceText = (importReplaceAllKeys && importMode == ImportMode.UpdateExisting)
                    ? "\n• 기존 키 전체 교체됨" : "";

                EditorUtility.DisplayDialog("완료",
                    $"'{collectionName}' Localization Table이 성공적으로 {actionText}되었습니다!\n" +
                    $"• {entries.Count}개 키 처리됨\n" +
                    $"• {selectedLocales.Count}개 언어 지원" + replaceText, "확인");

                Debug.Log($"[SWLocalization] '{collectionName}' {actionText} 완료: {entries.Count}개 항목");
            }
            catch (Exception e)
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("오류", $"Localization Table 생성 중 오류:\n{e.Message}", "확인");
                Debug.LogError($"[SWLocalization] Import 실패: {e}");
            }
        }

        private void ClearAllKeysFromCollection(StringTableCollection collection)
        {
            if (collection == null) return;

            var keysToRemove = collection.SharedData.Entries.Select(e => e.Key).ToList();
            foreach (var key in keysToRemove)
            {
                collection.SharedData.RemoveKey(key);
            }

            foreach (var locale in selectedLocales)
            {
                var table = collection.GetTable(locale.Identifier) as StringTable;
                if (table != null) table.Clear();
            }

            Debug.Log($"[SWLocalization] '{collection.name}'에서 {keysToRemove.Count}개 키가 삭제되었습니다.");
        }

        private void RemoveKeysNotInTSV(StringTableCollection collection, HashSet<string> tsvKeys)
        {
            if (collection == null || tsvKeys == null) return;

            var keysToRemove = collection.SharedData.Entries
                .Where(e => !tsvKeys.Contains(e.Key))
                .Select(e => e.Key)
                .ToList();

            if (keysToRemove.Count == 0) return;

            foreach (var key in keysToRemove)
            {
                collection.SharedData.RemoveKey(key);
                foreach (var locale in selectedLocales)
                {
                    var table = collection.GetTable(locale.Identifier) as StringTable;
                    if (table != null && table.GetEntry(key) != null)
                    {
                        table.RemoveEntry(key);
                    }
                }
            }

            Debug.Log($"[SWLocalization] '{collection.name}'에서 TSV에 없는 {keysToRemove.Count}개 키가 삭제되었습니다.");
        }

        private StringTableCollection GetOrCreateStringTableCollection(string name)
        {
            string fullSavePath = Path.Combine(importSavePath, name);
            if (!Directory.Exists(fullSavePath))
            {
                Directory.CreateDirectory(fullSavePath);
                AssetDatabase.Refresh();
            }

            var newCollection = LocalizationEditorSettings.CreateStringTableCollection(name, fullSavePath);

            foreach (var locale in selectedLocales)
            {
                if (newCollection.GetTable(locale.Identifier) != null) continue;
                try
                {
                    newCollection.AddNewTable(locale.Identifier);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[SWLocalization] 로케일 '{locale.LocaleName}' 테이블 추가 실패: {e.Message}");
                }
            }

            return newCollection;
        }

        private string FindTranslationForLocale(ImportEntry entry, Locale locale)
        {
            if (entry.translations.TryGetValue(locale.Identifier.Code, out string exact))
                return exact;

            var candidates = GetLocaleCandidates(locale);

            foreach (var code in candidates)
            {
                if (entry.translations.TryGetValue(code, out string value))
                    return value;
            }

            foreach (var code in candidates)
            {
                var matchingKey = entry.translations.Keys.FirstOrDefault(k =>
                    string.Equals(k, code, StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrEmpty(matchingKey))
                    return entry.translations[matchingKey];
            }

            return "";
        }

        private List<string> GetLocaleCandidates(Locale locale)
        {
            var list = new List<string>();
            var localeCode = locale.Identifier.Code;
            var languageCode = localeCode.Split('-')[0];

            list.Add(localeCode);
            list.Add(languageCode);

            switch (languageCode.ToLower())
            {
                case "ko": list.AddRange(new[] { "KR", "kr", "Korean", "한국어" }); break;
                case "en": list.AddRange(new[] { "EN", "English", "ENG" }); break;
                case "ja": list.AddRange(new[] { "JP", "jp", "Japanese", "JPN" }); break;
                case "fr": list.AddRange(new[] { "FR", "French", "FRA" }); break;
                case "es": list.AddRange(new[] { "ES", "Spanish", "ESP" }); break;
                case "de": list.AddRange(new[] { "DE", "German", "DEU" }); break;
                case "pt": list.AddRange(new[] { "PT", "Portuguese", "POR" }); break;
                case "ru": list.AddRange(new[] { "RU", "Russian", "RUS" }); break;
                case "tr": list.AddRange(new[] { "TR", "Turkish", "TUR" }); break;
                case "it": list.AddRange(new[] { "IT", "Italian", "ITA" }); break;
                case "id": list.AddRange(new[] { "ID", "Indonesian", "IND" }); break;
                case "hi": list.AddRange(new[] { "HI", "Hindi", "HIN" }); break;
                case "ar": list.AddRange(new[] { "AR", "Arabic", "ARA" }); break;
                case "zh":
                    if (localeCode.Contains("Hans") || localeCode.Contains("CN"))
                        list.AddRange(new[] { "CN", "cn", "Chinese", "CHN", "zh-CN" });
                    else if (localeCode.Contains("Hant") || localeCode.Contains("TW"))
                        list.AddRange(new[] { "TW", "tw", "zh-TW", "Traditional" });
                    else
                        list.AddRange(new[] { "CN", "Chinese", "CHN" });
                    break;
            }

            list.Add(locale.LocaleName);
            return list.Distinct().ToList();
        }
        #endregion // Import 탭

        #region Smart String 탭
        private void DrawSmartStringTab()
        {
            EditorGUILayout.HelpBox(
                "기존 Localization 항목들의 Smart String 설정을 일괄 변경할 수 있습니다.",
                MessageType.Info);

            SWEditorUtils.DrawHeader("Target Collection");

            var newCollection = DrawCollectionField("컬렉션", selectedCollection);
            if (newCollection != selectedCollection)
            {
                selectedCollection = newCollection;
                smartKeySelections.Clear();
            }

            if (selectedCollection == null)
            {
                SWEditorUtils.DrawEmptyNotice("String Table Collection을 선택해주세요.", MessageType.Warning);
                return;
            }

            EditorGUILayout.Space(10);
            SWEditorUtils.DrawHeader("Filter");
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            smartSearchFilter = EditorGUILayout.TextField("키 검색", smartSearchFilter);

            EditorGUILayout.BeginHorizontal();
            bool newNonSmart = EditorGUILayout.ToggleLeft("Smart가 아닌 항목만", smartFilterOnlyNonSmart, GUILayout.Width(160));
            bool newSmart = EditorGUILayout.ToggleLeft("Smart인 항목만", smartFilterOnlySmart, GUILayout.Width(160));
            EditorGUILayout.EndHorizontal();

            if (newNonSmart && !smartFilterOnlyNonSmart) smartFilterOnlySmart = false;
            if (newSmart && !smartFilterOnlySmart) smartFilterOnlyNonSmart = false;
            smartFilterOnlyNonSmart = newNonSmart;
            smartFilterOnlySmart = newSmart;
            if (smartFilterOnlyNonSmart && smartFilterOnlySmart) smartFilterOnlySmart = false;

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);
            SWEditorUtils.DrawHeader("Keys");
            DrawSmartKeySelection();

            EditorGUILayout.Space(10);
            SWEditorUtils.DrawHeader("Bulk Actions");

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("선택 항목 Smart 활성화", GUILayout.Height(30)))
            {
                ApplySmartStringToSelected(true);
            }
            if (GUILayout.Button("선택 항목 Smart 비활성화", GUILayout.Height(30)))
            {
                ApplySmartStringToSelected(false);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(3);
            EditorGUILayout.BeginHorizontal();
            if (SWEditorUtils.DangerButton("전체 키 Smart 활성화", "확인",
                $"'{selectedCollection.name}' 컬렉션의 모든 키에 Smart String을 활성화하시겠습니까?",
                "활성화", "취소"))
            {
                ApplySmartStringToAll(true);
            }
            if (SWEditorUtils.DangerButton("전체 키 Smart 비활성화", "확인",
                $"'{selectedCollection.name}' 컬렉션의 모든 키에 Smart String을 비활성화하시겠습니까?",
                "비활성화", "취소"))
            {
                ApplySmartStringToAll(false);
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawSmartKeySelection()
        {
            if (selectedCollection?.SharedData == null) return;

            var allKeys = selectedCollection.SharedData.Entries.Select(e => e.Key).OrderBy(k => k).ToList();

            var filteredKeys = allKeys.Where(key =>
            {
                if (!SWEditorUtils.MatchesFilter(key, smartSearchFilter))
                    return false;

                if (smartFilterOnlyNonSmart || smartFilterOnlySmart)
                {
                    bool isSmart = IsKeySmart(key);
                    if (smartFilterOnlyNonSmart && isSmart) return false;
                    if (smartFilterOnlySmart && !isSmart) return false;
                }
                return true;
            }).ToList();

            if (filteredKeys.Count == 0)
            {
                SWEditorUtils.DrawEmptyNotice("필터 조건에 맞는 키가 없습니다.");
                return;
            }

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"총 {filteredKeys.Count}개 키", EditorStyles.boldLabel);
            if (GUILayout.Button("모두 선택", GUILayout.Width(80), GUILayout.Height(18)))
            {
                foreach (var k in filteredKeys) smartKeySelections[k] = true;
            }
            if (GUILayout.Button("선택 해제", GUILayout.Width(80), GUILayout.Height(18)))
            {
                smartKeySelections.Clear();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(3);

            var firstLocale = selectedLocales.FirstOrDefault();
            StringTable previewTable = firstLocale != null
                ? selectedCollection.GetTable(firstLocale.Identifier) as StringTable
                : null;

            GUIStyle smartStyle = new GUIStyle(EditorStyles.label);
            smartStyle.normal.textColor = new Color(0.3f, 0.8f, 0.3f);
            GUIStyle normalStyle = new GUIStyle(EditorStyles.label);
            normalStyle.normal.textColor = Color.gray;

            smartKeyScroll = EditorGUILayout.BeginScrollView(smartKeyScroll, GUILayout.Height(280));

            foreach (var key in filteredKeys)
            {
                EditorGUILayout.BeginHorizontal();

                bool isSelected = smartKeySelections.TryGetValue(key, out bool sel) && sel;
                bool newSelected = EditorGUILayout.Toggle(isSelected, GUILayout.Width(18));
                if (newSelected != isSelected) smartKeySelections[key] = newSelected;

                EditorGUILayout.LabelField(key, GUILayout.Width(220));

                bool isSmart = IsKeySmartFromTable(previewTable, key);
                EditorGUILayout.LabelField(isSmart ? "✓ Smart" : "○ Normal",
                    isSmart ? smartStyle : normalStyle, GUILayout.Width(70));

                if (previewTable != null)
                {
                    var entry = previewTable.GetEntry(key);
                    if (entry != null)
                    {
                        string preview = entry.Value ?? "";
                        if (preview.Length > 30) preview = preview.Substring(0, 27) + "...";
                        EditorGUILayout.LabelField(preview, EditorStyles.miniLabel);
                    }
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();

            int selectedCount = smartKeySelections.Count(kv => kv.Value);
            if (selectedCount > 0)
            {
                EditorGUILayout.HelpBox($"{selectedCount}개 키가 선택됨", MessageType.None);
            }

            EditorGUILayout.EndVertical();
        }

        private bool IsKeySmart(string key)
        {
            var firstLocale = selectedLocales.FirstOrDefault();
            if (firstLocale == null) return false;
            var table = selectedCollection.GetTable(firstLocale.Identifier) as StringTable;
            return IsKeySmartFromTable(table, key);
        }

        private bool IsKeySmartFromTable(StringTable table, string key)
        {
            if (table == null) return false;
            var entry = table.GetEntry(key);
            return entry != null && entry.IsSmart;
        }

        private void ApplySmartStringToSelected(bool enableSmart)
        {
            var keys = smartKeySelections.Where(kv => kv.Value).Select(kv => kv.Key).ToList();
            if (keys.Count == 0)
            {
                EditorUtility.DisplayDialog("알림", "선택된 키가 없습니다.", "확인");
                return;
            }

            int processed = ApplySmartStringToKeys(keys, enableSmart);
            string action = enableSmart ? "활성화" : "비활성화";
            EditorUtility.DisplayDialog("완료", $"{processed}개 키의 Smart String이 {action}되었습니다.", "확인");
        }

        private void ApplySmartStringToAll(bool enableSmart)
        {
            if (selectedCollection == null) return;
            var allKeys = selectedCollection.SharedData.Entries.Select(e => e.Key).ToList();
            int processed = ApplySmartStringToKeys(allKeys, enableSmart);
            string action = enableSmart ? "활성화" : "비활성화";
            EditorUtility.DisplayDialog("완료", $"총 {processed}개 키의 Smart String이 {action}되었습니다.", "확인");
        }

        private int ApplySmartStringToKeys(List<string> keys, bool enableSmart)
        {
            if (selectedCollection == null || keys.Count == 0) return 0;

            int processed = 0;
            EditorUtility.DisplayProgressBar("Smart String 적용 중", "처리 중...", 0);

            try
            {
                foreach (var locale in selectedLocales)
                {
                    var table = selectedCollection.GetTable(locale.Identifier) as StringTable;
                    if (table == null) continue;

                    foreach (var key in keys)
                    {
                        var entry = table.GetEntry(key);
                        if (entry != null)
                        {
                            entry.IsSmart = enableSmart;
                            processed++;
                        }
                    }
                    EditorUtility.SetDirty(table);
                }

                EditorUtility.SetDirty(selectedCollection);
                EditorUtility.SetDirty(selectedCollection.SharedData);
                AssetDatabase.SaveAssets();

                Debug.Log($"[SWLocalization] Smart String {(enableSmart ? "ON" : "OFF")}: {processed}개 항목 처리됨");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            smartKeySelections.Clear();
            return processed;
        }
        #endregion // Smart String 탭

        #region Validator 탭
        private void DrawValidatorTab()
        {
            SWEditorUtils.DrawHeader("Target");

            selectedCollection = DrawCollectionField("Table Collection", selectedCollection);

            EditorGUILayout.Space(5);
            EditorGUILayout.BeginHorizontal();
            using (new SWEditorUtils.GUIEnabledScope(selectedCollection != null))
            {
                if (GUILayout.Button("검증 실행", GUILayout.Height(28)))
                {
                    ValidateCollection();
                }
            }
            if (GUILayout.Button("모든 컬렉션 검증", GUILayout.Height(28)))
            {
                ValidateAllCollections();
            }
            EditorGUILayout.EndHorizontal();

            if (validationResults.Count == 0) return;

            EditorGUILayout.Space(10);

            validatorShowFilterOptions = EditorGUILayout.Foldout(validatorShowFilterOptions, "표시 옵션", true);
            if (validatorShowFilterOptions)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUI.indentLevel++;

                EditorGUILayout.LabelField("표시할 항목:", EditorStyles.boldLabel);
                validatorShowErrors = EditorGUILayout.Toggle("Error 오류", validatorShowErrors);
                validatorShowWarnings = EditorGUILayout.Toggle("Warning 경고", validatorShowWarnings);
                validatorShowInfo = EditorGUILayout.Toggle("Info 정보", validatorShowInfo);

                bool hasMultipleCollections = validationResults.Select(r => r.collectionName).Distinct().Count() > 1;
                if (hasMultipleCollections)
                {
                    EditorGUILayout.Space(3);
                    validatorGroupByCollection = EditorGUILayout.Toggle("컬렉션별 그룹핑", validatorGroupByCollection);
                }

                EditorGUI.indentLevel--;
                EditorGUILayout.EndVertical();
            }

            var filteredResults = GetFilteredValidationResults();
            EditorGUILayout.Space(5);
            SWEditorUtils.DrawHeader($"Results ({filteredResults.Count}/{validationResults.Count})");
            DrawValidationResults(filteredResults);
        }

        private void ValidateCollection()
        {
            if (selectedCollection == null)
            {
                EditorUtility.DisplayDialog("오류", "검증할 컬렉션을 선택해주세요.", "확인");
                return;
            }

            validationResults.Clear();
            ValidateStringTableCollection(selectedCollection);
            Debug.Log($"[SWLocalization] '{selectedCollection.name}' 검증 완료: {validationResults.Count}개 결과");
        }

        private void ValidateAllCollections()
        {
            validationResults.Clear();
            validatorCollectionFoldouts.Clear();

            var collections = LocalizationEditorSettings.GetStringTableCollections();
            foreach (var c in collections)
            {
                ValidateStringTableCollection(c);
            }
            Debug.Log($"[SWLocalization] 전체 검증 완료: {validationResults.Count}개 결과");
        }

        private void ValidateStringTableCollection(StringTableCollection collection)
        {
            if (collection == null) return;

            var availableLocales = LocalizationEditorSettings.GetLocales();

            // 1. 빈 테이블 / 테이블 누락
            foreach (var locale in availableLocales)
            {
                var table = collection.GetTable(locale.Identifier) as StringTable;
                if (table == null)
                {
                    validationResults.Add(new ValidationResult
                    {
                        message = $"'{locale.LocaleName}' 언어에 대한 테이블이 없습니다.",
                        level = ValidationLevel.Warning,
                        locale = locale.Identifier.Code,
                        collectionName = collection.name
                    });
                    continue;
                }

                if (table.Count == 0)
                {
                    validationResults.Add(new ValidationResult
                    {
                        message = $"'{locale.LocaleName}' 테이블이 비어있습니다.",
                        level = ValidationLevel.Warning,
                        locale = locale.Identifier.Code,
                        collectionName = collection.name
                    });
                }
            }

            // 2. 누락된 번역 / 빈 번역
            var allKeys = collection.SharedData.Entries.Select(e => e.Key).ToList();

            foreach (var key in allKeys)
            {
                var missingLocales = new List<string>();
                var emptyLocales = new List<string>();

                foreach (var locale in availableLocales)
                {
                    var table = collection.GetTable(locale.Identifier) as StringTable;
                    if (table == null)
                    {
                        missingLocales.Add(locale.LocaleName);
                        continue;
                    }

                    var entry = table.GetEntry(key);
                    if (entry == null) missingLocales.Add(locale.LocaleName);
                    else if (string.IsNullOrEmpty(entry.Value)) emptyLocales.Add(locale.LocaleName);
                }

                if (missingLocales.Count > 0)
                {
                    validationResults.Add(new ValidationResult
                    {
                        message = $"[{collection.name}] 키 '{key}'가 다음 언어에서 누락됨: {string.Join(", ", missingLocales)}",
                        level = ValidationLevel.Error,
                        key = key,
                        collectionName = collection.name
                    });
                }

                if (emptyLocales.Count > 0)
                {
                    validationResults.Add(new ValidationResult
                    {
                        message = $"[{collection.name}] 키 '{key}'가 다음 언어에서 비어있음: {string.Join(", ", emptyLocales)}",
                        level = ValidationLevel.Warning,
                        key = key,
                        collectionName = collection.name
                    });
                }
            }

            // 3. 중복 키
            var keyGroups = collection.SharedData.Entries.GroupBy(e => e.Key).Where(g => g.Count() > 1);
            foreach (var group in keyGroups)
            {
                validationResults.Add(new ValidationResult
                {
                    message = $"[{collection.name}] 중복 키 발견: '{group.Key}' ({group.Count()}개)",
                    level = ValidationLevel.Error,
                    key = group.Key,
                    collectionName = collection.name
                });
            }

            // 4. Unity가 자동 생성한 것으로 의심되는 키
            var suspiciousPattern = new Regex(@".+ \d+$");
            foreach (var entry in collection.SharedData.Entries)
            {
                if (suspiciousPattern.IsMatch(entry.Key))
                {
                    validationResults.Add(new ValidationResult
                    {
                        message = $"[{collection.name}] 의심스러운 중복 키: '{entry.Key}' (Unity가 자동 생성한 것으로 보임)",
                        level = ValidationLevel.Warning,
                        key = entry.Key,
                        collectionName = collection.name
                    });
                }
            }

            // 5. 긴 텍스트
            foreach (var locale in availableLocales)
            {
                var table = collection.GetTable(locale.Identifier) as StringTable;
                if (table == null) continue;

                foreach (var entry in table.Values)
                {
                    if (!string.IsNullOrEmpty(entry.Value) && entry.Value.Length > 100)
                    {
                        validationResults.Add(new ValidationResult
                        {
                            message = $"긴 텍스트 ({entry.Value.Length}자): '{entry.Key}' in {locale.LocaleName}",
                            level = ValidationLevel.Info,
                            key = entry.Key,
                            locale = locale.Identifier.Code,
                            collectionName = collection.name
                        });
                    }
                }
            }
        }

        private List<ValidationResult> GetFilteredValidationResults()
        {
            return validationResults.Where(r =>
                (r.level == ValidationLevel.Error && validatorShowErrors) ||
                (r.level == ValidationLevel.Warning && validatorShowWarnings) ||
                (r.level == ValidationLevel.Info && validatorShowInfo)
            ).ToList();
        }

        private void DrawValidationResults(List<ValidationResult> filteredResults)
        {
            int errorCount = filteredResults.Count(r => r.level == ValidationLevel.Error);
            int warningCount = filteredResults.Count(r => r.level == ValidationLevel.Warning);
            int infoCount = filteredResults.Count(r => r.level == ValidationLevel.Info);

            EditorGUILayout.BeginHorizontal();
            if (errorCount > 0 && validatorShowErrors)
                EditorGUILayout.LabelField($"Error: {errorCount}", GUILayout.Width(90));
            if (warningCount > 0 && validatorShowWarnings)
                EditorGUILayout.LabelField($"Warning: {warningCount}", GUILayout.Width(100));
            if (infoCount > 0 && validatorShowInfo)
                EditorGUILayout.LabelField($"Info: {infoCount}", GUILayout.Width(80));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(3);

            validatorScroll = EditorGUILayout.BeginScrollView(validatorScroll);

            bool hasMultiple = filteredResults.Select(r => r.collectionName).Distinct().Count() > 1;
            if (validatorGroupByCollection && hasMultiple)
            {
                DrawGroupedValidationResults(filteredResults);
            }
            else
            {
                foreach (var r in filteredResults.OrderBy(r => r.level))
                {
                    DrawValidationResult(r);
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawGroupedValidationResults(List<ValidationResult> results)
        {
            var groups = results.GroupBy(r => r.collectionName ?? "Unknown").OrderBy(g => g.Key);

            foreach (var group in groups)
            {
                string name = group.Key;
                if (!validatorCollectionFoldouts.ContainsKey(name))
                    validatorCollectionFoldouts[name] = true;

                var list = group.ToList();
                int e = list.Count(r => r.level == ValidationLevel.Error);
                int w = list.Count(r => r.level == ValidationLevel.Warning);
                int i = list.Count(r => r.level == ValidationLevel.Info);

                string header = $"{name} ({list.Count})";
                if (e > 0) header += $" E:{e}";
                if (w > 0) header += $" W:{w}";
                if (i > 0) header += $" I:{i}";

                validatorCollectionFoldouts[name] = EditorGUILayout.Foldout(
                    validatorCollectionFoldouts[name], header, true);

                if (validatorCollectionFoldouts[name])
                {
                    EditorGUI.indentLevel++;
                    foreach (var r in list.OrderBy(x => x.level))
                    {
                        DrawValidationResult(r);
                    }
                    EditorGUI.indentLevel--;
                }
                EditorGUILayout.Space(3);
            }
        }

        private void DrawValidationResult(ValidationResult result)
        {
            Color bgColor = result.level switch
            {
                ValidationLevel.Error => new Color(1f, 0.8f, 0.8f),
                ValidationLevel.Warning => new Color(1f, 1f, 0.8f),
                _ => new Color(0.8f, 0.9f, 1f)
            };

            using (new SWEditorUtils.GUIBgColorScope(bgColor))
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                string icon = result.level switch
                {
                    ValidationLevel.Error => "[ERROR]",
                    ValidationLevel.Warning => "[WARNING]",
                    _ => "[INFO]"
                };

                EditorGUILayout.LabelField($"{icon} {result.message}", EditorStyles.wordWrappedLabel);

                if (!string.IsNullOrEmpty(result.key) || !string.IsNullOrEmpty(result.locale))
                {
                    EditorGUILayout.BeginHorizontal();
                    if (!string.IsNullOrEmpty(result.key))
                        EditorGUILayout.LabelField($"키: {result.key}", EditorStyles.miniLabel);
                    if (!string.IsNullOrEmpty(result.locale))
                        EditorGUILayout.LabelField($"언어: {result.locale}", EditorStyles.miniLabel);
                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.EndVertical();
            }
        }
        #endregion // Validator 탭
    }
}
