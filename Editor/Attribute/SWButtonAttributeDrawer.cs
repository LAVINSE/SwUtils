using UnityEditor;
using UnityEngine;

namespace SWTools
{
    /// <summary>
    /// <see cref="SWButtonAttribute"/>가 붙은 메서드를 인스펙터 버튼으로 그리는 드로어입니다.
    /// </summary>
    [CustomPropertyDrawer(typeof(SWButtonAttribute))]
    public class SWButtonAttributeDrawer : PropertyDrawer
    {
        
    }
}
