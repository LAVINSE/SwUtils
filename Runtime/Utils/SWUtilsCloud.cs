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
                Debug.LogError($"[SWUtilsCloud] iCloud Base64 decode failed: {exception.Message}");
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
                Debug.LogError($"[SWUtilsCloud] iCloud Base64 encode failed: {exception.Message}");
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
                Debug.Log($"[SWUtilsCloud] GPGS auth: {status}");
                onComplete?.Invoke(isAuthenticated);
            });
#elif UNITY_IOS && !UNITY_EDITOR
            _ForceSynciCloudData();
            isAuthenticated = true;
            isInitialized = true;
            Debug.Log("[SWUtilsCloud] iCloud initialized");
            onComplete?.Invoke(true);
#elif (UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_STANDALONE_LINUX) && SW_STEAMWORKS_NET
            isAuthenticated = SteamManager.Initialized;
            isInitialized = true;
            Debug.Log($"[SWUtilsCloud] Steam initialized: {isAuthenticated}");
            onComplete?.Invoke(isAuthenticated);
#else
            isInitialized = true;
            isAuthenticated = false;
            Debug.LogWarning("[SWUtilsCloud] No cloud SDK enabled. Using local fallback.");
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
                Debug.LogError("[SWUtilsCloud] Save failed: empty json");
                onComplete?.Invoke(false);
                return;
            }

            Debug.Log($"[SWUtilsCloud] Save begin (length: {json.Length}, provider: {ProviderName})");

#if UNITY_ANDROID && SW_GOOGLEPLAY_ENABLE && !UNITY_EDITOR
            if (!isAuthenticated)
            {
                Debug.LogWarning("[SWUtilsCloud] GPGS not authenticated. Saving local only.");
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
                        Debug.LogError($"[SWUtilsCloud] GPGS open failed: {status}");
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
                        Debug.Log($"[SWUtilsCloud] GPGS commit: {commitStatus}");
                        SaveLocalAndComplete(json, saveName, onComplete, success);
                    });
                });
#elif UNITY_IOS && !UNITY_EDITOR
            try
            {
                bool result = iCloudSet(saveName, json);
                _ForceSynciCloudData();
                Debug.Log($"[SWUtilsCloud] iCloud save: {result}");
                SaveLocalAndComplete(json, saveName, onComplete, result);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[SWUtilsCloud] iCloud save failed: {exception.Message}");
                SaveLocalAndComplete(json, saveName, onComplete, false);
            }
#elif (UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_STANDALONE_LINUX) && SW_STEAMWORKS_NET
            if (!SteamManager.Initialized)
            {
                Debug.LogWarning("[SWUtilsCloud] Steam not initialized. Saving local only.");
                SaveLocalAndComplete(json, saveName, onComplete, false);
                return;
            }

            try
            {
                byte[] data = Encoding.UTF8.GetBytes(json);
                string fileName = saveName + ".json";
                bool result = SteamRemoteStorage.FileWrite(fileName, data, data.Length);
                Debug.Log($"[SWUtilsCloud] Steam save: {result}");
                SaveLocalAndComplete(json, saveName, onComplete, result);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[SWUtilsCloud] Steam save failed: {exception.Message}");
                SaveLocalAndComplete(json, saveName, onComplete, false);
            }
#else
            Debug.Log("[SWUtilsCloud] Local fallback save");
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
            Debug.Log($"[SWUtilsCloud] Load begin (provider: {ProviderName})");

#if UNITY_ANDROID && SW_GOOGLEPLAY_ENABLE && !UNITY_EDITOR
            if (!isAuthenticated)
            {
                Debug.LogWarning("[SWUtilsCloud] GPGS not authenticated. Loading local only.");
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
                        Debug.LogError($"[SWUtilsCloud] GPGS open failed: {status}");
                        LoadLocalAndComplete(saveName, onComplete);
                        return;
                    }

                    PlayGamesPlatform.Instance.SavedGame.ReadBinaryData(metadata, (readStatus, data) =>
                    {
                        if (readStatus != SavedGameRequestStatus.Success || data == null || data.Length == 0)
                        {
                            Debug.LogWarning($"[SWUtilsCloud] GPGS read empty: {readStatus}");
                            LoadLocalAndComplete(saveName, onComplete);
                            return;
                        }

                        string json = Encoding.UTF8.GetString(data);
                        Debug.Log($"[SWUtilsCloud] GPGS load success (length: {json.Length})");
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
                    Debug.LogWarning("[SWUtilsCloud] iCloud empty, fallback to local");
                    LoadLocalAndComplete(saveName, onComplete);
                }
                else
                {
                    Debug.Log($"[SWUtilsCloud] iCloud load success (length: {json.Length})");
                    CacheLocalAndComplete(json, saveName, onComplete, true);
                }
            }
            catch (Exception exception)
            {
                Debug.LogError($"[SWUtilsCloud] iCloud load failed: {exception.Message}");
                LoadLocalAndComplete(saveName, onComplete);
            }
