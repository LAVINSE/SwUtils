namespace SW.Quest
{
    /// <summary>
    /// 퀘스트 작업 묶음의 진행 상태입니다.
    /// </summary>
    public enum SWQuestTaskGroupState
    {
        /// <summary>아직 시작하지 않은 상태입니다.</summary>
        Inactive,
        /// <summary>묶음에 포함된 작업을 진행하는 상태입니다.</summary>
        Running,
        /// <summary>묶음에 포함된 모든 작업을 끝낸 상태입니다.</summary>
        Completed
    }
}
