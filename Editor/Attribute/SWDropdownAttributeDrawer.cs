using UnityEngine;
using UnityEditor;
using System;

namespace SWTools
{
    /// <summary>
    /// Inspector에서 드롭다운으로 렌더링하는 PropertyDrawer입니다.
    /// </summary>
    [CustomPropertyDrawer(typeof(SWDropdownAttribute))]
    public class SWDropdownAttributeDrawer : PropertyDrawer
    {
        #region 필드
        /// <summary>
        /// 현재 드로어에 적용된 드롭다운 어트리뷰트입니다.
        /// </summary>
        protected SWDropdownAttribute dropdownAttribute;

        /// <summary>
        /// 인스펙터에 표시할 드롭다운 항목 이름 배열입니다.
        /// </summary>
        protected string[] dropdownDisplayNames;

        /// <summary>
        /// 현재 선택된 드롭다운 인덱스
        /// </summary>
        protected int selectedIndex = -1;

        /// <summary>
        /// 드롭다운 값들의 타입
        /// </summary>
        protected Type valueType;

        private bool isInitialized;
        #endregion // 필드

        #region 프로퍼티
        #endregion // 프로퍼티

        /// <summary>
        /// Inspector GUI를 그립니다.
        /// </summary>
        /// <param name="position">그려질 영역의 Rect</param>
        /// <param name="property">대상 SerializedProperty</param>
        /// <param name="label">필드 라벨</param>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // 최초 1회만 초기화 수행
            if (!isInitialized)
            {
                Initialize(property);
            }

            // 유효성 검사 실패 시 기본 필드로 표시
            if (!IsValid())
            {
                DrawErrorField(position, property, label);
                return;
            }

            DrawDropdown(position, property, label);
        }

        /// <summary>
        /// 드롭다운 데이터를 초기화합니다.
        /// </summary>
        /// <param name="property">대상 SerializedProperty</param>
        private void Initialize(SerializedProperty property)
        {
            isInitialized = true;
            dropdownAttribute = (SWDropdownAttribute)attribute;

            if (dropdownAttribute.DropdownValues == null || dropdownAttribute.DropdownValues.Length == 0)
            {
                Debug.LogWarning($"[SWDropdown] '{property.name}' 필드의 드롭다운 값이 비어있습니다.");
                return;
            }

            valueType = dropdownAttribute.DropdownValues[0].GetType();

            // 프로퍼티 타입과 드롭다운 값 타입 일치 여부 검사
            if (!IsTypeCompatible(property))
            {
                Debug.LogError($"[SWDropdown] '{property.name}' 필드 타입과 드롭다운 값 타입이 일치하지 않습니다.");
                return;
            }

            // 표시용 문자열 배열 생성
            dropdownDisplayNames = new string[dropdownAttribute.DropdownValues.Length];
            for (int i = 0; i < dropdownAttribute.DropdownValues.Length; i++)
            {
                dropdownDisplayNames[i] = dropdownAttribute.DropdownValues[i].ToString();
            }

            // 현재 값에 해당하는 인덱스 찾기
            selectedIndex = FindCurrentValueIndex(property);
        }

        /// <summary>
        /// 프로퍼티 타입과 드롭다운 값 타입이 호환되는지 검사합니다.
        /// </summary>
        private bool IsTypeCompatible(SerializedProperty property)
        {
            return (valueType == typeof(string) && property.propertyType == SerializedPropertyType.String) ||
                   (valueType == typeof(int) && property.propertyType == SerializedPropertyType.Integer) ||
                   (valueType == typeof(float) && property.propertyType == SerializedPropertyType.Float);
        }

        /// <summary>
        /// 현재 프로퍼티 값에 해당하는 드롭다운 인덱스를 찾습니다.
        /// </summary>
        /// <param name="property">대상 SerializedProperty</param>
        /// <returns>찾은 인덱스, 없으면 0</returns>
        private int FindCurrentValueIndex(SerializedProperty property)
        {
            for (int i = 0; i < dropdownDisplayNames.Length; i++)
            {
                bool isMatch = valueType == typeof(string) && property.stringValue == dropdownDisplayNames[i] ||
                               valueType == typeof(int) && property.intValue == Convert.ToInt32(dropdownDisplayNames[i]) ||
                               valueType == typeof(float) && Mathf.Approximately(property.floatValue, Convert.ToSingle(dropdownDisplayNames[i]));

                if (isMatch) return i;
            }
            return 0;
        }

        /// <summary>
        /// 드롭다운이 유효한 상태인지 확인합니다.
        /// </summary>
        private bool IsValid()
        {
            return dropdownDisplayNames != null &&
                   dropdownDisplayNames.Length > 0 &&
                   selectedIndex >= 0;
        }

        /// <summary>
        /// 에러 상태일 때 기본 필드와 경고를 표시합니다.
        /// </summary>
        private void DrawErrorField(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.PropertyField(position, property, label);
        }

        /// <summary>
        /// 드롭다운 팝업을 그리고 선택 변경을 처리합니다.
        /// </summary>
        private void DrawDropdown(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginChangeCheck();

            // GUIContent를 직접 사용하여 tooltip 유지
            selectedIndex = EditorGUI.Popup(position, label, selectedIndex,
                Array.ConvertAll(dropdownDisplayNames, x => new GUIContent(x)));

            if (EditorGUI.EndChangeCheck())
            {
                ApplySelectedValue(property);
            }
        }

        /// <summary>
        /// 선택된 값을 프로퍼티에 적용합니다.
        /// </summary>
        /// <param name="property">대상 SerializedProperty</param>
        private void ApplySelectedValue(SerializedProperty property)
        {
            string selectedValue = dropdownDisplayNames[selectedIndex];

            if (valueType == typeof(string))
            {
                property.stringValue = selectedValue;
            }
            else if (valueType == typeof(int))
            {
                property.intValue = Convert.ToInt32(selectedValue);
            }
            else if (valueType == typeof(float))
            {
                property.floatValue = Convert.ToSingle(selectedValue);
            }

            property.serializedObject.ApplyModifiedProperties();
        }
    }
}
