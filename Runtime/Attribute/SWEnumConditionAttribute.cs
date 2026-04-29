using UnityEngine;
using System;
using System.Collections;

namespace SWTools
{
    /// <summary>
    /// 특정 enum 값에 따라 필드를 조건부로 표시하거나 숨기는 어트리뷰트입니다.
    /// Inspector에서 enum 필드의 값에 따라 다른 필드의 표시 여부를 제어할 때 사용합니다.
    /// EX - [SWEnumCondition("Enum필드이름", Enum인덱스, ...)]
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Struct, Inherited = true)]
    public class SWEnumConditionAttribute : PropertyAttribute
    {
        #region 필드
        /// <summary>
        /// Inspector에서 매 프레임 호출되므로
        /// BitArray를 이용해 가벼운 검색
        /// 크기 고정
        /// </summary>
        private BitArray bitArray = new(32);
        #endregion // 필드

        #region 프로퍼티
        /// <summary>
        /// 조건으로 사용할 enum 필드의 이름
        /// </summary>
        public string ConditionEnum { get; private set; } = "";

        /// <summary>
        /// 조건에 맞지 않을 때 필드를 숨길지 여부
        /// </summary>
        public bool Hidden { get; private set; } = false;
        #endregion // 프로퍼티

        /// <summary>
        /// 조건 enum 필드 이름과 표시할 enum 값을 지정해 조건부 표시 어트리뷰트를 생성합니다.
        /// </summary>
        /// <param name="conditionEnum">조건으로 사용할 enum 필드 이름입니다.</param>
        /// <param name="enumValues">필드를 표시할 enum 값 목록입니다.</param>
        public SWEnumConditionAttribute(string conditionEnum, params int[] enumValues)
        {
            this.ConditionEnum = conditionEnum;
            this.Hidden = true;

            for(int i = 0; i < enumValues.Length; i++)
            {
                bitArray.Set(enumValues[i], true);
            }
        }

        /// <summary>
        /// 지정한 enum 값이 표시 조건에 포함되어 있는지 확인합니다.
        /// </summary>
        /// <param name="enumValue">확인할 enum 값입니다.</param>
        /// <returns>표시 조건에 포함되어 있으면 true입니다.</returns>
        public bool ContainsBitFlag(int enumValue)
        {
            return bitArray.Get(enumValue);
        }
    }
}
