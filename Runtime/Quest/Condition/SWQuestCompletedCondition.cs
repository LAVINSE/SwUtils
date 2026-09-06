using UnityEngine;

using SW.Attributes;

namespace SW.Quest
{
    /// <summary>
    /// 지정한 퀘스트 또는 업적이 완료되었는지 검사합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "SWQuestCompletedCondition_", menuName = "SWUtils/Quest/Condition/Quest Completed")]
    public sealed class SWQuestCompletedCondition : SWQuestCondition
    {
        #region 필드
        [SWGroup("완료 조건")]
        [SerializeField] private SWQuest targetQuest;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>완료 여부를 검사할 퀘스트 정의입니다.</summary>
        public SWQuest TargetQuest => targetQuest;
        #endregion // 프로퍼티

        /// <inheritdoc />
        public override bool IsMet(SWQuestSystem questSystem, SWQuest quest)
            => questSystem != null && targetQuest != null && questSystem.ContainsCompleted(targetQuest);
    }
}
