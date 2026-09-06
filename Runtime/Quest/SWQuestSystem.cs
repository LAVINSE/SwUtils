using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

using SW.Attributes;

using SW.Util;

namespace SW.Quest
{
    /// <summary>
    /// 일반 퀘스트와 업적의 등록, 진행 보고, 완료, 취소와 저장 복원을 관리합니다.
    /// </summary>
    /// <remarks>
    /// 씬에 직접 배치하거나 <see cref="SWSingleton{T}.Instance"/>를 통해 자동 생성할 수 있습니다.
    /// 자동 생성한 경우 저장 복원과 업적 자동 등록 전에 <see cref="Initialize"/>로 데이터베이스를 연결해야 합니다.
    /// </remarks>
    public sealed class SWQuestSystem : SWSingleton<SWQuestSystem>
    {
        #region 상수
        /// <summary>암호화된 SWPlayerPrefs에 사용하는 기본 저장 키입니다.</summary>
        public const string DefaultSaveKey = "SWQuestSystem_SaveData";

        /// <summary>현재 퀘스트 저장 데이터 형식 버전입니다.</summary>
        public const int CurrentSaveDataVersion = 1;
        #endregion // 상수

        #region 필드
        [SWGroup("데이터베이스")]
        [FormerlySerializedAs("database")]
        [SerializeField] private SWQuestDatabase questDatabase;
        [SerializeField] private SWAchievementDatabase achievementDatabase;

        [SWGroup("자동 초기화")]
        [SerializeField] private bool loadOnAwake = true;
        [SerializeField] private bool registerAchievementsAutomatically = true;
        [SerializeField] private string saveKey = DefaultSaveKey;

        private readonly List<SWQuest> activeQuests = new();
        private readonly List<SWQuest> completedQuests = new();
        private readonly List<SWAchievement> activeAchievements = new();
        private readonly List<SWAchievement> completedAchievements = new();
        private ISWQuestSaveStore saveStore;
        #endregion // 필드

        #region 이벤트
        /// <summary>일반 퀘스트가 등록된 뒤 발생합니다.</summary>
        public event Action<SWQuest> QuestRegistered;

        /// <summary>일반 퀘스트가 완료된 뒤 발생합니다.</summary>
        public event Action<SWQuest> QuestCompleted;

        /// <summary>일반 퀘스트가 취소된 뒤 발생합니다.</summary>
        public event Action<SWQuest> QuestCanceled;

        /// <summary>업적이 등록된 뒤 발생합니다.</summary>
        public event Action<SWAchievement> AchievementRegistered;

        /// <summary>업적이 달성된 뒤 발생합니다.</summary>
        public event Action<SWAchievement> AchievementUnlocked;

        /// <summary>일반 퀘스트 또는 업적 작업의 진행량이 변경된 뒤 발생합니다.</summary>
        public event SWQuest.TaskProgressChangedHandler TaskProgressChanged;

        /// <summary>퀘스트 또는 업적 보상이 지급된 뒤 발생합니다.</summary>
        public event SWQuest.RewardGrantedHandler RewardGranted;
        #endregion // 이벤트

        #region 프로퍼티
        /// <summary>현재 연결된 일반 퀘스트 데이터베이스입니다.</summary>
        public SWQuestDatabase QuestDatabase => questDatabase;

        /// <summary>현재 연결된 업적 데이터베이스입니다.</summary>
        public SWAchievementDatabase AchievementDatabase => achievementDatabase;

        /// <summary>데이터베이스 초기화가 완료되었는지 여부입니다.</summary>
        public bool IsInitialized { get; private set; }

        /// <summary>프로젝트별 조건과 보상에서 사용할 수 있는 외부 문맥 객체입니다.</summary>
        public object Context { get; private set; }

        /// <summary>현재 진행 중이거나 완료를 기다리는 일반 퀘스트입니다.</summary>
        public IReadOnlyList<SWQuest> ActiveQuests => activeQuests;

