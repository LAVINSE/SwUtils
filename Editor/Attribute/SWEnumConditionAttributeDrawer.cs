using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SWTools
{
    /// <summary>
    /// Inspector에서 렌더링하는 커스텀 PropertyDrawer입니다.
    /// 조건 enum 값에 따라 필드를 표시하거나 숨기고, 비활성화 상태를 제어합니다.
    /// </summary>
    [CustomPropertyDrawer(typeof(SWEnumConditionAttribute))]
    public class SWEnumConditionAttributeDrawer : PropertyDrawer
    {
        #region 필드
        /// <summary>
        /// propertyPath를 키로, enumPropPath를 값으로 저장하는 캐시입니다.
        /// 매 프레임 문자열 연산을 피하기 위해 사용합니다.
        /// </summary>
        private static Dictionary<string, string> cachedPaths = new();
        #endregion // 필드

        #region 프로퍼티
        #endregion // 프로퍼티

        [InitializeOnLoadMethod]
        private static void ClearCacheOnReload()
        {
            cachedPaths.Clear();
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SWEnumConditionAttribute enumConditionAttribute = (SWEnumConditionAttribute)attribute;
            bool enabled = GetConditionAttributeResult(enumConditionAttribute, property);
            bool previouslyEnabled = GUI.enabled;
            GUI.enabled = enabled;
            if (!enumConditionAttribute.Hidden || enabled)
            {
                EditorGUI.PropertyField(position, property, label, true);
            }
            GUI.enabled = previouslyEnabled;
        }

        /// <summary>
        /// 조건 enum 값을 확인하여 필드 표시 여부를 결정합니다.
        /// </summary>
        /// <param name="enumConditionAttribute">검사할 어트리뷰트</param>
        /// <param name="property">대상 프로퍼티</param>
        /// <returns>필드를 표시하면 true, 아니면 false</returns>
        private bool GetConditionAttributeResult(SWEnumConditionAttribute enumConditionAttribute, SerializedProperty property)
        {
            bool enabled = true;

            SerializedProperty enumProp;
            string enumPropPath = string.Empty;
            string propertyPath = property.propertyPath;

            if (!cachedPaths.TryGetValue(propertyPath, out enumPropPath))
            {
                enumPropPath = propertyPath.Replace(property.name, enumConditionAttribute.ConditionEnum);
                cachedPaths.Add(propertyPath, enumPropPath);
            }

            enumProp = property.serializedObject.FindProperty(enumPropPath);

            if (enumProp != null)
            {
                int currentEnum = enumProp.enumValueIndex;
                enabled = enumConditionAttribute.ContainsBitFlag(currentEnum);
            }
            else
            {
                Debug.LogError($"[SWEnumCondition] 조건 enum을 찾을 수 없습니다: '{enumConditionAttribute.ConditionEnum}'");
            }

            return enabled;
        }

        /// <summary>
        /// 프로퍼티의 높이를 반환합니다. 숨김 상태면 0을 반환하여 공간을 차지하지 않습니다.
        /// </summary>
        /// <param name="property">대상 프로퍼티</param>
        /// <param name="label">표시할 라벨</param>
        /// <returns>프로퍼티 높이 (픽셀)</returns>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            SWEnumConditionAttribute enumConditionAttribute = (SWEnumConditionAttribute)attribute;
            bool enabled = GetConditionAttributeResult(enumConditionAttribute, property);

            if (!enumConditionAttribute.Hidden || enabled)
            {
                return EditorGUI.GetPropertyHeight(property, label);
            }
            else
            {
                return -EditorGUIUtility.standardVerticalSpacing;
            }
        }
    }
}
