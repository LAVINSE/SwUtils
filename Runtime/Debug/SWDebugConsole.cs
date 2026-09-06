using System.Diagnostics;

#if SW_DEBUG_MODE
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.Profiling;
#endif

using SW.Attributes;

namespace SW.Debugging
{
    /// <summary>
    /// 빌드에서 로그 확인, [SWCommand] 명령 실행, 상태 표시를 제공하는 인게임 디버그 콘솔입니다.
    /// </summary>
    /// <remarks>
    /// - SW_DEBUG_MODE 심볼이 있을 때만 동작하며, 없으면 이 클래스의 호출 코드 자체가
    ///   Conditional 어트리뷰트로 컴파일에서 제거됩니다. (SWLog와 동일한 방식)
    /// - UI는 IMGUI(OnGUI)로 코드가 직접 그리므로 프리팹, Canvas, 폰트 등 별도 에셋 제작이 필요 없습니다.
    /// - 열기 키와 동시 터치 개수는 SWDebugConsoleSettings에서 설정합니다.
    /// - SW_DEBUG_MODE가 정의되고 자동 생성 설정이 켜져 있으면 씬 로드 후 자동으로 생성됩니다.
    /// </remarks>
    public static class SWDebugConsole
    {
        #region 프로퍼티
        /// <summary>콘솔이 현재 열려 있는지 여부입니다. 심볼이 없으면 항상 false입니다.</summary>
        public static bool IsOpen
        {
            get
            {
#if SW_DEBUG_MODE
                return SWDebugConsoleBehaviour.HasInstance && SWDebugConsoleBehaviour.Instance.IsOpen;
#else
                return false;
#endif
            }
        }

        /// <summary>성능 오버레이가 현재 표시 중인지 여부입니다. 심볼이 없으면 항상 false입니다.</summary>
        public static bool IsOverlayVisible
        {
            get
            {
#if SW_DEBUG_MODE
                return SWDebugConsoleBehaviour.HasInstance && SWDebugConsoleBehaviour.Instance.IsOverlayVisible;
#else
                return false;
#endif
            }
        }
        #endregion // 프로퍼티

        #region 함수
        /// <summary>
        /// 콘솔을 엽니다.
        /// </summary>
        [Conditional("SW_DEBUG_MODE")]
        public static void Show()
        {
#if SW_DEBUG_MODE
            SWDebugConsoleBehaviour.EnsureCreated();
            SWDebugConsoleBehaviour.Instance.SetOpen(true);
#endif
        }

        /// <summary>
        /// 콘솔을 닫습니다.
        /// </summary>
        [Conditional("SW_DEBUG_MODE")]
        public static void Hide()
        {
#if SW_DEBUG_MODE
            if (SWDebugConsoleBehaviour.HasInstance)
                SWDebugConsoleBehaviour.Instance.SetOpen(false);
#endif
        }

        /// <summary>
        /// 콘솔 열림 상태를 토글합니다.
        /// </summary>
        [Conditional("SW_DEBUG_MODE")]
        public static void Toggle()
        {
#if SW_DEBUG_MODE
            SWDebugConsoleBehaviour.EnsureCreated();
            SWDebugConsoleBehaviour.Instance.SetOpen(!SWDebugConsoleBehaviour.Instance.IsOpen);
#endif
        }

        /// <summary>
        /// 인스턴스 메서드의 [SWCommand]를 콘솔에 등록합니다. 보통 Awake에서 호출합니다.
        /// </summary>
        /// <param name="target">명령을 가진 오브젝트입니다.</param>
        [Conditional("SW_DEBUG_MODE")]
        public static void RegisterInstance(object target)
        {
#if SW_DEBUG_MODE
            SWDebugConsoleBehaviour.EnsureCreated();
            SWDebugConsoleBehaviour.Instance.RegisterInstanceCommands(target);
#endif
        }

        /// <summary>
        /// 등록한 인스턴스 명령을 해제합니다. 보통 OnDestroy에서 호출합니다.
        /// </summary>
        /// <param name="target">해제할 오브젝트입니다.</param>
        [Conditional("SW_DEBUG_MODE")]
        public static void UnregisterInstance(object target)
        {
#if SW_DEBUG_MODE
            if (SWDebugConsoleBehaviour.HasInstance)
                SWDebugConsoleBehaviour.Instance.UnregisterInstanceCommands(target);
#endif
        }

        /// <summary>
        /// 상태 탭에 매 프레임 갱신되는 감시 값을 등록합니다.
        /// <code>SWDebugConsole.Watch("Gold", () => player.Gold.ToString());</code>
        /// </summary>
        /// <param name="name">표시 이름입니다. 같은 이름은 덮어씁니다.</param>
        /// <param name="valueGetter">표시할 값을 반환하는 함수입니다.</param>
        [Conditional("SW_DEBUG_MODE")]
        public static void Watch(string name, System.Func<string> valueGetter)
        {
#if SW_DEBUG_MODE
            SWDebugConsoleBehaviour.EnsureCreated();
            SWDebugConsoleBehaviour.Instance.AddWatch(name, valueGetter);
#endif
        }

        /// <summary>
        /// 감시 값을 제거합니다.
        /// </summary>
        /// <param name="name">제거할 감시 이름입니다.</param>
        [Conditional("SW_DEBUG_MODE")]
        public static void Unwatch(string name)
        {
#if SW_DEBUG_MODE
            if (SWDebugConsoleBehaviour.HasInstance)
                SWDebugConsoleBehaviour.Instance.RemoveWatch(name);
#endif
        }

        /// <summary>
        /// 명령 문자열을 코드에서 직접 실행합니다.
        /// <code>SWDebugConsole.Execute("gold 5000");</code>
        /// </summary>
        /// <param name="commandLine">명령과 인자를 포함한 한 줄 문자열입니다.</param>
        [Conditional("SW_DEBUG_MODE")]
        public static void Execute(string commandLine)
        {
#if SW_DEBUG_MODE
            SWDebugConsoleBehaviour.EnsureCreated();
            SWDebugConsoleBehaviour.Instance.ExecuteCommandLine(commandLine);
#endif
        }

        /// <summary>
        /// 성능 오버레이를 표시합니다.
        /// </summary>
        [Conditional("SW_DEBUG_MODE")]
        public static void ShowOverlay()
        {
#if SW_DEBUG_MODE
            SWDebugConsoleBehaviour.EnsureCreated();
            SWDebugConsoleBehaviour.Instance.SetOverlayVisible(true);
#endif
        }

        /// <summary>
        /// 성능 오버레이를 숨깁니다.
        /// </summary>
        [Conditional("SW_DEBUG_MODE")]
        public static void HideOverlay()
        {
#if SW_DEBUG_MODE
            if (SWDebugConsoleBehaviour.HasInstance)
                SWDebugConsoleBehaviour.Instance.SetOverlayVisible(false);
#endif
        }

