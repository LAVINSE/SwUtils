using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SWTools
{
    /// <summary>
    /// <see cref="SWReadOnlyAttribute"/>가 붙은 필드를 읽기 전용으로 그리는 드로어입니다.
    /// </summary>
    [CustomPropertyDrawer(typeof(SWReadOnlyAttribute))]
    public class SWReadOnlyAttributeDrawer : PropertyDrawer
    {
        #region 필드
        #endregion // 필드

        #region 프로퍼티
        #endregion // 프로퍼티

        /// <summary>
        /// 읽기 전용 필드의 기본 프로퍼티 높이를 반환합니다.
        /// </summary>
        /// <param name="property">높이를 계산할 SerializedProperty입니다.</param>
        /// <param name="label">필드 라벨입니다.</param>
        /// <returns>프로퍼티 표시 높이입니다.</returns>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        /// <summary>
        /// 필드를 비활성화된 상태로 그립니다.
        /// </summary>
        /// <param name="position">그려질 영역입니다.</param>
        /// <param name="property">대상 SerializedProperty입니다.</param>
        /// <param name="label">필드 라벨입니다.</param>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            GUI.enabled = false;
            EditorGUI.PropertyField(position, property, label, true);
            GUI.enabled = true;
        }
    }
}
