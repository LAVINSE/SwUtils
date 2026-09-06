using System;
using UnityEngine;

using SW.Attributes;

namespace SW.Quest
{
    /// <summary>
    /// 문자열을 대소문자까지 정확하게 비교하는 퀘스트 대상입니다.
    /// </summary>
    [CreateAssetMenu(fileName = "SWQuestStringTarget_", menuName = "SWUtils/Quest/Target/String")]
    public sealed class SWQuestStringTarget : SWQuestTarget
    {
        #region 필드
        [SWGroup("대상")]
        [SerializeField] private string value;
        #endregion // 필드

        #region 프로퍼티
        /// <inheritdoc />
        public override object Value => value;
        #endregion // 프로퍼티

        /// <inheritdoc />
        public override bool Matches(object target)
            => target is string stringTarget && string.Equals(value, stringTarget, StringComparison.Ordinal);
    }
}
