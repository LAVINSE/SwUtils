using System.Collections;
using System.Collections.Generic;
using SWTools;
using UnityEngine;

namespace SWUtils
{
    /// <summary>
    /// BGM/SFX 재생, 볼륨 조절, 페이드, AudioSource 재사용을 처리하는 오디오 매니저.
    /// SWAudioManager.Instance로 전역 접근하거나 씬에 직접 배치해서 사용한다.
    /// </summary>
    public class SWAudioManager : SWSingleton<SWAudioManager>
    {
        #region 필드
        [Header("=====> 라이브러리 <=====")]
        [SerializeField] private SWAudioLibrary audioLibrary;

        [Header("=====> 음악 <=====")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField,] private bool playMusicOnStart;
        [SerializeField, SWCondition("playMusicOnStart", true)] private string startMusicKey;

        [Header("=====> 효과음 <=====")]
        [SerializeField] private AudioSource sfxSourcePrefab;
        [SerializeField] private int initialSfxSources = 8;
        [SerializeField] private int maxSfxSources = 32;

        [Header("=====> 설정 <=====")]
        [Range(0f, 1f)]
        [SerializeField] private float masterVolume = 1f;
        [Range(0f, 1f)]
        [SerializeField] private float musicVolume = 1f;
        [Range(0f, 1f)]
        [SerializeField] private float sfxVolume = 1f;

        private readonly List<AudioSource> sfxSources = new();
        private Coroutine musicFadeRoutine;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>키 기반 재생에 사용하는 사운드 라이브러리.</summary>
        public SWAudioLibrary AudioLibrary => audioLibrary;
        /// <summary>음악 재생에 사용하는 AudioSource.</summary>
        public AudioSource MusicSource => musicSource;
        /// <summary>전체 볼륨.</summary>
        public float MasterVolume => masterVolume;
        /// <summary>음악 볼륨.</summary>
        public float MusicVolume => musicVolume;
        /// <summary>효과음 볼륨.</summary>
        public float SfxVolume => sfxVolume;
        #endregion // 프로퍼티

        #region 초기화
        public override void Awake()
        {
            base.Awake();
            if (Instance != this) return;

            EnsureMusicSource();
            PrewarmSfxSources(initialSfxSources);
            ApplyVolumes();

            SWUtilsLog.Log($"[SWAudioManager] Initialized. SFX Sources: {sfxSources.Count}");
        }

        private void Start()
        {
            if (!playMusicOnStart) return;
            PlayMusic(startMusicKey);
        }

        private void OnDestroy()
        {
            StopAllCoroutines();
            sfxSources.Clear();
            SWUtilsLog.Log("[SWAudioManager] Destroyed.");
        }
        #endregion // 초기화

        #region 라이브러리
        /// <summary>
        /// 런타임에 사용할 사운드 라이브러리를 지정한다.
        /// </summary>
        /// <param name="library">BGM/SFX 키가 등록된 라이브러리.</param>
        public void SetAudioLibrary(SWAudioLibrary library)
        {
            audioLibrary = library;
        }
        #endregion // 라이브러리

        #region 음악
        /// <summary>
        /// 라이브러리에 등록된 BGM을 키로 찾아 재생한다.
        /// </summary>
        /// <param name="key">라이브러리에 등록된 BGM 키.</param>
        /// <param name="loop">반복 재생 여부.</param>
        /// <param name="fadeDuration">페이드 전환 시간.</param>
        public void PlayMusic(string key, bool loop = true, float fadeDuration = 0f)
        {
            if (!TryGetMusicClip(key, out AudioClip clip))
                return;

            PlayMusicClip(clip, loop, fadeDuration);
        }

