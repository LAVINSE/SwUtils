namespace SW.Quest
{
    /// <summary>
    /// 퀘스트 시스템의 직렬화 문자열을 보관하고 읽는 저장소 계약입니다.
    /// </summary>
    public interface ISWQuestSaveStore
    {
        /// <summary>
        /// 지정한 키의 저장 데이터가 있는지 확인합니다.
        /// </summary>
        /// <param name="key">확인할 저장 키입니다.</param>
        /// <returns>저장 데이터가 있으면 <see langword="true"/>입니다.</returns>
        bool HasData(string key);

        /// <summary>
        /// 지정한 키에 직렬화 문자열을 저장합니다.
        /// </summary>
        /// <param name="key">사용할 저장 키입니다.</param>
        /// <param name="serializedData">저장할 직렬화 문자열입니다.</param>
        /// <returns>저장했으면 <see langword="true"/>입니다.</returns>
        bool Save(string key, string serializedData);

        /// <summary>
        /// 지정한 키의 직렬화 문자열을 읽습니다.
        /// </summary>
        /// <param name="key">사용할 저장 키입니다.</param>
        /// <param name="serializedData">읽은 직렬화 문자열입니다.</param>
        /// <returns>데이터를 읽었으면 <see langword="true"/>입니다.</returns>
        bool TryLoad(string key, out string serializedData);
    }
}
