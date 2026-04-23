using System;
using System.Collections.Generic;
using System.Linq;
using SWUtils;
using UnityEditor;
using UnityEngine;

#if UNITY_2021_2_OR_NEWER
using UnityEditor.Build;
#endif

namespace SWTools
{
    /// <summary>
    /// 빌드 타겟 그룹별 Scripting Define Symbol을 확인하고 편집하는 에디터 창.
    /// </summary>
    public class SWDefineSymbolWindow : EditorWindow
    {
        #region 필드
        private const string PresetSymbolsKey = "SWTools.DefineSymbolWindow.Presets";

        private static readonly string[] defaultPresetSymbols =
        {
            
        };

        private BuildTargetGroup selectedGroup;
        private List<string> currentSymbols = new();
        private List<string> presetSymbols = new();
        private string newSymbol = "";
        private string presetInput = "";
        private Vector2 scrollPosition;
        #endregion // 필드

        #region 초기화
        [MenuItem("SWTools/Define Symbol Window")]
        public static void ShowWindow()
        {
            SWDefineSymbolWindow window = GetWindow<SWDefineSymbolWindow>();
            SWEditorUtils.SetupWindow(window, "SW Define Symbols", "d_SettingsIcon", 420, 420);
            window.Show();
        }

        private void OnEnable()
        {
            selectedGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            LoadPresets();
            RefreshSymbols();

            SWUtilsLog.Log("[SWDefineSymbolWindow] Opened.");
        }

        private void OnDisable()
        {
            SavePresets();
            SWUtilsLog.Log("[SWDefineSymbolWindow] Closed.");
        }
        #endregion // 초기화

        #region GUI
        private void OnGUI()
        {
            DrawTargetSection();
            EditorGUILayout.Space(8);
            DrawAddSection();
            EditorGUILayout.Space(8);
            DrawPresetSection();
            EditorGUILayout.Space(8);
            DrawCurrentSymbolsSection();
        }

