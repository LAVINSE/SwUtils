using System.Collections.Generic;
using SWTools;
using UnityEngine;

public class SWAttributeExample : SWMonoBehaviour
{
    public enum EEnumConditionType
    {
        Hide,
        Show,
    }

    #region 필드
    [SWGroup("Group Example")]
    [SerializeField] private int groupIndex;

    [SWGroup("Group Example - Reaonly")]
    [SWReadOnly]
    [SerializeField] private int groupReadOnly;

    [SWGroup("Group Example - Condition")]
    [SWCondition("isConditionExample", true)]
    [SerializeField] private int conditionExample;
    [SerializeField] private bool isConditionExample;

    [SWGroup("Group Example - Dropdown")]
    [SWDropdown(2, 4, 8, 16)]
    [SerializeField] private int dropDownValueExample;

    [SWGroup("Group Example - EnumCondition")]
    [SWEnumCondition("conditionTypeExample", (int)EEnumConditionType.Show)]
    [SerializeField] private int enumConditionExample;
#pragma warning disable CS0414
    [SerializeField] private EEnumConditionType conditionTypeExample = EEnumConditionType.Hide;
#pragma warning restore CS0414

    [SWGroup("Group Example - ButtonBar")]
    [SWButtonBar(new string[] { "1", "2", "3" }, new string[] { "ButtonBarExample1", "ButtonBarExample2", "ButtonBarExample3" }, new bool[] { false, false, false })]
    [SerializeField] private bool isDebugButtonBar;
    #endregion // 필드

    #region 함수
    [SWButton("함수 버튼 예제")]
    private void ButtonExample()
    {
        Debug.Log("버튼 예제");
    }

    private void ButtonBarExample1()
    {
        Debug.Log("버튼 바 예제 1");
    }

    private void ButtonBarExample2()
    {
        Debug.Log("버튼 바 예제 2");
    }

    private void ButtonBarExample3()
    {
        Debug.Log("버튼 바 예제 3");
    }
    #endregion // 함수
}