        /// <summary>완료한 일반 퀘스트입니다.</summary>
        public IReadOnlyList<SWQuest> CompletedQuests => completedQuests;

        /// <summary>현재 진행 중인 업적입니다.</summary>
        public IReadOnlyList<SWAchievement> ActiveAchievements => activeAchievements;

        /// <summary>달성한 업적입니다.</summary>
        public IReadOnlyList<SWAchievement> CompletedAchievements => completedAchievements;

        /// <summary>실제로 사용할 저장 키입니다.</summary>
        public string SaveKey => string.IsNullOrWhiteSpace(saveKey) ? DefaultSaveKey : saveKey.Trim();

        /// <summary>퀘스트 직렬화 문자열을 보관하는 저장소입니다.</summary>
        public ISWQuestSaveStore SaveStore => saveStore ??= new SWQuestPlayerPrefsSaveStore();
        #endregion // 프로퍼티

        #region 초기화
        /// <inheritdoc />
        public override void Awake()
        {
            base.Awake();
            if (Instance != this)
            {
                return;
            }

            if (questDatabase != null || achievementDatabase != null)
            {
                Initialize(questDatabase, achievementDatabase, loadOnAwake);
            }
        }

        /// <inheritdoc />
        public override void OnDestroy()
        {
            ClearRuntimeState();
            QuestRegistered = null;
            QuestCompleted = null;
            QuestCanceled = null;
            AchievementRegistered = null;
            AchievementUnlocked = null;
            TaskProgressChanged = null;
            RewardGranted = null;
            base.OnDestroy();
        }

        /// <summary>
        /// 일반 퀘스트 데이터베이스만 연결하고 저장 복원을 수행합니다.
        /// </summary>
        /// <param name="questDatabase">사용할 퀘스트 데이터베이스입니다.</param>
        /// <param name="loadSavedData">저장 데이터가 있으면 복원할지 여부입니다.</param>
        /// <returns>초기화에 성공했으면 <see langword="true"/>입니다.</returns>
        public bool Initialize(SWQuestDatabase questDatabase, bool loadSavedData = true)
            => Initialize(questDatabase, null, loadSavedData);

        /// <summary>
        /// 일반 퀘스트와 업적 데이터베이스를 연결하고 저장 복원 또는 업적 자동 등록을 수행합니다.
        /// </summary>
        /// <param name="questDatabase">사용할 일반 퀘스트 데이터베이스입니다.</param>
        /// <param name="achievementDatabase">사용할 업적 데이터베이스입니다.</param>
        /// <param name="loadSavedData">저장 데이터가 있으면 복원할지 여부입니다.</param>
        /// <returns>하나 이상의 데이터베이스를 연결했으면 <see langword="true"/>입니다.</returns>
        public bool Initialize(SWQuestDatabase questDatabase,
            SWAchievementDatabase achievementDatabase, bool loadSavedData = true)
        {
            if (questDatabase == null && achievementDatabase == null)
            {
                SWLog.LogError("[SWQuestSystem] 초기화 실패: 퀘스트 또는 업적 데이터베이스가 없습니다.");
                return false;
            }

            if (IsInitialized)
            {
                if (this.questDatabase == questDatabase
                    && this.achievementDatabase == achievementDatabase)
                {
                    return true;
                }

                SWLog.LogWarning("[SWQuestSystem] 다른 데이터베이스로 다시 초기화하려면 ClearRuntimeState를 먼저 호출해야 합니다.");
                return false;
            }

            this.questDatabase = questDatabase;
            this.achievementDatabase = achievementDatabase;
            IsInitialized = true;

            if (this.questDatabase != null)
            {
                LogValidationMessages(this.questDatabase.ValidateDefinitions(), "퀘스트");
            }

            if (this.achievementDatabase != null)
            {
                LogValidationMessages(this.achievementDatabase.ValidateDefinitions(), "업적");
            }

            if (loadSavedData && HasSavedData() && !Load())
            {
                SWLog.LogWarning("[SWQuestSystem] 저장 데이터를 복원하지 못해 빈 런타임 상태로 시작합니다.");
            }

            if (registerAchievementsAutomatically)
            {
                RegisterMissingAchievements();
            }

            return true;
        }

