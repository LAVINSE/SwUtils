using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SWTools
{
    /// <summary>
    /// Unity 하이어라키 행의 배경, 아이콘, 보조 도구 표시를 관리하는 에디터 유틸리티입니다.
    /// </summary>
    [InitializeOnLoad]
    public static class SWHierarchyTools
    {
        /// <summary>
        /// 하이어라키 행에 적용할 표시 스타일입니다.
        /// </summary>
        public enum RowStyle
        {
            /// <summary>일반 오브젝트 행 스타일입니다.</summary>
            Normal,
            /// <summary>폴더처럼 강조하는 행 스타일입니다.</summary>
            Folder,
            /// <summary>구분선 용도로 사용하는 행 스타일입니다.</summary>
            Divider
        }

        /// <summary>
        /// 하이어라키 오브젝트별 스타일 설정 데이터입니다.
        /// </summary>
        [Serializable]
        public class Entry
        {
            /// <summary>오브젝트를 식별하는 전역 ID입니다.</summary>
            public string globalId;
            /// <summary>스타일을 저장할 때의 오브젝트 이름입니다.</summary>
            public string name;
            /// <summary>시작 색상 HTML 문자열입니다.</summary>
            public string colorA;
            /// <summary>끝 색상 HTML 문자열입니다.</summary>
            public string colorB;
            /// <summary>그라데이션을 사용할지 여부입니다.</summary>
            public bool useGradient;
            /// <summary>Unity 내장 아이콘 이름 또는 에셋 아이콘 경로입니다.</summary>
            public string iconName;
            /// <summary>행 표시 스타일입니다.</summary>
            public RowStyle style;
        }

        [Serializable]
        private class EntryList
        {
            /// <summary>저장된 하이어라키 스타일 항목 목록입니다.</summary>
            public List<Entry> entries = new();
        }

        private const string DataKey = "SWTools.HierarchyTools.Data.v2";
        private const string EnabledKey = "SWTools.HierarchyTools.Enabled";
        private const string BackgroundKey = "SWTools.HierarchyTools.Background";
        private const string GradientKey = "SWTools.HierarchyTools.Gradient";
        private const string ComponentMinimapKey = "SWTools.HierarchyTools.ComponentMinimap.v2";
        private const string ComponentIconsKey = "SWTools.HierarchyTools.ComponentIcons";
        private const string MissingWarningKey = "SWTools.HierarchyTools.MissingWarning";
        private const string ActiveToggleKey = "SWTools.HierarchyTools.ActiveToggle";
        private const string ZebraKey = "SWTools.HierarchyTools.Zebra";
        private const string ShortcutsKey = "SWTools.HierarchyTools.Shortcuts";
        private const string LineWidthKey = "SWTools.HierarchyTools.LineWidth";

        private static readonly Dictionary<string, Entry> entriesById = new();
        private static readonly Dictionary<int, Entry> entriesByInstanceId = new();
        private static readonly Dictionary<int, bool> missingScriptCache = new();
        private static readonly Dictionary<int, Component[]> componentCache = new();
        private static readonly Dictionary<string, Texture> iconCache = new();
        private static readonly Dictionary<string, Texture2D> assetIconCache = new();
        private static readonly Color defaultA = new(0.2f, 0.65f, 1f, 1f);
        private static readonly Color defaultB = new(0.68f, 0.45f, 1f, 1f);
        private static readonly Color[] componentColors =
        {
            new(0.25f, 0.65f, 1f, 0.95f),
            new(0.34f, 0.82f, 0.44f, 0.95f),
            new(1f, 0.67f, 0.24f, 0.95f),
            new(0.95f, 0.38f, 0.6f, 0.95f),
            new(0.68f, 0.52f, 1f, 0.95f),
        };

        private static bool loaded;
        private static bool settingsLoaded;
        private static bool enabled;
        private static bool drawBackground;
        private static bool useGradientByDefault;
        private static bool drawComponentMinimap;
        private static bool drawComponentIcons;
        private static bool drawMissingScriptWarning;
        private static bool drawActiveToggle;
        private static bool drawZebraRows;
        private static bool enableShortcuts;
        private static float lineWidth;
        private static readonly Dictionary<string, Texture2D> gradientTextureCache = new();
        private static GameObject hoveredObject;

        /// <summary>하이어라키 도구 표시 기능이 활성화되어 있는지 여부입니다.</summary>
        public static bool Enabled
        {
            get { EnsureSettingsLoaded(); return enabled; }
            set { enabled = value; SetBool(EnabledKey, value); }
        }

        /// <summary>스타일이 지정된 행의 배경을 그릴지 여부입니다.</summary>
        public static bool DrawBackground
        {
            get { EnsureSettingsLoaded(); return drawBackground; }
            set { drawBackground = value; SetBool(BackgroundKey, value); }
        }

        /// <summary>새 스타일 적용 시 기본으로 그라데이션을 사용할지 여부입니다.</summary>
        public static bool UseGradientByDefault
        {
            get { EnsureSettingsLoaded(); return useGradientByDefault; }
            set { useGradientByDefault = value; SetBool(GradientKey, value); }
        }

        /// <summary>하이어라키 오른쪽에 컴포넌트 미니맵을 표시할지 여부입니다.</summary>
        public static bool DrawComponentMinimap
        {
            get { EnsureSettingsLoaded(); return drawComponentMinimap; }
            set { drawComponentMinimap = value; SetBool(ComponentMinimapKey, value); }
        }

        /// <summary>하이어라키 오른쪽에 컴포넌트 아이콘을 표시할지 여부입니다.</summary>
        public static bool DrawComponentIcons
        {
            get { EnsureSettingsLoaded(); return drawComponentIcons; }
            set { drawComponentIcons = value; SetBool(ComponentIconsKey, value); }
        }

        /// <summary>Missing Script 경고 표시를 그릴지 여부입니다.</summary>
        public static bool DrawMissingScriptWarning
        {
            get { EnsureSettingsLoaded(); return drawMissingScriptWarning; }
            set { drawMissingScriptWarning = value; SetBool(MissingWarningKey, value); }
        }

        /// <summary>하이어라키 행에 활성화 토글을 표시할지 여부입니다.</summary>
        public static bool DrawActiveToggle
        {
            get { EnsureSettingsLoaded(); return drawActiveToggle; }
            set { drawActiveToggle = value; SetBool(ActiveToggleKey, value); }
        }

        /// <summary>하이어라키 행에 교차 배경을 표시할지 여부입니다.</summary>
        public static bool DrawZebraRows
        {
            get { EnsureSettingsLoaded(); return drawZebraRows; }
            set { drawZebraRows = value; SetBool(ZebraKey, value); }
        }

        /// <summary>하이어라키 도구 단축키를 사용할지 여부입니다.</summary>
        public static bool EnableShortcuts
        {
            get { EnsureSettingsLoaded(); return enableShortcuts; }
            set { enableShortcuts = value; SetBool(ShortcutsKey, value); }
        }

        /// <summary>행 왼쪽 색상 라인의 너비입니다.</summary>
        public static float LineWidth
        {
            get { EnsureSettingsLoaded(); return lineWidth; }
            set
            {
                lineWidth = Mathf.Clamp(value, 1f, 12f);
                SWEditorUtils.SavePref(LineWidthKey, lineWidth);
                Repaint();
            }
        }

        static SWHierarchyTools()
        {
            EnsureSettingsLoaded();
            EnsureLoaded();
            EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyGUI;
            EditorApplication.hierarchyChanged += ClearTransientCaches;
        }

        /// <summary>
        /// 선택한 오브젝트에 하이어라키 색상 스타일을 적용합니다.
        /// </summary>
        /// <param name="gameObjects">스타일을 적용할 오브젝트 목록입니다.</param>
        /// <param name="colorA">시작 색상입니다.</param>
        /// <param name="colorB">끝 색상입니다.</param>
        /// <param name="useGradient">그라데이션 사용 여부입니다.</param>
        /// <param name="includeChildren">자식 오브젝트까지 포함할지 여부입니다.</param>
        public static void Apply(IEnumerable<GameObject> gameObjects, Color colorA, Color colorB, bool useGradient, bool includeChildren)
        {
            ForEach(gameObjects, includeChildren, gameObject =>
            {
                Entry entry = GetOrCreate(gameObject);
                entry.colorA = ToHtml(colorA);
                entry.colorB = ToHtml(colorB);
                entry.useGradient = useGradient;
                entry.name = gameObject.name;
            });
            Save();
        }

        /// <summary>
        /// 선택한 오브젝트에 하이어라키 아이콘을 적용합니다.
        /// </summary>
        /// <param name="gameObjects">아이콘을 적용할 오브젝트 목록입니다.</param>
        /// <param name="iconName">Unity 내장 아이콘 이름 또는 에셋 아이콘 키입니다.</param>
        /// <param name="includeChildren">자식 오브젝트까지 포함할지 여부입니다.</param>
        public static void SetIcon(IEnumerable<GameObject> gameObjects, string iconName, bool includeChildren)
        {
            ForEach(gameObjects, includeChildren, gameObject =>
            {
                Entry entry = GetOrCreate(gameObject);
                entry.iconName = iconName;
                entry.name = gameObject.name;
                ApplyUnityObjectIcon(gameObject, iconName);
            });
            Save();
        }

        /// <summary>
        /// 선택한 오브젝트에 에셋 Texture 아이콘을 적용합니다.
        /// </summary>
        /// <param name="gameObjects">아이콘을 적용할 오브젝트 목록입니다.</param>
        /// <param name="icon">적용할 Texture2D 아이콘입니다.</param>
        /// <param name="includeChildren">자식 오브젝트까지 포함할지 여부입니다.</param>
        public static void SetAssetIcon(IEnumerable<GameObject> gameObjects, Texture2D icon, bool includeChildren)
        {
            if (icon == null)
                return;

            string path = AssetDatabase.GetAssetPath(icon);
            if (string.IsNullOrEmpty(path))
                return;

            SetIcon(gameObjects, $"asset:{path}", includeChildren);
        }

        /// <summary>
        /// 선택한 오브젝트에 행 스타일을 적용합니다.
        /// </summary>
        /// <param name="gameObjects">스타일을 적용할 오브젝트 목록입니다.</param>
        /// <param name="style">적용할 행 스타일입니다.</param>
        /// <param name="includeChildren">자식 오브젝트까지 포함할지 여부입니다.</param>
        public static void SetStyle(IEnumerable<GameObject> gameObjects, RowStyle style, bool includeChildren)
        {
            ForEach(gameObjects, includeChildren, gameObject =>
            {
                Entry entry = GetOrCreate(gameObject);
                entry.style = style;
                entry.name = gameObject.name;
            });
            Save();
        }

        /// <summary>
        /// 선택한 오브젝트의 하이어라키 스타일과 아이콘을 제거합니다.
        /// </summary>
        /// <param name="gameObjects">스타일을 제거할 오브젝트 목록입니다.</param>
        /// <param name="includeChildren">자식 오브젝트까지 포함할지 여부입니다.</param>
        public static void Clear(IEnumerable<GameObject> gameObjects, bool includeChildren)
        {
            EnsureLoaded();
            ForEach(gameObjects, includeChildren, gameObject =>
            {
                entriesById.Remove(GetGlobalId(gameObject));
                ClearUnityObjectIcon(gameObject);
            });
            Save();
        }

        /// <summary>
        /// 단일 오브젝트의 하이어라키 스타일과 아이콘을 제거합니다.
        /// </summary>
        /// <param name="gameObject">스타일을 제거할 오브젝트입니다.</param>
        public static void Clear(GameObject gameObject)
        {
            if (gameObject == null) return;
            EnsureLoaded();
            entriesById.Remove(GetGlobalId(gameObject));
            ClearUnityObjectIcon(gameObject);
            Save();
        }

        /// <summary>
        /// 저장된 모든 하이어라키 스타일과 아이콘을 제거합니다.
        /// </summary>
        public static void ClearAll()
        {
            EnsureLoaded();
            foreach (Entry entry in entriesById.Values)
                ClearUnityObjectIcon(Resolve(entry.globalId));

            entriesById.Clear();
            Save();
        }

        /// <summary>
        /// 더 이상 존재하지 않는 오브젝트의 저장 데이터를 정리합니다.
        /// </summary>
        public static void CleanMissing()
        {
            EnsureLoaded();
            List<string> missing = new();
            foreach (string id in entriesById.Keys)
            {
                if (Resolve(id) == null)
                    missing.Add(id);
            }

            foreach (string id in missing)
                entriesById.Remove(id);

            Save();
        }

        /// <summary>
        /// 저장된 하이어라키 스타일 항목을 오브젝트와 색상으로 변환해 반환합니다.
        /// </summary>
        /// <returns>오브젝트, 스타일 항목, 시작 색상, 끝 색상 목록입니다.</returns>
        public static List<(GameObject gameObject, Entry entry, Color colorA, Color colorB)> GetEntries()
        {
            EnsureLoaded();
            List<(GameObject, Entry, Color, Color)> result = new();
            foreach (Entry entry in entriesById.Values)
            {
                result.Add((Resolve(entry.globalId), entry, Parse(entry.colorA, defaultA), Parse(entry.colorB, defaultB)));
            }
            return result;
        }

        /// <summary>
        /// 새 GameObject를 만들고 지정한 하이어라키 스타일을 즉시 적용합니다.
        /// </summary>
        /// <param name="name">생성할 오브젝트 이름입니다.</param>
        /// <param name="style">적용할 행 스타일입니다.</param>
        /// <param name="colorA">시작 색상입니다.</param>
        /// <param name="colorB">끝 색상입니다.</param>
        /// <param name="useGradient">그라데이션 사용 여부입니다.</param>
        /// <param name="iconName">적용할 아이콘 이름입니다.</param>
        /// <returns>생성된 GameObject입니다.</returns>
        public static GameObject CreateStyledObject(string name, RowStyle style, Color colorA, Color colorB, bool useGradient, string iconName)
        {
            GameObject gameObject = new(name);
            if (Selection.activeTransform != null)
                gameObject.transform.SetParent(Selection.activeTransform, false);

            Undo.RegisterCreatedObjectUndo(gameObject, $"Create Hierarchy {style}");
            Apply(new[] { gameObject }, colorA, colorB, useGradient, false);
            SetIcon(new[] { gameObject }, iconName, false);
            SetStyle(new[] { gameObject }, style, false);
            Selection.activeGameObject = gameObject;
            EditorSceneManager.MarkSceneDirty(gameObject.scene);
            return gameObject;
        }

        private static void OnHierarchyGUI(int instanceId, Rect rowRect)
        {
            GameObject gameObject = EntityIdToObject(instanceId) as GameObject;
            if (gameObject == null)
                return;

            Event evt = Event.current;
            if (rowRect.Contains(evt.mousePosition))
                hoveredObject = gameObject;

            HandleMouse(gameObject, rowRect);
            HandleShortcuts(evt);

            if (!Enabled || evt.type != EventType.Repaint)
                return;

            if (drawZebraRows && instanceId % 2 != 0)
                DrawFullRow(rowRect, EditorGUIUtility.isProSkin ? new Color(1f, 1f, 1f, 0.025f) : new Color(0f, 0f, 0f, 0.025f));

            Entry entry = TryGetEntry(gameObject);
            if (entry != null)
            {
                DrawEntry(rowRect, entry);
                DrawObjectIcon(rowRect, entry);
            }

            DrawRightTools(gameObject, rowRect, entry);
        }

        /// <summary>
        /// 외부 하이어라키 GUI 흐름에서 오른쪽 보조 도구 영역을 그립니다.
        /// </summary>
        /// <param name="gameObject">대상 GameObject입니다.</param>
        /// <param name="rowRect">하이어라키 행 영역입니다.</param>
        public static void DrawIntegratedRightTools(GameObject gameObject, Rect rowRect)
        {
            if (!Enabled || Event.current.type != EventType.Repaint || gameObject == null)
                return;

            DrawRightTools(gameObject, rowRect, TryGetEntry(gameObject));
        }

        private static void DrawEntry(Rect rowRect, Entry entry)
        {
            Color colorA = Parse(entry.colorA, defaultA);
            Color colorB = Parse(entry.colorB, defaultB);
            bool useGradient = entry.useGradient;

            if (drawBackground || entry.style != RowStyle.Normal)
            {
                Rect backgroundRect = new(0f, rowRect.y, rowRect.xMax + 260f, rowRect.height);
                Color tintA = colorA;
                Color tintB = useGradient ? colorB : colorA;
                tintA.a = entry.style == RowStyle.Normal ? 0.12f : 0.28f;
                tintB.a = entry.style == RowStyle.Normal ? 0.05f : 0.18f;
                DrawGradientRect(backgroundRect, tintA, tintB);
            }

            Rect lineRect = new(0f, rowRect.y, lineWidth, rowRect.height);
            if (useGradient)
                DrawGradientRect(lineRect, colorA, colorB);
            else
                EditorGUI.DrawRect(lineRect, colorA);

            if (entry.style == RowStyle.Divider)
            {
                Rect dividerRect = new(rowRect.x + 10f, rowRect.y + rowRect.height * 0.5f, Mathf.Max(20f, rowRect.width - 120f), 1f);
                if (useGradient)
                    DrawGradientRect(dividerRect, colorA, colorB);
                else
                    EditorGUI.DrawRect(dividerRect, colorA);
            }

            if (entry.style == RowStyle.Folder)
            {
                Rect folderBar = new(rowRect.x - 12f, rowRect.y + 3f, 3f, rowRect.height - 6f);
                EditorGUI.DrawRect(folderBar, colorA);
            }
        }

        private static void DrawObjectIcon(Rect rowRect, Entry entry)
        {
            Texture icon = GetIcon(entry.iconName);
            if (icon == null)
                return;

            Rect iconRect = new(rowRect.x - 18f, rowRect.y, SWEditorUtils.DefaultIconSize, SWEditorUtils.DefaultIconSize);
            GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit);
        }

        private static void DrawRightTools(GameObject gameObject, Rect rowRect, Entry entry)
        {
            float x = rowRect.xMax - 6f;

            if (drawMissingScriptWarning && HasMissingScriptsCached(gameObject))
            {
                x -= 18f;
                Rect warningRect = new(x, rowRect.y + 1f, 14f, 14f);
                GUI.Label(warningRect, new GUIContent("!", "Missing script"));
                x -= 3f;
            }

            if (drawActiveToggle)
            {
                x -= 16f;
                Rect activeRect = new(x, rowRect.y + 3f, 10f, 10f);
                Color color = gameObject.activeSelf ? new Color(0.35f, 0.82f, 0.39f, 0.95f) : new Color(0.6f, 0.6f, 0.6f, 0.45f);
                EditorGUI.DrawRect(activeRect, color);
                x -= 4f;
            }

            if (drawComponentIcons)
                x = DrawComponentIconsRow(gameObject, rowRect, x);

            if (drawComponentMinimap)
                DrawComponentDots(gameObject, rowRect, x);
        }

        private static float DrawComponentIconsRow(GameObject gameObject, Rect rowRect, float startX)
        {
            Component[] components = GetCachedComponents(gameObject);
            if (components == null || components.Length <= 1)
                return startX;

            float x = startX;
            int drawn = 0;
            for (int i = components.Length - 1; i >= 0 && drawn < 6; i--)
            {
                Component component = components[i];
                if (component == null || component is Transform)
                    continue;

                Texture icon = SWEditorUtils.GetComponentIcon(component);
                if (icon == null)
                    continue;

                x -= 16f;
                Rect iconRect = new(x, rowRect.y, SWEditorUtils.DefaultIconSize, SWEditorUtils.DefaultIconSize);
                SWEditorUtils.DrawIcon(iconRect, icon, SWEditorUtils.IsComponentEnabled(component));
                drawn++;
            }

            return x - 3f;
        }

        private static void DrawComponentDots(GameObject gameObject, Rect rowRect, float startX)
        {
            Component[] components = GetCachedComponents(gameObject);
            if (components == null || components.Length <= 1)
                return;

            float x = startX;
            int drawn = 0;
            for (int i = components.Length - 1; i >= 0 && drawn < 5; i--)
            {
                Component component = components[i];
                if (component == null || component is Transform)
                    continue;

                x -= 8f;
                Rect dot = new(x, rowRect.y + 6f, 4f, 4f);
                EditorGUI.DrawRect(dot, componentColors[drawn % componentColors.Length]);
                drawn++;
            }
        }

        private static void HandleMouse(GameObject gameObject, Rect rowRect)
        {
            if (!enabled || !drawActiveToggle || Event.current.type != EventType.MouseDown)
                return;

            Rect activeRect = new(rowRect.xMax - 22f, rowRect.y + 2f, 14f, 14f);
            if (!activeRect.Contains(Event.current.mousePosition))
                return;

            Undo.RecordObject(gameObject, "Toggle Active");
            gameObject.SetActive(!gameObject.activeSelf);
            EditorUtility.SetDirty(gameObject);
            Event.current.Use();
        }

        private static void HandleShortcuts(Event evt)
        {
            if (!enableShortcuts || hoveredObject == null || evt.type != EventType.KeyDown)
                return;

            if (evt.keyCode == KeyCode.A)
            {
                Undo.RecordObject(hoveredObject, "Toggle Active");
                hoveredObject.SetActive(!hoveredObject.activeSelf);
                EditorUtility.SetDirty(hoveredObject);
                evt.Use();
            }
            else if (evt.keyCode == KeyCode.F)
            {
                Selection.activeGameObject = hoveredObject;
                if (SceneView.lastActiveSceneView != null)
                    SceneView.lastActiveSceneView.FrameSelected();
                evt.Use();
            }
            else if (evt.keyCode == KeyCode.X)
            {
                Undo.DestroyObjectImmediate(hoveredObject);
                hoveredObject = null;
                evt.Use();
            }
            else if (evt.keyCode == KeyCode.E)
            {
                SetExpandedRecursive(hoveredObject, true);
                evt.Use();
            }
        }

        private static Entry TryGetEntry(GameObject gameObject)
        {
            EnsureLoaded();
            return entriesByInstanceId.TryGetValue(gameObject.GetInstanceID(), out Entry entry) ? entry : null;
        }

        private static Entry GetOrCreate(GameObject gameObject)
        {
            if (gameObject == null) return null;
            EnsureLoaded();

            string id = GetGlobalId(gameObject);
            if (!entriesById.TryGetValue(id, out Entry entry))
            {
                entry = new Entry
                {
                    globalId = id,
                    name = gameObject.name,
                    colorA = ToHtml(defaultA),
                    colorB = ToHtml(defaultB),
                    useGradient = useGradientByDefault,
                    style = RowStyle.Normal
                };
                entriesById[id] = entry;
            }
            return entry;
        }

        private static void ForEach(IEnumerable<GameObject> gameObjects, bool includeChildren, Action<GameObject> action)
        {
            if (gameObjects == null) return;

            HashSet<GameObject> targets = new();
            foreach (GameObject gameObject in gameObjects)
            {
                if (gameObject == null) continue;
                targets.Add(gameObject);

                if (!includeChildren) continue;
                foreach (Transform child in gameObject.GetComponentsInChildren<Transform>(true))
                {
                    if (child != null && child.gameObject != null)
                        targets.Add(child.gameObject);
                }
            }

            foreach (GameObject target in targets)
                action(target);
        }

        private static void EnsureLoaded()
        {
            if (loaded) return;
            loaded = true;

            entriesById.Clear();
            string json = EditorPrefs.GetString(DataKey, "");
            if (string.IsNullOrEmpty(json)) return;

            EntryList list = JsonUtility.FromJson<EntryList>(json);
            if (list?.entries == null) return;

            foreach (Entry entry in list.entries)
            {
                if (!string.IsNullOrEmpty(entry.globalId))
                {
                    entriesById[entry.globalId] = entry;
                    ApplyUnityObjectIcon(Resolve(entry.globalId), entry.iconName);
                }
            }

            RebuildInstanceCache();
        }

        private static void Save()
        {
            EntryList list = new();
            list.entries.AddRange(entriesById.Values);
            EditorPrefs.SetString(DataKey, JsonUtility.ToJson(list));
            RebuildInstanceCache();
            Repaint();
        }

        private static void SetBool(string key, bool value)
        {
            SWEditorUtils.SavePref(key, value);
            Repaint();
        }

        private static void EnsureSettingsLoaded()
        {
            if (settingsLoaded) return;
            settingsLoaded = true;

            enabled = SWEditorUtils.LoadPref(EnabledKey, true);
            drawBackground = SWEditorUtils.LoadPref(BackgroundKey, true);
            useGradientByDefault = SWEditorUtils.LoadPref(GradientKey, true);
            drawComponentMinimap = SWEditorUtils.LoadPref(ComponentMinimapKey, false);
            drawComponentIcons = SWEditorUtils.LoadPref(ComponentIconsKey, true);
            drawMissingScriptWarning = SWEditorUtils.LoadPref(MissingWarningKey, true);
            drawActiveToggle = SWEditorUtils.LoadPref(ActiveToggleKey, false);
            drawZebraRows = SWEditorUtils.LoadPref(ZebraKey, false);
            enableShortcuts = SWEditorUtils.LoadPref(ShortcutsKey, true);
            lineWidth = SWEditorUtils.LoadPref(LineWidthKey, 4f);
        }

        private static void RebuildInstanceCache()
        {
            entriesByInstanceId.Clear();
            foreach (Entry entry in entriesById.Values)
            {
                GameObject gameObject = Resolve(entry.globalId);
                if (gameObject != null)
                    entriesByInstanceId[gameObject.GetInstanceID()] = entry;
            }
        }

        private static void ClearTransientCaches()
        {
            missingScriptCache.Clear();
            componentCache.Clear();
            RebuildInstanceCache();
        }

        private static bool HasMissingScriptsCached(GameObject gameObject)
        {
            int id = gameObject.GetInstanceID();
            if (missingScriptCache.TryGetValue(id, out bool cached))
                return cached;

            bool hasMissing = SWEditorUtils.HasMissingScripts(gameObject);
            missingScriptCache[id] = hasMissing;
            return hasMissing;
        }

        private static Component[] GetCachedComponents(GameObject gameObject)
        {
            int id = gameObject.GetInstanceID();
            if (componentCache.TryGetValue(id, out Component[] cached) && cached != null)
                return cached;

            Component[] components = gameObject.GetComponents<Component>();
            componentCache[id] = components;
            return components;
        }

        private static void Repaint()
        {
            EditorApplication.RepaintHierarchyWindow();
        }

        private static string GetGlobalId(GameObject gameObject)
        {
            return GlobalObjectId.GetGlobalObjectIdSlow(gameObject).ToString();
        }

        private static GameObject Resolve(string globalId)
        {
            if (string.IsNullOrEmpty(globalId)) return null;
            return GlobalObjectId.TryParse(globalId, out GlobalObjectId id)
                ? GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id) as GameObject
                : null;
        }

        private static string ToHtml(Color color)
        {
            return "#" + ColorUtility.ToHtmlStringRGBA(color);
        }

        private static Color Parse(string html, Color fallback)
        {
            return !string.IsNullOrEmpty(html) && ColorUtility.TryParseHtmlString(html, out Color color) ? color : fallback;
        }

        private static Texture GetIcon(string iconName)
        {
            if (string.IsNullOrEmpty(iconName)) return null;

            if (iconName.StartsWith("asset:", StringComparison.Ordinal))
                return GetAssetIcon(iconName.Substring("asset:".Length));

            if (iconCache.TryGetValue(iconName, out Texture cached)) return cached;

            Texture icon = EditorGUIUtility.IconContent(iconName).image;
            iconCache[iconName] = icon;
            return icon;
        }

        private static Texture2D GetAssetIcon(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath)) return null;
            if (assetIconCache.TryGetValue(assetPath, out Texture2D cached)) return cached;

            Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            assetIconCache[assetPath] = icon;
            return icon;
        }

        private static void ApplyUnityObjectIcon(GameObject gameObject, string iconName)
        {
            if (gameObject == null)
                return;

            if (string.IsNullOrEmpty(iconName))
            {
                ClearUnityObjectIcon(gameObject);
                return;
            }

            Texture2D icon = GetIcon(iconName) as Texture2D;
            if (icon == null)
                return;

            EditorGUIUtility.SetIconForObject(gameObject, icon);
            EditorUtility.SetDirty(gameObject);
            if (gameObject.scene.IsValid())
                EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }

        private static void ClearUnityObjectIcon(GameObject gameObject)
        {
            if (gameObject == null)
                return;

            EditorGUIUtility.SetIconForObject(gameObject, null);
            EditorUtility.SetDirty(gameObject);
            if (gameObject.scene.IsValid())
                EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }

        private static void DrawFullRow(Rect rowRect, Color color)
        {
            EditorGUI.DrawRect(new Rect(0f, rowRect.y, rowRect.xMax + 260f, rowRect.height), color);
        }

        private static UnityEngine.Object EntityIdToObject(int instanceId)
        {
#if UNITY_6000_3_OR_NEWER
            return EditorUtility.EntityIdToObject(instanceId);
#else
            return EditorUtility.InstanceIDToObject(instanceId);
#endif
        }

        private static void DrawGradientRect(Rect rect, Color left, Color right)
        {
            string key = GetGradientKey(left, right);
            if (!gradientTextureCache.TryGetValue(key, out Texture2D texture))
            {
                texture = new Texture2D(2, 1, TextureFormat.RGBA32, false)
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
                texture.SetPixel(0, 0, left);
                texture.SetPixel(1, 0, right);
                texture.Apply(false);
                gradientTextureCache[key] = texture;
            }

            GUI.DrawTexture(rect, texture);
        }

        private static string GetGradientKey(Color left, Color right)
        {
            Color32 l = left;
            Color32 r = right;
            return $"{l.r:X2}{l.g:X2}{l.b:X2}{l.a:X2}-{r.r:X2}{r.g:X2}{r.b:X2}{r.a:X2}";
        }

        private static void SetExpandedRecursive(GameObject gameObject, bool expanded)
        {
            Type type = typeof(EditorWindow).Assembly.GetType("UnityEditor.SceneHierarchyWindow");
            if (type == null) return;

            EditorWindow window = EditorWindow.GetWindow(type);
            MethodInfo method = type.GetMethod("SetExpandedRecursive", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            method?.Invoke(window, new object[] { gameObject.GetInstanceID(), expanded });
            Repaint();
        }
    }
}
