using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

using Object = UnityEngine.Object;

namespace SWTools
{
    public static class SWEditorUtils
    {
        #region 색상
        /// <summary> 헤더 구분선 색상 </summary>
        public static readonly Color HeaderLineColor = new(0.3f, 0.3f, 0.3f, 1f);
        /// <summary> 비활성 컴포넌트 아이콘 색상 </summary>
        public static readonly Color DisabledIconColor = new(1f, 1f, 1f, 0.5f);
        /// <summary> 에러/누락 항목 표시 색상 </summary>
        public static readonly Color ErrorColor = new(1f, 0.35f, 0.35f, 1f);
        /// <summary> 강조 표시 색상 </summary>
        public static readonly Color HighlightColor = new(0.3f, 0.7f, 1f, 1f);
        /// <summary> 어두운 배경 (아이콘 placeholder 등) </summary>
        public static readonly Color DarkBgColor = new(0.2f, 0.2f, 0.2f, 1f);
        /// <summary> 성공/활성 상태 배경 색상 </summary>
        public static readonly Color ActiveBgColor = Color.cyan;
        #endregion // 색상

        #region 레이아웃 상수
        /// <summary> 기본 아이콘 크기 (하이어라키, 팔레트 등) </summary>
        public const int DefaultIconSize = 16;
        /// <summary> 기본 버튼 높이 </summary>
        public const float DefaultButtonHeight = 25f;
        /// <summary> 작은 버튼 높이 (Ping, Open 등) </summary>
        public const float SmallButtonHeight = 20f;
        /// <summary> 탭바 높이 </summary>
        public const float TabBarHeight = 25f;
        /// <summary> 기본 탭바 상단 여백 </summary>
        public const float TabBarTopSpace = 5f;
        /// <summary> 기본 탭바 하단 여백 </summary>
        public const float TabBarBottomSpace = 10f;
        #endregion // 레이아웃 상수

        #region 헤더 & 구분선
        /// <summary>
        /// 볼드 라벨 + 하단 구분선으로 이루어진 섹션 헤더를 그립니다.
        /// 모든 SWTools 윈도우에서 DrawHeader 패턴을 통일합니다.
        /// </summary>
        public static void DrawHeader(string title)
        {
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            Rect rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, HeaderLineColor);
            EditorGUILayout.Space(3);
        }