        /// <summary>
        /// 성능 오버레이 표시 상태를 토글합니다.
        /// </summary>
        [Conditional("SW_DEBUG_MODE")]
        public static void ToggleOverlay()
        {
#if SW_DEBUG_MODE
            SWDebugConsoleBehaviour.EnsureCreated();
            SWDebugConsoleBehaviour.Instance.SetOverlayVisible(!SWDebugConsoleBehaviour.Instance.IsOverlayVisible);
#endif
        }

        /// <summary>
        /// 성능 오버레이의 최소/최대 FPS 기록을 초기화합니다.
        /// </summary>
        [Conditional("SW_DEBUG_MODE")]
        public static void ResetOverlayStats()
        {
#if SW_DEBUG_MODE
            if (SWDebugConsoleBehaviour.HasInstance)
                SWDebugConsoleBehaviour.Instance.ResetOverlayMinMax();
#endif
        }

        /// <summary>
        /// 콘솔 명령: 성능 오버레이 표시를 토글합니다.
        /// </summary>
        [SWCommand("overlay", "성능 오버레이 표시를 토글합니다", "Debug")]
        private static void OverlayCommand()
        {
            ToggleOverlay();
        }

        /// <summary>
        /// 콘솔 명령: 성능 오버레이 최소/최대 기록을 초기화합니다.
        /// </summary>
        [SWCommand("overlay_reset", "성능 오버레이 최소/최대 기록을 초기화합니다", "Debug")]
        private static void OverlayResetCommand()
        {
            ResetOverlayStats();
        }
        #endregion // 함수
    }

#if SW_DEBUG_MODE
    /// <summary>
    /// SWDebugConsole의 실제 구현 MonoBehaviour입니다. SW_DEBUG_MODE에서만 컴파일됩니다.
    /// </summary>
    internal sealed class SWDebugConsoleBehaviour : MonoBehaviour
    {
        #region 데이터
        /// <summary>수집된 로그 한 건입니다.</summary>
        private sealed class LogEntry
        {
            /// <summary>로그 메시지입니다.</summary>
            public string message;
            /// <summary>로그가 발생한 호출 스택입니다.</summary>
            public string stackTrace;
            /// <summary>로그의 심각도입니다.</summary>
            public LogType type;
            /// <summary>같은 로그가 반복된 횟수입니다.</summary>
            public int count = 1;
            /// <summary>로그가 마지막으로 발생한 시각입니다.</summary>
            public string timeText;
        }

        /// <summary>등록된 콘솔 명령 한 건입니다.</summary>
        private sealed class CommandInfo
        {
            /// <summary>콘솔에서 입력할 명령 이름입니다.</summary>
            public string name;
            /// <summary>명령 사용 목적입니다.</summary>
            public string description;
            /// <summary>명령을 표시할 그룹입니다.</summary>
            public string group;
            /// <summary>명령이 실행할 메서드입니다.</summary>
            public MethodInfo method;
            /// <summary>인스턴스 메서드를 호출할 대상입니다.</summary>
            public object target;
            /// <summary>명령이 받는 매개변수입니다.</summary>
            public ParameterInfo[] parameters;
            /// <summary>사용자가 입력한 인수 문자열입니다.</summary>
            public string argsInput = string.Empty;

            /// <summary>help와 치트 패널에 표시할 시그니처 문자열입니다.</summary>
            public string Signature
            {
                get
                {
                    if (parameters.Length == 0) return name;

                    StringBuilder builder = new(name);
                    for (int index = 0; index < parameters.Length; index++)
                        builder.Append(' ').Append('<').Append(parameters[index].Name).Append(':')
                            .Append(parameters[index].ParameterType.Name).Append('>');
                    return builder.ToString();
                }
            }
        }

        /// <summary>상태 탭 감시 항목입니다.</summary>
        private sealed class WatchEntry
        {
            /// <summary>감시 항목 이름입니다.</summary>
            public string name;
            /// <summary>현재 표시값을 반환하는 함수입니다.</summary>
            public Func<string> valueGetter;
        }
        #endregion // 데이터

        #region 필드
        private const int MaxLogCount = 500;
        private const int MaxHistoryCount = 30;
        private static readonly string[] TabNames = { "로그", "명령", "상태" };

        private static SWDebugConsoleBehaviour instance;

        private readonly object logLock = new();
        private readonly List<LogEntry> logEntries = new();
        private readonly List<CommandInfo> commands = new();
        private readonly List<WatchEntry> watches = new();
        private readonly List<string> inputHistory = new();

        private bool isOpen;
        private int selectedTab;
        private int selectedLogIndex = -1;
        private int historyIndex = -1;
        private bool showLog = true;
        private bool showWarning = true;
        private bool showError = true;
        private bool gestureLatched;
        private bool staticCommandsScanned;

        private string searchText = string.Empty;
        private string commandInput = string.Empty;

        private Vector2 logScrollPosition;
        private Vector2 stackScrollPosition;
        private Vector2 commandScrollPosition;
        private Vector2 statusScrollPosition;

        private float smoothedDeltaTime;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>구현 인스턴스입니다.</summary>
        internal static SWDebugConsoleBehaviour Instance => instance;

        /// <summary>인스턴스가 존재하는지 여부입니다.</summary>
        internal static bool HasInstance => instance != null;

        /// <summary>콘솔이 열려 있는지 여부입니다.</summary>
        internal bool IsOpen => isOpen;
        #endregion // 프로퍼티

        #region 초기화
        /// <summary>
        /// 플레이 진입 시 정적 상태를 초기화합니다. Domain Reload가 꺼져 있어도 항상 호출됩니다.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            instance = null;
        }

        /// <summary>
        /// 씬 로드 후 콘솔을 자동 생성합니다. 자동 생성과 오버레이 자동 표시가 모두 꺼져 있으면 생성하지 않습니다.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreate()
        {
            SWDebugConsoleSettings settings = SWDebugConsoleSettings.Load();
            if (!settings.AutoCreateOnLoad && !settings.OverlayEnabledOnStart) return;

            EnsureCreated();
        }

        /// <summary>
        /// 콘솔 오브젝트가 없으면 생성합니다.
        /// </summary>
        internal static void EnsureCreated()
        {
            if (instance != null) return;
            if (!Application.isPlaying) return;

            GameObject consoleObject = new("[SWDebugConsole]");
            consoleObject.hideFlags = HideFlags.HideInHierarchy;
            DontDestroyOnLoad(consoleObject);
            instance = consoleObject.AddComponent<SWDebugConsoleBehaviour>();
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            Application.logMessageReceivedThreaded += HandleLogMessage;
            overlayVisible = SWDebugConsoleSettings.Load().OverlayEnabledOnStart;
        }

        private void OnDestroy()
        {
            if (overlayBackgroundTexture != null)
                Destroy(overlayBackgroundTexture);

            Application.logMessageReceivedThreaded -= HandleLogMessage;
            if (instance == this)
                instance = null;
        }
        #endregion // 초기화

