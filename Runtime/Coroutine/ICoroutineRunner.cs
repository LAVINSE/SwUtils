using System;
using System.Collections;
using UnityEngine;

namespace SWCoroutine
{
    public interface ICoroutineRunner
    {
        /// <summary>
        /// 코루틴을 실행한다
        /// </summary>
        /// <param name="routine">실행할 IEnumerator</param>
        /// <returns>코루틴</returns>
        public Coroutine Run(IEnumerator routine);

        /// <summary>
        /// 실행 중인 코루틴을 중단한다
        /// </summary>
        /// <param name="coroutine">중단할 코루틴</param>
        public void Stop(Coroutine coroutine);

        /// <summary>
        /// 모든 코루틴을 중단한다.
        /// </summary>
        public void StopAll();

        /// <summary>
        /// 일정 시간 후 액션을 실행한다.
        /// </summary>
        /// <param name="delay">지연 시간(초)</param>
        /// <param name="action">실행할 액션</param>
        /// <returns>제어용 Coroutine</returns>
        public Coroutine DelayedCall(float delay, Action action);

        /// <summary>
        /// 다음 프레임에 액션을 실행한다.
        /// </summary>
        /// <param name="action">실행할 액션</param>
        /// <returns>제어용 Coroutine</returns>
        public Coroutine NextFrame(Action action);

        /// <summary>
        /// 프레임 끝에 액션을 실행한다.
        /// </summary>
        /// <param name="action">실행할 액션</param>
        /// <returns>제어용 Coroutine</returns>
        public Coroutine EndOfFrame(Action action);

        /// <summary>
        /// 조건이 참이 될 때까지 기다렸다가 액션을 실행한다.
        /// </summary>
        /// <param name="condition">평가할 조건</param>
        /// <param name="action">실행할 액션</param>
        /// <returns>제어용 Coroutine</returns>
        public Coroutine WaitUntil(Func<bool> condition, Action action);

        /// <summary>
        /// 지정한 횟수만큼 간격을 두고 액션을 반복 실행한다.
        /// </summary>
        /// <param name="interval">실행 간격(초)</param>
        /// <param name="count">반복 횟수. 0 이하면 무한 반복</param>
        /// <param name="action">실행할 액션. 인자는 현재 인덱스</param>
        /// <returns>제어용 Coroutine</returns>
        public Coroutine Repeat(float interval, int count, Action<int> action);

        /// <summary>
        /// duration 동안 0~1 값을 매 프레임 전달한다.
        /// </summary>
        /// <param name="duration">전체 시간(초)</param>
        /// <param name="onUpdate">0~1 값을 받는 콜백</param>
        /// <param name="onComplete">완료 시 콜백</param>
        /// <param name="unscaled">true면 Time.timeScale 무시</param>
        /// <returns>제어용 Coroutine</returns>
        public Coroutine Tween(float duration, Action<float> onUpdate,
            Action onComplete = null, bool unscaled = false);
    }
}