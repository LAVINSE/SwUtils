using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SWTools
{
    /// <summary>
    /// 인스펙터 그룹의 데이터를 저장하는 클래스
    /// </summary>
    public class SWGroupDataEditor
    {
        #region 필드
        /// <summary>
        /// 그룹을 정의한 어트리뷰트입니다.
        /// </summary>
        public SWGroupAttribute GroupAttribute;

        /// <summary>
        /// true일 경우 : 펼쳐진 상태
        /// false일 경우 : 접힌 상태
        /// </summary>
        public bool IsGroupOpen;

        /// <summary>
        /// 그룹에 속한 프로퍼티 리스트
        /// </summary>
        public List<SerializedProperty> PropertiesList = new();

        /// <summary>
        /// 빠른 검색용 HashSet
        /// </summary>
        public HashSet<string> GroupHashSet = new();

        /// <summary>
        /// 그룹의 표시 색상
        /// </summary>
        public Color GroupColor;

        #endregion // 필드

        #region 프로퍼티
        #endregion // 프로퍼티

        /// <summary>
        /// 그룹의 모든 데이터 초기화
        /// </summary>
        public void ClearGroup()
        {
            GroupAttribute = null;
            GroupHashSet.Clear();
            PropertiesList.Clear();
        }
    }
}
