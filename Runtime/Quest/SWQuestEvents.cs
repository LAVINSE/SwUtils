namespace SW.Quest
{
    /// <summary>
    /// 퀘스트가 런타임 시스템에 등록되었음을 알리는 이벤트 데이터입니다.
    /// </summary>
    public readonly struct SWQuestRegisteredEvent
    {
        /// <summary>이벤트 데이터를 생성합니다.</summary>
        /// <param name="questSystem">이벤트를 발생시킨 퀘스트 시스템입니다.</param>
        /// <param name="quest">등록된 런타임 퀘스트입니다.</param>
        public SWQuestRegisteredEvent(SWQuestSystem questSystem, SWQuest quest)
        {
            QuestSystem = questSystem;
            Quest = quest;
        }

        /// <summary>이벤트를 발생시킨 퀘스트 시스템입니다.</summary>
        public SWQuestSystem QuestSystem { get; }
        /// <summary>등록된 런타임 퀘스트입니다.</summary>
        public SWQuest Quest { get; }
    }

    /// <summary>
    /// 일반 퀘스트가 완료되었음을 알리는 이벤트 데이터입니다.
    /// </summary>
    public readonly struct SWQuestCompletedEvent
    {
        /// <summary>이벤트 데이터를 생성합니다.</summary>
        /// <param name="questSystem">이벤트를 발생시킨 퀘스트 시스템입니다.</param>
        /// <param name="quest">완료된 런타임 퀘스트입니다.</param>
        public SWQuestCompletedEvent(SWQuestSystem questSystem, SWQuest quest)
        {
            QuestSystem = questSystem;
            Quest = quest;
        }

        /// <summary>이벤트를 발생시킨 퀘스트 시스템입니다.</summary>
        public SWQuestSystem QuestSystem { get; }
        /// <summary>완료된 런타임 퀘스트입니다.</summary>
        public SWQuest Quest { get; }
    }

    /// <summary>
    /// 일반 퀘스트가 취소되었음을 알리는 이벤트 데이터입니다.
    /// </summary>
    public readonly struct SWQuestCanceledEvent
    {
        /// <summary>이벤트 데이터를 생성합니다.</summary>
        /// <param name="questSystem">이벤트를 발생시킨 퀘스트 시스템입니다.</param>
        /// <param name="quest">취소된 런타임 퀘스트입니다.</param>
        public SWQuestCanceledEvent(SWQuestSystem questSystem, SWQuest quest)
        {
            QuestSystem = questSystem;
            Quest = quest;
        }

        /// <summary>이벤트를 발생시킨 퀘스트 시스템입니다.</summary>
        public SWQuestSystem QuestSystem { get; }
        /// <summary>취소된 런타임 퀘스트입니다.</summary>
        public SWQuest Quest { get; }
    }

    /// <summary>
    /// 업적이 달성되었음을 알리는 이벤트 데이터입니다.
    /// </summary>
    public readonly struct SWAchievementUnlockedEvent
    {
        /// <summary>이벤트 데이터를 생성합니다.</summary>
        /// <param name="questSystem">이벤트를 발생시킨 퀘스트 시스템입니다.</param>
        /// <param name="achievement">달성된 런타임 업적입니다.</param>
        public SWAchievementUnlockedEvent(SWQuestSystem questSystem, SWAchievement achievement)
        {
            QuestSystem = questSystem;
            Achievement = achievement;
        }

        /// <summary>이벤트를 발생시킨 퀘스트 시스템입니다.</summary>
        public SWQuestSystem QuestSystem { get; }
        /// <summary>달성된 런타임 업적입니다.</summary>
        public SWAchievement Achievement { get; }
    }

    /// <summary>
    /// 퀘스트 작업의 진행량이 변경되었음을 알리는 이벤트 데이터입니다.
    /// </summary>
    public readonly struct SWQuestTaskProgressChangedEvent
    {
        /// <summary>이벤트 데이터를 생성합니다.</summary>
        /// <param name="questSystem">이벤트를 발생시킨 퀘스트 시스템입니다.</param>
        /// <param name="quest">작업을 소유한 런타임 퀘스트입니다.</param>
        /// <param name="task">진행량이 변경된 작업입니다.</param>
        /// <param name="currentProgress">변경된 현재 진행량입니다.</param>
        /// <param name="previousProgress">변경 전 진행량입니다.</param>
        public SWQuestTaskProgressChangedEvent(SWQuestSystem questSystem, SWQuest quest,
            SWQuestTask task, int currentProgress, int previousProgress)
        {
            QuestSystem = questSystem;
            Quest = quest;
            Task = task;
            CurrentProgress = currentProgress;
            PreviousProgress = previousProgress;
        }

        /// <summary>이벤트를 발생시킨 퀘스트 시스템입니다.</summary>
        public SWQuestSystem QuestSystem { get; }
        /// <summary>진행량이 변경된 작업을 소유한 퀘스트입니다.</summary>
        public SWQuest Quest { get; }
        /// <summary>진행량이 변경된 작업입니다.</summary>
        public SWQuestTask Task { get; }
        /// <summary>변경된 현재 진행량입니다.</summary>
        public int CurrentProgress { get; }
        /// <summary>변경 전 진행량입니다.</summary>
        public int PreviousProgress { get; }
    }

    /// <summary>
    /// 퀘스트 보상이 지급되었음을 알리는 이벤트 데이터입니다.
    /// </summary>
    public readonly struct SWQuestRewardGrantedEvent
    {
        /// <summary>이벤트 데이터를 생성합니다.</summary>
        /// <param name="questSystem">이벤트를 발생시킨 퀘스트 시스템입니다.</param>
        /// <param name="quest">보상을 지급한 런타임 퀘스트입니다.</param>
        /// <param name="reward">지급된 보상 정의입니다.</param>
        public SWQuestRewardGrantedEvent(SWQuestSystem questSystem, SWQuest quest, SWQuestReward reward)
        {
            QuestSystem = questSystem;
            Quest = quest;
            Reward = reward;
        }

        /// <summary>이벤트를 발생시킨 퀘스트 시스템입니다.</summary>
        public SWQuestSystem QuestSystem { get; }
        /// <summary>보상을 지급한 퀘스트입니다.</summary>
        public SWQuest Quest { get; }
        /// <summary>지급된 보상 정의입니다.</summary>
        public SWQuestReward Reward { get; }
    }
}
