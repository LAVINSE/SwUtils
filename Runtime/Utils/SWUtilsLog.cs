using UnityEngine;

namespace SWUtils
{
        /// <summary>
        /// SW_DEBUG_MODE 디파인이 설정된 경우에만 동작하는 로그 유틸리티.
        /// 릴리즈 빌드에서는 Conditional 속성으로 호출 자체가 제거된다.
        /// </summary>
        public static class SWUtilsLog
        {
                #region 함수
                /// <summary>
                /// 일반 로그를 출력한다. (SW_DEBUG_MODE 디파인 시에만 동작)
                /// </summary>
                /// <param name="message">출력할 메시지</param>
                [System.Diagnostics.Conditional("SW_DEBUG_MODE")]
                public static void Log(object message)
                {
#if SW_DEBUG_MODE
            Debug.Log(message);
#endif // SW_DEBUG_MODE
                }

                /// <summary>
                /// 경고 로그를 출력한다. (SW_DEBUG_MODE 디파인 시에만 동작)
                /// </summary>
                /// <param name="message">출력할 메시지</param>
                [System.Diagnostics.Conditional("SW_DEBUG_MODE")]
                public static void LogWarning(object message)
                {
#if SW_DEBUG_MODE
            Debug.LogWarning(message);
#endif // SW_DEBUG_MODE
                }

                /// <summary>
                /// 에러 로그를 출력한다. (SW_DEBUG_MODE 디파인 시에만 동작)
                /// </summary>
                /// <param name="message">출력할 메시지</param>
                [System.Diagnostics.Conditional("SW_DEBUG_MODE")]
                public static void LogError(object message)
                {
#if SW_DEBUG_MODE
            Debug.LogError(message);
#endif // SW_DEBUG_MODE
                }
                #endregion // 함수
        }
}