        #region 열기/닫기
        /// <summary>
        /// 콘솔 열림 상태를 설정합니다.
        /// </summary>
        /// <param name="open">열림 여부입니다.</param>
        internal void SetOpen(bool open)
        {
            if (isOpen == open) return;

            isOpen = open;
            if (isOpen)
                ScanStaticCommandsIfNeeded();
        }

        private void Update()
        {
            smoothedDeltaTime = Mathf.Lerp(smoothedDeltaTime, Time.unscaledDeltaTime, 0.1f);
            CheckOpenGesture();
            UpdateOverlay();
        }

        /// <summary>
        /// 설정된 키 또는 동시 터치로 콘솔을 토글합니다. 키와 터치 개수는 SWDebugConsoleSettings에서 설정합니다.
        /// </summary>
        private void CheckOpenGesture()
        {
            SWDebugConsoleSettings settings = SWDebugConsoleSettings.Load();

            if (settings.EnableInputSystem && CheckInputSystemOpenGesture(settings))
                return;

            CheckLegacyOpenGesture(settings);
        }

        /// <summary>
        /// Input System 패키지가 있을 때 리플렉션으로 열기 입력을 확인합니다.
        /// </summary>
        /// <param name="settings">콘솔 설정입니다.</param>
        /// <returns>입력이 처리되었으면 true입니다.</returns>
        private bool CheckInputSystemOpenGesture(SWDebugConsoleSettings settings)
        {
            if (settings.OpenKey != SWConsoleOpenKey.None
                && AreInputSystemRequiredModifiersPressed(settings)
                && SWInputSystemReflectionCache.WasKeyboardKeyPressed(settings.GetInputSystemKeyPropertyName()))
            {
                SetOpen(!isOpen);
                return true;
            }

            int touchCount = SWInputSystemReflectionCache.GetPressedTouchCount();
            if (touchCount < 0) return false;

            return HandleTouchCount(touchCount, settings.TouchCountToOpen);
        }

        /// <summary>
        /// 기본 Unity 입력 API로 열기 입력을 확인합니다.
        /// </summary>
        /// <param name="settings">콘솔 설정입니다.</param>
        private void CheckLegacyOpenGesture(SWDebugConsoleSettings settings)
        {
            try
            {
                if (settings.OpenKey != SWConsoleOpenKey.None
                    && AreLegacyRequiredModifiersPressed(settings)
                    && Input.GetKeyDown(settings.GetLegacyKeyCode()))
                {
                    SetOpen(!isOpen);
                    return;
                }

                HandleTouchCount(Input.touchCount, settings.TouchCountToOpen);
            }
            catch (InvalidOperationException)
            {
                gestureLatched = false;
            }
        }

        /// <summary>
        /// 동시 터치 개수로 콘솔 열기 입력을 처리합니다.
        /// </summary>
        /// <param name="touchCount">현재 눌린 터치 개수입니다.</param>
        /// <param name="requiredTouchCount">콘솔을 여는 데 필요한 터치 개수입니다.</param>
        /// <returns>터치 입력이 처리되었으면 true입니다.</returns>
        private bool HandleTouchCount(int touchCount, int requiredTouchCount)
        {
            if (touchCount >= requiredTouchCount)
            {
                if (!gestureLatched)
                {
                    gestureLatched = true;
                    SetOpen(!isOpen);
                }

                return true;
            }

            gestureLatched = false;
            return false;
        }

        /// <summary>
        /// Input System에서 요구 조합키가 눌렸는지 확인합니다.
        /// </summary>
        /// <param name="settings">콘솔 설정입니다.</param>
        /// <returns>필요한 조합키가 모두 눌렸으면 true입니다.</returns>
        private static bool AreInputSystemRequiredModifiersPressed(SWDebugConsoleSettings settings)
        {
            if (settings.RequireControlKey
                && !SWInputSystemReflectionCache.IsAnyKeyboardKeyPressed(SWInputSystemReflectionCache.ControlKeyPropertyNames))
                return false;

            if (settings.RequireShiftKey
                && !SWInputSystemReflectionCache.IsAnyKeyboardKeyPressed(SWInputSystemReflectionCache.ShiftKeyPropertyNames))
                return false;

            if (settings.RequireAltKey
                && !SWInputSystemReflectionCache.IsAnyKeyboardKeyPressed(SWInputSystemReflectionCache.AltKeyPropertyNames))
                return false;

            return true;
        }

        /// <summary>
        /// 기본 Unity 입력 API에서 요구 조합키가 눌렸는지 확인합니다.
        /// </summary>
        /// <param name="settings">콘솔 설정입니다.</param>
        /// <returns>필요한 조합키가 모두 눌렸으면 true입니다.</returns>
        private static bool AreLegacyRequiredModifiersPressed(SWDebugConsoleSettings settings)
        {
            if (settings.RequireControlKey
                && !Input.GetKey(KeyCode.LeftControl)
                && !Input.GetKey(KeyCode.RightControl))
                return false;

            if (settings.RequireShiftKey
                && !Input.GetKey(KeyCode.LeftShift)
                && !Input.GetKey(KeyCode.RightShift))
                return false;

            if (settings.RequireAltKey
                && !Input.GetKey(KeyCode.LeftAlt)
                && !Input.GetKey(KeyCode.RightAlt))
                return false;

            return true;
        }
        #endregion // 열기/닫기

        #region Input System 리플렉션 캐시
        /// <summary>
        /// Input System의 키보드와 터치 입력을 리플렉션으로 조회하고 멤버 정보를 캐시합니다. 패키지가 없어도 사용할 수 있습니다.
        /// </summary>
        private static class SWInputSystemReflectionCache
        {
            #region 상수
            private const string KeyboardTypeName = "UnityEngine.InputSystem.Keyboard, Unity.InputSystem";
            private const string TouchscreenTypeName = "UnityEngine.InputSystem.Touchscreen, Unity.InputSystem";
            #endregion // 상수

            #region 필드
            public static readonly string[] ControlKeyPropertyNames = { "leftCtrlKey", "rightCtrlKey" };
            public static readonly string[] ShiftKeyPropertyNames = { "leftShiftKey", "rightShiftKey" };
            public static readonly string[] AltKeyPropertyNames = { "leftAltKey", "rightAltKey" };

            private static readonly Dictionary<string, PropertyInfo> keyboardKeyPropertyDict = new();
            private static readonly Dictionary<Type, Dictionary<string, PropertyInfo>> instancePropertyDict = new();
            private static readonly Dictionary<Type, MethodInfo> indexerGetterDict = new();
            private static readonly object[] indexArgumentCache = new object[1];

            private static bool initialized;
            private static Type keyboardType;
            private static Type touchscreenType;
            private static PropertyInfo keyboardCurrentProperty;
            private static PropertyInfo touchscreenCurrentProperty;
            #endregion // 필드

