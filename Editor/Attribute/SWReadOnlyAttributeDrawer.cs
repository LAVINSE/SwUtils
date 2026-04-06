using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SWTools
{
    [CustomPropertyDrawer(typeof(SWReadOnlyAttribute))]
    public class SWReadOnlyAttributeDrawer : PropertyDrawer
    {
        #region 필드
        #endregion // 필드

        #region 프로퍼티
        #endregion // 프로퍼티

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            GUI.enabled = false;
            EditorGUI.PropertyField(position, property, label, true);
            GUI.enabled = true;
        }
    }
}
