using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace SWTools
{
    /// <summary>
    /// SWMonobehaviour를 상속받은 모든 컴포넌트에 대한 커스텀 에디터
    /// </summary>
    [CanEditMultipleObjects]
    [CustomEditor(typeof(SWMonoBehaviour), true)]
    public class SWMonobehaviourEditor : Editor
    {
        #region 필드
        /// <summary>
        /// 에디터에 적용할 (Unity Style Sheet) 스타일시트
        /// </summary>
        public StyleSheet EditorStyleSheet;

        /// <summary>
        /// 중복 초기화 방지 플래그
        /// </summary>
        public bool DrawerInitialized;

        /// <summary>
        /// Key - 그룹 이름
        /// Value - 그룹 데이터
        /// 모든 인스펙터 그룹 정보 저장
        /// </summary>
        public Dictionary<string, SWGroupDataEditor> GroupDataDict;

        /// <summary>
        /// 그룹에 속하지 않는 프로퍼티 리스트
        /// </summary>
        public List<SerializedProperty> PropertiesList;

        /// <summary>
        /// SWButtonAttribute가 적용된 메서드 정보 리스트
        /// </summary>
        private List<SWButtonMethodInfo> buttonMethodList;

        /// <summary>
        /// 상시 다시 그려져야 하는지 여부
        /// SWRequiresConstantRepaintAttribute가 있으면 true
        /// </summary>
        private bool requiresConstantRepaint;

        /// <summary>
        /// 플레이 모드에서만 상시 다시 그려져야 하는지 여부
        /// SWRequiresConstantRepaintOnlyWhenPlaing가 있으면 true
        /// </summary>
        private bool requiresConstantRepaintOnlyWhenPlaying;

        /// <summary>
        /// 타겟 SWMonobehaviour
        /// </summary>
        private SWMonoBehaviour targetMonobehaviour;

        /// <summary>
        /// 타겟 SWMonobehaviour가 null이 아닌지 여부
        /// </summary>
        private bool targetMonobehaviourIsNotNull;

        /// <summary>
        /// SWHiddenAttribute로 숨겨야 할 프로퍼티 이름 배열
        /// </summary>
        private string[] hiddenPropertiesToHide;

        /// <summary>
        /// 숨겨야 할 SWHidden 프로퍼티가 있는지 여부
        /// </summary>
        private bool hasHiddenProperties = false;

        /// <summary>
        /// 기본 인스펙터를 그려야 하는지 여부
        /// 그룹화된 필드가 없으면 true
        /// </summary>
        protected bool shouldDrawBase = true;

        /// <summary>
        /// 타겟 오브젝트의 이름
        /// EditorPrefs 키 생성에 사용
        /// </summary>
        protected string targetTypeName;
        #endregion // 필드

        #region 내부 클래스
        /// <summary>
        /// 버튼 메서드 정보를 저장하는 클래스
        /// </summary>
        private class SWButtonMethodInfo
        {
            public MethodInfo Method;
            public SWButtonAttribute Attribute;
        }
        #endregion // 내부 클래스

        #region 프로퍼티
        #endregion // 프로퍼티

        /// <summary>
        /// 에디터가 리페인트가 필요한지 결정한다
        /// </summary>
        /// <returns></returns>
        public override bool RequiresConstantRepaint()
        {
            if (requiresConstantRepaintOnlyWhenPlaying)
            {
                // 플레이 중 + 타켓이 존재 + 컴포넌트 활성화 상태일 때만 리페인트
                return Application.isPlaying
                && targetMonobehaviourIsNotNull
                && targetMonobehaviour.enabled;
            }
            else
            {
                return requiresConstantRepaint;
            }
        }

        /// <summary>
        /// 에디터를 초기화합니다
        /// 1. 타겟 오브젝트의 모든 필드 정보 수집
        /// 2. SWGroupAttribute가 적용된 필드들을 그룹화
        /// 3. 각 그룹의 색상 및 초기 접힘 상태 설정
        /// 4. SerializedProperty 리스트 구성
        /// 5. SWButtonAttribute가 적용된 메서드 수집
        /// </summary>
        protected virtual void Initialized()
        {
            if (DrawerInitialized && PropertiesList != null)
            {
                return;
            }

            shouldDrawBase = true;
            GroupDataDict = new();
            PropertiesList = new();
            buttonMethodList = new();
            targetTypeName = target.GetType().Name;

            targetMonobehaviour = (SWMonoBehaviour)target;

            if (targetMonobehaviour != null)
            {
                targetMonobehaviourIsNotNull = true;
            }

            //리페인트 관련 어트리뷰트 확인
            requiresConstantRepaint = serializedObject.targetObject
                .GetType()
                .GetCustomAttribute<SWRequiresConstantRepaintAttribute>() != null;

            requiresConstantRepaintOnlyWhenPlaying = serializedObject.targetObject
                .GetType()
                .GetCustomAttribute<SWRequiresConstantRepaintOnlyWhenPlayingAttribute>() != null;

            // 버튼 메서드 수집
            CollectButtonMethods();

            // 필드 정보 수집
            List<FieldInfo> fieldInfoList;
            SWGroupAttribute previousGroupAttribute = default;
            int fieldInfoLength = SWMonobehaviourFieldInfo.GetFieldInfo(target, out fieldInfoList);

            for (int i = 0; i < fieldInfoLength; ++i)
            {
                SWGroupAttribute group = Attribute.GetCustomAttribute(fieldInfoList[i], typeof(SWGroupAttribute)) as SWGroupAttribute;
                SWGroupDataEditor groupData;

                if (group == null)
                {
                    // 이전 그룹이 다음 그룹까지 모든 필드 포함 옵션을 가지고 있으면
                    if (previousGroupAttribute != null && previousGroupAttribute.GroupAllFieldsUntilNextGroupAttribute)
                    {
                        // 기본 인스펙터 사용 안 함
                        shouldDrawBase = false;

                        // 그룹이 아직 없으면 새로 생성
                        if (!GroupDataDict.TryGetValue(previousGroupAttribute.GroupName, out groupData))
                        {
                            GroupDataDict.Add(previousGroupAttribute.GroupName, new SWGroupDataEditor
                            {
                                GroupAttribute = previousGroupAttribute,
                                GroupHashSet = new() { fieldInfoList[i].Name },
                                GroupColor = previousGroupAttribute.GroupColor
                            });
                        }
                        else
                        {
                            // 기존 그룹에 필드 추가
                            groupData.GroupColor = previousGroupAttribute.GroupColor;
                            groupData.GroupHashSet.Add(fieldInfoList[i].Name);
                        }
                    }
                    continue;
                }

                // 현재 그룹 어트리뷰트를 다음 반복을 위해 저장
                previousGroupAttribute = group;

                if (!GroupDataDict.TryGetValue(group.GroupName, out groupData))
                {
                    // 기본 접힘 상태 결정
                    bool fallbackOpenState = true;

                    if (group.ClosedByDefault)
                    {
                        fallbackOpenState = false;
                    }

                    // EditorPrefs에서 저장된 접힘 상태 불러오기
                    // 키 형식: "{그룹이름}{필드이름}{인스턴스ID}"
                    bool isGroupOpen = EditorPrefs.GetBool(string.Format($"{group.GroupName}{fieldInfoList[i].Name}{target.GetInstanceID()}"), fallbackOpenState);

                    // 새 그룹 데이터 생성 및 추가
                    GroupDataDict.Add(group.GroupName, new SWGroupDataEditor
                    {
                        GroupAttribute = group,
                        GroupHashSet = new() { fieldInfoList[i].Name },
                        GroupColor = group.GroupColor,
                        IsGroupOpen = isGroupOpen
                    });
                }
                else
                {
                    // 기존 그룹에 필드 추가
                    groupData.GroupHashSet.Add(fieldInfoList[i].Name);
                    groupData.GroupColor = group.GroupColor;
                }
            }

            // SerializedProperty 이터레이터로 모든 프로퍼티 순회
            SerializedProperty iterator = serializedObject.GetIterator();

            if (iterator.NextVisible(true))
            {
                do
                {
                    FillPropertiesList(iterator);
                } while (iterator.NextVisible(false));
            }

            DrawerInitialized = true;
        }

        /// <summary>
        /// SWButtonAttribute가 적용된 메서드들을 수집합니다
        /// </summary>
        private void CollectButtonMethods()
        {
            Type targetType = target.GetType();

            // 모든 인스턴스 메서드 검색 (public, non-public, 상속 포함)
            MethodInfo[] methods = targetType.GetMethods(
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.DeclaredOnly
            );

            // 상속된 메서드도 포함
            Type baseType = targetType.BaseType;
            while (baseType != null && baseType != typeof(MonoBehaviour))
            {
                MethodInfo[] baseMethods = baseType.GetMethods(
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.DeclaredOnly
                );
                methods = methods.Concat(baseMethods).ToArray();
                baseType = baseType.BaseType;
            }

            foreach (MethodInfo method in methods)
            {
                SWButtonAttribute buttonAttribute = method.GetCustomAttribute<SWButtonAttribute>();
                if (buttonAttribute != null)
                {
                    buttonMethodList.Add(new SWButtonMethodInfo
                    {
                        Method = method,
                        Attribute = buttonAttribute
                    });
                }
            }
        }

        /// <summary>
        /// SerializedProperty를 적절한 리스트에 추가합니다
        /// </summary>
        /// <param name="serializedProperty">추가할 SerializedProperty</param>
        public void FillPropertiesList(SerializedProperty serializedProperty)
        {
            bool shouldClose = false;

            // 모든 그룹을 순회하며 이 프로퍼티가 속한 그룹 찾기
            foreach (KeyValuePair<string, SWGroupDataEditor> pair in GroupDataDict)
            {
                if (pair.Value.GroupHashSet.Contains(serializedProperty.name))
                {
                    // 프로퍼티 복사본 생성 
                    SerializedProperty property = serializedProperty.Copy();
                    shouldClose = true;
                    pair.Value.PropertiesList.Add(property);
                    break;
                }
            }

            // 어떤 그룹에도 속하지 않으면 일반 리스트에 추가
            if (!shouldClose)
            {
                SerializedProperty property = serializedProperty.Copy();
                PropertiesList.Add(property);
            }
        }

        /// <summary>
        /// UI Toolkit을 사용하여 커스텀 인스펙터 GUI 생성
        /// 1. 초기화
        /// 2. 스타일시트 적용
        /// 3. 그룹이 없으면 기본 인스펙터 표시
        /// 4. 그룹이 있으면 스크립트 필드 + 각 그룹 폴드아웃 표시
        /// 5. 버튼 표시
        /// </summary>
        /// <returns></returns>
        public override VisualElement CreateInspectorGUI()
        {
            Initialized();

            VisualElement root = new();
            root.styleSheets.Add(EditorStyleSheet);

            SerializedProperty scriptProperty = serializedObject.FindProperty("m_Script");

            if (PropertiesList.Count == 0)
            {
                DrawButtons(root);
                return root;
            }

            if (shouldDrawBase)
            {
                VisualElement defaultInspector = new();
                InspectorElement.FillDefaultInspector(defaultInspector, serializedObject, this);
                root.Add(defaultInspector);

                DrawButtons(root);
                return root;
            }

            // 스크립트 필드 추가 
            PropertyField scriptField = new(scriptProperty);
            scriptField.SetEnabled(false);
            root.Add(scriptField);

            // 그룹에 속하지 않는 프로퍼티들 먼저 그리기 (그룹 위에 있던 필드들)
            foreach (SerializedProperty property in PropertiesList)
            {
                if (property.name == "m_Script")
                    continue;

                PropertyField field = new PropertyField(property);
                field.label = ObjectNames.NicifyVariableName(property.name);
                field.tooltip = property.tooltip;
                root.Add(field);
            }

            // 각 그룹을 폴드아웃으로 그리기
            foreach (KeyValuePair<string, SWGroupDataEditor> pair in GroupDataDict)
            {
                DrawGroup(pair.Value, root);
            }

            DrawButtons(root);

            serializedObject.ApplyModifiedProperties();

            return root;
        }

        /// <summary>
        /// SWButtonAttribute가 적용된 메서드들을 버튼으로 그립니다
        /// </summary>
        /// <param name="root">버튼을 추가할 부모 요소</param>
        protected virtual void DrawButtons(VisualElement root)
        {
            if (buttonMethodList == null || buttonMethodList.Count == 0)
            {
                return;
            }

            // 버튼 컨테이너 생성
            VisualElement buttonContainer = new VisualElement();
            buttonContainer.AddToClassList("sw-button-container");
            buttonContainer.style.marginTop = 10;

            foreach (SWButtonMethodInfo buttonInfo in buttonMethodList)
            {
                // Space 적용
                if (buttonInfo.Attribute.Space > 0)
                {
                    VisualElement spacer = new VisualElement();
                    spacer.style.height = buttonInfo.Attribute.Space;
                    buttonContainer.Add(spacer);
                }

                // 버튼 이름 결정 (DisplayName이 없으면 메서드 이름 사용)
                string buttonName = string.IsNullOrEmpty(buttonInfo.Attribute.DisplayName)
                    ? ObjectNames.NicifyVariableName(buttonInfo.Method.Name)
                    : buttonInfo.Attribute.DisplayName;

                // 버튼 생성
                Button button = new Button(() => InvokeButtonMethod(buttonInfo.Method))
                {
                    text = buttonName
                };
                button.AddToClassList("sw-button");

                // 파라미터가 있는 메서드는 비활성화
                ParameterInfo[] parameters = buttonInfo.Method.GetParameters();
                if (parameters.Length > 0)
                {
                    button.SetEnabled(false);
                    button.tooltip = "파라미터가 있는 메서드는 지원하지 않습니다.";
                }

                buttonContainer.Add(button);
            }

            root.Add(buttonContainer);
        }

        /// <summary>
        /// 버튼 클릭 시 메서드를 호출합니다
        /// </summary>
        /// <param name="method">호출할 메서드</param>
        private void InvokeButtonMethod(MethodInfo method)
        {
            // 다중 선택 지원
            foreach (UnityEngine.Object targetObject in targets)
            {
                // Undo 기록
                Undo.RecordObject(targetObject, $"Invoke {method.Name}");

                // 메서드 호출
                method.Invoke(targetObject, null);

                // 변경 사항 표시
                EditorUtility.SetDirty(targetObject);
            }
        }

        protected virtual void DrawGroup(SWGroupDataEditor groupData, VisualElement root)
        {
            // 폴드아웃 생성 및 설정
            Foldout foldout = new Foldout();
            foldout.text = groupData.GroupAttribute.GroupName;  // 그룹 이름 설정
            foldout.value = groupData.IsGroupOpen;               // 초기 접힘 상태
            foldout.AddToClassList("sw-foldout");               // CSS 클래스 추가
            foldout.style.borderLeftColor = groupData.GroupColor; // 왼쪽 테두리 색상

            // viewDataKey: UI 상태 자동 저장/복원용 고유 키
            foldout.viewDataKey = target.name + "-" + targetTypeName + groupData.GroupAttribute.GroupName;
            root.Add(foldout);

            // 토글(헤더) 요소에 CSS 클래스 추가
            var toggleElement = foldout.Q<Toggle>();
            toggleElement.AddToClassList("sw-foldout-toggle");

            // 그룹 내 모든 프로퍼티 그리기
            for (int i = 0; i < groupData.PropertiesList.Count; i++)
            {
                DrawChild(i, foldout, root);
            }

            /// <summary>
            /// 그룹 내 개별 프로퍼티를 그리는 로컬 함수입니다.
            /// </summary>
            /// <param name="i">프로퍼티 인덱스</param>
            /// <param name="foldout">부모 폴드아웃</param>
            /// <param name="root">루트 요소 (사용되지 않음)</param>
            void DrawChild(int i, Foldout foldout, VisualElement root)
            {
                // SWHidden으로 숨겨야 할 프로퍼티인지 확인
                if (hasHiddenProperties &&
                    hiddenPropertiesToHide.Contains(groupData.PropertiesList[i].name))
                {
                    return;  // 숨겨야 할 프로퍼티면 그리지 않음
                }

                // PropertyField 생성
                PropertyField field = new PropertyField(groupData.PropertiesList[i]);

                // 라벨을 읽기 좋게 변환 (예: "moveSpeed" → "Move Speed")
                field.label = ObjectNames.NicifyVariableName(groupData.PropertiesList[i].name);

                // 툴팁 설정
                field.tooltip = groupData.PropertiesList[i].tooltip;

                // 폴드아웃에 추가
                foldout.Add(field);
            }
        }
    }
}