            #region 함수
            /// <summary>
            /// Keyboard 키가 이번 프레임에 눌렸는지 확인합니다.
            /// </summary>
            /// <param name="keyPropertyName">Keyboard의 키 프로퍼티 이름입니다.</param>
            /// <returns>눌렸으면 true입니다.</returns>
            public static bool WasKeyboardKeyPressed(string keyPropertyName)
            {
                if (string.IsNullOrEmpty(keyPropertyName)) return false;

                object keyControl = GetKeyboardKeyControl(keyPropertyName);
                object wasPressedThisFrame = GetInstancePropertyValue(keyControl, "wasPressedThisFrame");
                return wasPressedThisFrame is bool pressed && pressed;
            }

            /// <summary>
            /// Keyboard 키 목록 중 하나라도 눌려 있는지 확인합니다.
            /// </summary>
            /// <param name="keyPropertyNames">Keyboard의 키 프로퍼티 이름 목록입니다.</param>
            /// <returns>하나라도 눌려 있으면 true입니다.</returns>
            public static bool IsAnyKeyboardKeyPressed(string[] keyPropertyNames)
            {
                for (int index = 0; index < keyPropertyNames.Length; index++)
                {
                    object keyControl = GetKeyboardKeyControl(keyPropertyNames[index]);
                    object isPressed = GetInstancePropertyValue(keyControl, "isPressed");
                    if (isPressed is bool pressed && pressed)
                        return true;
                }

                return false;
            }

            /// <summary>
            /// Input System에서 현재 눌린 터치 개수를 가져옵니다.
            /// </summary>
            /// <returns>가져오면 터치 개수, 사용할 수 없으면 -1입니다.</returns>
            public static int GetPressedTouchCount()
            {
                EnsureInitialized();
                object touchscreen = touchscreenCurrentProperty?.GetValue(null);
                if (touchscreen == null) return -1;

                object touches = GetInstancePropertyValue(touchscreen, "touches");
                if (touches == null) return -1;

                object countValue = GetInstancePropertyValue(touches, "Count");
                if (countValue is not int count) return -1;

                MethodInfo getItemMethod = GetIndexerGetter(touches.GetType());
                if (getItemMethod == null) return -1;

                int pressedCount = 0;
                for (int index = 0; index < count; index++)
                {
                    indexArgumentCache[0] = index;
                    object touch = getItemMethod.Invoke(touches, indexArgumentCache);
                    object pressControl = GetInstancePropertyValue(touch, "press");
                    object isPressed = GetInstancePropertyValue(pressControl, "isPressed");
                    if (isPressed is bool pressed && pressed)
                        pressedCount++;
                }

                return pressedCount;
            }

            private static object GetKeyboardKeyControl(string keyPropertyName)
            {
                EnsureInitialized();
                object keyboard = keyboardCurrentProperty?.GetValue(null);
                if (keyboard == null) return null;

                PropertyInfo keyProperty = GetKeyboardKeyProperty(keyPropertyName);
                return keyProperty?.GetValue(keyboard);
            }

            private static void EnsureInitialized()
            {
                if (initialized) return;

                keyboardType = Type.GetType(KeyboardTypeName);
                touchscreenType = Type.GetType(TouchscreenTypeName);
                keyboardCurrentProperty = keyboardType?.GetProperty("current", BindingFlags.Public | BindingFlags.Static);
                touchscreenCurrentProperty = touchscreenType?.GetProperty("current", BindingFlags.Public | BindingFlags.Static);
                initialized = true;
            }

            private static PropertyInfo GetKeyboardKeyProperty(string keyPropertyName)
            {
                EnsureInitialized();
                if (keyboardType == null) return null;

                if (keyboardKeyPropertyDict.TryGetValue(keyPropertyName, out PropertyInfo cachedProperty))
                    return cachedProperty;

                PropertyInfo property = keyboardType.GetProperty(keyPropertyName, BindingFlags.Public | BindingFlags.Instance);
                keyboardKeyPropertyDict[keyPropertyName] = property;
                return property;
            }

            private static object GetInstancePropertyValue(object target, string propertyName)
            {
                if (target == null) return null;

                PropertyInfo property = GetInstanceProperty(target.GetType(), propertyName);
                return property?.GetValue(target);
            }

            private static PropertyInfo GetInstanceProperty(Type type, string propertyName)
            {
                if (!instancePropertyDict.TryGetValue(type, out Dictionary<string, PropertyInfo> propertyDict))
                {
                    propertyDict = new Dictionary<string, PropertyInfo>();
                    instancePropertyDict[type] = propertyDict;
                }

                if (propertyDict.TryGetValue(propertyName, out PropertyInfo cachedProperty))
                    return cachedProperty;

                PropertyInfo property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
                propertyDict[propertyName] = property;
                return property;
            }

            private static MethodInfo GetIndexerGetter(Type type)
            {
                if (indexerGetterDict.TryGetValue(type, out MethodInfo cachedMethod))
                    return cachedMethod;

                MethodInfo method = type.GetMethod("get_Item");
                indexerGetterDict[type] = method;
                return method;
            }
            #endregion // 함수
        }
        #endregion // Input System 리플렉션 캐시

        #region 로그 수집
        /// <summary>
        /// Unity 로그 콜백입니다. 다른 스레드에서도 호출될 수 있어 lock으로 보호합니다.
        /// </summary>
        private void HandleLogMessage(string condition, string stackTrace, LogType type)
        {
            lock (logLock)
            {
                // 직전 로그와 동일하면 개수만 증가 (collapse)
                if (logEntries.Count > 0)
                {
                    LogEntry lastEntry = logEntries[logEntries.Count - 1];
                    if (lastEntry.type == type && lastEntry.message == condition)
                    {
                        lastEntry.count++;
                        return;
                    }
                }

                logEntries.Add(new LogEntry
                {
                    message = condition,
                    stackTrace = stackTrace,
                    type = type,
                    timeText = DateTime.Now.ToString("HH:mm:ss")
                });

                if (logEntries.Count > MaxLogCount)
                {
                    logEntries.RemoveAt(0);
                    if (selectedLogIndex >= 0)
                        selectedLogIndex--;
                }
            }
        }
        #endregion // 로그 수집

        #region 명령 등록
        /// <summary>
        /// 모든 어셈블리에서 static [SWCommand] 메서드를 1회 수집합니다.
        /// </summary>
        private void ScanStaticCommandsIfNeeded()
        {
            if (staticCommandsScanned) return;
            staticCommandsScanned = true;

            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int index = 0; index < assemblies.Length; index++)
            {
                string assemblyName = assemblies[index].GetName().Name;
                if (ShouldSkipAssembly(assemblyName)) continue;

                try
                {
                    Type[] types = assemblies[index].GetTypes();
                    for (int typeIndex = 0; typeIndex < types.Length; typeIndex++)
                        CollectCommands(types[typeIndex], null, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                }
                catch (ReflectionTypeLoadException)
                {
                    // 로드 실패한 타입이 있는 어셈블리는 건너뜁니다.
                }
            }
        }

