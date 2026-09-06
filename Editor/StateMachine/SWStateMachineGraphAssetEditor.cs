using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

using SW.StateMachine;
using SW.EditorTools.Util;

#if UNITY_6000_4_OR_NEWER
using SWObjectIdentifier = UnityEngine.EntityId;
#else
using SWObjectIdentifier = System.Int32;
#endif

namespace SW.EditorTools.StateMachine
{
    /// <summary>
    /// 상태 머신 그래프 에셋에서 그래프 편집기를 열 수 있게 합니다.
    /// </summary>
    [CustomEditor(typeof(SWStateMachineGraphAsset))]
    internal sealed class SWStateMachineGraphAssetEditor : Editor
    {
        #region 인스펙터
        /// <summary>기본 속성과 그래프 편집 버튼을 표시합니다.</summary>
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            GUILayout.Space(8f);

            if (GUILayout.Button("상태 머신 그래프 편집"))
                SWStateMachineGraphWindow.OpenGraph((SWStateMachineGraphAsset)target);
        }
        #endregion // 인스펙터

        #region 에셋 열기
        /// <summary>그래프 에셋을 두 번 클릭하면 그래프 편집기에서 엽니다.</summary>
        [OnOpenAsset]
        private static bool OnOpenAsset(SWObjectIdentifier objectIdentifier, int line)
        {
            string assetPath = AssetDatabase.GetAssetPath(SWEditorObjectUtility.FindObject(objectIdentifier));
            SWStateMachineGraphAsset asset =
                AssetDatabase.LoadAssetAtPath<SWStateMachineGraphAsset>(assetPath);
            if (asset == null)
                return false;

            SWStateMachineGraphWindow.OpenGraph(asset);
            return true;
        }
        #endregion // 에셋 열기
    }
}
