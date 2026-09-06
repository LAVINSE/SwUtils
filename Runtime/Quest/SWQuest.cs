using System;
using System.Collections.Generic;
using UnityEngine;

using SW.Attributes;

using SW.Base;

using SW.Util;

namespace SW.Quest
{
    /// <summary>
    /// 순서가 있는 작업 묶음, 조건과 보상을 조합하는 퀘스트 정의 및 런타임 객체입니다.
    /// </summary>
    [CreateAssetMenu(fileName = "SWQuest_", menuName = "SWUtils/Quest/Quest")]
    public class SWQuest : SWIdentifiedObject
    {
        #region 이벤트
        /// <summary>퀘스트 상태가 변경될 때 호출되는 이벤트 처리자입니다.</summary>
        /// <param name="quest">상태가 변경된 런타임 퀘스트입니다.</param>
        /// <param name="currentState">변경된 현재 상태입니다.</param>
        /// <param name="previousState">변경 전 상태입니다.</param>
        public delegate void StateChangedHandler(SWQuest quest, SWQuestState currentState,
            SWQuestState previousState);

        /// <summary>현재 작업 묶음이 변경될 때 호출되는 이벤트 처리자입니다.</summary>
        /// <param name="quest">작업 묶음이 변경된 런타임 퀘스트입니다.</param>
        /// <param name="currentTaskGroup">새로 시작한 작업 묶음입니다.</param>
        /// <param name="previousTaskGroup">이전에 완료한 작업 묶음입니다.</param>
        public delegate void TaskGroupChangedHandler(SWQuest quest, SWQuestTaskGroup currentTaskGroup,
            SWQuestTaskGroup previousTaskGroup);

        /// <summary>소속 작업의 진행량이 변경될 때 호출되는 이벤트 처리자입니다.</summary>
        /// <param name="quest">작업을 소유한 런타임 퀘스트입니다.</param>
        /// <param name="task">진행량이 변경된 작업입니다.</param>
        /// <param name="currentProgress">변경된 현재 진행량입니다.</param>
        /// <param name="previousProgress">변경 전 진행량입니다.</param>
        public delegate void TaskProgressChangedHandler(SWQuest quest, SWQuestTask task,
            int currentProgress, int previousProgress);

        /// <summary>퀘스트가 완료될 때 호출되는 이벤트 처리자입니다.</summary>
        /// <param name="quest">완료된 런타임 퀘스트입니다.</param>
        public delegate void CompletedHandler(SWQuest quest);

        /// <summary>퀘스트가 취소될 때 호출되는 이벤트 처리자입니다.</summary>
        /// <param name="quest">취소된 런타임 퀘스트입니다.</param>
        public delegate void CanceledHandler(SWQuest quest);

        /// <summary>퀘스트 보상이 지급된 뒤 호출되는 이벤트 처리자입니다.</summary>
        /// <param name="quest">보상을 지급한 런타임 퀘스트입니다.</param>
        /// <param name="reward">지급된 보상 정의입니다.</param>
        public delegate void RewardGrantedHandler(SWQuest quest, SWQuestReward reward);
        #endregion // 이벤트

        #region 필드
        [SWGroup("표시")]
        [SerializeField] private Sprite icon;

        [SWGroup("작업")]
        [SerializeField] private SWQuestTaskGroup[] taskGroups = Array.Empty<SWQuestTaskGroup>();

        [SWGroup("보상")]
        [SerializeField] private SWQuestReward[] rewards = Array.Empty<SWQuestReward>();

        [SWGroup("설정")]
        [SerializeField] private bool completeAutomatically;
        [SerializeField] private bool cancelable;
        [SerializeField] private bool savable = true;

        [SWGroup("조건")]
        [SerializeField] private SWQuestCondition[] acceptanceConditions = Array.Empty<SWQuestCondition>();
        [SerializeField] private SWQuestCondition[] cancellationConditions = Array.Empty<SWQuestCondition>();

        private int currentTaskGroupIndex;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>이 런타임 퀘스트의 원본 정의 에셋입니다.</summary>
        public SWQuest OriginQuest { get; private set; }

        /// <summary>현재 퀘스트를 관리하는 시스템입니다.</summary>
        public SWQuestSystem Owner { get; private set; }

