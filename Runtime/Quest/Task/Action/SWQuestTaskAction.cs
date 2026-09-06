using SW.Base;

namespace SW.Quest
{
    /// <summary>
    /// 에셋으로 구성하는 퀘스트 작업 진행 계산의 기본 클래스입니다.
    /// </summary>
    public abstract class SWQuestTaskAction : SWScriptableObject, ISWQuestTaskAction
    {
        /// <inheritdoc />
        public abstract int Calculate(SWQuestTask task, int currentProgress, int reportAmount);

        /// <summary>
        /// 정수 범위를 넘지 않도록 두 값을 안전하게 더합니다.
        /// </summary>
        /// <param name="left">왼쪽 값입니다.</param>
        /// <param name="right">오른쪽 값입니다.</param>
        /// <returns>정수 범위 안으로 제한된 합계입니다.</returns>
        protected static int AddWithoutOverflow(int left, int right)
        {
            long result = (long)left + right;
            if (result > int.MaxValue)
            {
                return int.MaxValue;
            }

            return result < int.MinValue ? int.MinValue : (int)result;
        }
    }
}
