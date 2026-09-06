using System;
using System.Collections.Generic;
using UnityEngine;

using SW.Util;

namespace SW.Quest
{
    /// <summary>
    /// 동시에 진행하는 퀘스트 작업 목록입니다. 퀘스트는 작업 묶음을 순서대로 진행합니다.
    /// </summary>
    [Serializable]
    public sealed class SWQuestTaskGroup
    {
        #region 필드
        [Tooltip("퀘스트 안에서 고유해야 하며 저장 복원 시 묶음을 식별합니다.")]
        [SerializeField] private string codeName;
        [SerializeField] private SWQuestTask[] tasks = Array.Empty<SWQuestTask>();
        #endregion // 필드

        #region 프로퍼티
        /// <summary>저장 복원과 정의 검증에 사용하는 작업 묶음 코드명입니다.</summary>
        public string CodeName => codeName;

        /// <summary>이 작업 묶음을 소유한 런타임 퀘스트입니다.</summary>
        public SWQuest Owner { get; private set; }

        /// <summary>현재 작업 묶음 상태입니다.</summary>
        public SWQuestTaskGroupState State { get; private set; }

        /// <summary>작업 묶음에 포함된 런타임 작업 목록입니다.</summary>
        public IReadOnlyList<SWQuestTask> Tasks => tasks ?? Array.Empty<SWQuestTask>();

