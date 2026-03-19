using System;

namespace SWTools
{
    /// <summary>
    /// 에디터 + PlayMode에서 매 프레임 다시 그린다
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class SWRequiresConstantRepaintAttribute : Attribute
    {
        
    }
}
