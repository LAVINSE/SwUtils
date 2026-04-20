using UnityEngine;

namespace SWUtils
{
    /// <summary>
    /// 문자열 처리 관련 유틸리티.
    /// </summary>
    public static class SWUtilsString
    {
        #region 함수
        /// <summary>
        /// 정규식을 이용해 문자열에서 처음 등장하는 숫자를 반환한다.
        /// </summary>
        /// <param name="source">검색할 문자열</param>
        /// <returns>처음 등장하는 숫자. 없으면 0</returns>
        public static int GetFirstStringNumber(string source)
        {
            var match = System.Text.RegularExpressions.Regex.Match(source, @"\d+");

            if (match.Success && int.TryParse(match.Value, out int result))
            {
                return result;
            }

            return 0;
        }
        #endregion // 함수
    }
}