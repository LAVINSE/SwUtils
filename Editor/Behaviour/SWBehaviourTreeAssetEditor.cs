using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using SW.BehaviourTree;
using SW.EditorTools.Util;

#if UNITY_6000_4_OR_NEWER
using SWObjectIdentifier = UnityEngine.EntityId;
#else
using SWObjectIdentifier = System.Int32;
#endif

namespace SW.EditorTools.Behaviour
{
    /// <summary>Behaviour Tree 에셋 Inspector에 그래프 편집 진입점을 제공합니다.</summary>
    [CustomEditor(typeof(SWBehaviourTreeAsset))]
    internal sealed class SWBehaviourTreeAssetEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            SWBehaviourTreeAsset treeAsset = (SWBehaviourTreeAsset)target;
            EditorGUILayout.LabelField("Nodes", treeAsset.Nodes.Count.ToString());
            EditorGUILayout.LabelField("Blackboard Keys", treeAsset.Blackboard.Entries.Count.ToString());
            EditorGUILayout.Space(8f);
            if (GUILayout.Button("Behaviour Tree 편집", GUILayout.Height(28f)))
                SWBehaviourTreeEditorWindow.OpenTree(treeAsset);
        }

        /// <summary>Behaviour Tree 에셋을 두 번 누르면 전용 그래프 편집기에서 엽니다.</summary>
        [OnOpenAsset]
        private static bool OnOpenAsset(SWObjectIdentifier objectIdentifier, int line)
        {
            SWBehaviourTreeAsset asset = SWEditorObjectUtility.FindObject(objectIdentifier)
                as SWBehaviourTreeAsset;
            if (asset == null)
                return false;
            SWBehaviourTreeEditorWindow.OpenTree(asset);
            return true;
        }
    }
}
