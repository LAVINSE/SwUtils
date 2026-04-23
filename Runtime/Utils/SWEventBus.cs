using System;
using System.Collections.Generic;

namespace SWUtils
{
    /// <summary>
    /// 시스템 간 의존성을 줄이기 위한 타입 기반 전역 이벤트 버스.
    /// </summary>
    public static class SWEventBus
    {
        #region 필드
        private static readonly Dictionary<Type, Delegate> eventTable = new();
        #endregion // 필드

        #region 함수
        /// <summary>
        /// 지정한 이벤트 타입에 리스너를 등록한다.
        /// </summary>
        /// <typeparam name="TEvent">이벤트 데이터 타입.</typeparam>
        /// <param name="listener">등록할 리스너.</param>
        public static void Subscribe<TEvent>(Action<TEvent> listener)
        {
            if (listener == null)
            {
                SWUtilsLog.LogWarning($"[SWEventBus] Subscribe failed. Listener is null. Event: {typeof(TEvent).Name}");
                return;
            }

            Type eventType = typeof(TEvent);
            if (eventTable.TryGetValue(eventType, out Delegate existing))
                eventTable[eventType] = Delegate.Combine(existing, listener);
            else
                eventTable[eventType] = listener;

            SWUtilsLog.Log($"[SWEventBus] Subscribe: {eventType.Name}");
        }

        /// <summary>
        /// 지정한 이벤트 타입에서 리스너를 제거한다.
        /// </summary>
        /// <typeparam name="TEvent">이벤트 데이터 타입.</typeparam>
        /// <param name="listener">제거할 리스너.</param>
        public static void Unsubscribe<TEvent>(Action<TEvent> listener)
        {
            if (listener == null)
            {
                SWUtilsLog.LogWarning($"[SWEventBus] Unsubscribe failed. Listener is null. Event: {typeof(TEvent).Name}");
                return;
            }

            Type eventType = typeof(TEvent);
            if (!eventTable.TryGetValue(eventType, out Delegate existing))
                return;

            Delegate updated = Delegate.Remove(existing, listener);
            if (updated == null)
                eventTable.Remove(eventType);
            else
                eventTable[eventType] = updated;

            SWUtilsLog.Log($"[SWEventBus] Unsubscribe: {eventType.Name}");
        }

        /// <summary>
        /// 지정한 이벤트 데이터를 등록된 모든 리스너에게 전달한다.
        /// </summary>
        /// <typeparam name="TEvent">이벤트 데이터 타입.</typeparam>
        /// <param name="eventData">전달할 이벤트 데이터.</param>
        public static void Publish<TEvent>(TEvent eventData)
        {
            Type eventType = typeof(TEvent);
            if (!eventTable.TryGetValue(eventType, out Delegate existing))
            {
                SWUtilsLog.Log($"[SWEventBus] Publish skipped. No listeners: {eventType.Name}");
                return;
            }

            Action<TEvent> callback = existing as Action<TEvent>;
            if (callback == null)
            {
                SWUtilsLog.LogError($"[SWEventBus] Publish failed. Invalid delegate type: {eventType.Name}");
                return;
            }

            foreach (Delegate listenerDelegate in callback.GetInvocationList())
            {
                try
                {
                    Action<TEvent> listener = (Action<TEvent>)listenerDelegate;
                    listener.Invoke(eventData);
                }
                catch (Exception exception)
                {
                    SWUtilsLog.LogError($"[SWEventBus] Listener exception. Event: {eventType.Name}, Error: {exception.Message}");
                }
            }

            SWUtilsLog.Log($"[SWEventBus] Publish: {eventType.Name}");
        }

        /// <summary>
        /// 지정한 이벤트 타입에 등록된 모든 리스너를 제거한다.
        /// </summary>
        /// <typeparam name="TEvent">이벤트 데이터 타입.</typeparam>
        public static void Clear<TEvent>()
        {
            eventTable.Remove(typeof(TEvent));
            SWUtilsLog.Log($"[SWEventBus] Clear: {typeof(TEvent).Name}");
        }

        /// <summary>
        /// 모든 이벤트 리스너를 제거한다.
        /// </summary>
        public static void ClearAll()
        {
            eventTable.Clear();
            SWUtilsLog.Log("[SWEventBus] Clear all.");
        }

        /// <summary>
        /// 지정한 이벤트 타입에 리스너가 하나 이상 등록되어 있는지 확인한다.
        /// </summary>
        /// <typeparam name="TEvent">이벤트 데이터 타입.</typeparam>
        /// <returns>리스너가 있으면 true.</returns>
        public static bool HasListener<TEvent>()
        {
            return eventTable.ContainsKey(typeof(TEvent));
        }
        #endregion // 함수
    }
}