        private void DrawTargetSection()
        {
            SWEditorUtils.DrawHeader("=====> 빌드 타겟 <=====");

            EditorGUI.BeginChangeCheck();
            selectedGroup = (BuildTargetGroup)EditorGUILayout.EnumPopup("Target Group", selectedGroup);
            if (EditorGUI.EndChangeCheck())
            {
                RefreshSymbols();
            }

            EditorGUILayout.BeginHorizontal();
            if (SWEditorUtils.Button("Refresh"))
                RefreshSymbols();

            if (SWEditorUtils.DangerButton("Clear All Defines", "Clear Define Symbols",
                    $"Remove all define symbols from {selectedGroup}?", "Clear"))
            {
                currentSymbols.Clear();
                ApplySymbols();
                SWUtilsLog.LogWarning($"[SWDefineSymbolWindow] Clear all defines. Group: {selectedGroup}");
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawAddSection()
        {
            SWEditorUtils.DrawHeader("=====> 심볼 추가 <=====");

            EditorGUILayout.BeginHorizontal();
            newSymbol = EditorGUILayout.TextField(newSymbol);
            using (new SWEditorUtils.GUIEnabledScope(IsValidSymbol(newSymbol)))
            {
                if (GUILayout.Button("Add", GUILayout.Width(70)))
                {
                    AddSymbol(newSymbol);
                    newSymbol = "";
                    GUI.FocusControl(null);
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.HelpBox("Use letters, numbers, and underscore only. Example: DebugMode", MessageType.Info);
        }

        private void DrawPresetSection()
        {
            SWEditorUtils.DrawHeader("=====> 프리셋 <=====");

            for (int index = 0; index < presetSymbols.Count; index++)
            {
                string symbol = presetSymbols[index];
                bool enabled = currentSymbols.Contains(symbol);

                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                bool nextEnabled = EditorGUILayout.ToggleLeft(symbol, enabled);
                if (nextEnabled != enabled)
                {
                    if (nextEnabled) AddSymbol(symbol);
                    else RemoveSymbol(symbol);
                }

                if (GUILayout.Button("Remove Preset", GUILayout.Width(110)))
                {
                    presetSymbols.RemoveAt(index);
                    SavePresets();
                    SWUtilsLog.Log($"[SWDefineSymbolWindow] Remove preset: {symbol}");
                    GUIUtility.ExitGUI();
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.BeginHorizontal();
            presetInput = EditorGUILayout.TextField(presetInput);
            using (new SWEditorUtils.GUIEnabledScope(IsValidSymbol(presetInput) && !presetSymbols.Contains(presetInput)))
            {
                if (GUILayout.Button("Add Preset", GUILayout.Width(90)))
                {
                    string symbol = NormalizeSymbol(presetInput);
                    presetSymbols.Add(symbol);
                    presetSymbols.Sort(StringComparer.Ordinal);
                    presetInput = "";
                    SavePresets();
                    SWUtilsLog.Log($"[SWDefineSymbolWindow] Add preset: {symbol}");
                    GUI.FocusControl(null);
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawCurrentSymbolsSection()
        {
            SWEditorUtils.DrawHeader($"=====> 현재 심볼 ({currentSymbols.Count}) <=====");

            if (currentSymbols.Count == 0)
            {
                SWEditorUtils.DrawEmptyNotice("No define symbols are set for this target group.", MessageType.None);
                return;
            }

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            for (int index = 0; index < currentSymbols.Count; index++)
            {
                string symbol = currentSymbols[index];

                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                EditorGUILayout.SelectableLabel(symbol, GUILayout.Height(18));
                if (GUILayout.Button("Remove", GUILayout.Width(70), GUILayout.Height(20)))
                {
                    RemoveSymbol(symbol);
                    GUIUtility.ExitGUI();
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
        }
        #endregion // GUI

        #region 함수
        private void RefreshSymbols()
        {
            string raw = GetDefines(selectedGroup);
            currentSymbols = ParseSymbols(raw);
            Repaint();

            SWUtilsLog.Log($"[SWDefineSymbolWindow] Refresh. Group: {selectedGroup}, Count: {currentSymbols.Count}");
        }

        private void AddSymbol(string symbol)
        {
            symbol = NormalizeSymbol(symbol);
            if (!IsValidSymbol(symbol))
            {
                SWUtilsLog.LogWarning($"[SWDefineSymbolWindow] Add symbol failed. Invalid symbol: {symbol}");
                return;
            }

            if (currentSymbols.Contains(symbol))
                return;

            currentSymbols.Add(symbol);
            currentSymbols.Sort(StringComparer.Ordinal);
            ApplySymbols();

            SWUtilsLog.Log($"[SWDefineSymbolWindow] Add symbol: {symbol}");
        }

        private void RemoveSymbol(string symbol)
        {
            if (!currentSymbols.Remove(symbol))
                return;

            ApplySymbols();
            SWUtilsLog.Log($"[SWDefineSymbolWindow] Remove symbol: {symbol}");
        }

        private void ApplySymbols()
        {
            currentSymbols = currentSymbols
                .Where(IsValidSymbol)
                .Distinct()
                .OrderBy(symbol => symbol, StringComparer.Ordinal)
                .ToList();

            string defineSymbols = string.Join(";", currentSymbols);
            SetDefines(selectedGroup, defineSymbols);
            AssetDatabase.SaveAssets();
            RefreshSymbols();

            SWUtilsLog.Log($"[SWDefineSymbolWindow] Apply symbols. Group: {selectedGroup}, Symbols: {defineSymbols}");
        }

        private void LoadPresets()
        {
            string saved = EditorPrefs.GetString(PresetSymbolsKey, string.Join(";", defaultPresetSymbols));
            presetSymbols = ParseSymbols(saved);
        }

        private void SavePresets()
        {
            EditorPrefs.SetString(PresetSymbolsKey, string.Join(";", presetSymbols));
        }
        #endregion // 함수

        #region 유틸리티
        private static List<string> ParseSymbols(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return new List<string>();

            return raw.Split(new[] { ';', ',', ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(NormalizeSymbol)
                .Where(IsValidSymbol)
                .Distinct()
                .OrderBy(symbol => symbol, StringComparer.Ordinal)
                .ToList();
        }

        private static string NormalizeSymbol(string symbol)
        {
            return string.IsNullOrWhiteSpace(symbol) ? string.Empty : symbol.Trim();
        }

        private static bool IsValidSymbol(string symbol)
        {
            symbol = NormalizeSymbol(symbol);
            if (string.IsNullOrEmpty(symbol)) return false;

            foreach (char character in symbol)
            {
                if (!char.IsLetterOrDigit(character) && character != '_')
                    return false;
            }

            return !char.IsDigit(symbol[0]);
        }

        private static string GetDefines(BuildTargetGroup group)
        {
#if UNITY_2021_2_OR_NEWER
            NamedBuildTarget namedTarget = NamedBuildTarget.FromBuildTargetGroup(group);
            return PlayerSettings.GetScriptingDefineSymbols(namedTarget);
#else
            return PlayerSettings.GetScriptingDefineSymbolsForGroup(group);
#endif
        }

        private static void SetDefines(BuildTargetGroup group, string symbols)
        {
#if UNITY_2021_2_OR_NEWER
            NamedBuildTarget namedTarget = NamedBuildTarget.FromBuildTargetGroup(group);
            PlayerSettings.SetScriptingDefineSymbols(namedTarget, symbols);
#else
            PlayerSettings.SetScriptingDefineSymbolsForGroup(group, symbols);
#endif
        }
        #endregion // 유틸리티
    }
}
