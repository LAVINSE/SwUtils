using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SWTools
{
    /// <summary>
    /// 하이어라키 행 스타일, 아이콘, 표시 옵션을 설정하는 에디터 창입니다.
    /// </summary>
    public class SWHierarchyToolsWindow : EditorWindow
    {
        private static readonly Color[] Palette =
        {
            new(0.96f, 0.32f, 0.32f, 1f),
            new(1f, 0.62f, 0.2f, 1f),
            new(1f, 0.84f, 0.24f, 1f),
            new(0.32f, 0.8f, 0.42f, 1f),
            new(0.2f, 0.65f, 1f, 1f),
            new(0.58f, 0.42f, 1f, 1f),
            new(1f, 0.42f, 0.75f, 1f),
            new(0.72f, 0.72f, 0.72f, 1f),
        };

        private static readonly string[] IconNames =
        {
            "d_AudioSource Icon",
            "d_Camera Icon",
            "d_Canvas Icon",
            "d_CapsuleCollider Icon",
            "d_DirectionalLight Icon",
            "d_EventSystem Icon",
            "d_Favorite Icon",
            "d_Folder Icon",
            "d_GameObject Icon",
            "d_Light Icon",
            "d_Material Icon",
            "d_MeshRenderer Icon",
            "d_ParticleSystem Icon",
            "d_Prefab Icon",
            "d_Rigidbody Icon",
            "d_SceneAsset Icon",
            "d_SettingsIcon",
            "d_SpriteRenderer Icon",
            "d_Transform Icon",
        };

        private static readonly string[] TabNames = { "Display", "Apply" };
        private const string CustomIconGuidsKey = "SWTools.HierarchyTools.CustomIconGuids";
        private const float LeftPanelWidth = 300f;
        private const float SplitterWidth = 2f;

        private Color colorA = new(0.2f, 0.65f, 1f, 1f);
        private Color colorB = new(0.58f, 0.42f, 1f, 1f);
        private bool useGradient = true;
        private bool includeChildren;
        private Texture2D customTextureIcon;
        private Vector2 registryScroll;
        private Vector2 customIconScroll;
        private int selectedTab;
        private List<string> customIconGuids = new();
        private bool removeCustomIconMode;

        /// <summary>
        /// Hierarchy Tools 창을 엽니다.
        /// </summary>
        [MenuItem("SWTools/Hierarchy Tools")]
        public static void ShowWindow()
        {
            SWHierarchyToolsWindow window = GetWindow<SWHierarchyToolsWindow>();
            SWEditorUtils.SetupWindow(window, "Hierarchy Tools", "d_UnityEditor.HierarchyWindow", 700, 520);
            window.Show();
        }

        [MenuItem("GameObject/SWTools/Hierarchy/Clear", false, 30)]
        private static void ClearMenu()
        {
            SWHierarchyTools.Clear(GetSelectedGameObjects(), false);
        }

        private void OnEnable()
        {
            customIconGuids = SWEditorUtils.LoadList(CustomIconGuidsKey);
        }

        private void OnDisable()
        {
            SaveCustomIcons();
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();

            DrawRegistryPanel();

            Rect splitter = EditorGUILayout.GetControlRect(false, GUILayout.Width(SplitterWidth), GUILayout.ExpandHeight(true));
            EditorGUI.DrawRect(splitter, SWEditorUtils.HeaderLineColor);

            DrawMainPanel();

            EditorGUILayout.EndHorizontal();
        }

        private void DrawRegistryPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(LeftPanelWidth));

            List<(GameObject gameObject, SWHierarchyTools.Entry entry, Color colorA, Color colorB)> entries = SWHierarchyTools.GetEntries();
            SWEditorUtils.DrawHeader($"등록 목록 ({entries.Count})");

            if (entries.Count == 0)
            {
                SWEditorUtils.DrawEmptyNotice("등록된 하이어라키 항목이 없습니다.", MessageType.None);
            }
            else
            {
                DrawRegistryList(entries);
            }

            GUILayout.FlexibleSpace();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("누락 정리", GUILayout.Height(SWEditorUtils.SmallButtonHeight)))
                SWHierarchyTools.CleanMissing();

            if (GUILayout.Button("전체 제거", GUILayout.Height(SWEditorUtils.SmallButtonHeight)))
            {
                if (EditorUtility.DisplayDialog("Hierarchy Tools 초기화", "모든 하이어라키 꾸밈을 제거할까요?", "제거", "취소"))
                    SWHierarchyTools.ClearAll();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawRegistryList(List<(GameObject gameObject, SWHierarchyTools.Entry entry, Color colorA, Color colorB)> entries)
        {
            registryScroll = EditorGUILayout.BeginScrollView(registryScroll);

            for (int i = 0; i < entries.Count; i++)
            {
                var item = entries[i];
                bool exists = item.gameObject != null;

                Rect rowRect = EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

                Rect swatchRect = GUILayoutUtility.GetRect(18f, 18f, GUILayout.Width(18f), GUILayout.Height(18f));
                SWEditorUtils.DrawColorSwatch(swatchRect, item.colorA, item.entry.useGradient ? item.colorB : item.colorA);

                using (new SWEditorUtils.GUIColorScope(exists ? Color.white : SWEditorUtils.ErrorColor))
                {
                    string label = exists ? item.gameObject.name : $"{item.entry.name} (missing)";
                    if (GUILayout.Button(label, EditorStyles.label, GUILayout.ExpandWidth(true), GUILayout.Height(18f)))
                    {
                        if (exists)
                            SelectAndPing(item.gameObject);
                    }
                }

                if (GUILayout.Button("x", GUILayout.Width(18f), GUILayout.Height(18f)))
                {
                    if (exists)
                        SWHierarchyTools.Clear(item.gameObject);
                    else
                        SWHierarchyTools.CleanMissing();

                    GUIUtility.ExitGUI();
                }

                EditorGUILayout.EndHorizontal();

                if (Event.current.type == EventType.MouseDown
                    && Event.current.clickCount == 2
                    && rowRect.Contains(Event.current.mousePosition)
                    && exists)
                {
                    SelectAndPing(item.gameObject);
                    Event.current.Use();
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawMainPanel()
        {
            EditorGUILayout.BeginVertical();

            selectedTab = SWEditorUtils.DrawTabBar(selectedTab, TabNames);
            if (selectedTab == 0)
                DrawDisplayTab();
            else
                DrawApplyTab();

            EditorGUILayout.EndVertical();
        }

        private void DrawDisplayTab()
        {
            SWEditorUtils.DrawHeader("표시");

            EditorGUI.BeginChangeCheck();
            bool enabled = DrawToggleRow("활성화", "Hierarchy Tools 기능을 켭니다.", SWHierarchyTools.Enabled);
            bool background = DrawToggleRow("배경 색상", "행 배경에 색상을 표시합니다.", SWHierarchyTools.DrawBackground);
            bool gradientDefault = DrawToggleRow("기본 그라데이션", "새 꾸밈에 그라데이션을 기본 적용합니다.", SWHierarchyTools.UseGradientByDefault);
            bool componentIcons = DrawToggleRow("컴포넌트 아이콘", "오른쪽에 컴포넌트 아이콘을 표시합니다.", SWHierarchyTools.DrawComponentIcons);
            bool minimap = DrawToggleRow("컴포넌트 점", "작은 컴포넌트 점을 표시합니다.", SWHierarchyTools.DrawComponentMinimap);
            bool warning = DrawToggleRow("누락 경고", "Missing Script 경고를 표시합니다.", SWHierarchyTools.DrawMissingScriptWarning);
            bool activeToggle = DrawToggleRow("활성 토글", "오른쪽에 활성 상태 토글을 표시합니다.", SWHierarchyTools.DrawActiveToggle);
            bool zebra = DrawToggleRow("교차 행", "행마다 번갈아 배경을 표시합니다.", SWHierarchyTools.DrawZebraRows);
            bool shortcuts = DrawToggleRow("호버 단축키", "마우스를 올린 오브젝트의 단축키를 사용합니다.", SWHierarchyTools.EnableShortcuts);
            float lineWidth = EditorGUILayout.Slider("라인 두께", SWHierarchyTools.LineWidth, 1f, 12f);
            float componentIconSpacing = EditorGUILayout.Slider("컴포넌트 아이콘 간격", SWHierarchyTools.ComponentIconSpacing, 0f, 12f);

            if (EditorGUI.EndChangeCheck())
            {
                SWHierarchyTools.Enabled = enabled;
                SWHierarchyTools.DrawBackground = background;
                SWHierarchyTools.UseGradientByDefault = gradientDefault;
                SWHierarchyTools.DrawComponentIcons = componentIcons;
                SWHierarchyTools.DrawComponentMinimap = minimap;
                SWHierarchyTools.DrawMissingScriptWarning = warning;
                SWHierarchyTools.DrawActiveToggle = activeToggle;
                SWHierarchyTools.DrawZebraRows = zebra;
                SWHierarchyTools.EnableShortcuts = shortcuts;
                SWHierarchyTools.LineWidth = lineWidth;
                SWHierarchyTools.ComponentIconSpacing = componentIconSpacing;
            }

            EditorGUILayout.Space(6);
            EditorGUILayout.HelpBox("오브젝트 아이콘은 Unity 기본 GameObject 아이콘을 대체합니다. 오른쪽 영역은 컴포넌트 아이콘, 경고, 토글 표시에 사용됩니다.", MessageType.None);
        }

        private void DrawApplyTab()
        {
            SWEditorUtils.DrawHeader("적용");

            List<GameObject> selected = GetSelectedGameObjects();
            EditorGUILayout.LabelField($"선택된 GameObject: {selected.Count}");
            includeChildren = DrawToggleRow("자식 포함", "자식 오브젝트까지 함께 적용합니다.", includeChildren);

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("빠른 색상", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            foreach (Color color in Palette)
            {
                using (new SWEditorUtils.GUIBgColorScope(color))
                {
                    if (GUILayout.Button(GUIContent.none, GUILayout.Height(24f)))
                    {
                        colorA = color;
                        SWHierarchyTools.Apply(selected, colorA, colorB, false, includeChildren);
                    }
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);
            useGradient = EditorGUILayout.Toggle("그라데이션 사용", useGradient);
            colorA = EditorGUILayout.ColorField("색상 A", colorA);
            using (new SWEditorUtils.GUIEnabledScope(useGradient))
                colorB = EditorGUILayout.ColorField("색상 B", colorB);

            if (GUILayout.Button("색상 적용", GUILayout.Height(SWEditorUtils.DefaultButtonHeight)))
                SWHierarchyTools.Apply(selected, colorA, colorB, useGradient, includeChildren);

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Unity 오브젝트 아이콘", EditorStyles.boldLabel);
            DrawBuiltinIconPalette(selected);

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("커스텀 텍스처 아이콘", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            customTextureIcon = (Texture2D)EditorGUILayout.ObjectField(customTextureIcon, typeof(Texture2D), false);
            using (new SWEditorUtils.GUIEnabledScope(customTextureIcon != null))
            {
                if (GUILayout.Button("추가", GUILayout.Width(55), GUILayout.Height(SWEditorUtils.DefaultButtonHeight)))
                    RegisterCustomIcon(customTextureIcon);

                if (GUILayout.Button("적용", GUILayout.Width(60), GUILayout.Height(SWEditorUtils.DefaultButtonHeight)))
                    SWHierarchyTools.SetAssetIcon(selected, customTextureIcon, includeChildren);
            }
            EditorGUILayout.EndHorizontal();
            DrawCustomIconPalette(selected);

            EditorGUILayout.Space(8);
            SWEditorUtils.DrawHeader("생성");
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("폴더", GUILayout.Height(SWEditorUtils.DefaultButtonHeight)))
                SWHierarchyTools.CreateStyledObject("Folder", SWHierarchyTools.RowStyle.Folder, colorA, colorB, useGradient, "d_Folder Icon");
            if (GUILayout.Button("구분선 예시", GUILayout.Height(SWEditorUtils.DefaultButtonHeight)))
                SWHierarchyTools.CreateStyledObject("--------------- Example ---------------", SWHierarchyTools.RowStyle.Normal, colorA, colorB, useGradient, "");
            EditorGUILayout.EndHorizontal();
        }

        private void DrawBuiltinIconPalette(List<GameObject> selected)
        {
            const int iconsPerRow = 10;
            int column = 0;

            EditorGUILayout.BeginHorizontal();
            foreach (string iconName in IconNames)
            {
                if (GUILayout.Button(EditorGUIUtility.IconContent(iconName), GUILayout.Width(32f), GUILayout.Height(26f)))
                    SWHierarchyTools.SetIcon(selected, iconName, includeChildren);

                column++;
                if (column % iconsPerRow == 0)
                {
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawCustomIconPalette(List<GameObject> selected)
        {
            if (customIconGuids.Count == 0)
            {
                SWEditorUtils.DrawEmptyNotice("등록된 커스텀 아이콘이 없습니다.", MessageType.None);
                return;
            }

            removeCustomIconMode = DrawToggleRow("제거 모드", "커스텀 아이콘을 클릭하면 목록에서 제거합니다.", removeCustomIconMode);
            customIconScroll = EditorGUILayout.BeginScrollView(customIconScroll, GUILayout.MaxHeight(82f));
            int column = 0;
            EditorGUILayout.BeginHorizontal();

            for (int i = 0; i < customIconGuids.Count; i++)
            {
                string guid = customIconGuids[i];
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Texture2D icon = string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<Texture2D>(path);

                if (icon == null)
                {
                    if (GUILayout.Button("x", GUILayout.Width(32f), GUILayout.Height(26f)))
                    {
                        customIconGuids.RemoveAt(i);
                        SaveCustomIcons();
                        GUIUtility.ExitGUI();
                    }
                }
                else
                {
                    GUIContent content = removeCustomIconMode
                        ? new GUIContent("x", path)
                        : new GUIContent(icon, path);

                    if (GUILayout.Button(content, GUILayout.Width(32f), GUILayout.Height(26f)))
                    {
                        if (removeCustomIconMode)
                        {
                            customIconGuids.RemoveAt(i);
                            SaveCustomIcons();
                            GUIUtility.ExitGUI();
                        }
                        else
                        {
                            SWHierarchyTools.SetAssetIcon(selected, icon, includeChildren);
                        }
                    }
                }

                column++;
                if (column % 10 == 0)
                {
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                }
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndScrollView();
        }

        private void RegisterCustomIcon(Texture2D icon)
        {
            string path = AssetDatabase.GetAssetPath(icon);
            if (string.IsNullOrEmpty(path)) return;

            string guid = AssetDatabase.AssetPathToGUID(path);
            if (string.IsNullOrEmpty(guid) || customIconGuids.Contains(guid)) return;

            customIconGuids.Add(guid);
            SaveCustomIcons();
        }

        private void SaveCustomIcons()
        {
            SWEditorUtils.SaveList(CustomIconGuidsKey, customIconGuids);
        }

        private static bool DrawToggleRow(string label, string description, bool value)
        {
            EditorGUILayout.BeginHorizontal();
            bool newValue = EditorGUILayout.Toggle(value, GUILayout.Width(18f));
            EditorGUILayout.LabelField(label, GUILayout.Width(140f));
            EditorGUILayout.LabelField(description, EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
            return newValue;
        }

        private static void SelectAndPing(GameObject gameObject)
        {
            Selection.activeGameObject = gameObject;
            EditorGUIUtility.PingObject(gameObject);
        }

        private static List<GameObject> GetSelectedGameObjects()
        {
            List<GameObject> result = new();
            foreach (GameObject gameObject in Selection.gameObjects)
            {
                if (gameObject != null)
                    result.Add(gameObject);
            }
            return result;
        }
    }
}
