using System;

namespace SWTools
{
    /// <summary>
    /// 엑셀/TSV 컬럼명을 데이터 필드에 매핑하기 위한 어트리뷰트.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class SWTableAttribute : Attribute
    {
        #region 필드
        /// <summary>
        /// 매핑할 테이블 컬럼명.
        /// </summary>
        public readonly string ColumnName;

        /// <summary>
        /// 값이 비어 있으면 오류로 처리할지 여부.
        /// </summary>
        public bool Required { get; set; }
        #endregion // 필드

        #region 초기화
        /// <summary>
        /// 테이블 컬럼명을 지정해 컬럼 매핑 어트리뷰트를 생성합니다.
        /// </summary>
        /// <param name="columnName">매핑할 테이블 컬럼명입니다.</param>
        public SWTableAttribute(string columnName)
        {
            ColumnName = columnName;
        }
        #endregion // 초기화
    }
}
