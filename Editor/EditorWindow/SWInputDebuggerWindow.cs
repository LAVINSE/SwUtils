using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace SWTools
{
    /// <summary>
    /// 플레이 중에 EventSystem 선택 오브젝트, 마우스/터치, UI Raycast 결과,
    /// InputSystem의 활성 Action Map 등을 실시간으로 보여주는 디버거 창입니다.
    /// </summary>
    public class SWInputDebuggerWindow : EditorWindow
    {
        #region 필드
        private Vector2 scrollPosition;

        // Raycast 결과 버퍼 (매 프레임 재사용)
        private readonly List<RaycastResult> raycastResults = new();
        private PointerEventData cachedPointerData;

        // 표시 토글
        private bool showEventSystem = true;
        private bool showPointer = true;
        private bool showRaycast = true;
        private bool showInputSystem = true;
        private bool showKeyboardMouse = true;

        // 마지막 클릭 정보
        private Vector2 lastClickPosition;
        private string lastClickedObjectName = "(없음)";
        private double lastClickTime;

        // 리페인트 주기 제어
        private double lastRepaintTime;
        private const double REPAINT_INTERVAL = 0.05; // 20fps 정도로 갱신
        #endregion // 필드

        [MenuItem("SWTools/Input Debugger Window")]
        public static void ShowWindow()
        {
            SWInputDebuggerWindow window = GetWindow<SWInputDebuggerWindow>();
            window.titleContent = new GUIContent("SW Input Debugger",
                EditorGUIUtility.FindTexture("d_EventSystem Icon"));
            window.minSize = new Vector2(320, 400);
            window.Show();
        }

        private void OnEnable()
        {
            EditorApplication.update += OnEditorUpdate;
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
        }

        private void OnEditorUpdate()
        {
            if (!Application.isPlaying) return;

            double now = EditorApplication.timeSinceStartup;
            if (now - lastRepaintTime >= REPAINT_INTERVAL)
            {
                lastRepaintTime = now;
                Repaint();
            }
        }

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("플레이 중에만 실시간 정보가 표시됩니다.", MessageType.Info);
            }

            EditorGUILayout.Space(5);

            showEventSystem = EditorGUILayout.Foldout(showEventSystem, "EventSystem", true);
            if (showEventSystem) DrawEventSystemInfo();

            EditorGUILayout.Space(5);
            showPointer = EditorGUILayout.Foldout(showPointer, "Pointer (Mouse / Touch)", true);
            if (showPointer) DrawPointerInfo();

            EditorGUILayout.Space(5);
            showRaycast = EditorGUILayout.Foldout(showRaycast, "UI Raycast (현재 포인터)", true);
            if (showRaycast) DrawRaycastInfo();

            EditorGUILayout.Space(5);
            showKeyboardMouse = EditorGUILayout.Foldout(showKeyboardMouse, "Keyboard / Mouse Buttons", true);
            if (showKeyboardMouse) DrawKeyboardMouseInfo();

            EditorGUILayout.Space(5);
            showInputSystem = EditorGUILayout.Foldout(showInputSystem, "Input System", true);
            if (showInputSystem) DrawInputSystemInfo();

            EditorGUILayout.EndScrollView();
        }

        #region EventSystem
        private void DrawEventSystemInfo()
        {
            DrawHeader("EventSystem");

            EventSystem es = EventSystem.current;
            if (es == null)
            {
                EditorGUILayout.HelpBox("현재 활성 EventSystem이 없습니다.", MessageType.Warning);
                return;
            }

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField("EventSystem", es, typeof(EventSystem), true);
                EditorGUILayout.ObjectField("Selected", es.currentSelectedGameObject, typeof(GameObject), true);

                GameObject firstSelected = es.firstSelectedGameObject;
                EditorGUILayout.ObjectField("First Selected", firstSelected, typeof(GameObject), true);

                EditorGUILayout.Toggle("Send Navigation Events", es.sendNavigationEvents);
                EditorGUILayout.IntField("Pixel Drag Threshold", es.pixelDragThreshold);
            }

            EditorGUILayout.LabelField("Last Clicked", lastClickedObjectName);
            EditorGUILayout.LabelField("Last Click Pos", lastClickPosition.ToString("F0"));
            double ago = EditorApplication.timeSinceStartup - lastClickTime;
            EditorGUILayout.LabelField("Click Age (s)",
                lastClickTime > 0 ? $"{ago:F2}" : "(없음)");
        }
        #endregion

        #region Pointer
        private void DrawPointerInfo()
        {
            DrawHeader("Pointer");

            Vector2 mousePos = Input.mousePosition;
            EditorGUILayout.LabelField("Mouse Position", mousePos.ToString("F0"));
            EditorGUILayout.LabelField("Mouse Normalized",
                new Vector2(mousePos.x / Mathf.Max(1, Screen.width),
                           mousePos.y / Mathf.Max(1, Screen.height)).ToString("F2"));

            // Touch
            int touchCount = Input.touchCount;
            EditorGUILayout.LabelField("Touch Count", touchCount.ToString());
            for (int i = 0; i < touchCount && i < 5; i++)
            {
                Touch t = Input.GetTouch(i);
                EditorGUILayout.LabelField($"  Touch[{i}]", $"{t.phase} @ {t.position:F0} (fid={t.fingerId})");
            }

            // 클릭 감지 (OnGUI 이벤트로 저장)
            if (Application.isPlaying && Input.GetMouseButtonDown(0))
            {
                lastClickPosition = mousePos;
                lastClickTime = EditorApplication.timeSinceStartup;

                EventSystem es = EventSystem.current;
                if (es != null && es.currentSelectedGameObject != null)
                {
                    lastClickedObjectName = es.currentSelectedGameObject.name;
                }
            }
        }
        #endregion

        #region Raycast
        private void DrawRaycastInfo()
        {
            DrawHeader("UI Raycast");

            EventSystem es = EventSystem.current;
            if (es == null)
            {
                EditorGUILayout.HelpBox("EventSystem이 없습니다.", MessageType.Info);
                return;
            }

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("플레이 중에만 Raycast가 수행됩니다.", MessageType.Info);
                return;
            }

            // 현재 마우스 위치로 Raycast
            if (cachedPointerData == null)
            {
                cachedPointerData = new PointerEventData(es);
            }
            cachedPointerData.position = Input.mousePosition;

            raycastResults.Clear();
            es.RaycastAll(cachedPointerData, raycastResults);

            EditorGUILayout.LabelField("Hit Count", raycastResults.Count.ToString());

            if (raycastResults.Count == 0)
            {
                EditorGUILayout.HelpBox("현재 포인터 아래에 닿은 UI가 없습니다.", MessageType.None);
                return;
            }

            for (int i = 0; i < raycastResults.Count && i < 10; i++)
            {
                RaycastResult r = raycastResults[i];
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                GUILayout.Label($"[{i}]", GUILayout.Width(25));
                GUILayout.Label(r.gameObject != null ? r.gameObject.name : "(null)",
                    GUILayout.ExpandWidth(true));
                GUILayout.Label($"d={r.distance:F1}", GUILayout.Width(55));
                if (GUILayout.Button("Ping", GUILayout.Width(40), GUILayout.Height(18)))
                {
                    if (r.gameObject != null)
                    {
                        Selection.activeGameObject = r.gameObject;
                        EditorGUIUtility.PingObject(r.gameObject);
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
        }
        #endregion

        #region Keyboard / Mouse
        private void DrawKeyboardMouseInfo()
        {
            DrawHeader("Keyboard / Mouse");

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("플레이 중에만 입력이 표시됩니다.", MessageType.Info);
                return;
            }

            EditorGUILayout.BeginHorizontal();
            DrawMouseButton("LMB", 0);
            DrawMouseButton("RMB", 1);
            DrawMouseButton("MMB", 2);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField("Mouse ScrollDelta", Input.mouseScrollDelta.ToString("F2"));
            EditorGUILayout.LabelField("Any Key", Input.anyKey ? "● 눌림" : "○");

            // 주요 modifier
            string modifiers = "";
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) modifiers += "Shift ";
            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) modifiers += "Ctrl ";
            if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)) modifiers += "Alt ";
            EditorGUILayout.LabelField("Modifiers", string.IsNullOrEmpty(modifiers) ? "(없음)" : modifiers);
        }

        private void DrawMouseButton(string label, int button)
        {
            bool pressed = Input.GetMouseButton(button);
            GUI.backgroundColor = pressed ? Color.green : Color.white;
            GUILayout.Button(pressed ? $"{label} ●" : $"{label} ○", GUILayout.Height(22));
            GUI.backgroundColor = Color.white;
        }
        #endregion

        #region Input System
        private void DrawInputSystemInfo()
        {
            DrawHeader("Input System");

#if ENABLE_INPUT_SYSTEM
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("플레이 중에만 표시됩니다.", MessageType.Info);
                return;
            }

            // 연결된 디바이스
            var devices = InputSystem.devices;
            EditorGUILayout.LabelField("Devices", devices.Count.ToString());
            foreach (var device in devices)
            {
                EditorGUILayout.LabelField($"  • {device.displayName}", $"{device.layout} ({(device.enabled ? "on" : "off")})");
            }

            // 활성 Action Map (PlayerInput이 있다면)
            var playerInputs = Object.FindObjectsByType<PlayerInput>(FindObjectsSortMode.None);
            if (playerInputs != null && playerInputs.Length > 0)
            {
                EditorGUILayout.Space(3);
                EditorGUILayout.LabelField("PlayerInput", EditorStyles.miniBoldLabel);
                foreach (var pi in playerInputs)
                {
                    if (pi == null) continue;
                    using (new EditorGUI.DisabledScope(true))
                    {
                        EditorGUILayout.ObjectField("Object", pi.gameObject, typeof(GameObject), true);
                    }
                    EditorGUILayout.LabelField("  Current Map", pi.currentActionMap != null ? pi.currentActionMap.name : "(none)");
                    EditorGUILayout.LabelField("  Control Scheme", string.IsNullOrEmpty(pi.currentControlScheme) ? "(none)" : pi.currentControlScheme);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("활성 PlayerInput 컴포넌트가 없습니다.", MessageType.None);
            }

            // Pointer / Keyboard 현재값
            if (Mouse.current != null)
            {
                EditorGUILayout.Space(3);
                EditorGUILayout.LabelField("Mouse.current", Mouse.current.position.ReadValue().ToString("F0"));
            }
            if (Keyboard.current != null)
            {
                EditorGUILayout.LabelField("Keyboard.anyKey",
                    Keyboard.current.anyKey.isPressed ? "● 눌림" : "○");
            }
#else
            EditorGUILayout.HelpBox("Input System 패키지가 설치되어 있지 않거나 활성화되지 않았습니다.\n(Project Settings > Player > Active Input Handling)", MessageType.Info);
#endif
        }
        #endregion

        private void DrawHeader(string title)
        {
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            Rect rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, new Color(0.3f, 0.3f, 0.3f, 1f));
            EditorGUILayout.Space(3);
        }
    }
}