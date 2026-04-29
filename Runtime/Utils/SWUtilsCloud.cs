using System;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

#if UNITY_ANDROID && SW_GOOGLEPLAY_ENABLE && !UNITY_EDITOR
using GooglePlayGames;
using GooglePlayGames.BasicApi;
using GooglePlayGames.BasicApi.SavedGame;
#endif

#if UNITY_IOS && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

#if (UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_STANDALONE_LINUX) && SW_STEAMWORKS_NET
using Steamworks;
#endif

namespace SWUtils
{
    /// <summary>
    /// 플랫폼별 클라우드 저장소를 통합 관리한다.
    /// Android: Google Play Games Saved Games (SW_GOOGLEPLAY_ENABLE 디파인 필요)
    /// iOS: iCloud Key-Value Storage (네이티브 플러그인 필요)
    /// Standalone: Steamworks Remote Storage (SW_STEAMWORKS_NET 디파인 필요)
    /// SDK가 없거나 에디터에서는 로컬 PlayerPrefs로 폴백한다.
    /// </summary>
    public static class SWUtilsCloud
    {
        #region 필드
        /// <summary>기본 저장 슬롯 이름.</summary>
        private const string DefaultSaveName = "swutils_save";
        /// <summary>로컬 폴백 PlayerPrefs 키 prefix.</summary>
        private const string LocalFallbackKey = "SwUtilsCloud_Local";
        /// <summary>초기화 완료 여부.</summary>
        private static bool isInitialized;
        /// <summary>인증 완료 여부.</summary>
        private static bool isAuthenticated;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>초기화 여부.</summary>
        public static bool IsInitialized => isInitialized;

        /// <summary>인증 여부.</summary>
        public static bool IsAuthenticated => isAuthenticated;

        /// <summary>현재 플랫폼에서 클라우드를 사용 가능한지 여부.</summary>
        public static bool IsAvailable
        {
            get
            {
#if UNITY_ANDROID && SW_GOOGLEPLAY_ENABLE && !UNITY_EDITOR
                return isAuthenticated;
#elif UNITY_IOS && !UNITY_EDITOR
                return true;
#elif (UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_STANDALONE_LINUX) && SW_STEAMWORKS_NET
                return SteamManager.Initialized;
#else
                return false;
#endif
            }
        }

        /// <summary>현재 사용 중인 클라우드 종류 이름.</summary>
        public static string ProviderName
        {
            get
            {
#if UNITY_ANDROID && SW_GOOGLEPLAY_ENABLE && !UNITY_EDITOR
                return "Google Play Games";
#elif UNITY_IOS && !UNITY_EDITOR
                return "iCloud";
#elif (UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_STANDALONE_LINUX) && SW_STEAMWORKS_NET
                return "Steam Cloud";
#else
                return "Local Fallback";
#endif
            }
        }
        #endregion // 프로퍼티

        #region iOS 네이티브 바인딩
#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern string _GetiCloudData(string key);

        [DllImport("__Internal")]
        private static extern bool _SetiCloudData(string key, string value);

        [DllImport("__Internal")]
        private static extern void _ForceSynciCloudData();

