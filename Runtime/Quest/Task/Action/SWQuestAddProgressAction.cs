using UnityEngine;

namespace SW.Quest
{
    /// <summary>
    /// 보고 변화량을 현재 진행량에 더합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "SWQuestAddProgressAction", menuName = "SWUtils/Quest/Task Action/Add Progress")]
    public sealed class SWQuestAddProgressAction : SWQuestTaskAction
    {
        /// <inheritdoc />
        public override int Calculate(SWQuestTask task, int currentProgress, int reportAmount)
            => AddWithoutOverflow(currentProgress, reportAmount);
    }
}
