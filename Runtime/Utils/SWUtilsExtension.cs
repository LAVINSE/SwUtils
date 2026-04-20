using UnityEngine;

namespace SWUtils
{
    /// <summary>
    /// 실수, 벡터, 카메라 등에 사용되는 확장 메서드 모음.
    /// </summary>
    public static class SWUtilsExtension
    {
        #region 실수
        /// <summary>
        /// 작음 여부를 검사한다.
        /// </summary>
        /// <param name="valueA">비교 대상 값</param>
        /// <param name="valueB">기준 값</param>
        /// <returns>valueA가 valueB보다 작으면 true</returns>
        public static bool ExIsLess(this float valueA, float valueB)
        {
            return valueA < valueB - float.Epsilon;
        }

        /// <summary>
        /// 같음 여부를 검사한다.
        /// </summary>
        /// <param name="valueA">비교 대상 값</param>
        /// <param name="valueB">기준 값</param>
        /// <returns>valueA와 valueB가 근사적으로 같으면 true</returns>
        public static bool ExIsEquals(this float valueA, float valueB)
        {
            return Mathf.Approximately(valueA, valueB);
        }

        /// <summary>
        /// 작거나 같음 여부를 검사한다.
        /// </summary>
        /// <param name="valueA">비교 대상 값</param>
        /// <param name="valueB">기준 값</param>
        /// <returns>valueA가 valueB보다 작거나 같으면 true</returns>
        public static bool ExIsLessEquals(this float valueA, float valueB)
        {
            return valueA.ExIsLess(valueB) || valueA.ExIsEquals(valueB);
        }

        /// <summary>
        /// 큰 여부를 검사한다.
        /// </summary>
        /// <param name="valueA">비교 대상 값</param>
        /// <param name="valueB">기준 값</param>
        /// <returns>valueA가 valueB보다 크면 true</returns>
        public static bool ExIsGreat(this float valueA, float valueB)
        {
            return valueA > valueB + float.Epsilon;
        }

        /// <summary>
        /// 크거나 같음 여부를 검사한다.
        /// </summary>
        /// <param name="valueA">비교 대상 값</param>
        /// <param name="valueB">기준 값</param>
        /// <returns>valueA가 valueB보다 크거나 같으면 true</returns>
        public static bool ExIsGreatEquals(this float valueA, float valueB)
        {
            return valueA.ExIsGreat(valueB) || valueA.ExIsEquals(valueB);
        }
        #endregion // 실수

        #region 벡터
        /// <summary>
        /// 월드 좌표를 로컬 좌표로 변환한다.
        /// </summary>
        /// <param name="worldPosition">변환할 월드 좌표</param>
        /// <param name="parentObject">기준 부모 오브젝트</param>
        /// <param name="isCoordinate">좌표(true) 또는 방향 벡터(false) 여부</param>
        /// <returns>변환된 로컬 좌표</returns>
        public static Vector3 ExToLocal(this Vector3 worldPosition, GameObject parentObject, bool isCoordinate = true)
        {
            var vector4 = new Vector4(worldPosition.x, worldPosition.y, worldPosition.z, isCoordinate ? 1.0f : 0.0f);
            return parentObject.transform.worldToLocalMatrix * vector4;
        }

        /// <summary>
        /// 로컬 좌표를 월드 좌표로 변환한다.
        /// </summary>
        /// <param name="localPosition">변환할 로컬 좌표</param>
        /// <param name="parentObject">기준 부모 오브젝트</param>
        /// <param name="isCoordinate">좌표(true) 또는 방향 벡터(false) 여부</param>
        /// <returns>변환된 월드 좌표</returns>
        public static Vector3 ExToWorld(this Vector3 localPosition, GameObject parentObject, bool isCoordinate = true)
        {
            var vector4 = new Vector4(localPosition.x, localPosition.y, localPosition.z, isCoordinate ? 1.0f : 0.0f);
            return parentObject.transform.localToWorldMatrix * vector4;
        }

        /// <summary>
        /// X, Y, Z 좌표를 선택적으로 변경한다.
        /// </summary>
        /// <param name="position">원본 벡터</param>
        /// <param name="x">변경할 X 값 (null이면 유지)</param>
        /// <param name="y">변경할 Y 값 (null이면 유지)</param>
        /// <param name="z">변경할 Z 값 (null이면 유지)</param>
        /// <returns>선택적으로 변경된 벡터</returns>
        public static Vector3 With(this Vector3 position, float? x = null, float? y = null, float? z = null)
        {
            position.x = x != null ? (float)x : position.x;
            position.y = y != null ? (float)y : position.y;
            position.z = z != null ? (float)z : position.z;

            return position;
        }
        #endregion // 벡터
    }
}