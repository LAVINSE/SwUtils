using UnityEngine;

namespace SWUtils
{
    public static class SWUtilsString
    {
        /** 정규식 => 문자열에서 처음등장하는 숫자 반환 */
        public static int GetFirstStringNumber(string str)
        {
            var match = System.Text.RegularExpressions.Regex.Match(str, @"\d+");

            if (match.Success && int.TryParse(match.Value, out int result))
            {
                return result;
            }

            return 0;
        }
    }
}