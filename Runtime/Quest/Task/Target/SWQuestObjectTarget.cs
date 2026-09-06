using System;
using UnityEngine;

using SW.Attributes;

namespace SW.Quest
{
    /// <summary>
    /// Unity 오브젝트 참조 또는 이름으로 보고 대상을 비교하는 퀘스트 대상입니다.
    /// </summary>
    [CreateAssetMenu(fileName = "SWQuestObjectTarget_", menuName = "SWUtils/Quest/Target/Unity Object")]
    public sealed class SWQuestObjectTarget : SWQuestTarget
    {
        #region 필드
        [SWGroup("대상")]
        [SerializeField] private UnityEngine.Object value;
        [SerializeField] private bool matchInstanceName = true;
        [SerializeField] private bool ignoreCloneSuffix = true;
        #endregion // 필드

        #region 프로퍼티
        /// <inheritdoc />
        public override object Value => value;
        #endregion // 프로퍼티

        /// <inheritdoc />
        public override bool Matches(object target)
        {
            UnityEngine.Object expectedObject = ResolveUnityObject(value);
            UnityEngine.Object targetObject = ResolveUnityObject(target);
            if (expectedObject == null || targetObject == null)
            {
                return false;
            }

            if (expectedObject == targetObject)
            {
                return true;
            }

            if (!matchInstanceName)
            {
                return false;
            }

            string expectedName = NormalizeName(expectedObject.name);
            string targetName = NormalizeName(targetObject.name);
            return string.Equals(expectedName, targetName, StringComparison.Ordinal);
        }

        /// <summary>
        /// 컴포넌트이면 게임 오브젝트를 반환하고, 그 외 Unity 오브젝트는 그대로 반환합니다.
        /// </summary>
        private static UnityEngine.Object ResolveUnityObject(object target)
        {
            if (target is Component component)
            {
                return component.gameObject;
            }

            return target as UnityEngine.Object;
        }

        /// <summary>
        /// 필요하면 런타임 복제 이름의 접미사를 제거합니다.
        /// </summary>
        private string NormalizeName(string objectName)
        {
            const string CloneSuffix = "(Clone)";

            if (!ignoreCloneSuffix || string.IsNullOrEmpty(objectName)
                || !objectName.EndsWith(CloneSuffix, StringComparison.Ordinal))
            {
                return objectName;
            }

            return objectName.Substring(0, objectName.Length - CloneSuffix.Length).TrimEnd();
        }
    }
}