        /// <summary>목록과 알림에 표시할 아이콘입니다.</summary>
        public Sprite Icon => icon;

        /// <summary>현재 퀘스트 상태입니다.</summary>
        public SWQuestState State { get; private set; }

        /// <summary>현재 진행 중인 작업 묶음의 인덱스입니다.</summary>
        public int CurrentTaskGroupIndex => currentTaskGroupIndex;

        /// <summary>현재 진행 중인 작업 묶음입니다.</summary>
        public SWQuestTaskGroup CurrentTaskGroup
            => taskGroups != null && currentTaskGroupIndex >= 0 && currentTaskGroupIndex < taskGroups.Length
                ? taskGroups[currentTaskGroupIndex]
                : null;

        /// <summary>퀘스트에 포함된 모든 작업 묶음입니다.</summary>
        public IReadOnlyList<SWQuestTaskGroup> TaskGroups
            => taskGroups ?? Array.Empty<SWQuestTaskGroup>();

        /// <summary>퀘스트 완료 시 지급하는 보상 목록입니다.</summary>
        public IReadOnlyList<SWQuestReward> Rewards
            => rewards ?? Array.Empty<SWQuestReward>();

        /// <summary>퀘스트가 런타임 시스템에 등록되었는지 여부입니다.</summary>
        public bool IsRegistered => Owner != null && State != SWQuestState.Canceled;

        /// <summary>퀘스트가 완료 확정을 기다리는지 여부입니다.</summary>
        public bool IsWaitingForCompletion => State == SWQuestState.WaitingForCompletion;

        /// <summary>퀘스트가 완료되었는지 여부입니다.</summary>
        public bool IsCompleted => State == SWQuestState.Completed;

        /// <summary>퀘스트가 취소되었는지 여부입니다.</summary>
        public bool IsCanceled => State == SWQuestState.Canceled;

        /// <summary>현재 퀘스트 상태를 저장할지 여부입니다.</summary>
        public virtual bool IsSavable => savable;

        /// <summary>모든 작업을 끝냈을 때 즉시 완료할지 여부입니다.</summary>
        protected virtual bool CompleteAutomatically => completeAutomatically;

        /// <summary>현재 상태와 취소 조건을 기준으로 취소할 수 있는지 여부입니다.</summary>
        public virtual bool CanCancel
            => cancelable && IsActiveState(State) && AreConditionsMet(cancellationConditions, Owner);

        /// <summary>퀘스트 상태가 변경될 때 발생합니다.</summary>
        public event StateChangedHandler StateChanged;

        /// <summary>현재 작업 묶음이 다음 묶음으로 변경될 때 발생합니다.</summary>
        public event TaskGroupChangedHandler TaskGroupChanged;

        /// <summary>소속 작업의 진행량이 변경될 때 발생합니다.</summary>
        public event TaskProgressChangedHandler TaskProgressChanged;

        /// <summary>퀘스트 완료와 보상 지급이 끝난 뒤 발생합니다.</summary>
        public event CompletedHandler Completed;

        /// <summary>퀘스트가 취소된 뒤 발생합니다.</summary>
        public event CanceledHandler Canceled;

        /// <summary>각 보상 지급이 끝난 뒤 발생합니다.</summary>
        public event RewardGrantedHandler RewardGranted;
        #endregion // 프로퍼티

        #region 복사
        /// <inheritdoc />
        public override object Clone()
            => CreateRuntimeClone();

        /// <summary>
        /// 작업까지 독립적으로 복제한 런타임 퀘스트를 생성합니다.
        /// </summary>
        /// <returns>초기 상태의 런타임 퀘스트입니다.</returns>
        public SWQuest CreateRuntimeClone()
        {
            SWQuest clone = Instantiate(this);
            clone.name = name;
            clone.OriginQuest = OriginQuest != null ? OriginQuest : this;

            int groupCount = taskGroups != null ? taskGroups.Length : 0;
            clone.taskGroups = new SWQuestTaskGroup[groupCount];
            for (int index = 0; index < groupCount; index++)
            {
                clone.taskGroups[index] = taskGroups[index]?.CreateRuntimeClone();
            }

            clone.ResetRuntimeState();
            return clone;
        }
        #endregion // 복사

