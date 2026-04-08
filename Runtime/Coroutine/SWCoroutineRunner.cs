using System;
using System.Collections;
using System.Collections.Generic;
using SWTools;
using UnityEngine;

namespace SWCoroutine
{
    public class SWCoroutineRunner : SWMonoBehaviour, ICoroutineRunner
    {
        #region 필드
        private readonly Dictionary<float, WaitForSeconds> waitDict = new();
        private readonly Dictionary<float, WaitForSecondsRealtime> waitRealtimeCacheDict = new();
        private readonly WaitForEndOfFrame waitEndOfFrame = new();
        private readonly WaitForFixedUpdate waitFixedUpdate = new();
        #endregion // 필드

        #region 프로퍼티
        /// <summary>프레임 끝 대기 (캐시된 싱글 인스턴스).</summary>
        public WaitForEndOfFrame WaitEndOfFrame => waitEndOfFrame;
        /// <summary>FixedUpdate 대기 (캐시된 싱글 인스턴스).</summary>
        public WaitForFixedUpdate WaitFixedUpdate => waitFixedUpdate;
        #endregion // 프로퍼티

        #region 초기화
        private void OnDestroy()
        {
            StopAll();
            waitDict.Clear();
            waitRealtimeCacheDict.Clear();
        }
        #endregion // 초기화

        #region Wait 캐시
        /// <summary>
        /// 캐시된 WaitForSeconds를 반환한다. 없으면 생성하여 캐싱한다.
        /// </summary>
        /// <param name="seconds">대기 시간(초)</param>
        /// <returns>캐시된 WaitForSeconds 인스턴스</returns>
        public WaitForSeconds Wait(float seconds)
        {
            if (!waitDict.TryGetValue(seconds, out var wait))
            {
                wait = new WaitForSeconds(seconds);
                waitDict[seconds] = wait;
            }
            return wait;
        }

        /// <summary>
        /// 캐시된 WaitForSecondsRealtime을 반환한다. 없으면 생성하여 캐싱한다.
        /// </summary>
        /// <param name="seconds">대기 시간(초)</param>
        /// <returns>캐시된 WaitForSecondsRealtime 인스턴스</returns>
        public WaitForSecondsRealtime WaitRealtime(float seconds)
        {
            if (!waitRealtimeCacheDict.TryGetValue(seconds, out var wait))
            {
                wait = new WaitForSecondsRealtime(seconds);
                waitRealtimeCacheDict[seconds] = wait;
            }
            return wait;
        }
        #endregion // Wait 캐시

        #region 코루틴 실행
        /// <summary>
        /// 코루틴을 실행한다.
        /// </summary>
        /// <param name="routine">실행할 IEnumerator</param>
        /// <returns>제어용 Coroutine 핸들</returns>
        public Coroutine Run(IEnumerator routine)
        {
            return routine != null ? StartCoroutine(routine) : null;
        }

        /// <summary>
        /// 실행 중인 코루틴을 중단한다.
        /// </summary>
        /// <param name="routine">중단할 Coroutine 핸들</param>
        public void Stop(Coroutine routine)
        {
            if (routine != null) StopCoroutine(routine);
        }

        /// <summary>
        /// 모든 코루틴을 중단한다.
        /// </summary>
        public void StopAll()
        {
            StopAllCoroutines();
        }
        #endregion // 코루틴 실행

        #region 지연 실행
        /// <summary>
        /// 일정 시간 후 액션을 실행한다.
        /// </summary>
        /// <param name="delay">지연 시간(초)</param>
        /// <param name="action">실행할 액션</param>
        /// <returns>제어용 Coroutine 핸들</returns>
        public Coroutine DelayedCall(float delay, Action action)
        {
            return Run(DelayedCallRoutine(delay, action));
        }

        /// <summary>
        /// 다음 프레임에 액션을 실행한다.
        /// </summary>
        /// <param name="action">실행할 액션</param>
        /// <returns>제어용 Coroutine 핸들</returns>
        public Coroutine NextFrame(Action action)
        {
            return Run(NextFrameRoutine(action));
        }

        /// <summary>
        /// 프레임 끝에 액션을 실행한다.
        /// </summary>
        /// <param name="action">실행할 액션</param>
        /// <returns>제어용 Coroutine 핸들</returns>
        public Coroutine EndOfFrame(Action action)
        {
            return Run(EndOfFrameRoutine(action));
        }