        /// <summary>
        /// 데이터베이스 검증 결과를 시스템 로그에 출력합니다.
        /// </summary>
        private static void LogValidationMessages(IReadOnlyList<string> messages,
            string databaseName)
        {
            for (int index = 0; index < messages.Count; index++)
            {
                SWLog.LogWarning($"[SWQuestSystem] {databaseName} 데이터베이스 검증: {messages[index]}");
            }
        }

        /// <summary>
        /// 조건과 보상 구현에서 사용할 프로젝트별 문맥 객체를 지정합니다.
        /// </summary>
        /// <param name="context">게임 서비스, 플레이어 데이터 등의 외부 문맥입니다.</param>
        public void SetContext(object context)
        {
            Context = context;
        }

        /// <summary>
        /// 퀘스트 저장과 불러오기에 사용할 저장소를 교체합니다.
        /// 자동 불러오기 전에 교체하려면 자동 불러오기를 끄고 수동으로 초기화해야 합니다.
        /// </summary>
        /// <param name="store">사용할 저장소입니다. <see langword="null"/>이면 기본 저장소로 되돌립니다.</param>
        public void SetSaveStore(ISWQuestSaveStore store)
        {
            saveStore = store;
        }

        /// <summary>
        /// 지정한 타입으로 외부 문맥 객체를 가져옵니다.
        /// </summary>
        /// <typeparam name="TContext">가져올 문맥 타입입니다.</typeparam>
        /// <param name="context">가져온 문맥 객체입니다.</param>
        /// <returns>타입이 일치하는 문맥이 있으면 <see langword="true"/>입니다.</returns>
        public bool TryGetContext<TContext>(out TContext context) where TContext : class
        {
            context = Context as TContext;
            return context != null;
        }
        #endregion // 초기화

        #region 등록
        /// <summary>
        /// 조건을 충족하는 일반 퀘스트 또는 업적 정의를 런타임에 등록합니다.
        /// </summary>
        /// <param name="definition">등록할 원본 정의 에셋입니다.</param>
        /// <returns>등록된 런타임 복제본이며, 등록하지 못하면 <see langword="null"/>입니다.</returns>
        public SWQuest Register(SWQuest definition)
        {
            if (definition == null)
            {
                SWLog.LogWarning("[SWQuestSystem] 등록 실패: 퀘스트 정의가 없습니다.");
                return null;
            }

            if (ContainsActive(definition) || ContainsCompleted(definition))
            {
                SWLog.LogWarning($"[SWQuestSystem] 등록 생략: 이미 등록했거나 완료한 정의입니다. 퀘스트: {definition.CodeName}");
                return null;
            }

            if (!definition.IsAcceptable(this))
            {
                return null;
            }

            return CreateAndAddRuntimeQuest(definition, null, true);
        }

        /// <summary>
        /// 데이터베이스의 등록되지 않은 모든 업적을 런타임에 등록합니다.
        /// </summary>
        /// <returns>새로 등록한 업적 수입니다.</returns>
        public int RegisterMissingAchievements()
        {
            if (achievementDatabase == null)
            {
                return 0;
            }

            IReadOnlyList<SWAchievement> achievements = achievementDatabase.Achievements;
            int registeredCount = 0;

            for (int index = 0; index < achievements.Count; index++)
            {
                SWAchievement achievement = achievements[index];
                if (achievement != null && !ContainsActive(achievement) && !ContainsCompleted(achievement)
                    && Register(achievement) != null)
                {
                    registeredCount++;
                }
            }

            return registeredCount;
        }