        #region 조건
        /// <summary>
        /// 현재 시스템 문맥에서 퀘스트를 수락할 수 있는지 확인합니다.
        /// </summary>
        /// <param name="questSystem">퀘스트를 등록할 시스템입니다.</param>
        /// <returns>모든 수락 조건을 충족하면 <see langword="true"/>입니다.</returns>
        public bool IsAcceptable(SWQuestSystem questSystem)
            => questSystem != null && AreConditionsMet(acceptanceConditions, questSystem);

        /// <summary>
        /// 모든 조건을 검사하며 조건 실행 중 예외가 발생하면 충족하지 않은 것으로 처리합니다.
        /// </summary>
        private bool AreConditionsMet(SWQuestCondition[] conditions, SWQuestSystem questSystem)
        {
            if (conditions == null)
            {
                return true;
            }

            for (int index = 0; index < conditions.Length; index++)
            {
                SWQuestCondition conditionAsset = conditions[index];
                ISWQuestCondition condition = conditionAsset;
                if (condition == null)
                {
                    continue;
                }

                try
                {
                    if (!condition.IsMet(questSystem, this))
                    {
                        return false;
                    }
                }
                catch (Exception exception)
                {
                    SWLog.LogError($"[SWQuest] 조건 검사 실패: {CodeName}, 조건: {conditionAsset.name}, 오류: {exception.Message}");
                    return false;
                }
            }

            return true;
        }
        #endregion // 조건

