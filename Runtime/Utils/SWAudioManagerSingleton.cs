using UnityEngine;

namespace SWUtils
{
    /// <summary>
    /// 기존 SWAudioManagerSingleton 호출부를 유지하기 위한 호환 래퍼.
    /// 새 코드에서는 SWAudioManager.Instance를 사용한다.
    /// </summary>
    public class SWAudioManagerSingleton : SWSingleton<SWAudioManagerSingleton>
    {
        #region 필드
        [Header("=====> 참조 <=====")]
        [SerializeField] private SWAudioManager audioManager;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>실제 오디오 기능을 처리하는 매니저.</summary>
        public SWAudioManager AudioManager => audioManager;
        #endregion // 프로퍼티

        #region 초기화
        public override void Awake()
        {
            base.Awake();

            if (Instance != this) return;

            EnsureAudioManager();
            SWUtilsLog.Log("[SWAudioManagerSingleton] Initialized.");
        }
        #endregion // 초기화

        #region 라이브러리
        /// <summary>
        /// 런타임에 사용할 사운드 라이브러리를 지정한다.
        /// </summary>
        /// <param name="library">BGM/SFX 키가 등록된 라이브러리.</param>
        public void SetAudioLibrary(SWAudioLibrary library)
        {
            EnsureAudioManager();
            audioManager.SetAudioLibrary(library);
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
            EnsureAudioManager();
            audioManager.PlayMusic(key, loop, fadeDuration);
        }

        /// <summary>
        /// 현재 BGM을 정지한다.
        /// </summary>
        /// <param name="fadeDuration">페이드아웃 시간.</param>
        public void StopMusic(float fadeDuration = 0f)
        {
            EnsureAudioManager();
            audioManager.StopMusic(fadeDuration);
        }

        /// <summary>
        /// 현재 BGM을 일시 정지한다.
        /// </summary>
        public void PauseMusic()
        {
            EnsureAudioManager();
            audioManager.PauseMusic();
        }

        /// <summary>
        /// 일시 정지된 BGM을 다시 재생한다.
        /// </summary>
        public void ResumeMusic()
        {
            EnsureAudioManager();
            audioManager.ResumeMusic();
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
            EnsureAudioManager();
            return audioManager.PlaySfx(key, volumeScale, pitch);
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
            EnsureAudioManager();
            return audioManager.PlaySfxAtPoint(key, position, volumeScale, pitch);
        }

        /// <summary>
        /// 모든 효과음 재생을 정지한다.
        /// </summary>
        public void StopAllSfx()
        {
            EnsureAudioManager();
            audioManager.StopAllSfx();
        }
        #endregion // 효과음

        #region 볼륨
        /// <summary>
        /// 전체 볼륨을 설정한다.
        /// </summary>
        /// <param name="volume">0~1 사이의 볼륨 값.</param>
        public void SetMasterVolume(float volume)
        {
            EnsureAudioManager();
            audioManager.SetMasterVolume(volume);
        }

        /// <summary>
        /// 음악 볼륨을 설정한다.
        /// </summary>
        /// <param name="volume">0~1 사이의 볼륨 값.</param>
        public void SetMusicVolume(float volume)
        {
            EnsureAudioManager();
            audioManager.SetMusicVolume(volume);
        }

        /// <summary>
        /// 효과음 볼륨을 설정한다.
        /// </summary>
        /// <param name="volume">0~1 사이의 볼륨 값.</param>
        public void SetSfxVolume(float volume)
        {
            EnsureAudioManager();
            audioManager.SetSfxVolume(volume);
        }

        /// <summary>
        /// 현재 볼륨 설정을 PlayerPrefs에 저장한다.
        /// </summary>
        /// <param name="keyPrefix">저장 키 prefix.</param>
        public void SaveVolumes(string keyPrefix = "SWAudioManager")
        {
            EnsureAudioManager();
            audioManager.SaveVolumes(keyPrefix);
        }

        /// <summary>
        /// PlayerPrefs에 저장된 볼륨 설정을 불러온다.
        /// </summary>
        /// <param name="keyPrefix">저장 키 prefix.</param>
        public void LoadVolumes(string keyPrefix = "SWAudioManager")
        {
            EnsureAudioManager();
            audioManager.LoadVolumes(keyPrefix);
        }
        #endregion // 볼륨

        #region 내부
        /// <summary>
        /// 연결된 SWAudioManager가 없으면 같은 오브젝트에서 찾거나 자동으로 추가한다.
        /// </summary>
        private void EnsureAudioManager()
        {
            if (audioManager != null) return;

            audioManager = GetComponent<SWAudioManager>();
            if (audioManager == null)
            {
                audioManager = gameObject.AddComponent<SWAudioManager>();
                SWUtilsLog.Log("[SWAudioManagerSingleton] SWAudioManager auto-created.");
            }
        }
        #endregion // 내부
    }
}