        /// <summary>
        /// 런타임 복제본을 생성하고 이벤트와 목록에 연결합니다.
        /// </summary>
        private SWQuest CreateAndAddRuntimeQuest(SWQuest definition, SWQuestSaveData saveData,
            bool invokeRegisteredEvent)
        {
            SWQuest runtimeQuest = definition.CreateRuntimeClone();
            if (runtimeQuest == null || !runtimeQuest.PrepareRuntime(this))
            {
                DestroyRuntimeQuest(runtimeQuest);
                return null;
            }

            if (saveData == null)
            {
                SubscribeRuntimeQuest(runtimeQuest);
                AddToActiveList(runtimeQuest);
                if (invokeRegisteredEvent)
                {
                    InvokeRegisteredEvent(runtimeQuest);
                }

                runtimeQuest.Begin();
            }
            else
            {
                runtimeQuest.Restore(saveData);
                SubscribeRuntimeQuest(runtimeQuest);
                AddRestoredQuest(runtimeQuest);
            }

            return runtimeQuest;
        }

        /// <summary>
        /// 런타임 퀘스트 이벤트를 시스템 처리기에 연결합니다.
        /// </summary>
        private void SubscribeRuntimeQuest(SWQuest quest)
        {
            quest.Completed += HandleQuestCompleted;
            quest.Canceled += HandleQuestCanceled;
            quest.TaskProgressChanged += HandleTaskProgressChanged;
            quest.RewardGranted += HandleRewardGranted;
        }

        /// <summary>
        /// 런타임 퀘스트 이벤트를 시스템 처리기에서 해제합니다.
        /// </summary>
        private void UnsubscribeRuntimeQuest(SWQuest quest)
        {
            if (quest == null)
            {
                return;
            }

            quest.Completed -= HandleQuestCompleted;
            quest.Canceled -= HandleQuestCanceled;
            quest.TaskProgressChanged -= HandleTaskProgressChanged;
            quest.RewardGranted -= HandleRewardGranted;
        }
        #endregion // 등록

        #region 진행
        /// <summary>
        /// 모든 진행 중인 일반 퀘스트와 업적에 보고를 전달합니다.
        /// </summary>
        /// <param name="report">전달할 진행 보고입니다.</param>
        public void ReceiveReport(SWQuestReport report)
        {
            SWQuest[] questSnapshot = activeQuests.ToArray();
            for (int index = 0; index < questSnapshot.Length; index++)
            {
                questSnapshot[index]?.ReceiveReport(report);
            }

            SWAchievement[] achievementSnapshot = activeAchievements.ToArray();
            for (int index = 0; index < achievementSnapshot.Length; index++)
            {
                achievementSnapshot[index]?.ReceiveReport(report);
            }
        }

        /// <summary>
        /// 카테고리 코드명, 대상과 변화량으로 진행 보고를 생성하여 전달합니다.
        /// </summary>
        /// <param name="categoryCode">보고를 구분하는 카테고리 코드명입니다.</param>
        /// <param name="target">보고 대상입니다.</param>
        /// <param name="amount">진행 변화량입니다.</param>
        public void ReceiveReport(string categoryCode, object target, int amount = 1)
            => ReceiveReport(new SWQuestReport(categoryCode, target, amount));

        /// <summary>
        /// 카테고리, 대상과 변화량으로 진행 보고를 생성하여 전달합니다.
        /// </summary>
        /// <param name="category">보고를 구분하는 카테고리입니다.</param>
        /// <param name="target">보고 대상입니다.</param>
        /// <param name="amount">진행 변화량입니다.</param>
        public void ReceiveReport(SW.Base.SWCategory category, object target, int amount = 1)
            => ReceiveReport(new SWQuestReport(category, target, amount));

        /// <summary>
        /// 완료 확정을 기다리는 모든 일반 퀘스트를 완료합니다.
        /// </summary>
        /// <returns>완료한 퀘스트 수입니다.</returns>
        public int CompleteWaitingQuests()
        {
            SWQuest[] questSnapshot = activeQuests.ToArray();
            int completedCount = 0;

            for (int index = 0; index < questSnapshot.Length; index++)
            {
                if (questSnapshot[index] != null && questSnapshot[index].Complete())
                {
                    completedCount++;
                }
            }

            return completedCount;
        }
        #endregion // 진행

