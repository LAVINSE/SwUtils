using System.Collections;
using UnityEngine;

namespace SW.Popup
{
    /// <summary>
    /// 팝업 표시 시 작게 시작해 커졌다가 원래 크기로 돌아오는 기본 표시 연출입니다.
    /// </summary>
    /// <remarks>
    /// 코루틴으로 크기를 보간하며 Time.timeScale의 영향을 받지 않습니다.
    /// </remarks>
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
        public override SWPopupEffectHandle Play(SWPopupBase popup, Transform target)
        {
            if (target == null) return null;

            target.gameObject.SetActive(true);
            target.localScale = startScale;

            return SWPopupEffectHandle.Run(popup, PlayRoutine(target));
        }

        /// <summary>
        /// 지정한 시간만큼 대기한 뒤 크기를 키웠다가 기본 크기로 되돌립니다.
        /// </summary>
        /// <param name="target">연출 대상 Transform입니다.</param>
        private IEnumerator PlayRoutine(Transform target)
        {
            if (delay > 0f)
                yield return SWPopupEffectRoutines.WaitRealtime(delay);

            yield return SWPopupEffectRoutines.ScaleTo(
                target, Vector3.one * showUpScale, Mathf.Max(0f, showUpDuration));

            yield return SWPopupEffectRoutines.ScaleTo(
                target, Vector3.one, Mathf.Max(0f, showDownDuration));
        }
        #endregion // 재생
    }
}
