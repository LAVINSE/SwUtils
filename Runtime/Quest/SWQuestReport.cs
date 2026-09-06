using System;

using SW.Base;

namespace SW.Quest
{
    /// <summary>
    /// 퀘스트 시스템에 전달하는 진행 보고입니다.
    /// </summary>
    public readonly struct SWQuestReport
    {
        /// <summary>
        /// 코드명, 대상과 변화량을 지정하여 진행 보고를 생성합니다.
        /// </summary>
        /// <param name="categoryCode">보고를 구분하는 카테고리 코드명입니다.</param>
        /// <param name="target">보고 대상입니다.</param>
        /// <param name="amount">진행 변화량입니다.</param>
        public SWQuestReport(string categoryCode, object target, int amount = 1)
        {
            CategoryCode = categoryCode ?? string.Empty;
            Target = target;
            Amount = amount;
        }

        /// <summary>
        /// 카테고리, 대상과 변화량을 지정하여 진행 보고를 생성합니다.
        /// </summary>
        /// <param name="category">보고를 구분하는 카테고리입니다.</param>
        /// <param name="target">보고 대상입니다.</param>
        /// <param name="amount">진행 변화량입니다.</param>
        public SWQuestReport(SWCategory category, object target, int amount = 1)
            : this(category != null ? category.CodeName : string.Empty, target, amount)
        {
        }

        /// <summary>보고를 구분하는 카테고리 코드명입니다.</summary>
        public string CategoryCode { get; }

        /// <summary>보고 대상입니다.</summary>
        public object Target { get; }

        /// <summary>진행 계산에 전달할 변화량입니다.</summary>
        public int Amount { get; }

        /// <summary>
        /// 지정한 카테고리 코드명과 보고의 카테고리가 같은지 확인합니다.
        /// </summary>
        /// <param name="categoryCode">비교할 카테고리 코드명입니다.</param>
        /// <returns>같은 카테고리이면 <see langword="true"/>입니다.</returns>
        public bool HasCategory(string categoryCode)
            => string.Equals(CategoryCode, categoryCode, StringComparison.Ordinal);
    }
}
