using UnityEngine;

namespace SWUtils
{
    public static class SWUtilsExtension
    {
        #region 실수
        /// <summary>
        /// 작음 여부를 검사한다
        /// </summary>
        /// <param name="a">비교 대상 값</param>
        /// <param name="b">기준 값</param>
        /// <returns>a가 b보다 작으면 true</returns>
        public static bool ExIsLess(this float a, float b)
        {
            return a < b - float.Epsilon;
        }

        /// <summary>
        /// 같음 여부를 검사한다
        /// </summary>
        /// <param name="a">비교 대상 값</param>
        /// <param name="b">기준 값</param>
        /// <returns>a와 b가 근사적으로 같으면 true</returns>
        public static bool ExIsEquals(this float a, float b)
        {
            return Mathf.Approximately(a, b);
        }

        /// <summary>
        /// 작거나 같음 여부를 검사한다
        /// </summary>
        /// <param name="a">비교 대상 값</param>
        /// <param name="b">기준 값</param>
        /// <returns>a가 b보다 작거나 같으면 true</returns>
        public static bool ExIsLessEquals(this float a, float b)
        {
            return a.ExIsLess(b) || a.ExIsEquals(b);
        }

        /// <summary>
        /// 큰 여부를 검사한다
        /// </summary>
        /// <param name="a">비교 대상 값</param>
        /// <param name="b">기준 값</param>
        /// <returns>a가 b보다 크면 true</returns>
        public static bool ExIsGreat(this float a, float b)
        {
            return a > b + float.Epsilon;
        }

        /// <summary>
        /// 크거나 같음 여부를 검사한다
        /// </summary>
        /// <param name="a">비교 대상 값</param>
        /// <param name="b">기준 값</param>
        /// <returns>a가 b보다 크거나 같으면 true</returns>
        public static bool ExIsGreatEquals(this float a, float b)
        {
            return a.ExIsGreat(b) || a.ExIsEquals(b);
        }
        #endregion // 실수

        #region 벡터
        /// <summary>
        /// 월드 좌표를 로컬 좌표로 변환한다
        /// </summary>
        /// <param name="posA">변환할 월드 좌표</param>
        /// <param name="parentObj">기준 부모 오브젝트</param>
        /// <param name="isCoord">좌표(true) 또는 방향 벡터(false) 여부</param>
        /// <returns>변환된 로컬 좌표</returns>
        public static Vector3 ExToLocal(this Vector3 posA, GameObject parentObj, bool isCoord = true)
        {
            var vector4 = new Vector4(posA.x, posA.y, posA.z, isCoord ? 1.0f : 0.0f);
            return parentObj.transform.worldToLocalMatrix * vector4;
        }

        /// <summary>
        /// 로컬 좌표를 월드 좌표로 변환한다
        /// </summary>
        /// <param name="posA">변환할 로컬 좌표</param>
        /// <param name="parentObj">기준 부모 오브젝트</param>
        /// <param name="isCoord">좌표(true) 또는 방향 벡터(false) 여부</param>
        /// <returns>변환된 월드 좌표</returns>
        public static Vector3 ExToWorld(this Vector3 posA, GameObject parentObj, bool isCoord = true)
        {
            var stVec4 = new Vector4(posA.x, posA.y, posA.z, isCoord ? 1.0f : 0.0f);
            return parentObj.transform.localToWorldMatrix * stVec4;
        }

        /// <summary>
        /// X, Y, Z 좌표를 선택적으로 변경한다
        /// </summary>
        /// <param name="pos">원본 벡터</param>
        /// <param name="x">변경할 X 값 (null이면 유지)</param>
        /// <param name="y">변경할 Y 값 (null이면 유지)</param>
        /// <param name="z">변경할 Z 값 (null이면 유지)</param>
        /// <returns>선택적으로 변경된 벡터</returns>
        public static Vector3 With(this Vector3 pos, float? x = null, float? y = null, float? z = null)
        {
            pos.x = x != null ? (float)x : pos.x;
            pos.y = y != null ? (float)y : pos.y;
            pos.z = z != null ? (float)z : pos.z;

            return pos;
        }
        #endregion // 벡터

        #region 카메라
        /// <summary>
        /// 수평 FOV를 수직 FOV로 변환하여 카메라에 적용한다
        /// </summary>
        /// <param name="camera">적용할 카메라</param>
        /// <param name="horizontalFov">설정할 수평 시야각 (도 단위)</param>
        public static void SetHorizontalFov(this Camera camera, float horizontalFov)
        {
            float verticalFov = 2f * Mathf.Atan(
                Mathf.Tan(horizontalFov * Mathf.Deg2Rad / 2f) / camera.aspect
            ) * Mathf.Rad2Deg;

            camera.fieldOfView = verticalFov;
        }
        #endregion // 카메라
    }
}