        /// <summary>
        /// 색상을 지정할 수 있는 섹션 헤더를 그립니다.
        /// </summary>
        public static void DrawHeader(string title, Color lineColor)
        {
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            Rect rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, lineColor);
            EditorGUILayout.Space(3);
        }

        /// <summary>
        /// 접기/펼치기가 가능한 Foldout 섹션 헤더를 그립니다.
        /// </summary>
        /// <returns>현재 펼쳐진 상태</returns>
        public static bool DrawFoldoutHeader(string title, bool isOpen)
        {
            isOpen = EditorGUILayout.Foldout(isOpen, title, true, EditorStyles.foldoutHeader);
            if (isOpen)
            {
                Rect rect = EditorGUILayout.GetControlRect(false, 1);
                EditorGUI.DrawRect(rect, HeaderLineColor);
                EditorGUILayout.Space(3);
            }
            return isOpen;
        }

        /// <summary>
        /// 수평 구분선을 그립니다.
        /// </summary>
        public static void DrawSeparator(float height = 1f)
        {
            Rect rect = EditorGUILayout.GetControlRect(false, height);
            EditorGUI.DrawRect(rect, HeaderLineColor);
        }

        /// <summary>
        /// 여백이 포함된 수평 구분선을 그립니다.
        /// </summary>
        public static void DrawSeparatorWithSpace(float spaceBefore = 5f, float spaceAfter = 5f, float height = 1f)
        {
            EditorGUILayout.Space(spaceBefore);
            DrawSeparator(height);
            EditorGUILayout.Space(spaceAfter);
        }

        /// <summary>
        /// 지정된 Rect에 단색 또는 좌우 그라데이션 색상 견본을 그립니다.
        /// </summary>
        public static void DrawColorSwatch(Rect rect, Color colorA, Color colorB)
        {
            if (Mathf.Approximately(colorA.r, colorB.r) &&
                Mathf.Approximately(colorA.g, colorB.g) &&
                Mathf.Approximately(colorA.b, colorB.b) &&
                Mathf.Approximately(colorA.a, colorB.a))
            {
                EditorGUI.DrawRect(rect, colorA);
            }
            else
            {
                int steps = Mathf.Max(1, Mathf.RoundToInt(rect.width));
                float stepWidth = rect.width / steps;

                for (int i = 0; i < steps; i++)
                {
                    float t = steps <= 1 ? 0f : i / (steps - 1f);
                    Rect stepRect = new(rect.x + i * stepWidth, rect.y, stepWidth + 1f, rect.height);
                    EditorGUI.DrawRect(stepRect, Color.Lerp(colorA, colorB, t));
                }
            }

            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 1f), HeaderLineColor);
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), HeaderLineColor);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, 1f, rect.height), HeaderLineColor);
            EditorGUI.DrawRect(new Rect(rect.xMax - 1f, rect.y, 1f, rect.height), HeaderLineColor);
        }
        #endregion // 헤더 & 구분선

        #region 탭바
        /// <summary>
        /// GUILayout.Toolbar 스타일의 탭바를 그립니다.
        /// 모든 SWTools 윈도우에서 동일한 탭바 스타일을 사용합니다.
        /// </summary>
        /// <returns>선택된 탭 인덱스</returns>
        public static int DrawTabBar(int selectedTab, string[] tabNames)
        {
            EditorGUILayout.Space(TabBarTopSpace);
            selectedTab = GUILayout.Toolbar(selectedTab, tabNames, GUILayout.Height(TabBarHeight));
            EditorGUILayout.Space(TabBarBottomSpace);
            return selectedTab;
        }

        /// <summary>
        /// 높이를 지정할 수 있는 탭바를 그립니다.
        /// </summary>
        public static int DrawTabBar(int selectedTab, string[] tabNames, float height)
        {
            EditorGUILayout.Space(TabBarTopSpace);
            selectedTab = GUILayout.Toolbar(selectedTab, tabNames, GUILayout.Height(height));
            EditorGUILayout.Space(TabBarBottomSpace);
            return selectedTab;
        }

        /// <summary>
        /// GUIContent 배열을 받는 탭바를 그립니다.
        /// 아이콘 + 텍스트 조합이 가능합니다.
        /// <code>
        /// private static readonly GUIContent[] tabContents =
        /// {
        ///     new GUIContent("Time", EditorGUIUtility.FindTexture("d_UnityEditor.AnimationWindow")),
        ///     new GUIContent("Performance", EditorGUIUtility.FindTexture("d_Profiler.CPU")),
        /// };
        /// 
        /// selectedTab = SWEditorUtils.DrawTabBar(selectedTab, tabContents);
        /// </code>
        /// </summary>
        /// <returns>선택된 탭 인덱스</returns>
        public static int DrawTabBar(int selectedTab, GUIContent[] tabContents)
        {
            EditorGUILayout.Space(TabBarTopSpace);
            selectedTab = GUILayout.Toolbar(selectedTab, tabContents, GUILayout.Height(TabBarHeight));
            EditorGUILayout.Space(TabBarBottomSpace);
            return selectedTab;
        }

        /// <summary>
        /// GUIContent 배열 + 높이 지정이 가능한 탭바를 그립니다.
        /// </summary>
        public static int DrawTabBar(int selectedTab, GUIContent[] tabContents, float height)
        {
            EditorGUILayout.Space(TabBarTopSpace);
            selectedTab = GUILayout.Toolbar(selectedTab, tabContents, GUILayout.Height(height));
            EditorGUILayout.Space(TabBarBottomSpace);
            return selectedTab;
        }

        /// <summary>
        /// 문자열 배열과 아이콘 이름 배열로 GUIContent 탭 배열을 생성하는 헬퍼입니다.
        /// OnEnable 등에서 한 번만 호출해 캐싱해두면 좋습니다.
        /// <code>
        /// // OnEnable에서 한 번 생성
        /// tabContents = SWEditorUtils.CreateTabContents(
        ///     new[] { "Time", "Perf", "Scene", "Util" },
        ///     new[] { "d_UnityEditor.AnimationWindow", "d_Profiler.CPU", "d_SceneAsset Icon", "d_SettingsIcon" }
        /// );
        /// </code>
        /// </summary>
        /// <param name="labels">탭 텍스트 배열</param>
        /// <param name="iconNames">Unity 내장 아이콘 이름 배열 (null 항목은 아이콘 없이 생성)</param>
        /// <returns>GUIContent 배열</returns>
        public static GUIContent[] CreateTabContents(string[] labels, string[] iconNames)
        {
            GUIContent[] contents = new GUIContent[labels.Length];
            for (int i = 0; i < labels.Length; i++)
            {
                Texture icon = null;
                if (iconNames != null && i < iconNames.Length && !string.IsNullOrEmpty(iconNames[i]))
                {
                    icon = EditorGUIUtility.FindTexture(iconNames[i]);
                }
                contents[i] = icon != null
                    ? new GUIContent(labels[i], icon)
                    : new GUIContent(labels[i]);
            }
            return contents;
        }
        #endregion // 탭바

        #region 스크롤뷰
        /// <summary>
        /// 스크롤뷰를 시작합니다. using 블록과 함께 사용하세요.
        /// <code>
        /// using (var scroll = SWEditorUtils.ScrollView(ref scrollPos))
        /// {
        ///     // 내용 그리기
        /// }
        /// </code>
        /// </summary>
        public static EditorGUILayout.ScrollViewScope ScrollView(ref Vector2 scrollPosition)
        {
            var scope = new EditorGUILayout.ScrollViewScope(scrollPosition);
            scrollPosition = scope.scrollPosition;
            return scope;
        }

        /// <summary>
        /// 스타일이 적용된 스크롤뷰를 시작합니다.
        /// </summary>
        public static EditorGUILayout.ScrollViewScope ScrollView(ref Vector2 scrollPosition, GUIStyle style)
        {
            var scope = new EditorGUILayout.ScrollViewScope(scrollPosition, style);
            scrollPosition = scope.scrollPosition;
            return scope;
        }
        #endregion // 스크롤뷰

        #region 검색 필터
        /// <summary>
        /// 툴바 스타일의 검색 필터 입력 필드를 그립니다.
        /// </summary>
        /// <returns>현재 필터 텍스트</returns>
        public static string DrawSearchField(string searchFilter, float width = 200f)
        {
            return GUILayout.TextField(searchFilter, EditorStyles.toolbarSearchField, GUILayout.Width(width));
        }

        /// <summary>
        /// 검색 필터 텍스트가 대상 문자열에 포함되는지 확인합니다.
        /// 빈 필터는 항상 true를 반환합니다. 대소문자를 구분하지 않습니다.
        /// </summary>
        public static bool MatchesFilter(string text, string filter)
        {
            if (string.IsNullOrEmpty(filter)) return true;
            if (string.IsNullOrEmpty(text)) return false;
            return text.IndexOf(filter.Trim(), StringComparison.OrdinalIgnoreCase) >= 0;
        }
        #endregion // 검색 필터

        #region 드래그 앤 드롭
        /// <summary>
        /// 드래그 앤 드롭 수신 영역을 그립니다.
        /// 드롭된 오브젝트가 있으면 리스트로 반환합니다.
        /// </summary>
        /// <param name="message">영역에 표시할 안내 문구</param>
        /// <param name="height">영역 높이</param>
        /// <returns>드롭된 오브젝트 배열 (드롭이 없으면 null)</returns>
        public static Object[] DrawDropArea(string message = "여기에 드래그해서 등록", float height = 32f)
        {
            Rect dropRect = GUILayoutUtility.GetRect(0, height, GUILayout.ExpandWidth(true));
            GUI.Box(dropRect, message, EditorStyles.helpBox);

            Event evt = Event.current;
            if (!dropRect.Contains(evt.mousePosition)) return null;

            if (evt.type == EventType.DragUpdated)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                evt.Use();
            }
            else if (evt.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                evt.Use();
                return DragAndDrop.objectReferences;
            }

            return null;
        }

        /// <summary>
        /// 특정 타입만 허용하는 드래그 앤 드롭 수신 영역을 그립니다.
        /// </summary>
        /// <typeparam name="T">허용할 오브젝트 타입</typeparam>
        /// <returns>드롭된 해당 타입 오브젝트 리스트 (없으면 null)</returns>
        public static List<T> DrawDropArea<T>(string message = "여기에 드래그해서 등록", float height = 32f)
            where T : Object
        {
            Object[] dropped = DrawDropArea(message, height);
            if (dropped == null) return null;

            List<T> result = new();
            foreach (Object obj in dropped)
            {
                if (obj is T typed)
                {
                    result.Add(typed);
                }
            }
            return result.Count > 0 ? result : null;
        }

        /// <summary>
        /// 아이콘 영역에서 시작되는 드래그 아웃을 처리합니다.
        /// 팔레트 등에서 에셋을 씬뷰로 드래그할 때 사용합니다.
        /// </summary>
        public static void HandleDragOut(Rect dragRect, Object asset, string dragTitle = null)
        {
            if (asset == null) return;

            Event evt = Event.current;
            if (evt.type == EventType.MouseDrag && dragRect.Contains(evt.mousePosition))
            {
                DragAndDrop.PrepareStartDrag();
                DragAndDrop.objectReferences = new[] { asset };

                string path = AssetDatabase.GetAssetPath(asset);
                if (!string.IsNullOrEmpty(path))
                {
                    DragAndDrop.paths = new[] { path };
                }

                DragAndDrop.StartDrag(dragTitle ?? asset.name);
                evt.Use();
            }
        }

        #endregion // 드래그 앤 드롭

        #region 아이콘
        /// <summary>
        /// 지정된 Rect에 텍스처를 그립니다. 비활성 상태일 경우 반투명으로 표시합니다.
        /// </summary>
        public static void DrawIcon(Rect rect, Texture icon, bool isEnabled = true)
        {
            if (icon == null) return;

            Color originalColor = GUI.color;
            GUI.color = isEnabled ? Color.white : DisabledIconColor;
            GUI.DrawTexture(rect, icon, ScaleMode.ScaleToFit);
            GUI.color = originalColor;
        }

        /// <summary>
        /// 컴포넌트의 아이콘 텍스처를 가져옵니다.
        /// MonoBehaviour인 경우 스크립트 파일의 아이콘도 시도합니다.
        /// </summary>
        public static Texture GetComponentIcon(Component component)
        {
            if (component == null) return null;

            Texture icon = AssetPreview.GetMiniThumbnail(component);

            if (icon == null && component is MonoBehaviour monoBehaviour)
            {
                MonoScript script = MonoScript.FromMonoBehaviour(monoBehaviour);
                if (script != null)
                {
                    string path = AssetDatabase.GetAssetPath(script);
                    icon = AssetDatabase.GetCachedIcon(path);
                }
            }

            return icon;
        }

        /// <summary>
        /// 에셋의 미니 썸네일을 가져옵니다.
        /// </summary>
        public static Texture GetAssetIcon(Object asset)
        {
            return asset != null ? AssetPreview.GetMiniThumbnail(asset) : null;
        }

        /// <summary>
        /// 에셋 경로에서 텍스처를 로드합니다.
        /// USS에 정의된 아이콘 경로를 IMGUI에서 사용할 때 유용합니다.
        /// <code>
        /// // USS: background-image: url("/Assets/Editor/Icons/my-icon.png");
        /// // C#:
        /// Texture icon = SWEditorUtils.LoadTexture("Assets/Editor/Icons/my-icon.png");
        /// </code>
        /// </summary>
        public static Texture2D LoadTexture(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath)) return null;
            return AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        }

        /// <summary>
        /// Unity 내장 아이콘을 이름으로 로드합니다.
        /// <code>
        /// Texture icon = SWEditorUtils.LoadBuiltinIcon("d_Profiler.CPU");
        /// </code>
        /// </summary>
        public static Texture LoadBuiltinIcon(string iconName)
        {
            if (string.IsNullOrEmpty(iconName)) return null;

            // FindTexture: Unity 내장 에디터 아이콘 전용
            Texture icon = EditorGUIUtility.FindTexture(iconName);

            // 못 찾으면 IconContent로 재시도 (더 넓은 범위)
            if (icon == null)
            {
                GUIContent content = EditorGUIUtility.IconContent(iconName);
                icon = content?.image;
            }

            return icon;
        }

        /// <summary>
        /// 텍스처를 캐싱하며 로드합니다.
        /// OnGUI에서 매 프레임 호출해도 한 번만 로드합니다.
        /// <code>
        /// // 필드 선언
        /// private static readonly Dictionary&lt;string, Texture&gt; iconCache = new();
        ///
        /// // OnGUI에서
        /// Texture icon = SWEditorUtils.LoadTextureCached("Assets/Icons/star.png", iconCache);
        /// </code>
        /// </summary>
        public static Texture LoadTextureCached(string assetPath, Dictionary<string, Texture> cache)
        {
            if (string.IsNullOrEmpty(assetPath)) return null;

            if (cache.TryGetValue(assetPath, out Texture cached))
            {
                return cached;
            }

            Texture loaded = LoadTexture(assetPath);
            if (loaded == null)
            {
                loaded = LoadBuiltinIcon(assetPath);
            }

            cache[assetPath] = loaded;
            return loaded;
        }

        /// <summary>
        /// 아이콘 + 텍스트로 구성된 GUIContent를 생성합니다.
        /// 에셋 경로 또는 Unity 내장 아이콘 이름 모두 지원합니다.
        /// <code>
        /// // 에셋 경로
        /// GUIContent tab1 = SWEditorUtils.IconContent("Time", "Assets/Icons/time.png");
        /// // Unity 내장 아이콘
        /// GUIContent tab2 = SWEditorUtils.IconContent("Perf", "d_Profiler.CPU");
        /// // 아이콘 없이 (경로가 null이면 텍스트만)
        /// GUIContent tab3 = SWEditorUtils.IconContent("Util", null);
        /// </code>
        /// </summary>
        public static GUIContent IconContent(string label, string iconPathOrName, string tooltip = null)
        {
            Texture icon = null;

            if (!string.IsNullOrEmpty(iconPathOrName))
            {
                // 에셋 경로인지 확인 (Assets/ 또는 Packages/로 시작)
                if (iconPathOrName.StartsWith("Assets/") || iconPathOrName.StartsWith("Packages/"))
                {
                    icon = LoadTexture(iconPathOrName);
                }

                // 에셋에서 못 찾았으면 내장 아이콘으로 시도
                if (icon == null)
                {
                    icon = LoadBuiltinIcon(iconPathOrName);
                }
            }

            return icon != null
                ? new GUIContent(label, icon, tooltip ?? "")
                : new GUIContent(label, tooltip ?? "");
        }
        #endregion // 아이콘

        #region StyleSheet
        /// <summary>
        /// SWEditorUtils 전용 StyleSheet 이름.
        /// 프로젝트 내에 이 이름의 USS 파일을 만들어두면 자동으로 로드됩니다.
        /// </summary>
        private const string UTILS_STYLESHEET_NAME = "SWEditorUtilsStylesheet";

        /// <summary>
        /// 캐싱된 StyleSheet 인스턴스
        /// </summary>
        private static StyleSheet _cachedStyleSheet;

        /// <summary>
        /// StyleSheet 검색 시도 여부 (없는 경우 매번 FindAssets 방지)
        /// </summary>
        private static bool _styleSheetSearched;

        /// <summary>
        /// StyleSheet에서 추출한 커스텀 스타일 캐시
        /// </summary>
        private static SWStyleCache _styleCache;

        /// <summary>
        /// SWEditorUtilsStylesheet를 로드합니다.
        /// 최초 호출 시 한 번만 검색하고 이후 캐싱된 결과를 반환합니다.
        /// </summary>
        public static StyleSheet GetStyleSheet()
        {
            if (!_styleSheetSearched)
            {
                _styleSheetSearched = true;
                string[] guids = AssetDatabase.FindAssets($"{UTILS_STYLESHEET_NAME} t:StyleSheet");
                if (guids.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    _cachedStyleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(path);
                }
                else
                {
                    Debug.LogWarning($"[SWEditorUtils] StyleSheet '{UTILS_STYLESHEET_NAME}'을(를) 찾을 수 없습니다.");
                }
            }
            return _cachedStyleSheet;
        }

        /// <summary>
        /// StyleSheet를 강제로 다시 로드합니다.
        /// USS 파일을 수정한 후 즉시 반영하고 싶을 때 사용합니다.
        /// </summary>
        public static void ReloadStyleSheet()
        {
            _styleSheetSearched = false;
            _cachedStyleSheet = null;
            _styleCache = null;
        }

        /// <summary>
        /// 이름으로 특정 StyleSheet를 로드합니다.
        /// SWMonobehaviourEditorStylesheet 등 다른 USS를 로드할 때 사용합니다.
        /// <code>
        /// StyleSheet ss = SWEditorUtils.FindStyleSheet("SWMonobehaviourEditorStylesheet");
        /// root.styleSheets.Add(ss);
        /// </code>
        /// </summary>
        public static StyleSheet FindStyleSheet(string stylesheetName)
        {
            if (string.IsNullOrEmpty(stylesheetName)) return null;

            string[] guids = AssetDatabase.FindAssets($"{stylesheetName} t:StyleSheet");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                return AssetDatabase.LoadAssetAtPath<StyleSheet>(path);
            }

            Debug.LogWarning($"[SWEditorUtils] StyleSheet '{stylesheetName}'을(를) 찾을 수 없습니다.");
            return null;
        }

        // ---- UIToolkit VisualElement에 StyleSheet 적용 ----

        /// <summary>
        /// VisualElement에 SWEditorUtilsStylesheet를 적용합니다.
        /// <code>
        /// public override VisualElement CreateInspectorGUI()
        /// {
        ///     VisualElement root = new();
        ///     SWEditorUtils.ApplyStyleSheet(root);
        ///     // ... 
        ///     return root;
        /// }
        /// </code>
        /// </summary>
        /// <returns>적용 성공 여부</returns>
        public static bool ApplyStyleSheet(VisualElement root)
        {
            StyleSheet ss = GetStyleSheet();
            if (ss == null || root == null) return false;
            root.styleSheets.Add(ss);
            return true;
        }

        /// <summary>
        /// VisualElement에 이름으로 찾은 StyleSheet를 적용합니다.
        /// </summary>
        public static bool ApplyStyleSheet(VisualElement root, string stylesheetName)
        {
            StyleSheet ss = FindStyleSheet(stylesheetName);
            if (ss == null || root == null) return false;
            root.styleSheets.Add(ss);
            return true;
        }

        /// <summary>
        /// SWEditorUtilsStylesheet의 스타일 캐시를 가져옵니다.
        /// 최초 호출 시 StyleSheet를 로드하고 SWStyleCache를 생성합니다.
        /// <code>
        /// var styles = SWEditorUtils.GetStyleCache();
        /// Texture icon = styles.GetIcon("time");
        /// Color color = styles.GetColor("error");
        /// </code>
        /// </summary>
        public static SWStyleCache GetStyleCache()
        {
            if (_styleCache == null)
            {
                StyleSheet ss = GetStyleSheet();
                if (ss != null)
                {
                    _styleCache = new SWStyleCache(ss);
                }
            }
            return _styleCache;
        }

        /// <summary>
        /// 특정 StyleSheet로 SWStyleCache를 생성합니다.
        /// SWEditorUtilsStylesheet 이외의 USS에서 값을 읽고 싶을 때 사용합니다.
        /// <code>
        /// StyleSheet myUSS = SWEditorUtils.FindStyleSheet("MyCustomStylesheet");
        /// var cache = SWEditorUtils.CreateStyleCache(myUSS);
        /// Texture icon = cache.GetIcon("myicon");
        /// </code>
        /// </summary>
        public static SWStyleCache CreateStyleCache(StyleSheet styleSheet)
        {
            return styleSheet != null ? new SWStyleCache(styleSheet) : null;
        }

        #endregion // StyleSheet

        #region EditorPrefs
        /// <summary>
        /// 현재 프로젝트에 고유한 EditorPrefs 키를 생성합니다.
        /// Application.dataPath의 해시를 접미사로 사용합니다.
        /// </summary>
        public static string GetProjectKey(string baseKey)
        {
            return $"{baseKey}.{Application.dataPath.GetHashCode()}";
        }

        /// <summary>
        /// 프로젝트별 EditorPrefs에 문자열을 저장합니다.
        /// </summary>
        public static void SavePref(string key, string value)
        {
            EditorPrefs.SetString(GetProjectKey(key), value);
        }

        /// <summary>
        /// 프로젝트별 EditorPrefs에서 문자열을 불러옵니다.
        /// </summary>
        public static string LoadPref(string key, string defaultValue = "")
        {
            return EditorPrefs.GetString(GetProjectKey(key), defaultValue);
        }

        /// <summary>
        /// 프로젝트별 EditorPrefs에 int를 저장합니다.
        /// </summary>
        public static void SavePref(string key, int value)
        {
            EditorPrefs.SetInt(GetProjectKey(key), value);
        }

        /// <summary>
        /// 프로젝트별 EditorPrefs에서 int를 불러옵니다.
        /// </summary>
        public static int LoadPref(string key, int defaultValue)
        {
            return EditorPrefs.GetInt(GetProjectKey(key), defaultValue);
        }

        /// <summary>
        /// 프로젝트별 EditorPrefs에 float를 저장합니다.
        /// </summary>
        public static void SavePref(string key, float value)
        {
            EditorPrefs.SetFloat(GetProjectKey(key), value);
        }

        /// <summary>
        /// 프로젝트별 EditorPrefs에서 float를 불러옵니다.
        /// </summary>
        public static float LoadPref(string key, float defaultValue)
        {
            return EditorPrefs.GetFloat(GetProjectKey(key), defaultValue);
        }

        /// <summary>
        /// 프로젝트별 EditorPrefs에 bool을 저장합니다.
        /// </summary>
        public static void SavePref(string key, bool value)
        {
            EditorPrefs.SetBool(GetProjectKey(key), value);
        }

        /// <summary>
        /// 프로젝트별 EditorPrefs에서 bool을 불러옵니다.
        /// </summary>
        public static bool LoadPref(string key, bool defaultValue)
        {
            return EditorPrefs.GetBool(GetProjectKey(key), defaultValue);
        }

        // ---- 리스트 저장/로드 (구분자 '|') ----

        /// <summary>
        /// 문자열 리스트를 EditorPrefs에 '|' 구분자로 저장합니다.
        /// </summary>
        public static void SaveList(string key, List<string> list)
        {
            string joined = string.Join("|", list);
            EditorPrefs.SetString(GetProjectKey(key), joined);
        }

        /// <summary>
        /// EditorPrefs에서 '|' 구분자로 저장된 문자열 리스트를 불러옵니다.
        /// </summary>
        public static List<string> LoadList(string key)
        {
            string joined = EditorPrefs.GetString(GetProjectKey(key), "");
            List<string> result = new();

            if (!string.IsNullOrEmpty(joined))
            {
                string[] entries = joined.Split('|');
                foreach (string entry in entries)
                {
                    if (!string.IsNullOrEmpty(entry))
                    {
                        result.Add(entry);
                    }
                }
            }

            return result;
        }
        #endregion // EditorPrefs

        #region 오브젝트 조작
        /// <summary>
        /// 프로젝트 창에서 오브젝트를 Ping하고 Selection에 설정합니다.
        /// </summary>
        public static void PingAndSelect(Object obj)
        {
            if (obj == null) return;
            EditorGUIUtility.PingObject(obj);
            Selection.activeObject = obj;
        }

        /// <summary>
        /// 에셋 경로를 통해 에셋을 Ping하고 선택합니다.
        /// </summary>
        public static void PingAssetAtPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return;
            Object asset = AssetDatabase.LoadAssetAtPath<Object>(path);
            PingAndSelect(asset);
        }

        /// <summary>
        /// 컴포넌트의 enabled 프로퍼티 값을 가져옵니다.
        /// enabled 프로퍼티가 없으면 true를 반환합니다.
        /// </summary>
        public static bool IsComponentEnabled(Component component)
        {
            if (component == null) return false;
            PropertyInfo prop = component.GetType().GetProperty("enabled",
                BindingFlags.Public | BindingFlags.Instance);
            return prop == null || (bool)prop.GetValue(component);
        }

        /// <summary>
        /// 컴포넌트의 enabled 상태를 토글합니다. Undo를 지원합니다.
        /// </summary>
        public static void ToggleComponentEnabled(Component component)
        {
            if (component == null) return;
            PropertyInfo prop = component.GetType().GetProperty("enabled",
                BindingFlags.Public | BindingFlags.Instance);

            if (prop != null && prop.CanWrite)
            {
                Undo.RecordObject(component, $"Toggle {component.GetType().Name}");
                bool current = (bool)prop.GetValue(component);
                prop.SetValue(component, !current);
                EditorUtility.SetDirty(component);
            }
        }
        #endregion // 오브젝트 조작

        #region 버튼
        /// <summary>
        /// 표준 높이의 버튼을 그립니다.
        /// </summary>
        public static bool Button(string label)
        {
            return GUILayout.Button(label, GUILayout.Height(DefaultButtonHeight));
        }

        /// <summary>
        /// 작은 크기의 버튼을 그립니다. (Ping, Open 등에 적합)
        /// </summary>
        public static bool SmallButton(string label, float width = 45f)
        {
            return GUILayout.Button(label, GUILayout.Width(width), GUILayout.Height(SmallButtonHeight));
        }

        /// <summary>
        /// 배경색이 적용된 토글 버튼을 그립니다.
        /// 활성 상태이면 지정된 색상, 비활성이면 기본 흰색으로 표시합니다.
        /// </summary>
        /// <returns>클릭 여부</returns>
        public static bool ToggleButton(string label, bool isActive, Color activeColor,
            float height = 0f)
        {
            if (height <= 0f) height = DefaultButtonHeight;

            using (new GUIBgColorScope(isActive ? activeColor : Color.white))
            {
                return GUILayout.Button(label, GUILayout.Height(height));
            }
        }

        /// <summary>
        /// 확인 다이얼로그가 포함된 위험 작업 버튼을 그립니다.
        /// </summary>
        /// <returns>사용자가 확인을 눌렀으면 true</returns>
        public static bool DangerButton(string buttonLabel, string dialogTitle,
            string dialogMessage, string okLabel = "실행", string cancelLabel = "취소")
        {
            if (GUILayout.Button(buttonLabel, GUILayout.Height(DefaultButtonHeight)))
            {
                return EditorUtility.DisplayDialog(dialogTitle, dialogMessage, okLabel, cancelLabel);
            }
            return false;
        }
        #endregion // 버튼

        #region 도움말
        /// <summary>
        /// 플레이 모드 전용 기능에 대한 안내 메시지를 표시합니다.
        /// 플레이 중이 아니면 HelpBox를 그리고 true를 반환합니다.
        /// <code>
        /// if (SWEditorUtils.DrawPlayModeOnlyNotice()) return;
        /// // 여기부터 플레이 모드 전용 코드
        /// </code>
        /// </summary>
        /// <returns>플레이 모드가 아니면 true (= 이후 로직을 건너뛰어야 함)</returns>
        public static bool DrawPlayModeOnlyNotice(string message = "플레이 중에만 실시간 정보가 표시됩니다.")
        {
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox(message, MessageType.Info);
                return true;
            }
            return false;
        }

        /// <summary>
        /// 빈 상태에 대한 안내 메시지를 표시합니다.
        /// </summary>
        public static void DrawEmptyNotice(string message, MessageType type = MessageType.Info)
        {
            EditorGUILayout.HelpBox(message, type);
        }
        #endregion // 도움말

        #region EditorWindow 헬퍼
        /// <summary>
        /// EditorWindow의 기본 설정을 적용합니다. (타이틀, 아이콘, 최소 크기)
        /// <code>
        /// // OnEnable 또는 ShowWindow에서:
        /// SWEditorUtils.SetupWindow(this, "SW My Tool", "d_SettingsIcon", 320, 400);
        /// </code>
        /// </summary>
        public static void SetupWindow(EditorWindow window, string title,
            string iconName = null, float minWidth = 320f, float minHeight = 400f)
        {
            Texture icon = !string.IsNullOrEmpty(iconName)
                ? EditorGUIUtility.FindTexture(iconName)
                : null;

            window.titleContent = icon != null
                ? new GUIContent(title, icon)
                : new GUIContent(title);

            window.minSize = new Vector2(minWidth, minHeight);
        }
        #endregion // EditorWindow 헬퍼

        #region 리페인트 타이머
        /// <summary>
        /// 일정 간격으로 리페인트 여부를 판단합니다.
        /// <code>
        /// private double lastRepaintTime;
        ///
        /// void OnEditorUpdate()
        /// {
        ///     if (SWEditorUtils.ShouldRepaint(ref lastRepaintTime, 0.05))
        ///         Repaint();
        /// }
        /// </code>
        /// </summary>
        /// <returns>리페인트해야 하면 true</returns>
        public static bool ShouldRepaint(ref double lastRepaintTime, double interval = 0.05)
        {
            double now = EditorApplication.timeSinceStartup;
            if (now - lastRepaintTime >= interval)
            {
                lastRepaintTime = now;
                return true;
            }
            return false;
        }
        #endregion // 리페인트 타이머

        #region 리플렉션
        /// <summary>
        /// 리플렉션을 사용해 Console 로그를 클리어합니다.
        /// </summary>
        public static void ClearConsole()
        {
            var logEntries = Type.GetType("UnityEditor.LogEntries, UnityEditor");
            var clearMethod = logEntries?.GetMethod("Clear",
                BindingFlags.Static | BindingFlags.Public);
            clearMethod?.Invoke(null, null);
        }
        #endregion // 리플렉션

        #region 에셋 유틸리티
        /// <summary>
        /// 에셋의 GUID를 반환합니다. 에셋이 아니면 null을 반환합니다.
        /// </summary>
        public static string GetAssetGuid(Object asset)
        {
            if (asset == null) return null;
            string path = AssetDatabase.GetAssetPath(asset);
            return string.IsNullOrEmpty(path) ? null : AssetDatabase.AssetPathToGUID(path);
        }

        /// <summary>
        /// GUID에서 에셋을 로드합니다. 존재하지 않으면 null을 반환합니다.
        /// </summary>
        public static T LoadAssetFromGuid<T>(string guid) where T : Object
        {
            if (string.IsNullOrEmpty(guid)) return null;
            string path = AssetDatabase.GUIDToAssetPath(guid);
            return string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<T>(path);
        }

        /// <summary>
        /// Missing Script가 있는지 확인합니다.
        /// </summary>
        public static bool HasMissingScripts(GameObject obj)
        {
            if (obj == null) return false;
            var components = obj.GetComponents<MonoBehaviour>();
            foreach (var c in components)
            {
                if (c == null) return true;
            }
            return false;
        }
        #endregion // 에셋 유틸리티

        #region 포맷
        /// <summary>
        /// 바이트 크기를 사람이 읽기 쉬운 형태로 변환합니다.
        /// 예: 1536 → "1.5 KB", 2097152 → "2.0 MB"
        /// </summary>
        public static string FormatBytes(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024f:F1} KB";
            if (bytes < 1024 * 1024 * 1024) return $"{bytes / (1024f * 1024f):F1} MB";
            return $"{bytes / (1024f * 1024f * 1024f):F2} GB";
        }

        /// <summary>
        /// 경과 시간(초)을 읽기 쉬운 형태로 변환합니다.
        /// 예: 65.3 → "1m 5.3s", 3661 → "1h 1m 1s"
        /// </summary>
        public static string FormatDuration(double seconds)
        {
            if (seconds < 60) return $"{seconds:F1}s";
            if (seconds < 3600)
            {
                int m = (int)(seconds / 60);
                double s = seconds - m * 60;
                return $"{m}m {s:F1}s";
            }

            int h = (int)(seconds / 3600);
            int min = (int)((seconds - h * 3600) / 60);
            int sec = (int)(seconds - h * 3600 - min * 60);
            return $"{h}h {min}m {sec}s";
        }
        #endregion // 포맷

        /// <summary>
        /// SWEditorUtilsStylesheet에 정의된 스타일 값을 캐싱해서 관리하는 클래스입니다.
        /// USS에서 커스텀 변수(--sw-xxx)를 정의하고, 이 클래스에서 파싱해 IMGUI에 전달합니다.
        ///
        /// USS 파일 예시 (SWEditorUtilsStylesheet.uss):
        /// <code>
        /// /* 아이콘 정의 */
        /// .sw-icon-time     { background-image: url("/Assets/Editor/Icons/time.png"); }
        /// .sw-icon-perf     { background-image: url("/Assets/Editor/Icons/perf.png"); }
        /// .sw-icon-scene    { background-image: url("/Assets/Editor/Icons/scene.png"); }
        /// .sw-icon-settings { background-image: url("/Assets/Editor/Icons/settings.png"); }
        ///
        /// /* 색상 정의 */
        /// .sw-color-header-line { background-color: rgb(77, 77, 77); }
        /// .sw-color-error       { background-color: rgb(255, 89, 89); }
        /// .sw-color-highlight   { background-color: rgb(77, 179, 255); }
        /// .sw-color-active      { background-color: rgb(0, 255, 255); }
        ///
        /// /* 레이아웃 */
        /// .sw-layout { height: 25px; min-width: 320px; min-height: 400px; }
        /// </code>
        ///
        /// C# 사용 예시:
        /// <code>
        /// var styles = SWEditorUtils.GetStyleCache();
        ///
        /// // 아이콘 가져오기 (USS .sw-icon-time의 background-image)
        /// Texture icon = styles.GetIcon("time");
        ///
        /// // 색상 가져오기 (USS .sw-color-header-line의 background-color)
        /// Color color = styles.GetColor("header-line");
        ///
        /// // 탭바에 USS 아이콘 적용
        /// var tabs = new GUIContent[]
        /// {
        ///     new GUIContent("Time", styles.GetIcon("time")),
        ///     new GUIContent("Perf", styles.GetIcon("perf")),
        /// };
        /// selectedTab = SWEditorUtils.DrawTabBar(selectedTab, tabs);
        /// </code>
        /// </summary>
        public class SWStyleCache
        {
            private readonly StyleSheet _styleSheet;

            // USS에서 읽어온 값 캐시
            private readonly Dictionary<string, Texture2D> _iconCache = new();
            private readonly Dictionary<string, Color> _colorCache = new();

            // USS 파싱용 임시 VisualElement
            private VisualElement _probeRoot;

            public SWStyleCache(StyleSheet styleSheet)
            {
                _styleSheet = styleSheet;
            }

            /// <summary>
            /// USS에 정의된 .sw-icon-{name} 클래스의 background-image를 가져옵니다.
            /// <code>
            /// // USS: .sw-icon-time { background-image: url("/Assets/Icons/time.png"); }
            /// Texture icon = styles.GetIcon("time");
            /// </code>
            /// </summary>
            public Texture2D GetIcon(string name)
            {
                if (_iconCache.TryGetValue(name, out Texture2D cached))
                    return cached;

                Texture2D icon = ExtractBackgroundImage($"sw-icon-{name}");
                _iconCache[name] = icon;
                return icon;
            }

            /// <summary>
            /// USS에 정의된 .sw-color-{name} 클래스의 background-color를 가져옵니다.
            /// <code>
            /// // USS: .sw-color-error { background-color: rgb(255, 89, 89); }
            /// Color errorColor = styles.GetColor("error");
            /// </code>
            /// </summary>
            public Color GetColor(string name, Color fallback = default)
            {
                if (_colorCache.TryGetValue(name, out Color cached))
                    return cached;

                Color color = ExtractBackgroundColor($"sw-color-{name}", fallback);
                _colorCache[name] = color;
                return color;
            }

            /// <summary>
            /// 임의의 USS 클래스명에서 background-image를 추출합니다.
            /// </summary>
            public Texture2D GetIconByClass(string className)
            {
                if (_iconCache.TryGetValue(className, out Texture2D cached))
                    return cached;

                Texture2D icon = ExtractBackgroundImage(className);
                _iconCache[className] = icon;
                return icon;
            }

            /// <summary>
            /// 임의의 USS 클래스명에서 background-color를 추출합니다.
            /// </summary>
            public Color GetColorByClass(string className, Color fallback = default)
            {
                if (_colorCache.TryGetValue(className, out Color cached))
                    return cached;

                Color color = ExtractBackgroundColor(className, fallback);
                _colorCache[className] = color;
                return color;
            }

            /// <summary>
            /// 캐시를 초기화합니다. USS 파일 수정 후 호출하세요.
            /// </summary>
            public void ClearCache()
            {
                _iconCache.Clear();
                _colorCache.Clear();
            }

            private VisualElement CreateProbe(string className)
            {
                if (_probeRoot == null)
                {
                    _probeRoot = new VisualElement();
                    _probeRoot.styleSheets.Add(_styleSheet);
                }

                VisualElement probe = new VisualElement();
                probe.AddToClassList(className);
                _probeRoot.Add(probe);
                return probe;
            }

            private Texture2D ExtractBackgroundImage(string className)
            {
                VisualElement probe = CreateProbe(className);
                var bg = probe.resolvedStyle.backgroundImage;
                Texture2D tex = bg.texture;

                // resolvedStyle이 안 되면 customStyle로 시도
                if (tex == null)
                {
                    // USS에서 직접 에셋 경로를 파싱하는 대안
                    tex = TryExtractTextureFromStyleSheet(className);
                }

                probe.RemoveFromHierarchy();
                return tex;
            }

            private Color ExtractBackgroundColor(string className, Color fallback)
            {
                VisualElement probe = CreateProbe(className);
                Color color = probe.resolvedStyle.backgroundColor;

                // 투명(기본값)이면 fallback 반환
                if (color.a < 0.01f)
                    color = fallback;

                probe.RemoveFromHierarchy();
                return color;
            }

            /// <summary>
            /// StyleSheet 내부를 리플렉션으로 탐색해 텍스처를 찾는 대안 방식입니다.
            /// resolvedStyle이 에디터 컨텍스트에서 동작하지 않을 때 사용합니다.
            /// </summary>
            private Texture2D TryExtractTextureFromStyleSheet(string className)
            {
                if (_styleSheet == null) return null;

                // StyleSheet에서 직접 규칙을 읽는 공식 API가 없으므로
                // 에셋 경로 컨벤션으로 대체: .sw-icon-{name} → 같은 폴더의 아이콘 에셋 검색
                // 클래스명에서 이름 추출 (sw-icon-time → time)
                string iconName = className;
                if (iconName.StartsWith("sw-icon-"))
                    iconName = iconName.Substring("sw-icon-".Length);

                // StyleSheet가 위치한 폴더에서 아이콘 검색
                string ssPath = AssetDatabase.GetAssetPath(_styleSheet);
                if (string.IsNullOrEmpty(ssPath)) return null;

                string ssFolder = System.IO.Path.GetDirectoryName(ssPath);
                string[] guids = AssetDatabase.FindAssets($"{iconName} t:Texture2D", new[] { ssFolder });
                if (guids.Length > 0)
                {
                    string texPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                    return AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
                }

                // 프로젝트 전체에서 검색
                guids = AssetDatabase.FindAssets($"sw-icon-{iconName} t:Texture2D");
                if (guids.Length > 0)
                {
                    string texPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                    return AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
                }

                return null;
            }
        }

        /// <summary>
        /// GUI.color를 임시로 변경했다가 자동 복원하는 스코프.
        /// <code>
        /// using (new SWEditorUtils.GUIColorScope(Color.red))
        /// {
        ///     GUILayout.Label("빨간 텍스트");
        /// }
        /// // 여기서 원래 색상 자동 복원
        /// </code>
        /// </summary>
        public struct GUIColorScope : IDisposable
        {
            private readonly Color _previousColor;

            public GUIColorScope(Color newColor)
            {
                _previousColor = GUI.color;
                GUI.color = newColor;
            }

            public void Dispose()
            {
                GUI.color = _previousColor;
            }
        }

        /// <summary>
        /// GUI.backgroundColor를 임시로 변경했다가 자동 복원하는 스코프.
        /// <code>
        /// using (new SWEditorUtils.GUIBgColorScope(Color.cyan))
        /// {
        ///     GUILayout.Button("강조 버튼");
        /// }
        /// </code>
        /// </summary>
        public struct GUIBgColorScope : IDisposable
        {
            private readonly Color _previousColor;

            public GUIBgColorScope(Color newColor)
            {
                _previousColor = GUI.backgroundColor;
                GUI.backgroundColor = newColor;
            }

            public void Dispose()
            {
                GUI.backgroundColor = _previousColor;
            }
        }

        /// <summary>
        /// GUI.enabled를 임시로 변경했다가 자동 복원하는 스코프.
        /// <code>
        /// using (new SWEditorUtils.GUIEnabledScope(false))
        /// {
        ///     EditorGUILayout.TextField("읽기 전용", value);
        /// }
        /// </code>
        /// </summary>
        public struct GUIEnabledScope : IDisposable
        {
            private readonly bool _previousEnabled;

            public GUIEnabledScope(bool enabled)
            {
                _previousEnabled = GUI.enabled;
                GUI.enabled = enabled;
            }

            public void Dispose()
            {
                GUI.enabled = _previousEnabled;
            }
        }
    }
}
