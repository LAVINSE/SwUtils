namespace SW.Quest
{
    /// <summary>
    /// 퀘스트 수락 또는 취소 가능 여부를 판정하는 조건 계약입니다.
    /// </summary>
    public interface ISWQuestCondition
    {
        /// <summary>조건을 설명하는 문장입니다.</summary>
        string Description { get; }

        /// <summary>
        /// 현재 문맥에서 조건 충족 여부를 판정합니다.
        /// </summary>
        /// <param name="questSystem">퀘스트를 관리하는 시스템입니다.</param>
        /// <param name="quest">조건을 검사할 퀘스트입니다.</param>
        /// <returns>조건이 충족되었으면 <see langword="true"/>입니다.</returns>
        bool IsMet(SWQuestSystem questSystem, SWQuest quest);
    }
}
