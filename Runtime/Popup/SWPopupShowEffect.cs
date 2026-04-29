using DG.Tweening;
using UnityEngine;

namespace SWUtils
{
    /// <summary>
    /// 팝업이 표시될 때 재생되는 연출의 ScriptableObject 기반 클래스입니다.
    /// </summary>
    public abstract class SWPopupShowEffect : ScriptableObject
    {
        /// <summary>
        /// 팝업 표시 연출을 단일 대상에 재생합니다.
        /// </summary>
        /// <param name="popup">표시되는 팝업입니다.</param>
        /// <param name="target">연출을 적용할 Transform입니다.</param>
        /// <returns>재생된 Tween입니다. 연출이 없으면 null일 수 있습니다.</returns>
        public abstract Tween Play(SWPopupBase popup, Transform target);
    }
}
