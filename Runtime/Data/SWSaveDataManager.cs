using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace SWUtils
{
    /// <summary>
    /// 게임 진행 데이터, SWUtilsPlayerPrefs, 클라우드 백업을 한 번에 관리하는 저장 매니저.
    /// SetData로 저장할 데이터 인스턴스를 등록한 뒤 SaveAll/LoadAll을 사용한다.
    /// </summary>
    public static class SWSaveDataManager
    {
        #region Nested Types
        /// <summary>
        /// 저장 슬롯 파일의 간단한 정보.
        /// </summary>
        public readonly struct SaveFileInfo
        {
            public SaveFileInfo(string slot, string fileName, string path, bool exists, long size, DateTime lastWriteTimeUtc)
            {
                Slot = slot;
                FileName = fileName;
                Path = path;
                Exists = exists;
                Size = size;
                LastWriteTimeUtc = lastWriteTimeUtc;
            }

            public string Slot { get; }
            public string FileName { get; }
            public string Path { get; }
            public bool Exists { get; }
            public long Size { get; }
            public DateTime LastWriteTimeUtc { get; }
        }

        [Serializable]
        private class CloudBackupData
        {
            public int version = 1;
            public string slot;
            public string saveDataJson;
            public string playerPrefsJson;
            public string savedAtUtc;
        }
        #endregion // Nested Types

        #region Fields
        public const string DefaultSlotName = SWSaveSlot.Default;
        private const string SaveDirectoryName = "SaveData";
        private const string SaveExtension = ".json";
        private const string BackupExtension = ".bak";
        private const string TempExtension = ".tmp";
        private const string CloudFallbackKeyPrefix = "SwUtilsCloud_Local";

        private static string currentSlot = DefaultSlotName;
        private static object currentData;
        private static Type currentDataType;
        #endregion // Fields

        #region Properties
        /// <summary>
        /// slot 인자를 생략했을 때 사용하는 현재 슬롯 이름.
        /// </summary>
        public static string CurrentSlot => currentSlot;

        /// <summary>
        /// SetData로 런타임 저장 데이터가 등록되어 있는지 여부.
        /// </summary>
        public static bool HasData => currentData != null;

        /// <summary>
        /// 현재 등록된 런타임 저장 데이터 타입.
        /// </summary>
        public static Type CurrentDataType => currentDataType;

        /// <summary>
        /// 기본 슬롯 이름. SWUtilsCloud의 기본 저장 이름과 동일하다.
        /// </summary>
        public static string DefaultSlot => DefaultSlotName;

        /// <summary>
        /// 저장 파일이 생성되는 폴더 경로.
        /// </summary>
        public static string SaveDirectoryPath => Path.Combine(Application.persistentDataPath, SaveDirectoryName);
        #endregion // Properties

        #region Slot
        /// <summary>
        /// 현재 기본 저장 슬롯을 변경한다. PlayerPrefs 슬롯도 같은 이름으로 맞춘다.
        /// </summary>
        public static void SetSlot(string slot)
        {
            currentSlot = NormalizeSlotName(slot);
            SWUtilsPlayerPrefs.SetSlot(currentSlot);
        }
        #endregion // Slot

        #region Data
        /// <summary>
        /// 런타임 저장 데이터를 등록한다. 이후 SaveAll/LoadAll은 이 데이터 타입을 기준으로 동작한다.
        /// </summary>
        public static void SetData<T>(T data) where T : class
        {
            if (data == null)
            {
                SWUtilsLog.LogWarning("[SWSaveDataManager] SetData skipped. Data is null.");
                return;
            }

            currentData = data;
            currentDataType = data.GetType();
        }

        /// <summary>
        /// 등록된 런타임 저장 데이터를 반환한다.
        /// </summary>
        public static T GetData<T>() where T : class
        {
            return TryGetData(out T data) ? data : null;
        }

        /// <summary>
        /// 등록된 런타임 저장 데이터를 가져온다.
        /// </summary>
        public static bool TryGetData<T>(out T data) where T : class
        {
            data = currentData as T;
            return data != null;
        }

        /// <summary>
        /// 메모리에 등록된 런타임 저장 데이터를 제거한다.
        /// </summary>
        public static void ClearData()
        {
            currentData = null;
            currentDataType = null;
        }
        #endregion // Data

        #region Save
        /// <summary>
        /// JSON 문자열을 선택된 슬롯의 로컬 저장 파일에 저장한다.
        /// </summary>
        private static bool SaveJson(string json, string slot = null, bool createBackup = true)
        {
            if (string.IsNullOrEmpty(json))
            {
                SWUtilsLog.LogError("[SWSaveDataManager] SaveJson failed. JSON is empty.");
                return false;
            }

            string normalizedSlot = ResolveSlotName(slot);
            string path = GetSavePath(normalizedSlot);
            string tempPath = path + TempExtension;
            string backupPath = path + BackupExtension;

            try
            {
                EnsureSaveDirectory();

                File.WriteAllText(tempPath, json);

                if (File.Exists(path))
                {
                    if (createBackup)
                        File.Copy(path, backupPath, true);

                    File.Delete(path);
                }

                File.Move(tempPath, path);
                SWUtilsLog.Log($"[SWSaveDataManager] Save complete. Slot: {normalizedSlot}");
                return true;
            }
            catch (Exception exception)
            {
                TryDeleteFile(tempPath);
                SWUtilsLog.LogError($"[SWSaveDataManager] SaveJson failed. Slot: {normalizedSlot}, Error: {exception.Message}");
                return false;
            }
        }

        /// <summary>
        /// 등록된 런타임 저장 데이터를 로컬 저장 파일에 저장한다.
        /// </summary>
        private static bool SaveRegisteredData(string slot = null, bool prettyPrint = false, bool createBackup = true)
        {
            if (currentData == null)
            {
                SWUtilsLog.LogError("[SWSaveDataManager] Save failed. Runtime data is not set. Call SetData first.");
                return false;
            }

            string normalizedSlot = ResolveSlotName(slot);
            SetSharedSlot(normalizedSlot);

            try
            {
                string json = JsonUtility.ToJson(currentData, prettyPrint);
                bool saved = SaveJson(json, normalizedSlot, createBackup);
                if (saved)
                    SWUtilsPlayerPrefs.Save();

                return saved;
            }
            catch (Exception exception)
            {
                SWUtilsLog.LogError($"[SWSaveDataManager] Save failed. Slot: {normalizedSlot}, Error: {exception.Message}");
                return false;
            }
        }

        /// <summary>
        /// 등록된 저장 데이터와 PlayerPrefs를 저장하고, 옵션에 따라 둘을 묶어 클라우드에 백업한다.
        /// </summary>
        public static bool SaveAll(Action<bool> onCloudComplete = null, string slot = null,
            bool prettyPrint = false, bool createBackup = true, bool backupToCloud = true)
        {
            if (currentData == null)
            {
                SWUtilsLog.LogError("[SWSaveDataManager] SaveAll failed. Runtime data is not set. Call SetData first.");
                onCloudComplete?.Invoke(false);
                return false;
            }

            string normalizedSlot = ResolveSlotName(slot);
            SetSharedSlot(normalizedSlot);

            bool saved = SaveRegisteredData(normalizedSlot, prettyPrint, createBackup);
            if (!saved)
            {
                onCloudComplete?.Invoke(false);
                return false;
            }

            if (backupToCloud)
                BackupToCloud(onCloudComplete, normalizedSlot);
            else
                onCloudComplete?.Invoke(true);

            return true;
        }

        /// <summary>
        /// 등록된 저장 데이터와 PlayerPrefs를 저장하고 클라우드 백업까지 비동기로 수행한다.
        /// </summary>
        public static Task<bool> SaveAllAsync(string slot = null, bool prettyPrint = false,
            bool createBackup = true, bool backupToCloud = true)
        {
            if (!backupToCloud)
                return Task.FromResult(SaveAll(onCloudComplete: null, slot: slot,
                    prettyPrint: prettyPrint, createBackup: createBackup, backupToCloud: false));

            var taskCompletionSource = new TaskCompletionSource<bool>();
            bool localSaved = SaveAll(success => taskCompletionSource.SetResult(success),
                slot, prettyPrint, createBackup, true);

            if (!localSaved)
                taskCompletionSource.TrySetResult(false);

            return taskCompletionSource.Task;
        }
        #endregion // Save

        #region Load
        /// <summary>
        /// 선택된 슬롯의 로컬 저장 파일에서 JSON 문자열을 읽는다.
        /// </summary>
        private static bool TryLoadJson(out string json, string slot = null)
        {
            json = string.Empty;
            string normalizedSlot = ResolveSlotName(slot);
            string path = GetSavePath(normalizedSlot);

            if (!File.Exists(path))
                return false;

            try
            {
                json = File.ReadAllText(path);
                return true;
            }
            catch (Exception exception)
            {
                SWUtilsLog.LogError($"[SWSaveDataManager] TryLoadJson failed. Slot: {normalizedSlot}, Error: {exception.Message}");
                return false;
            }
        }

        /// <summary>
        /// 클라우드에서 저장 데이터와 PlayerPrefs를 먼저 복원한 뒤 로컬 저장 데이터를 메모리로 로드한다.
        /// 역직렬화할 타입을 알 수 있도록 LoadAll 호출 전에 SetData를 먼저 호출해야 한다.
        /// </summary>
        public static void LoadAll(Action<bool> onComplete = null, string slot = null,
            bool cloudFirst = true, bool createBackup = true)
        {
            string normalizedSlot = ResolveSlotName(slot);
            SetSharedSlot(normalizedSlot);

            if (!cloudFirst)
            {
                bool localSuccess = LoadRegisteredData(normalizedSlot);
                onComplete?.Invoke(localSuccess);
                return;
            }

            RestoreFromCloud(_ =>
            {
                bool success = LoadRegisteredData(normalizedSlot);
                onComplete?.Invoke(success);
            }, normalizedSlot, createBackup);
        }

        /// <summary>
        /// 저장 데이터와 PlayerPrefs를 클라우드에서 복원한 뒤 로컬 데이터를 메모리로 로드하는 과정을 비동기로 수행한다.
        /// </summary>
        public static Task<bool> LoadAllAsync(string slot = null, bool cloudFirst = true, bool createBackup = true)
        {
            var taskCompletionSource = new TaskCompletionSource<bool>();
            LoadAll(success => taskCompletionSource.SetResult(success), slot, cloudFirst, createBackup);
            return taskCompletionSource.Task;
        }

        private static bool LoadRegisteredData(string slot = null)
        {
            if (currentDataType == null)
            {
                SWUtilsLog.LogError("[SWSaveDataManager] Load failed. Runtime data type is not set. Call SetData first.");
                return false;
            }

            string normalizedSlot = ResolveSlotName(slot);
            SetSharedSlot(normalizedSlot);
            string path = GetSavePath(normalizedSlot);

            if (!File.Exists(path))
            {
                SWUtilsLog.LogWarning($"[SWSaveDataManager] Load skipped. Save file does not exist. Slot: {normalizedSlot}");
                return false;
            }

            try
            {
                string json = File.ReadAllText(path);
                object loaded = JsonUtility.FromJson(json, currentDataType);
                if (loaded == null)
                {
                    SWUtilsLog.LogWarning($"[SWSaveDataManager] Load failed. JsonUtility returned null. Slot: {normalizedSlot}");
                    return false;
                }

                currentData = loaded;
                currentDataType = loaded.GetType();
                SWUtilsLog.Log($"[SWSaveDataManager] Load complete. Slot: {normalizedSlot}");
                return true;
            }
            catch (Exception exception)
            {
                SWUtilsLog.LogError($"[SWSaveDataManager] Load failed. Slot: {normalizedSlot}, Error: {exception.Message}");
                return false;
            }
        }
        #endregion // Load

        #region Management
        /// <summary>
        /// 선택된 슬롯의 로컬 저장 파일이 있는지 확인한다.
        /// </summary>
        public static bool HasSave(string slot = null)
        {
            return File.Exists(GetSavePath(slot));
        }

        /// <summary>
        /// 선택된 슬롯의 로컬 저장 파일과 백업 파일을 삭제한다.
        /// </summary>
        public static bool Delete(string slot = null)
        {
            string normalizedSlot = ResolveSlotName(slot);
            string path = GetSavePath(normalizedSlot);
            string backupPath = path + BackupExtension;
            bool deleted = false;

            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                    deleted = true;
                }

                if (File.Exists(backupPath))
                    File.Delete(backupPath);

                SWUtilsLog.Log($"[SWSaveDataManager] Delete complete. Slot: {normalizedSlot}");
                return deleted;
            }
            catch (Exception exception)
            {
                SWUtilsLog.LogError($"[SWSaveDataManager] Delete failed. Slot: {normalizedSlot}, Error: {exception.Message}");
                return false;
            }
        }

        /// <summary>
        /// 한 슬롯의 로컬 저장 파일을 다른 슬롯으로 복사한다.
        /// </summary>
        public static bool CopySlot(string fromSlot, string toSlot, bool overwrite = true)
        {
            string normalizedFromSlot = ResolveSlotName(fromSlot);
            string normalizedToSlot = ResolveSlotName(toSlot);
            string fromPath = GetSavePath(fromSlot);
            string toPath = GetSavePath(toSlot);

            if (!File.Exists(fromPath))
            {
                SWUtilsLog.LogWarning($"[SWSaveDataManager] CopySlot failed. Source does not exist. Slot: {normalizedFromSlot}");
                return false;
            }

            if (!overwrite && File.Exists(toPath))
            {
                SWUtilsLog.LogWarning($"[SWSaveDataManager] CopySlot failed. Target already exists. Slot: {normalizedToSlot}");
                return false;
            }

            try
            {
                EnsureSaveDirectory();
                File.Copy(fromPath, toPath, overwrite);
                return true;
            }
            catch (Exception exception)
            {
                SWUtilsLog.LogError($"[SWSaveDataManager] CopySlot failed. Error: {exception.Message}");
                return false;
            }
        }

        /// <summary>
        /// 저장 폴더 안의 모든 슬롯 저장 파일 정보를 반환한다.
        /// </summary>
        public static IReadOnlyList<SaveFileInfo> ListSaves()
        {
            var saves = new List<SaveFileInfo>();
            if (!Directory.Exists(SaveDirectoryPath))
                return saves;

            string[] files = Directory.GetFiles(SaveDirectoryPath, "*" + SaveExtension);
            for (int index = 0; index < files.Length; index++)
            {
                FileInfo fileInfo = new FileInfo(files[index]);
                string slot = Path.GetFileNameWithoutExtension(fileInfo.Name);
                saves.Add(new SaveFileInfo(
                    slot,
                    fileInfo.Name,
                    fileInfo.FullName,
                    fileInfo.Exists,
                    fileInfo.Exists ? fileInfo.Length : 0,
                    fileInfo.Exists ? fileInfo.LastWriteTimeUtc : DateTime.MinValue));
            }

            return saves;
        }

        /// <summary>
        /// 선택된 슬롯 저장 파일의 정보를 반환한다.
        /// </summary>
        public static SaveFileInfo GetSaveInfo(string slot = null)
        {
            string normalizedSlot = ResolveSlotName(slot);
            string path = GetSavePath(normalizedSlot);
            FileInfo fileInfo = new FileInfo(path);

            return new SaveFileInfo(
                normalizedSlot,
                fileInfo.Name,
                fileInfo.FullName,
                fileInfo.Exists,
                fileInfo.Exists ? fileInfo.Length : 0,
                fileInfo.Exists ? fileInfo.LastWriteTimeUtc : DateTime.MinValue);
        }
        #endregion // Management

        #region Cloud
        /// <summary>
        /// 선택된 로컬 슬롯의 저장 데이터와 PlayerPrefs를 SWUtilsCloud에 백업한다.
        /// </summary>
        public static void BackupToCloud(Action<bool> onComplete = null, string slot = null)
        {
            string normalizedSlot = ResolveSlotName(slot);
            SetSharedSlot(normalizedSlot);

            if (!TryLoadJson(out string saveDataJson, normalizedSlot))
            {
                SWUtilsLog.LogWarning($"[SWSaveDataManager] BackupToCloud failed. Local save does not exist. Slot: {normalizedSlot}");
                onComplete?.Invoke(false);
                return;
            }

            SWUtilsPlayerPrefs.Save();

            var backupData = new CloudBackupData
            {
                slot = normalizedSlot,
                saveDataJson = saveDataJson,
                playerPrefsJson = SWUtilsPlayerPrefs.ExportToJson(IsCloudBackupKey),
                savedAtUtc = DateTime.UtcNow.ToString("o")
            };

            string cloudJson = JsonUtility.ToJson(backupData);
            SWUtilsCloud.Save(cloudJson, onComplete, normalizedSlot);
        }

        /// <summary>
        /// SWUtilsCloud에서 저장 데이터를 내려받아 선택된 로컬 슬롯과 PlayerPrefs에 복원한다.
        /// </summary>
        public static void RestoreFromCloud(Action<bool> onComplete = null, string slot = null, bool createBackup = true)
        {
            string normalizedSlot = ResolveSlotName(slot);
            SetSharedSlot(normalizedSlot);

            SWUtilsCloud.Load((success, json) =>
            {
                if (!success || string.IsNullOrEmpty(json))
                {
                    onComplete?.Invoke(false);
                    return;
                }

                bool restored = RestoreCloudJson(json, normalizedSlot, createBackup);
                onComplete?.Invoke(restored);
            }, normalizedSlot);
        }

        /// <summary>
        /// 선택된 슬롯의 클라우드 저장 데이터를 삭제한다.
        /// </summary>
        public static void DeleteCloud(Action<bool> onComplete = null, string slot = null)
        {
            SWUtilsCloud.Delete(onComplete, ResolveSlotName(slot));
        }

        /// <summary>
        /// 선택된 로컬 슬롯의 저장 데이터와 PlayerPrefs를 SWUtilsCloud에 비동기로 백업한다.
        /// </summary>
        public static Task<bool> BackupToCloudAsync(string slot = null)
        {
            var taskCompletionSource = new TaskCompletionSource<bool>();
            BackupToCloud(success => taskCompletionSource.SetResult(success), slot);
            return taskCompletionSource.Task;
        }

        /// <summary>
        /// SWUtilsCloud에서 저장 데이터를 내려받아 선택된 로컬 슬롯과 PlayerPrefs에 비동기로 복원한다.
        /// </summary>
        public static Task<bool> RestoreFromCloudAsync(string slot = null, bool createBackup = true)
        {
            var taskCompletionSource = new TaskCompletionSource<bool>();
            RestoreFromCloud(success => taskCompletionSource.SetResult(success), slot, createBackup);
            return taskCompletionSource.Task;
        }

        /// <summary>
        /// 선택된 슬롯의 클라우드 저장 데이터를 비동기로 삭제한다.
        /// </summary>
        public static Task<bool> DeleteCloudAsync(string slot = null)
        {
            return SWUtilsCloud.DeleteAsync(ResolveSlotName(slot));
        }

        private static bool RestoreCloudJson(string json, string slot, bool createBackup)
        {
            try
            {
                CloudBackupData backupData = JsonUtility.FromJson<CloudBackupData>(json);
                if (backupData != null && (!string.IsNullOrEmpty(backupData.saveDataJson)
                    || !string.IsNullOrEmpty(backupData.playerPrefsJson)))
                {
                    bool saveDataRestored = string.IsNullOrEmpty(backupData.saveDataJson)
                        || SaveJson(backupData.saveDataJson, slot, createBackup);

                    bool prefsRestored = string.IsNullOrEmpty(backupData.playerPrefsJson)
                        || SWUtilsPlayerPrefs.ImportFromJson(backupData.playerPrefsJson);

                    if (prefsRestored)
                        SWUtilsPlayerPrefs.Save();

                    return saveDataRestored && prefsRestored;
                }
            }
            catch (Exception exception)
            {
                SWUtilsLog.LogWarning($"[SWSaveDataManager] Cloud backup bundle parse failed. Fallback to raw save json. Error: {exception.Message}");
            }

            return SaveJson(json, slot, createBackup);
        }
        #endregion // Cloud

        #region Path
        /// <summary>
        /// 선택된 슬롯의 로컬 저장 파일 전체 경로를 반환한다.
        /// </summary>
        public static string GetSavePath(string slot = null)
        {
            return Path.Combine(SaveDirectoryPath, ResolveSlotName(slot) + SaveExtension);
        }

        private static void EnsureSaveDirectory()
        {
            if (!Directory.Exists(SaveDirectoryPath))
                Directory.CreateDirectory(SaveDirectoryPath);
        }

        /// <summary>
        /// 슬롯 이름을 로컬 파일명과 클라우드 저장 이름으로 사용할 수 있게 정리한다.
        /// </summary>
        public static string NormalizeSlotName(string slot)
        {
            if (string.IsNullOrWhiteSpace(slot))
                return DefaultSlotName;

            string normalized = slot.Trim();
            char[] invalidChars = Path.GetInvalidFileNameChars();

            for (int index = 0; index < invalidChars.Length; index++)
                normalized = normalized.Replace(invalidChars[index], '_');

            return string.IsNullOrWhiteSpace(normalized) ? DefaultSlotName : normalized;
        }

        private static string ResolveSlotName(string slot)
        {
            return string.IsNullOrWhiteSpace(slot) ? currentSlot : NormalizeSlotName(slot);
        }

        private static void SetSharedSlot(string slot)
        {
            string normalizedSlot = ResolveSlotName(slot);
            SetSlot(normalizedSlot);
        }

        private static bool IsCloudBackupKey(string key)
        {
            return !key.StartsWith(CloudFallbackKeyPrefix, StringComparison.Ordinal);
        }

        private static void TryDeleteFile(string path)
        {
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch (Exception exception)
            {
                SWUtilsLog.LogWarning($"[SWSaveDataManager] Failed to delete temp file. Error: {exception.Message}");
            }
        }
        #endregion // Path
    }
}
