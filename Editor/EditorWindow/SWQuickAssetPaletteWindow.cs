// ============================================================================
// 파일 5: SWQuickAssetPaletteWindow.cs
// ============================================================================
// 자주 쓰는 에셋(프리팹, 머티리얼, SO 등)을 즐겨찾기로 등록해놓고
// 원클릭으로 Ping, Open, Instantiate 할 수 있는 팔레트 창입니다.
// ============================================================================

using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SWTools
{
    /// <summary>
    /// 자주 쓰는 에셋을 즐겨찾기처럼 등록해두고 원클릭으로
    /// Ping / Open / Instantiate 할 수 있는 팔레트 창입니다.
    /// SWTestToolsWindow의 Scene 북마크 기능을 에셋으로 확장한 개념.
    /// </summary>
    public class SWQuickAssetPaletteWindow : EditorWindow
    {
        #region 필드
        private const string PALETTE_KEY = "SWTools.QuickAssetPalette.Guids";

        private List<string> assetGuids = new();
        private Vector2 scrollPosition;
        private string searchFilter = "";

        // 표시 캐시
        private readonly List<PaletteItem> items = new();
        private bool needsCacheRebuild = true;

        // 아이콘 크기
        private float iconSize = 32f;
        private const float MIN_ICON = 20f;
        private const float MAX_ICON = 64f;

        private class PaletteItem
        {
            public string guid;
            public string path;
            public Object asset;
            public GUIContent content;
            public bool exists;
        }
        #endregion // 필드

        [MenuItem("SWTools/Quick Asset Palette")]
        public static void ShowWindow()
        {
            SWQuickAssetPaletteWindow window = GetWindow<SWQuickAssetPaletteWindow>();
            window.titleContent = new GUIContent("SW Asset Palette",
                EditorGUIUtility.FindTexture("d_Favorite Icon"));
            window.minSize = new Vector2(300, 300);
            window.Show();
        }

        private void OnEnable()
        {
            LoadPalette();
            needsCacheRebuild = true;
        }

        private void OnDisable()
        {
            SavePalette();
        }

        private void LoadPalette()
        {
            string joined = EditorPrefs.GetString(GetProjectKey(PALETTE_KEY), "");
            assetGuids.Clear();
            if (!string.IsNullOrEmpty(joined))
            {
                string[] guids = joined.Split('|');
                foreach (string g in guids)
                {
                    if (!string.IsNullOrEmpty(g)) assetGuids.Add(g);
                }
            }
        }

        private void SavePalette()
        {
            string joined = string.Join("|", assetGuids);
            EditorPrefs.SetString(GetProjectKey(PALETTE_KEY), joined);
        }

        private string GetProjectKey(string key)
        {
            return $"{key}.{Application.dataPath.GetHashCode()}";
        }

        private void RebuildCache()
        {
            items.Clear();
            foreach (string guid in assetGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                bool exists = !string.IsNullOrEmpty(path);
                Object asset = exists ? AssetDatabase.LoadAssetAtPath<Object>(path) : null;

                string displayName = exists ? System.IO.Path.GetFileNameWithoutExtension(path) : "(없음)";
                Texture icon = asset != null ? AssetPreview.GetMiniThumbnail(asset) : null;

                items.Add(new PaletteItem
                {
                    guid = guid,
                    path = path,
                    asset = asset,
                    content = new GUIContent(displayName, icon, path),
                    exists = exists && asset != null,
                });
            }
            needsCacheRebuild = false;
        }

        private void OnGUI()
        {
            if (needsCacheRebuild) RebuildCache();

            DrawToolbar();
            DrawDropArea();
            EditorGUILayout.Space(5);
            DrawItems();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("새로고침", EditorStyles.toolbarButton, GUILayout.Width(70)))
            {
                needsCacheRebuild = true;
            }

            if (GUILayout.Button("선택 항목 추가", EditorStyles.toolbarButton, GUILayout.Width(110)))
            {
                AddSelection();
            }

            if (GUILayout.Button("없는 항목 정리", EditorStyles.toolbarButton, GUILayout.Width(110)))
            {
                CleanMissing();
            }

            GUILayout.FlexibleSpace();

            GUILayout.Label("아이콘", GUILayout.Width(40));
            iconSize = GUILayout.HorizontalSlider(iconSize, MIN_ICON, MAX_ICON, GUILayout.Width(80));

            GUILayout.Space(5);
            searchFilter = GUILayout.TextField(searchFilter,
                EditorStyles.toolbarSearchField, GUILayout.Width(130));

            EditorGUILayout.EndHorizontal();
        }

        private void DrawDropArea()
        {
            Rect dropRect = GUILayoutUtility.GetRect(0, 32, GUILayout.ExpandWidth(true));
            GUI.Box(dropRect, "여기에 에셋을 드래그해서 등록", EditorStyles.helpBox);

            Event evt = Event.current;
            if (!dropRect.Contains(evt.mousePosition)) return;

            if (evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                if (evt.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();
                    foreach (Object obj in DragAndDrop.objectReferences)
                    {
                        AddAsset(obj);
                    }
                    evt.Use();
                }
            }
        }

        private void DrawItems()
        {
            if (items.Count == 0)
            {
                EditorGUILayout.HelpBox("등록된 에셋이 없습니다. 위 드래그 영역이나 '선택 항목 추가'를 사용하세요.",
                    MessageType.Info);
                return;
            }

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            string filter = searchFilter.Trim().ToLowerInvariant();
            float rowHeight = iconSize + 8f;

            for (int i = 0; i < items.Count; i++)
            {
                PaletteItem item = items[i];

                if (!string.IsNullOrEmpty(filter) &&
                    (item.content.text == null || !item.content.text.ToLowerInvariant().Contains(filter)))
                {
                    continue;
                }

                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox, GUILayout.Height(rowHeight));

                // 아이콘 (드래그 시작점으로도 사용)
                Rect iconRect = GUILayoutUtility.GetRect(iconSize, iconSize,
                    GUILayout.Width(iconSize), GUILayout.Height(iconSize));
                if (item.content.image != null)
                {
                    GUI.DrawTexture(iconRect, item.content.image, ScaleMode.ScaleToFit);
                }
                else
                {
                    EditorGUI.DrawRect(iconRect, new Color(0.2f, 0.2f, 0.2f, 1f));
                }
                HandleDragOut(iconRect, item);

                // 이름 + 경로
                EditorGUILayout.BeginVertical();
                if (!item.exists) GUI.color = Color.red;
                GUILayout.Label(item.content.text, EditorStyles.boldLabel);
                GUI.color = Color.white;
                GUILayout.Label(item.path, EditorStyles.miniLabel);
                EditorGUILayout.EndVertical();

                // 액션 버튼
                GUI.enabled = item.exists;
                if (GUILayout.Button("Ping", GUILayout.Width(45), GUILayout.Height(20)))
                {
                    EditorGUIUtility.PingObject(item.asset);
                    Selection.activeObject = item.asset;
                }
                if (GUILayout.Button("Open", GUILayout.Width(45), GUILayout.Height(20)))
                {
                    AssetDatabase.OpenAsset(item.asset);
                }
                if (item.asset is GameObject)
                {
                    if (GUILayout.Button("Spawn", GUILayout.Width(55), GUILayout.Height(20)))
                    {
                        InstantiatePrefab(item.asset as GameObject);
                    }
                }
                GUI.enabled = true;

                if (GUILayout.Button("✕", GUILayout.Width(22), GUILayout.Height(20)))
                {
                    assetGuids.RemoveAt(i);
                    SavePalette();
                    needsCacheRebuild = true;
                    GUIUtility.ExitGUI();
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// 아이콘 영역에서 시작되는 드래그를 처리해 외부(씬뷰 등)로 끌어다 놓을 수 있게 합니다.
        /// </summary>
        private void HandleDragOut(Rect iconRect, PaletteItem item)
        {
            if (!item.exists) return;

            Event evt = Event.current;
            if (evt.type == EventType.MouseDrag && iconRect.Contains(evt.mousePosition))
            {
                DragAndDrop.PrepareStartDrag();
                DragAndDrop.objectReferences = new[] { item.asset };
                DragAndDrop.paths = new[] { item.path };
                DragAndDrop.StartDrag(item.content.text);
                evt.Use();
            }
        }

        private void AddSelection()
        {
            foreach (Object obj in Selection.objects)
            {
                AddAsset(obj);
            }
        }

        private void AddAsset(Object asset)
        {
            if (asset == null) return;
            string path = AssetDatabase.GetAssetPath(asset);
            if (string.IsNullOrEmpty(path)) return;

            string guid = AssetDatabase.AssetPathToGUID(path);
            if (assetGuids.Contains(guid))
            {
                Debug.Log($"[SWTools] 이미 팔레트에 등록된 에셋: {path}");
                return;
            }

            assetGuids.Add(guid);
            SavePalette();
            needsCacheRebuild = true;
            Repaint();
        }

        private void CleanMissing()
        {
            int before = assetGuids.Count;
            assetGuids.RemoveAll(guid =>
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                return string.IsNullOrEmpty(path);
            });
            int removed = before - assetGuids.Count;
            SavePalette();
            needsCacheRebuild = true;
            Debug.Log($"[SWTools] 없는 항목 {removed}개 정리됨.");
        }

        /// <summary>
        /// 현재 씬에 프리팹 인스턴스를 생성합니다. 선택 오브젝트의 자식으로 생성.
        /// </summary>
        private void InstantiatePrefab(GameObject prefab)
        {
            if (prefab == null) return;

            GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (instance == null) return;

            Transform parent = Selection.activeGameObject != null
                ? Selection.activeGameObject.transform : null;
            if (parent != null)
            {
                instance.transform.SetParent(parent, false);
            }

            Undo.RegisterCreatedObjectUndo(instance, "Spawn From Palette");
            Selection.activeGameObject = instance;
            EditorSceneManager.MarkSceneDirty(instance.scene);
        }
    }
}