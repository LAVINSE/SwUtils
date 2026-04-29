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
        /// <summary>
        /// 시트명을 지정해 시트 매핑 어트리뷰트를 생성합니다.
        /// </summary>
        /// <param name="sheetName">매핑할 시트명입니다.</param>
        public SWTableSheetAttribute(string sheetName)
        {
            SheetName = sheetName;
        }
        #endregion // 초기화
    }
}
