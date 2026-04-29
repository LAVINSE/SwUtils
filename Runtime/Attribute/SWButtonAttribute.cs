using System;
using UnityEngine;

namespace SWTools
{
    /// <summary>
    /// 인스펙터에 메서드 실행 버튼을 표시하기 위한 어트리뷰트입니다.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class SWButtonAttribute : PropertyAttribute
    {
        #region 필드
        #endregion // 필드

        #region 프로퍼티
        /// <summary>
        /// 버튼에 표시할 이름입니다. 비어 있으면 메서드 이름을 사용합니다.
        /// </summary>
        public string DisplayName { get; private set; }

        /// <summary>
        /// 버튼 위쪽에 추가할 여백입니다.
        /// </summary>
        public float Space { get; private set; }
        #endregion // 프로퍼티

        /// <summary>
        /// 표시 이름을 지정해 버튼 어트리뷰트를 생성합니다.
        /// </summary>
        /// <param name="displayName">버튼에 표시할 이름입니다.</param>
        public SWButtonAttribute(string displayName = null)
        {
            DisplayName = displayName;
            Space = 0f;
        }

        /// <summary>
        /// 버튼 위쪽 여백을 지정해 버튼 어트리뷰트를 생성합니다.
        /// </summary>
        /// <param name="space">버튼 위쪽에 추가할 여백입니다.</param>
        public SWButtonAttribute(float space)
        {
            DisplayName = null;
            Space = space;
        }

        /// <summary>
        /// 표시 이름과 위쪽 여백을 지정해 버튼 어트리뷰트를 생성합니다.
        /// </summary>
        /// <param name="displayName">버튼에 표시할 이름입니다.</param>
        /// <param name="space">버튼 위쪽에 추가할 여백입니다.</param>
        public SWButtonAttribute(string displayName, float space)
        {
            DisplayName = displayName;
            Space = space;
        }
    }
}