        #region 조회
        /// <summary>
        /// 지정한 정의의 진행 중인 런타임 퀘스트가 있는지 확인합니다.
        /// </summary>
        /// <param name="definition">확인할 일반 퀘스트 또는 업적 정의입니다.</param>
        /// <returns>진행 목록에 같은 정의가 있으면 <see langword="true"/>입니다.</returns>
        public bool ContainsActive(SWQuest definition)
            => FindMatchingQuest(definition, activeQuests, activeAchievements) != null;

        /// <summary>
        /// 지정한 정의의 완료된 런타임 퀘스트가 있는지 확인합니다.
        /// </summary>
        /// <param name="definition">확인할 일반 퀘스트 또는 업적 정의입니다.</param>
        /// <returns>완료 목록에 같은 정의가 있으면 <see langword="true"/>입니다.</returns>
        public bool ContainsCompleted(SWQuest definition)
            => FindMatchingQuest(definition, completedQuests, completedAchievements) != null;

        /// <summary>
        /// 코드명에 해당하는 진행 중인 일반 퀘스트를 반환합니다.
        /// </summary>
        /// <param name="codeName">찾을 일반 퀘스트 코드명입니다.</param>
        /// <returns>진행 중인 일반 퀘스트이며, 없으면 <see langword="null"/>입니다.</returns>
        public SWQuest FindActiveQuest(string codeName)
            => FindByCodeName(activeQuests, codeName);

        /// <summary>
        /// 코드명에 해당하는 완료된 일반 퀘스트를 반환합니다.
        /// </summary>
        /// <param name="codeName">찾을 일반 퀘스트 코드명입니다.</param>
        /// <returns>완료한 일반 퀘스트이며, 없으면 <see langword="null"/>입니다.</returns>
        public SWQuest FindCompletedQuest(string codeName)
            => FindByCodeName(completedQuests, codeName);

        /// <summary>
        /// 코드명에 해당하는 진행 중인 업적을 반환합니다.
        /// </summary>
        /// <param name="codeName">찾을 업적 코드명입니다.</param>
        /// <returns>진행 중인 업적이며, 없으면 <see langword="null"/>입니다.</returns>
        public SWAchievement FindActiveAchievement(string codeName)
            => FindByCodeName(activeAchievements, codeName);

        /// <summary>
        /// 코드명에 해당하는 달성된 업적을 반환합니다.
        /// </summary>
        /// <param name="codeName">찾을 업적 코드명입니다.</param>
        /// <returns>달성한 업적이며, 없으면 <see langword="null"/>입니다.</returns>
        public SWAchievement FindCompletedAchievement(string codeName)
            => FindByCodeName(completedAchievements, codeName);

        /// <summary>
        /// 정의 타입에 맞는 목록에서 같은 런타임 퀘스트를 찾습니다.
        /// </summary>
        private static SWQuest FindMatchingQuest(SWQuest definition, IReadOnlyList<SWQuest> quests,
            IReadOnlyList<SWAchievement> achievements)
        {
            if (definition == null)
            {
                return null;
            }

            if (definition is SWAchievement)
            {
                return FindSameQuest(achievements, definition);
            }

            return FindSameQuest(quests, definition);
        }

        /// <summary>
        /// 목록에서 같은 정의의 런타임 퀘스트를 찾습니다.
        /// </summary>
        private static TQuest FindSameQuest<TQuest>(IReadOnlyList<TQuest> quests, SWQuest definition)
            where TQuest : SWQuest
        {
            for (int index = 0; index < quests.Count; index++)
            {
                if (quests[index] != null && quests[index].IsSameQuest(definition))
                {
                    return quests[index];
                }
            }

            return null;
        }

