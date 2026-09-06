using UnityEngine;

using SW.Util;

namespace SW.Quest
{
    /// <summary>
    /// 자동 완료, 저장 강제 사용과 취소 금지 규칙이 적용되는 업적입니다.
    /// 퀘스트 시스템의 진행 보고를 일반 퀘스트와 함께 받습니다.
    /// </summary>
    [CreateAssetMenu(fileName = "SWAchievement_", menuName = "SWUtils/Quest/Achievement")]
    public class SWAchievement : SWQuest
    {
        /// <inheritdoc />
        public override bool IsSavable => true;

        /// <inheritdoc />
        public override bool CanCancel => false;

        /// <inheritdoc />
        protected override bool CompleteAutomatically => true;

        /// <inheritdoc />
        public override bool Cancel()
        {
            SWLog.LogWarning($"[SWAchievement] 업적은 취소할 수 없습니다: {CodeName}");
            return false;
        }
    }
}