        #region 생명주기
        /// <summary>
        /// 런타임 퀘스트와 작업을 시스템에 연결합니다.
        /// </summary>
        /// <param name="questSystem">퀘스트를 관리할 시스템입니다.</param>
        /// <returns>유효한 작업 구성이면 <see langword="true"/>입니다.</returns>
        internal bool PrepareRuntime(SWQuestSystem questSystem)
        {
            if (questSystem == null)
            {
                SWLog.LogError($"[SWQuest] 런타임 준비 실패: 퀘스트 시스템이 없습니다. 퀘스트: {CodeName}");
                return false;
            }

            if (!ValidateRuntimeConfiguration())
            {
                return false;
            }

            Owner = questSystem;
            currentTaskGroupIndex = 0;
            State = SWQuestState.Inactive;

            for (int groupIndex = 0; groupIndex < taskGroups.Length; groupIndex++)
            {
                SWQuestTaskGroup taskGroup = taskGroups[groupIndex];
                taskGroup.Initialize(this);

                IReadOnlyList<SWQuestTask> tasks = taskGroup.Tasks;
                for (int taskIndex = 0; taskIndex < tasks.Count; taskIndex++)
                {
                    if (tasks[taskIndex] != null)
                    {
                        tasks[taskIndex].ProgressChanged += HandleTaskProgressChanged;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// 첫 작업 묶음을 시작합니다.
        /// </summary>
        internal void Begin()
        {
            if (Owner == null || State != SWQuestState.Inactive)
            {
                return;
            }

            SetState(SWQuestState.Running);
            CurrentTaskGroup.StartGroup();
            AdvanceCompletedGroups();
        }

        /// <summary>
        /// 런타임 작업 복제본과 모든 이벤트 참조를 정리합니다.
        /// </summary>
        internal void ReleaseRuntime()
        {
            if (taskGroups != null)
            {
                for (int groupIndex = 0; groupIndex < taskGroups.Length; groupIndex++)
                {
                    SWQuestTaskGroup taskGroup = taskGroups[groupIndex];
                    if (taskGroup == null)
                    {
                        continue;
                    }

                    IReadOnlyList<SWQuestTask> tasks = taskGroup.Tasks;
                    for (int taskIndex = 0; taskIndex < tasks.Count; taskIndex++)
                    {
                        if (tasks[taskIndex] != null)
                        {
                            tasks[taskIndex].ProgressChanged -= HandleTaskProgressChanged;
                        }
                    }

                    taskGroup.ReleaseRuntime();
                }
            }

            Owner = null;
            StateChanged = null;
            TaskGroupChanged = null;
            TaskProgressChanged = null;
            Completed = null;
            Canceled = null;
            RewardGranted = null;
        }
        #endregion // 생명주기

        #region 진행
        /// <summary>
        /// 현재 작업 묶음에 진행 보고를 전달합니다.
        /// </summary>
        /// <param name="report">전달할 진행 보고입니다.</param>
        /// <returns>하나 이상의 작업 진행량이 변경되었으면 <see langword="true"/>입니다.</returns>
        public bool ReceiveReport(SWQuestReport report)
        {
            if (State != SWQuestState.Running || CurrentTaskGroup == null)
            {
                return false;
            }

            bool changed = CurrentTaskGroup.ApplyReport(report);
            if (changed)
            {
                AdvanceCompletedGroups();
            }

            return changed;
        }

        /// <summary>
        /// 완료 대기 중인 퀘스트의 완료와 보상 지급을 확정합니다.
        /// </summary>
        /// <returns>완료 처리했으면 <see langword="true"/>입니다.</returns>
        public bool Complete()
        {
            if (State != SWQuestState.WaitingForCompletion)
            {
                return false;
            }

            SetState(SWQuestState.Completed);
            GrantRewards();
            Completed?.Invoke(this);
            return true;
        }

        /// <summary>
        /// 남은 작업을 모두 완료하고 퀘스트 완료와 보상 지급을 확정합니다.
        /// </summary>
        /// <returns>완료 처리했으면 <see langword="true"/>입니다.</returns>
        public bool ForceComplete()
        {
            if (!IsActiveState(State))
            {
                return false;
            }

            for (int index = 0; index < taskGroups.Length; index++)
            {
                SWQuestTaskGroup taskGroup = taskGroups[index];
                if (taskGroup == null)
                {
                    continue;
                }

                if (taskGroup.State == SWQuestTaskGroupState.Inactive)
                {
                    taskGroup.StartGroup(false);
                }

                taskGroup.Complete();
            }

            currentTaskGroupIndex = taskGroups.Length - 1;
            SetState(SWQuestState.WaitingForCompletion);
            return Complete();
        }

        /// <summary>
        /// 취소 조건을 충족한 진행 중 퀘스트를 취소합니다.
        /// </summary>
        /// <returns>취소 처리했으면 <see langword="true"/>입니다.</returns>
        public virtual bool Cancel()
        {
            if (!CanCancel)
            {
                return false;
            }

            SetState(SWQuestState.Canceled);
            Canceled?.Invoke(this);
            return true;
        }

        /// <summary>
        /// 지정한 대상이 퀘스트의 어느 작업에든 포함되는지 확인합니다.
        /// </summary>
        /// <param name="target">확인할 대상입니다.</param>
        /// <returns>일치하는 작업이 있으면 <see langword="true"/>입니다.</returns>
        public bool ContainsTarget(object target)
        {
            if (taskGroups == null)
            {
                return false;
            }

            for (int index = 0; index < taskGroups.Length; index++)
            {
                if (taskGroups[index] != null && taskGroups[index].ContainsTarget(target))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 지정한 퀘스트 대상 에셋이 퀘스트의 어느 작업에든 포함되는지 확인합니다.
        /// </summary>
        /// <param name="target">확인할 퀘스트 대상 에셋입니다.</param>
        /// <returns>일치하는 작업이 있으면 <see langword="true"/>입니다.</returns>
        public bool ContainsTarget(SWQuestTarget target)
            => target != null && ContainsTarget(target.Value);

        /// <summary>
        /// 같은 정의 또는 같은 코드명을 사용하는 퀘스트인지 확인합니다.
        /// </summary>
        /// <param name="other">비교할 퀘스트입니다.</param>
        /// <returns>같은 정의이면 <see langword="true"/>입니다.</returns>
        public bool IsSameQuest(SWQuest other)
        {
            if (other == null || (this is SWAchievement) != (other is SWAchievement))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            SWQuest thisOrigin = OriginQuest != null ? OriginQuest : this;
            SWQuest otherOrigin = other.OriginQuest != null ? other.OriginQuest : other;
            if (ReferenceEquals(thisOrigin, otherOrigin))
            {
                return true;
            }

            return !string.IsNullOrEmpty(CodeName)
                && string.Equals(CodeName, other.CodeName, StringComparison.Ordinal);
        }

        /// <summary>
        /// 완료된 작업 묶음을 순서대로 넘기고 마지막 묶음이면 완료 대기로 전환합니다.
        /// </summary>
        private void AdvanceCompletedGroups()
        {
            while (State == SWQuestState.Running && CurrentTaskGroup != null
                && CurrentTaskGroup.AreAllTasksCompleted)
            {
                SWQuestTaskGroup previousTaskGroup = CurrentTaskGroup;
                previousTaskGroup.Complete();

                if (currentTaskGroupIndex >= taskGroups.Length - 1)
                {
                    SetState(SWQuestState.WaitingForCompletion);
                    if (CompleteAutomatically)
                    {
                        Complete();
                    }

                    return;
                }

                currentTaskGroupIndex++;
                CurrentTaskGroup.StartGroup();
                TaskGroupChanged?.Invoke(this, CurrentTaskGroup, previousTaskGroup);
            }
        }
        #endregion // 진행

        #region 저장
        /// <summary>
        /// 현재 퀘스트와 모든 작업 진행 상태를 저장 데이터로 변환합니다.
        /// </summary>
        /// <returns>현재 퀘스트 저장 데이터입니다.</returns>
        public SWQuestSaveData CreateSaveData()
        {
            int groupCount = taskGroups != null ? taskGroups.Length : 0;
            SWQuestTaskGroupSaveData[] groupSaveData = new SWQuestTaskGroupSaveData[groupCount];

            for (int index = 0; index < groupCount; index++)
            {
                groupSaveData[index] = taskGroups[index]?.CreateSaveData();
            }

            return new SWQuestSaveData
            {
                codeName = CodeName,
                state = State,
                currentTaskGroupIndex = currentTaskGroupIndex,
                currentTaskGroupCodeName = CurrentTaskGroup?.CodeName,
                taskGroups = groupSaveData
            };
        }

        /// <summary>
        /// 준비된 런타임 퀘스트에 저장 상태를 적용합니다. 보상은 다시 지급하지 않습니다.
        /// </summary>
        /// <param name="saveData">적용할 저장 데이터입니다.</param>
        internal void Restore(SWQuestSaveData saveData)
        {
            if (saveData == null)
            {
                return;
            }

            int savedGroupCount = saveData.taskGroups != null ? saveData.taskGroups.Length : 0;
            HashSet<int> restoredGroupIndexes = new();
            for (int savedGroupIndex = 0; savedGroupIndex < savedGroupCount; savedGroupIndex++)
            {
                SWQuestTaskGroupSaveData groupSaveData = saveData.taskGroups[savedGroupIndex];
                if (groupSaveData == null)
                {
                    continue;
                }

                int groupIndex = FindTaskGroupIndex(groupSaveData.codeName, restoredGroupIndexes);
                if (groupIndex < 0 && string.IsNullOrEmpty(groupSaveData.codeName)
                    && savedGroupIndex < taskGroups.Length
                    && !restoredGroupIndexes.Contains(savedGroupIndex))
                {
                    groupIndex = savedGroupIndex;
                }

                if (groupIndex < 0)
                {
                    SWLog.LogWarning($"[SWQuest] 저장된 작업 묶음을 현재 정의에서 찾지 못했습니다. 퀘스트: {CodeName}, 묶음: {groupSaveData.codeName}");
                    continue;
                }

                taskGroups[groupIndex]?.Restore(groupSaveData);
                restoredGroupIndexes.Add(groupIndex);
            }

            int savedCurrentGroupIndex = FindTaskGroupIndex(saveData.currentTaskGroupCodeName, null);
            currentTaskGroupIndex = savedCurrentGroupIndex >= 0
                ? savedCurrentGroupIndex
                : Mathf.Clamp(saveData.currentTaskGroupIndex, 0, taskGroups.Length - 1);
            State = saveData.state == SWQuestState.Completed
                ? SWQuestState.Completed
                : saveData.state == SWQuestState.WaitingForCompletion
                    ? SWQuestState.WaitingForCompletion
                    : SWQuestState.Running;

            NormalizeRestoredGroupStates();
        }

        /// <summary>
        /// 아직 복원하지 않은 현재 작업 묶음 중 코드명이 같은 묶음의 인덱스를 반환합니다.
        /// </summary>
        /// <param name="codeName">저장된 작업 묶음 코드명입니다.</param>
        /// <param name="restoredGroupIndexes">이미 복원한 현재 묶음 인덱스이며, 제한하지 않으면 <see langword="null"/>입니다.</param>
        /// <returns>일치하는 작업 묶음 인덱스이며, 없으면 <c>-1</c>입니다.</returns>
        private int FindTaskGroupIndex(string codeName, ISet<int> restoredGroupIndexes)
        {
            if (string.IsNullOrEmpty(codeName) || taskGroups == null)
            {
                return -1;
            }

            for (int index = 0; index < taskGroups.Length; index++)
            {
                if ((restoredGroupIndexes == null || !restoredGroupIndexes.Contains(index))
                    && taskGroups[index] != null
                    && string.Equals(taskGroups[index].CodeName, codeName, StringComparison.Ordinal))
                {
                    return index;
                }
            }

            return -1;
        }

        /// <summary>
        /// 현재 퀘스트 상태에 맞도록 복원된 작업 묶음 상태를 보정합니다.
        /// </summary>
        private void NormalizeRestoredGroupStates()
        {
            if (State == SWQuestState.Completed)
            {
                currentTaskGroupIndex = taskGroups.Length - 1;
                for (int index = 0; index < taskGroups.Length; index++)
                {
                    taskGroups[index]?.Complete(false);
                }

                return;
            }

            for (int index = 0; index < taskGroups.Length; index++)
            {
                SWQuestTaskGroup taskGroup = taskGroups[index];
                if (taskGroup == null)
                {
                    continue;
                }

                if (index < currentTaskGroupIndex && !taskGroup.AreAllTasksCompleted)
                {
                    taskGroup.Complete(false);
                }
                else if (index == currentTaskGroupIndex
                    && State != SWQuestState.Completed
                    && taskGroup.State == SWQuestTaskGroupState.Inactive)
                {
                    taskGroup.StartGroup(false);
                }
            }

            if (State == SWQuestState.WaitingForCompletion)
            {
                for (int index = 0; index < taskGroups.Length; index++)
                {
                    if (taskGroups[index] != null && !taskGroups[index].AreAllTasksCompleted)
                    {
                        currentTaskGroupIndex = index;
                        if (taskGroups[index].State == SWQuestTaskGroupState.Inactive)
                        {
                            taskGroups[index].StartGroup(false);
                        }

                        State = SWQuestState.Running;
                        return;
                    }
                }

                currentTaskGroupIndex = taskGroups.Length - 1;
            }

            if (State == SWQuestState.Running && CurrentTaskGroup != null
                && CurrentTaskGroup.AreAllTasksCompleted)
            {
                while (CurrentTaskGroup != null && CurrentTaskGroup.AreAllTasksCompleted)
                {
                    CurrentTaskGroup.Complete(false);
                    if (currentTaskGroupIndex >= taskGroups.Length - 1)
                    {
                        State = SWQuestState.WaitingForCompletion;
                        return;
                    }

                    currentTaskGroupIndex++;
                    if (CurrentTaskGroup.State == SWQuestTaskGroupState.Inactive)
                    {
                        CurrentTaskGroup.StartGroup(false);
                    }
                }
            }
        }
        #endregion // 저장

        #region 내부 처리
        /// <summary>
        /// 퀘스트 정의가 런타임에서 진행 가능한 구성인지 확인합니다.
        /// </summary>
        private bool ValidateRuntimeConfiguration()
        {
            if (string.IsNullOrWhiteSpace(CodeName))
            {
                SWLog.LogError($"[SWQuest] 런타임 준비 실패: 코드명이 비어 있습니다. 에셋: {name}");
                return false;
            }

            if (taskGroups == null || taskGroups.Length == 0)
            {
                SWLog.LogError($"[SWQuest] 런타임 준비 실패: 작업 묶음이 없습니다. 퀘스트: {CodeName}");
                return false;
            }

            HashSet<string> taskGroupCodeNames = new(StringComparer.Ordinal);
            for (int index = 0; index < taskGroups.Length; index++)
            {
                if (taskGroups[index] == null || taskGroups[index].Tasks.Count == 0)
                {
                    SWLog.LogError($"[SWQuest] 런타임 준비 실패: 비어 있는 작업 묶음이 있습니다. 퀘스트: {CodeName}, 인덱스: {index}");
                    return false;
                }

                string taskGroupCodeName = taskGroups[index].CodeName;
                if (string.IsNullOrWhiteSpace(taskGroupCodeName))
                {
                    SWLog.LogError($"[SWQuest] 런타임 준비 실패: 작업 묶음 코드명이 비어 있습니다. 퀘스트: {CodeName}, 묶음: {index}");
                    return false;
                }

                if (!taskGroupCodeNames.Add(taskGroupCodeName))
                {
                    SWLog.LogError($"[SWQuest] 런타임 준비 실패: 작업 묶음 코드명이 중복됩니다. 퀘스트: {CodeName}, 묶음: {taskGroupCodeName}");
                    return false;
                }

                IReadOnlyList<SWQuestTask> tasks = taskGroups[index].Tasks;
                HashSet<string> taskCodeNames = new(StringComparer.Ordinal);
                for (int taskIndex = 0; taskIndex < tasks.Count; taskIndex++)
                {
                    if (tasks[taskIndex] == null)
                    {
                        SWLog.LogError($"[SWQuest] 런타임 준비 실패: 비어 있는 작업이 있습니다. 퀘스트: {CodeName}, 묶음: {index}");
                        return false;
                    }

                    string taskCodeName = tasks[taskIndex].CodeName;
                    if (string.IsNullOrWhiteSpace(taskCodeName))
                    {
                        SWLog.LogError($"[SWQuest] 런타임 준비 실패: 작업 코드명이 비어 있습니다. 퀘스트: {CodeName}, 묶음: {index}, 작업: {taskIndex}");
                        return false;
                    }

                    if (!taskCodeNames.Add(taskCodeName))
                    {
                        SWLog.LogError($"[SWQuest] 런타임 준비 실패: 한 작업 묶음 안의 코드명이 중복됩니다. 퀘스트: {CodeName}, 묶음: {index}, 작업: {taskCodeName}");
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// 작업 진행 변경을 퀘스트 이벤트로 전달합니다.
        /// </summary>
        private void HandleTaskProgressChanged(SWQuestTask task, int currentProgress, int previousProgress)
        {
            TaskProgressChanged?.Invoke(this, task, currentProgress, previousProgress);
        }

        /// <summary>
        /// 모든 보상을 독립적으로 지급하여 한 보상의 예외가 다음 보상을 막지 않도록 합니다.
        /// </summary>
        private void GrantRewards()
        {
            if (rewards == null)
            {
                return;
            }

            for (int index = 0; index < rewards.Length; index++)
            {
                SWQuestReward rewardAsset = rewards[index];
                ISWQuestReward reward = rewardAsset;
                if (reward == null)
                {
                    continue;
                }

                try
                {
                    reward.Grant(Owner, this);
                    RewardGranted?.Invoke(this, rewardAsset);
                }
                catch (Exception exception)
                {
                    SWLog.LogError($"[SWQuest] 보상 지급 실패: {CodeName}, 보상: {rewardAsset.name}, 오류: {exception.Message}");
                }
            }
        }

        /// <summary>
        /// 퀘스트 상태를 변경하고 이벤트를 발생시킵니다.
        /// </summary>
        private void SetState(SWQuestState state)
        {
            if (State == state)
            {
                return;
            }

            SWQuestState previousState = State;
            State = state;
            StateChanged?.Invoke(this, State, previousState);
        }

        /// <summary>
        /// 진행 또는 완료 대기 상태인지 확인합니다.
        /// </summary>
        private static bool IsActiveState(SWQuestState state)
            => state == SWQuestState.Running || state == SWQuestState.WaitingForCompletion;

        /// <summary>
        /// 런타임 전용 상태와 이벤트를 초기화합니다.
        /// </summary>
        private void ResetRuntimeState()
        {
            Owner = null;
            State = SWQuestState.Inactive;
            currentTaskGroupIndex = 0;
            StateChanged = null;
            TaskGroupChanged = null;
            TaskProgressChanged = null;
            Completed = null;
            Canceled = null;
            RewardGranted = null;
        }
        #endregion // 내부 처리
    }
}
