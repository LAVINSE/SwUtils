using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SWUtils
{
    public class SWUtilsFactory : MonoBehaviour
    {
        #region 함수
        /// <summary>
        /// 게임 객체를 생성한다
        /// </summary>
        /// <param name="objName">객체 이름</param>
        /// <param name="parentObj">부모 객체</param>
        /// <param name="pos">위치</param>
        /// <param name="scale">크기</param>
        /// <param name="rotate">회전</param>
        /// <param name="isStayWorldPos"></param>
        /// <returns></returns>
        public static GameObject CreateGameObj(string objName,
            GameObject parentObj, Vector3 pos, Vector3 scale, Vector3 rotate,
            bool isStayWorldPos = false)
        {
            var oGameObj = new GameObject(objName);
            oGameObj.transform.SetParent(parentObj?.transform, isStayWorldPos);

            oGameObj.transform.localScale = scale;
            oGameObj.transform.localPosition = pos;
            oGameObj.transform.localEulerAngles = rotate;

            return oGameObj;
        }

        /// <summary>
        /// 사본 객체를 생성한다
        /// </summary>
        /// <param name="objName">객체 이름</param>
        /// <param name="parentObj">부모 객체</param>
        /// <param name="pos">위치</param>
        /// <param name="scale">크기</param>
        /// <param name="rotate">회전</param>
        /// <param name="isStayWorldPos"></param>
        /// <returns></returns>
        public static GameObject CreateCloneGameObj(string objName,
            GameObject prefabObj, GameObject parentObj, Vector3 pos,
            Vector3 scale, Vector3 rotate, bool isStayWorldPos = false)
        {
            var oGameObj = GameObject.Instantiate(prefabObj, Vector3.zero, Quaternion.identity);
            oGameObj.name = objName;
            oGameObj.transform.SetParent(parentObj?.transform, isStayWorldPos);

            oGameObj.transform.localScale = scale;
            oGameObj.transform.localPosition = pos;
            oGameObj.transform.localEulerAngles = rotate;

            return oGameObj;
        }

        /// <summary>
        /// 게임 객체를 생성한다 <제네릭>
        /// </summary>
        /// <param name="objName">객체 이름</param>
        /// <param name="parentObj">부모 객체</param>
        /// <param name="pos">위치</param>
        /// <param name="scale">크기</param>
        /// <param name="rotate">회전</param>
        /// <param name="isStayWorldPos"></param>
        public static T CreateGameObj<T>(string objName,
            GameObject parentObj, Vector3 pos, Vector3 scale, Vector3 rotate,
            bool isStayWorldPos = false) where T : Component
        {
            var oGameObject = SWUtilsFactory.CreateGameObj(objName,
                parentObj, pos, scale, rotate, isStayWorldPos);

            return oGameObject.GetComponent<T>() ?? oGameObject.AddComponent<T>();
        }

        /// <summary>
        /// 사본 객체를 생성한다 <제네릭>
        /// </summary>
        /// <param name="objName">객체 이름</param>
        /// <param name="parentObj">부모 객체</param>
        /// <param name="pos">위치</param>
        /// <param name="scale">크기</param>
        /// <param name="rotate">회전</param>
        /// <param name="isStayWorldPos"></param>
        public static T CreateCloneGameObj<T>(string objName,
            GameObject prefabObj, GameObject parentObj, Vector3 pos,
            Vector3 scale, Vector3 rotate,
            bool isStayWorldPos = false) where T : Component
        {
            var oGameObject = SWUtilsFactory.CreateCloneGameObj(objName,
                prefabObj, parentObj, pos, scale, rotate, isStayWorldPos);

            return oGameObject.GetComponent<T>() ?? oGameObject.AddComponent<T>();
        }
        #endregion // 함수
    }
}