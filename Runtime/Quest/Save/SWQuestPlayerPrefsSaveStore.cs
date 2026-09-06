using System;

using SW.Data;

using SW.Util;

namespace SW.Quest
{
    /// <summary>
    /// 퀘스트 직렬화 문자열을 암호화된 <see cref="SWPlayerPrefs"/>에 보관하는 기본 저장소입니다.
    /// </summary>
    public sealed class SWQuestPlayerPrefsSaveStore : ISWQuestSaveStore
    {
        /// <inheritdoc />
        public bool HasData(string key)
            => !string.IsNullOrEmpty(key) && SWPlayerPrefs.HasKey(key);

        /// <inheritdoc />
        public bool Save(string key, string serializedData)
        {
            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(serializedData))
            {
                return false;
            }

            try
            {
                SWPlayerPrefs.SetString(key, serializedData);
                SWPlayerPrefs.Save();
                return true;
            }
            catch (Exception exception)
            {
                SWLog.LogError($"[SWQuestPlayerPrefsSaveStore] 저장 실패: {exception.Message}");
                return false;
            }
        }

        /// <inheritdoc />
        public bool TryLoad(string key, out string serializedData)
        {
            serializedData = string.Empty;
            if (!HasData(key))
            {
                return false;
            }

            try
            {
                serializedData = SWPlayerPrefs.GetString(key);
                return !string.IsNullOrEmpty(serializedData);
            }
            catch (Exception exception)
            {
                SWLog.LogError($"[SWQuestPlayerPrefsSaveStore] 불러오기 실패: {exception.Message}");
                return false;
            }
        }
    }
}
