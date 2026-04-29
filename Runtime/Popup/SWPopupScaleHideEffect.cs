using DG.Tweening;
using UnityEngine;

namespace SWUtils
{
    /// <summary>
    /// 팝업 숨김 시 원래 크기에서 작아지는 기본 숨김 연출입니다.
    /// </summary>
    [CreateAssetMenu(menuName = "SWUtils/Popup Hide Effects/Scale", fileName = "SWPopupScaleHideEffect")]
    public class SWPopupScaleHideEffect : SWPopupHideEffect
    {
        #region 필드
        [SerializeField] private float delay;
        [SerializeField] private float scaleUpDuration = 0.1f;
        [SerializeField] private float scaleDownDuration = 0.25f;
        [SerializeField] private float scaleUp = 1f;
        [SerializeField] private float endScale;
        #endregion // 필드

        #region 재생
        /// <inheritdoc/>
        public override Tween Play(SWPopupBase popup, Transform target)
        {
            if (target == null) return null;

            target.DOKill();
            target.localScale = Vector3.one;

            Sequence sequence = DOTween.Sequence();
            sequence.SetDelay(delay);
            sequence.Append(target.DOScale(scaleUp, Mathf.Max(0f, scaleUpDuration)).SetUpdate(true));
            sequence.Append(target.DOScale(endScale, Mathf.Max(0f, scaleDownDuration)).SetUpdate(true));
            sequence.SetUpdate(true);
            return sequence;
        }
        #endregion // 재생
    }
}
