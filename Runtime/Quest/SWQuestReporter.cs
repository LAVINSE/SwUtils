using System;
using UnityEngine;

using SW.Attributes;

using SW.Base;

using SW.Util;

namespace SW.Quest
{
    /// <summary>
    /// 직접 호출 또는 삼차원·이차원 트리거 진입으로 퀘스트 진행을 보고합니다.
    /// </summary>
    public sealed class SWQuestReporter : SWMonoBehaviour
    {
        #region 필드
        [SWGroup("진행 보고")]
        [SerializeField] private SWQuestSystem questSystem;
        [SerializeField] private SWCategory category;
        [SerializeField] private SWQuestTarget target;
        [SerializeField] private int amount = 1;

        [SWGroup("트리거")]
        [Tooltip("비어 있으면 모든 충돌체의 진입을 보고합니다.")]
        [SerializeField] private string[] colliderTags = Array.Empty<string>();
        #endregion // 필드

        private void OnTriggerEnter(Collider other)
        {
            ReportIfTagMatches(other);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            ReportIfTagMatches(other);
        }

        /// <summary>
        /// 설정된 카테고리, 대상과 변화량을 퀘스트 시스템에 보고합니다.
        /// </summary>
        [SWButton("진행 보고")]
        public void Report()
        {
            SWQuestSystem targetSystem = ResolveQuestSystem();
            if (targetSystem == null)
            {
                return;
            }

            targetSystem.ReceiveReport(category, target != null ? target.Value : null, amount);
        }

        /// <summary>
        /// 충돌체 태그가 설정과 일치하면 진행을 보고합니다.
        /// </summary>
        private void ReportIfTagMatches(Component other)
        {
            if (other == null)
            {
                return;
            }

            if (colliderTags == null || colliderTags.Length == 0)
            {
                Report();
                return;
            }

            for (int index = 0; index < colliderTags.Length; index++)
            {
                string colliderTag = colliderTags[index];
                if (!string.IsNullOrEmpty(colliderTag) && other.CompareTag(colliderTag))
                {
                    Report();
                    return;
                }
            }
        }

        /// <summary>
        /// 인스펙터 참조가 없으면 전역 퀘스트 시스템을 반환합니다.
        /// </summary>
        private SWQuestSystem ResolveQuestSystem()
        {
            SWQuestSystem targetSystem = questSystem != null ? questSystem : SWQuestSystem.Instance;
            if (targetSystem == null)
            {
                SWLog.LogWarning($"[SWQuestReporter] 퀘스트 시스템을 찾을 수 없습니다: {name}");
            }

            return targetSystem;
        }
    }
}
