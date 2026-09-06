namespace SW.Quest
{
    /// <summary>
    /// 진행 보고를 작업 진행량으로 변환하는 계산 계약입니다.
    /// </summary>
    public interface ISWQuestTaskAction
    {
        /// <summary>
        /// 보고를 반영한 새로운 진행량을 계산합니다.
        /// </summary>
        /// <param name="task">진행량을 계산할 작업입니다.</param>
        /// <param name="currentProgress">현재 진행량입니다.</param>
        /// <param name="reportAmount">보고에 포함된 변화량입니다.</param>
        /// <returns>계산된 진행량입니다.</returns>
        int Calculate(SWQuestTask task, int currentProgress, int reportAmount);
    }
}
