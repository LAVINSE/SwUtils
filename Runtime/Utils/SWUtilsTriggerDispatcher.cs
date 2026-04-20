using UnityEngine;

namespace SWUtils
{
    /// <summary>
    /// 2D/3D 트리거 콜라이더 이벤트를 콜백으로 전달하는 디스패처.
    /// OnTriggerEnter/Stay/Exit 이벤트를 외부에서 구독할 수 있게 한다.
    /// </summary>
    public class SWUtilsTriggerDispatcher : MonoBehaviour
    {
        #region 프로퍼티
        /// <summary>3D 트리거 Enter 콜백.</summary>
        public System.Action<SWUtilsTriggerDispatcher, Collider> EnterCallback { get; set; } = null;
        /// <summary>3D 트리거 Stay 콜백.</summary>
        public System.Action<SWUtilsTriggerDispatcher, Collider> StayCallback { get; set; } = null;
        /// <summary>3D 트리거 Exit 콜백.</summary>
        public System.Action<SWUtilsTriggerDispatcher, Collider> ExitCallback { get; set; } = null;

        /// <summary>2D 트리거 Enter 콜백.</summary>
        public System.Action<SWUtilsTriggerDispatcher, Collider2D> Enter2DCallback { get; set; } = null;
        /// <summary>2D 트리거 Stay 콜백.</summary>
        public System.Action<SWUtilsTriggerDispatcher, Collider2D> Stay2DCallback { get; set; } = null;
        /// <summary>2D 트리거 Exit 콜백.</summary>
        public System.Action<SWUtilsTriggerDispatcher, Collider2D> Exit2DCallback { get; set; } = null;
        #endregion // 프로퍼티

        #region 함수
        /// <summary>
        /// 3D 트리거에 접촉했을 때 호출된다.
        /// </summary>
        /// <param name="collider">접촉한 콜라이더</param>
        public void OnTriggerEnter(Collider collider)
        {
            this.EnterCallback?.Invoke(this, collider);
        }

        /// <summary>
        /// 3D 트리거에 접촉 중일 때 매 프레임 호출된다.
        /// </summary>
        /// <param name="collider">접촉 중인 콜라이더</param>
        public void OnTriggerStay(Collider collider)
        {
            this.StayCallback?.Invoke(this, collider);
        }

        /// <summary>
        /// 3D 트리거 접촉이 끝났을 때 호출된다.
        /// </summary>
        /// <param name="collider">이탈한 콜라이더</param>
        public void OnTriggerExit(Collider collider)
        {
            this.ExitCallback?.Invoke(this, collider);
        }

        /// <summary>
        /// 2D 트리거에 접촉했을 때 호출된다.
        /// </summary>
        /// <param name="collider2D">접촉한 2D 콜라이더</param>
        public void OnTriggerEnter2D(Collider2D collider2D)
        {
            this.Enter2DCallback?.Invoke(this, collider2D);
        }

        /// <summary>
        /// 2D 트리거에 접촉 중일 때 매 프레임 호출된다.
        /// </summary>
        /// <param name="collider2D">접촉 중인 2D 콜라이더</param>
        public void OnTriggerStay2D(Collider2D collider2D)
        {
            this.Stay2DCallback?.Invoke(this, collider2D);
        }

        /// <summary>
        /// 2D 트리거 접촉이 끝났을 때 호출된다.
        /// </summary>
        /// <param name="collider2D">이탈한 2D 콜라이더</param>
        public void OnTriggerExit2D(Collider2D collider2D)
        {
            this.Exit2DCallback?.Invoke(this, collider2D);
        }
        #endregion // 함수
    }
}