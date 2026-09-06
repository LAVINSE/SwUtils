using System.Globalization;
using UnityEditor;
using UnityEngine;

#if UNITY_6000_4_OR_NEWER
using SWObjectIdentifier = UnityEngine.EntityId;
#else
using SWObjectIdentifier = System.Int32;
#endif

namespace SW.EditorTools.Util
{
    /// <summary>
    /// Unity 버전에 맞는 오브젝트 식별자 조회와 변환을 제공합니다.
    /// </summary>
    internal static class SWEditorObjectUtility
    {
        #region 조회
        /// <summary>현재 에디터 세션에서 오브젝트를 구분하는 식별자를 반환합니다.</summary>
        /// <param name="target">식별자를 가져올 오브젝트입니다.</param>
        /// <returns>오브젝트 식별자입니다. 대상이 없으면 기본값을 반환합니다.</returns>
        internal static SWObjectIdentifier GetIdentifier(Object target)
        {
            if (target == null)
                return default;

#if UNITY_6000_4_OR_NEWER
            return target.GetEntityId();
#else
            return target.GetInstanceID();
#endif
        }

        /// <summary>현재 에디터 세션의 식별자로 오브젝트를 찾습니다.</summary>
        /// <param name="identifier">조회할 오브젝트 식별자입니다.</param>
        /// <returns>찾은 오브젝트입니다. 없으면 null을 반환합니다.</returns>
        internal static Object FindObject(SWObjectIdentifier identifier)
        {
#if UNITY_6000_4_OR_NEWER
            return EditorUtility.EntityIdToObject(identifier);
#else
            return EditorUtility.InstanceIDToObject(identifier);
#endif
        }
        #endregion // 조회

        #region 설정 키
        /// <summary>에디터 설정 키에 사용할 오브젝트 식별자 문자열을 반환합니다.</summary>
        /// <param name="target">설정 키를 생성할 오브젝트입니다.</param>
        /// <returns>식별자의 숫자 문자열입니다. 에디터 세션이 바뀌면 달라질 수 있습니다.</returns>
        internal static string GetPreferenceIdentifier(Object target)
        {
#if UNITY_6000_4_OR_NEWER
            return EntityId.ToULong(GetIdentifier(target)).ToString(CultureInfo.InvariantCulture);
#else
            return GetIdentifier(target).ToString(CultureInfo.InvariantCulture);
#endif
        }
        #endregion // 설정 키
    }
}