        /// <summary>
        /// 조건이 참이 될 때까지 기다렸다가 액션을 실행한다.
        /// </summary>
        /// <param name="condition">평가할 조건</param>
        /// <param name="action">실행할 액션</param>
        /// <returns>제어용 Coroutine 핸들</returns>
        public Coroutine WaitUntil(Func<bool> condition, Action action)
        {
            return Run(WaitUntilRoutine(condition, action));
        }

        /// <summary>
        /// 지정한 횟수만큼 간격을 두고 액션을 반복 실행한다.
        /// </summary>
        /// <param name="interval">실행 간격(초)</param>
        /// <param name="count">반복 횟수. 0 이하면 무한 반복</param>
        /// <param name="action">실행할 액션. 인자는 현재 인덱스</param>
        /// <returns>제어용 Coroutine 핸들</returns>
        public Coroutine Repeat(float interval, int count, Action<int> action)
        {
            return Run(RepeatRoutine(interval, count, action));
        }

        /// <summary>
        /// duration 동안 0~1 값을 매 프레임 전달한다. 트윈/애니메이션에 사용.
        /// </summary>
        /// <param name="duration">전체 시간(초)</param>
        /// <param name="onUpdate">0~1 값을 받는 콜백</param>
        /// <param name="onComplete">완료 시 콜백</param>
        /// <param name="unscaled">true면 Time.timeScale 무시</param>
        /// <returns>제어용 Coroutine 핸들</returns>
        public Coroutine Tween(float duration, Action<float> onUpdate,
            Action onComplete = null, bool unscaled = false)
        {
            return Run(TweenRoutine(duration, onUpdate, onComplete, unscaled));
        }
        #endregion // 지연 실행

        #region 내부 코루틴
        /// <summary>
        /// 지연 후 액션 실행 코루틴 본체.
        /// </summary>
        /// <param name="delay">지연 시간(초)</param>
        /// <param name="action">실행할 액션</param>
        /// <returns>IEnumerator</returns>
        private IEnumerator DelayedCallRoutine(float delay, Action action)
        {
            if (delay > 0f) yield return Wait(delay);
            action?.Invoke();
        }

        /// <summary>
        /// 다음 프레임 실행 코루틴 본체.
        /// </summary>
        /// <param name="action">실행할 액션</param>
        /// <returns>IEnumerator</returns>
        private IEnumerator NextFrameRoutine(Action action)
        {
            yield return null;
            action?.Invoke();
        }

        /// <summary>
        /// 프레임 끝 실행 코루틴 본체.
        /// </summary>
        /// <param name="action">실행할 액션</param>
        /// <returns>IEnumerator</returns>
        private IEnumerator EndOfFrameRoutine(Action action)
        {
            yield return waitEndOfFrame;
            action?.Invoke();
        }

        /// <summary>
        /// 조건 대기 실행 코루틴 본체.
        /// </summary>
        /// <param name="condition">평가할 조건</param>
        /// <param name="action">실행할 액션</param>
        /// <returns>IEnumerator</returns>
        private IEnumerator WaitUntilRoutine(Func<bool> condition, Action action)
        {
            while (condition != null && !condition()) yield return null;
            action?.Invoke();
        }

        /// <summary>
        /// 반복 실행 코루틴 본체.
        /// </summary>
        /// <param name="interval">실행 간격(초)</param>
        /// <param name="count">반복 횟수. 0 이하면 무한</param>
        /// <param name="action">실행할 액션. 인자는 현재 인덱스</param>
        /// <returns>IEnumerator</returns>
        private IEnumerator RepeatRoutine(float interval, int count, Action<int> action)
        {
            int i = 0;
            var wait = Wait(interval);
            while (count <= 0 || i < count)
            {
                action?.Invoke(i);
                i++;
                yield return wait;
            }
        }

        /// <summary>
        /// 트윈 실행 코루틴 본체.
        /// </summary>
        /// <param name="duration">전체 시간(초)</param>
        /// <param name="onUpdate">0~1 값을 받는 콜백</param>
        /// <param name="onComplete">완료 시 콜백</param>
        /// <param name="unscaled">true면 Time.timeScale 무시</param>
        /// <returns>IEnumerator</returns>
        private IEnumerator TweenRoutine(float duration, Action<float> onUpdate,
            Action onComplete, bool unscaled)
        {
            if (duration <= 0f)
            {
                onUpdate?.Invoke(1f);
                onComplete?.Invoke();
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += unscaled ? Time.unscaledDeltaTime : Time.deltaTime;
                onUpdate?.Invoke(Mathf.Clamp01(elapsed / duration));
                yield return null;
            }
            onUpdate?.Invoke(1f);
            onComplete?.Invoke();
        }
        #endregion // 내부 코루틴
    }
}