using System;
using UnityEngine;

namespace SWTools
{
    [AttributeUsage(AttributeTargets.Method)]
    public class SWButtonAttribute : PropertyAttribute
    {
        #region 필드
        #endregion // 필드

        #region 프로퍼티
        public string DisplayName { get; private set; }
        public float Space { get; private set; }
        #endregion // 프로퍼티

        public SWButtonAttribute(string displayName = null)
        {
            DisplayName = displayName;
            Space = 0f;
        }

        public SWButtonAttribute(float space)
        {
            DisplayName = null;
            Space = space;
        }

        public SWButtonAttribute(string displayName, float space)
        {
            DisplayName = displayName;
            Space = space;
        }
    }
}
