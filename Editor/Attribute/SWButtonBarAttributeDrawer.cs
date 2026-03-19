using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace SWTools
{
    [CustomPropertyDrawer(typeof(SWButtonBarAttribute))]
    public class SWButtonBarAttributeDrawer : PropertyDrawer
    {
        #region 필드
        /// <summary>
        /// 메서드 캐싱
        /// </summary>
        private MethodInfo[] eventMethodInfos = null;
        #endregion // 필드

        #region 프로퍼티
        #endregion // 프로퍼티

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            SWButtonBarAttribute buttonBarAttribute = (SWButtonBarAttribute)attribute;
            System.Type eventOwnerType = property.serializedObject.targetObject.GetType();

            var root = new VisualElement();

            Toolbar buttonBar = new();
            buttonBar.AddToClassList("sw-toolbar");

            if (eventMethodInfos == null)
            {
                eventMethodInfos = new MethodInfo[buttonBarAttribute.MethodNames.Length];
            }

            for (var i = 0; i < buttonBarAttribute.Labels.Length; i++)
            {
                var newButton = new ToolbarButton();
                newButton.text = buttonBarAttribute.Labels[i];
                newButton.style.flexGrow = 1;

                if (eventMethodInfos[i] == null)
                {
                    eventMethodInfos[i] = eventOwnerType.GetMethod(
                        buttonBarAttribute.MethodNames[i],
                        BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic
                    );
                }

                if (eventMethodInfos[i] != null)
                {
                    var methodIndex = i;
                    newButton.clicked += () => eventMethodInfos[methodIndex].Invoke(
                        property.serializedObject.targetObject, null
                    );
                }
                else
                {
                    Debug.LogWarning($"[SWButtonBar] '{eventOwnerType.Name}' 클래스에서 '{buttonBarAttribute.MethodNames[i]}' 메서드를 찾을 수 없습니다.");
                }

                if (buttonBarAttribute.OnlyWhenPlayMode[i] && !Application.isPlaying)
                {
                    newButton.SetEnabled(false);
                }

                buttonBar.Add(newButton);
            }

            root.Add(buttonBar);
            return root;
        }
    }
}
