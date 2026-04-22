using UnityEditor;
using UnityEngine;

namespace SWTools
{
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