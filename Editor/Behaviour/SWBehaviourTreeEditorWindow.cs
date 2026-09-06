using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using SW.BehaviourTree;
using SW.EditorTools.StateMachine;

namespace SW.EditorTools.Behaviour
{
    /// <summary>Behaviour Tree 에셋을 시각적으로 생성하고 편집하는 Unity 6 전용 창입니다.</summary>
    public sealed class SWBehaviourTreeEditorWindow : EditorWindow
    {
        private SWBehaviourTreeAsset treeAsset;
        private SWBehaviourGraphView graphView;
        private ScrollView blackboardPanel;
        private ScrollView inspectorPanel;
        private Label titleLabel;
        private SWBehaviourNodeView selectedNodeView;
        private SWBehaviourTreeEditorSettings editorSettings;
        private VisualElement welcomeOverlay;
        private SWGraphAssetListPanel graphAssetListPanel;
        private VisualElement validationPanel;
        private Button validationHeaderButton;
        private ScrollView validationMessageList;
        private bool validationExpanded;
        private bool runtimeInspectorVisible;
        private SWBehaviourTreeRunner runtimeRunner;

        [MenuItem("SWTools/Utils/Behaviour/Tree Editor")]
        public static SWBehaviourTreeEditorWindow OpenWindow()
        {
            SWBehaviourTreeEditorWindow window = GetWindow<SWBehaviourTreeEditorWindow>();
            window.titleContent = new GUIContent("SW Behaviour Tree");
            window.minSize = new Vector2(1000f, 600f);
            return window;
        }

        /// <summary>지정한 Behaviour Tree 에셋을 편집기에서 엽니다.</summary>
        public static void OpenTree(SWBehaviourTreeAsset asset)
        {
            SWBehaviourTreeEditorWindow window = OpenWindow();
            window.SetTree(asset);
            window.Focus();
        }

        public void CreateGUI()
        {
            editorSettings = SWBehaviourTreeEditorSettings.instance;
            rootVisualElement.Clear();
            StyleSheet styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(
                AssetDatabase.GUIDToAssetPath("c1902963a6ec47b49e068b3713d1464d"));
            if (styleSheet != null)
                rootVisualElement.styleSheets.Add(styleSheet);
            rootVisualElement.AddToClassList("sw-behaviour-window");
            rootVisualElement.style.flexDirection = FlexDirection.Column;
            rootVisualElement.style.backgroundColor = new Color(0.075f, 0.08f, 0.09f);
            CreateToolbar();
            CreateGraphArea();
            SetTree(treeAsset);
            rootVisualElement.schedule.Execute(RefreshRuntimeStatuses)
                .Every(editorSettings.RuntimeRefreshMilliseconds);
        }

        private void CreateToolbar()
        {
            Toolbar toolbar = new();
            toolbar.AddToClassList("sw-behaviour-toolbar");
            toolbar.style.height = 34f;

            titleLabel = new Label("Behaviour Tree");
            SWGraphEditorVisualUtility.ApplyToolbarTitle(titleLabel);
            toolbar.Add(titleLabel);

            toolbar.Add(SWGraphEditorVisualUtility.CreateToolbarButton(
                "Create Node", () => graphView?.OpenSearchAtCenter()));
            toolbar.Add(SWGraphEditorVisualUtility.CreateToolbarButton(
                "Frame All", () => graphView?.FrameAllNodes()));
            toolbar.Add(SWGraphEditorVisualUtility.CreateToolbarButton(
                "Auto Layout", () => graphView?.AutoLayout()));
            toolbar.Add(SWGraphEditorVisualUtility.CreateToolbarButton(
                "New Script", ShowNodeScriptMenu));
            toolbar.Add(SWGraphEditorVisualUtility.CreateToolbarButton(
                "Validate", ValidateTree));
            toolbar.Add(SWGraphEditorVisualUtility.CreateToolbarButton(
                "Save Asset", SaveTree));
            toolbar.Add(SWGraphEditorVisualUtility.CreateToolbarButton(
                "Settings",
                () => SettingsService.OpenProjectSettings("Project/SWUtils/Behaviour Tree")));
            rootVisualElement.Add(toolbar);
        }

