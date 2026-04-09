using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace SWUtils
{
    /// <summary>
    /// AES 암호화를 적용한 PlayerPrefs
    /// 키와 값 모두 암호화하여 저장한다
    /// 전체 데이터를 JSON으로 export/import 할 수 있어 클라우드 동기화에 사용 가능
    /// </summary>
    public static class SWUtilsPlayerPrefs
    {
        [Serializable]
        private class PrefsData
        {
            public List<PrefsEntry> entries = new List<PrefsEntry>();
        }

        [Serializable]
        private class PrefsEntry
        {
            public string key;
            public string value;
        }

        #region 필드
        /// <summary>암호화에 사용되는 고정 솔트. 프로젝트별로 변경 권장.</summary>
        private const string Salt = "SwUtilsPrefs_2026_SaltKey_ChangeMe";
        /// <summary>관리 중인 키 목록을 저장하는 PlayerPrefs 키.</summary>
        private static string KeyIndexName => $"SwUtilsPrefs_KeyIndex_{currentSlot}";
        /// <summary>암호화된 키 prefix (일반 PlayerPrefs와 구분).</summary>
        private const string EncPrefix = "SwEnc_";
        /// <summary>관리 중인 키 목록 캐시.</summary>
        private static HashSet<string> keyIndexCache;

        /// <summary>현재 활성 슬롯 이름. 모든 키 앞에 자동으로 붙는다.</summary>
        private static string currentSlot = "default";

        /// <summary>현재 활성 슬롯 이름.</summary>
        public static string CurrentSlot => currentSlot;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>관리 중인 키 목록.</summary>
        private static HashSet<string> KeyIndex_
        {
            get
            {
                if (keyIndexCache == null)
                {
                    keyIndexCache = new HashSet<string>();
                    string raw = PlayerPrefs.GetString(KeyIndexName, string.Empty);
                    if (!string.IsNullOrEmpty(raw))
                    {
                        foreach (var k in raw.Split('|'))
                        {
                            if (!string.IsNullOrEmpty(k)) keyIndexCache.Add(k);
                        }
                    }
                }
                return keyIndexCache;
            }
        }
        #endregion // 프로퍼티

        #region 슬롯 관리
        /// <summary>
        /// 활성 슬롯을 변경한다. 변경 후 모든 Get/Set은 해당 슬롯에 적용된다.
        /// </summary>
        /// <param name="slotName">슬롯 이름</param>
        public static void SetSlot(string slotName)
        {
            if (string.IsNullOrEmpty(slotName)) slotName = "default";
            currentSlot = slotName;
            keyIndexCache = null; // 인덱스 캐시 리셋
        }
        #endregion // 슬롯 관리

        #region 암호화 / 복호화
        /// <summary>
        /// AES 키와 IV를 생성한다. 솔트 기반으로 항상 동일한 값을 반환.
        /// </summary>
        private static void GetKeyIV(out byte[] key, out byte[] iv)
        {
            using (var derive = new Rfc2898DeriveBytes(Salt, Encoding.UTF8.GetBytes("SwUtilsIVSalt"), 1000))
            {
                key = derive.GetBytes(16);
                iv = derive.GetBytes(16);
            }
        }

        /// <summary>
        /// 문자열을 AES 암호화하여 Base64 문자열로 반환한다.
        /// </summary>
        /// <param name="plain">암호화할 평문</param>
        /// <returns>Base64 인코딩된 암호문</returns>
        private static string Encrypt(string plain)
        {
            if (string.IsNullOrEmpty(plain)) return string.Empty;

            try
            {
                GetKeyIV(out byte[] key, out byte[] iv);
                using (var aes = Aes.Create())
                {
                    aes.Key = key;
                    aes.IV = iv;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;

                    using (var encryptor = aes.CreateEncryptor())
                    {
                        byte[] bytes = Encoding.UTF8.GetBytes(plain);
                        byte[] encrypted = encryptor.TransformFinalBlock(bytes, 0, bytes.Length);
                        return Convert.ToBase64String(encrypted);
                    }
                }
            }
            catch (Exception e)
            {
                SWUtilsLog.LogError($"[SWUtilsPlayerPrefs] Encrypt failed: {e.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Base64 암호문을 복호화하여 평문 문자열로 반환한다.
        /// </summary>
        /// <param name="cipher">Base64 인코딩된 암호문</param>
        /// <returns>복호화된 평문</returns>
        private static string Decrypt(string cipher)
        {
            if (string.IsNullOrEmpty(cipher)) return string.Empty;

            try
            {
                GetKeyIV(out byte[] key, out byte[] iv);
                using (var aes = Aes.Create())
                {
                    aes.Key = key;
                    aes.IV = iv;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;

                    using (var decryptor = aes.CreateDecryptor())
                    {
                        byte[] bytes = Convert.FromBase64String(cipher);
                        byte[] decrypted = decryptor.TransformFinalBlock(bytes, 0, bytes.Length);
                        return Encoding.UTF8.GetString(decrypted);
                    }
                }
            }
            catch (Exception e)
            {
                SWUtilsLog.LogError($"[SWUtilsPlayerPrefs] Decrypt failed: {e.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// 키를 해시하여 저장용 키로 변환한다.
        /// </summary>
        private static string HashKey(string key)
        {
            using (var sha = SHA256.Create())
            {
                // 슬롯 이름을 해시에 포함 → 슬롯별로 다른 키 생성
                byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(key + Salt + currentSlot));
                return EncPrefix + Convert.ToBase64String(bytes)
                    .Replace("/", "_").Replace("+", "-").Substring(0, 22);
            }
        }
        #endregion // 암호화 / 복호화

        #region 키 인덱스 관리
        /// <summary>
        /// 키 인덱스에 키를 추가하고 저장한다.
        /// </summary>
        private static void AddToIndex(string key)
        {
            if (KeyIndex_.Add(key))
            {
                SaveIndex();
            }
        }

        /// <summary>
        /// 키 인덱스에서 키를 제거하고 저장한다.
        /// </summary>
        private static void RemoveFromIndex(string key)
        {
            if (KeyIndex_.Remove(key))
            {
                SaveIndex();
            }
        }

        /// <summary>
        /// 키 인덱스를 PlayerPrefs에 저장한다.
        /// </summary>
        private static void SaveIndex()
        {
            string joined = string.Join("|", KeyIndex_);
            PlayerPrefs.SetString(KeyIndexName, joined);
        }
        #endregion // 키 인덱스 관리

        #region Set
        /// <summary>
        /// 문자열 값을 암호화하여 저장한다.
        /// </summary>
        /// <param name="key">저장 키</param>
        /// <param name="value">저장할 값</param>
        public static void SetString(string key, string value)
        {
            string encKey = HashKey(key);
            string encValue = Encrypt(value ?? string.Empty);
            PlayerPrefs.SetString(encKey, encValue);
            AddToIndex(key);
        }

        /// <summary>
        /// 정수 값을 암호화하여 저장한다.
        /// </summary>
        /// <param name="key">저장 키</param>
        /// <param name="value">저장할 값</param>
        public static void SetInt(string key, int value)
        {
            SetString(key, value.ToString());
        }

        /// <summary>
        /// 실수 값을 암호화하여 저장한다.
        /// </summary>
        /// <param name="key">저장 키</param>
        /// <param name="value">저장할 값</param>
        public static void SetFloat(string key, float value)
        {
            SetString(key, value.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// bool 값을 암호화하여 저장한다.
        /// </summary>
        /// <param name="key">저장 키</param>
        /// <param name="value">저장할 값</param>
        public static void SetBool(string key, bool value)
        {
            SetString(key, value ? "1" : "0");
        }
        #endregion // Set

        #region Get
        /// <summary>
        /// 암호화된 문자열 값을 복호화하여 반환한다.
        /// </summary>
        /// <param name="key">저장 키</param>
        /// <param name="defaultValue">키가 없을 때 반환할 기본값</param>
        /// <returns>복호화된 값</returns>
        public static string GetString(string key, string defaultValue = "")
        {
            string encKey = HashKey(key);
            if (!PlayerPrefs.HasKey(encKey)) return defaultValue;
            string encValue = PlayerPrefs.GetString(encKey, string.Empty);
            string decrypted = Decrypt(encValue);
            return string.IsNullOrEmpty(decrypted) ? defaultValue : decrypted;
        }

        /// <summary>
        /// 암호화된 정수 값을 복호화하여 반환한다.
        /// </summary>
        public static int GetInt(string key, int defaultValue = 0)
        {
            string s = GetString(key, null);
            if (string.IsNullOrEmpty(s)) return defaultValue;
            return int.TryParse(s, out int result) ? result : defaultValue;
        }

        /// <summary>
        /// 암호화된 실수 값을 복호화하여 반환한다.
        /// </summary>
        public static float GetFloat(string key, float defaultValue = 0f)
        {
            string s = GetString(key, null);
            if (string.IsNullOrEmpty(s)) return defaultValue;
            return float.TryParse(s, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out float result) ? result : defaultValue;
        }

        /// <summary>
        /// 암호화된 bool 값을 복호화하여 반환한다.
        /// </summary>
        public static bool GetBool(string key, bool defaultValue = false)
        {
            string s = GetString(key, null);
            if (string.IsNullOrEmpty(s)) return defaultValue;
            return s == "1";
        }
        #endregion // Get

        #region 관리
        /// <summary>
        /// 키 존재 여부를 확인한다.
        /// </summary>
        public static bool HasKey(string key)
        {
            return PlayerPrefs.HasKey(HashKey(key));
        }

        /// <summary>
        /// 특정 키를 삭제한다.
        /// </summary>
        public static void DeleteKey(string key)
        {
            PlayerPrefs.DeleteKey(HashKey(key));
            RemoveFromIndex(key);
        }

        /// <summary>
        /// 암호화된 모든 데이터를 삭제한다.
        /// 일반 PlayerPrefs는 영향받지 않는다.
        /// </summary>
        public static void DeleteAll()
        {
            foreach (var key in new List<string>(KeyIndex_))
            {
                PlayerPrefs.DeleteKey(HashKey(key));
            }
            keyIndexCache?.Clear();
            PlayerPrefs.DeleteKey(KeyIndexName);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// PlayerPrefs를 디스크에 저장한다.
        /// </summary>
        public static void Save()
        {
            PlayerPrefs.Save();
        }
        #endregion // 관리

        #region JSON Export / Import
        /// <summary>
        /// 관리 중인 모든 암호화 데이터를 JSON 문자열로 반환한다.
        /// 클라우드 저장용으로 사용한다.
        /// </summary>
        /// <returns>JSON 문자열 (복호화된 평문 상태)</returns>
        public static string ExportToJson()
        {
            var data = new PrefsData();
            foreach (var key in KeyIndex_)
            {
                string value = GetString(key, null);
                if (value != null)
                {
                    data.entries.Add(new PrefsEntry { key = key, value = value });
                }
            }
            return JsonUtility.ToJson(data);
        }

        /// <summary>
        /// JSON 문자열로부터 데이터를 복원한다.
        /// 기존 데이터는 모두 삭제된다.
        /// </summary>
        /// <param name="json">ExportToJson으로 생성된 JSON</param>
        /// <returns>복원 성공 여부</returns>
        public static bool ImportFromJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return false;

            try
            {
                var data = JsonUtility.FromJson<PrefsData>(json);
                if (data == null || data.entries == null) return false;

                DeleteAll();

                foreach (var entry in data.entries)
                {
                    SetString(entry.key, entry.value);
                }

                Save();
                return true;
            }
            catch (Exception e)
            {
                SWUtilsLog.LogError($"[SWUtilsPlayerPrefs] ImportFromJson failed: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// JSON 문자열을 기존 데이터에 병합한다. 동일 키는 덮어쓴다.
        /// </summary>
        /// <param name="json">ExportToJson으로 생성된 JSON</param>
        /// <returns>병합 성공 여부</returns>
        public static bool MergeFromJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return false;

            try
            {
                var data = JsonUtility.FromJson<PrefsData>(json);
                if (data == null || data.entries == null) return false;

                foreach (var entry in data.entries)
                {
                    SetString(entry.key, entry.value);
                }

                Save();
                return true;
            }
            catch (Exception e)
            {
                SWUtilsLog.LogError($"[SWUtilsPlayerPrefs] MergeFromJson failed: {e.Message}");
                return false;
            }
        }
        #endregion // JSON Export / Import
    }
}