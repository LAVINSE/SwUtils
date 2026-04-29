using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

namespace SWTools
{
    /// <summary>
    /// 하이어라키에 GameObject가 가진 주요 컴포넌트 아이콘을 표시하는 에디터 확장입니다.
    /// </summary>
    public static class SWShowComponentEditor
    {
        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyGUI;
        }

        /** 하이러라키에 표시한다 */
        private static void OnHierarchyGUI(int instanceID, Rect selectionRect)
        {
            GameObject obj = EditorUtility.EntityIdToObject(instanceID) as GameObject;
            if (obj == null) return;

            var components = GetRelevantComponents(obj);
            if (components.Count == 0) return;

            DrawComponentIcons(components, selectionRect);
        }

        /** 객체가 가지고 있는 컴포넌트들을 반환한다 */
        private static List<Component> GetRelevantComponents(GameObject obj)
        {
            return obj.GetComponents<Component>()
                .Where(x => x != null && !(x is Transform))
                .Reverse()
                .ToList();
        }

        /** 컴포넌트 아이콘들을 그린다 */
        private static void DrawComponentIcons(List<Component> components, Rect selectionRect)
        {
            Rect iconRect = new Rect(
                selectionRect.xMax - SWEditorUtils.DefaultIconSize,
                selectionRect.y,
                SWEditorUtils.DefaultIconSize,
                SWEditorUtils.DefaultIconSize);

            foreach (var component in components)
            {
                // SWEditorUtils 아이콘 유틸리티 사용
                Texture icon = SWEditorUtils.GetComponentIcon(component);
                if (icon == null) continue;

                // SWEditorUtils 아이콘 렌더링 사용
                bool isEnabled = SWEditorUtils.IsComponentEnabled(component);
                SWEditorUtils.DrawIcon(iconRect, icon, isEnabled);

                iconRect.x -= SWEditorUtils.DefaultIconSize;
            }
        }
    }
}
