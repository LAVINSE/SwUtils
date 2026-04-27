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
        [Serializable]
        public class AudioEntry
        {
            [SerializeField] private string key;
            [SerializeField] private AudioClip clip;

            public string Key => key;
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
        public IReadOnlyList<AudioEntry> MusicEntries => musicEntries;
        public IReadOnlyList<AudioEntry> SfxEntries => sfxEntries;
        #endregion // 프로퍼티

        #region 조회
        public bool TryGetMusicClip(string key, out AudioClip clip)
        {
            EnsureMaps();
            return musicMap.TryGetValue(key, out clip) && clip != null;
        }

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
