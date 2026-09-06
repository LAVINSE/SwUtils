using System;

namespace SW.Quest
{
    /// <summary>
    /// 한 작업의 진행 상태를 저장하는 데이터입니다.
    /// </summary>
    [Serializable]
    public sealed class SWQuestTaskSaveData
    {
        /// <summary>작업 코드명입니다.</summary>
        public string codeName;
        /// <summary>현재 진행량입니다.</summary>
        public int currentProgress;
        /// <summary>작업 상태입니다.</summary>
        public SWQuestTaskState state;
    }

    /// <summary>
    /// 한 작업 묶음의 진행 상태를 저장하는 데이터입니다.
    /// </summary>
    [Serializable]
    public sealed class SWQuestTaskGroupSaveData
    {
        /// <summary>작업 묶음 코드명입니다.</summary>
        public string codeName;
        /// <summary>작업 묶음 상태입니다.</summary>
        public SWQuestTaskGroupState state;
        /// <summary>묶음에 포함된 작업 저장 데이터입니다.</summary>
        public SWQuestTaskSaveData[] tasks = Array.Empty<SWQuestTaskSaveData>();
    }

    /// <summary>
    /// 한 퀘스트의 런타임 상태를 저장하는 데이터입니다.
    /// </summary>
    [Serializable]
    public sealed class SWQuestSaveData
    {
        /// <summary>퀘스트 정의 코드명입니다.</summary>
        public string codeName;
        /// <summary>퀘스트 상태입니다.</summary>
        public SWQuestState state;
        /// <summary>현재 작업 묶음 인덱스입니다.</summary>
        public int currentTaskGroupIndex;
        /// <summary>현재 작업 묶음 코드명입니다.</summary>
        public string currentTaskGroupCodeName;
        /// <summary>모든 작업 묶음의 진행 상태입니다.</summary>
        public SWQuestTaskGroupSaveData[] taskGroups = Array.Empty<SWQuestTaskGroupSaveData>();
    }

    /// <summary>
    /// 퀘스트 시스템 전체 상태를 저장하는 데이터입니다.
    /// 게임 저장 데이터의 필드로 포함하거나 <see cref="SWQuestSystem.Save"/>에 사용할 수 있습니다.
    /// </summary>
    [Serializable]
    public sealed class SWQuestSystemSaveData
    {
        /// <summary>저장 데이터 형식 버전입니다.</summary>
        public int version = 1;
        /// <summary>진행 중인 일반 퀘스트입니다.</summary>
        public SWQuestSaveData[] activeQuests = Array.Empty<SWQuestSaveData>();
        /// <summary>완료한 일반 퀘스트입니다.</summary>
        public SWQuestSaveData[] completedQuests = Array.Empty<SWQuestSaveData>();
        /// <summary>진행 중인 업적입니다.</summary>
        public SWQuestSaveData[] activeAchievements = Array.Empty<SWQuestSaveData>();
        /// <summary>달성한 업적입니다.</summary>
        public SWQuestSaveData[] completedAchievements = Array.Empty<SWQuestSaveData>();
    }
}
