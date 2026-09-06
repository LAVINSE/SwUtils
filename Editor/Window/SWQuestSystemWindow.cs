using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

using Object = UnityEngine.Object;

using SW.Base;

using SW.EditorTools.Util;

using SW.Quest;

namespace SW.EditorTools.Window
{
    /// <summary>
    /// 퀘스트, 업적과 관련 구성 에셋을 한 화면에서 생성, 복제, 삭제하고 편집하는 창입니다.
    /// </summary>
    /// <remarks>
    /// 프로젝트에서 구현한 파생 조건, 보상, 대상과 진행 계산 타입도 자동으로 찾아 생성 목록에 표시합니다.
    /// </remarks>
    public sealed class SWQuestSystemWindow : EditorWindow
    {
        #region 타입
        /// <summary>
        /// 창에서 관리하는 에셋 분류입니다.
        /// </summary>
        private enum ManagedAssetKind
        {
            Quest,
            Achievement,
            Task,
            Target,
            Condition,
            Reward,
            TaskAction,
            InitialProgressValue,
            QuestDatabase,
            AchievementDatabase
        }

        /// <summary>
        /// 목록 정렬 방식입니다.
        /// </summary>
        private enum AssetSortMode
        {
            CodeName,
            AssetName,
            TypeName
        }
        #endregion // 타입

        #region 상수
        private const string PreferenceKeyPrefix = "SWTools.QuestEditor.";
        private const float DefaultListWidth = 330f;

        private static readonly ManagedAssetKind[] ManagedAssetKinds =
        {
            ManagedAssetKind.Quest,
            ManagedAssetKind.Achievement,
            ManagedAssetKind.Task,
            ManagedAssetKind.Target,
            ManagedAssetKind.Condition,
            ManagedAssetKind.Reward,
            ManagedAssetKind.TaskAction,
            ManagedAssetKind.InitialProgressValue,
            ManagedAssetKind.QuestDatabase,
            ManagedAssetKind.AchievementDatabase
        };

        private static readonly string[] ToolbarNames =
        {
            "퀘스트",
            "업적",
            "작업",
            "대상",
            "조건",
            "보상",
            "진행 계산",
            "시작 진행값",
            "퀘스트 데이터베이스",
            "업적 데이터베이스",
            "설정"
        };

        private static readonly string[] SortModeNames =
        {
            "코드명순",
            "에셋 이름순",
            "타입 이름순"
        };

        private static readonly string[] DefaultCreatePaths =
        {
            "Assets/SWData/Quest/Quest",
            "Assets/SWData/Quest/Achievement",
            "Assets/SWData/Quest/Task",
            "Assets/SWData/Quest/Target",
            "Assets/SWData/Quest/Condition",
            "Assets/SWData/Quest/Reward",
            "Assets/SWData/Quest/TaskAction",
            "Assets/SWData/Quest/InitialProgress",
            "Assets/SWData/Quest/Database",
            "Assets/SWData/Quest/Database"
        };

        private static readonly string[] DefaultNamePrefixes =
        {
            "QUEST_",
            "ACHIEVEMENT_",
            "TASK_",
            "TARGET_",
            "CONDITION_",
            "REWARD_",
            "TASK_ACTION_",
            "INITIAL_PROGRESS_",
            "QUEST_DATABASE_",
            "ACHIEVEMENT_DATABASE_"
        };
        #endregion // 상수

        #region 필드
        private readonly Dictionary<ManagedAssetKind, List<ScriptableObject>> assetsByKind = new();
        private readonly Dictionary<ManagedAssetKind, ScriptableObject> selectedAssetsByKind = new();
        private readonly Dictionary<ManagedAssetKind, Vector2> listScrollPositionsByKind = new();
        private readonly Dictionary<ManagedAssetKind, string> searchTextsByKind = new();
        private readonly Dictionary<ManagedAssetKind, Type[]> creationTypesByKind = new();
        private readonly Dictionary<ManagedAssetKind, int> creationTypeIndexesByKind = new();

        private string[] createPaths;
        private string[] namePrefixes;
        private int toolbarIndex;
        private AssetSortMode sortMode;
        private bool saveAssetsAutomatically = true;
        private float listWidth = DefaultListWidth;
        private Vector2 inspectorScrollPosition;
        private Vector2 settingsScrollPosition;
        private Editor cachedEditor;
        #endregion // 필드

