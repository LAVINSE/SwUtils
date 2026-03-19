using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SWTools
{
    /// <summary>
    /// PlayMode에서만 매 프레임 다시 그린다
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class SWRequiresConstantRepaintOnlyWhenPlayingAttribute : Attribute
    {
    
    }
}
