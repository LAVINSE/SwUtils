using UnityEditor;
using UnityEngine;

namespace SWTools
{
    /// <summary>
    /// Missing Script가 있는 GameObject를 하이어라키에서 경고 아이콘으로 표시하는 에디터 확장입니다.
    /// </summary>
    [InitializeOnLoad]
    public class SWShowWarningComponentEditor
    {
        private static readonly GUIContent WarningIcon = new GUIContent("⚠️", "This GameObject has missing scripts");

        static SWShowWarningComponentEditor()
        {
            EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyGUI;
        }

        private static void OnHierarchyGUI(int instanceID, Rect selectionRect)
        {
            GameObject obj = EditorUtility.EntityIdToObject(instanceID) as GameObject;
            if (obj == null) return;

            // SWEditorUtils의 Missing Script 확인 사용
            if (SWEditorUtils.HasMissingScripts(obj))
            {
                DrawWarningIcon(selectionRect);
            }
        }

        private static void DrawWarningIcon(Rect selectionRect)
        {
            Rect iconRect = new Rect(
                selectionRect.xMax - SWEditorUtils.DefaultIconSize,
                selectionRect.y,
                SWEditorUtils.DefaultIconSize,
                SWEditorUtils.DefaultIconSize);
            GUI.Label(iconRect, WarningIcon);
        }
    }
}
