using System;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

#if UNITY_ANDROID && GPGS_ENABLED && !UNITY_EDITOR
using GooglePlayGames;
using GooglePlayGames.BasicApi;
using GooglePlayGames.BasicApi.SavedGame;
#endif

#if UNITY_IOS && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

#if (UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_STANDALONE_LINUX) && STEAMWORKS_NET
using Steamworks;
#endif

namespace SWUtils
{
    /// <summary>
    /// 플랫폼별 클라우드 저장소를 통합 관리한다.
    /// Android: Google Play Games Saved Games (GPGS_ENABLED 디파인 필요)
    /// iOS: iCloud Key-Value Storage (네이티브 플러그인 필요)
    /// Standalone: Steamworks Remote Storage (STEAMWORKS_NET 디파인 필요)
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
#if UNITY_ANDROID && GPGS_ENABLED && !UNITY_EDITOR
                return isAuthenticated;
#elif UNITY_IOS && !UNITY_EDITOR
                return true;
#elif (UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_STANDALONE_LINUX) && STEAMWORKS_NET
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
#if UNITY_ANDROID && GPGS_ENABLED && !UNITY_EDITOR
                return "Google Play Games";
#elif UNITY_IOS && !UNITY_EDITOR
                return "iCloud";
#elif (UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_STANDALONE_LINUX) && STEAMWORKS_NET
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
        private static extern void _DeleteiCloudData();

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

#if UNITY_ANDROID && GPGS_ENABLED && !UNITY_EDITOR
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
#elif (UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_STANDALONE_LINUX) && STEAMWORKS_NET
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
            if (string.IsNullOrEmpty(json))
            {
                Debug.LogError("[SWUtilsCloud] Save failed: empty json");
                onComplete?.Invoke(false);
                return;
            }

            Debug.Log($"[SWUtilsCloud] Save begin (length: {json.Length}, provider: {ProviderName})");

#if UNITY_ANDROID && GPGS_ENABLED && !UNITY_EDITOR
            if (!isAuthenticated)
            {
                Debug.LogWarning("[SWUtilsCloud] GPGS not authenticated. Saving local only.");
                SaveLocal(json, saveName);
                onComplete?.Invoke(false);
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
                        SaveLocal(json, saveName);
                        onComplete?.Invoke(false);
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
                        SaveLocal(json, saveName);
                        onComplete?.Invoke(success);
                    });
                });
#elif UNITY_IOS && !UNITY_EDITOR
            try
            {
                bool result = iCloudSet(saveName, json);
                _ForceSynciCloudData();
                SaveLocal(json, saveName);
                Debug.Log($"[SWUtilsCloud] iCloud save: {result}");
                onComplete?.Invoke(result);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[SWUtilsCloud] iCloud save failed: {exception.Message}");
                SaveLocal(json, saveName);
                onComplete?.Invoke(false);
            }
#elif (UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_STANDALONE_LINUX) && STEAMWORKS_NET
            if (!SteamManager.Initialized)
            {
                Debug.LogWarning("[SWUtilsCloud] Steam not initialized. Saving local only.");
                SaveLocal(json, saveName);
                onComplete?.Invoke(false);
                return;
            }

            try
            {
                byte[] data = Encoding.UTF8.GetBytes(json);
                string fileName = saveName + ".json";
                bool result = SteamRemoteStorage.FileWrite(fileName, data, data.Length);
                Debug.Log($"[SWUtilsCloud] Steam save: {result}");
                SaveLocal(json, saveName);
                onComplete?.Invoke(result);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[SWUtilsCloud] Steam save failed: {exception.Message}");
                SaveLocal(json, saveName);
                onComplete?.Invoke(false);
            }
#else
            SaveLocal(json, saveName);
            Debug.Log("[SWUtilsCloud] Local fallback save");
            onComplete?.Invoke(true);
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
            Debug.Log($"[SWUtilsCloud] Load begin (provider: {ProviderName})");