        /// <summary>
        /// 명령 수집에서 제외할 어셈블리인지 확인합니다.
        /// </summary>
        private static bool ShouldSkipAssembly(string assemblyName)
        {
            return assemblyName.StartsWith("Unity", StringComparison.Ordinal)
                || assemblyName.StartsWith("System", StringComparison.Ordinal)
                || assemblyName.StartsWith("Mono.", StringComparison.Ordinal)
                || assemblyName.StartsWith("Microsoft", StringComparison.Ordinal)
                || assemblyName == "mscorlib"
                || assemblyName == "netstandard";
        }

        /// <summary>
        /// 인스턴스의 [SWCommand] 메서드를 등록합니다.
        /// </summary>
        /// <param name="target">명령을 가진 오브젝트입니다.</param>
        internal void RegisterInstanceCommands(object target)
        {
            if (target == null) return;

            UnregisterInstanceCommands(target);
            CollectCommands(target.GetType(), target,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        }

        /// <summary>
        /// 인스턴스의 명령 등록을 해제합니다.
        /// </summary>
        /// <param name="target">해제할 오브젝트입니다.</param>
        internal void UnregisterInstanceCommands(object target)
        {
            if (target == null) return;

            for (int index = commands.Count - 1; index >= 0; index--)
            {
                if (ReferenceEquals(commands[index].target, target))
                    commands.RemoveAt(index);
            }
        }

        /// <summary>
        /// 타입에서 [SWCommand] 메서드를 찾아 등록합니다. (상속 계층 포함)
        /// </summary>
        private void CollectCommands(Type type, object target, BindingFlags bindingFlags)
        {
            MethodInfo[] methods = type.GetMethods(bindingFlags | BindingFlags.FlattenHierarchy);
            for (int index = 0; index < methods.Length; index++)
            {
                SWCommandAttribute commandAttribute = methods[index].GetCustomAttribute<SWCommandAttribute>();
                if (commandAttribute == null) continue;

                string commandName = string.IsNullOrEmpty(commandAttribute.Name)
                    ? methods[index].Name
                    : commandAttribute.Name;

                commands.Add(new CommandInfo
                {
                    name = commandName,
                    description = commandAttribute.Description,
                    group = string.IsNullOrEmpty(commandAttribute.Group) ? "기본" : commandAttribute.Group,
                    method = methods[index],
                    target = target,
                    parameters = methods[index].GetParameters()
                });
            }
        }
        #endregion // 명령 등록

        #region 명령 실행
        /// <summary>
        /// 명령 한 줄을 파싱해서 실행합니다.
        /// </summary>
        /// <param name="commandLine">명령과 인자를 포함한 문자열입니다.</param>
        internal void ExecuteCommandLine(string commandLine)
        {
            if (string.IsNullOrWhiteSpace(commandLine)) return;

            ScanStaticCommandsIfNeeded();
            AddHistory(commandLine);

            List<string> tokens = Tokenize(commandLine);
            if (tokens.Count == 0) return;

            string commandName = tokens[0];

            // 내장 명령
            if (string.Equals(commandName, "help", StringComparison.OrdinalIgnoreCase))
            {
                PrintHelp();
                return;
            }

            if (string.Equals(commandName, "clear", StringComparison.OrdinalIgnoreCase))
            {
                lock (logLock) { logEntries.Clear(); }
                selectedLogIndex = -1;
                return;
            }

            CommandInfo command = FindCommand(commandName, tokens.Count - 1);
            if (command == null)
            {
                UnityEngine.Debug.LogWarning($"[SWDebugConsole] 알 수 없는 명령: {commandName} (help로 목록 확인)");
                return;
            }

            InvokeCommand(command, tokens);
        }

        /// <summary>
        /// 이름과 인자 개수로 명령을 찾습니다. 파괴된 인스턴스 명령은 건너뜁니다.
        /// </summary>
        private CommandInfo FindCommand(string commandName, int argumentCount)
        {
            CommandInfo nameMatched = null;

            for (int index = 0; index < commands.Count; index++)
            {
                CommandInfo command = commands[index];
                if (!string.Equals(command.name, commandName, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (command.target is UnityEngine.Object unityTarget && unityTarget == null)
                    continue;

                nameMatched = command;
                if (command.parameters.Length == argumentCount)
                    return command;
            }

            return nameMatched;
        }

        /// <summary>
        /// 인자를 변환해 명령 메서드를 호출합니다.
        /// </summary>
        private void InvokeCommand(CommandInfo command, List<string> tokens)
        {
            int argumentCount = tokens.Count - 1;
            if (command.parameters.Length != argumentCount)
            {
                UnityEngine.Debug.LogWarning($"[SWDebugConsole] 인자 개수가 맞지 않습니다. 사용법: {command.Signature}");
                return;
            }

            object[] arguments = new object[argumentCount];
            for (int index = 0; index < argumentCount; index++)
            {
                if (!TryConvertArgument(tokens[index + 1], command.parameters[index].ParameterType, out arguments[index]))
                {
                    UnityEngine.Debug.LogWarning($"[SWDebugConsole] 인자 변환 실패: '{tokens[index + 1]}' → {command.parameters[index].ParameterType.Name}");
                    return;
                }
            }

            try
            {
                object result = command.method.Invoke(command.target, arguments);
                UnityEngine.Debug.Log(result != null
                    ? $"[SWDebugConsole] {command.name} → {result}"
                    : $"[SWDebugConsole] {command.name} 실행 완료");
            }
            catch (Exception exception)
            {
                UnityEngine.Debug.LogError($"[SWDebugConsole] {command.name} 실행 오류: {exception.InnerException?.Message ?? exception.Message}");
            }
        }

        /// <summary>
        /// 문자열 인자를 파라미터 타입으로 변환합니다. int, float, bool, string, enum을 지원합니다.
        /// </summary>
        private static bool TryConvertArgument(string token, Type parameterType, out object value)
        {
            value = null;

            if (parameterType == typeof(string)) { value = token; return true; }
            if (parameterType == typeof(int)) { bool ok = int.TryParse(token, out int intValue); value = intValue; return ok; }
            if (parameterType == typeof(float))
            {
                bool ok = float.TryParse(token, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out float floatValue);
                value = floatValue;
                return ok;
            }
            if (parameterType == typeof(bool))
            {
                if (token == "1" || string.Equals(token, "on", StringComparison.OrdinalIgnoreCase)) { value = true; return true; }
                if (token == "0" || string.Equals(token, "off", StringComparison.OrdinalIgnoreCase)) { value = false; return true; }
                bool ok = bool.TryParse(token, out bool boolValue);
                value = boolValue;
                return ok;
            }
            if (parameterType.IsEnum)
            {
                try { value = Enum.Parse(parameterType, token, true); return true; }
                catch { return false; }
            }

            return false;
        }

        /// <summary>
        /// 명령 줄을 토큰으로 분리합니다. 큰따옴표로 공백 포함 문자열을 지원합니다.
        /// </summary>
        private static List<string> Tokenize(string commandLine)
        {
            List<string> tokens = new();
            StringBuilder builder = new();
            bool insideQuotes = false;

            for (int index = 0; index < commandLine.Length; index++)
            {
                char character = commandLine[index];

                if (character == '"')
                {
                    insideQuotes = !insideQuotes;
                    continue;
                }

                if (character == ' ' && !insideQuotes)
                {
                    if (builder.Length > 0)
                    {
                        tokens.Add(builder.ToString());
                        builder.Clear();
                    }
                    continue;
                }

                builder.Append(character);
            }

            if (builder.Length > 0)
                tokens.Add(builder.ToString());

            return tokens;
        }

        /// <summary>
        /// 등록된 명령 목록을 로그로 출력합니다.
        /// </summary>
        private void PrintHelp()
        {
            StringBuilder builder = new("[SWDebugConsole] 명령 목록\n");
            builder.AppendLine("help - 명령 목록 출력");
            builder.AppendLine("clear - 로그 비우기");

            for (int index = 0; index < commands.Count; index++)
            {
                builder.Append(commands[index].Signature);
                if (!string.IsNullOrEmpty(commands[index].description))
                    builder.Append(" - ").Append(commands[index].description);
                builder.AppendLine();
            }

            UnityEngine.Debug.Log(builder.ToString());
        }

        /// <summary>
        /// 입력 히스토리에 명령을 추가합니다.
        /// </summary>
        private void AddHistory(string commandLine)
        {
            inputHistory.Remove(commandLine);
            inputHistory.Add(commandLine);
            if (inputHistory.Count > MaxHistoryCount)
                inputHistory.RemoveAt(0);

            historyIndex = -1;
        }
        #endregion // 명령 실행

        #region 감시
        /// <summary>
        /// 상태 탭 감시 항목을 추가하거나 덮어씁니다.
        /// </summary>
        internal void AddWatch(string watchName, Func<string> valueGetter)
        {
            if (string.IsNullOrEmpty(watchName) || valueGetter == null) return;

            RemoveWatch(watchName);
            watches.Add(new WatchEntry { name = watchName, valueGetter = valueGetter });
        }

        /// <summary>
        /// 감시 항목을 제거합니다.
        /// </summary>
        internal void RemoveWatch(string watchName)
        {
            for (int index = watches.Count - 1; index >= 0; index--)
            {
                if (watches[index].name == watchName)
                    watches.RemoveAt(index);
            }
        }
        #endregion // 감시

        #region 성능 오버레이
        /// <summary>플레이 시작 직후 최소/최대 기록 오염을 막는 준비 시간(초)입니다.</summary>
        private const float OverlayWarmupSeconds = 2f;
        /// <summary>오버레이의 화면 가장자리 여백(픽셀)입니다.</summary>
        private const float OverlayScreenMargin = 10f;

        private readonly StringBuilder overlayBuilder = new();

        private bool overlayVisible;
        private float overlayWarmupRemaining = OverlayWarmupSeconds;
        private float overlayUpdateTimer;
        private float overlayMinFps = float.MaxValue;
        private float overlayMaxFps;
        private string overlayText = string.Empty;
        private Color overlayColor = Color.white;

        private GUIStyle overlayStyle;
        private Texture2D overlayBackgroundTexture;

        /// <summary>오버레이가 표시 중인지 여부입니다.</summary>
        internal bool IsOverlayVisible => overlayVisible;

        /// <summary>
        /// 오버레이 표시 상태를 설정합니다.
        /// </summary>
        /// <param name="visible">표시 여부입니다.</param>
        internal void SetOverlayVisible(bool visible)
        {
            if (overlayVisible == visible) return;

            overlayVisible = visible;
            if (overlayVisible)
            {
                ResetOverlayMinMax();
                overlayUpdateTimer = 0f;
            }
        }

        /// <summary>
        /// 최소/최대 FPS 기록을 초기화하고 준비 시간을 다시 시작합니다.
        /// </summary>
        internal void ResetOverlayMinMax()
        {
            overlayMinFps = float.MaxValue;
            overlayMaxFps = 0f;
            overlayWarmupRemaining = OverlayWarmupSeconds;
        }

        /// <summary>
        /// 오버레이 측정값을 갱신합니다. Update에서 매 프레임 호출합니다.
        /// </summary>
        private void UpdateOverlay()
        {
            if (!overlayVisible) return;

            float instantFps = Time.unscaledDeltaTime > 0f ? 1f / Time.unscaledDeltaTime : 0f;

            if (overlayWarmupRemaining > 0f)
            {
                overlayWarmupRemaining -= Time.unscaledDeltaTime;
            }
            else
            {
                if (instantFps < overlayMinFps) overlayMinFps = instantFps;
                if (instantFps > overlayMaxFps) overlayMaxFps = instantFps;
            }

            SWDebugConsoleSettings settings = SWDebugConsoleSettings.Load();
            overlayUpdateTimer += Time.unscaledDeltaTime;
            if (overlayUpdateTimer >= settings.OverlayUpdateInterval)
            {
                overlayUpdateTimer = 0f;
                RebuildOverlayText(settings);
            }
        }

        /// <summary>
        /// 오버레이 표시 문자열과 색상을 갱신합니다. 갱신 간격마다 한 번만 수행합니다.
        /// </summary>
        /// <param name="settings">현재 설정입니다.</param>
        private void RebuildOverlayText(SWDebugConsoleSettings settings)
        {
            float fps = smoothedDeltaTime > 0f ? 1f / smoothedDeltaTime : 0f;
            float frameMilliseconds = smoothedDeltaTime * 1000f;

            overlayBuilder.Clear();

            if (settings.ShowFps)
                overlayBuilder.Append("FPS ").Append(fps.ToString("F1"))
                    .Append("  (").Append(frameMilliseconds.ToString("F1")).Append("ms)");

            if (settings.ShowMinMax && overlayMaxFps > 0f)
            {
                if (overlayBuilder.Length > 0) overlayBuilder.AppendLine();
                overlayBuilder.Append("MIN ").Append(overlayMinFps.ToString("F0"))
                    .Append("  MAX ").Append(overlayMaxFps.ToString("F0"));
            }

            if (settings.ShowMemory)
            {
                if (overlayBuilder.Length > 0) overlayBuilder.AppendLine();
                float monoMegabytes = Profiler.GetMonoUsedSizeLong() / (1024f * 1024f);
                float allocatedMegabytes = Profiler.GetTotalAllocatedMemoryLong() / (1024f * 1024f);
                overlayBuilder.Append("MONO ").Append(monoMegabytes.ToString("F1")).Append("MB")
                    .AppendLine()
                    .Append("ALLOC ").Append(allocatedMegabytes.ToString("F1")).Append("MB");
            }

            overlayText = overlayBuilder.ToString();
            overlayColor = GetOverlayColor(fps, settings);
        }

        /// <summary>
        /// FPS 경계값에 따라 오버레이 표시 색상을 반환합니다.
        /// </summary>
        private static Color GetOverlayColor(float fps, SWDebugConsoleSettings settings)
        {
            if (fps < settings.FpsDangerThreshold) return new Color(1f, 0.45f, 0.45f);
            if (fps < settings.FpsWarningThreshold) return Color.yellow;
            return new Color(0.55f, 1f, 0.55f);
        }

        /// <summary>
        /// 오버레이를 그립니다. OnGUI 첫 줄에서 호출합니다. 콘솔이 열려 있으면 그리지 않습니다.
        /// </summary>
        private void DrawOverlay()
        {
            if (!overlayVisible) return;
            if (isOpen) return;
            if (string.IsNullOrEmpty(overlayText)) return;

            SWDebugConsoleSettings settings = SWDebugConsoleSettings.Load();
            EnsureOverlayStyle(settings);

            GUIContent content = new(overlayText);
            Vector2 size = overlayStyle.CalcSize(content);
            Rect rect = GetOverlayRect(size, settings.OverlayAnchor);

            GUI.color = new Color(1f, 1f, 1f, 0.9f);
            GUI.DrawTexture(rect, overlayBackgroundTexture);

            GUI.color = overlayColor;
            GUI.Label(rect, content, overlayStyle);
            GUI.color = Color.white;
        }

        /// <summary>
        /// 오버레이 스타일과 배경 텍스처를 준비합니다. 크기 배율이 바뀌면 글자 크기를 갱신합니다.
        /// </summary>
        /// <param name="settings">현재 설정입니다.</param>
        private void EnsureOverlayStyle(SWDebugConsoleSettings settings)
        {
            if (overlayStyle == null)
            {
                overlayStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.UpperLeft,
                    padding = new RectOffset(8, 8, 6, 6),
                };
            }

            int fontSize = Mathf.RoundToInt(14f * settings.OverlayScale);
            if (overlayStyle.fontSize != fontSize)
                overlayStyle.fontSize = fontSize;

            if (overlayBackgroundTexture == null)
            {
                overlayBackgroundTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                overlayBackgroundTexture.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.6f));
                overlayBackgroundTexture.Apply();
            }
        }

