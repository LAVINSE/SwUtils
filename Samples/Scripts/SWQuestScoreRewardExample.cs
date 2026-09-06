using UnityEngine;

using SW.Quest;

/// <summary>
/// 퀘스트 시스템의 외부 문맥을 이용해 점수를 지급하는 사용자 정의 보상 예제입니다.
/// </summary>
[CreateAssetMenu(fileName = "SWQuestScoreRewardExample_", menuName = "SWUtils/Samples/Quest Score Reward")]
public sealed class SWQuestScoreRewardExample : SWQuestReward
{
    /// <inheritdoc />
    public override void Grant(SWQuestSystem questSystem, SWQuest quest)
    {
        if (questSystem != null && questSystem.TryGetContext(out SWQuestExample example))
        {
            example.AddScore(Quantity);
        }
    }
}
