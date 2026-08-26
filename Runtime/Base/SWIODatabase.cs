using System.Collections.Generic;
using UnityEngine;

using SW.Util;

namespace SW.Base
{
    /// <summary>
    /// SWIdentifiedObject 목록을 관리하는 데이터베이스 에셋.
    /// </summary>
    [CreateAssetMenu(fileName = "SWIODatabase", menuName = "SWBase/IO Database")]
    public class SWIODatabase : SWScriptableObject
    {
        #region 필드
        [SerializeField] private List<SWIdentifiedObject> datas = new();

        /// <summary>코드명 조회용</summary>
        private Dictionary<string, SWIdentifiedObject> codeNameMap;
        /// <summary>ID 조회용</summary>
        private Dictionary<int, SWIdentifiedObject> idMap;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>등록된 데이터 목록</summary>
        public IReadOnlyList<SWIdentifiedObject> Datas => datas;

        /// <summary>등록된 데이터 수</summary>
        public int Count => datas.Count;

        /// <summary>인덱스로 데이터 접근</summary>
        public SWIdentifiedObject this[int index] => datas[index];
        #endregion // 프로퍼티

        #region 에디터
#if UNITY_EDITOR
        private void OnValidate()
        {
            InvalidateCache();
        }

        /// <summary>
        /// 데이터의 Id 필드를 설정합니다.
        /// </summary>
        /// <param name="target">ID를 설정할 데이터</param>
        /// <param name="id">설정할 ID</param>
        private void SetID(SWIdentifiedObject target, int id)
        {
            UnityEditor.SerializedObject serializedTarget = new(target);
            serializedTarget.FindProperty("id").intValue = id;
            serializedTarget.ApplyModifiedPropertiesWithoutUndo();
            MarkDirty(target);
        }

#endif // UNITY_EDITOR

        /// <summary>
        /// 에디터에서 직렬화 변경을 알립니다. 플레이어 빌드에서는 아무 일도 하지 않습니다.
        /// </summary>
        /// <param name="target">변경된 오브젝트</param>
        private void MarkDirty(UnityEngine.Object target)
        {
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(target);
#endif // UNITY_EDITOR
        }
        #endregion // 에디터

        #region 목록 관리
        /// <summary>
        /// 데이터를 추가한다
        /// ID가 0이면 현재 최대 ID + 1을 부여.
        /// </summary>
        /// <param name="newData">추가할 데이터</param>
        public void Add(SWIdentifiedObject newData)
        {
            if (newData == null)
            {
                SWLog.LogError($"[SWIODatabase] Add 실패: 데이터가 null입니다. ({name})");
                return;
            }

            if (datas.Contains(newData))
            {
                SWLog.LogError($"[SWIODatabase] Add 실패: 이미 등록된 데이터입니다. ({newData.name})");
                return;
            }

            datas.Add(newData);

            if (newData.ID == 0)
            {
#if UNITY_EDITOR
                SetID(newData, GetNextID());
#endif // UNITY_EDITOR
            }

            InvalidateCache();
            MarkDirty(this);
        }

        /// <summary>
        /// 데이터를 제거합니다
        /// 다른 데이터의 ID는 변경되지 않습니다.
        /// </summary>
        /// <param name="data">제거할 데이터</param>
        /// <returns>제거했으면 true</returns>
        public bool Remove(SWIdentifiedObject data)
        {
            bool removed = datas.Remove(data);

            if (removed)
            {
                InvalidateCache();
                MarkDirty(this);
            }

            return removed;
        }

        /// <summary>
        /// 데이터를 코드명 기준 오름차순으로 정렬한다
        /// ID는 변경되지 않습니다.
        /// </summary>
        public void SortByCodeName()
        {
            datas.Sort((left, right) => string.Compare(left != null ? left.CodeName : string.Empty, right != null ? right.CodeName : string.Empty, System.StringComparison.Ordinal));

            MarkDirty(this);
        }

        /// <summary>
        /// 등록 여부를 확인합니다.
        /// </summary>
        /// <param name="data">확인할 데이터</param>
        /// <returns>등록되어 있으면 true</returns>
        public bool Contains(SWIdentifiedObject data)
            => datas.Contains(data);

        /// <summary>
        /// 목록에서 null항목을 제거합니다.
        /// </summary>
        /// <returns>제거된 항목 수</returns>
        public int RemoveNullEntries()
        {
            int removedCount = datas.RemoveAll(x => x == null);

            if (removedCount > 0)
            {
                InvalidateCache();
                MarkDirty(this);
            }

            return removedCount;
        }
        #endregion // 목록 관리

