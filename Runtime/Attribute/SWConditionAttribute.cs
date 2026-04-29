using UnityEngine;
using System;


#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR

namespace SWTools
{
    /// <summary>
    /// Inspector에서 필드를 조건부로 표시/숨김/비활성화하기 위한 커스텀 어트리뷰트입니다.
    /// 지정된 boolean 필드의 값에 따라 해당 프로퍼티의 표시 상태가 결정됩니다.
    /// EX - [SWCondition("필드명")]
    /// EX - [SWCondition("필드명 , true)]
    /// EX - [SWCondition("필드명 , true, true)]  
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Struct, Inherited = true)]
    public class SWConditionAttribute : PropertyAttribute
    {
        #region 필드
        #endregion // 필드

        #region 프로퍼티
        /// <summary>
        /// 조건으로 사용할 Boolean 필드 이름
        /// </summary>
        public string ConditionBoolean { get; private set; } = "";

        /// <summary>
        /// true일 경우 : 조건이 충족되지 않아 프로퍼티를 숨김
        /// false일 경우 : 조건이 충족되지 않아도 비활성화 상태로 표시
        /// </summary>
        public bool Hidden { get; private set; } = false;

        /// <summary>
        /// true일 경우 : 조건 결과를 반전
        /// Boolean 필드가 false일 때 활성화, true일 때 비활성화 
        /// </summary>
        public bool Negative { get; private set; } = false;
        #endregion // 프로퍼티

        /// <summary>
        /// 조건 Boolean 필드 이름을 지정해 조건부 표시 어트리뷰트를 생성합니다.
        /// </summary>
        /// <param name="conditionBoolean">조건으로 사용할 Boolean 필드 이름입니다.</param>
        public SWConditionAttribute(string conditionBoolean)
        {
            this.ConditionBoolean = conditionBoolean;
            this.Hidden = false;
        }

        /// <summary>
        /// 조건 Boolean 필드 이름과 숨김 처리 여부를 지정해 조건부 표시 어트리뷰트를 생성합니다.
        /// </summary>
        /// <param name="conditionBoolean">조건으로 사용할 Boolean 필드 이름입니다.</param>
        /// <param name="hideInInspector">조건에 맞지 않을 때 인스펙터에서 숨길지 여부입니다.</param>
        public SWConditionAttribute(string conditionBoolean, bool hideInInspector)
        {
            this.ConditionBoolean = conditionBoolean;
            this.Hidden = hideInInspector;
            this.Negative = false;
        }

        /// <summary>
        /// 조건 Boolean 필드 이름, 숨김 처리 여부, 조건 반전 여부를 지정해 조건부 표시 어트리뷰트를 생성합니다.
        /// </summary>
        /// <param name="conditionBoolean">조건으로 사용할 Boolean 필드 이름입니다.</param>
        /// <param name="hideInInspector">조건에 맞지 않을 때 인스펙터에서 숨길지 여부입니다.</param>
        /// <param name="negative">조건 결과를 반전할지 여부입니다.</param>
        public SWConditionAttribute(string conditionBoolean, bool hideInInspector, bool negative)
        {
            this.ConditionBoolean = conditionBoolean;
            this.Hidden = hideInInspector;
            this.Negative = negative;
        }
    }
}