        /// <summary>
        /// iCloud에서 Base64로 디코딩된 문자열을 가져온다.
        /// </summary>
        /// <param name="key">저장 키</param>
        /// <returns>디코딩된 문자열. 실패 시 빈 문자열</returns>
        private static string iCloudGet(string key)
        {
            string raw = _GetiCloudData(key);
            if (string.IsNullOrEmpty(raw)) return string.Empty;
            try
            {
                return Encoding.UTF8.GetString(Convert.FromBase64String(raw));
            }
            catch (Exception exception)
            {
                SWUtilsLog.LogError($"[SWUtilsCloud] iCloud Base64 디코딩 실패: {exception.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// iCloud에 Base64로 인코딩하여 저장한다.
        /// </summary>
        /// <param name="key">저장 키</param>
        /// <param name="value">저장할 값</param>
        /// <returns>저장 성공 여부</returns>
        private static bool iCloudSet(string key, string value)
        {
            try
            {
                string encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
                return _SetiCloudData(key, encoded);
            }
            catch (Exception exception)
            {
                SWUtilsLog.LogError($"[SWUtilsCloud] iCloud Base64 인코딩 실패: {exception.Message}");
                return false;
            }
        }
#endif
        #endregion // iOS 네이티브 바인딩

        #region 초기화
        /// <summary>
        /// 클라우드 시스템을 초기화하고 인증을 시도한다. (콜백)
        /// </summary>
        /// <param name="onComplete">완료 콜백 (성공 여부)</param>
        public static void Init(Action<bool> onComplete = null)
        {
            if (isInitialized)
            {
                onComplete?.Invoke(isAuthenticated);
                return;
            }

#if UNITY_ANDROID && SW_GOOGLEPLAY_ENABLE && !UNITY_EDITOR
            PlayGamesPlatform.Activate();
            PlayGamesPlatform.Instance.Authenticate((status) =>
            {
                isAuthenticated = (status == SignInStatus.Success);
                isInitialized = true;
                SWUtilsLog.Log($"[SWUtilsCloud] GPGS 인증 결과: {status}");
                onComplete?.Invoke(isAuthenticated);
            });
#elif UNITY_IOS && !UNITY_EDITOR
            _ForceSynciCloudData();
            isAuthenticated = true;
            isInitialized = true;
            SWUtilsLog.Log("[SWUtilsCloud] iCloud 초기화 완료");
            onComplete?.Invoke(true);
#elif (UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_STANDALONE_LINUX) && SW_STEAMWORKS_NET
            isAuthenticated = SteamManager.Initialized;
            isInitialized = true;
            SWUtilsLog.Log($"[SWUtilsCloud] Steam 초기화 상태: {isAuthenticated}");
            onComplete?.Invoke(isAuthenticated);
#else
            isInitialized = true;
            isAuthenticated = false;
            SWUtilsLog.LogWarning("[SWUtilsCloud] 활성화된 클라우드 SDK가 없어 로컬 fallback을 사용합니다.");
            onComplete?.Invoke(false);
#endif
        }

        /// <summary>
        /// 클라우드 시스템을 초기화하고 인증을 시도한다. (async)
        /// </summary>
        /// <returns>성공 여부</returns>
        public static Task<bool> InitAsync()
        {
            var taskCompletionSource = new TaskCompletionSource<bool>();
            Init(success => taskCompletionSource.SetResult(success));
            return taskCompletionSource.Task;
        }
        #endregion // 초기화

        #region 저장
        /// <summary>
        /// JSON 문자열을 클라우드에 저장한다. (콜백)
        /// </summary>
        /// <param name="json">저장할 JSON 문자열</param>
        /// <param name="onComplete">완료 콜백 (성공 여부)</param>
        /// <param name="saveName">저장 슬롯 이름</param>
        public static void Save(string json, Action<bool> onComplete = null, string saveName = DefaultSaveName)
        {
            saveName = NormalizeSaveName(saveName);

            if (string.IsNullOrEmpty(json))
            {
                SWUtilsLog.LogError("[SWUtilsCloud] 저장 실패: JSON 문자열이 비어 있습니다.");
                onComplete?.Invoke(false);
                return;
            }

            SWUtilsLog.Log($"[SWUtilsCloud] 저장 시작 (길이: {json.Length}, 제공자: {ProviderName})");

#if UNITY_ANDROID && SW_GOOGLEPLAY_ENABLE && !UNITY_EDITOR
            if (!isAuthenticated)
            {
                SWUtilsLog.LogWarning("[SWUtilsCloud] GPGS 인증이 없어 로컬에만 저장합니다.");
                SaveLocalAndComplete(json, saveName, onComplete, false);
                return;
            }

            PlayGamesPlatform.Instance.SavedGame.OpenWithAutomaticConflictResolution(
                saveName,
                DataSource.ReadCacheOrNetwork,
                ConflictResolutionStrategy.UseLongestPlaytime,
                (status, metadata) =>
                {
                    if (status != SavedGameRequestStatus.Success)
                    {
                        SWUtilsLog.LogError($"[SWUtilsCloud] GPGS 저장 데이터 열기 실패: {status}");
                        SaveLocalAndComplete(json, saveName, onComplete, false);
                        return;
                    }

                    var update = new SavedGameMetadataUpdate.Builder()
                        .WithUpdatedDescription($"Saved at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}")
                        .Build();

                    byte[] data = Encoding.UTF8.GetBytes(json);
                    PlayGamesPlatform.Instance.SavedGame.CommitUpdate(metadata, update, data, (commitStatus, _) =>
                    {
                        bool success = commitStatus == SavedGameRequestStatus.Success;
                        SWUtilsLog.Log($"[SWUtilsCloud] GPGS 저장 커밋 결과: {commitStatus}");
                        SaveLocalAndComplete(json, saveName, onComplete, success);
                    });
                });
#elif UNITY_IOS && !UNITY_EDITOR
            try
            {
                bool result = iCloudSet(saveName, json);
                _ForceSynciCloudData();
                SWUtilsLog.Log($"[SWUtilsCloud] iCloud 저장 결과: {result}");
                SaveLocalAndComplete(json, saveName, onComplete, result);
            }
            catch (Exception exception)
            {
                SWUtilsLog.LogError($"[SWUtilsCloud] iCloud 저장 실패: {exception.Message}");
                SaveLocalAndComplete(json, saveName, onComplete, false);
            }
#elif (UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_STANDALONE_LINUX) && SW_STEAMWORKS_NET
            if (!SteamManager.Initialized)
            {
                SWUtilsLog.LogWarning("[SWUtilsCloud] Steam이 초기화되지 않아 로컬에만 저장합니다.");
                SaveLocalAndComplete(json, saveName, onComplete, false);
                return;
            }

            try
            {
                byte[] data = Encoding.UTF8.GetBytes(json);
                string fileName = saveName + ".json";
                bool result = SteamRemoteStorage.FileWrite(fileName, data, data.Length);
                SWUtilsLog.Log($"[SWUtilsCloud] Steam 저장 결과: {result}");
                SaveLocalAndComplete(json, saveName, onComplete, result);
            }
            catch (Exception exception)
            {
                SWUtilsLog.LogError($"[SWUtilsCloud] Steam 저장 실패: {exception.Message}");
                SaveLocalAndComplete(json, saveName, onComplete, false);
            }
#else
            SWUtilsLog.Log("[SWUtilsCloud] 로컬 fallback 저장");
            SaveLocalAndComplete(json, saveName, onComplete, true);
#endif
        }

        /// <summary>
        /// JSON 문자열을 클라우드에 저장한다. (async)
        /// </summary>
        /// <param name="json">저장할 JSON 문자열</param>
        /// <param name="saveName">저장 슬롯 이름</param>
        /// <returns>성공 여부</returns>
        public static Task<bool> SaveAsync(string json, string saveName = DefaultSaveName)
        {
            var taskCompletionSource = new TaskCompletionSource<bool>();
            Save(json, success => taskCompletionSource.SetResult(success), saveName);
            return taskCompletionSource.Task;
        }
        #endregion // 저장

        #region 로드
        /// <summary>
        /// 클라우드에서 JSON 문자열을 로드한다. (콜백)
        /// </summary>
        /// <param name="onComplete">완료 콜백 (성공 여부, JSON 문자열)</param>
        /// <param name="saveName">저장 슬롯 이름</param>
        public static void Load(Action<bool, string> onComplete, string saveName = DefaultSaveName)
        {
            saveName = NormalizeSaveName(saveName);
            SWUtilsLog.Log($"[SWUtilsCloud] 로드 시작 (제공자: {ProviderName})");

#if UNITY_ANDROID && SW_GOOGLEPLAY_ENABLE && !UNITY_EDITOR
            if (!isAuthenticated)
            {
                SWUtilsLog.LogWarning("[SWUtilsCloud] GPGS 인증이 없어 로컬에서만 로드합니다.");
                LoadLocalAndComplete(saveName, onComplete);
                return;
            }

            PlayGamesPlatform.Instance.SavedGame.OpenWithAutomaticConflictResolution(
                saveName,
                DataSource.ReadCacheOrNetwork,
                ConflictResolutionStrategy.UseLongestPlaytime,
                (status, metadata) =>
                {
                    if (status != SavedGameRequestStatus.Success)
                    {
                        SWUtilsLog.LogError($"[SWUtilsCloud] GPGS 저장 데이터 열기 실패: {status}");
                        LoadLocalAndComplete(saveName, onComplete);
                        return;
                    }

                    PlayGamesPlatform.Instance.SavedGame.ReadBinaryData(metadata, (readStatus, data) =>
                    {
                        if (readStatus != SavedGameRequestStatus.Success || data == null || data.Length == 0)
                        {
                            SWUtilsLog.LogWarning($"[SWUtilsCloud] GPGS 읽기 결과가 비어 있습니다: {readStatus}");
                            LoadLocalAndComplete(saveName, onComplete);
                            return;
                        }

                        string json = Encoding.UTF8.GetString(data);
                        SWUtilsLog.Log($"[SWUtilsCloud] GPGS 로드 성공 (길이: {json.Length})");
                        CacheLocalAndComplete(json, saveName, onComplete, true);
                    });
                });
#elif UNITY_IOS && !UNITY_EDITOR
            try
            {
                _ForceSynciCloudData();
                string json = iCloudGet(saveName);
                if (string.IsNullOrEmpty(json))
                {
                    SWUtilsLog.LogWarning("[SWUtilsCloud] iCloud 데이터가 비어 있어 로컬 fallback으로 로드합니다.");
                    LoadLocalAndComplete(saveName, onComplete);
                }
                else
                {
                    SWUtilsLog.Log($"[SWUtilsCloud] iCloud 로드 성공 (길이: {json.Length})");
                    CacheLocalAndComplete(json, saveName, onComplete, true);
                }
            }
            catch (Exception exception)
            {
                SWUtilsLog.LogError($"[SWUtilsCloud] iCloud 로드 실패: {exception.Message}");
                LoadLocalAndComplete(saveName, onComplete);
            }
#elif (UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_STANDALONE_LINUX) && SW_STEAMWORKS_NET
            if (!SteamManager.Initialized)
            {
                SWUtilsLog.LogWarning("[SWUtilsCloud] Steam이 초기화되지 않아 로컬에서만 로드합니다.");
                LoadLocalAndComplete(saveName, onComplete);
                return;
            }

            try
            {
                string fileName = saveName + ".json";
                if (!SteamRemoteStorage.FileExists(fileName))
                {
                    SWUtilsLog.LogWarning("[SWUtilsCloud] Steam 파일을 찾을 수 없어 로컬 fallback으로 로드합니다.");
                    LoadLocalAndComplete(saveName, onComplete);
                    return;
                }

                int size = SteamRemoteStorage.GetFileSize(fileName);
                byte[] data = new byte[size];
                int read = SteamRemoteStorage.FileRead(fileName, data, size);

                if (read <= 0)
                {
                    SWUtilsLog.LogWarning("[SWUtilsCloud] Steam 읽기 결과가 비어 있어 로컬 fallback으로 로드합니다.");
                    LoadLocalAndComplete(saveName, onComplete);
                    return;
                }

                // FIX: read 바이트 수만큼만 디코딩 (read < size일 수 있음)
                string json = Encoding.UTF8.GetString(data, 0, read);
                SWUtilsLog.Log($"[SWUtilsCloud] Steam 로드 성공 (길이: {json.Length})");
                CacheLocalAndComplete(json, saveName, onComplete, true);
            }
            catch (Exception exception)
            {
                SWUtilsLog.LogError($"[SWUtilsCloud] Steam 로드 실패: {exception.Message}");
                LoadLocalAndComplete(saveName, onComplete);
            }
#else
            LoadLocalAndComplete(saveName, onComplete);
#endif
        }

        /// <summary>
        /// 클라우드에서 JSON 문자열을 로드한다. (async)
        /// </summary>
        /// <param name="saveName">저장 슬롯 이름</param>
        /// <returns>(성공 여부, JSON 문자열) 튜플</returns>
        public static Task<(bool success, string json)> LoadAsync(string saveName = DefaultSaveName)
        {
            var taskCompletionSource = new TaskCompletionSource<(bool, string)>();
            Load((success, json) => taskCompletionSource.SetResult((success, json)), saveName);
            return taskCompletionSource.Task;
        }
        #endregion // 로드

        #region 삭제
        /// <summary>
        /// 클라우드에서 저장 데이터를 삭제한다. (콜백)
        /// </summary>
        /// <param name="onComplete">완료 콜백 (성공 여부)</param>
        /// <param name="saveName">저장 슬롯 이름</param>
        public static void Delete(Action<bool> onComplete = null, string saveName = DefaultSaveName)
        {
            saveName = NormalizeSaveName(saveName);
            SWUtilsLog.Log($"[SWUtilsCloud] 삭제 시작 (제공자: {ProviderName})");

#if UNITY_ANDROID && SW_GOOGLEPLAY_ENABLE && !UNITY_EDITOR
            if (!isAuthenticated)
            {
                DeleteLocal(saveName);
                onComplete?.Invoke(false);
                return;
            }

            PlayGamesPlatform.Instance.SavedGame.OpenWithAutomaticConflictResolution(
                saveName,
                DataSource.ReadCacheOrNetwork,
                ConflictResolutionStrategy.UseLongestPlaytime,
                (status, metadata) =>
                {
                    if (status == SavedGameRequestStatus.Success)
                    {
                        PlayGamesPlatform.Instance.SavedGame.Delete(metadata);
                        DeleteLocal(saveName);
                        SWUtilsLog.Log("[SWUtilsCloud] GPGS 삭제 성공");
                        onComplete?.Invoke(true);
                    }
                    else
                    {
                        SWUtilsLog.LogError($"[SWUtilsCloud] GPGS 삭제 실패: {status}");
                        onComplete?.Invoke(false);
                    }
                });
#elif UNITY_IOS && !UNITY_EDITOR
            try
            {
                bool result = iCloudSet(saveName, string.Empty);
                _ForceSynciCloudData();
                DeleteLocal(saveName);
                SWUtilsLog.Log($"[SWUtilsCloud] iCloud 삭제 결과: {result}");
                onComplete?.Invoke(result);
            }
            catch (Exception exception)
            {
                SWUtilsLog.LogError($"[SWUtilsCloud] iCloud 삭제 실패: {exception.Message}");
                onComplete?.Invoke(false);
            }
#elif (UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_STANDALONE_LINUX) && SW_STEAMWORKS_NET
            try
            {
                string fileName = saveName + ".json";
                bool result = SteamRemoteStorage.FileExists(fileName)
                    && SteamRemoteStorage.FileDelete(fileName);
                DeleteLocal(saveName);
                SWUtilsLog.Log($"[SWUtilsCloud] Steam 삭제 결과: {result}");
                onComplete?.Invoke(result);
            }
            catch (Exception exception)
            {
                SWUtilsLog.LogError($"[SWUtilsCloud] Steam 삭제 실패: {exception.Message}");
                onComplete?.Invoke(false);
            }
#else
            DeleteLocal(saveName);
            SWUtilsLog.Log("[SWUtilsCloud] 로컬 fallback 삭제");
            onComplete?.Invoke(true);
#endif
        }

        /// <summary>
        /// 클라우드에서 저장 데이터를 삭제한다. (async)
        /// </summary>
        /// <param name="saveName">저장 슬롯 이름</param>
        /// <returns>성공 여부</returns>
        public static Task<bool> DeleteAsync(string saveName = DefaultSaveName)
        {
            var taskCompletionSource = new TaskCompletionSource<bool>();
            Delete(success => taskCompletionSource.SetResult(success), saveName);
            return taskCompletionSource.Task;
        }
        #endregion // 삭제

        #region JSON Helper
        /// <summary>
        /// 객체를 JSON으로 직렬화하여 클라우드에 저장한다. (콜백)
        /// </summary>
        public static void SaveJson<T>(T data, Action<bool> onComplete = null, string saveName = DefaultSaveName)
        {
            if (data == null)
            {
                SWUtilsLog.LogError("[SWUtilsCloud] SaveJson 실패: data가 null입니다.");
                onComplete?.Invoke(false);
                return;
            }

            Save(JsonUtility.ToJson(data), onComplete, saveName);
        }

        /// <summary>
        /// 객체를 JSON으로 직렬화하여 클라우드에 저장한다. (async)
        /// </summary>
        public static Task<bool> SaveJsonAsync<T>(T data, string saveName = DefaultSaveName)
        {
            if (data == null)
            {
                SWUtilsLog.LogError("[SWUtilsCloud] SaveJsonAsync 실패: data가 null입니다.");
                return Task.FromResult(false);
            }

            return SaveAsync(JsonUtility.ToJson(data), saveName);
        }

        /// <summary>
        /// 클라우드에서 JSON을 로드하여 객체로 역직렬화한다. (콜백)
        /// </summary>
        public static void LoadJson<T>(Action<bool, T> onComplete, string saveName = DefaultSaveName) where T : class
        {
            Load((success, json) =>
            {
                if (!success || string.IsNullOrEmpty(json))
                {
                    onComplete?.Invoke(false, default);
                    return;
                }

                try
                {
                    T data = JsonUtility.FromJson<T>(json);
                    onComplete?.Invoke(data != null, data);
                }
                catch (Exception exception)
                {
                    SWUtilsLog.LogError($"[SWUtilsCloud] LoadJson 실패: {exception.Message}");
                    onComplete?.Invoke(false, default);
                }
            }, saveName);
        }

        /// <summary>
        /// 클라우드에서 JSON을 로드하여 객체로 역직렬화한다. (async)
        /// </summary>
        public static async Task<(bool success, T data)> LoadJsonAsync<T>(string saveName = DefaultSaveName) where T : class
        {
            var (success, json) = await LoadAsync(saveName);
            if (!success || string.IsNullOrEmpty(json)) return (false, default);

            try
            {
                T data = JsonUtility.FromJson<T>(json);
                return (data != null, data);
            }
            catch (Exception exception)
            {
                SWUtilsLog.LogError($"[SWUtilsCloud] LoadJsonAsync 실패: {exception.Message}");
                return (false, default);
            }
        }
        #endregion // JSON Helper

        #region 로컬 폴백
        /// <summary>
        /// 비어 있거나 공백인 저장 이름을 기본값으로 정규화한다.
        /// </summary>
        /// <param name="saveName">저장 이름</param>
        /// <returns>정규화된 저장 이름</returns>
        private static string NormalizeSaveName(string saveName)
        {
            return string.IsNullOrWhiteSpace(saveName) ? DefaultSaveName : saveName.Trim();
        }

        /// <summary>
        /// 로컬에 저장하고 Action&lt;bool&gt; 콜백을 호출한다.
        /// </summary>
        private static void SaveLocalAndComplete(string json, string saveName, Action<bool> onComplete, bool success)
        {
            SaveLocal(json, saveName);
            onComplete?.Invoke(success);
        }

        /// <summary>
        /// 클라우드 로드 성공 시 로컬 캐시를 갱신하고 Action&lt;bool, string&gt; 콜백을 호출한다.
        /// </summary>
        private static void CacheLocalAndComplete(string json, string saveName, Action<bool, string> onComplete, bool success)
        {
            SaveLocal(json, saveName);
            onComplete?.Invoke(success, json);
        }

        /// <summary>
        /// 로컬 PlayerPrefs에서 폴백 로드한 뒤 콜백을 호출한다.
        /// </summary>
        private static void LoadLocalAndComplete(string saveName, Action<bool, string> onComplete)
        {
            string local = LoadLocal(saveName);
            SWUtilsLog.Log($"[SWUtilsCloud] 로컬 fallback 로드 (길이: {local?.Length ?? 0})");
            onComplete?.Invoke(!string.IsNullOrEmpty(local), local);
        }

        /// <summary>
        /// 로컬 PlayerPrefs에 fallback 데이터를 저장한다.
        /// </summary>
        /// <param name="json">저장할 JSON 문자열</param>
        /// <param name="saveName">저장 이름</param>
        private static void SaveLocal(string json, string saveName)
        {
            SWUtilsPlayerPrefs.SetString($"{LocalFallbackKey}_{saveName}", json);
            SWUtilsPlayerPrefs.Save();
        }

        /// <summary>
        /// 로컬 PlayerPrefs에서 폴백 로드한다.
        /// </summary>
        /// <param name="saveName">저장 슬롯 이름</param>
        /// <returns>저장된 JSON 문자열</returns>
        private static string LoadLocal(string saveName)
        {
            return SWUtilsPlayerPrefs.GetString($"{LocalFallbackKey}_{saveName}", string.Empty);
        }

        /// <summary>
        /// 로컬 폴백 데이터를 삭제한다.
        /// </summary>
        /// <param name="saveName">저장 슬롯 이름</param>
        private static void DeleteLocal(string saveName)
        {
            SWUtilsPlayerPrefs.DeleteKey($"{LocalFallbackKey}_{saveName}");
            SWUtilsPlayerPrefs.Save();
        }
        #endregion // 로컬 폴백

        #region SWUtilsPlayerPrefs 통합
        /// <summary>
        /// SWUtilsPlayerPrefs의 모든 데이터를 클라우드에 백업한다. (콜백)
        /// </summary>
        /// <param name="onComplete">완료 콜백 (성공 여부)</param>
        /// <param name="saveName">저장 슬롯 이름</param>
        public static void BackupPrefs(Action<bool> onComplete = null, string saveName = DefaultSaveName)
        {
            string json = SWUtilsPlayerPrefs.ExportToJson(IsCloudBackupKey);
            Save(json, onComplete, saveName);
        }

        /// <summary>
        /// SWUtilsPlayerPrefs의 모든 데이터를 클라우드에 백업한다. (async)
        /// </summary>
        /// <param name="saveName">저장 슬롯 이름</param>
        /// <returns>성공 여부</returns>
        public static Task<bool> BackupPrefsAsync(string saveName = DefaultSaveName)
        {
            return SaveAsync(SWUtilsPlayerPrefs.ExportToJson(IsCloudBackupKey), saveName);
        }

        private static bool IsCloudBackupKey(string key)
        {
            return !key.StartsWith(LocalFallbackKey, StringComparison.Ordinal);
        }

        /// <summary>
        /// 클라우드에서 데이터를 받아 SWUtilsPlayerPrefs로 복원한다. (콜백)
        /// </summary>
        /// <param name="onComplete">완료 콜백 (성공 여부)</param>
        /// <param name="merge">true면 병합, false면 전체 덮어쓰기</param>
        /// <param name="saveName">저장 슬롯 이름</param>
        public static void RestorePrefs(Action<bool> onComplete = null, bool merge = false, string saveName = DefaultSaveName)
        {
            Load((success, json) =>
            {
                if (!success || string.IsNullOrEmpty(json))
                {
                    onComplete?.Invoke(false);
                    return;
                }

                bool result = merge
                    ? SWUtilsPlayerPrefs.MergeFromJson(json)
                    : SWUtilsPlayerPrefs.ImportFromJson(json);
                onComplete?.Invoke(result);
            }, saveName);
        }

        /// <summary>
        /// 클라우드에서 데이터를 받아 SWUtilsPlayerPrefs로 복원한다. (async)
        /// </summary>
        /// <param name="merge">true면 병합, false면 전체 덮어쓰기</param>
        /// <param name="saveName">저장 슬롯 이름</param>
        /// <returns>성공 여부</returns>
        public static async Task<bool> RestorePrefsAsync(bool merge = false, string saveName = DefaultSaveName)
        {
            var (success, json) = await LoadAsync(saveName);
            if (!success || string.IsNullOrEmpty(json)) return false;

            return merge
                ? SWUtilsPlayerPrefs.MergeFromJson(json)
                : SWUtilsPlayerPrefs.ImportFromJson(json);
        }
        #endregion // SWUtilsPlayerPrefs 통합
    }
}
