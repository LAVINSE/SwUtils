using System.Collections.Generic;
using System.Linq;
using SWUtils;
using UnityEditor;
using UnityEngine;

namespace SWTools
{
    /// <summary>
    /// 엑셀에서 복사한 TSV 데이터를 ScriptableObject의 [SWTableSheet] 리스트에 적용하는 에디터 창.
    /// 왼쪽 패널에서 ScriptableObject 목록을 관리하고, 오른쪽에서 TSV 입력/미리보기/적용을 수행합니다.
    /// </summary>
    public class SWExcelTableImporterWindow : EditorWindow
    {
        #region 필드
        private const string LAST_SHEET_KEY = "SWTools.ExcelTable.LastSheet";
        private const string REGISTERED_OBJECTS_KEY = "SWTools.ExcelTable.RegisteredObjects";
        private const float LEFT_PANEL_WIDTH = 300f;
        private const float SPLITTER_WIDTH = 2f;

        // 왼쪽 패널: ScriptableObject 리스트
        private List<string> registeredObjectGuids = new();
        private readonly List<RegisteredObjectCache> objectCaches = new();
        private bool needsCacheRebuild = true;
        private Vector2 objectListScroll;
        private int selectedObjectIndex = -1;

        // 오른쪽 패널: 기존 기능
        private ScriptableObject targetObject;
        private string selectedSheetName = "";
        private string tableText = "";
        private Vector2 inputScroll;
        private Vector2 previewScroll;
        private bool showPreview = true;

        private List<SWExcelTableParser.SheetFieldInfo> sheetFields = new();
        private SWExcelTableParser.ParseResult previewResult;

        /// <summary>
        /// 등록된 ScriptableObject의 표시용 캐시 정보입니다.
        /// </summary>
        private class RegisteredObjectCache
        {
            /// <summary>에셋 GUID입니다.</summary>
            public string guid;
            /// <summary>에셋 경로입니다.</summary>
            public string path;
            /// <summary>캐시된 ScriptableObject 에셋입니다.</summary>
            public ScriptableObject asset;
            /// <summary>목록에 표시할 이름입니다.</summary>
            public string displayName;
            /// <summary>에셋이 현재 존재하는지 여부입니다.</summary>
            public bool exists;
        }
        #endregion // 필드

        #region 초기화
        /// <summary>
        /// Excel Table Importer 창을 엽니다.
        /// </summary>
        [MenuItem("SWTools/Excel Table Importer")]
        public static void ShowWindow()
        {
            SWExcelTableImporterWindow window = GetWindow<SWExcelTableImporterWindow>();
            SWEditorUtils.SetupWindow(window, "SW Excel Table", "d_TextAsset Icon", 740, 520);
            window.Show();
        }

        private void OnEnable()
        {
            selectedSheetName = EditorPrefs.GetString(LAST_SHEET_KEY, selectedSheetName);
            LoadRegisteredObjects();
            needsCacheRebuild = true;
        }

        private void OnDisable()
        {
            EditorPrefs.SetString(LAST_SHEET_KEY, selectedSheetName);
            SaveRegisteredObjects();
        }
        #endregion // 초기화

        #region GUI
        private void OnGUI()
        {
            if (needsCacheRebuild) RebuildObjectCache();

            EditorGUILayout.BeginHorizontal();

            // ── 왼쪽 패널 ──
            DrawLeftPanel();

            // ── 구분선 ──
            Rect splitter = EditorGUILayout.GetControlRect(false, GUILayout.Width(SPLITTER_WIDTH), GUILayout.ExpandHeight(true));
            EditorGUI.DrawRect(splitter, SWEditorUtils.HeaderLineColor);

            // ── 오른쪽 패널 ──
            DrawRightPanel();

            EditorGUILayout.EndHorizontal();
        }
        #endregion // GUI

        #region 왼쪽 패널
        private void DrawLeftPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(LEFT_PANEL_WIDTH));

            SWEditorUtils.DrawHeader("대상 목록");