        private void CreateGraphArea()
        {
            TwoPaneSplitView splitView = new TwoPaneSplitView(
                0, 260f, TwoPaneSplitViewOrientation.Horizontal);
            splitView.style.flexGrow = 1f;
            graphAssetListPanel = new SWGraphAssetListPanel(
                "Graph List",
                "New Behaviour Tree",
                typeof(SWBehaviourTreeAsset),
                CreateTreeAsset,
                asset => SetTree(asset as SWBehaviourTreeAsset));
            splitView.Add(graphAssetListPanel);

            VisualElement host = new();
            host.style.flexGrow = 1f;
            host.style.position = Position.Relative;
            graphView = new SWBehaviourGraphView(ShowNodeInspector, OnGraphChanged);
            host.Add(graphView);

            welcomeOverlay = CreateWelcomeOverlay();
            host.Add(welcomeOverlay);

            blackboardPanel = CreateFloatingPanel(
                12f, null, editorSettings.BlackboardWidth, editorSettings.PanelHeight);
            blackboardPanel.style.top = 42f;
            host.Add(blackboardPanel);
            AddResizeHandle(blackboardPanel, false, size =>
            {
                editorSettings.BlackboardWidth = size.x;
                editorSettings.PanelHeight = size.y;
                editorSettings.SaveSettings();
            });
            inspectorPanel = CreateFloatingPanel(
                null, 12f, editorSettings.InspectorWidth, editorSettings.PanelHeight);
            host.Add(inspectorPanel);
            AddResizeHandle(inspectorPanel, true, size =>
            {
                editorSettings.InspectorWidth = size.x;
                editorSettings.PanelHeight = size.y;
                editorSettings.SaveSettings();
            });
            host.Add(graphAssetListPanel.CreateCollapseButton(splitView));
            splitView.Add(host);
            rootVisualElement.Add(splitView);
            CreateValidationPanel();
        }

        /// <summary>Behaviour Tree 검증 결과를 Console 형태로 표시하는 하단 패널을 생성합니다.</summary>
        private void CreateValidationPanel()
        {
            validationPanel = new VisualElement();
            validationPanel.style.flexShrink = 0f;
            validationPanel.style.backgroundColor = new Color(0.095f, 0.1f, 0.11f);
            validationPanel.style.borderTopWidth = 1f;
            validationPanel.style.borderTopColor = new Color(0.25f, 0.27f, 0.29f);

            validationHeaderButton = new Button(() =>
            {
                SetValidationExpanded(!validationExpanded);
                ValidateTree();
            });
            validationHeaderButton.style.height = 26f;
            validationHeaderButton.style.unityTextAlign = TextAnchor.MiddleLeft;
            validationHeaderButton.style.marginLeft = 0f;
            validationHeaderButton.style.marginRight = 0f;
            validationHeaderButton.style.marginTop = 0f;
            validationHeaderButton.style.marginBottom = 0f;
            validationPanel.Add(validationHeaderButton);

            validationMessageList = new ScrollView();
            validationMessageList.style.height = 150f;
            validationMessageList.style.paddingLeft = 8f;
            validationMessageList.style.paddingRight = 8f;
            validationMessageList.style.paddingTop = 5f;
            validationMessageList.style.paddingBottom = 5f;
            validationPanel.Add(validationMessageList);
            rootVisualElement.Add(validationPanel);
            SetValidationExpanded(false);
        }

        /// <summary>하단 검증 결과 목록의 펼침 상태를 변경합니다.</summary>
        private void SetValidationExpanded(bool isExpanded)
        {
            validationExpanded = isExpanded;
            if (validationMessageList != null)
            {
                validationMessageList.style.display = isExpanded
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
            }
        }

        private static ScrollView CreateFloatingPanel(
            float? left, float? right, float width, float height)
        {
            ScrollView panel = new();
            panel.AddToClassList("sw-behaviour-panel");
            panel.style.position = Position.Absolute;
            panel.style.top = 12f;
            panel.style.width = width;
            panel.style.height = height;
            panel.style.paddingLeft = 10f;
            panel.style.paddingRight = 10f;
            panel.style.paddingTop = 8f;
            panel.style.paddingBottom = 10f;
            panel.style.backgroundColor = new Color(0.115f, 0.12f, 0.13f, 0.97f);
            panel.style.borderTopLeftRadius = 4f;
            panel.style.borderTopRightRadius = 4f;
            panel.style.borderBottomLeftRadius = 4f;
            panel.style.borderBottomRightRadius = 4f;
            panel.style.borderTopWidth = 1f;
            panel.style.borderBottomWidth = 1f;
            panel.style.borderLeftWidth = 1f;
            panel.style.borderRightWidth = 1f;
            Color borderColor = new(0.28f, 0.30f, 0.32f);
            panel.style.borderTopColor = borderColor;
            panel.style.borderBottomColor = borderColor;
            panel.style.borderLeftColor = borderColor;
            panel.style.borderRightColor = borderColor;
            if (left.HasValue) panel.style.left = left.Value;
            if (right.HasValue) panel.style.right = right.Value;
            return panel;
        }

        /// <summary>떠 있는 패널 아래 모서리에 크기 조절 손잡이를 추가합니다.</summary>
        private static void AddResizeHandle(
            VisualElement panel,
            bool resizeFromLeft,
            Action<Vector2> resizeCompleted)
        {
            Label handle = new(resizeFromLeft ? "◣" : "◢");
            handle.tooltip = "끌어서 패널 크기 조절";
            handle.style.position = Position.Absolute;
            handle.style.bottom = 1f;
            handle.style.width = 18f;
            handle.style.height = 18f;
            handle.style.unityTextAlign = TextAnchor.MiddleCenter;
            handle.style.color = new Color(0.58f, 0.60f, 0.63f);
            if (resizeFromLeft) handle.style.left = 1f;
            else handle.style.right = 1f;
            panel.hierarchy.Add(handle);
            handle.AddManipulator(new SWPanelResizeManipulator(
                panel, resizeFromLeft, resizeCompleted));
        }