        /// <summary>
        /// 목록에서 코드명이 같은 런타임 퀘스트를 찾습니다.
        /// </summary>
        private static TQuest FindByCodeName<TQuest>(IReadOnlyList<TQuest> quests, string codeName)
            where TQuest : SWQuest
        {
            if (string.IsNullOrEmpty(codeName))
            {
                return null;
            }

            for (int index = 0; index < quests.Count; index++)
            {
                if (quests[index] != null
                    && string.Equals(quests[index].CodeName, codeName, StringComparison.Ordinal))
                {
                    return quests[index];
                }
            }

            return null;
        }
        #endregion // 조회

        #region 저장
        /// <summary>
        /// 저장 가능한 퀘스트와 업적 상태를 하나의 데이터로 생성합니다.
        /// </summary>
        /// <returns>게임 저장 데이터에 포함할 수 있는 퀘스트 시스템 데이터입니다.</returns>
        public SWQuestSystemSaveData CreateSaveData()
        {
            return new SWQuestSystemSaveData
            {
                version = CurrentSaveDataVersion,
                activeQuests = CreateSaveArray(activeQuests),
                completedQuests = CreateSaveArray(completedQuests),
                activeAchievements = CreateSaveArray(activeAchievements),
                completedAchievements = CreateSaveArray(completedAchievements)
            };
        }

        /// <summary>
        /// 현재 퀘스트 상태를 암호화된 SWPlayerPrefs에 저장합니다.
        /// </summary>
        /// <returns>저장했으면 <see langword="true"/>입니다.</returns>
        [SWButton("퀘스트 저장")]
        public bool Save()
        {
            try
            {
                string serializedData = JsonUtility.ToJson(CreateSaveData());
                return SaveStore.Save(SaveKey, serializedData);
            }
            catch (Exception exception)
            {
                SWLog.LogError($"[SWQuestSystem] 저장 실패: {exception.Message}");
                return false;
            }
        }

        /// <summary>
        /// 암호화된 SWPlayerPrefs에서 현재 저장 키의 퀘스트 상태를 복원합니다.
        /// </summary>
        /// <returns>저장 데이터를 찾아 복원했으면 <see langword="true"/>입니다.</returns>
        [SWButton("퀘스트 불러오기")]
        public bool Load()
        {
            if (questDatabase == null && achievementDatabase == null)
            {
                return false;
            }

            try
            {
                if (!SaveStore.TryLoad(SaveKey, out string serializedData))
                {
                    return false;
                }

                SWQuestSystemSaveData saveData = JsonUtility.FromJson<SWQuestSystemSaveData>(serializedData);
                return RestoreSaveData(saveData);
            }
            catch (Exception exception)
            {
                SWLog.LogError($"[SWQuestSystem] 불러오기 실패: {exception.Message}");
                return false;
            }
        }

        /// <summary>
        /// 현재 저장소에 퀘스트 데이터가 있는지 예외를 격리하여 확인합니다.
        /// </summary>
        /// <returns>저장 데이터가 있으면 <see langword="true"/>입니다.</returns>
        private bool HasSavedData()
        {
            try
            {
                return SaveStore.HasData(SaveKey);
            }
            catch (Exception exception)
            {
                SWLog.LogError($"[SWQuestSystem] 저장 데이터 확인 실패: {exception.Message}");
                return false;
            }
        }

        /// <summary>
        /// 전달한 데이터로 퀘스트 시스템 상태를 교체합니다. 완료 보상은 다시 지급하지 않습니다.
        /// </summary>
        /// <param name="saveData">복원할 퀘스트 시스템 저장 데이터입니다.</param>
        /// <returns>데이터를 복원했으면 <see langword="true"/>입니다.</returns>
        public bool RestoreSaveData(SWQuestSystemSaveData saveData)
        {
            if ((questDatabase == null && achievementDatabase == null) || saveData == null)
            {
                return false;
            }

            if (saveData.version < 0 || saveData.version > CurrentSaveDataVersion)
            {
                SWLog.LogError($"[SWQuestSystem] 지원하지 않는 저장 데이터 버전입니다. 현재: {CurrentSaveDataVersion}, 저장: {saveData.version}");
                return false;
            }

            ClearRuntimeState(false);
            RestoreQuestArray(saveData.activeQuests, false);
            RestoreQuestArray(saveData.completedQuests, false);
            RestoreQuestArray(saveData.activeAchievements, true);
            RestoreQuestArray(saveData.completedAchievements, true);
            return true;
        }