        /// <summary>모든 작업이 완료되었는지 여부입니다.</summary>
        public bool AreAllTasksCompleted
        {
            get
            {
                if (tasks == null || tasks.Length == 0)
                {
                    return false;
                }

                for (int index = 0; index < tasks.Length; index++)
                {
                    if (tasks[index] == null || !tasks[index].IsCompleted)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        /// <summary>작업 묶음이 완료되었는지 여부입니다.</summary>
        public bool IsCompleted => State == SWQuestTaskGroupState.Completed;
        #endregion // 프로퍼티

        #region 복사
        /// <summary>
        /// 작업 정의를 각각 복제한 런타임 작업 묶음을 생성합니다.
        /// </summary>
        /// <returns>런타임 작업 묶음입니다.</returns>
        internal SWQuestTaskGroup CreateRuntimeClone()
        {
            int taskCount = tasks != null ? tasks.Length : 0;
            SWQuestTask[] runtimeTasks = new SWQuestTask[taskCount];

            for (int index = 0; index < taskCount; index++)
            {
                if (tasks[index] != null)
                {
                    runtimeTasks[index] = tasks[index].CreateRuntimeClone();
                }
            }

            return new SWQuestTaskGroup
            {
                codeName = codeName,
                tasks = runtimeTasks
            };
        }
        #endregion // 복사

        #region 생명주기
        /// <summary>
        /// 런타임 작업 묶음과 작업들을 소유 퀘스트에 연결합니다.
        /// </summary>
        /// <param name="owner">작업 묶음을 소유할 런타임 퀘스트입니다.</param>
        internal void Initialize(SWQuest owner)
        {
            Owner = owner;
            State = SWQuestTaskGroupState.Inactive;

            if (tasks == null)
            {
                tasks = Array.Empty<SWQuestTask>();
                return;
            }

            for (int index = 0; index < tasks.Length; index++)
            {
                tasks[index]?.Initialize(owner);
            }
        }

        /// <summary>
        /// 묶음의 모든 작업을 시작합니다.
        /// </summary>
        /// <param name="applyInitialProgress">각 작업의 시작 진행량을 적용할지 여부입니다.</param>
        internal void StartGroup(bool applyInitialProgress = true)
        {
            State = SWQuestTaskGroupState.Running;

            if (tasks == null)
            {
                return;
            }

            for (int index = 0; index < tasks.Length; index++)
            {
                if (tasks[index] != null && !tasks[index].IsCompleted)
                {
                    tasks[index].StartTask(applyInitialProgress);
                }
            }
        }

        /// <summary>
        /// 묶음의 모든 작업을 강제로 완료합니다.
        /// </summary>
        /// <param name="notifyProgressChanged">작업 진행량 변경 이벤트를 발생시킬지 여부입니다.</param>
        internal void Complete(bool notifyProgressChanged = true)
        {
            if (tasks != null)
            {
                for (int index = 0; index < tasks.Length; index++)
                {
                    tasks[index]?.Complete(notifyProgressChanged);
                }
            }

            State = SWQuestTaskGroupState.Completed;
        }

        /// <summary>
        /// 런타임 작업 복제본과 이벤트를 정리합니다.
        /// </summary>
        internal void ReleaseRuntime()
        {
            Owner = null;

            if (tasks == null)
            {
                return;
            }

            for (int index = 0; index < tasks.Length; index++)
            {
                SWQuestTask task = tasks[index];
                if (task == null)
                {
                    continue;
                }

                task.ReleaseRuntime();
                if (Application.isPlaying)
                {
                    UnityEngine.Object.Destroy(task);
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(task);
                }
            }
        }
        #endregion // 생명주기

        #region 진행
        /// <summary>
        /// 묶음의 모든 작업에 진행 보고를 전달합니다.
        /// </summary>
        /// <param name="report">전달할 진행 보고입니다.</param>
        /// <returns>하나 이상의 작업 진행량이 변경되었으면 <see langword="true"/>입니다.</returns>
        internal bool ApplyReport(SWQuestReport report)
        {
            if (State != SWQuestTaskGroupState.Running || tasks == null)
            {
                return false;
            }

            bool changed = false;
            for (int index = 0; index < tasks.Length; index++)
            {
                if (tasks[index] != null && tasks[index].ApplyReport(report))
                {
                    changed = true;
                }
            }

            return changed;
        }

        /// <summary>
        /// 지정한 대상과 일치하는 첫 번째 작업을 반환합니다.
        /// </summary>
        /// <param name="target">찾을 대상입니다.</param>
        /// <returns>일치하는 작업이며, 없으면 <see langword="null"/>입니다.</returns>
        public SWQuestTask FindTaskByTarget(object target)
        {
            if (tasks == null)
            {
                return null;
            }

            for (int index = 0; index < tasks.Length; index++)
            {
                if (tasks[index] != null && tasks[index].ContainsTarget(target))
                {
                    return tasks[index];
                }
            }

            return null;
        }

        /// <summary>
        /// 지정한 퀘스트 대상 에셋과 일치하는 첫 번째 작업을 반환합니다.
        /// </summary>
        /// <param name="target">찾을 퀘스트 대상 에셋입니다.</param>
        /// <returns>일치하는 작업이며, 없으면 <see langword="null"/>입니다.</returns>
        public SWQuestTask FindTaskByTarget(SWQuestTarget target)
            => target != null ? FindTaskByTarget(target.Value) : null;

        /// <summary>
        /// 지정한 대상이 작업 묶음에 포함되는지 확인합니다.
        /// </summary>
        /// <param name="target">확인할 대상입니다.</param>
        /// <returns>일치하는 작업이 있으면 <see langword="true"/>입니다.</returns>
        public bool ContainsTarget(object target)
            => FindTaskByTarget(target) != null;

        /// <summary>
        /// 지정한 퀘스트 대상 에셋이 작업 묶음에 포함되는지 확인합니다.
        /// </summary>
        /// <param name="target">확인할 퀘스트 대상 에셋입니다.</param>
        /// <returns>일치하는 작업이 있으면 <see langword="true"/>입니다.</returns>
        public bool ContainsTarget(SWQuestTarget target)
            => target != null && ContainsTarget(target.Value);
        #endregion // 진행

        #region 저장
        /// <summary>
        /// 현재 작업 묶음을 저장 데이터로 변환합니다.
        /// </summary>
        /// <returns>현재 작업 묶음 저장 데이터입니다.</returns>
        internal SWQuestTaskGroupSaveData CreateSaveData()
        {
            int taskCount = tasks != null ? tasks.Length : 0;
            SWQuestTaskSaveData[] taskSaveData = new SWQuestTaskSaveData[taskCount];

            for (int index = 0; index < taskCount; index++)
            {
                taskSaveData[index] = tasks[index]?.CreateSaveData();
            }

            return new SWQuestTaskGroupSaveData
            {
                codeName = CodeName,
                state = State,
                tasks = taskSaveData
            };
        }

        /// <summary>
        /// 저장 데이터에서 작업 묶음과 각 작업의 상태를 복원합니다.
        /// </summary>
        /// <param name="saveData">복원할 작업 묶음 데이터입니다.</param>
        internal void Restore(SWQuestTaskGroupSaveData saveData)
        {
            if (saveData == null)
            {
                State = SWQuestTaskGroupState.Inactive;
                return;
            }

            State = saveData.state == SWQuestTaskGroupState.Completed
                ? SWQuestTaskGroupState.Completed
                : saveData.state == SWQuestTaskGroupState.Running
                    ? SWQuestTaskGroupState.Running
                    : SWQuestTaskGroupState.Inactive;
            int savedTaskCount = saveData.tasks != null ? saveData.tasks.Length : 0;
            int taskCount = tasks != null ? tasks.Length : 0;

            if (State == SWQuestTaskGroupState.Running)
            {
                for (int index = 0; index < taskCount; index++)
                {
                    tasks[index]?.StartTask(false);
                }
            }
            else if (State == SWQuestTaskGroupState.Completed)
            {
                Complete(false);
            }

            HashSet<int> restoredTaskIndexes = new();
            for (int savedTaskIndex = 0; savedTaskIndex < savedTaskCount; savedTaskIndex++)
            {
                SWQuestTaskSaveData taskSaveData = saveData.tasks[savedTaskIndex];
                if (taskSaveData == null)
                {
                    continue;
                }

                int taskIndex = FindTaskIndex(taskSaveData.codeName, restoredTaskIndexes);
                if (taskIndex < 0 && string.IsNullOrEmpty(taskSaveData.codeName)
                    && savedTaskIndex < taskCount && !restoredTaskIndexes.Contains(savedTaskIndex))
                {
                    taskIndex = savedTaskIndex;
                }

                if (taskIndex < 0)
                {
                    SWLog.LogWarning($"[SWQuestTaskGroup] 저장된 작업을 현재 정의에서 찾지 못했습니다: {taskSaveData.codeName}");
                    continue;
                }

                SWQuestTask task = tasks[taskIndex];
                task.Restore(taskSaveData);
                restoredTaskIndexes.Add(taskIndex);
            }

            if (State == SWQuestTaskGroupState.Completed)
            {
                Complete(false);
            }
            else if (State == SWQuestTaskGroupState.Running)
            {
                for (int index = 0; index < taskCount; index++)
                {
                    if (tasks[index] != null && tasks[index].State == SWQuestTaskState.Inactive)
                    {
                        tasks[index].StartTask(false);
                    }
                }
            }
        }

        /// <summary>
        /// 아직 복원하지 않은 현재 작업 중 코드명이 같은 작업의 인덱스를 반환합니다.
        /// </summary>
        /// <param name="codeName">저장된 작업 코드명입니다.</param>
        /// <param name="restoredTaskIndexes">이미 복원한 현재 작업 인덱스입니다.</param>
        /// <returns>일치하는 작업 인덱스이며, 없으면 <c>-1</c>입니다.</returns>
        private int FindTaskIndex(string codeName, ISet<int> restoredTaskIndexes)
        {
            if (string.IsNullOrEmpty(codeName) || tasks == null)
            {
                return -1;
            }

            for (int index = 0; index < tasks.Length; index++)
            {
                if (!restoredTaskIndexes.Contains(index) && tasks[index] != null
                    && string.Equals(tasks[index].CodeName, codeName, StringComparison.Ordinal))
                {
                    return index;
                }
            }

            return -1;
        }
        #endregion // 저장
    }
}