        private void SetTree(SWBehaviourTreeAsset asset)
        {
            treeAsset = asset;
            selectedNodeView = null;
            runtimeRunner = null;
            graphAssetListPanel?.SelectAsset(asset);
            if (titleLabel != null)
                titleLabel.text = asset == null ? "Behaviour Tree" : asset.name;
            graphView?.SetTree(asset);
            if (welcomeOverlay != null)
                welcomeOverlay.style.display = asset == null
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
            RebuildBlackboard();
            ShowEmptyInspector();
            ValidateTree();
        }

        private void CreateTreeAsset()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Behaviour Tree 생성", "SWBehaviourTree", "asset", "저장 위치를 선택하세요.");
            if (string.IsNullOrWhiteSpace(path))
                return;
            SWBehaviourTreeAsset asset = CreateInstance<SWBehaviourTreeAsset>();
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            SetTree(asset);
            graphAssetListPanel?.Refresh();
            Selection.activeObject = asset;
        }

        /// <summary>프로젝트에 존재하는 Behaviour Tree 에셋을 이름순으로 표시합니다.</summary>
        private void ShowTreeAssetMenu()
        {
            string[] assetIdentifiers = AssetDatabase.FindAssets("t:SWBehaviourTreeAsset");
            Array.Sort(assetIdentifiers, (left, right) => string.Compare(
                AssetDatabase.GUIDToAssetPath(left),
                AssetDatabase.GUIDToAssetPath(right),
                StringComparison.OrdinalIgnoreCase));
            GenericMenu menu = new();
            menu.AddItem(new GUIContent("Create New Tree..."), false, CreateTreeAsset);
            menu.AddSeparator(string.Empty);
            if (assetIdentifiers.Length == 0)
                menu.AddDisabledItem(new GUIContent("Behaviour Tree 에셋 없음"));
            for (int index = 0; index < assetIdentifiers.Length; index++)
            {
                string path = AssetDatabase.GUIDToAssetPath(assetIdentifiers[index]);
                SWBehaviourTreeAsset asset = AssetDatabase.LoadAssetAtPath<SWBehaviourTreeAsset>(path);
                if (asset == null)
                    continue;
                string menuPath = $"{asset.name}  ({Path.GetDirectoryName(path)})";
                menu.AddItem(new GUIContent(menuPath), asset == treeAsset, () => SetTree(asset));
            }
            menu.ShowAsContext();
        }

        /// <summary>Action, Composite, Decorator 노드 스크립트 생성 메뉴를 표시합니다.</summary>
        private void ShowNodeScriptMenu()
        {
            GenericMenu menu = new();
            menu.AddItem(new GUIContent("Action Node"), false,
                () => CreateNodeScript(SWBehaviourNodeScriptKind.Action));
            menu.AddItem(new GUIContent("Composite Node"), false,
                () => CreateNodeScript(SWBehaviourNodeScriptKind.Composite));
            menu.AddItem(new GUIContent("Decorator Node"), false,
                () => CreateNodeScript(SWBehaviourNodeScriptKind.Decorator));
            menu.ShowAsContext();
        }

        /// <summary>선택한 종류의 사용자 Behaviour 노드 스크립트를 생성합니다.</summary>
        private static void CreateNodeScript(SWBehaviourNodeScriptKind scriptKind)
        {
            string defaultName = scriptKind switch
            {
                SWBehaviourNodeScriptKind.Composite => "NewCompositeNode",
                SWBehaviourNodeScriptKind.Decorator => "NewDecoratorNode",
                _ => "NewActionNode",
            };
            string path = EditorUtility.SaveFilePanelInProject(
                "Behaviour Node Script 생성", defaultName, "cs", "저장 위치를 선택하세요.");
            if (string.IsNullOrWhiteSpace(path))
                return;
            string className = Path.GetFileNameWithoutExtension(path);
            if (!IsValidClassName(className))
            {
                EditorUtility.DisplayDialog("스크립트 생성 실패",
                    "파일 이름은 유효한 C# 클래스 이름이어야 합니다.", "확인");
                return;
            }
            string templatePath = AssetDatabase.GUIDToAssetPath(GetTemplateGuid(scriptKind));
            string scriptText = !string.IsNullOrWhiteSpace(templatePath) && File.Exists(templatePath)
                ? File.ReadAllText(templatePath).Replace("#SCRIPTNAME#", className)
                : CreateNodeScriptText(className, scriptKind);
            File.WriteAllText(Path.GetFullPath(path), scriptText);
            AssetDatabase.ImportAsset(path);
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
        }

