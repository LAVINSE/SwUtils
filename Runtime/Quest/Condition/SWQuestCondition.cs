using UnityEngine;

using SW.Attributes;

using SW.Base;

namespace SW.Quest
{
    /// <summary>
    /// 에셋으로 구성하는 퀘스트 조건의 기본 클래스입니다.
    /// </summary>
    public abstract class SWQuestCondition : SWScriptableObject, ISWQuestCondition
    {
        #region 필드
        [SWGroup("조건 정보")]
        [SerializeField, TextArea] private string description;
        #endregion // 필드

        #region 프로퍼티
        /// <inheritdoc />
        public string Description => description;
        #endregion // 프로퍼티

        /// <inheritdoc />
        public abstract bool IsMet(SWQuestSystem questSystem, SWQuest quest);
    }
}
