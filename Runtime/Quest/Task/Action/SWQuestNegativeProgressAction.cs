using UnityEngine;

namespace SW.Quest
{
    /// <summary>
    /// 음수 보고만 현재 진행량에 더해 진행량을 감소시킵니다.
    /// </summary>
    [CreateAssetMenu(fileName = "SWQuestNegativeProgressAction", menuName = "SWUtils/Quest/Task Action/Add Negative Progress")]
    public sealed class SWQuestNegativeProgressAction : SWQuestTaskAction
    {
        /// <inheritdoc />
        public override int Calculate(SWQuestTask task, int currentProgress, int reportAmount)
            => reportAmount < 0 ? AddWithoutOverflow(currentProgress, reportAmount) : currentProgress;
    }
}
