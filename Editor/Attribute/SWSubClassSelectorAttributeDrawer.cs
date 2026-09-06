using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

using SW.Attributes;
using SW.Util;

namespace SW.EditorTools.Attributes
{
    /// <summary>
    /// SWSubClassSelectorAttribute가 붙은 SerializeReference 필드의 하위 타입 선택 사용자 인터페이스를 그립니다.
    /// </summary>
    [CustomPropertyDrawer(typeof(SWSubClassSelectorAttribute))]
    public class SWSubClassSelectorAttributeDrawer : PropertyDrawer
    {
        #region 필드
        private const float WarningHeight = 38f;
        private const int MaximumDropdownLineCount = 13;
        private static readonly GUIContent NoneContent = new GUIContent("None");
        private static string copiedJson;
        private static Type copiedType;
        private readonly Dictionary<Type, SWSubClassSelectorDropdown> typeDropdowns = new Dictionary<Type, SWSubClassSelectorDropdown>();
        private readonly Dictionary<Type, AdvancedDropdownState> dropdownStates = new Dictionary<Type, AdvancedDropdownState>();
        #endregion // 필드

        #region 함수
        /// <summary>
        /// Inspector에서 하위 타입 선택 사용자 인터페이스와 실제 필드를 그립니다.
        /// </summary>
        /// <param name="position">그려질 영역입니다.</param>
        /// <param name="property">대상 SerializedProperty입니다.</param>
        /// <param name="label">필드 라벨입니다.</param>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SWConditionAttribute conditionAttribute = GetConditionAttribute();
            if (conditionAttribute != null)
            {
                bool conditionResult = GetConditionAttributeResult(conditionAttribute, property);
                if (!ShouldDisplay(conditionAttribute, conditionResult))
                {
                    return;
                }

                bool previousEnabled = GUI.enabled;
                try
                {
                    GUI.enabled = previousEnabled && conditionResult;
                    DrawProperty(position, property, label);
                }
                finally
                {
                    GUI.enabled = previousEnabled;
                }

                return;
            }

            DrawProperty(position, property, label);
        }

        /// <summary>
        /// 조건 처리 이후 실제 프로퍼티를 그립니다.
        /// </summary>
        /// <param name="position">그려질 영역입니다.</param>
        /// <param name="property">대상 프로퍼티입니다.</param>
        /// <param name="label">프로퍼티 라벨입니다.</param>
        private void DrawProperty(Rect position, SerializedProperty property, GUIContent label)
        {
            if (IsCollectionProperty(property))
            {
                DrawCollection(position, property, label);
                return;
            }

            if (property.propertyType != SerializedPropertyType.ManagedReference)
            {
                DrawInvalidProperty(position, property, label);
                return;
            }

            Type baseType = GetBaseType();
            DrawManagedReference(position, property, CreateLabel(property, label), baseType);
        }

        /// <summary>
        /// Inspector에서 차지할 전체 높이를 반환합니다.
        /// </summary>
        /// <param name="property">대상 SerializedProperty입니다.</param>
        /// <param name="label">필드 라벨입니다.</param>
        /// <returns>Inspector 표시 높이입니다.</returns>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            SWConditionAttribute conditionAttribute = GetConditionAttribute();
            if (conditionAttribute != null)
            {
                bool conditionResult = GetConditionAttributeResult(conditionAttribute, property);
                if (!ShouldDisplay(conditionAttribute, conditionResult))
                {
                    return -EditorGUIUtility.standardVerticalSpacing;
                }
            }

            if (IsCollectionProperty(property))
            {
                return GetCollectionHeight(property, label);
            }

            if (property.propertyType != SerializedPropertyType.ManagedReference)
            {
                return EditorGUI.GetPropertyHeight(property, label, true) + WarningHeight;
            }