        /// <summary>
        /// 퀘스트 시스템 관리 창을 엽니다.
        /// </summary>
        [MenuItem("SWTools/Utils/Data/Quest System Editor")]
        public static void ShowWindow()
        {
            SWQuestSystemWindow window = GetWindow<SWQuestSystemWindow>();
            SWEditorUtils.SetupWindow(window, "SW Quest System", "d_ScriptableObject Icon", 980, 560);
            window.Show();
        }

        #region 생명주기
        private void OnEnable()
        {
            LoadSettings();

            for (int index = 0; index < ManagedAssetKinds.Length; index++)
            {
                ManagedAssetKind kind = ManagedAssetKinds[index];
                assetsByKind.TryAdd(kind, new List<ScriptableObject>());
                selectedAssetsByKind.TryAdd(kind, null);
                listScrollPositionsByKind.TryAdd(kind, Vector2.zero);
                searchTextsByKind.TryAdd(kind, string.Empty);
                creationTypeIndexesByKind.TryAdd(kind, 0);
                creationTypesByKind[kind] = FindCreationTypes(kind);
                RefreshAssets(kind);
            }
        }

        private void OnDisable()
        {
            SaveSettings();
            DestroyImmediate(cachedEditor);
        }

        private void OnProjectChange()
        {
            RefreshAllAssets();
            Repaint();
        }
        #endregion // 생명주기

        #region 화면
        private void OnGUI()
        {
            toolbarIndex = SWEditorUtils.DrawTabBar(toolbarIndex, ToolbarNames);
            if (toolbarIndex >= ManagedAssetKinds.Length)
            {
                DrawSettings();
                return;
            }

            DrawAssetManagement(ManagedAssetKinds[toolbarIndex]);
        }