        /// <summary>
        /// 지정한 목록에서 저장 가능한 퀘스트 데이터만 배열로 생성합니다.
        /// </summary>
        private static SWQuestSaveData[] CreateSaveArray<TQuest>(IReadOnlyList<TQuest> quests)
            where TQuest : SWQuest
        {
            List<SWQuestSaveData> saveData = new(quests.Count);
            for (int index = 0; index < quests.Count; index++)
            {
                if (quests[index] != null && quests[index].IsSavable)
                {
                    saveData.Add(quests[index].CreateSaveData());
                }
            }

            return saveData.ToArray();
        }

        /// <summary>
        /// 저장 배열의 각 정의를 데이터베이스에서 찾아 런타임 상태로 복원합니다.
        /// </summary>
        private void RestoreQuestArray(SWQuestSaveData[] saveDataArray, bool isAchievement)
        {
            if (saveDataArray == null)
            {
                return;
            }

            for (int index = 0; index < saveDataArray.Length; index++)
            {
                SWQuestSaveData questSaveData = saveDataArray[index];
                if (questSaveData == null || string.IsNullOrEmpty(questSaveData.codeName))
                {
                    continue;
                }

                SWQuest definition = isAchievement
                    ? achievementDatabase?.FindAchievement(questSaveData.codeName)
                    : questDatabase?.FindQuest(questSaveData.codeName);

                if (definition == null)
                {
                    SWLog.LogWarning($"[SWQuestSystem] 저장된 정의를 찾지 못했습니다: {questSaveData.codeName}");
                    continue;
                }

                if (ContainsActive(definition) || ContainsCompleted(definition))
                {
                    SWLog.LogWarning($"[SWQuestSystem] 중복 저장 항목을 생략합니다: {questSaveData.codeName}");
                    continue;
                }

                CreateAndAddRuntimeQuest(definition, questSaveData, false);
            }
        }
        #endregion // 저장

        #region 초기화 해제
        /// <summary>
        /// 모든 런타임 퀘스트와 문맥을 정리하고 다시 초기화할 수 있는 상태로 만듭니다.
        /// 저장 데이터는 삭제하지 않습니다.
        /// </summary>
        [SWButton("런타임 퀘스트 초기화")]
        public void ClearRuntimeState()
        {
            ClearRuntimeState(true);
        }

        /// <summary>
        /// 런타임 상태를 정리하고 선택적으로 데이터베이스 초기화 상태도 해제합니다.
        /// </summary>
        private void ClearRuntimeState(bool clearInitialization)
        {
            List<SWQuest> quests = new(activeQuests.Count + completedQuests.Count
                + activeAchievements.Count + completedAchievements.Count);
            quests.AddRange(activeQuests);
            quests.AddRange(completedQuests);
            quests.AddRange(activeAchievements);
            quests.AddRange(completedAchievements);

            activeQuests.Clear();
            completedQuests.Clear();
            activeAchievements.Clear();
            completedAchievements.Clear();

            for (int index = 0; index < quests.Count; index++)
            {
                SWQuest quest = quests[index];
                UnsubscribeRuntimeQuest(quest);
                DestroyRuntimeQuest(quest);
            }

            if (clearInitialization)
            {
                IsInitialized = false;
                Context = null;
            }
        }

        /// <summary>
        /// 런타임 퀘스트와 그 작업 복제본을 안전하게 정리합니다.
        /// </summary>
        private static void DestroyRuntimeQuest(SWQuest quest)
        {
            if (quest == null)
            {
                return;
            }

            quest.ReleaseRuntime();
            if (Application.isPlaying)
            {
                Destroy(quest);
            }
            else
            {
                DestroyImmediate(quest);
            }
        }
        #endregion // 초기화 해제

