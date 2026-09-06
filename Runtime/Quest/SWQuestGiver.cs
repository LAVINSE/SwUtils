using System;
using UnityEngine;

using SW.Attributes;

using SW.Base;

using SW.Util;

namespace SW.Quest
{
    /// <summary>
    /// 연결된 퀘스트 정의를 시작 시 또는 외부 호출 시 퀘스트 시스템에 등록합니다.
    /// </summary>
    public sealed class SWQuestGiver : SWMonoBehaviour
    {
        #region 필드
        [SWGroup("퀘스트 지급")]
        [SerializeField] private SWQuestSystem questSystem;
        [SerializeField] private SWQuest[] quests = Array.Empty<SWQuest>();
        [SerializeField] private bool giveOnStart = true;
        #endregion // 필드

        private void Start()
        {
            if (giveOnStart)
            {
                Give();
            }
        }

        /// <summary>
        /// 연결된 모든 퀘스트 중 조건을 충족하고 아직 등록되지 않은 퀘스트를 지급합니다.
        /// </summary>
        /// <returns>새로 등록한 퀘스트 수입니다.</returns>
        [SWButton("퀘스트 지급")]
        public int Give()
        {
            SWQuestSystem targetSystem = ResolveQuestSystem();
            if (targetSystem == null)
            {
                return 0;
            }

            if (quests == null)
            {
                return 0;
            }

            int registeredCount = 0;
            for (int index = 0; index < quests.Length; index++)
            {
                if (quests[index] != null && targetSystem.Register(quests[index]) != null)
                {
                    registeredCount++;
                }
            }

            return registeredCount;
        }

        /// <summary>
        /// 인스펙터 참조가 없으면 전역 퀘스트 시스템을 반환합니다.
        /// </summary>
        private SWQuestSystem ResolveQuestSystem()
        {
            SWQuestSystem targetSystem = questSystem != null ? questSystem : SWQuestSystem.Instance;
            if (targetSystem == null)
            {
                SWLog.LogWarning($"[SWQuestGiver] 퀘스트 시스템을 찾을 수 없습니다: {name}");
            }

            return targetSystem;
        }
    }
}