        private static bool IsValidClassName(string className)
        {
            if (string.IsNullOrWhiteSpace(className) ||
                !(char.IsLetter(className[0]) || className[0] == '_'))
                return false;
            for (int index = 1; index < className.Length; index++)
            {
                if (!(char.IsLetterOrDigit(className[index]) || className[index] == '_'))
                    return false;
            }
            return true;
        }

        private static string GetTemplateGuid(SWBehaviourNodeScriptKind scriptKind)
        {
            return scriptKind switch
            {
                SWBehaviourNodeScriptKind.Composite => "d0603fd1e29d4fbcb03d9825dddcb9e8",
                SWBehaviourNodeScriptKind.Decorator => "8eed401158fe4fc0a8c46f277667973d",
                _ => "2c8cd002406b4c4285cc2f9961825ea9",
            };
        }

        private VisualElement CreateWelcomeOverlay()
        {
            VisualElement overlay = new();
            overlay.AddToClassList("sw-behaviour-welcome-overlay");
            VisualElement card = new();
            card.AddToClassList("sw-behaviour-welcome-card");
            Label title = new Label("Behaviour Tree")
            {
                tooltip = "새 Tree를 만들거나 프로젝트의 Tree Asset을 선택하세요.",
            };
            title.AddToClassList("sw-graph-welcome-title");
            card.Add(title);
            Label description = new("Create a new tree or open an existing asset.");
            description.AddToClassList("sw-behaviour-welcome-description");
            card.Add(description);
            Button createButton = new(CreateTreeAsset) { text = "Create New Tree" };
            Button openButton = new(ShowTreeAssetMenu) { text = "Open Existing Tree" };
            card.Add(createButton);
            card.Add(openButton);
            overlay.Add(card);
            return overlay;
        }

        private static string CreateNodeScriptText(
            string className,
            SWBehaviourNodeScriptKind scriptKind)
        {
            string baseType = scriptKind switch
            {
                SWBehaviourNodeScriptKind.Composite => "SWBehaviourCompositeNode",
                SWBehaviourNodeScriptKind.Decorator => "SWBehaviourDecoratorNode",
                _ => "SWBehaviourActionNode",
            };
            string updateBody = scriptKind switch
            {
                SWBehaviourNodeScriptKind.Composite =>
                    "            if (ChildIdentifiers.Count == 0 ||\n" +
                    "                !tree.TryGetNode(ChildIdentifiers[0], out SWBehaviourNode child))\n" +
                    "                return SWBehaviourStatus.Failure;\n" +
                    "            return child.Tick(context, tree);",
                SWBehaviourNodeScriptKind.Decorator =>
                    "            return TickChild(context, tree);",
                _ => "            return SWBehaviourStatus.Success;",
            };
            return
                "using SW.BehaviourTree;\n\n" +
                "/// <summary>사용자 정의 Behaviour Tree 노드입니다.</summary>\n" +
                $"[SWBehaviourNodeCategory(\"Custom/{scriptKind}\")]\n" +
                $"public sealed class {className} : {baseType}\n" +
                "{\n" +
                "    /// <summary>노드가 갱신될 때 실행됩니다.</summary>\n" +
                "    protected override SWBehaviourStatus OnUpdate(\n" +
                "        SWBehaviourContext context,\n" +
                "        SWBehaviourTreeAsset tree)\n" +
                "    {\n" + updateBody + "\n" +
                "    }\n" +
                "}\n";
        }