        #region 조회
        /// <summary>
        /// ID로 데이터를 찾습니다.
        /// </summary>
        /// <param name="id">찾을 ID</param>
        /// <returns>찾은 데이터, 없으면 null</returns>
        public SWIdentifiedObject GetDataByID(int id)
        {
            EnsureCache();
            return idMap.TryGetValue(id, out SWIdentifiedObject data) ? data : null;
        }

        /// <summary>
        /// ID로 데이터를 찾아 지정 타입으로 반환합니다.
        /// </summary>
        /// <typeparam name="T">반환 타입</typeparam>
        /// <param name="id">찾을 ID</param>
        /// <returns>찾은 데이터, 없거나 타입이 다르면 null</returns>
        public T GetDataByID<T>(int id) where T : SWIdentifiedObject
            => GetDataByID(id) as T;

        /// <summary>
        /// ID로 데이터를 찾습니다.
        /// </summary>
        /// <param name="id">찾을 ID</param>
        /// <param name="data">찾은 데이터</param>
        /// <returns>찾았으면 true</returns>
        public bool TryGetDataByID(int id, out SWIdentifiedObject data)
        {
            data = GetDataByID(id);
            return data != null;
        }

        /// <summary>
        /// 코드명으로 데이터를 찾습니다.
        /// </summary>
        /// <param name="codeName">찾을 코드명</param>
        /// <returns>찾은 데이터, 없으면 null</returns>
        public SWIdentifiedObject GetDataByCodeName(string codeName)
        {
            if (string.IsNullOrEmpty(codeName))
            {
                return null;
            }

            EnsureCache();
            return codeNameMap.TryGetValue(codeName, out SWIdentifiedObject data) ? data : null;
        }

        /// <summary>
        /// 코드명으로 데이터를 찾아 지정 타입으로 반환합니다.
        /// </summary>
        /// <typeparam name="T">반환 타입</typeparam>
        /// <param name="codeName">찾을 코드명</param>
        /// <returns>찾은 데이터, 없거나 타입이 다르면 null</returns>
        public T GetDataByCodeName<T>(string codeName) where T : SWIdentifiedObject
            => GetDataByCodeName(codeName) as T;

        /// <summary>
        /// 코드명으로 데이터를 찾습니다.
        /// </summary>
        /// <param name="codeName">찾을 코드명</param>
        /// <param name="data">찾은 데이터</param>
        /// <returns>찾았으면 true</returns>
        public bool TryGetDataByCodeName(string codeName, out SWIdentifiedObject data)
        {
            data = GetDataByCodeName(codeName);
            return data != null;
        }
        #endregion // 조회

        #region ID 관리
        /// <summary>
        /// 현재 최대 ID + 1을 반환합니다.
        /// </summary>
        /// <returns>다음에 부여할 ID</returns>
        public int GetNextID()
        {
            int maxId = 0;

            for (int i = 0; i < datas.Count; i++)
            {
                if (datas[i] != null && datas[i].ID > maxId)
                {
                    maxId = datas[i].ID;
                }
            }

            return maxId + 1;
        }

        /// <summary>
        /// 모든 데이터의 ID를 현재 인덱스 순서로 재설정합니다.
        /// </summary>
        public void ReorderIdsByIndex()
        {
            for (int i = 0; i < datas.Count; i++)
            {
                if (datas[i] != null)
                {
#if UNITY_EDITOR
                    SetID(datas[i], i);
#endif // UNITY_EDITOR
                }
            }

            InvalidateCache();
            MarkDirty(this);
        }
        #endregion // ID 관리

        /// <summary>
        /// 조회 캐시가 없으면 생성합니다.
        /// </summary>
        private void EnsureCache()
        {
            if (idMap != null && codeNameMap != null)
            {
                return;
            }

            idMap = new(datas.Count);
            codeNameMap = new(datas.Count);

            for (int i = 0; i < datas.Count; i++)
            {
                SWIdentifiedObject data = datas[i];

                if (data == null)
                {
                    continue;
                }

                if (!idMap.TryAdd(data.ID, data))
                {
                    SWLog.LogError($"[SWIODatabase] 중복 ID입니다: {data.ID} ({data.name}) - {name}");
                }

                if (!string.IsNullOrEmpty(data.CodeName) && !codeNameMap.TryAdd(data.CodeName, data))
                {
                    SWLog.LogError($"[SWIODatabase] 중복 코드명입니다: {data.CodeName} ({data.name}) - {name}");
                }
            }
        }

        /// <summary>
        /// 조회 캐시를 무효화합니다.
        /// </summary>
        private void InvalidateCache()
        {
            idMap = null;
            codeNameMap = null;
        }
        
        
    }
}
