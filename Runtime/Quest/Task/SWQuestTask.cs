using System;
using System.Collections.Generic;
using UnityEngine;

using SW.Attributes;

using SW.Base;

using SW.Util;

namespace SW.Quest
{
    /// <summary>
    /// 카테고리와 대상이 일치하는 보고를 받아 진행량을 관리하는 퀘스트 작업입니다.
    /// </summary>
    [CreateAssetMenu(fileName = "SWQuestTask_", menuName = "SWUtils/Quest/Task")]
    public class SWQuestTask : SWIdentifiedObject
    {
        #region 이벤트
        /// <summary>
        /// 작업 상태가 변경될 때 호출되는 이벤트 처리자입니다.
        /// </summary>
        /// <param name="task">상태가 변경된 작업입니다.</param>
        /// <param name="currentState">변경된 현재 상태입니다.</param>
        /// <param name="previousState">변경 전 상태입니다.</param>
        public delegate void StateChangedHandler(SWQuestTask task, SWQuestTaskState currentState,
            SWQuestTaskState previousState);

        /// <summary>
        /// 작업 진행량이 변경될 때 호출되는 이벤트 처리자입니다.
        /// </summary>
        /// <param name="task">진행량이 변경된 작업입니다.</param>
        /// <param name="currentProgress">변경된 현재 진행량입니다.</param>
        /// <param name="previousProgress">변경 전 진행량입니다.</param>
        public delegate void ProgressChangedHandler(SWQuestTask task, int currentProgress,
            int previousProgress);
        #endregion // 이벤트

        #region 필드
        [SWGroup("진행 계산")]
        [Tooltip("비어 있으면 보고 변화량을 현재 진행량에 더합니다.")]
        [SerializeField] private SWQuestTaskAction progressAction;

        [SWGroup("대상")]
        [Tooltip("비어 있으면 카테고리만 일치하는 모든 보고를 받습니다.")]
        [SerializeField] private SWQuestTarget[] targets = Array.Empty<SWQuestTarget>();

        [SWGroup("설정")]
        [SerializeField] private SWQuestInitialProgressValue initialProgressValue;
        [SerializeField, Min(1)] private int requiredProgress = 1;
        [SerializeField] private bool receiveReportsAfterCompletion;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>이 런타임 작업의 원본 정의 에셋입니다.</summary>
        public SWQuestTask OriginTask { get; private set; }

        /// <summary>현재 작업을 소유한 런타임 퀘스트입니다.</summary>
        public SWQuest Owner { get; private set; }

        /// <summary>현재 작업 상태입니다.</summary>
        public SWQuestTaskState State { get; private set; }

        /// <summary>현재 진행량입니다.</summary>
        public int CurrentProgress { get; private set; }

        /// <summary>완료에 필요한 진행량입니다.</summary>
        public int RequiredProgress => Mathf.Max(1, requiredProgress);

        /// <summary>작업이 완료되었는지 여부입니다.</summary>
        public bool IsCompleted => State == SWQuestTaskState.Completed;

        /// <summary>작업 완료 후에도 같은 작업 묶음이 진행 중이면 보고를 받을지 여부입니다.</summary>
        public bool ReceiveReportsAfterCompletion => receiveReportsAfterCompletion;

        /// <summary>이 작업에 연결된 대상 목록입니다.</summary>
        public IReadOnlyList<SWQuestTarget> Targets => targets ?? Array.Empty<SWQuestTarget>();

        /// <summary>작업 상태가 변경될 때 발생합니다.</summary>
        public event StateChangedHandler StateChanged;

        /// <summary>작업 진행량이 변경될 때 발생합니다.</summary>
        public event ProgressChangedHandler ProgressChanged;
        #endregion // 프로퍼티

        #region 복사
        /// <inheritdoc />
        public override object Clone()
            => CreateRuntimeClone();

