using SW.Base;

namespace SW.Quest
{
    /// <summary>
    /// 작업이 시작될 때 사용할 진행량을 제공하는 에셋의 기본 클래스입니다.
    /// </summary>
    public abstract class SWQuestInitialProgressValue : SWScriptableObject
    {
        /// <summary>
        /// 작업의 시작 진행량을 반환합니다.
        /// </summary>
        /// <param name="task">시작하는 작업입니다.</param>
        /// <returns>시작 진행량입니다.</returns>
        public abstract int GetValue(SWQuestTask task);
    }
}
