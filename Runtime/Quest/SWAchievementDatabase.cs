using System;
using System.Collections.Generic;
using UnityEngine;

using SW.Attributes;

using SW.Base;

using SW.Util;

namespace SW.Quest
{
    /// <summary>
    /// 업적 정의만 관리하는 데이터베이스입니다.
    /// </summary>
    [CreateAssetMenu(fileName = "SWAchievementDatabase", menuName = "SWUtils/Quest/Achievement Database")]
    public sealed class SWAchievementDatabase : SWScriptableObject
    {
        #region 필드
        [SWGroup("업적")]
        [SerializeField] private List<SWAchievement> achievements = new();

        private Dictionary<string, SWAchievement> achievementsByCodeName;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>등록된 업적 정의입니다.</summary>
        public IReadOnlyList<SWAchievement> Achievements
            => achievements != null ? achievements : Array.Empty<SWAchievement>();
        #endregion // 프로퍼티

        #region 에디터
#if UNITY_EDITOR
        private void OnValidate()
        {
            achievements ??= new List<SWAchievement>();
            InvalidateCache();
        }

        /// <summary>
        /// 프로젝트에 있는 모든 업적 정의를 찾아 목록을 다시 구성합니다.
        /// </summary>
        [SWButton("프로젝트 업적 수집")]
        public void SynchronizeProjectDefinitions()
        {
            achievements = new List<SWAchievement>();
            string[] assetIdentifiers = UnityEditor.AssetDatabase.FindAssets("t:SWAchievement");

            for (int index = 0; index < assetIdentifiers.Length; index++)
            {
                string assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(assetIdentifiers[index]);
                SWAchievement definition = UnityEditor.AssetDatabase.LoadAssetAtPath<SWAchievement>(assetPath);
                if (definition != null)
                {
                    achievements.Add(definition);
                }
            }

            achievements.Sort(CompareDefinitions);
            InvalidateCache();
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.AssetDatabase.SaveAssets();
            SWLog.Log($"[SWAchievementDatabase] 수집 완료: 업적 {achievements.Count}개");
        }

        /// <summary>
        /// 코드명 기준으로 업적 정의 에셋을 정렬합니다.
        /// </summary>
        private static int CompareDefinitions(SWAchievement left, SWAchievement right)
        {
            string leftCodeName = left != null ? left.CodeName : string.Empty;
            string rightCodeName = right != null ? right.CodeName : string.Empty;
            return string.Compare(leftCodeName, rightCodeName, StringComparison.Ordinal);
        }
#endif // UNITY_EDITOR
        #endregion // 에디터

        #region 조회
        /// <summary>
        /// 코드명으로 업적 정의를 찾습니다.
        /// </summary>
        /// <param name="codeName">찾을 업적 코드명입니다.</param>
        /// <returns>업적 정의이며, 없으면 <see langword="null"/>입니다.</returns>
        public SWAchievement FindAchievement(string codeName)
        {
            if (string.IsNullOrEmpty(codeName))
            {
                return null;
            }

            EnsureCache();
            return achievementsByCodeName.TryGetValue(codeName, out SWAchievement achievement)
                ? achievement
                : null;
        }
        #endregion // 조회

        #region 검증
        /// <summary>
        /// 데이터베이스의 빈 참조, 코드명과 중복 항목을 검사합니다.
        /// </summary>
        /// <returns>발견한 문제 설명 목록입니다.</returns>
        public IReadOnlyList<string> ValidateDefinitions()
            => SWQuestDefinitionValidator.Validate(achievements, "업적");

#if UNITY_EDITOR
        /// <summary>
        /// 데이터베이스 검증 결과를 콘솔에 출력합니다.
        /// </summary>
        [SWButton("업적 데이터베이스 검증")]
        private void LogValidation()
        {
            IReadOnlyList<string> messages = ValidateDefinitions();
            if (messages.Count == 0)
            {
                SWLog.Log("[SWAchievementDatabase] 검증 완료: 문제가 없습니다.");
                return;
            }

            for (int index = 0; index < messages.Count; index++)
            {
                SWLog.LogWarning($"[SWAchievementDatabase] {messages[index]}");
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
            if (achievementsByCodeName != null)
            {
                return;
            }

            achievementsByCodeName = new Dictionary<string, SWAchievement>(StringComparer.Ordinal);
            if (achievements == null)
            {
                return;
            }

            for (int index = 0; index < achievements.Count; index++)
            {
                SWAchievement achievement = achievements[index];
                if (achievement == null || string.IsNullOrWhiteSpace(achievement.CodeName))
                {
                    continue;
                }

                if (!achievementsByCodeName.TryAdd(achievement.CodeName, achievement))
                {
                    SWLog.LogError($"[SWAchievementDatabase] 업적 중복 코드명입니다: {achievement.CodeName}");
                }
            }
        }

        /// <summary>
        /// 코드명 조회 캐시를 무효화합니다.
        /// </summary>
        private void InvalidateCache()
        {
            achievementsByCodeName = null;
        }
        #endregion // 캐시
    }
}
