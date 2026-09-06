namespace SW.Quest
{
    /// <summary>
    /// 퀘스트 작업의 진행 상태입니다.
    /// </summary>
    public enum SWQuestTaskState
    {
        /// <summary>아직 시작하지 않은 상태입니다.</summary>
        Inactive,
        /// <summary>진행 보고를 받을 수 있는 상태입니다.</summary>
        Running,
        /// <summary>필요 진행량을 채운 상태입니다.</summary>
        Completed
    }
}