#if UNITY_ANDROID && GPGS_ENABLED && !UNITY_EDITOR
            if (!isAuthenticated)
            {
                Debug.LogWarning("[SWUtilsCloud] GPGS not authenticated. Loading local only.");
                string local = LoadLocal(saveName);
                onComplete?.Invoke(!string.IsNullOrEmpty(local), local);
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
                        string local = LoadLocal(saveName);
                        onComplete?.Invoke(!string.IsNullOrEmpty(local), local);
                        return;
                    }

                    PlayGamesPlatform.Instance.SavedGame.ReadBinaryData(metadata, (readStatus, data) =>
                    {
                        if (readStatus != SavedGameRequestStatus.Success || data == null || data.Length == 0)
                        {
                            Debug.LogWarning($"[SWUtilsCloud] GPGS read empty: {readStatus}");
                            string local = LoadLocal(saveName);
                            onComplete?.Invoke(!string.IsNullOrEmpty(local), local);
                            return;
                        }

                        string json = Encoding.UTF8.GetString(data);
                        Debug.Log($"[SWUtilsCloud] GPGS load success (length: {json.Length})");
                        onComplete?.Invoke(true, json);
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
                    string local = LoadLocal(saveName);
                    onComplete?.Invoke(!string.IsNullOrEmpty(local), local);
                }
                else
                {
                    Debug.Log($"[SWUtilsCloud] iCloud load success (length: {json.Length})");
                    onComplete?.Invoke(true, json);
                }
            }
            catch (Exception exception)
            {
                Debug.LogError($"[SWUtilsCloud] iCloud load failed: {exception.Message}");
                string local = LoadLocal(saveName);
                onComplete?.Invoke(!string.IsNullOrEmpty(local), local);
            }
#elif (UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_STANDALONE_LINUX) && STEAMWORKS_NET
            if (!SteamManager.Initialized)
            {
                Debug.LogWarning("[SWUtilsCloud] Steam not initialized. Loading local only.");
                string local = LoadLocal(saveName);
                onComplete?.Invoke(!string.IsNullOrEmpty(local), local);
                return;
            }

            try
            {
                string fileName = saveName + ".json";
                if (!SteamRemoteStorage.FileExists(fileName))
                {
                    Debug.LogWarning("[SWUtilsCloud] Steam file not found, fallback to local");
                    string local = LoadLocal(saveName);
                    onComplete?.Invoke(!string.IsNullOrEmpty(local), local);
                    return;
                }

                int size = SteamRemoteStorage.GetFileSize(fileName);
                byte[] data = new byte[size];
                int read = SteamRemoteStorage.FileRead(fileName, data, size);

                if (read <= 0)
                {
                    Debug.LogWarning("[SWUtilsCloud] Steam read empty, fallback to local");
                    string local = LoadLocal(saveName);
                    onComplete?.Invoke(!string.IsNullOrEmpty(local), local);
                    return;
                }

                string json = Encoding.UTF8.GetString(data);
                Debug.Log($"[SWUtilsCloud] Steam load success (length: {json.Length})");
                onComplete?.Invoke(true, json);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[SWUtilsCloud] Steam load failed: {exception.Message}");
                string local = LoadLocal(saveName);
                onComplete?.Invoke(!string.IsNullOrEmpty(local), local);
            }
#else
            string fallback = LoadLocal(saveName);
            Debug.Log($"[SWUtilsCloud] Local fallback load (length: {fallback?.Length ?? 0})");
            onComplete?.Invoke(!string.IsNullOrEmpty(fallback), fallback);
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
            Debug.Log($"[SWUtilsCloud] Delete begin (provider: {ProviderName})");

#if UNITY_ANDROID && GPGS_ENABLED && !UNITY_EDITOR
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
                _DeleteiCloudData();
                _ForceSynciCloudData();
                DeleteLocal(saveName);
                Debug.Log("[SWUtilsCloud] iCloud delete success");
                onComplete?.Invoke(true);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[SWUtilsCloud] iCloud delete failed: {exception.Message}");
                onComplete?.Invoke(false);
            }
#elif (UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_STANDALONE_LINUX) && STEAMWORKS_NET
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

        #region 로컬 폴백
        /// <summary>
        /// 로컬 PlayerPrefs에 폴백 저장한다.
        /// </summary>
        /// <param name="json">저장할 JSON 문자열</param>
        /// <param name="saveName">저장 슬롯 이름</param>
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