        /// <summary>
        /// 원본 정의 정보를 유지하는 런타임 작업 복제본을 생성합니다.
        /// </summary>
        /// <returns>초기 상태의 런타임 작업입니다.</returns>
        public SWQuestTask CreateRuntimeClone()
        {
            SWQuestTask clone = Instantiate(this);
            clone.name = name;
            clone.OriginTask = OriginTask != null ? OriginTask : this;
            clone.ResetRuntimeState();
            return clone;
        }
        #endregion // 복사

        #region 생명주기
        /// <summary>
        /// 런타임 작업을 소유 퀘스트에 연결합니다.
        /// </summary>
        /// <param name="owner">작업을 소유할 런타임 퀘스트입니다.</param>
        internal void Initialize(SWQuest owner)
        {
            Owner = owner;
            ResetRuntimeState();
        }

        /// <summary>
        /// 작업을 시작하고 선택적인 시작 진행량을 적용합니다.
        /// </summary>
        /// <param name="applyInitialProgress">시작 진행량 제공자를 실행할지 여부입니다.</param>
        internal void StartTask(bool applyInitialProgress)
        {
            SetState(SWQuestTaskState.Running);

            if (applyInitialProgress && initialProgressValue != null)
            {
                try
                {
                    SetProgress(initialProgressValue.GetValue(this));
                }
                catch (Exception exception)
                {
                    SWLog.LogError($"[SWQuestTask] 시작 진행량 계산 실패: {CodeName}, 오류: {exception.Message}");
                }
            }
        }

        /// <summary>
        /// 작업을 완료 상태로 변경합니다.
        /// </summary>
        /// <param name="notifyProgressChanged">진행량 변경 이벤트를 발생시킬지 여부입니다.</param>
        internal void Complete(bool notifyProgressChanged = true)
        {
            if (!notifyProgressChanged)
            {
                CurrentProgress = RequiredProgress;
                State = SWQuestTaskState.Completed;
                return;
            }

            if (State == SWQuestTaskState.Inactive)
            {
                SetState(SWQuestTaskState.Running);
            }

            SetProgress(RequiredProgress);
        }

        /// <summary>
        /// 저장 데이터에서 작업 상태를 복원합니다.
        /// </summary>
        /// <param name="saveData">복원할 작업 저장 데이터입니다.</param>
        internal void Restore(SWQuestTaskSaveData saveData)
        {
            if (saveData == null)
            {
                ResetRuntimeState();
                return;
            }

            CurrentProgress = Mathf.Clamp(saveData.currentProgress, 0, RequiredProgress);

            if (CurrentProgress >= RequiredProgress)
            {
                State = SWQuestTaskState.Completed;
            }
            else if (saveData.state == SWQuestTaskState.Inactive)
            {
                State = SWQuestTaskState.Inactive;
            }
            else
            {
                State = SWQuestTaskState.Running;
            }
        }

        /// <summary>
        /// 런타임 이벤트와 소유자 참조를 정리합니다.
        /// </summary>
        internal void ReleaseRuntime()
        {
            Owner = null;
            StateChanged = null;
            ProgressChanged = null;
        }
        #endregion // 생명주기

        #region 진행
        /// <summary>
        /// 보고가 현재 작업과 일치하면 진행량을 갱신합니다.
        /// </summary>
        /// <param name="report">반영할 진행 보고입니다.</param>
        /// <returns>진행량이 변경되었으면 <see langword="true"/>입니다.</returns>
        internal bool ApplyReport(SWQuestReport report)
        {
            if (!CanReceiveReport() || !MatchesReport(report))
            {
                return false;
            }

            int previousProgress = CurrentProgress;
            int calculatedProgress;

            try
            {
                ISWQuestTaskAction selectedAction = progressAction;
                calculatedProgress = selectedAction != null
                    ? selectedAction.Calculate(this, CurrentProgress, report.Amount)
                    : AddWithoutOverflow(CurrentProgress, report.Amount);
            }
            catch (Exception exception)
            {
                SWLog.LogError($"[SWQuestTask] 진행량 계산 실패: {CodeName}, 오류: {exception.Message}");
                return false;
            }

            SetProgress(calculatedProgress);
            return CurrentProgress != previousProgress;
        }