            return GetManagedReferenceHeight(property, label);
        }

        /// <summary>
        /// 같은 필드에 선언된 SWConditionAttribute를 반환합니다.
        /// </summary>
        /// <returns>조건 어트리뷰트가 있으면 해당 인스턴스, 없으면 null입니다.</returns>
        private SWConditionAttribute GetConditionAttribute()
        {
            return fieldInfo?.GetCustomAttributes(typeof(SWConditionAttribute), true)
                .FirstOrDefault() as SWConditionAttribute;
        }

        /// <summary>
        /// SWConditionAttribute가 가리키는 Boolean 필드 값을 읽습니다.
        /// </summary>
        /// <param name="conditionAttribute">조건 어트리뷰트입니다.</param>
        /// <param name="property">현재 표시 중인 프로퍼티입니다.</param>
        /// <returns>조건 충족 여부입니다.</returns>
        private bool GetConditionAttributeResult(SWConditionAttribute conditionAttribute, SerializedProperty property)
        {
            bool conditionResult = true;

            string conditionPath = GetConditionPath(property, conditionAttribute.ConditionBoolean);
            SerializedProperty conditionProperty = property.serializedObject.FindProperty(conditionPath);
            conditionProperty ??= property.serializedObject.FindProperty(conditionAttribute.ConditionBoolean);

            if (conditionProperty != null)
            {
                conditionResult = conditionProperty.boolValue;
            }
            else
            {
                SWLog.LogError("지정한 Boolean 필드명을 찾을 수 없습니다 - " + conditionAttribute.ConditionBoolean);
            }

            if (conditionAttribute.Negative)
            {
                conditionResult = !conditionResult;
            }

            return conditionResult;
        }

        /// <summary>
        /// 현재 프로퍼티와 같은 계층에 있는 조건 필드 경로를 계산합니다.
        /// </summary>
        /// <param name="property">현재 표시 중인 프로퍼티입니다.</param>
        /// <param name="conditionFieldName">조건 Boolean 필드 이름입니다.</param>
        /// <returns>조건 필드의 SerializedProperty 경로입니다.</returns>
        private static string GetConditionPath(SerializedProperty property, string conditionFieldName)
        {
            string propertyPath = property.propertyPath;
            int lastDotIndex = propertyPath.LastIndexOf('.');
            return lastDotIndex < 0
                ? conditionFieldName
                : string.Concat(propertyPath.Substring(0, lastDotIndex + 1), conditionFieldName);
        }

        /// <summary>
        /// 숨김 옵션과 조건 결과를 조합하여 필드를 표시할지 결정합니다.
        /// </summary>
        /// <param name="conditionAttribute">조건 어트리뷰트입니다.</param>
        /// <param name="conditionResult">조건 충족 여부입니다.</param>
        /// <returns>필드를 그려야 하면 true입니다.</returns>
        private static bool ShouldDisplay(SWConditionAttribute conditionAttribute, bool conditionResult)
        {
            return !conditionAttribute.Hidden || conditionResult;
        }

        /// <summary>
        /// SerializeReference 배열 또는 리스트를 직접 그립니다.
        /// </summary>
        /// <param name="position">그려질 영역입니다.</param>
        /// <param name="property">대상 컬렉션 SerializedProperty입니다.</param>
        /// <param name="label">필드 라벨입니다.</param>
        private void DrawCollection(Rect position, SerializedProperty property, GUIContent label)
        {
            Rect foldoutPosition = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            property.isExpanded = EditorGUI.Foldout(foldoutPosition, property.isExpanded, label, true);

            if (!property.isExpanded)
            {
                return;
            }

            EditorGUI.indentLevel++;

            float currentY = foldoutPosition.yMax + EditorGUIUtility.standardVerticalSpacing;
            SerializedProperty sizeProperty = property.FindPropertyRelative("Array.size");
            Rect sizePosition = new Rect(position.x, currentY, position.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.PropertyField(sizePosition, sizeProperty);

            currentY = sizePosition.yMax + EditorGUIUtility.standardVerticalSpacing;
            Type baseType = GetBaseType();

            for (int index = 0; index < property.arraySize; index++)
            {
                SerializedProperty elementProperty = property.GetArrayElementAtIndex(index);
                GUIContent elementLabel = new GUIContent($"Element {index}");
                float elementHeight = elementProperty.propertyType == SerializedPropertyType.ManagedReference
                    ? GetManagedReferenceHeight(elementProperty, elementLabel)
                    : EditorGUI.GetPropertyHeight(elementProperty, elementLabel, true);
                Rect elementPosition = new Rect(position.x, currentY, position.width, elementHeight);

                if (elementProperty.propertyType == SerializedPropertyType.ManagedReference)
                {
                    DrawManagedReference(elementPosition, elementProperty, elementLabel, baseType);
                }
                else
                {
                    EditorGUI.PropertyField(elementPosition, elementProperty, elementLabel, true);
                }

                currentY += elementHeight + EditorGUIUtility.standardVerticalSpacing;
            }

            EditorGUI.indentLevel--;
        }

        /// <summary>
        /// 단일 SerializeReference 값을 그립니다.
        /// </summary>
        /// <param name="position">그려질 영역입니다.</param>
        /// <param name="property">대상 SerializedProperty입니다.</param>
        /// <param name="label">필드 라벨입니다.</param>
        /// <param name="baseType">선택 가능한 타입의 기준 타입입니다.</param>
        private void DrawManagedReference(Rect position, SerializedProperty property, GUIContent label, Type baseType)
        {
            EditorGUI.BeginProperty(position, label, property);

            Rect selectorPosition = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            Rect dropdownPosition = EditorGUI.PrefixLabel(selectorPosition, label);
            GUIContent selectedTypeName = GetSelectedTypeName(property);

            if (EditorGUI.DropdownButton(dropdownPosition, selectedTypeName, FocusType.Keyboard))
            {
                SWSubClassSelectorDropdown dropdown = GetDropdown(baseType);
                dropdown.OnTypeSelected = selectedType => ApplySelectedType(property, selectedType);
                dropdown.Show(dropdownPosition);
            }

            HandleSelectorContextMenu(dropdownPosition, property, baseType);

            if (!string.IsNullOrEmpty(property.managedReferenceFullTypename))
            {
                Rect foldoutPosition = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
                property.isExpanded = EditorGUI.Foldout(foldoutPosition, property.isExpanded, GUIContent.none, true);
            }

            if (property.isExpanded && property.managedReferenceValue != null)
            {
                Rect propertyPosition = new Rect(
                    position.x,
                    selectorPosition.yMax + EditorGUIUtility.standardVerticalSpacing,
                    position.width,
                    GetChildPropertiesHeight(property));

                DrawChildProperties(propertyPosition, property);
            }

            EditorGUI.EndProperty();
        }

        /// <summary>
        /// 선택 버튼 우클릭 메뉴를 처리합니다.
        /// </summary>
        /// <param name="position">우클릭을 받을 영역입니다.</param>
        /// <param name="property">대상 SerializedProperty입니다.</param>
        /// <param name="baseType">선택 가능한 타입의 기준 타입입니다.</param>
        private void HandleSelectorContextMenu(Rect position, SerializedProperty property, Type baseType)
        {
            Event currentEvent = Event.current;
            if (currentEvent.type != EventType.ContextClick || !position.Contains(currentEvent.mousePosition))
            {
                return;
            }

            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("Copy"), false, () => CopyManagedReference(property));

            if (CanPaste(baseType))
            {
                menu.AddItem(new GUIContent("Paste"), false, () => PasteManagedReference(property));
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("Paste"));
            }

            menu.AddSeparator(string.Empty);
            menu.AddItem(new GUIContent("Clear"), false, () => ApplySelectedType(property, null));

            if (property.managedReferenceValue != null)
            {
                Type selectedType = property.managedReferenceValue.GetType();
                menu.AddItem(new GUIContent("Reset"), false, () => ApplySelectedType(property, selectedType, false));
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("Reset"));
            }

            menu.ShowAsContext();
            currentEvent.Use();
        }

        /// <summary>
        /// 현재 값을 클립보드 저장소에 복사합니다.
        /// </summary>
        /// <param name="property">대상 SerializedProperty입니다.</param>
        private void CopyManagedReference(SerializedProperty property)
        {
            object value = property.managedReferenceValue;
            if (value == null)
            {
                copiedJson = null;
                copiedType = null;
                return;
            }

            copiedType = value.GetType();
            copiedJson = JsonUtility.ToJson(value);
        }

        /// <summary>
        /// 클립보드 저장소의 값을 현재 필드에 붙여넣습니다.
        /// </summary>
        /// <param name="property">대상 SerializedProperty입니다.</param>
        private void PasteManagedReference(SerializedProperty property)
        {
            if (copiedType == null)
            {
                return;
            }

            object instance = Activator.CreateInstance(copiedType);
            if (!string.IsNullOrEmpty(copiedJson))
            {
                JsonUtility.FromJsonOverwrite(copiedJson, instance);
            }

            property.serializedObject.Update();
            property.managedReferenceValue = instance;
            property.isExpanded = true;
            property.serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// 클립보드 저장소의 타입을 현재 필드에 붙여넣을 수 있는지 확인합니다.
        /// </summary>
        /// <param name="baseType">선택 가능한 타입의 기준 타입입니다.</param>
        /// <returns>붙여넣을 수 있으면 true를 반환합니다.</returns>
        private bool CanPaste(Type baseType)
        {
            return copiedType != null && baseType != null && baseType.IsAssignableFrom(copiedType);
        }

        /// <summary>
        /// 선택 타입을 SerializeReference 값에 적용합니다.
        /// </summary>
        /// <param name="property">대상 SerializedProperty입니다.</param>
        /// <param name="selectedType">선택한 타입입니다.</param>
        /// <param name="restorePreviousValues">이전 값과 같은 이름의 필드를 복원할지 여부입니다.</param>
        private void ApplySelectedType(SerializedProperty property, Type selectedType, bool restorePreviousValues = true)
        {
            object previousValue = property.managedReferenceValue;
            string previousJson = previousValue != null && restorePreviousValues ? JsonUtility.ToJson(previousValue) : null;
            object nextValue = selectedType == null ? null : Activator.CreateInstance(selectedType);

            if (nextValue != null && !string.IsNullOrEmpty(previousJson))
            {
                JsonUtility.FromJsonOverwrite(previousJson, nextValue);
            }

            property.serializedObject.Update();
            property.managedReferenceValue = nextValue;
            property.isExpanded = nextValue != null;
            property.serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// 기준 타입에 맞는 타입 선택 드롭다운을 반환합니다.
        /// </summary>
        /// <param name="baseType">선택 가능한 타입의 기준 타입입니다.</param>
        /// <returns>검색 가능한 타입 선택 드롭다운입니다.</returns>
        private SWSubClassSelectorDropdown GetDropdown(Type baseType)
        {
            if (baseType == null)
            {
                baseType = typeof(object);
            }

            if (!typeDropdowns.TryGetValue(baseType, out SWSubClassSelectorDropdown dropdown))
            {
                if (!dropdownStates.TryGetValue(baseType, out AdvancedDropdownState state))
                {
                    state = new AdvancedDropdownState();
                    dropdownStates.Add(baseType, state);
                }

                dropdown = new SWSubClassSelectorDropdown(state, baseType, MaximumDropdownLineCount);
                typeDropdowns.Add(baseType, dropdown);
            }

            return dropdown;
        }

        /// <summary>
        /// 현재 선택된 타입 이름을 반환합니다.
        /// </summary>
        /// <param name="property">대상 SerializedProperty입니다.</param>
        /// <returns>선택 버튼에 표시할 타입 이름입니다.</returns>
        private GUIContent GetSelectedTypeName(SerializedProperty property)
        {
            object currentValue = property.managedReferenceValue;
            if (currentValue == null)
            {
                return NoneContent;
            }

            return new GUIContent(SWSubClassSelectorTypeUtility.GetTypeNameWithoutPath(currentValue.GetType()));
        }

        /// <summary>
        /// 어트리뷰트 옵션을 반영한 라벨을 생성합니다.
        /// </summary>
        /// <param name="property">대상 SerializedProperty입니다.</param>
        /// <param name="label">기본 라벨입니다.</param>
        /// <returns>Inspector에 표시할 라벨입니다.</returns>
        private GUIContent CreateLabel(SerializedProperty property, GUIContent label)
        {
            SWSubClassSelectorAttribute selectorAttribute = (SWSubClassSelectorAttribute)attribute;
            object currentValue = property.managedReferenceValue;

            if (!selectorAttribute.UseToStringAsLabel || currentValue == null || property.hasMultipleDifferentValues)
            {
                return label;
            }

            return new GUIContent(currentValue.ToString(), label.tooltip);
        }

        /// <summary>
        /// 컬렉션 필드인지 확인합니다.
        /// </summary>
        /// <param name="property">검사할 SerializedProperty입니다.</param>
        /// <returns>배열 또는 리스트 필드이면 true를 반환합니다.</returns>
        private bool IsCollectionProperty(SerializedProperty property)
        {
            return property.isArray && property.propertyType != SerializedPropertyType.String;
        }

        /// <summary>
        /// 컬렉션 필드가 Inspector에서 차지할 높이를 반환합니다.
        /// </summary>
        /// <param name="property">대상 컬렉션 SerializedProperty입니다.</param>
        /// <param name="label">필드 라벨입니다.</param>
        /// <returns>컬렉션 전체 표시 높이입니다.</returns>
        private float GetCollectionHeight(SerializedProperty property, GUIContent label)
        {
            float height = EditorGUIUtility.singleLineHeight;

            if (!property.isExpanded)
            {
                return height;
            }

            height += EditorGUIUtility.standardVerticalSpacing;
            height += EditorGUIUtility.singleLineHeight;

            for (int index = 0; index < property.arraySize; index++)
            {
                SerializedProperty elementProperty = property.GetArrayElementAtIndex(index);
                GUIContent elementLabel = new GUIContent($"Element {index}");
                height += EditorGUIUtility.standardVerticalSpacing;
                height += elementProperty.propertyType == SerializedPropertyType.ManagedReference
                    ? GetManagedReferenceHeight(elementProperty, elementLabel)
                    : EditorGUI.GetPropertyHeight(elementProperty, elementLabel, true);
            }

            return height;
        }

        /// <summary>
        /// SerializeReference 요소가 Inspector에서 차지할 높이를 반환합니다.
        /// </summary>
        /// <param name="property">대상 SerializedProperty입니다.</param>
        /// <param name="label">필드 라벨입니다.</param>
        /// <returns>SerializeReference 표시 높이입니다.</returns>
        private float GetManagedReferenceHeight(SerializedProperty property, GUIContent label)
        {
            float height = EditorGUIUtility.singleLineHeight;

            if (property.isExpanded && property.managedReferenceValue != null)
            {
                height += EditorGUIUtility.standardVerticalSpacing;
                height += GetChildPropertiesHeight(property);
            }

            return height;
        }

        /// <summary>
        /// SerializeReference 값의 자식 프로퍼티를 그립니다.
        /// </summary>
        /// <param name="position">그려질 영역입니다.</param>
        /// <param name="property">대상 SerializedProperty입니다.</param>
        private void DrawChildProperties(Rect position, SerializedProperty property)
        {
            Rect childPosition = new Rect(position.x, position.y, position.width, 0f);

            foreach (SerializedProperty childProperty in GetChildProperties(property))
            {
                GUIContent childLabel = new GUIContent(childProperty.displayName, childProperty.tooltip);
                childPosition.height = EditorGUI.GetPropertyHeight(childProperty, childLabel, true);
                EditorGUI.PropertyField(childPosition, childProperty, childLabel, true);
                childPosition.y += childPosition.height + EditorGUIUtility.standardVerticalSpacing;
            }
        }

        /// <summary>
        /// SerializeReference 값의 자식 프로퍼티 높이를 반환합니다.
        /// </summary>
        /// <param name="property">대상 SerializedProperty입니다.</param>
        /// <returns>자식 프로퍼티 전체 표시 높이입니다.</returns>
        private float GetChildPropertiesHeight(SerializedProperty property)
        {
            float height = 0f;
            bool isFirst = true;

            foreach (SerializedProperty childProperty in GetChildProperties(property))
            {
                if (!isFirst)
                {
                    height += EditorGUIUtility.standardVerticalSpacing;
                }

                isFirst = false;
                GUIContent childLabel = new GUIContent(childProperty.displayName, childProperty.tooltip);
                height += EditorGUI.GetPropertyHeight(childProperty, childLabel, true);
            }

            return height;
        }

        /// <summary>
        /// SerializeReference 값의 직계 자식 프로퍼티를 반환합니다.
        /// </summary>
        /// <param name="property">대상 SerializedProperty입니다.</param>
        /// <returns>직계 자식 프로퍼티 목록입니다.</returns>
        private IEnumerable<SerializedProperty> GetChildProperties(SerializedProperty property)
        {
            SerializedProperty iterator = property.Copy();
            SerializedProperty endProperty = iterator.GetEndProperty();
            bool enterChildren = true;

            while (iterator.NextVisible(enterChildren) && !SerializedProperty.EqualContents(iterator, endProperty))
            {
                enterChildren = false;
                yield return iterator.Copy();
            }
        }

        /// <summary>
        /// SerializeReference가 아닌 필드에 어트리뷰트가 사용되었을 때 경고와 기본 필드를 표시합니다.
        /// </summary>
        /// <param name="position">그려질 영역입니다.</param>
        /// <param name="property">대상 SerializedProperty입니다.</param>
        /// <param name="label">필드 라벨입니다.</param>
        private void DrawInvalidProperty(Rect position, SerializedProperty property, GUIContent label)
        {
            Rect warningPosition = new Rect(position.x, position.y, position.width, WarningHeight);
            EditorGUI.HelpBox(warningPosition, "SWSubClassSelector는 SerializeReference 필드에만 사용할 수 있습니다.", MessageType.Warning);

            Rect propertyPosition = new Rect(
                position.x,
                warningPosition.yMax + EditorGUIUtility.standardVerticalSpacing,
                position.width,
                EditorGUI.GetPropertyHeight(property, label, true));

            EditorGUI.PropertyField(propertyPosition, property, label, true);
        }

        /// <summary>
        /// 필드 또는 컬렉션 요소의 기준 타입을 반환합니다.
        /// </summary>
        /// <returns>SerializeReference에 할당할 수 있는 기준 타입입니다.</returns>
        private Type GetBaseType()
        {
            Type fieldType = fieldInfo.FieldType;

            if (fieldType.IsArray)
            {
                return fieldType.GetElementType();
            }

            if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(List<>))
            {
                return fieldType.GetGenericArguments()[0];
            }

            return fieldType;
        }
        #endregion // 함수
    }

    /// <summary>
    /// 검색 가능한 하위 타입 선택 드롭다운입니다.
    /// </summary>
    internal class SWSubClassSelectorDropdown : AdvancedDropdown
    {
        #region 필드
        private readonly Type baseType;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>
        /// 타입이 선택되었을 때 호출할 콜백입니다.
        /// </summary>
        public Action<Type> OnTypeSelected { get; set; }
        #endregion // 프로퍼티

        #region 함수
        /// <summary>
        /// 검색 가능한 하위 타입 선택 드롭다운을 생성합니다.
        /// </summary>
        /// <param name="state">드롭다운 상태입니다.</param>
        /// <param name="baseType">선택 가능한 타입의 기준 타입입니다.</param>
        /// <param name="maximumLineCount">드롭다운에 표시할 최대 줄 수입니다.</param>
        public SWSubClassSelectorDropdown(AdvancedDropdownState state, Type baseType, int maximumLineCount) : base(state)
        {
            this.baseType = baseType;
            minimumSize = new Vector2(260f, EditorGUIUtility.singleLineHeight * maximumLineCount);
        }

        /// <summary>
        /// 드롭다운 루트 항목을 생성합니다.
        /// </summary>
        /// <returns>루트 드롭다운 항목입니다.</returns>
        protected override AdvancedDropdownItem BuildRoot()
        {
            AdvancedDropdownItem root = new AdvancedDropdownItem("Select Type");
            root.AddChild(new SWSubClassSelectorDropdownItem("None", null));

            foreach (Type derivedType in SWSubClassSelectorTypeUtility.GetDerivedTypes(baseType))
            {
                AddTypeItem(root, derivedType);
            }

            return root;
        }

        /// <summary>
        /// 타입 선택 항목을 경로에 맞게 추가합니다.
        /// </summary>
        /// <param name="root">루트 드롭다운 항목입니다.</param>
        /// <param name="type">추가할 타입입니다.</param>
        private void AddTypeItem(AdvancedDropdownItem root, Type type)
        {
            string menuName = SWSubClassSelectorTypeUtility.GetMenuName(type);
            string[] pathParts = menuName.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            AdvancedDropdownItem parent = root;

            for (int index = 0; index < pathParts.Length; index++)
            {
                bool isLast = index == pathParts.Length - 1;
                if (isLast)
                {
                    parent.AddChild(new SWSubClassSelectorDropdownItem(pathParts[index], type));
                    continue;
                }

                parent = GetOrCreateChild(parent, pathParts[index]);
            }
        }

        /// <summary>
        /// 이름과 일치하는 하위 항목을 찾거나 새로 생성합니다.
        /// </summary>
        /// <param name="parent">부모 항목입니다.</param>
        /// <param name="name">찾을 항목 이름입니다.</param>
        /// <returns>찾거나 생성한 하위 항목입니다.</returns>
        private AdvancedDropdownItem GetOrCreateChild(AdvancedDropdownItem parent, string name)
        {
#if UNITY_6000_6_OR_NEWER
            foreach (AdvancedDropdownItem child in parent.childList)
#else
            foreach (AdvancedDropdownItem child in parent.children)
#endif
            {
                if (child.name == name)
                {
                    return child;
                }
            }

            AdvancedDropdownItem newChild = new AdvancedDropdownItem(name);
            parent.AddChild(newChild);
            return newChild;
        }

        /// <summary>
        /// 항목 선택 결과를 처리합니다.
        /// </summary>
        /// <param name="item">선택된 항목입니다.</param>
        protected override void ItemSelected(AdvancedDropdownItem item)
        {
            if (item is SWSubClassSelectorDropdownItem typeItem)
            {
                OnTypeSelected?.Invoke(typeItem.Type);
            }
        }
        #endregion // 함수
    }

    /// <summary>
    /// 하위 타입 정보를 담는 드롭다운 항목입니다.
    /// </summary>
    internal class SWSubClassSelectorDropdownItem : AdvancedDropdownItem
    {
        #region 프로퍼티
        /// <summary>
        /// 선택 시 적용할 타입입니다.
        /// </summary>
        public Type Type { get; private set; }
        #endregion // 프로퍼티

        #region 함수
        /// <summary>
        /// 하위 타입 드롭다운 항목을 생성합니다.
        /// </summary>
        /// <param name="name">항목 이름입니다.</param>
        /// <param name="type">선택 시 적용할 타입입니다.</param>
        public SWSubClassSelectorDropdownItem(string name, Type type) : base(name)
        {
            Type = type;
        }
        #endregion // 함수
    }

    /// <summary>
    /// SWSubClassSelector 타입 검색과 표시 이름 생성을 담당합니다.
    /// </summary>
    internal static class SWSubClassSelectorTypeUtility
    {
        #region 필드
        private static readonly Dictionary<Type, Type[]> BaseTypeToDerivedTypes = new Dictionary<Type, Type[]>();
        #endregion // 필드

        #region 함수
        /// <summary>
        /// 기준 타입에 할당할 수 있는 직렬화 가능 하위 타입 목록을 반환합니다.
        /// </summary>
        /// <param name="baseType">SerializeReference 필드 또는 컬렉션 요소의 기준 타입입니다.</param>
        /// <returns>Inspector 선택 메뉴에 표시할 하위 타입 목록입니다.</returns>
        public static Type[] GetDerivedTypes(Type baseType)
        {
            if (baseType == null)
            {
                return Array.Empty<Type>();
            }

            if (!BaseTypeToDerivedTypes.TryGetValue(baseType, out Type[] derivedTypes))
            {
                derivedTypes = TypeCache.GetTypesDerivedFrom(baseType)
                    .Where(type => IsSelectableType(baseType, type))
                    .OrderBy(GetMenuName)
                    .ToArray();

                BaseTypeToDerivedTypes.Add(baseType, derivedTypes);
            }

            return derivedTypes;
        }

        /// <summary>
        /// 타입 선택 메뉴에 표시할 전체 경로를 반환합니다.
        /// </summary>
        /// <param name="type">표시 이름을 만들 타입입니다.</param>
        /// <returns>타입 선택 메뉴에 표시할 경로입니다.</returns>
        public static string GetMenuName(Type type)
        {
            SWAddTypeMenuAttribute menuAttribute = GetAddTypeMenuAttribute(type);
            if (menuAttribute != null && !string.IsNullOrWhiteSpace(menuAttribute.MenuName))
            {
                return menuAttribute.MenuName;
            }

            return string.IsNullOrEmpty(type.Namespace) ? type.Name : $"{type.Namespace}/{type.Name}";
        }

        /// <summary>
        /// 타입 선택 버튼에 표시할 경로 없는 이름을 반환합니다.
        /// </summary>
        /// <param name="type">표시 이름을 만들 타입입니다.</param>
        /// <returns>경로를 제외한 타입 이름입니다.</returns>
        public static string GetTypeNameWithoutPath(Type type)
        {
            string menuName = GetMenuName(type);
            int separatorIndex = menuName.LastIndexOf("/", StringComparison.Ordinal);
            string typeName = separatorIndex >= 0 ? menuName.Substring(separatorIndex + 1) : menuName;
            return ObjectNames.NicifyVariableName(typeName);
        }

        /// <summary>
        /// SerializeReference 선택 후보로 사용할 수 있는 타입인지 확인합니다.
        /// </summary>
        /// <param name="baseType">필드의 기준 타입입니다.</param>
        /// <param name="type">검사할 타입입니다.</param>
        /// <returns>선택 가능한 타입이면 true를 반환합니다.</returns>
        private static bool IsSelectableType(Type baseType, Type type)
        {
            if (type == null || type == baseType)
            {
                return false;
            }

            if (type.IsAbstract || type.IsGenericType)
            {
                return false;
            }

            if (typeof(UnityEngine.Object).IsAssignableFrom(type))
            {
                return false;
            }

            if (!type.IsPublic && !type.IsNested)
            {
                return false;
            }

            if (!type.IsSerializable)
            {
                return false;
            }

            if (Attribute.GetCustomAttribute(type, typeof(SWHideInTypeMenuAttribute), false) != null)
            {
                return false;
            }

            return baseType.IsAssignableFrom(type);
        }

        /// <summary>
        /// 타입 메뉴 이름 어트리뷰트를 반환합니다.
        /// </summary>
        /// <param name="type">검사할 타입입니다.</param>
        /// <returns>타입 메뉴 이름 어트리뷰트입니다.</returns>
        private static SWAddTypeMenuAttribute GetAddTypeMenuAttribute(Type type)
        {
            return Attribute.GetCustomAttribute(type, typeof(SWAddTypeMenuAttribute), false) as SWAddTypeMenuAttribute;
        }
        #endregion // 함수
    }
}