        private void RebuildBlackboard()
        {
            if (blackboardPanel == null)
                return;
            blackboardPanel.Clear();
            blackboardPanel.Add(CreatePanelTitle("Blackboard"));
            if (treeAsset == null)
            {
                blackboardPanel.Add(new HelpBox("Behaviour Tree 에셋을 선택하세요.", HelpBoxMessageType.Info));
                return;
            }

            if (treeAsset.Blackboard.RemoveInvalidEntries())
            {
                EditorUtility.SetDirty(treeAsset);
                treeAsset.Blackboard.RebuildLookup();
            }

            VisualElement controls = new();
            controls.style.flexDirection = FlexDirection.Row;
            EnumField typeField = new(SWBehaviourBlackboardValueType.Boolean);
            typeField.style.flexGrow = 1f;
            controls.Add(typeField);
            controls.Add(new Button(() =>
            {
                Undo.RecordObject(treeAsset, "Blackboard 항목 추가");
                treeAsset.Blackboard.Add("New Key", (SWBehaviourBlackboardValueType)typeField.value);
                EditorUtility.SetDirty(treeAsset);
                RebuildBlackboard();
            }) { text = "+" });
            controls.Add(new Button(ShowCustomBlackboardKeyMenu) { text = "Custom" });
            blackboardPanel.Add(controls);

            SerializedObject serializedTree = new(treeAsset);
            SerializedProperty entries = serializedTree.FindProperty("blackboard").FindPropertyRelative("entries");
            int entryCount = Mathf.Min(entries.arraySize, treeAsset.Blackboard.Entries.Count);
            for (int index = 0; index < entryCount; index++)
            {
                SWBehaviourBlackboardEntry entry = treeAsset.Blackboard.Entries[index];
                if (entry == null)
                    continue;
                string capturedIdentifier = entry.Identifier;
                SerializedProperty entryProperty = entries.GetArrayElementAtIndex(index);
                VisualElement row = new();
                row.style.flexDirection = FlexDirection.Column;
                row.style.marginTop = 4f;
                row.style.paddingLeft = 6f;
                row.style.paddingRight = 6f;
                row.style.paddingTop = 5f;
                row.style.paddingBottom = 5f;
                row.style.backgroundColor = new Color(0.15f, 0.16f, 0.17f);
                VisualElement header = new();
                header.style.flexDirection = FlexDirection.Row;
                PropertyField nameField = new(entryProperty.FindPropertyRelative("name"), string.Empty);
                nameField.style.flexGrow = 1f;
                header.Add(nameField);
                Label typeLabel = new(entry.ValueType.ToString());
                typeLabel.style.minWidth = 58f;
                typeLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                header.Add(typeLabel);
                Button removeButton = new(() =>
                {
                    Undo.RecordObject(treeAsset, "Blackboard 항목 삭제");
                    treeAsset.Blackboard.Remove(capturedIdentifier);
                    EditorUtility.SetDirty(treeAsset);
                    RebuildBlackboard();
                }) { text = "−" };
                removeButton.style.width = 26f;
                header.Add(removeButton);
                row.Add(header);
                SerializedProperty valueProperty = entry.ValueType == SWBehaviourBlackboardValueType.Custom
                    ? entryProperty.FindPropertyRelative("value")
                    : entryProperty.FindPropertyRelative(
                        GetBlackboardValuePropertyName(entry.ValueType));
                if (valueProperty != null)
                    row.Add(new PropertyField(valueProperty, "Value"));
                blackboardPanel.Add(row);
            }
            blackboardPanel.Bind(serializedTree);
        }

        /// <summary>프로젝트에 선언된 사용자 Blackboard Key 타입 생성 메뉴를 표시합니다.</summary>
        private void ShowCustomBlackboardKeyMenu()
        {
            GenericMenu menu = new();
            TypeCache.TypeCollection keyTypes =
                TypeCache.GetTypesDerivedFrom<SWBehaviourBlackboardEntry>();
            List<Type> customTypes = new();
            foreach (Type keyType in keyTypes)
            {
                if (!keyType.IsAbstract && !keyType.IsGenericType &&
                    keyType != typeof(SWBehaviourBlackboardEntry) &&
                    keyType.GetConstructor(Type.EmptyTypes) != null)
                    customTypes.Add(keyType);
            }
            customTypes.Sort((left, right) =>
                string.Compare(left.FullName, right.FullName, StringComparison.Ordinal));
            if (customTypes.Count == 0)
                menu.AddDisabledItem(new GUIContent("등록된 사용자 Key 없음"));
            for (int index = 0; index < customTypes.Count; index++)
            {
                Type capturedType = customTypes[index];
                menu.AddItem(new GUIContent(capturedType.Name), false, () =>
                {
                    Undo.RecordObject(treeAsset, "사용자 Blackboard Key 추가");
                    treeAsset.Blackboard.Add("New Key", capturedType);
                    EditorUtility.SetDirty(treeAsset);
                    RebuildBlackboard();
                });
            }
            menu.ShowAsContext();
        }

        private void ShowNodeInspector(SWBehaviourNodeView nodeView)
        {
            selectedNodeView = nodeView;
            runtimeInspectorVisible = false;
            inspectorPanel.Clear();
            AddInspectorTabs(false);
            inspectorPanel.Add(CreatePanelTitle("Node Inspector"));

            TextField nameField = new("Display Name") { value = nodeView.Data.DisplayName };
            nameField.RegisterValueChangedCallback(changeEvent =>
            {
                RecordNodeChange("Behaviour 노드 이름 변경");
                nodeView.Data.DisplayName = changeEvent.newValue;
                nodeView.RefreshVisuals();
            });
            inspectorPanel.Add(nameField);

            TextField descriptionField = new("Description")
            {
                value = nodeView.Data.Description,
                multiline = true,
            };
            descriptionField.RegisterValueChangedCallback(changeEvent =>
            {
                RecordNodeChange("Behaviour 노드 설명 변경");
                nodeView.Data.Description = changeEvent.newValue;
                nodeView.RefreshVisuals();
            });
            inspectorPanel.Add(descriptionField);

            Button rootButton = new(() =>
            {
                RecordNodeChange("Root 노드 변경");
                treeAsset.SetRoot(nodeView.Data.Identifier);
                graphView.Reload();
                RebuildBlackboard();
                ShowEmptyInspector();
            }) { text = treeAsset.RootNodeIdentifier == nodeView.Data.Identifier ? "Root Node" : "Set as Root" };
            rootButton.SetEnabled(treeAsset.RootNodeIdentifier != nodeView.Data.Identifier);
            inspectorPanel.Add(rootButton);

            SerializedObject serializedTree = new(treeAsset);
            SerializedProperty nodes = serializedTree.FindProperty("nodes");
            for (int index = 0; index < nodes.arraySize; index++)
            {
                SerializedProperty nodeProperty = nodes.GetArrayElementAtIndex(index);
                if (ReferenceEquals(nodeProperty.managedReferenceValue, nodeView.Data))
                {
                    AddNodeSpecificProperties(nodeProperty);
                    inspectorPanel.Bind(serializedTree);
                    break;
                }
            }
        }

