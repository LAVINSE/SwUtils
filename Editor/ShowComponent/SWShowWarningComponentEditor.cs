using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SWTools
{
    [InitializeOnLoad]
    public class SWShowWarningComponentEditor
    {
        private const int IconSize = 16;
        private static readonly GUIContent WarningIcon = new GUIContent("⚠️", "This GameObject has missing scripts");

        static SWShowWarningComponentEditor()
        {
            EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyGUI;
        }

        private static void OnHierarchyGUI(int instanceID, Rect selectionRect)
        {
            GameObject obj = EditorUtility.EntityIdToObject(instanceID) as GameObject;
            if (obj == null) return;

            if (HasMissingScripts(obj))
            {
                DrawWarningIcon(selectionRect);
            }
        }

        private static bool HasMissingScripts(GameObject obj)
        {
            return obj.GetComponents<MonoBehaviour>().Any(x => x == null);
        }

        private static void DrawWarningIcon(Rect selectionRect)
        {
            Rect iconRect = new Rect(selectionRect.xMax - IconSize, selectionRect.y, IconSize, IconSize);
            GUI.Label(iconRect, WarningIcon);
        }
    }
}