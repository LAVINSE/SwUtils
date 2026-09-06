using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SW.Util
{
    /// <summary>
    /// 화면에는 그리지 않고 사각형 영역만 레이캐스트 대상으로 사용하는 UI 그래픽입니다.
    /// Image 컴포넌트 없이 클릭 영역을 만들 때 캔버스 배치와 렌더링 부하를 줄일 수 있습니다.
    /// </summary>
    [AddComponentMenu("UI/SW Rect Dummy")]
    [RequireComponent(typeof(CanvasRenderer))]
    public class SWRectDummy : Graphic, ICanvasRaycastFilter
    {
        #region 레이캐스트
        /// <summary>
        /// 지정한 화면 좌표가 RectTransform 내부에 있는지 확인합니다.
        /// </summary>
        /// <param name="screenPosition">검사할 화면 좌표입니다.</param>
        /// <param name="eventCamera">이벤트 카메라입니다.</param>
        /// <returns>레이캐스트를 허용하면 true입니다.</returns>
        public bool IsRaycastLocationValid(Vector2 screenPosition, Camera eventCamera)
        {
            if (!isActiveAndEnabled) return true;

            return RectTransformUtility.RectangleContainsScreenPoint(
                rectTransform,
                screenPosition,
                eventCamera);
        }
        #endregion // 레이캐스트

        #region 렌더링
        /// <summary>
        /// 메시를 생성하지 않아 화면에 아무것도 그리지 않습니다.
        /// </summary>
        /// <param name="vertexHelper">정점과 삼각형 데이터를 관리하는 VertexHelper입니다.</param>
        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();
        }
        #endregion // 렌더링

        #region 배치
        /// <summary>
        /// 부모 RectTransform 크기에 맞게 앵커와 크기를 맞춥니다.
        /// </summary>
        [ContextMenu("Fit Parent")]
        public void FitParent()
        {
            var parentRectTransform = rectTransform.parent as RectTransform;
            if (parentRectTransform == null) return;

            rectTransform.localPosition = Vector3.zero;
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.sizeDelta = Vector2.zero;
            rectTransform.pivot = Vector2.one * 0.5f;
        }
        #endregion // 배치

#if UNITY_EDITOR
        #region 기즈모
        private void OnDrawGizmos()
        {
            if (HasEventHandler()) return;

            Color color = Color.green;
            color.a = 0.1f;
            DrawRectGizmo(color);
        }

        private void OnDrawGizmosSelected()
        {
            if (HasEventHandler()) return;

            DrawRectGizmo(Color.green);
        }

        private void DrawRectGizmo(Color color)
        {
            Gizmos.color = color;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(rectTransform.rect.center, rectTransform.rect.size);
        }

        private bool HasEventHandler()
        {
            return GetComponent<IEventSystemHandler>() != null;
        }
        #endregion // 기즈모
#endif // UNITY_EDITOR
    }
}
