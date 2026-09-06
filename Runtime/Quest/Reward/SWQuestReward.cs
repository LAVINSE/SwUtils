using UnityEngine;

using SW.Attributes;

using SW.Base;

namespace SW.Quest
{
    /// <summary>
    /// 에셋으로 구성하는 퀘스트 보상의 기본 클래스입니다.
    /// 프로젝트별 재화, 아이템 또는 경험치 지급 클래스가 이 타입을 상속합니다.
    /// </summary>
    public abstract class SWQuestReward : SWScriptableObject, ISWQuestReward
    {
        #region 필드
        [SWGroup("보상 정보")]
        [SerializeField] private Sprite icon;
        [SerializeField, TextArea] private string description;
        [SerializeField] private int quantity = 1;
        #endregion // 필드

        #region 프로퍼티
        /// <inheritdoc />
        public Sprite Icon => icon;
        /// <inheritdoc />
        public string Description => description;
        /// <inheritdoc />
        public int Quantity => quantity;
        #endregion // 프로퍼티

        /// <inheritdoc />
        public abstract void Grant(SWQuestSystem questSystem, SWQuest quest);
    }
}
