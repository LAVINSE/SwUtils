using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SWUtils
{
    public class SwUtilsTriggerDispatcher : MonoBehaviour
    {
        #region 프로퍼티
        public System.Action<SwUtilsTriggerDispatcher, Collider> EnterCallback { get; set; } = null;
        public System.Action<SwUtilsTriggerDispatcher, Collider> StayCallback { get; set; } = null;
        public System.Action<SwUtilsTriggerDispatcher, Collider> ExitCallback { get; set; } = null;

        public System.Action<SwUtilsTriggerDispatcher, Collider2D> Enter2DCallback { get; set; } = null;
        public System.Action<SwUtilsTriggerDispatcher, Collider2D> Stay2DCallback { get; set; } = null;
        public System.Action<SwUtilsTriggerDispatcher, Collider2D> Exit2DCallback { get; set; } = null;
        #endregion // 프로퍼티


        #region 함수
        /** 3D 트리거 접촉했을때 */
        public void OnTriggerEnter(Collider collider)
        {
            this.EnterCallback?.Invoke(this, collider);
        }

        /** 3D 트리거 접촉중일때 */
        public void OnTriggerStay(Collider collider)
        {
            this.StayCallback?.Invoke(this, collider);
        }

        /** 3D 트리거 접촉끝났을때 */
        public void OnTriggerExit(Collider collider)
        {
            this.ExitCallback?.Invoke(this, collider);
        }

        /** 2D 트리거 접촉했을때*/
        public void OnTriggerEnter2D(Collider2D collider2D)
        {
            this.Enter2DCallback?.Invoke(this, collider2D);
        }

        /** 2D 트리거 접촉중일때 */
        public void OnTriggerStay2D(Collider2D collider2D)
        {
            this.Stay2DCallback?.Invoke(this, collider2D);
        }

        /** 2D 트리거 접촉끝났을때 */
        public void OnTriggerExit2D(Collider2D collider2D)
        {
            this.Exit2DCallback?.Invoke(this, collider2D);
        }
        #endregion // 함수
    }
}