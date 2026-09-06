using UnityEngine;

namespace SW.Quest
{
    /// <summary>
    /// 양수 보고는 누적하고 0 이하 보고를 받으면 진행량을 0으로 초기화합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "SWQuestContinuousProgressAction", menuName = "SWUtils/Quest/Task Action/Continuous Progress")]
    public sealed class SWQuestContinuousProgressAction : SWQuestTaskAction
    {
        /// <inheritdoc />
        public override int Calculate(SWQuestTask task, int currentProgress, int reportAmount)
            => reportAmount > 0 ? AddWithoutOverflow(currentProgress, reportAmount) : 0;
    }
}
