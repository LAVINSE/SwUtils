using SW.Base;

namespace SW.Quest
{
    /// <summary>
    /// 진행 보고의 대상과 작업 대상을 비교하는 에셋의 기본 클래스입니다.
    /// </summary>
    public abstract class SWQuestTarget : SWScriptableObject
    {
        /// <summary>보고에 사용할 원본 대상 값입니다.</summary>
        public abstract object Value { get; }

        /// <summary>
        /// 보고 대상이 현재 작업 대상과 일치하는지 확인합니다.
        /// </summary>
        /// <param name="target">비교할 보고 대상입니다.</param>
        /// <returns>대상이 일치하면 <see langword="true"/>입니다.</returns>
        public abstract bool Matches(object target);
    }
}
