using System;
using System.Collections.Generic;
using SWUtils;
using UnityEditor;
using UnityEngine;

namespace SWTools
{
    /// <summary>
    /// SWUtilsPlayerPrefs가 관리하는 암호화 PlayerPrefs 데이터를 조회, 수정, 삭제, JSON 입출력하는 에디터 창.
    /// </summary>
    public class SWPlayerPrefsViewerWindow : EditorWindow
    {
        /// <summary>
        /// JSON 입출력에 사용하는 PlayerPrefs 항목 컨테이너입니다.
        /// </summary>
        [Serializable]
        private class PrefsData
        {
            /// <summary>저장된 PlayerPrefs 항목 목록입니다.</summary>
            public List<PrefsEntry> entries = new();
        }

        /// <summary>
        /// 단일 PlayerPrefs 키와 값입니다.
        /// </summary>
        [Serializable]
        private class PrefsEntry
        {
            /// <summary>PlayerPrefs 키입니다.</summary>
            public string key;
            /// <summary>PlayerPrefs 값입니다.</summary>
            public string value;
        }

        private const string SlotPrefKey = "SWTools.PlayerPrefsViewer.Slot";

        private readonly List<PrefsEntry> entries = new();
        private Vector2 scrollPosition;
        private Vector2 jsonScrollPosition;
        private string slotName = "default";
        private string searchFilter = "";
        private string editKey = "";
        private string editValue = "";
        private string jsonText = "";
        private string statusMessage = "";

        /// <summary>
        /// PlayerPrefs Viewer 창을 엽니다.
        /// </summary>
        [MenuItem("SWTools/PlayerPrefs Viewer")]
        public static void ShowWindow()
        {
            SWPlayerPrefsViewerWindow window = GetWindow<SWPlayerPrefsViewerWindow>();
            SWEditorUtils.SetupWindow(window, "SW PlayerPrefs", "d_SaveAs", 460, 520);
            window.Show();
        }

        private void OnEnable()
        {
            slotName = EditorPrefs.GetString(SlotPrefKey, SWUtilsPlayerPrefs.CurrentSlot);
            ApplySlot(false);
            RefreshEntries();
        }

        private void OnGUI()
        {
            DrawSlotSection();
            EditorGUILayout.Space(8);
            DrawEditSection();
            EditorGUILayout.Space(8);
            DrawListSection();
            EditorGUILayout.Space(8);
            DrawJsonSection();

            if (!string.IsNullOrEmpty(statusMessage))
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.HelpBox(statusMessage, MessageType.Info);
            }
        }