        /// <summary>
        /// 표시 크기와 앵커에 맞는 화면 사각형을 계산합니다.
        /// </summary>
        /// <param name="size">표시 문자열 크기입니다.</param>
        /// <param name="anchor">표시할 화면 모서리입니다.</param>
        private static Rect GetOverlayRect(Vector2 size, SWOverlayAnchor anchor)
        {
            float x = anchor is SWOverlayAnchor.TopLeft or SWOverlayAnchor.BottomLeft
                ? OverlayScreenMargin
                : Screen.width - size.x - OverlayScreenMargin;

            float y = anchor is SWOverlayAnchor.TopLeft or SWOverlayAnchor.TopRight
                ? OverlayScreenMargin
                : Screen.height - size.y - OverlayScreenMargin;

            return new Rect(x, y, size.x, size.y);
        }
        #endregion // 성능 오버레이

        #region GUI
        private void OnGUI()
        {
            DrawOverlay();

            if (!isOpen) return;

            // DPI 기반 스케일: 모바일 고해상도에서도 읽을 수 있는 크기로 확대합니다.
            float uiScale = Mathf.Max(1f, Screen.dpi > 0f ? Screen.dpi / 150f : 1f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(uiScale, uiScale, 1f));

            float panelWidth = Screen.width / uiScale;
            float panelHeight = Screen.height / uiScale * 0.65f;

            GUILayout.BeginArea(new Rect(0f, 0f, panelWidth, panelHeight), GUI.skin.box);

            DrawHeaderBar();

            switch (selectedTab)
            {
                case 0: DrawLogTab(panelHeight); break;
                case 1: DrawCommandTab(); break;
                case 2: DrawStatusTab(); break;
            }

            GUILayout.EndArea();
            GUI.matrix = Matrix4x4.identity;
        }