        /// <summary>공통 그래프 필드를 제외한 사용자 노드의 직렬화 필드만 Inspector에 추가합니다.</summary>
        private void AddNodeSpecificProperties(SerializedProperty nodeProperty)
        {
            HashSet<string> hiddenFields = new(StringComparer.Ordinal)
            {
                "identifier",
                "displayName",
                "description",
                "position",
                "childIdentifiers",
            };
            SerializedProperty iterator = nodeProperty.Copy();
            SerializedProperty endProperty = iterator.GetEndProperty();
            bool enterChildren = true;
            bool hasVisibleProperty = false;
            while (iterator.NextVisible(enterChildren) &&
                !SerializedProperty.EqualContents(iterator, endProperty))
            {
                enterChildren = false;
                if (hiddenFields.Contains(iterator.name))
                    continue;
                inspectorPanel.Add(new PropertyField(iterator.Copy()));
                hasVisibleProperty = true;
            }
            if (!hasVisibleProperty)
            {
                Label emptyLabel = new("추가 설정이 없는 노드입니다.");
                emptyLabel.style.color = new Color(0.58f, 0.60f, 0.63f);
                inspectorPanel.Add(emptyLabel);
            }
        }

        private void ShowEmptyInspector()
        {
            if (inspectorPanel == null)
                return;
            runtimeInspectorVisible = false;
            inspectorPanel.Clear();
            AddInspectorTabs(false);
            inspectorPanel.Add(CreatePanelTitle("Node Inspector"));
            inspectorPanel.Add(new HelpBox("그래프에서 노드를 선택하세요.", HelpBoxMessageType.Info));
        }

        /// <summary>Behaviour Tree 인스펙터에 선택 항목과 Runtime Debug 탭을 추가합니다.</summary>
        private void AddInspectorTabs(bool isRuntimeTab)
        {
            VisualElement tabBar = new VisualElement();
            tabBar.style.flexDirection = FlexDirection.Row;
            tabBar.style.marginBottom = 10f;
            Button selectionTab = new Button(ShowSelectionInspector) { text = "Selection" };
            Button runtimeTab = new Button(ShowRuntimeInspector) { text = "Runtime" };
            selectionTab.style.flexGrow = 1f;
            runtimeTab.style.flexGrow = 1f;
            SWStateMachineGraphStyles.ApplyTabButton(selectionTab, !isRuntimeTab);
            SWStateMachineGraphStyles.ApplyTabButton(runtimeTab, isRuntimeTab);
            tabBar.Add(selectionTab);
            tabBar.Add(runtimeTab);
            inspectorPanel.Add(tabBar);
        }

        /// <summary>마지막으로 선택한 Behaviour 노드의 Inspector로 돌아갑니다.</summary>
        private void ShowSelectionInspector()
        {
            if (selectedNodeView != null && selectedNodeView.panel != null)
                ShowNodeInspector(selectedNodeView);
            else
                ShowEmptyInspector();
        }

        /// <summary>현재 Behaviour Tree 실행 인스턴스의 상태를 Inspector에 표시합니다.</summary>
        private void ShowRuntimeInspector()
        {
            runtimeInspectorVisible = true;
            runtimeRunner = FindRuntimeRunner();
            PopulateRuntimeInspector(runtimeRunner);
        }