        private void DrawSlotSection()
        {
            SWEditorUtils.DrawHeader("Slot");

            EditorGUILayout.BeginHorizontal();
            slotName = EditorGUILayout.TextField("Current Slot", slotName);
            if (GUILayout.Button("Apply", GUILayout.Width(70)))
                ApplySlot(true);
            if (GUILayout.Button("Default", GUILayout.Width(70)))
            {
                slotName = "default";
                ApplySlot(true);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            searchFilter = EditorGUILayout.TextField("Search", searchFilter);
            if (GUILayout.Button("Refresh", GUILayout.Width(80)))
                RefreshEntries();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawEditSection()
        {
            SWEditorUtils.DrawHeader("Add / Edit");

            editKey = EditorGUILayout.TextField("Key", editKey);
            editValue = EditorGUILayout.TextField("Value", editValue);

            EditorGUILayout.BeginHorizontal();
            using (new SWEditorUtils.GUIEnabledScope(!string.IsNullOrWhiteSpace(editKey)))
            {
                if (GUILayout.Button("Save String", GUILayout.Height(SWEditorUtils.DefaultButtonHeight)))
                    SaveEntry();
            }

            if (GUILayout.Button("Clear Fields", GUILayout.Height(SWEditorUtils.DefaultButtonHeight)))
            {
                editKey = "";
                editValue = "";
                GUI.FocusControl(null);
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawListSection()
        {
            SWEditorUtils.DrawHeader($"Entries ({entries.Count})");

            if (entries.Count == 0)
            {
                SWEditorUtils.DrawEmptyNotice("No SWUtilsPlayerPrefs entries in this slot.", MessageType.None);
                return;
            }

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.MinHeight(160));
            for (int index = 0; index < entries.Count; index++)
            {
                PrefsEntry entry = entries[index];
                if (!MatchesFilter(entry))
                    continue;

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.SelectableLabel(entry.key, EditorStyles.boldLabel, GUILayout.Height(18));

                if (GUILayout.Button("Edit", GUILayout.Width(48), GUILayout.Height(20)))
                {
                    editKey = entry.key;
                    editValue = entry.value;
                    GUI.FocusControl(null);
                }

                if (GUILayout.Button("Copy", GUILayout.Width(52), GUILayout.Height(20)))
                {
                    EditorGUIUtility.systemCopyBuffer = entry.value ?? "";
                    statusMessage = $"Copied value: {entry.key}";
                }

                if (GUILayout.Button("Delete", GUILayout.Width(58), GUILayout.Height(20)))
                {
                    DeleteEntry(entry.key);
                    GUIUtility.ExitGUI();
                }

                EditorGUILayout.EndHorizontal();

                using (new EditorGUI.DisabledScope(true))
                    EditorGUILayout.TextField(entry.value ?? "");

                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndScrollView();

            if (SWEditorUtils.DangerButton("Delete All In Slot", "Delete SWUtilsPlayerPrefs",
                    $"Delete all encrypted SWUtilsPlayerPrefs entries in slot '{SWUtilsPlayerPrefs.CurrentSlot}'?",
                    "Delete"))
            {
                SWUtilsPlayerPrefs.DeleteAll();
                RefreshEntries("Deleted all entries in current slot.");
            }
        }

        private void DrawJsonSection()
        {
            SWEditorUtils.DrawHeader("JSON Export / Import");

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Export", GUILayout.Height(SWEditorUtils.DefaultButtonHeight)))
            {
                jsonText = SWUtilsPlayerPrefs.ExportToJson();
                statusMessage = "Exported current slot to JSON.";
            }

            using (new SWEditorUtils.GUIEnabledScope(!string.IsNullOrEmpty(jsonText)))
            {
                if (GUILayout.Button("Copy JSON", GUILayout.Height(SWEditorUtils.DefaultButtonHeight)))
                {
                    EditorGUIUtility.systemCopyBuffer = jsonText;
                    statusMessage = "Copied JSON.";
                }
            }
            EditorGUILayout.EndHorizontal();

            jsonScrollPosition = EditorGUILayout.BeginScrollView(jsonScrollPosition, GUILayout.Height(90));
            jsonText = EditorGUILayout.TextArea(jsonText, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();

            EditorGUILayout.BeginHorizontal();
            using (new SWEditorUtils.GUIEnabledScope(!string.IsNullOrWhiteSpace(jsonText)))
            {
                if (SWEditorUtils.DangerButton("Import Replace", "Import SWUtilsPlayerPrefs",
                        "Replace current slot data with this JSON?", "Import"))
                    ImportJson(false);

                if (GUILayout.Button("Import Merge", GUILayout.Height(SWEditorUtils.DefaultButtonHeight)))
                    ImportJson(true);
            }
            EditorGUILayout.EndHorizontal();
        }

        private void ApplySlot(bool showStatus)
        {
            if (string.IsNullOrWhiteSpace(slotName))
                slotName = "default";

            slotName = slotName.Trim();
            SWUtilsPlayerPrefs.SetSlot(slotName);
            EditorPrefs.SetString(SlotPrefKey, slotName);
            RefreshEntries(showStatus ? $"Applied slot: {slotName}" : "");
        }

        private void RefreshEntries(string message = "")
        {
            entries.Clear();

            string json = SWUtilsPlayerPrefs.ExportToJson();
            if (!string.IsNullOrEmpty(json))
            {
                PrefsData data = JsonUtility.FromJson<PrefsData>(json);
                if (data?.entries != null)
                    entries.AddRange(data.entries);
            }

            entries.Sort((left, right) => string.Compare(left.key, right.key, StringComparison.Ordinal));
            statusMessage = message;
            Repaint();
        }

        private void SaveEntry()
        {
            string key = editKey.Trim();
            SWUtilsPlayerPrefs.SetString(key, editValue ?? "");
            SWUtilsPlayerPrefs.Save();
            RefreshEntries($"Saved key: {key}");
        }

        private void DeleteEntry(string key)
        {
            if (!EditorUtility.DisplayDialog("Delete Entry", $"Delete '{key}'?", "Delete", "Cancel"))
                return;

            SWUtilsPlayerPrefs.DeleteKey(key);
            SWUtilsPlayerPrefs.Save();
            RefreshEntries($"Deleted key: {key}");
        }

        private void ImportJson(bool merge)
        {
            bool success = merge
                ? SWUtilsPlayerPrefs.MergeFromJson(jsonText)
                : SWUtilsPlayerPrefs.ImportFromJson(jsonText);

            RefreshEntries(success ? "Import completed." : "Import failed. Check JSON format.");
        }

        private bool MatchesFilter(PrefsEntry entry)
        {
            if (string.IsNullOrWhiteSpace(searchFilter))
                return true;

            string filter = searchFilter.Trim();
            return SWEditorUtils.MatchesFilter(entry.key, filter) ||
                   SWEditorUtils.MatchesFilter(entry.value, filter);
        }
    }
}
