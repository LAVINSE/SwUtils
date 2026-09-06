namespace SW.Quest
{
    /// <summary>
    /// 퀘스트 런타임의 진행 상태입니다.
    /// </summary>
    public enum SWQuestState
    {
        /// <summary>아직 등록되지 않은 상태입니다.</summary>
        Inactive,
        /// <summary>현재 작업을 진행하는 상태입니다.</summary>
        Running,
        /// <summary>모든 작업을 끝내고 완료 확정을 기다리는 상태입니다.</summary>
        WaitingForCompletion,
        /// <summary>완료되어 보상 지급까지 끝난 상태입니다.</summary>
        Completed,
        /// <summary>취소된 상태입니다.</summary>
        Canceled
    }
}
