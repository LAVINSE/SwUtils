using DG.Tweening;
using UnityEngine;

namespace SWUtils
{
    /// <summary>
    /// 팝업 표시 시 작게 시작해 커졌다가 원래 크기로 돌아오는 기본 표시 연출입니다.
    /// </summary>
    [CreateAssetMenu(menuName = "SWUtils/Popup Show Effects/Scale", fileName = "SWPopupScaleShowEffect")]
    public class SWPopupScaleShowEffect : SWPopupShowEffect
    {
        #region 필드
        [SerializeField] private float delay;
        [SerializeField] private float showUpDuration = 0.2f;
        [SerializeField] private float showDownDuration = 0.1f;
        [SerializeField] private Vector3 startScale = new(0.1f, 0.1f, 0.1f);
        [SerializeField] private float showUpScale = 1.1f;
        #endregion // 필드

        #region 재생
        /// <inheritdoc/>
        public override Tween Play(SWPopupBase popup, Transform target)
        {
            if (target == null) return null;

            target.gameObject.SetActive(true);
            target.DOKill();
            target.localScale = startScale;

            Sequence sequence = DOTween.Sequence();
            sequence.SetDelay(delay);
            sequence.Append(target.DOScale(showUpScale, Mathf.Max(0f, showUpDuration)).SetUpdate(true));
            sequence.Append(target.DOScale(1f, Mathf.Max(0f, showDownDuration)).SetUpdate(true));
            sequence.SetUpdate(true);
            return sequence;
        }
        #endregion // 재생
    }
}
