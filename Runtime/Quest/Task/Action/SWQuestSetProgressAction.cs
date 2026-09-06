using UnityEngine;

namespace SW.Quest
{
    /// <summary>
    /// 현재 진행량을 보고 변화량으로 교체합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "SWQuestSetProgressAction", menuName = "SWUtils/Quest/Task Action/Set Progress")]
    public sealed class SWQuestSetProgressAction : SWQuestTaskAction
    {
        /// <inheritdoc />
        public override int Calculate(SWQuestTask task, int currentProgress, int reportAmount)
            => reportAmount;
    }
}
