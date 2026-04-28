using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWUtils
{
    /// <summary>
    /// 키 기반 팝업 프리팹과 표시 옵션을 관리하는 ScriptableObject입니다.
    /// </summary>
    [CreateAssetMenu(menuName = "SWUtils/Popup Catalog", fileName = "SWPopupCatalog")]
    public class SWPopupCatalog : ScriptableObject
    {
        #region 데이터
        /// <summary>
        /// 팝업 등록 정보입니다.
        /// </summary>
        [Serializable]
        public class Entry
        {
            /// <summary>팝업을 찾을 때 사용하는 고유 키입니다.</summary>
            public string key;

            /// <summary>생성할 팝업 프리팹입니다.</summary>
            public SWPopupBase prefab;

            /// <summary>닫힌 팝업을 파괴하지 않고 재사용할지 여부입니다.</summary>
            public bool useCache;
        }
        #endregion // 데이터

        #region 필드
        [SerializeField] private List<Entry> entries = new();
        #endregion // 필드

        #region 프로퍼티
        /// <summary>등록된 팝업 목록입니다.</summary>
        public IReadOnlyList<Entry> Entries => entries;
        #endregion // 프로퍼티

        #region 조회
        /// <summary>
        /// 키에 해당하는 팝업 등록 정보를 가져옵니다.
        /// </summary>
        /// <param name="key">조회할 팝업 키입니다.</param>
        /// <param name="entry">조회된 팝업 등록 정보입니다.</param>
        /// <returns>유효한 등록 정보가 있으면 true입니다.</returns>
        public bool TryGetEntry(string key, out Entry entry)
        {
            entry = null;
            if (string.IsNullOrEmpty(key)) return false;

            for (int i = 0; i < entries.Count; i++)
            {
                Entry current = entries[i];
                if (current != null && current.key == key && current.prefab != null)
                {
                    entry = current;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 키에 해당하는 팝업 프리팹을 가져옵니다.
        /// </summary>
        /// <param name="key">조회할 팝업 키입니다.</param>
        /// <param name="prefab">조회된 팝업 프리팹입니다.</param>
        /// <returns>유효한 프리팹이 있으면 true입니다.</returns>
        public bool TryGetPrefab(string key, out SWPopupBase prefab)
        {
            prefab = null;

            if (!TryGetEntry(key, out Entry entry)) return false;

            prefab = entry.prefab;
            return true;
        }
        #endregion // 조회
    }
}