            // 리스트
            if (objectCaches.Count == 0)
            {
                SWEditorUtils.DrawEmptyNotice(
                    "아래 영역에 ScriptableObject를\n드래그하거나 + 버튼으로\n추가하세요.",
                    MessageType.Info);
            }
            else
            {
                DrawObjectList();
            }

            GUILayout.FlexibleSpace();

            // 추가 영역 (하단)
            DrawObjectAddArea();

            EditorGUILayout.Space(5);

            // 하단 버튼
            DrawLeftPanelFooter();

            EditorGUILayout.EndVertical();
        }

        private void DrawObjectAddArea()
        {
            // 드래그 앤 드롭
            var dropped = SWEditorUtils.DrawDropArea<ScriptableObject>(
                "여기에 SO를 드래그", 60f);
            if (dropped != null)
            {
                foreach (var so in dropped)
                    RegisterObject(so);
            }

            // Selection에서 추가
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("+  선택 항목 추가", GUILayout.Height(20)))
            {
                foreach (var obj in Selection.objects)
                {
                    if (obj is ScriptableObject so)
                        RegisterObject(so);
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawObjectList()
        {
            objectListScroll = EditorGUILayout.BeginScrollView(objectListScroll);

            for (int i = 0; i < objectCaches.Count; i++)
            {
                var cache = objectCaches[i];
                bool isSelected = (i == selectedObjectIndex);

                // 선택 상태 배경
                Rect rowRect = EditorGUILayout.BeginHorizontal(
                    isSelected ? "SelectionRect" : EditorStyles.helpBox);

                // 아이콘
                Texture icon = cache.asset != null
                    ? AssetPreview.GetMiniThumbnail(cache.asset)
                    : null;
                if (icon != null)
                {
                    Rect iconRect = GUILayoutUtility.GetRect(18, 18,
                        GUILayout.Width(18), GUILayout.Height(18));
                    GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit);
                }

                // 이름 (클릭으로 선택)
                using (new SWEditorUtils.GUIColorScope(
                    cache.exists ? Color.white : SWEditorUtils.ErrorColor))
                {
                    if (GUILayout.Button(cache.displayName, EditorStyles.label,
                        GUILayout.ExpandWidth(true), GUILayout.Height(18)))
                    {
                        SelectObjectAtIndex(i);
                    }
                }

                // 삭제 버튼
                if (GUILayout.Button("✕", GUILayout.Width(18), GUILayout.Height(18)))
                {
                    RemoveObjectAtIndex(i);
                    GUIUtility.ExitGUI();
                }

                EditorGUILayout.EndHorizontal();

                // 더블클릭 시 Ping
                if (Event.current.type == EventType.MouseDown
                    && Event.current.clickCount == 2
                    && rowRect.Contains(Event.current.mousePosition)
                    && cache.asset != null)
                {
                    EditorGUIUtility.PingObject(cache.asset);
                    Event.current.Use();
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawLeftPanelFooter()
        {
            if (objectCaches.Count == 0) return;

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("없는 항목 정리", GUILayout.Height(20)))
            {
                CleanMissingObjects();
            }
            if (GUILayout.Button("모두 삭제", GUILayout.Height(20)))
            {
                if (EditorUtility.DisplayDialog("확인",
                    "등록된 모든 ScriptableObject를 목록에서 제거하시겠습니까?",
                    "삭제", "취소"))
                {
                    registeredObjectGuids.Clear();
                    SaveRegisteredObjects();
                    needsCacheRebuild = true;
                    selectedObjectIndex = -1;
                    targetObject = null;
                    sheetFields.Clear();
                    previewResult = null;
                }
            }
            EditorGUILayout.EndHorizontal();
        }
        #endregion // 왼쪽 패널

        #region 오른쪽 패널
        private void DrawRightPanel()
        {
            EditorGUILayout.BeginVertical();

            DrawTargetSection();
            EditorGUILayout.Space(8);
            DrawInputSection();
            EditorGUILayout.Space(8);
            DrawPreviewSection();
            EditorGUILayout.Space(8);
            DrawApplySection();

            EditorGUILayout.EndVertical();
        }

        private void DrawTargetSection()
        {
            SWEditorUtils.DrawHeader("=====> 대상 <=====");

            // ObjectField (직접 지정도 가능)
            EditorGUI.BeginChangeCheck();
            targetObject = EditorGUILayout.ObjectField("ScriptableObject", targetObject,
                typeof(ScriptableObject), false) as ScriptableObject;
            if (EditorGUI.EndChangeCheck())
            {
                RefreshSheetFields();
                previewResult = null;
                SyncSelectionToTarget();
            }

            if (targetObject == null)
            {
                SWEditorUtils.DrawEmptyNotice(
                    "왼쪽 목록에서 선택하거나, 위 필드에 ScriptableObject를 지정하세요.",
                    MessageType.Info);
                return;
            }

            if (sheetFields.Count == 0)
            {
                SWEditorUtils.DrawEmptyNotice(
                    "[SWTableSheet]가 붙은 List<T> 또는 배열 필드를 찾을 수 없습니다.",
                    MessageType.Warning);
                return;
            }

            string[] sheetNames = sheetFields.Select(field => field.SheetName).ToArray();
            int selectedIndex = Mathf.Max(0, System.Array.IndexOf(sheetNames, selectedSheetName));
            int nextIndex = EditorGUILayout.Popup("Sheet", selectedIndex, sheetNames);
            selectedSheetName = sheetNames[nextIndex];

            SWExcelTableParser.SheetFieldInfo sheetField = GetSelectedSheetField();
            if (sheetField != null)
                EditorGUILayout.LabelField("Target Field",
                    $"{sheetField.Field.Name} : {sheetField.ElementType.Name}",
                    EditorStyles.miniLabel);
        }

        private void DrawInputSection()
        {
            SWEditorUtils.DrawHeader("=====> TSV 입력 <=====");

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("클립보드 붙여넣기", GUILayout.Height(24)))
            {
                tableText = EditorGUIUtility.systemCopyBuffer;
                previewResult = null;
                SWUtilsLog.Log("[SWExcelTableImporter] Paste from clipboard.");
            }

            if (GUILayout.Button("비우기", GUILayout.Width(70), GUILayout.Height(24)))
            {
                tableText = "";
                previewResult = null;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.HelpBox(
                "엑셀 또는 구글시트에서 헤더 행을 포함해 복사한 뒤 붙여넣으세요. 첫 줄은 컬럼명으로 사용됩니다.",
                MessageType.Info);

            inputScroll = EditorGUILayout.BeginScrollView(inputScroll, GUILayout.MinHeight(120));
            tableText = EditorGUILayout.TextArea(tableText, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
        }

        private void DrawPreviewSection()
        {
            SWEditorUtils.DrawHeader("=====> 미리보기 <=====");

            EditorGUILayout.BeginHorizontal();
            showPreview = EditorGUILayout.ToggleLeft("미리보기 표시", showPreview);
            if (GUILayout.Button("파싱", GUILayout.Width(70), GUILayout.Height(22)))
                ParsePreview();
            EditorGUILayout.EndHorizontal();

            if (!showPreview) return;

            if (previewResult == null)
            {
                SWEditorUtils.DrawEmptyNotice(
                    "파싱 버튼을 눌러 TSV 데이터를 확인하세요.", MessageType.None);
                return;
            }

            DrawParseMessages(previewResult);

            EditorGUILayout.LabelField(
                $"Headers: {previewResult.Headers.Count}, Rows: {previewResult.Rows.Count}",
                EditorStyles.boldLabel);

            previewScroll = EditorGUILayout.BeginScrollView(previewScroll, GUILayout.MinHeight(100));
            int maxRows = Mathf.Min(previewResult.Rows.Count, 10);
            for (int rowIndex = 0; rowIndex < maxRows; rowIndex++)
            {
                Dictionary<string, string> row = previewResult.Rows[rowIndex];
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField($"Row {rowIndex + 1}", EditorStyles.boldLabel);
                foreach (string header in previewResult.Headers)
                {
                    row.TryGetValue(header, out string value);
                    EditorGUILayout.LabelField(header, value);
                }
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawApplySection()
        {
            SWEditorUtils.DrawHeader("=====> 적용 <=====");

            using (new SWEditorUtils.GUIEnabledScope(CanApply()))
            {
                if (GUILayout.Button("ScriptableObject에 적용", GUILayout.Height(32)))
                    ApplyTable();
            }
        }
        #endregion // 오른쪽 패널

        #region 오브젝트 목록 관리
        private void RegisterObject(ScriptableObject so)
        {
            if (so == null) return;
            string path = AssetDatabase.GetAssetPath(so);
            if (string.IsNullOrEmpty(path)) return;

            string guid = AssetDatabase.AssetPathToGUID(path);
            if (registeredObjectGuids.Contains(guid))
            {
                // 이미 등록된 경우 해당 항목 선택
                int existingIndex = registeredObjectGuids.IndexOf(guid);
                SelectObjectAtIndex(existingIndex);
                return;
            }

            registeredObjectGuids.Add(guid);
            SaveRegisteredObjects();
            needsCacheRebuild = true;

            // 새로 추가된 항목 선택
            EditorApplication.delayCall += () =>
            {
                if (needsCacheRebuild) RebuildObjectCache();
                SelectObjectAtIndex(objectCaches.Count - 1);
            };
        }

        private void RemoveObjectAtIndex(int index)
        {
            if (index < 0 || index >= registeredObjectGuids.Count) return;

            bool wasSelected = (index == selectedObjectIndex);
            registeredObjectGuids.RemoveAt(index);
            SaveRegisteredObjects();
            needsCacheRebuild = true;

            if (wasSelected)
            {
                // 삭제된 항목이 선택 상태였으면 인접 항목 선택
                if (registeredObjectGuids.Count == 0)
                {
                    selectedObjectIndex = -1;
                    targetObject = null;
                    sheetFields.Clear();
                    previewResult = null;
                }
                else
                {
                    int newIndex = Mathf.Min(index, registeredObjectGuids.Count - 1);
                    EditorApplication.delayCall += () =>
                    {
                        if (needsCacheRebuild) RebuildObjectCache();
                        SelectObjectAtIndex(newIndex);
                    };
                }
            }
            else if (selectedObjectIndex > index)
            {
                selectedObjectIndex--;
            }
        }

        private void SelectObjectAtIndex(int index)
        {
            if (index < 0 || index >= objectCaches.Count) return;

            selectedObjectIndex = index;
            var cache = objectCaches[index];

            if (cache.exists && cache.asset != null)
            {
                targetObject = cache.asset;
                RefreshSheetFields();
                previewResult = null;
            }
        }

        /// <summary>
        /// ObjectField에서 직접 대상을 변경했을 때 왼쪽 리스트의 선택 상태를 동기화합니다.
        /// </summary>
        private void SyncSelectionToTarget()
        {
            if (targetObject == null)
            {
                selectedObjectIndex = -1;
                return;
            }

            string guid = SWEditorUtils.GetAssetGuid(targetObject);
            if (string.IsNullOrEmpty(guid)) return;

            int foundIndex = -1;
            for (int i = 0; i < objectCaches.Count; i++)
            {
                if (objectCaches[i].guid == guid)
                {
                    foundIndex = i;
                    break;
                }
            }

            if (foundIndex >= 0)
            {
                selectedObjectIndex = foundIndex;
            }
            else
            {
                // 리스트에 없으면 자동 등록
                RegisterObject(targetObject);
            }
        }

        private void CleanMissingObjects()
        {
            int before = registeredObjectGuids.Count;
            registeredObjectGuids.RemoveAll(guid =>
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                return string.IsNullOrEmpty(path);
            });
            int removed = before - registeredObjectGuids.Count;
            SaveRegisteredObjects();
            needsCacheRebuild = true;

            if (selectedObjectIndex >= registeredObjectGuids.Count)
            {
                selectedObjectIndex = registeredObjectGuids.Count - 1;
                if (selectedObjectIndex >= 0)
                    EditorApplication.delayCall += () =>
                    {
                        if (needsCacheRebuild) RebuildObjectCache();
                        SelectObjectAtIndex(selectedObjectIndex);
                    };
            }

            SWUtilsLog.Log($"[SWExcelTableImporter] 없는 항목 {removed}개 정리됨.");
        }

        private void RebuildObjectCache()
        {
            objectCaches.Clear();
            foreach (string guid in registeredObjectGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                bool exists = !string.IsNullOrEmpty(path);
                ScriptableObject asset = exists
                    ? AssetDatabase.LoadAssetAtPath<ScriptableObject>(path)
                    : null;

                string displayName = asset != null ? asset.name : "(없음)";

                objectCaches.Add(new RegisteredObjectCache
                {
                    guid = guid,
                    path = path,
                    asset = asset,
                    displayName = displayName,
                    exists = exists && asset != null,
                });
            }
            needsCacheRebuild = false;
        }

        private void SaveRegisteredObjects()
        {
            string joined = string.Join("|", registeredObjectGuids);
            EditorPrefs.SetString(
                SWEditorUtils.GetProjectKey(REGISTERED_OBJECTS_KEY), joined);
        }

        private void LoadRegisteredObjects()
        {
            string joined = EditorPrefs.GetString(
                SWEditorUtils.GetProjectKey(REGISTERED_OBJECTS_KEY), "");
            registeredObjectGuids.Clear();
            if (!string.IsNullOrEmpty(joined))
            {
                foreach (string g in joined.Split('|'))
                {
                    if (!string.IsNullOrEmpty(g))
                        registeredObjectGuids.Add(g);
                }
            }
        }
        #endregion // 오브젝트 목록 관리

        #region 기존 함수
        private void RefreshSheetFields()
        {
            sheetFields = SWExcelTableParser.GetSheetFields(targetObject);
            if (sheetFields.Count > 0 && string.IsNullOrEmpty(selectedSheetName))
                selectedSheetName = sheetFields[0].SheetName;
        }

        private SWExcelTableParser.SheetFieldInfo GetSelectedSheetField()
        {
            return sheetFields.FirstOrDefault(field =>
                string.Equals(field.SheetName, selectedSheetName,
                    System.StringComparison.OrdinalIgnoreCase));
        }

        private void ParsePreview()
        {
            previewResult = SWExcelTableParser.Parse(tableText);
            SWUtilsLog.Log(
                $"[SWExcelTableImporter] Parse preview. Rows: {previewResult.Rows.Count}");
        }

        private bool CanApply()
        {
            return targetObject != null
                && GetSelectedSheetField() != null
                && !string.IsNullOrWhiteSpace(tableText);
        }

        private void ApplyTable()
        {
            if (previewResult == null)
                ParsePreview();

            SWExcelTableParser.SheetFieldInfo sheetField = GetSelectedSheetField();
            if (sheetField == null)
            {
                EditorUtility.DisplayDialog("적용 실패",
                    "선택된 Sheet와 매칭되는 [SWTableSheet] 필드를 찾을 수 없습니다.", "확인");
                return;
            }

            if (previewResult.Errors.Count > 0)
            {
                EditorUtility.DisplayDialog("파싱 오류",
                    string.Join("\n", previewResult.Errors), "확인");
                return;
            }

            Undo.RecordObject(targetObject, "Apply Excel Table");
            bool success = SWExcelTableParser.ApplyToSheet(
                targetObject, sheetField, previewResult);
            if (!success)
            {
                EditorUtility.DisplayDialog("적용 실패",
                    string.Join("\n", previewResult.Errors), "확인");
                return;
            }

            EditorUtility.SetDirty(targetObject);
            AssetDatabase.SaveAssets();

            EditorUtility.DisplayDialog("적용 완료",
                $"{sheetField.SheetName} 시트 데이터 {previewResult.Rows.Count}개를 적용했습니다.",
                "확인");
            SWUtilsLog.Log(
                $"[SWExcelTableImporter] Apply complete. Target: {targetObject.name}, Sheet: {sheetField.SheetName}");
        }

        private void DrawParseMessages(SWExcelTableParser.ParseResult result)
        {
            foreach (string error in result.Errors)
                EditorGUILayout.HelpBox(error, MessageType.Error);

            foreach (string warning in result.Warnings)
                EditorGUILayout.HelpBox(warning, MessageType.Warning);
        }
        #endregion // 기존 함수
    }
}
