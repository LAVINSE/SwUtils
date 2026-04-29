using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWUtils
{
    /// <summary>
    /// BGM/SFX AudioClip을 키로 등록해서 SWAudioManager에서 찾아 쓰는 사운드 라이브러리.
    /// </summary>
    [CreateAssetMenu(fileName = "SWAudioLibrary", menuName = "SWUtils/Audio Library")]
    public class SWAudioLibrary : ScriptableObject
    {
        #region 타입
        /// <summary>
        /// 사운드 키와 AudioClip을 연결하는 라이브러리 항목입니다.
        /// </summary>
        [Serializable]
        public class AudioEntry
        {
            [SerializeField] private string key;
            [SerializeField] private AudioClip clip;

            /// <summary>사운드를 찾을 때 사용할 키입니다.</summary>
            public string Key => key;
            /// <summary>키에 연결된 AudioClip입니다.</summary>
            public AudioClip Clip => clip;
        }
        #endregion // 타입

        #region 필드
        [Header("=====> 배경음 <=====")]
        [SerializeField] private List<AudioEntry> musicEntries = new();

        [Header("=====> 효과음 <=====")]
        [SerializeField] private List<AudioEntry> sfxEntries = new();

        private Dictionary<string, AudioClip> musicMap;
        private Dictionary<string, AudioClip> sfxMap;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>BGM 항목 목록입니다.</summary>
        public IReadOnlyList<AudioEntry> MusicEntries => musicEntries;
        /// <summary>SFX 항목 목록입니다.</summary>
        public IReadOnlyList<AudioEntry> SfxEntries => sfxEntries;
        #endregion // 프로퍼티

        #region 조회
        /// <summary>
        /// BGM 키에 해당하는 AudioClip을 찾습니다.
        /// </summary>
        /// <param name="key">찾을 BGM 키입니다.</param>
        /// <param name="clip">찾은 AudioClip입니다.</param>
        /// <returns>유효한 AudioClip을 찾았으면 true입니다.</returns>
        public bool TryGetMusicClip(string key, out AudioClip clip)
        {
            EnsureMaps();
            return musicMap.TryGetValue(key, out clip) && clip != null;
        }

        /// <summary>
        /// SFX 키에 해당하는 AudioClip을 찾습니다.
        /// </summary>
        /// <param name="key">찾을 SFX 키입니다.</param>
        /// <param name="clip">찾은 AudioClip입니다.</param>
        /// <returns>유효한 AudioClip을 찾았으면 true입니다.</returns>
        public bool TryGetSfxClip(string key, out AudioClip clip)
        {
            EnsureMaps();
            return sfxMap.TryGetValue(key, out clip) && clip != null;
        }
        #endregion // 조회

        #region 내부
        private void OnValidate()
        {
            musicMap = null;
            sfxMap = null;
        }

        private void EnsureMaps()
        {
            musicMap ??= BuildMap(musicEntries, "Music");
            sfxMap ??= BuildMap(sfxEntries, "SFX");
        }

        private Dictionary<string, AudioClip> BuildMap(IReadOnlyList<AudioEntry> entries, string category)
        {
            Dictionary<string, AudioClip> map = new();

            if (entries == null) return map;

            foreach (AudioEntry entry in entries)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.Key))
                    continue;

                if (map.ContainsKey(entry.Key))
                {
                    SWUtilsLog.LogWarning($"[SWAudioLibrary] Duplicate {category} key ignored: {entry.Key}");
                    continue;
                }

                map.Add(entry.Key, entry.Clip);
            }

            return map;
        }
        #endregion // 내부
    }
}
