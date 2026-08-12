#region 다중 계층 상태
using System;
using SW.BehaviourTree;
using SW.StateMachine;
using SW.Util;
using UnityEngine;

namespace SW.Example
{
/// <summary>대기 중인 다중 계층 예제 상태입니다.</summary>
[SWStateMachineNodeCategory("Example/Layered States")]
public sealed class IdleState : ExampleLayeredState { }

/// <summary>이동 중인 다중 계층 예제 상태입니다.</summary>
[SWStateMachineNodeCategory("Example/Layered States")]
public sealed class MovingState : ExampleLayeredState { }

/// <summary>다중 계층 상태의 진입과 종료를 Console에 출력합니다.</summary>
public abstract class ExampleLayeredState : SWState<SWGraphAssetsExample>
{
    /// <summary>상태에 진입했음을 Console에 출력합니다.</summary>
    protected override void OnEnter()
    {
        SWLog.Log($"[Layered State Machine] Enter {GetType().Name}");
    }

    /// <summary>상태에서 나갔음을 Console에 출력합니다.</summary>
    protected override void OnExit()
    {
        SWLog.Log($"[Layered State Machine] Exit {GetType().Name}");
    }
}
#endregion // 다중 계층 상태

#region 스택 상태
/// <summary>게임이 진행 중인 스택 예제 상태입니다.</summary>
[SWStateMachineNodeCategory("Example/Stack States")]
public sealed class GameplayState : ExampleStackState { }

/// <summary>게임이 일시정지된 스택 예제 상태입니다.</summary>
[SWStateMachineNodeCategory("Example/Stack States")]
public sealed class PauseState : ExampleStackState { }

/// <summary>스택 상태의 생명주기를 Console에 출력합니다.</summary>
public abstract class ExampleStackState : SWStackState<SWGraphAssetsExample>
{
    /// <summary>상태가 스택에 추가되었음을 출력합니다.</summary>
    protected override void OnEnter()
    {
        SWLog.Log($"[Stack State Machine] Enter {GetType().Name}");
    }

    /// <summary>상태가 스택에서 제거되었음을 출력합니다.</summary>
    protected override void OnExit()
    {
        SWLog.Log($"[Stack State Machine] Exit {GetType().Name}");
    }
}
#endregion // 스택 상태

#region 전이 조건
/// <summary>Moving 상태로 전환할 조건을 평가합니다.</summary>
[SWStateMachineNodeCategory("Example/Conditions")]
public sealed class IsMovingCondition : SWStateMachineGraphCondition<SWGraphAssetsExample>
{
    /// <summary>이동 조건이 활성화되어 있으면 참을 반환합니다.</summary>
    public override bool Evaluate(SWGraphAssetsExample context) => context.IsMoving;
}

/// <summary>Idle 상태로 복귀할 조건을 평가합니다.</summary>
[SWStateMachineNodeCategory("Example/Conditions")]
public sealed class IsNotMovingCondition : SWStateMachineGraphCondition<SWGraphAssetsExample>
{
    /// <summary>이동 조건이 비활성화되어 있으면 참을 반환합니다.</summary>
    public override bool Evaluate(SWGraphAssetsExample context) => !context.IsMoving;
}

/// <summary>Pause 상태로 전환할 조건을 평가합니다.</summary>
[SWStateMachineNodeCategory("Example/Conditions")]
public sealed class IsPausedCondition : SWStateMachineGraphCondition<SWGraphAssetsExample>
{
    /// <summary>일시정지 조건이 활성화되어 있으면 참을 반환합니다.</summary>
    public override bool Evaluate(SWGraphAssetsExample context) => context.IsPaused;
}

/// <summary>Gameplay 상태로 복귀할 조건을 평가합니다.</summary>
[SWStateMachineNodeCategory("Example/Conditions")]
public sealed class IsNotPausedCondition : SWStateMachineGraphCondition<SWGraphAssetsExample>
{
    /// <summary>일시정지 조건이 비활성화되어 있으면 참을 반환합니다.</summary>
    public override bool Evaluate(SWGraphAssetsExample context) => !context.IsPaused;
}
#endregion // 전이 조건

#region Behaviour Tree 사용자 타입
/// <summary>Blackboard의 정수 값을 증가시키는 사용자 Action 노드 예제입니다.</summary>
[Serializable]
[SWBehaviourNodeCategory("Example/Actions")]
public sealed class IncrementBlackboardNode : SWBehaviourActionNode
{
    [SerializeField] private SWBehaviourNodeProperty<int> counter = new();
    [SerializeField]
    private SWBehaviourNodeProperty<int> amount = new()
    {
        FixedValue = 1,
    };

    /// <summary>Blackboard 값을 증가시키고 기록 성공 여부를 반환합니다.</summary>
    protected override SWBehaviourStatus OnUpdate(
        SWBehaviourContext context,
        SWBehaviourTreeAsset tree)
    {
        int changedValue = counter.GetValue(context) + amount.GetValue(context);
        return counter.SetValue(context, changedValue)
            ? SWBehaviourStatus.Success
            : SWBehaviourStatus.Failure;
    }
}

/// <summary>게임 오브젝트를 저장하는 사용자 정의 Blackboard Key 예제입니다.</summary>
[Serializable]
public sealed class GameObjectBlackboardEntry :
    SWBehaviourBlackboardEntry<GameObject>
{
}
#endregion // Behaviour Tree 사용자 타입
}
