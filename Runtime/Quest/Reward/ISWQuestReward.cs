using UnityEngine;

namespace SW.Quest
{
    /// <summary>
    /// 퀘스트 완료 보상을 지급하는 계약입니다.
    /// </summary>
    public interface ISWQuestReward
    {
        /// <summary>보상 표시 아이콘입니다.</summary>
        Sprite Icon { get; }
        /// <summary>보상 설명입니다.</summary>
        string Description { get; }
        /// <summary>표시 및 지급에 사용할 보상 수량입니다.</summary>
        int Quantity { get; }

        /// <summary>
        /// 퀘스트 완료 보상을 지급합니다.
        /// </summary>
        /// <param name="questSystem">퀘스트를 관리하는 시스템입니다.</param>
        /// <param name="quest">완료된 퀘스트입니다.</param>
        void Grant(SWQuestSystem questSystem, SWQuest quest);
    }
}
