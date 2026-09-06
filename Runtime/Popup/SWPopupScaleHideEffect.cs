using System.Collections;
using UnityEngine;

namespace SW.Popup
{
    /// <summary>
    /// 팝업 숨김 시 원래 크기에서 살짝 커졌다가 작아지는 기본 숨김 연출입니다.
    /// </summary>
    /// <remarks>
    /// 코루틴으로 크기를 보간하며 Time.timeScale의 영향을 받지 않습니다.
    /// </remarks>
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
        public override SWPopupEffectHandle Play(SWPopupBase popup, Transform target)
        {
            if (target == null) return null;

            target.localScale = Vector3.one;

            return SWPopupEffectHandle.Run(popup, PlayRoutine(target));
        }

        /// <summary>
        /// 지정한 시간만큼 대기한 뒤 중간 크기를 거쳐 최종 크기로 변경합니다.
        /// </summary>
        /// <param name="target">연출 대상 Transform입니다.</param>
        private IEnumerator PlayRoutine(Transform target)
        {
            if (delay > 0f)
                yield return SWPopupEffectRoutines.WaitRealtime(delay);

            yield return SWPopupEffectRoutines.ScaleTo(
                target, Vector3.one * scaleUp, Mathf.Max(0f, scaleUpDuration));

            yield return SWPopupEffectRoutines.ScaleTo(
                target, Vector3.one * endScale, Mathf.Max(0f, scaleDownDuration));
        }
        #endregion // 재생
    }
}
