using System;
using UnityEngine;

namespace SWTools
{
    /// <summary>
    /// 인스펙터에서 필드들을 그룹으로 묶어 표시하기 위한 어트리뷰트
    /// 필드, 프로퍼티, 클래스, 구조체에 적용 가능하며,
    /// SWMonobehaviourEditor에서 폴드아웃 형태로 렌더링됩니다.
    /// 
    /// 사용 예시:
    /// [SWGroup("Player Stats")]
    /// public int health;
    /// 
    /// [SWGroup("Player Stats")]
    /// public int mana;
    /// 
    /// [SWGroup("Enemy Settings", Color.red, true, true)]
    /// public float enemySpeed;
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Struct, Inherited = true)]
    public class SWGroupAttribute : PropertyAttribute
    {
        #region 필드
        private static readonly Color groupColor = new Color32(222, 184, 135, 255);
        #endregion // 필드

        #region 프로퍼티
        /// <summary>
        /// 인스펙터에 표시될 그룹 이름
        /// </summary>
        public string GroupName { get; private set; }

        /// <summary>
        /// true일 경우 : 다음 Group 어트리뷰트가 나타날 때까지 모든 필드를 현재 그룹에 포함 
        /// false일 경우 : 이 어트리뷰트가 붙은 필드만 그룹에 포함
        /// </summary>
        public bool GroupAllFieldsUntilNextGroupAttribute { get; private set; }

        /// <summary>
        /// 그룹의 색상
        /// </summary>
        public Color GroupColor { get; private set; }

        /// <summary>
        /// true일 경우 : 그룹이 기본적으로 접힌 상태로 시작
        /// false일 경우 : 그룹이 기본적으로 펼쳐진 상태로 시작
        /// </summary>
        public bool ClosedByDefault { get; private set; }
        #endregion // 프로퍼티


        /// <summary>
        /// 기본 그룹 색상으로 그룹 어트리뷰트를 생성합니다.
        /// </summary>
        /// <param name="groupName">인스펙터에 표시할 그룹 이름입니다.</param>
        /// <param name="groupAllFieldsUntilNextGroupAttribute">다음 그룹 어트리뷰트 전까지 필드를 모두 포함할지 여부입니다.</param>
        /// <param name="closedByDefault">그룹을 기본적으로 접힌 상태로 시작할지 여부입니다.</param>
        public SWGroupAttribute(string groupName, bool groupAllFieldsUntilNextGroupAttribute = true, bool closedByDefault = false)
        {
            this.GroupName = groupName;
            this.GroupAllFieldsUntilNextGroupAttribute = groupAllFieldsUntilNextGroupAttribute;
            this.GroupColor = groupColor;
            this.ClosedByDefault = closedByDefault;
        }

        /// <summary>
        /// 그룹 색상을 직접 지정해 그룹 어트리뷰트를 생성합니다.
        /// </summary>
        /// <param name="groupName">인스펙터에 표시할 그룹 이름입니다.</param>
        /// <param name="color">그룹에 적용할 색상입니다.</param>
        /// <param name="groupAllFieldsUntilNextGroupAttribute">다음 그룹 어트리뷰트 전까지 필드를 모두 포함할지 여부입니다.</param>
        /// <param name="closedByDefault">그룹을 기본적으로 접힌 상태로 시작할지 여부입니다.</param>
        public SWGroupAttribute(string groupName, Color color, bool groupAllFieldsUntilNextGroupAttribute = true,  bool closedByDefault = false)
        {
            this.GroupName = groupName;
            this.GroupAllFieldsUntilNextGroupAttribute = groupAllFieldsUntilNextGroupAttribute;
            this.GroupColor = color;
            this.ClosedByDefault = closedByDefault;
        }
    }
}
