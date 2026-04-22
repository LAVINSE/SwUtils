using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SWTools
{
    /// <summary>
    /// 자주 쓰는 에셋을 즐겨찾기처럼 등록해두고 원클릭으로
    /// Ping / Open / Instantiate 할 수 있는 팔레트 창입니다.
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
            SWEditorUtils.SetupWindow(window, "SW Asset Palette", "d_Favorite Icon", 300, 300);
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
            assetGuids = SWEditorUtils.LoadList(PALETTE_KEY);
        }

        private void SavePalette()
        {
            SWEditorUtils.SaveList(PALETTE_KEY, assetGuids);
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
                Texture icon = SWEditorUtils.GetAssetIcon(asset);

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
            searchFilter = SWEditorUtils.DrawSearchField(searchFilter, 130f);

            EditorGUILayout.EndHorizontal();
        }

        private void DrawDropArea()
        {
            // SWEditorUtils 드래그 앤 드롭 사용
            Object[] dropped = SWEditorUtils.DrawDropArea("여기에 에셋을 드래그해서 등록");
            if (dropped != null)
            {
                foreach (Object obj in dropped)
                {
                    AddAsset(obj);
                }
            }
        }

        private void DrawItems()
        {
            if (items.Count == 0)
            {
                SWEditorUtils.DrawEmptyNotice(
                    "등록된 에셋이 없습니다. 위 드래그 영역이나 '선택 항목 추가'를 사용하세요.");
                return;
            }

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            float rowHeight = iconSize + 8f;

            for (int i = 0; i < items.Count; i++)
            {
                PaletteItem item = items[i];

                if (!SWEditorUtils.MatchesFilter(item.content.text, searchFilter))
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
                    EditorGUI.DrawRect(iconRect, SWEditorUtils.DarkBgColor);
                }
                // SWEditorUtils 드래그 아웃 사용
                if (item.exists)
                {
                    SWEditorUtils.HandleDragOut(iconRect, item.asset, item.content.text);
                }

                // 이름 + 경로
                EditorGUILayout.BeginVertical();
                using (new SWEditorUtils.GUIColorScope(item.exists ? Color.white : SWEditorUtils.ErrorColor))
                {
                    GUILayout.Label(item.content.text, EditorStyles.boldLabel);
                }
                GUILayout.Label(item.path, EditorStyles.miniLabel);
                EditorGUILayout.EndVertical();

                // 액션 버튼
                using (new SWEditorUtils.GUIEnabledScope(item.exists))
                {
                    if (SWEditorUtils.SmallButton("Ping", 45f))
                    {
                        SWEditorUtils.PingAndSelect(item.asset);
                    }
                    if (SWEditorUtils.SmallButton("Open", 45f))
                    {
                        AssetDatabase.OpenAsset(item.asset);
                    }
                    if (item.asset is GameObject)
                    {
                        if (GUILayout.Button("Spawn", GUILayout.Width(55), GUILayout.Height(SWEditorUtils.SmallButtonHeight)))
                        {
                            InstantiatePrefab(item.asset as GameObject);
                        }
                    }
                }

                if (GUILayout.Button("✕", GUILayout.Width(22), GUILayout.Height(SWEditorUtils.SmallButtonHeight)))
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
            string guid = SWEditorUtils.GetAssetGuid(asset);
            if (string.IsNullOrEmpty(guid)) return;

            if (assetGuids.Contains(guid))
            {
                Debug.Log($"[SWTools] 이미 팔레트에 등록된 에셋: {AssetDatabase.GetAssetPath(asset)}");
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
        /// 현재 씬에 프리팹 인스턴스를 생성합니다.
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