        /// <summary>
        /// 현재 BGM을 정지한다.
        /// </summary>
        /// <param name="fadeDuration">페이드아웃 시간.</param>
        public void StopMusic(float fadeDuration = 0f)
        {
            EnsureMusicSource();

            if (musicFadeRoutine != null)
                StopCoroutine(musicFadeRoutine);

            if (fadeDuration > 0f)
            {
                musicFadeRoutine = StartCoroutine(FadeOutMusicRoutine(fadeDuration));
                SWUtilsLog.Log("[SWAudioManager] Fade out music.");
                return;
            }

            musicSource.Stop();
            SWUtilsLog.Log("[SWAudioManager] Stop music.");
        }

        public void PauseMusic()
        {
            EnsureMusicSource();
            musicSource.Pause();
            SWUtilsLog.Log("[SWAudioManager] Pause music.");
        }

        public void ResumeMusic()
        {
            EnsureMusicSource();
            musicSource.UnPause();
            SWUtilsLog.Log("[SWAudioManager] Resume music.");
        }
        #endregion // 음악

        #region 효과음
        /// <summary>
        /// 라이브러리에 등록된 2D 효과음을 키로 찾아 재생한다.
        /// </summary>
        /// <param name="key">라이브러리에 등록된 SFX 키.</param>
        /// <param name="volumeScale">효과음 볼륨 배율.</param>
        /// <param name="pitch">재생 피치.</param>
        /// <returns>재생에 사용된 AudioSource.</returns>
        public AudioSource PlaySfx(string key, float volumeScale = 1f, float pitch = 1f)
        {
            if (!TryGetSfxClip(key, out AudioClip clip))
                return null;

            return PlaySfxClip(clip, volumeScale, pitch);
        }

        /// <summary>
        /// 라이브러리에 등록된 효과음을 키로 찾아 지정한 월드 위치에서 3D 재생한다.
        /// </summary>
        /// <param name="key">라이브러리에 등록된 SFX 키.</param>
        /// <param name="position">재생 위치.</param>
        /// <param name="volumeScale">효과음 볼륨 배율.</param>
        /// <param name="pitch">재생 피치.</param>
        /// <returns>재생에 사용된 AudioSource.</returns>
        public AudioSource PlaySfxAtPoint(string key, Vector3 position, float volumeScale = 1f, float pitch = 1f)
        {
            if (!TryGetSfxClip(key, out AudioClip clip))
                return null;

            AudioSource source = PlaySfxClip(clip, volumeScale, pitch);
            if (source == null) return null;

            source.transform.position = position;
            source.spatialBlend = 1f;
            return source;
        }

        public void StopAllSfx()
        {
            foreach (AudioSource source in sfxSources)
            {
                if (source != null)
                    source.Stop();
            }

            SWUtilsLog.Log("[SWAudioManager] Stop all SFX.");
        }
        #endregion // 효과음

        #region 볼륨
        /// <summary>
        /// 전체 볼륨을 설정한다.
        /// </summary>
        /// <param name="volume">0~1 사이의 볼륨 값.</param>
        public void SetMasterVolume(float volume)
        {
            masterVolume = Mathf.Clamp01(volume);
            ApplyVolumes();
            SWUtilsLog.Log($"[SWAudioManager] Master volume: {masterVolume}");
        }

        /// <summary>
        /// 음악 볼륨을 설정한다.
        /// </summary>
        /// <param name="volume">0~1 사이의 볼륨 값.</param>
        public void SetMusicVolume(float volume)
        {
            musicVolume = Mathf.Clamp01(volume);
            ApplyVolumes();
            SWUtilsLog.Log($"[SWAudioManager] Music volume: {musicVolume}");
        }

        /// <summary>
        /// 효과음 볼륨을 설정한다.
        /// </summary>
        /// <param name="volume">0~1 사이의 볼륨 값.</param>
        public void SetSfxVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
            ApplyVolumes();
            SWUtilsLog.Log($"[SWAudioManager] SFX volume: {sfxVolume}");
        }