        /// <summary>지정한 Runner의 실행 노드 상태와 Blackboard 값을 Runtime Debug에 표시합니다.</summary>
        private void PopulateRuntimeInspector(SWBehaviourTreeRunner runner)
        {
            if (inspectorPanel == null)
                return;

            inspectorPanel.Clear();
            AddInspectorTabs(true);
            inspectorPanel.Add(CreatePanelTitle("Runtime Debug"));
            if (!EditorApplication.isPlaying)
            {
                inspectorPanel.Add(new HelpBox(
                    "Play Mode에서 Behaviour Tree를 실행하면 상태 정보를 확인할 수 있습니다.",
                    HelpBoxMessageType.Info));
                return;
            }
            if (runner == null || runner.RuntimeTree == null)
            {
                inspectorPanel.Add(new HelpBox(
                    "현재 그래프를 실행하는 Behaviour Tree Runner를 찾지 못했습니다.",
                    HelpBoxMessageType.Warning));
                return;
            }

            SWBehaviourTreeAsset runtimeTree = runner.RuntimeTree;
            VisualElement runnerCard = SWStateMachineGraphStyles.CreateCard("Runner");
            runnerCard.Add(new Label($"Game Object  ·  {runner.gameObject.name}"));
            if (runtimeTree.TryGetNode(
                    runtimeTree.RootNodeIdentifier,
                    out SWBehaviourNode rootNode))
            {
                runnerCard.Add(new Label($"Root Status  ·  {rootNode.Status}"));
            }
            inspectorPanel.Add(runnerCard);

            VisualElement statusCard = SWStateMachineGraphStyles.CreateCard("Node Status");
            int visibleStatusCount = 0;
            for (int index = 0; index < runtimeTree.Nodes.Count; index++)
            {
                SWBehaviourNode node = runtimeTree.Nodes[index];
                if (node.Status == SWBehaviourStatus.Inactive)
                    continue;
                string nodeName = string.IsNullOrWhiteSpace(node.DisplayName)
                    ? node.GetType().Name
                    : node.DisplayName;
                statusCard.Add(new Label($"{nodeName}  ·  {node.Status}"));
                visibleStatusCount++;
            }
            if (visibleStatusCount == 0)
                statusCard.Add(new Label("실행 기록이 있는 노드가 없습니다."));
            inspectorPanel.Add(statusCard);

            VisualElement blackboardCard = SWStateMachineGraphStyles.CreateCard("Blackboard");
            for (int index = 0; index < runtimeTree.Blackboard.Entries.Count; index++)
            {
                SWBehaviourBlackboardEntry entry = runtimeTree.Blackboard.Entries[index];
                if (entry == null)
                    continue;
                blackboardCard.Add(new Label(
                    $"{entry.Name}  ·  {FormatRuntimeValue(entry.GetBoxedValue())}"));
            }
            if (runtimeTree.Blackboard.Entries.Count == 0)
                blackboardCard.Add(new Label("Blackboard 항목이 없습니다."));
            inspectorPanel.Add(blackboardCard);
        }

        /// <summary>Runtime Debug에서 표시할 Blackboard 값을 읽기 쉬운 문자열로 변환합니다.</summary>
        private static string FormatRuntimeValue(object value)
        {
            if (value is UnityEngine.Object unityObject)
                return unityObject == null ? "None" : unityObject.name;
            return value?.ToString() ?? "null";
        }

        private static Label CreatePanelTitle(string text)
        {
            Label label = new(text);
            label.style.fontSize = 15f;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.marginBottom = 8f;
            return label;
        }

        /// <summary>Blackboard 값 종류에 대응하는 직렬화 필드 이름을 반환합니다.</summary>
        private static string GetBlackboardValuePropertyName(SWBehaviourBlackboardValueType valueType)
        {
            return valueType switch
            {
                SWBehaviourBlackboardValueType.Boolean => "booleanValue",
                SWBehaviourBlackboardValueType.Integer => "integerValue",
                SWBehaviourBlackboardValueType.Float => "floatValue",
                SWBehaviourBlackboardValueType.String => "stringValue",
                SWBehaviourBlackboardValueType.Vector2 => "vector2Value",
                SWBehaviourBlackboardValueType.Vector3 => "vector3Value",
                SWBehaviourBlackboardValueType.Object => "objectValue",
                _ => "stringValue",
            };
        }

        private void RecordNodeChange(string operation)
        {
            if (treeAsset == null)
                return;
            Undo.RecordObject(treeAsset, operation);
            EditorUtility.SetDirty(treeAsset);
        }

        private void OnGraphChanged()
        {
            RebuildBlackboard();
            ValidateTree();
        }

