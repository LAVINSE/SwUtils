using System;
using UnityEngine;

namespace SWTools
{
    /// <summary>
    /// 인스펙터에 버튼 바를 표시하는 커스텀 어트리뷰트
    /// EX - [SWButtonBar(new string[] {}, new string[] {}, new bool[] {})]
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class SWButtonBarAttribute : PropertyAttribute
    {
        #region 필드
        #endregion // 필드

        #region 프로퍼티
        /// <summary>
        /// 버튼에 표시될 라벨 텍스트 배열
        /// </summary>
        public string[] Labels { get; private set; }

        /// <summary>
        /// 버튼 클릭 시 호출될 메서드 이름 배열
        /// </summary>
        public string[] MethodNames { get; private set; }

        /// <summary>
        /// 플레이 모드에서만 활성화 여부
        /// </summary>
        public bool[] OnlyWhenPlayMode { get; private set; }
        #endregion // 프로퍼티

        /// <summary>
        /// 여러 버튼의 라벨, 호출 메서드, 플레이 모드 제한 정보를 지정해 버튼 바 어트리뷰트를 생성합니다.
        /// </summary>
        /// <param name="labels">버튼에 표시할 텍스트 배열입니다.</param>
        /// <param name="methodNames">각 버튼이 호출할 메서드 이름 배열입니다.</param>
        /// <param name="onlyWhenPlayMode">각 버튼의 플레이 모드 전용 여부 배열입니다.</param>
        public SWButtonBarAttribute(string[] labels, string[] methodNames, bool[] onlyWhenPlayMode)
        {
            if (labels.Length != methodNames.Length || labels.Length != onlyWhenPlayMode.Length)
            {
                SWUtils.SWUtilsLog.LogError("[SWButtonBar] 배열 길이가 일치하지 않습니다. 모든 배열의 길이가 동일해야 합니다.");
            }

            this.Labels = labels;
            this.MethodNames = methodNames;
            this.OnlyWhenPlayMode = onlyWhenPlayMode;
        }
    }
}