        /// <summary>
        /// 탭과 닫기 버튼을 그립니다.
        /// </summary>
        private void DrawHeaderBar()
        {
            GUILayout.BeginHorizontal();
            selectedTab = GUILayout.Toolbar(selectedTab, TabNames, GUILayout.Height(28f));
            if (GUILayout.Button("X", GUILayout.Width(32f), GUILayout.Height(28f)))
                SetOpen(false);
            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// 로그 탭을 그립니다.
        /// </summary>
        private void DrawLogTab(float panelHeight)
        {
            GUILayout.BeginHorizontal();
            showLog = GUILayout.Toggle(showLog, "Log", GUILayout.Width(56f));
            showWarning = GUILayout.Toggle(showWarning, "Warn", GUILayout.Width(60f));
            showError = GUILayout.Toggle(showError, "Error", GUILayout.Width(60f));
            searchText = GUILayout.TextField(searchText);

            if (GUILayout.Button("복사", GUILayout.Width(48f)))
                CopyLogsToClipboard();

            if (GUILayout.Button("비우기", GUILayout.Width(56f)))
            {
                lock (logLock) { logEntries.Clear(); }
                selectedLogIndex = -1;
            }
            GUILayout.EndHorizontal();

            logScrollPosition = GUILayout.BeginScrollView(logScrollPosition,
                GUILayout.Height(panelHeight * (selectedLogIndex >= 0 ? 0.45f : 0.72f)));

            lock (logLock)
            {
                for (int index = 0; index < logEntries.Count; index++)
                {
                    LogEntry entry = logEntries[index];
                    if (!IsLogVisible(entry)) continue;

                    GUI.contentColor = GetLogColor(entry.type);
                    string countSuffix = entry.count > 1 ? $" (x{entry.count})" : string.Empty;
                    string line = $"[{entry.timeText}] {entry.message}{countSuffix}";

                    if (GUILayout.Button(line, GUI.skin.label))
                        selectedLogIndex = selectedLogIndex == index ? -1 : index;
                }
            }

            GUI.contentColor = Color.white;
            GUILayout.EndScrollView();

            // 선택된 로그의 스택 트레이스
            if (selectedLogIndex >= 0)
            {
                string stackText;
                lock (logLock)
                {
                    stackText = selectedLogIndex < logEntries.Count
                        ? $"{logEntries[selectedLogIndex].message}\n{logEntries[selectedLogIndex].stackTrace}"
                        : string.Empty;
                }

                stackScrollPosition = GUILayout.BeginScrollView(stackScrollPosition, GUI.skin.box);
                GUILayout.TextArea(stackText, GUILayout.ExpandHeight(true));
                GUILayout.EndScrollView();
            }
        }

        /// <summary>
        /// 로그가 필터/검색을 통과하는지 확인합니다.
        /// </summary>
        private bool IsLogVisible(LogEntry entry)
        {
            bool typeVisible = entry.type switch
            {
                LogType.Warning => showWarning,
                LogType.Error or LogType.Exception or LogType.Assert => showError,
                _ => showLog
            };

            if (!typeVisible) return false;
            if (string.IsNullOrEmpty(searchText)) return true;

            return entry.message.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>
        /// 로그 타입별 표시 색을 반환합니다.
        /// </summary>
        private static Color GetLogColor(LogType type)
        {
            return type switch
            {
                LogType.Warning => Color.yellow,
                LogType.Error or LogType.Exception or LogType.Assert => new Color(1f, 0.45f, 0.45f),
                _ => Color.white
            };
        }

        /// <summary>
        /// 표시 중인 로그를 클립보드에 복사합니다.
        /// </summary>
        private void CopyLogsToClipboard()
        {
            StringBuilder builder = new();
            lock (logLock)
            {
                for (int index = 0; index < logEntries.Count; index++)
                {
                    LogEntry entry = logEntries[index];
                    if (!IsLogVisible(entry)) continue;

                    builder.Append('[').Append(entry.timeText).Append("] ").AppendLine(entry.message);
                    if (entry.type != LogType.Log && !string.IsNullOrEmpty(entry.stackTrace))
                        builder.AppendLine(entry.stackTrace);
                }
            }

            GUIUtility.systemCopyBuffer = builder.ToString();
        }

        /// <summary>
        /// 명령 탭을 그립니다. 입력창 + 등록된 명령의 버튼 목록입니다.
        /// </summary>
        private void DrawCommandTab()
        {
            ScanStaticCommandsIfNeeded();
            HandleCommandInputKeys();

            GUILayout.BeginHorizontal();
            GUI.SetNextControlName("SWConsoleInput");
            commandInput = GUILayout.TextField(commandInput, GUILayout.Height(28f));

            if (GUILayout.Button("실행", GUILayout.Width(56f), GUILayout.Height(28f)))
                SubmitCommandInput();

            if (GUILayout.Button("help", GUILayout.Width(52f), GUILayout.Height(28f)))
                ExecuteCommandLine("help");
            GUILayout.EndHorizontal();

            commandScrollPosition = GUILayout.BeginScrollView(commandScrollPosition);

            string currentGroup = null;
            for (int index = 0; index < commands.Count; index++)
            {
                CommandInfo command = commands[index];

                if (command.target is UnityEngine.Object unityTarget && unityTarget == null)
                    continue;

                if (command.group != currentGroup)
                {
                    currentGroup = command.group;
                    GUILayout.Space(4f);
                    GUILayout.Label($"── {currentGroup} ──");
                }

                GUILayout.BeginHorizontal(GUI.skin.box);
                GUILayout.Label(string.IsNullOrEmpty(command.description)
                    ? command.Signature
                    : $"{command.Signature}  ({command.description})");
                GUILayout.FlexibleSpace();

                if (command.parameters.Length > 0)
                    command.argsInput = GUILayout.TextField(command.argsInput, GUILayout.Width(140f));

                if (GUILayout.Button("실행", GUILayout.Width(56f)))
                {
                    string line = command.parameters.Length > 0
                        ? $"{command.name} {command.argsInput}"
                        : command.name;
                    ExecuteCommandLine(line);
                }
                GUILayout.EndHorizontal();
            }

            if (commands.Count == 0)
                GUILayout.Label("등록된 명령이 없습니다. [SWCommand] 어트리뷰트를 메서드에 붙여주세요.");

            GUILayout.EndScrollView();
        }

        /// <summary>
        /// 입력창 포커스 상태에서 Enter 실행, 위/아래 방향키 히스토리를 처리합니다.
        /// </summary>
        private void HandleCommandInputKeys()
        {
            Event currentEvent = Event.current;
            if (currentEvent.type != EventType.KeyDown) return;
            if (GUI.GetNameOfFocusedControl() != "SWConsoleInput") return;

            if (currentEvent.keyCode == KeyCode.Return || currentEvent.keyCode == KeyCode.KeypadEnter)
            {
                SubmitCommandInput();
                currentEvent.Use();
            }
            else if (currentEvent.keyCode == KeyCode.UpArrow && inputHistory.Count > 0)
            {
                historyIndex = historyIndex < 0 ? inputHistory.Count - 1 : Mathf.Max(0, historyIndex - 1);
                commandInput = inputHistory[historyIndex];
                currentEvent.Use();
            }
            else if (currentEvent.keyCode == KeyCode.DownArrow && historyIndex >= 0)
            {
                historyIndex = Mathf.Min(inputHistory.Count - 1, historyIndex + 1);
                commandInput = inputHistory[historyIndex];
                currentEvent.Use();
            }
        }

        /// <summary>
        /// 입력창의 명령을 실행하고 입력을 비웁니다.
        /// </summary>
        private void SubmitCommandInput()
        {
            if (string.IsNullOrWhiteSpace(commandInput)) return;

            ExecuteCommandLine(commandInput);
            commandInput = string.Empty;
        }

        /// <summary>
        /// 상태 탭을 그립니다. FPS, 메모리, 등록된 감시 값을 표시합니다.
        /// </summary>
        private void DrawStatusTab()
        {
            statusScrollPosition = GUILayout.BeginScrollView(statusScrollPosition);

            float fps = smoothedDeltaTime > 0f ? 1f / smoothedDeltaTime : 0f;
            GUILayout.Label($"FPS: {fps:F0} ({smoothedDeltaTime * 1000f:F1} ms)");
            GUILayout.Label($"TimeScale: {Time.timeScale:F2}");
            GUILayout.Label($"메모리(할당): {Profiler.GetTotalAllocatedMemoryLong() / (1024 * 1024)} MB");
            GUILayout.Label($"메모리(예약): {Profiler.GetTotalReservedMemoryLong() / (1024 * 1024)} MB");
            GUILayout.Label($"해상도: {Screen.width}x{Screen.height} @{Screen.dpi:F0}dpi");

            if (watches.Count > 0)
            {
                GUILayout.Space(6f);
                GUILayout.Label("── 감시 ──");

                for (int index = 0; index < watches.Count; index++)
                {
                    string valueText;
                    try
                    {
                        valueText = watches[index].valueGetter();
                    }
                    catch (Exception exception)
                    {
                        valueText = $"(오류: {exception.Message})";
                    }

                    GUILayout.Label($"{watches[index].name}: {valueText}");
                }
            }

            GUILayout.EndScrollView();
        }
        #endregion // GUI
    }
#endif // SW_DEBUG_MODE
}