#elif (UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_STANDALONE_LINUX) && SW_STEAMWORKS_NET
            if (!SteamManager.Initialized)
            {
                Debug.LogWarning("[SWUtilsCloud] Steam not initialized. Loading local only.");
                LoadLocalAndComplete(saveName, onComplete);
                return;
            }

            try
            {
                string fileName = saveName + ".json";
                if (!SteamRemoteStorage.FileExists(fileName))
                {
                    Debug.LogWarning("[SWUtilsCloud] Steam file not found, fallback to local");
                    LoadLocalAndComplete(saveName, onComplete);
                    return;
                }

                int size = SteamRemoteStorage.GetFileSize(fileName);
                byte[] data = new byte[size];
                int read = SteamRemoteStorage.FileRead(fileName, data, size);

                if (read <= 0)
                {
                    Debug.LogWarning("[SWUtilsCloud] Steam read empty, fallback to local");
                    LoadLocalAndComplete(saveName, onComplete);
                    return;
                }

                // FIX: read 바이트 수만큼만 디코딩 (read < size일 수 있음)
                string json = Encoding.UTF8.GetString(data, 0, read);
                Debug.Log($"[SWUtilsCloud] Steam load success (length: {json.Length})");
                CacheLocalAndComplete(json, saveName, onComplete, true);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[SWUtilsCloud] Steam load failed: {exception.Message}");
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
            Debug.Log($"[SWUtilsCloud] Delete begin (provider: {ProviderName})");

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
                        Debug.Log("[SWUtilsCloud] GPGS delete success");
                        onComplete?.Invoke(true);
                    }
                    else
                    {
                        Debug.LogError($"[SWUtilsCloud] GPGS delete failed: {status}");
                        onComplete?.Invoke(false);
                    }
                });
#elif UNITY_IOS && !UNITY_EDITOR
            try
            {
                bool result = iCloudSet(saveName, string.Empty);
                _ForceSynciCloudData();
                DeleteLocal(saveName);
                Debug.Log($"[SWUtilsCloud] iCloud delete success: {result}");
                onComplete?.Invoke(result);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[SWUtilsCloud] iCloud delete failed: {exception.Message}");
                onComplete?.Invoke(false);
            }
#elif (UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_STANDALONE_LINUX) && SW_STEAMWORKS_NET
            try
            {
                string fileName = saveName + ".json";
                bool result = SteamRemoteStorage.FileExists(fileName)
                    && SteamRemoteStorage.FileDelete(fileName);
                DeleteLocal(saveName);
                Debug.Log($"[SWUtilsCloud] Steam delete: {result}");
                onComplete?.Invoke(result);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[SWUtilsCloud] Steam delete failed: {exception.Message}");
                onComplete?.Invoke(false);
            }
#else
            DeleteLocal(saveName);
            Debug.Log("[SWUtilsCloud] Local fallback delete");
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
                Debug.LogError("[SWUtilsCloud] SaveJson failed: data is null");
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
                Debug.LogError("[SWUtilsCloud] SaveJsonAsync failed: data is null");
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
                    Debug.LogError($"[SWUtilsCloud] LoadJson failed: {exception.Message}");
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
                Debug.LogError($"[SWUtilsCloud] LoadJsonAsync failed: {exception.Message}");
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
            Debug.Log($"[SWUtilsCloud] Local fallback load (length: {local?.Length ?? 0})");
            onComplete?.Invoke(!string.IsNullOrEmpty(local), local);
        }

        /// <summary>
        /// 로컬 PlayerPrefs에 fallback 데이터를 저장한다.
        /// </summary>
        /// <param name="json">저장할 JSON 문자열</param>
        /// <param name="saveName">저장 이름</param>
        private static void SaveLocal(string json, string saveName)
        {
            PlayerPrefs.SetString($"{LocalFallbackKey}_{saveName}", json);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// 로컬 PlayerPrefs에서 폴백 로드한다.
        /// </summary>
        /// <param name="saveName">저장 슬롯 이름</param>
        /// <returns>저장된 JSON 문자열</returns>
        private static string LoadLocal(string saveName)
        {
            return PlayerPrefs.GetString($"{LocalFallbackKey}_{saveName}", string.Empty);
        }

        /// <summary>
        /// 로컬 폴백 데이터를 삭제한다.
        /// </summary>
        /// <param name="saveName">저장 슬롯 이름</param>
        private static void DeleteLocal(string saveName)
        {
            PlayerPrefs.DeleteKey($"{LocalFallbackKey}_{saveName}");
            PlayerPrefs.Save();
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
            string json = SWUtilsPlayerPrefs.ExportToJson();
            Save(json, onComplete, saveName);
        }

        /// <summary>
        /// SWUtilsPlayerPrefs의 모든 데이터를 클라우드에 백업한다. (async)
        /// </summary>
        /// <param name="saveName">저장 슬롯 이름</param>
        /// <returns>성공 여부</returns>
        public static Task<bool> BackupPrefsAsync(string saveName = DefaultSaveName)
        {
            return SaveAsync(SWUtilsPlayerPrefs.ExportToJson(), saveName);
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
