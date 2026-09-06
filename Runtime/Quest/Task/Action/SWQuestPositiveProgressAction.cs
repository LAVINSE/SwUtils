using UnityEngine;

namespace SW.Quest
{
    /// <summary>
    /// 양수 보고만 현재 진행량에 더합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "SWQuestPositiveProgressAction", menuName = "SWUtils/Quest/Task Action/Add Positive Progress")]
    public sealed class SWQuestPositiveProgressAction : SWQuestTaskAction
    {
        /// <inheritdoc />
        public override int Calculate(SWQuestTask task, int currentProgress, int reportAmount)
            => reportAmount > 0 ? AddWithoutOverflow(currentProgress, reportAmount) : currentProgress;
    }
}
