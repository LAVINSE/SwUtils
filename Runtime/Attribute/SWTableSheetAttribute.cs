using System;

namespace SWTools
{
    /// <summary>
    /// ScriptableObject 안의 리스트/배열 필드와 TSV 시트명을 연결하는 어트리뷰트.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class SWTableSheetAttribute : Attribute
    {
        #region 필드
        /// <summary>
        /// 매핑할 시트명.
        /// </summary>
        public readonly string SheetName;
        #endregion // 필드

        #region 초기화
        public SWTableSheetAttribute(string sheetName)
        {
            SheetName = sheetName;
        }
        #endregion // 초기화
    }
}