        /// <summary>
        /// 현재 볼륨 설정을 PlayerPrefs에 저장한다.
        /// </summary>
        /// <param name="keyPrefix">저장 키 prefix.</param>
        public void SaveVolumes(string keyPrefix = "SWAudioManager")
        {
            PlayerPrefs.SetFloat($"{keyPrefix}_Master", masterVolume);
            PlayerPrefs.SetFloat($"{keyPrefix}_Music", musicVolume);
            PlayerPrefs.SetFloat($"{keyPrefix}_Sfx", sfxVolume);
            PlayerPrefs.Save();

            SWUtilsLog.Log($"[SWAudioManager] Save volumes. Key: {keyPrefix}");
        }

        /// <summary>
        /// PlayerPrefs에 저장된 볼륨 설정을 불러온다.
        /// </summary>
        /// <param name="keyPrefix">저장 키 prefix.</param>
        public void LoadVolumes(string keyPrefix = "SWAudioManager")
        {
            masterVolume = PlayerPrefs.GetFloat($"{keyPrefix}_Master", masterVolume);
            musicVolume = PlayerPrefs.GetFloat($"{keyPrefix}_Music", musicVolume);
            sfxVolume = PlayerPrefs.GetFloat($"{keyPrefix}_Sfx", sfxVolume);
            ApplyVolumes();

            SWUtilsLog.Log($"[SWAudioManager] Load volumes. Key: {keyPrefix}");
        }
        #endregion // 볼륨

