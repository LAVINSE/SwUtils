using UnityEngine;

namespace SWUtils
{
        /// <summary>
        /// DebugMode 디파인이 설정된 경우에만 동작하는 로그 유틸리티.
        /// 릴리즈 빌드에서는 Conditional 속성으로 호출 자체가 제거된다.
        /// </summary>
        public static class SWUtilsLog
        {
                #region 함수
                /// <summary>
                /// 일반 로그를 출력한다. (DebugMode 디파인 시에만 동작)
                /// </summary>
                /// <param name="message">출력할 메시지</param>
                [System.Diagnostics.Conditional("DebugMode")]
                public static void Log(object message)
                {
#if DebugMode
            Debug.Log(message);
#endif // DebugMode
                }

                /// <summary>
                /// 경고 로그를 출력한다. (DebugMode 디파인 시에만 동작)
                /// </summary>
                /// <param name="message">출력할 메시지</param>
                [System.Diagnostics.Conditional("DebugMode")]
                public static void LogWarning(object message)
                {
#if DebugMode
            Debug.LogWarning(message);
#endif // DebugMode
                }

                /// <summary>
                /// 에러 로그를 출력한다. (DebugMode 디파인 시에만 동작)
                /// </summary>
                /// <param name="message">출력할 메시지</param>
                [System.Diagnostics.Conditional("DebugMode")]
                public static void LogError(object message)
                {
#if DebugMode
            Debug.LogError(message);
#endif // DebugMode
                }
                #endregion // 함수
        }
}