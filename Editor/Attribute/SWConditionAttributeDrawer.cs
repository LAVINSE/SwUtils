using UnityEngine;
using UnityEditor;

namespace SWTools
{   
    /// <summary>
    /// SWConditionAttribute를 위한 커스텀 PropertyDrawer입니다.
    /// Inspector에서 특정 bool 필드의 값에 따라 프로퍼티를 조건부로 표시/숨김/비활성화합니다.
    /// </summary>
    [CustomPropertyDrawer(typeof(SWConditionAttribute))]
    public class SWConditionAttributeDrawer : PropertyDrawer
    {
        /// <summary>
        /// Inspector에서 프로퍼티를 그리는 메서드입니다.
        /// 조건에 따라 프로퍼티를 활성화/비활성화하거나 완전히 숨깁니다.
        /// </summary>
        /// <param name="position">프로퍼티가 그려질 사각형 영역</param>
        /// <param name="property">그려질 대상 SerializedProperty</param>
        /// <param name="label">프로퍼티 라벨 (필드 이름)</param>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SWConditionAttribute conditionAttribute = (SWConditionAttribute)attribute;

            bool enabled = GetConditionAttributeResult(conditionAttribute, property);
            bool previouslyEnabled = GUI.enabled;
            bool shouldDisplay = ShouldDisplay(conditionAttribute, enabled);
            if (shouldDisplay)
            {
                GUI.enabled = enabled;
                EditorGUI.PropertyField(position, property, label, true);
                GUI.enabled = previouslyEnabled;
            }
        }

        /// <summary>
        /// 조건 boolean 필드의 값을 읽어와 최종 조건 결과를 반환합니다.
        /// Negative 옵션이 설정된 경우 결과를 반전시킵니다.
        /// </summary>
        /// <param name="conditionAttribute">조건 어트리뷰트 정보</param>
        /// <param name="property">현재 프로퍼티</param>
        /// <returns>조건 충족 여부 (true: 활성화, false: 비활성화)</returns>
        private bool GetConditionAttributeResult(SWConditionAttribute conditionAttribute, SerializedProperty property)
        {
            bool enabled = true;

            string propertyPath = property.propertyPath;
            string conditionPath = propertyPath.Replace(property.name, conditionAttribute.ConditionBoolean);

            SerializedProperty propertyValue = property.serializedObject.FindProperty(conditionPath);

            if (propertyValue != null)
            {
                enabled = propertyValue.boolValue;
            }
            else
            {
                SWUtils.SWUtilsLog.LogError("지정한 Boolean 필드명을 찾을 수 없습니다 - " + conditionAttribute.ConditionBoolean);
            }
            
            // Negative 옵션이 설정된 경우 결과를 반전
            // 예: 조건이 true일 때 숨기고 싶은 경우 사용
            if (conditionAttribute.Negative)
            {
                enabled = !enabled;
            }

            return enabled;
        }

        /// <summary>
        /// Hidden 옵션과 조건 결과를 종합하여 프로퍼티 표시 여부를 결정합니다.
        /// </summary>
        /// <param name="conditionAttribute">조건 어트리뷰트 정보</param>
        /// <param name="result">조건 평가 결과</param>
        /// <returns>true: 표시, false: 완전히 숨김</returns>
        private bool ShouldDisplay(SWConditionAttribute conditionAttribute, bool result)
        {
            bool shouldDisplay = !conditionAttribute.Hidden || result;
            return shouldDisplay;
        }
        
        /// <summary>
        /// 프로퍼티가 Inspector에서 차지할 높이를 반환합니다.
        /// 숨겨진 프로퍼티는 음수 높이를 반환하여 공간을 차지하지 않도록 합니다.
        /// </summary>
        /// <param name="property">대상 프로퍼티</param>
        /// <param name="label">프로퍼티 라벨</param>
        /// <returns>프로퍼티 높이 (픽셀)</returns>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            SWConditionAttribute conditionAttribute = (SWConditionAttribute)attribute;
            bool enabled = GetConditionAttributeResult(conditionAttribute, property);

            bool shouldDisplay = ShouldDisplay(conditionAttribute, enabled);
            if (shouldDisplay)
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