        #region 내부
        private bool TryGetMusicClip(string key, out AudioClip clip)
        {
            clip = null;

            if (audioLibrary == null)
            {
                SWUtilsLog.LogWarning("[SWAudioManager] PlayMusic failed. AudioLibrary is null.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(key))
            {
                SWUtilsLog.LogWarning("[SWAudioManager] PlayMusic failed. Key is empty.");
                return false;
            }

            if (!audioLibrary.TryGetMusicClip(key, out clip))
            {
                SWUtilsLog.LogWarning($"[SWAudioManager] PlayMusic failed. Music key not found: {key}");
                return false;
            }

            return true;
        }

        private bool TryGetSfxClip(string key, out AudioClip clip)
        {
            clip = null;

            if (audioLibrary == null)
            {
                SWUtilsLog.LogWarning("[SWAudioManager] PlaySfx failed. AudioLibrary is null.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(key))
            {
                SWUtilsLog.LogWarning("[SWAudioManager] PlaySfx failed. Key is empty.");
                return false;
            }

            if (!audioLibrary.TryGetSfxClip(key, out clip))
            {
                SWUtilsLog.LogWarning($"[SWAudioManager] PlaySfx failed. SFX key not found: {key}");
                return false;
            }

            return true;
        }

        private void PlayMusicClip(AudioClip clip, bool loop, float fadeDuration)
        {
            if (clip == null)
            {
                SWUtilsLog.LogWarning("[SWAudioManager] PlayMusic failed. Clip is null.");
                return;
            }

            EnsureMusicSource();

            if (musicFadeRoutine != null)
                StopCoroutine(musicFadeRoutine);

            if (fadeDuration > 0f && musicSource.isPlaying)
            {
                musicFadeRoutine = StartCoroutine(FadeToNewMusicRoutine(clip, loop, fadeDuration));
                SWUtilsLog.Log($"[SWAudioManager] Fade music to: {clip.name}");
                return;
            }

            musicSource.clip = clip;
            musicSource.loop = loop;
            musicSource.volume = masterVolume * musicVolume;
            musicSource.Play();

            SWUtilsLog.Log($"[SWAudioManager] Play music: {clip.name}");
        }

        private AudioSource PlaySfxClip(AudioClip clip, float volumeScale, float pitch)
        {
            if (clip == null)
            {
                SWUtilsLog.LogWarning("[SWAudioManager] PlaySfx failed. Clip is null.");
                return null;
            }

            AudioSource source = GetAvailableSfxSource();
            if (source == null)
            {
                SWUtilsLog.LogWarning("[SWAudioManager] PlaySfx failed. No available SFX source.");
                return null;
            }

            source.clip = clip;
            source.loop = false;
            source.pitch = pitch;
            source.volume = masterVolume * sfxVolume * Mathf.Clamp01(volumeScale);
            source.spatialBlend = 0f;
            source.transform.SetParent(transform, false);
            source.Play();

            SWUtilsLog.Log($"[SWAudioManager] Play SFX: {clip.name}");
            return source;
        }

        /// <summary>
        /// 음악용 AudioSource가 없으면 자동으로 생성한다.
        /// </summary>
        private void EnsureMusicSource()
        {
            if (musicSource != null) return;

            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.playOnAwake = false;
            SWUtilsLog.Log("[SWAudioManager] MusicSource auto-created.");
        }

        /// <summary>
        /// 효과음 AudioSource를 미리 생성한다.
        /// </summary>
        /// <param name="count">생성할 AudioSource 개수.</param>
        private void PrewarmSfxSources(int count)
        {
            int safeCount = Mathf.Max(0, count);
            for (int index = sfxSources.Count; index < safeCount; index++)
                CreateSfxSource();
        }

        /// <summary>
        /// 사용 가능한 효과음 AudioSource를 반환한다.
        /// </summary>
        /// <returns>사용 가능한 AudioSource.</returns>
        private AudioSource GetAvailableSfxSource()
        {
            foreach (AudioSource source in sfxSources)
            {
                if (source != null && !source.isPlaying)
                    return source;
            }

            if (sfxSources.Count >= maxSfxSources)
                return null;

            return CreateSfxSource();
        }

        /// <summary>
        /// 효과음 AudioSource를 생성한다.
        /// </summary>
        /// <returns>생성된 AudioSource.</returns>
        private AudioSource CreateSfxSource()
        {
            AudioSource source;
            if (sfxSourcePrefab != null)
                source = Instantiate(sfxSourcePrefab, transform);
            else
            {
                GameObject sourceObject = new("SFX Source");
                sourceObject.transform.SetParent(transform, false);
                source = sourceObject.AddComponent<AudioSource>();
            }

            source.playOnAwake = false;
            sfxSources.Add(source);
            return source;
        }

        /// <summary>
        /// 현재 볼륨 설정을 AudioSource에 적용한다.
        /// </summary>
        private void ApplyVolumes()
        {
            if (musicSource != null)
                musicSource.volume = masterVolume * musicVolume;

            foreach (AudioSource source in sfxSources)
            {
                if (source != null && !source.isPlaying)
                    source.volume = masterVolume * sfxVolume;
            }
        }

        /// <summary>
        /// 기존 음악을 페이드아웃한 뒤 새 음악으로 교체한다.
        /// </summary>
        private IEnumerator FadeToNewMusicRoutine(AudioClip clip, bool loop, float duration)
        {
            yield return FadeMusicVolumeRoutine(musicSource.volume, 0f, duration * 0.5f);

            musicSource.clip = clip;
            musicSource.loop = loop;
            musicSource.Play();

            yield return FadeMusicVolumeRoutine(0f, masterVolume * musicVolume, duration * 0.5f);
            musicFadeRoutine = null;
            SWUtilsLog.Log($"[SWAudioManager] Fade music complete: {clip.name}");
        }

        /// <summary>
        /// 음악을 페이드아웃하고 정지한다.
        /// </summary>
        private IEnumerator FadeOutMusicRoutine(float duration)
        {
            yield return FadeMusicVolumeRoutine(musicSource.volume, 0f, duration);
            musicSource.Stop();
            musicSource.volume = masterVolume * musicVolume;
            musicFadeRoutine = null;
            SWUtilsLog.Log("[SWAudioManager] Fade out complete.");
        }

        /// <summary>
        /// 음악 볼륨을 지정한 값까지 보간한다.
        /// </summary>
        private IEnumerator FadeMusicVolumeRoutine(float from, float to, float duration)
        {
            if (duration <= 0f)
            {
                musicSource.volume = to;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                musicSource.volume = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }

            musicSource.volume = to;
        }
        #endregion // 내부
    }
}