        /// <summary>
        /// 현재 진행량을 지정한 값으로 변경합니다.
        /// </summary>
        /// <param name="progress">설정할 진행량입니다.</param>
        public void SetProgress(int progress)
        {
            int previousProgress = CurrentProgress;
            CurrentProgress = Mathf.Clamp(progress, 0, RequiredProgress);

            if (State != SWQuestTaskState.Inactive)
            {
                SetState(CurrentProgress >= RequiredProgress
                    ? SWQuestTaskState.Completed
                    : SWQuestTaskState.Running);
            }

            if (CurrentProgress != previousProgress)
            {
                ProgressChanged?.Invoke(this, CurrentProgress, previousProgress);
            }
        }

        /// <summary>
        /// 지정한 보고의 카테고리와 대상이 현재 작업에 일치하는지 확인합니다.
        /// </summary>
        /// <param name="report">확인할 진행 보고입니다.</param>
        /// <returns>보고를 받을 수 있으면 <see langword="true"/>입니다.</returns>
        public bool MatchesReport(SWQuestReport report)
        {
            bool categoryMatches = Categories.Count == 0
                || HasCategory(report.CategoryCode);

            if (!categoryMatches)
            {
                return false;
            }

            if (targets == null || targets.Length == 0)
            {
                return true;
            }

            for (int index = 0; index < targets.Length; index++)
            {
                if (targets[index] != null && targets[index].Matches(report.Target))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 지정한 대상이 현재 작업 대상 목록에 포함되는지 확인합니다.
        /// </summary>
        /// <param name="target">확인할 대상입니다.</param>
        /// <returns>일치하는 대상이 있으면 <see langword="true"/>입니다.</returns>
        public bool ContainsTarget(object target)
        {
            if (targets == null)
            {
                return false;
            }

            for (int index = 0; index < targets.Length; index++)
            {
                if (targets[index] != null && targets[index].Matches(target))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 지정한 퀘스트 대상 에셋의 값이 현재 작업 대상 목록에 포함되는지 확인합니다.
        /// </summary>
        /// <param name="target">확인할 퀘스트 대상 에셋입니다.</param>
        /// <returns>일치하는 대상이 있으면 <see langword="true"/>입니다.</returns>
        public bool ContainsTarget(SWQuestTarget target)
            => target != null && ContainsTarget(target.Value);
        #endregion // 진행

        #region 저장
        /// <summary>
        /// 현재 작업 상태를 저장 데이터로 변환합니다.
        /// </summary>
        /// <returns>현재 작업 저장 데이터입니다.</returns>
        internal SWQuestTaskSaveData CreateSaveData()
        {
            return new SWQuestTaskSaveData
            {
                codeName = CodeName,
                currentProgress = CurrentProgress,
                state = State
            };
        }
        #endregion // 저장

        /// <summary>
        /// 현재 상태에서 진행 보고를 받을 수 있는지 확인합니다.
        /// </summary>
        private bool CanReceiveReport()
            => State == SWQuestTaskState.Running
                || State == SWQuestTaskState.Completed && receiveReportsAfterCompletion;

        /// <summary>
        /// 작업 상태를 변경하고 이벤트를 발생시킵니다.
        /// </summary>
        private void SetState(SWQuestTaskState state)
        {
            if (State == state)
            {
                return;
            }

            SWQuestTaskState previousState = State;
            State = state;
            StateChanged?.Invoke(this, State, previousState);
        }

        /// <summary>
        /// 런타임 전용 상태를 초기화합니다.
        /// </summary>
        private void ResetRuntimeState()
        {
            State = SWQuestTaskState.Inactive;
            CurrentProgress = 0;
            StateChanged = null;
            ProgressChanged = null;
        }

        /// <summary>
        /// 정수 범위를 넘지 않도록 두 값을 안전하게 더합니다.
        /// </summary>
        private static int AddWithoutOverflow(int left, int right)
        {
            long result = (long)left + right;
            if (result > int.MaxValue)
            {
                return int.MaxValue;
            }

            return result < int.MinValue ? int.MinValue : (int)result;
        }
    }
}
