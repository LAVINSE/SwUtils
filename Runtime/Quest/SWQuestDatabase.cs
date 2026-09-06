using System;
using System.Collections.Generic;
using UnityEngine;

using SW.Attributes;

using SW.Base;

using SW.Util;

namespace SW.Quest
{
    /// <summary>
    /// 일반 퀘스트 정의만 관리하는 데이터베이스입니다.
    /// </summary>
    [CreateAssetMenu(fileName = "SWQuestDatabase", menuName = "SWUtils/Quest/Quest Database")]
    public sealed class SWQuestDatabase : SWScriptableObject
    {
        #region 필드
        [SWGroup("일반 퀘스트")]
        [SerializeField] private List<SWQuest> quests = new();

        private Dictionary<string, SWQuest> questsByCodeName;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>등록된 일반 퀘스트 정의입니다.</summary>
        public IReadOnlyList<SWQuest> Quests
            => quests != null ? quests : Array.Empty<SWQuest>();
        #endregion // 프로퍼티

        #region 에디터
#if UNITY_EDITOR
        private void OnValidate()
        {
            quests ??= new List<SWQuest>();
            InvalidateCache();
        }

        /// <summary>
        /// 프로젝트에 있는 모든 일반 퀘스트 정의를 찾아 목록을 다시 구성합니다.
        /// </summary>
        [SWButton("프로젝트 퀘스트 수집")]
        public void SynchronizeProjectDefinitions()
        {
            quests = new List<SWQuest>();
            string[] assetIdentifiers = UnityEditor.AssetDatabase.FindAssets("t:SWQuest");

            for (int index = 0; index < assetIdentifiers.Length; index++)
            {
                string assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(assetIdentifiers[index]);
                SWQuest definition = UnityEditor.AssetDatabase.LoadAssetAtPath<SWQuest>(assetPath);
                if (definition != null && definition is not SWAchievement)
                {
                    quests.Add(definition);
                }
            }

            quests.Sort(CompareDefinitions);
            InvalidateCache();
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.AssetDatabase.SaveAssets();
            SWLog.Log($"[SWQuestDatabase] 수집 완료: 일반 퀘스트 {quests.Count}개");
        }

        /// <summary>
        /// 코드명 기준으로 퀘스트 정의 에셋을 정렬합니다.
        /// </summary>
        private static int CompareDefinitions(SWQuest left, SWQuest right)
        {
            string leftCodeName = left != null ? left.CodeName : string.Empty;
            string rightCodeName = right != null ? right.CodeName : string.Empty;
            return string.Compare(leftCodeName, rightCodeName, StringComparison.Ordinal);
        }
#endif // UNITY_EDITOR
        #endregion // 에디터

        #region 조회
        /// <summary>
        /// 코드명으로 일반 퀘스트 정의를 찾습니다.
        /// </summary>
        /// <param name="codeName">찾을 퀘스트 코드명입니다.</param>
        /// <returns>일반 퀘스트 정의이며, 없으면 <see langword="null"/>입니다.</returns>
        public SWQuest FindQuest(string codeName)
        {
            if (string.IsNullOrEmpty(codeName))
            {
                return null;
            }

            EnsureCache();
            return questsByCodeName.TryGetValue(codeName, out SWQuest quest) ? quest : null;
        }
        #endregion // 조회

        #region 검증
        /// <summary>
        /// 데이터베이스의 빈 참조, 코드명과 중복 항목을 검사합니다.
        /// </summary>
        /// <returns>발견한 문제 설명 목록입니다.</returns>
        public IReadOnlyList<string> ValidateDefinitions()
            => SWQuestDefinitionValidator.Validate(quests, "일반 퀘스트");

#if UNITY_EDITOR
        /// <summary>
        /// 데이터베이스 검증 결과를 콘솔에 출력합니다.
        /// </summary>
        [SWButton("퀘스트 데이터베이스 검증")]
        private void LogValidation()
        {
            IReadOnlyList<string> messages = ValidateDefinitions();
            if (messages.Count == 0)
            {
                SWLog.Log("[SWQuestDatabase] 검증 완료: 문제가 없습니다.");
                return;
            }

            for (int index = 0; index < messages.Count; index++)
            {
                SWLog.LogWarning($"[SWQuestDatabase] {messages[index]}");
            }
        }
#endif // UNITY_EDITOR
        #endregion // 검증

        #region 캐시
        /// <summary>
        /// 코드명 조회 캐시가 없으면 생성합니다.
        /// </summary>
        private void EnsureCache()
        {
            if (questsByCodeName != null)
            {
                return;
            }

            questsByCodeName = new Dictionary<string, SWQuest>(StringComparer.Ordinal);
            if (quests == null)
            {
                return;
            }

            for (int index = 0; index < quests.Count; index++)
            {
                SWQuest quest = quests[index];
                if (quest == null || string.IsNullOrWhiteSpace(quest.CodeName))
                {
                    continue;
                }

                if (!questsByCodeName.TryAdd(quest.CodeName, quest))
                {
                    SWLog.LogError($"[SWQuestDatabase] 일반 퀘스트 중복 코드명입니다: {quest.CodeName}");
                }
            }
        }

        /// <summary>
        /// 코드명 조회 캐시를 무효화합니다.
        /// </summary>
        private void InvalidateCache()
        {
            questsByCodeName = null;
        }
        #endregion // 캐시
    }
}