        #region 목록과 이벤트 처리
        /// <summary>
        /// 새 런타임 퀘스트를 타입에 맞는 진행 목록에 추가합니다.
        /// </summary>
        private void AddToActiveList(SWQuest quest)
        {
            if (quest is SWAchievement achievement)
            {
                activeAchievements.Add(achievement);
            }
            else
            {
                activeQuests.Add(quest);
            }
        }

        /// <summary>
        /// 복원된 상태를 기준으로 진행 또는 완료 목록에 퀘스트를 추가합니다.
        /// </summary>
        private void AddRestoredQuest(SWQuest quest)
        {
            bool completed = quest.State == SWQuestState.Completed;

            if (quest is SWAchievement achievement)
            {
                if (completed)
                {
                    completedAchievements.Add(achievement);
                }
                else
                {
                    activeAchievements.Add(achievement);
                }
            }
            else if (completed)
            {
                completedQuests.Add(quest);
            }
            else
            {
                activeQuests.Add(quest);
            }
        }

        /// <summary>
        /// 타입에 맞는 등록 이벤트와 전역 이벤트 버스를 호출합니다.
        /// </summary>
        private void InvokeRegisteredEvent(SWQuest quest)
        {
            if (quest is SWAchievement achievement)
            {
                AchievementRegistered?.Invoke(achievement);
            }
            else
            {
                QuestRegistered?.Invoke(quest);
            }

            SWEventBus.Publish(new SWQuestRegisteredEvent(this, quest), false);
        }

        /// <summary>
        /// 완료된 런타임 퀘스트를 진행 목록에서 완료 목록으로 옮깁니다.
        /// </summary>
        private void HandleQuestCompleted(SWQuest quest)
        {
            if (quest is SWAchievement achievement)
            {
                activeAchievements.Remove(achievement);
                if (!completedAchievements.Contains(achievement))
                {
                    completedAchievements.Add(achievement);
                }

                AchievementUnlocked?.Invoke(achievement);
                SWEventBus.Publish(new SWAchievementUnlockedEvent(this, achievement), false);
                return;
            }

            activeQuests.Remove(quest);
            if (!completedQuests.Contains(quest))
            {
                completedQuests.Add(quest);
            }

            QuestCompleted?.Invoke(quest);
            SWEventBus.Publish(new SWQuestCompletedEvent(this, quest), false);
        }

        /// <summary>
        /// 취소된 일반 퀘스트를 진행 목록에서 제거하고 런타임 복제본을 정리합니다.
        /// </summary>
        private void HandleQuestCanceled(SWQuest quest)
        {
            if (quest == null || quest is SWAchievement)
            {
                return;
            }

            activeQuests.Remove(quest);
            QuestCanceled?.Invoke(quest);
            SWEventBus.Publish(new SWQuestCanceledEvent(this, quest), false);

            UnsubscribeRuntimeQuest(quest);
            DestroyRuntimeQuest(quest);
        }

        /// <summary>
        /// 작업 진행 변경을 지역 이벤트와 전역 이벤트 버스로 전달합니다.
        /// </summary>
        private void HandleTaskProgressChanged(SWQuest quest, SWQuestTask task,
            int currentProgress, int previousProgress)
        {
            TaskProgressChanged?.Invoke(quest, task, currentProgress, previousProgress);
            SWEventBus.Publish(new SWQuestTaskProgressChangedEvent(this, quest, task,
                currentProgress, previousProgress), false);
        }

        /// <summary>
        /// 보상 지급을 지역 이벤트와 전역 이벤트 버스로 전달합니다.
        /// </summary>
        private void HandleRewardGranted(SWQuest quest, SWQuestReward reward)
        {
            RewardGranted?.Invoke(quest, reward);
            SWEventBus.Publish(new SWQuestRewardGrantedEvent(this, quest, reward), false);
        }
        #endregion // 목록과 이벤트 처리
    }
}