        private void ValidateTree()
        {
            if (validationHeaderButton == null || validationMessageList == null)
                return;

            validationMessageList.Clear();
            if (treeAsset == null)
            {
                validationHeaderButton.text = "  Graph Validation · Behaviour Tree 에셋을 선택하세요.";
                validationHeaderButton.style.color = new Color(0.62f, 0.65f, 0.68f);
                SetValidationExpanded(false);
                return;
            }

            List<string> issues = new();
            if (!treeAsset.TryGetNode(treeAsset.RootNodeIdentifier, out _))
                issues.Add("Root 노드가 없습니다.");
            HashSet<string> reachableNodes = new(StringComparer.Ordinal);
            CollectReachableNodes(treeAsset.RootNodeIdentifier, reachableNodes);
            for (int index = 0; index < treeAsset.Nodes.Count; index++)
            {
                SWBehaviourNode node = treeAsset.Nodes[index];
                if (!reachableNodes.Contains(node.Identifier))
                    issues.Add($"{node.DisplayName}: Root에서 도달할 수 없습니다.");
                if (node is SWBehaviourDecoratorNode && node.ChildIdentifiers.Count != 1)
                    issues.Add($"{node.DisplayName}: Decorator는 자식이 정확히 하나 필요합니다.");
                if (node is SWBehaviourCompositeNode && node.ChildIdentifiers.Count == 0)
                    issues.Add($"{node.DisplayName}: Composite에 자식이 없습니다.");
            }
            HashSet<string> blackboardNames = new(StringComparer.Ordinal);
            for (int index = 0; index < treeAsset.Blackboard.Entries.Count; index++)
            {
                SWBehaviourBlackboardEntry entry = treeAsset.Blackboard.Entries[index];
                if (entry == null)
                {
                    issues.Add("Blackboard에 복구할 수 없는 null 항목이 있습니다.");
                    continue;
                }
                string entryName = entry.Name;
                if (string.IsNullOrWhiteSpace(entryName))
                    issues.Add("Blackboard에 이름이 비어 있는 항목이 있습니다.");
                else if (!blackboardNames.Add(entryName))
                    issues.Add($"Blackboard 이름이 중복됩니다: {entryName}");
            }
            validationHeaderButton.text = issues.Count == 0
                ? "✓  Graph Validation · No Issues"
                : $"{(validationExpanded ? "▼" : "▶")}  Graph Validation · {issues.Count} Issues";
            validationHeaderButton.style.color = issues.Count == 0
                ? new Color(0.34f, 0.78f, 0.52f)
                : new Color(1f, 0.7f, 0.28f);
            validationHeaderButton.tooltip = issues.Count == 0
                ? "Behaviour Tree 구성에서 문제가 발견되지 않았습니다."
                : "클릭하여 검증 결과를 펼치거나 접습니다.";

            for (int index = 0; index < issues.Count; index++)
            {
                Label messageLabel = new Label($"⚠  {issues[index]}");
                messageLabel.style.whiteSpace = WhiteSpace.Normal;
                messageLabel.style.paddingLeft = 6f;
                messageLabel.style.paddingRight = 6f;
                messageLabel.style.paddingTop = 5f;
                messageLabel.style.paddingBottom = 5f;
                messageLabel.style.marginBottom = 2f;
                messageLabel.style.backgroundColor = new Color(0.16f, 0.13f, 0.08f);
                messageLabel.style.color = new Color(1f, 0.78f, 0.38f);
                validationMessageList.Add(messageLabel);
            }

            if (issues.Count == 0)
                SetValidationExpanded(false);
        }

        /// <summary>Root에서 연결을 따라 도달할 수 있는 노드 식별자를 수집합니다.</summary>
        private void CollectReachableNodes(string identifier, HashSet<string> reachableNodes)
        {
            if (!reachableNodes.Add(identifier) ||
                !treeAsset.TryGetNode(identifier, out SWBehaviourNode node))
                return;
            for (int index = 0; index < node.ChildIdentifiers.Count; index++)
                CollectReachableNodes(node.ChildIdentifiers[index], reachableNodes);
        }

        private void SaveTree()
        {
            if (treeAsset == null)
                return;
            EditorUtility.SetDirty(treeAsset);
            AssetDatabase.SaveAssetIfDirty(treeAsset);
            ValidateTree();
        }

        /// <summary>선택한 게임 오브젝트의 실행 복제본에서 노드 상태를 읽어 표시합니다.</summary>
        private void RefreshRuntimeStatuses()
        {
            runtimeRunner = FindRuntimeRunner();
            graphView?.RefreshNodeStatuses(runtimeRunner?.RuntimeTree);
            if (runtimeInspectorVisible)
                PopulateRuntimeInspector(runtimeRunner);
        }

        /// <summary>현재 그래프를 실행 중인 Behaviour Tree Runner를 선택 항목 우선으로 찾습니다.</summary>
        private SWBehaviourTreeRunner FindRuntimeRunner()
        {
            if (!EditorApplication.isPlaying || treeAsset == null)
                return null;

            SWBehaviourTreeRunner selectedRunner = Selection.activeGameObject == null
                ? null
                : Selection.activeGameObject.GetComponent<SWBehaviourTreeRunner>();
            if (selectedRunner != null && selectedRunner.TreeAsset == treeAsset)
                return selectedRunner;

#if UNITY_6000_4_OR_NEWER
            SWBehaviourTreeRunner[] runners = FindObjectsByType<SWBehaviourTreeRunner>();
#else
            SWBehaviourTreeRunner[] runners = FindObjectsByType<SWBehaviourTreeRunner>(
                FindObjectsSortMode.None);
#endif
            for (int index = 0; index < runners.Length; index++)
            {
                if (runners[index].TreeAsset == treeAsset)
                    return runners[index];
            }
            return null;
        }

        private enum SWBehaviourNodeScriptKind
        {
            Action,
            Composite,
            Decorator,
        }
    }
}
