using UnityEngine;

using SW.Quest;

/// <summary>
/// 퀘스트 시스템 초기화, 진행 보고와 완료 이벤트 구독 예제입니다.
/// </summary>
public sealed class SWQuestExample : MonoBehaviour
{
    #region 필드
    [SerializeField] private SWQuestSystem questSystem;
    [SerializeField] private SWQuestDatabase questDatabase;
    [SerializeField] private SWAchievementDatabase achievementDatabase;
    [SerializeField] private SWQuest questToRegister;
    [SerializeField] private SW.Base.SWCategory reportCategory;
    [SerializeField] private SWQuestTarget reportTarget;

    private int score;
    #endregion // 필드

    #region 프로퍼티
    /// <summary>예제 보상이 누적한 점수입니다.</summary>
    public int Score => score;
    #endregion // 프로퍼티

    private void Awake()
    {
        if (questSystem == null)
        {
            questSystem = SWQuestSystem.Instance;
        }

        if (questSystem == null)
        {
            return;
        }

        questSystem.SetContext(this);
        if (!questSystem.IsInitialized
            && (questDatabase != null || achievementDatabase != null))
        {
            questSystem.Initialize(questDatabase, achievementDatabase);
        }
    }

    private void OnEnable()
    {
        if (questSystem != null)
        {
            questSystem.QuestCompleted += HandleQuestCompleted;
            questSystem.AchievementUnlocked += HandleAchievementUnlocked;
        }
    }

    private void Start()
    {
        if (questSystem != null && questToRegister != null)
        {
            questSystem.Register(questToRegister);
        }
    }

    private void OnDisable()
    {
        if (questSystem != null)
        {
            questSystem.QuestCompleted -= HandleQuestCompleted;
            questSystem.AchievementUnlocked -= HandleAchievementUnlocked;
        }
    }

    /// <summary>
    /// 연결한 카테고리와 대상을 한 번 진행했다고 보고합니다.
    /// 사용자 인터페이스 버튼이나 게임 플레이 이벤트에서 호출할 수 있습니다.
    /// </summary>
    public void ReportProgress()
    {
        if (questSystem != null)
        {
            questSystem.ReceiveReport(reportCategory, reportTarget != null ? reportTarget.Value : null);
        }
    }

    /// <summary>
    /// 예제 보상에서 점수를 더합니다.
    /// </summary>
    /// <param name="amount">더할 점수입니다.</param>
    public void AddScore(int amount)
    {
        score += amount;
    }

    /// <summary>
    /// 일반 퀘스트 완료를 확인합니다.
    /// </summary>
    private static void HandleQuestCompleted(SWQuest quest)
    {
        Debug.Log($"퀘스트 완료: {quest.DisplayName}");
    }

    /// <summary>
    /// 업적 달성을 확인합니다.
    /// </summary>
    private static void HandleAchievementUnlocked(SWAchievement achievement)
    {
        Debug.Log($"업적 달성: {achievement.DisplayName}");
    }
}