        /// <summary>
        /// 선택한 분류의 에셋 목록과 인스펙터를 그립니다.
        /// </summary>
        private void DrawAssetManagement(ManagedAssetKind kind)
        {
            EditorGUILayout.BeginHorizontal();
            {
                DrawAssetListPanel(kind);
                DrawInspectorPanel(kind);
            }
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// 에셋 검색, 생성과 목록 영역을 그립니다.
        /// </summary>
        private void DrawAssetListPanel(ManagedAssetKind kind)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(listWidth));
            {
                SWEditorUtils.DrawHeader($"{GetKindLabel(kind)} 목록 ({assetsByKind[kind].Count})");
                DrawCreationControls(kind);

                EditorGUILayout.BeginHorizontal();
                {
                    searchTextsByKind[kind] = GUILayout.TextField(
                        searchTextsByKind[kind], EditorStyles.toolbarSearchField);

                    sortMode = (AssetSortMode)EditorGUILayout.Popup(
                        (int)sortMode, SortModeNames, GUILayout.Width(104f));

                    if (GUILayout.Button("새로 고침", EditorStyles.miniButton, GUILayout.Width(70f)))
                    {
                        RefreshAssets(kind);
                    }
                }
                EditorGUILayout.EndHorizontal();

                listScrollPositionsByKind[kind] = EditorGUILayout.BeginScrollView(
                    listScrollPositionsByKind[kind]);
                DrawAssetRows(kind);
                EditorGUILayout.EndScrollView();
            }
            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 생성 타입 선택과 주요 관리 버튼을 그립니다.
        /// </summary>
        private void DrawCreationControls(ManagedAssetKind kind)
        {
            Type[] creationTypes = creationTypesByKind[kind];
            string[] creationTypeNames = GetCreationTypeNames(creationTypes);

            EditorGUILayout.BeginHorizontal();
            {
                if (creationTypeNames.Length > 0)
                {
                    creationTypeIndexesByKind[kind] = EditorGUILayout.Popup(
                        creationTypeIndexesByKind[kind], creationTypeNames);
                }

                using (new EditorGUI.DisabledScope(creationTypes.Length == 0))
                {
                    if (GUILayout.Button("생성", GUILayout.Width(48f)))
                    {
                        CreateAsset(kind);
                    }
                }
            }
            EditorGUILayout.EndHorizontal();

            ScriptableObject selectedAsset = selectedAssetsByKind[kind];
            EditorGUILayout.BeginHorizontal();
            {
                using (new EditorGUI.DisabledScope(selectedAsset == null))
                {
                    if (GUILayout.Button("복제"))
                    {
                        DuplicateAsset(kind, selectedAsset);
                    }

                    if (GUILayout.Button("위치 표시"))
                    {
                        SWEditorUtils.PingAndSelect(selectedAsset);
                    }

                    if (GUILayout.Button("삭제"))
                    {
                        DeleteAsset(kind, selectedAsset);
                    }
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// 검색과 정렬을 반영한 에셋 행을 그립니다.
        /// </summary>
        private void DrawAssetRows(ManagedAssetKind kind)
        {
            List<ScriptableObject> visibleAssets = new(assetsByKind[kind]);
            visibleAssets.Sort(CompareAssets);
            string searchText = searchTextsByKind[kind];
            int visibleCount = 0;

            for (int index = 0; index < visibleAssets.Count; index++)
            {
                ScriptableObject asset = visibleAssets[index];
                if (asset == null || !MatchesSearch(asset, searchText))
                {
                    continue;
                }

                visibleCount++;
                bool selected = selectedAssetsByKind[kind] == asset;
                Color originalBackgroundColor = GUI.backgroundColor;
                if (selected)
                {
                    GUI.backgroundColor = SWEditorUtils.HighlightColor;
                }

                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                GUI.backgroundColor = originalBackgroundColor;
                {
                    Texture icon = AssetPreview.GetMiniThumbnail(asset);
                    GUIContent label = new(GetAssetLabel(asset), icon, asset.GetType().FullName);
                    if (GUILayout.Button(label, EditorStyles.label, GUILayout.Height(22f)))
                    {
                        SelectAsset(kind, asset);
                    }

                    if (GUILayout.Button("×", EditorStyles.miniButton, GUILayout.Width(22f)))
                    {
                        DeleteAsset(kind, asset);
                        EditorGUILayout.EndHorizontal();
                        return;
                    }
                }
                EditorGUILayout.EndHorizontal();
            }

            if (visibleCount == 0)
            {
                SWEditorUtils.DrawEmptyNotice(string.IsNullOrWhiteSpace(searchText)
                    ? "등록된 에셋이 없습니다."
                    : "검색 조건에 맞는 에셋이 없습니다.");
            }
        }

        /// <summary>
        /// 선택한 에셋의 관리 도구와 기본 인스펙터를 그립니다.
        /// </summary>
        private void DrawInspectorPanel(ManagedAssetKind kind)
        {
            ScriptableObject selectedAsset = selectedAssetsByKind[kind];
            EditorGUILayout.BeginVertical();
            {
                if (selectedAsset == null)
                {
                    SWEditorUtils.DrawEmptyNotice("왼쪽 목록에서 편집할 에셋을 선택하세요.");
                    EditorGUILayout.EndVertical();
                    return;
                }

                SWEditorUtils.DrawHeader($"{selectedAsset.name} ({selectedAsset.GetType().Name})");
                DrawRenameControls(kind, selectedAsset);
                DrawDatabaseControls(selectedAsset);

                inspectorScrollPosition = EditorGUILayout.BeginScrollView(inspectorScrollPosition);
                Editor.CreateCachedEditor(selectedAsset, null, ref cachedEditor);
                if (cachedEditor != null)
                {
                    cachedEditor.OnInspectorGUI();
                }
                EditorGUILayout.EndScrollView();
            }
            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 선택 에셋의 파일 이름을 변경하는 영역을 그립니다.
        /// </summary>
        private void DrawRenameControls(ManagedAssetKind kind, ScriptableObject selectedAsset)
        {
            EditorGUILayout.BeginHorizontal();
            string requestedName = EditorGUILayout.DelayedTextField("에셋 이름", selectedAsset.name);
            if (!string.Equals(requestedName, selectedAsset.name, StringComparison.Ordinal)
                && !string.IsNullOrWhiteSpace(requestedName))
            {
                string assetPath = AssetDatabase.GetAssetPath(selectedAsset);
                string errorMessage = AssetDatabase.RenameAsset(assetPath, requestedName.Trim());
                if (string.IsNullOrEmpty(errorMessage))
                {
                    RefreshAssets(kind, selectedAsset);
                }
                else
                {
                    ShowNotification(new GUIContent(errorMessage));
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// 데이터베이스 에셋에만 동기화와 검증 도구를 표시합니다.
        /// </summary>
        private void DrawDatabaseControls(ScriptableObject selectedAsset)
        {
            if (selectedAsset is not SWQuestDatabase
                && selectedAsset is not SWAchievementDatabase)
            {
                return;
            }

            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            {
                if (GUILayout.Button("프로젝트 정의 동기화"))
                {
                    SynchronizeDatabase(selectedAsset);
                }

                if (GUILayout.Button("구성 검증"))
                {
                    ValidateDatabase(selectedAsset);
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// 생성 경로, 접두사와 공통 동작 설정을 그립니다.
        /// </summary>
        private void DrawSettings()
        {
            settingsScrollPosition = EditorGUILayout.BeginScrollView(settingsScrollPosition);
            SWEditorUtils.DrawHeader("생성 경로와 이름 접두사");

            for (int index = 0; index < ManagedAssetKinds.Length; index++)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField(GetKindLabel(ManagedAssetKinds[index]), EditorStyles.boldLabel);
                createPaths[index] = EditorGUILayout.TextField("생성 경로", createPaths[index]);
                namePrefixes[index] = EditorGUILayout.TextField("이름 접두사", namePrefixes[index]);
                EditorGUILayout.EndVertical();
            }

            SWEditorUtils.DrawHeader("목록과 저장");
            listWidth = EditorGUILayout.Slider("목록 너비", listWidth, 260f, 520f);
            saveAssetsAutomatically = EditorGUILayout.Toggle("변경 후 자동 저장", saveAssetsAutomatically);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("설정 저장"))
            {
                SaveSettings();
                ShowNotification(new GUIContent("퀘스트 편집기 설정을 저장했습니다."));
            }

            if (GUILayout.Button("기본값 복원"))
            {
                ResetSettings();
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndScrollView();
        }
        #endregion // 화면

        #region 에셋 관리
        /// <summary>
        /// 선택한 타입의 새 에셋을 생성합니다.
        /// </summary>
        private void CreateAsset(ManagedAssetKind kind)
        {
            Type[] creationTypes = creationTypesByKind[kind];
            if (creationTypes.Length == 0)
            {
                return;
            }

            int selectedTypeIndex = Mathf.Clamp(creationTypeIndexesByKind[kind], 0,
                creationTypes.Length - 1);
            Type creationType = creationTypes[selectedTypeIndex];
            string creationPath = NormalizeAssetFolderPath(createPaths[(int)kind]);
            try
            {
                EnsureAssetFolder(creationPath);
            }
            catch (InvalidOperationException exception)
            {
                ShowNotification(new GUIContent(exception.Message));
                return;
            }

            ScriptableObject createdAsset = CreateInstance(creationType);
            string uniqueIdentifier = Guid.NewGuid().ToString("N").ToUpperInvariant();
            string codeName = namePrefixes[(int)kind] + uniqueIdentifier.Substring(0, 8);
            ApplyIdentity(createdAsset, codeName, CreateIntegerIdentifier(uniqueIdentifier));

            string fileName = namePrefixes[(int)kind] + creationType.Name;
            string assetPath = AssetDatabase.GenerateUniqueAssetPath(
                $"{creationPath}/{fileName}.asset");
            AssetDatabase.CreateAsset(createdAsset, assetPath);
            Undo.RegisterCreatedObjectUndo(createdAsset, $"{GetKindLabel(kind)} 생성");
            SynchronizeDatabase(createdAsset);
            SaveIfRequested();
            RefreshAssets(kind, createdAsset);
            SynchronizeRelatedDatabases(kind);
            SWEditorUtils.PingAndSelect(createdAsset);
        }

        /// <summary>
        /// 선택한 에셋을 새 식별값으로 복제합니다.
        /// </summary>
        private void DuplicateAsset(ManagedAssetKind kind, ScriptableObject sourceAsset)
        {
            if (sourceAsset == null)
            {
                return;
            }

            string sourcePath = AssetDatabase.GetAssetPath(sourceAsset);
            string directoryPath = Path.GetDirectoryName(sourcePath)?.Replace('\\', '/');
            string destinationPath = AssetDatabase.GenerateUniqueAssetPath(
                $"{directoryPath}/{sourceAsset.name}_Copy.asset");

            if (!AssetDatabase.CopyAsset(sourcePath, destinationPath))
            {
                ShowNotification(new GUIContent("에셋을 복제하지 못했습니다."));
                return;
            }

            ScriptableObject duplicatedAsset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(destinationPath);
            string uniqueIdentifier = Guid.NewGuid().ToString("N").ToUpperInvariant();
            ApplyIdentity(duplicatedAsset,
                namePrefixes[(int)kind] + uniqueIdentifier.Substring(0, 8),
                CreateIntegerIdentifier(uniqueIdentifier));
            SynchronizeDatabase(duplicatedAsset);
            SaveIfRequested();
            RefreshAssets(kind, duplicatedAsset);
            SynchronizeRelatedDatabases(kind);
            SWEditorUtils.PingAndSelect(duplicatedAsset);
        }

        /// <summary>
        /// 확인 후 선택한 에셋을 삭제합니다.
        /// </summary>
        private void DeleteAsset(ManagedAssetKind kind, ScriptableObject asset)
        {
            if (asset == null
                || !EditorUtility.DisplayDialog("에셋 삭제",
                    $"'{asset.name}' 에셋을 삭제하시겠습니까? 이 작업은 되돌릴 수 없습니다.",
                    "삭제", "취소"))
            {
                return;
            }

            string assetPath = AssetDatabase.GetAssetPath(asset);
            if (!AssetDatabase.DeleteAsset(assetPath))
            {
                ShowNotification(new GUIContent("에셋을 삭제하지 못했습니다."));
                return;
            }

            selectedAssetsByKind[kind] = null;
            DestroyImmediate(cachedEditor);
            cachedEditor = null;
            SaveIfRequested();
            RefreshAssets(kind);
            SynchronizeRelatedDatabases(kind);
        }

        /// <summary>
        /// 식별 에셋이면 코드명과 숫자 식별자를 지정합니다.
        /// </summary>
        private static void ApplyIdentity(ScriptableObject asset, string codeName,
            int integerIdentifier)
        {
            if (asset is not SWIdentifiedObject)
            {
                return;
            }

            SerializedObject serializedAsset = new(asset);
            SerializedProperty codeNameProperty = serializedAsset.FindProperty("codeName");
            SerializedProperty identifierProperty = serializedAsset.FindProperty("id");
            if (codeNameProperty != null)
            {
                codeNameProperty.stringValue = codeName;
            }

            if (identifierProperty != null)
            {
                identifierProperty.intValue = integerIdentifier;
            }

            serializedAsset.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
        }

        /// <summary>
        /// 문자열 식별자에서 양수 범위의 숫자 식별자를 생성합니다.
        /// </summary>
        private static int CreateIntegerIdentifier(string uniqueIdentifier)
        {
            byte[] bytes = new byte[4];
            for (int index = 0; index < bytes.Length; index++)
            {
                bytes[index] = Convert.ToByte(uniqueIdentifier.Substring(index * 2, 2), 16);
            }

            return BitConverter.ToInt32(bytes, 0) & int.MaxValue;
        }

        /// <summary>
        /// Assets 아래의 생성 폴더를 단계별로 만듭니다.
        /// </summary>
        private static void EnsureAssetFolder(string folderPath)
        {
            if (!string.Equals(folderPath, "Assets", StringComparison.Ordinal)
                && !folderPath.StartsWith("Assets/", StringComparison.Ordinal)
                || folderPath.Contains("..", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("생성 경로는 Assets 폴더 아래여야 합니다.");
            }

            string[] segments = folderPath.Split('/');
            string currentPath = segments[0];
            for (int index = 1; index < segments.Length; index++)
            {
                string nextPath = $"{currentPath}/{segments[index]}";
                if (!AssetDatabase.IsValidFolder(nextPath))
                {
                    AssetDatabase.CreateFolder(currentPath, segments[index]);
                }

                currentPath = nextPath;
            }
        }

        /// <summary>
        /// 사용자 설정의 생성 경로를 Unity 에셋 경로 형식으로 정규화합니다.
        /// </summary>
        private static string NormalizeAssetFolderPath(string folderPath)
        {
            return string.IsNullOrWhiteSpace(folderPath)
                ? "Assets"
                : folderPath.Trim().Replace('\\', '/').TrimEnd('/');
        }

        /// <summary>
        /// 설정에 따라 변경된 에셋을 저장합니다.
        /// </summary>
        private void SaveIfRequested()
        {
            if (saveAssetsAutomatically)
            {
                AssetDatabase.SaveAssets();
            }
        }
        #endregion // 에셋 관리

        #region 데이터베이스
        /// <summary>
        /// 선택한 데이터베이스를 프로젝트 정의와 동기화합니다.
        /// </summary>
        private static void SynchronizeDatabase(ScriptableObject databaseAsset)
        {
            switch (databaseAsset)
            {
                case SWQuestDatabase questDatabase:
                    questDatabase.SynchronizeProjectDefinitions();
                    break;
                case SWAchievementDatabase achievementDatabase:
                    achievementDatabase.SynchronizeProjectDefinitions();
                    break;
            }
        }

        /// <summary>
        /// 선택한 데이터베이스를 검증하고 결과를 콘솔에 출력합니다.
        /// </summary>
        private void ValidateDatabase(ScriptableObject databaseAsset)
        {
            IReadOnlyList<string> messages = databaseAsset switch
            {
                SWQuestDatabase questDatabase => questDatabase.ValidateDefinitions(),
                SWAchievementDatabase achievementDatabase => achievementDatabase.ValidateDefinitions(),
                _ => Array.Empty<string>()
            };

            if (messages.Count == 0)
            {
                Debug.Log($"[{databaseAsset.name}] 데이터베이스 검증을 통과했습니다.", databaseAsset);
                ShowNotification(new GUIContent("검증을 통과했습니다."));
                return;
            }

            for (int index = 0; index < messages.Count; index++)
            {
                Debug.LogWarning($"[{databaseAsset.name}] {messages[index]}", databaseAsset);
            }

            ShowNotification(new GUIContent($"문제 {messages.Count}개를 콘솔에 출력했습니다."));
        }

        /// <summary>
        /// 퀘스트나 업적 목록이 바뀌었으면 관련 데이터베이스를 다시 수집합니다.
        /// </summary>
        private static void SynchronizeRelatedDatabases(ManagedAssetKind kind)
        {
            if (kind == ManagedAssetKind.Quest)
            {
                SynchronizeAllDatabases<SWQuestDatabase>();
            }
            else if (kind == ManagedAssetKind.Achievement)
            {
                SynchronizeAllDatabases<SWAchievementDatabase>();
            }
        }

        /// <summary>
        /// 프로젝트의 지정 데이터베이스 타입을 모두 동기화합니다.
        /// </summary>
        private static void SynchronizeAllDatabases<TDatabase>() where TDatabase : ScriptableObject
        {
            string[] assetIdentifiers = AssetDatabase.FindAssets($"t:{typeof(TDatabase).Name}");
            for (int index = 0; index < assetIdentifiers.Length; index++)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(assetIdentifiers[index]);
                ScriptableObject databaseAsset = AssetDatabase.LoadAssetAtPath<TDatabase>(assetPath);
                SynchronizeDatabase(databaseAsset);
            }
        }
        #endregion // 데이터베이스

        #region 검색과 분류
        /// <summary>
        /// 관리 분류에 맞는 생성 가능한 타입을 찾습니다.
        /// </summary>
        private static Type[] FindCreationTypes(ManagedAssetKind kind)
        {
            Type baseType = GetBaseType(kind);
            if (kind == ManagedAssetKind.QuestDatabase
                || kind == ManagedAssetKind.AchievementDatabase)
            {
                return new[] { baseType };
            }

            List<Type> types = new();
            if (!baseType.IsAbstract && MatchesKind(baseType, kind))
            {
                types.Add(baseType);
            }

            foreach (Type type in TypeCache.GetTypesDerivedFrom(baseType))
            {
                if (!type.IsAbstract && !type.IsGenericType && MatchesKind(type, kind))
                {
                    types.Add(type);
                }
            }

            types.Sort((left, right) => string.Compare(
                left.FullName, right.FullName, StringComparison.Ordinal));
            return types.ToArray();
        }

        /// <summary>
        /// 지정한 분류의 프로젝트 에셋 목록을 다시 읽습니다.
        /// </summary>
        private void RefreshAssets(ManagedAssetKind kind, ScriptableObject assetToSelect = null)
        {
            List<ScriptableObject> assets = assetsByKind[kind];
            assets.Clear();
            HashSet<string> collectedPaths = new(StringComparer.Ordinal);
            Type[] creationTypes = creationTypesByKind[kind];

            for (int typeIndex = 0; typeIndex < creationTypes.Length; typeIndex++)
            {
                string[] assetIdentifiers = AssetDatabase.FindAssets(
                    $"t:{creationTypes[typeIndex].Name}");
                for (int assetIndex = 0; assetIndex < assetIdentifiers.Length; assetIndex++)
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(assetIdentifiers[assetIndex]);
                    if (!collectedPaths.Add(assetPath))
                    {
                        continue;
                    }

                    ScriptableObject asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);
                    if (asset != null && MatchesKind(asset.GetType(), kind))
                    {
                        assets.Add(asset);
                    }
                }
            }

            if (assetToSelect != null && assets.Contains(assetToSelect))
            {
                SelectAsset(kind, assetToSelect);
            }
            else if (selectedAssetsByKind[kind] != null
                && !assets.Contains(selectedAssetsByKind[kind]))
            {
                selectedAssetsByKind[kind] = null;
            }

            Repaint();
        }

        /// <summary>
        /// 모든 관리 분류의 에셋 목록을 다시 읽습니다.
        /// </summary>
        private void RefreshAllAssets()
        {
            for (int index = 0; index < ManagedAssetKinds.Length; index++)
            {
                RefreshAssets(ManagedAssetKinds[index]);
            }
        }

        /// <summary>
        /// 목록과 인스펙터에서 표시할 에셋을 선택합니다.
        /// </summary>
        private void SelectAsset(ManagedAssetKind kind, ScriptableObject asset)
        {
            if (selectedAssetsByKind[kind] != asset)
            {
                DestroyImmediate(cachedEditor);
                cachedEditor = null;
                inspectorScrollPosition = Vector2.zero;
            }

            selectedAssetsByKind[kind] = asset;
        }

        /// <summary>
        /// 타입이 관리 분류에 포함되는지 확인합니다.
        /// </summary>
        private static bool MatchesKind(Type type, ManagedAssetKind kind)
        {
            return kind switch
            {
                ManagedAssetKind.Quest => typeof(SWQuest).IsAssignableFrom(type)
                    && !typeof(SWAchievement).IsAssignableFrom(type),
                ManagedAssetKind.Achievement => typeof(SWAchievement).IsAssignableFrom(type),
                ManagedAssetKind.Task => typeof(SWQuestTask).IsAssignableFrom(type),
                ManagedAssetKind.Target => typeof(SWQuestTarget).IsAssignableFrom(type),
                ManagedAssetKind.Condition => typeof(SWQuestCondition).IsAssignableFrom(type),
                ManagedAssetKind.Reward => typeof(SWQuestReward).IsAssignableFrom(type),
                ManagedAssetKind.TaskAction => typeof(SWQuestTaskAction).IsAssignableFrom(type),
                ManagedAssetKind.InitialProgressValue => typeof(SWQuestInitialProgressValue).IsAssignableFrom(type),
                ManagedAssetKind.QuestDatabase => type == typeof(SWQuestDatabase),
                ManagedAssetKind.AchievementDatabase => type == typeof(SWAchievementDatabase),
                _ => false
            };
        }

        /// <summary>
        /// 관리 분류의 기반 타입을 반환합니다.
        /// </summary>
        private static Type GetBaseType(ManagedAssetKind kind)
        {
            return kind switch
            {
                ManagedAssetKind.Quest => typeof(SWQuest),
                ManagedAssetKind.Achievement => typeof(SWAchievement),
                ManagedAssetKind.Task => typeof(SWQuestTask),
                ManagedAssetKind.Target => typeof(SWQuestTarget),
                ManagedAssetKind.Condition => typeof(SWQuestCondition),
                ManagedAssetKind.Reward => typeof(SWQuestReward),
                ManagedAssetKind.TaskAction => typeof(SWQuestTaskAction),
                ManagedAssetKind.InitialProgressValue => typeof(SWQuestInitialProgressValue),
                ManagedAssetKind.QuestDatabase => typeof(SWQuestDatabase),
                ManagedAssetKind.AchievementDatabase => typeof(SWAchievementDatabase),
                _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
            };
        }

        /// <summary>
        /// 생성 타입 선택 목록의 표시 이름을 만듭니다.
        /// </summary>
        private static string[] GetCreationTypeNames(IReadOnlyList<Type> creationTypes)
        {
            string[] names = new string[creationTypes.Count];
            for (int index = 0; index < creationTypes.Count; index++)
            {
                names[index] = creationTypes[index].Name;
            }

            return names;
        }

        /// <summary>
        /// 검색어가 에셋 이름, 타입 이름, 코드명 또는 표시명에 포함되는지 확인합니다.
        /// </summary>
        private static bool MatchesSearch(ScriptableObject asset, string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                return true;
            }

            if (SWEditorUtils.MatchesFilter(asset.name, searchText)
                || SWEditorUtils.MatchesFilter(asset.GetType().Name, searchText))
            {
                return true;
            }

            return asset is SWIdentifiedObject identifiedAsset
                && (SWEditorUtils.MatchesFilter(identifiedAsset.CodeName, searchText)
                    || SWEditorUtils.MatchesFilter(identifiedAsset.DisplayName, searchText));
        }

        /// <summary>
        /// 현재 정렬 방식에 따라 두 에셋을 비교합니다.
        /// </summary>
        private int CompareAssets(ScriptableObject left, ScriptableObject right)
        {
            string leftValue = GetSortValue(left);
            string rightValue = GetSortValue(right);
            int comparison = string.Compare(leftValue, rightValue,
                StringComparison.OrdinalIgnoreCase);
            return comparison != 0
                ? comparison
                : string.Compare(left.name, right.name, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 현재 정렬 방식에 맞는 비교 문자열을 반환합니다.
        /// </summary>
        private string GetSortValue(ScriptableObject asset)
        {
            return sortMode switch
            {
                AssetSortMode.CodeName when asset is SWIdentifiedObject identifiedAsset
                    => identifiedAsset.CodeName,
                AssetSortMode.TypeName => asset.GetType().Name,
                _ => asset.name
            } ?? string.Empty;
        }

        /// <summary>
        /// 목록에 표시할 에셋 라벨을 반환합니다.
        /// </summary>
        private static string GetAssetLabel(ScriptableObject asset)
        {
            if (asset is SWIdentifiedObject identifiedAsset
                && !string.IsNullOrWhiteSpace(identifiedAsset.CodeName))
            {
                return $"{identifiedAsset.CodeName}  ·  {asset.name}";
            }

            return $"{asset.name}  ·  {asset.GetType().Name}";
        }

        /// <summary>
        /// 관리 분류의 한글 표시 이름을 반환합니다.
        /// </summary>
        private static string GetKindLabel(ManagedAssetKind kind)
            => ToolbarNames[(int)kind];
        #endregion // 검색과 분류

        #region 설정
        /// <summary>
        /// 프로젝트별 편집기 설정을 불러옵니다.
        /// </summary>
        private void LoadSettings()
        {
            createPaths = new string[ManagedAssetKinds.Length];
            namePrefixes = new string[ManagedAssetKinds.Length];
            for (int index = 0; index < ManagedAssetKinds.Length; index++)
            {
                string kindName = ManagedAssetKinds[index].ToString();
                createPaths[index] = SWEditorUtils.LoadPref(
                    $"{PreferenceKeyPrefix}Path.{kindName}", DefaultCreatePaths[index]);
                namePrefixes[index] = SWEditorUtils.LoadPref(
                    $"{PreferenceKeyPrefix}Prefix.{kindName}", DefaultNamePrefixes[index]);
            }

            sortMode = (AssetSortMode)SWEditorUtils.LoadPref(
                $"{PreferenceKeyPrefix}SortMode", 0);
            saveAssetsAutomatically = SWEditorUtils.LoadPref(
                $"{PreferenceKeyPrefix}SaveAssetsAutomatically", true);
            listWidth = SWEditorUtils.LoadPref(
                $"{PreferenceKeyPrefix}ListWidth", DefaultListWidth);
        }

        /// <summary>
        /// 프로젝트별 편집기 설정을 저장합니다.
        /// </summary>
        private void SaveSettings()
        {
            if (createPaths == null || namePrefixes == null)
            {
                return;
            }

            for (int index = 0; index < ManagedAssetKinds.Length; index++)
            {
                string kindName = ManagedAssetKinds[index].ToString();
                SWEditorUtils.SavePref($"{PreferenceKeyPrefix}Path.{kindName}", createPaths[index]);
                SWEditorUtils.SavePref($"{PreferenceKeyPrefix}Prefix.{kindName}", namePrefixes[index]);
            }

            SWEditorUtils.SavePref($"{PreferenceKeyPrefix}SortMode", (int)sortMode);
            SWEditorUtils.SavePref($"{PreferenceKeyPrefix}SaveAssetsAutomatically",
                saveAssetsAutomatically);
            SWEditorUtils.SavePref($"{PreferenceKeyPrefix}ListWidth", listWidth);
        }

        /// <summary>
        /// 편집기 설정을 기본값으로 되돌립니다.
        /// </summary>
        private void ResetSettings()
        {
            for (int index = 0; index < ManagedAssetKinds.Length; index++)
            {
                createPaths[index] = DefaultCreatePaths[index];
                namePrefixes[index] = DefaultNamePrefixes[index];
            }

            sortMode = AssetSortMode.CodeName;
            saveAssetsAutomatically = true;
            listWidth = DefaultListWidth;
            SaveSettings();
        }
        #endregion // 설정
    }
}
