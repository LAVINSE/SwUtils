using System;
using System.Collections;
using UnityEngine;

namespace SW.Popup
{
    /// <summary>
    /// 팝업 연출 코루틴을 제어하는 경량 핸들입니다. (DOTween Tween 대체)
    /// </summary>
    /// <remarks>
    /// Kill은 완료 콜백 없이 즉시 중단하고, Complete는 완료 콜백을 호출하며 중단합니다.
    /// DOTween의 Kill(false) / Kill(true)와 동일한 의미를 가집니다.
    /// </remarks>
    public sealed class SWPopupEffectHandle
    {
        #region 필드
        private MonoBehaviour owner;
        private Coroutine coroutine;
        private Action onComplete;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>연출이 재생 중인지 여부입니다.</summary>
        public bool IsPlaying { get; private set; }
        #endregion // 프로퍼티

        #region 함수
        /// <summary>
        /// 지정한 MonoBehaviour에서 연출 코루틴을 시작하고 핸들을 반환합니다.
        /// </summary>
        /// <param name="owner">코루틴을 실행할 MonoBehaviour입니다.</param>
        /// <param name="routine">재생할 연출 코루틴입니다.</param>
        /// <returns>생성된 핸들. owner 또는 routine이 없으면 null입니다.</returns>
        public static SWPopupEffectHandle Run(MonoBehaviour owner, IEnumerator routine)
        {
            if (owner == null || routine == null) return null;
            if (!owner.isActiveAndEnabled) return null;

            SWPopupEffectHandle handle = new()
            {
                owner = owner,
                IsPlaying = true
            };

            handle.coroutine = owner.StartCoroutine(handle.WrapRoutine(routine));
            return handle;
        }

        /// <summary>
        /// 연출 완료 시 호출할 콜백을 등록합니다. 이미 완료된 상태면 즉시 호출됩니다.
        /// </summary>
        /// <param name="callback">완료 콜백입니다.</param>
        /// <returns>체이닝용 자기 자신입니다.</returns>
        public SWPopupEffectHandle OnComplete(Action callback)
        {
            if (callback == null) return this;

            if (!IsPlaying)
            {
                callback.Invoke();
                return this;
            }

            onComplete += callback;
            return this;
        }

        /// <summary>
        /// 연출을 완료 콜백 없이 즉시 중단합니다.
        /// </summary>
        public void Kill()
        {
            if (!IsPlaying) return;

            IsPlaying = false;
            StopRoutine();
            onComplete = null;
        }

        /// <summary>
        /// 연출을 중단하고 완료 콜백을 즉시 호출합니다.
        /// </summary>
        public void Complete()
        {
            if (!IsPlaying) return;

            StopRoutine();
            Finish();
        }

        /// <summary>
        /// 실행 중인 코루틴을 중지합니다.
        /// </summary>
        private void StopRoutine()
        {
            if (owner != null && coroutine != null)
                owner.StopCoroutine(coroutine);

            coroutine = null;
        }

        /// <summary>
        /// 연출 코루틴이 끝나면 재생 상태를 정리하고 완료 콜백을 호출합니다.
        /// </summary>
        /// <param name="routine">완료를 기다릴 연출 코루틴입니다.</param>
        private IEnumerator WrapRoutine(IEnumerator routine)
        {
            yield return routine;
            Finish();
        }

        /// <summary>
        /// 재생 상태를 정리하고 완료 콜백을 호출합니다.
        /// </summary>
        private void Finish()
        {
            IsPlaying = false;
            coroutine = null;

            Action callback = onComplete;
            onComplete = null;
            callback?.Invoke();
        }
        #endregion // 함수
    }

    /// <summary>
    /// 팝업 연출의 대기, 크기 변경, 페이드를 처리하는 코루틴입니다. Time.timeScale의 영향을 받지 않습니다.
    /// </summary>
    public static class SWPopupEffectRoutines
    {
        /// <summary>
        /// unscaled 시간 기준으로 지정 시간만큼 대기합니다.
        /// </summary>
        /// <param name="seconds">대기 시간(초)입니다.</param>
        public static IEnumerator WaitRealtime(float seconds)
        {
            if (seconds <= 0f) yield break;

            float elapsed = 0f;
            while (elapsed < seconds)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        /// <summary>
        /// 대상의 localScale을 unscaled 시간 기준으로 보간합니다.
        /// </summary>
        /// <param name="target">스케일을 변경할 Transform입니다.</param>
        /// <param name="endScale">목표 스케일입니다.</param>
        /// <param name="duration">보간 시간(초)입니다.</param>
        public static IEnumerator ScaleTo(Transform target, Vector3 endScale, float duration)
        {
            if (target == null) yield break;

            if (duration <= 0f)
            {
                target.localScale = endScale;
                yield break;
            }

            Vector3 startScale = target.localScale;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                if (target == null) yield break;

                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                target.localScale = Vector3.LerpUnclamped(startScale, endScale, progress);
                yield return null;
            }

            if (target != null)
                target.localScale = endScale;
        }

        /// <summary>
        /// CanvasGroup의 alpha를 unscaled 시간 기준으로 보간합니다.
        /// </summary>
        /// <param name="target">알파를 변경할 CanvasGroup입니다.</param>
        /// <param name="endAlpha">목표 알파입니다.</param>
        /// <param name="duration">보간 시간(초)입니다.</param>
        public static IEnumerator FadeTo(CanvasGroup target, float endAlpha, float duration)
        {
            if (target == null) yield break;

            if (duration <= 0f)
            {
                target.alpha = endAlpha;
                yield break;
            }

            float startAlpha = target.alpha;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                if (target == null) yield break;

                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                target.alpha = Mathf.LerpUnclamped(startAlpha, endAlpha, progress);
                yield return null;
            }

            if (target != null)
                target.alpha = endAlpha;
        }
    }
}
