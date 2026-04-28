using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SWUtils
{
    /// <summary>
    /// 기존 SWSceneLoaderSingleton 호출부를 유지하기 위한 호환 래퍼.
    /// 새 코드에서는 SWSceneLoader.Instance를 사용한다.
    /// </summary>
    public class SWSceneLoaderSingleton : SWSingleton<SWSceneLoaderSingleton>
    {
        #region 필드
        [Header("=====> 참조 <=====")]
        [SerializeField] private SWSceneLoader sceneLoader;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>실제 씬 로딩 기능을 처리하는 로더.</summary>
        public SWSceneLoader SceneLoader => sceneLoader;
        #endregion // 프로퍼티

        #region 초기화
        public override void Awake()
        {
            base.Awake();

            if (Instance != this) return;

            EnsureSceneLoader();
            SWUtilsLog.Log("[SWSceneLoaderSingleton] Initialized.");
        }
        #endregion // 초기화

        #region 로드
        /// <summary>
        /// 씬 이름으로 씬을 로드한다.
        /// </summary>
        /// <param name="sceneName">로드할 씬 이름.</param>
        /// <param name="mode">씬 로드 방식.</param>
        /// <param name="onProgress">진행률 콜백.</param>
        /// <param name="onComplete">완료 콜백.</param>
        public void LoadScene(string sceneName, LoadSceneMode mode = LoadSceneMode.Single,
            Action<float> onProgress = null, Action onComplete = null)
        {
            EnsureSceneLoader();
            sceneLoader.LoadScene(sceneName, mode, onProgress, onComplete);
        }

        /// <summary>
        /// 빌드 인덱스로 씬을 로드한다.
        /// </summary>
        /// <param name="sceneBuildIndex">로드할 씬 빌드 인덱스.</param>
        /// <param name="mode">씬 로드 방식.</param>
        /// <param name="onProgress">진행률 콜백.</param>
        /// <param name="onComplete">완료 콜백.</param>
        public void LoadScene(int sceneBuildIndex, LoadSceneMode mode = LoadSceneMode.Single,
            Action<float> onProgress = null, Action onComplete = null)
        {
            EnsureSceneLoader();
            sceneLoader.LoadScene(sceneBuildIndex, mode, onProgress, onComplete);
        }

        /// <summary>
        /// 씬을 Additive 방식으로 로드한다.
        /// </summary>
        /// <param name="sceneName">로드할 씬 이름.</param>
        /// <param name="onProgress">진행률 콜백.</param>
        /// <param name="onComplete">완료 콜백.</param>
        public void LoadAdditive(string sceneName, Action<float> onProgress = null, Action onComplete = null)
        {
            EnsureSceneLoader();
            sceneLoader.LoadAdditive(sceneName, onProgress, onComplete);
        }

        /// <summary>
        /// 현재 활성 씬을 다시 로드한다.
        /// </summary>
        /// <param name="onProgress">진행률 콜백.</param>
        /// <param name="onComplete">완료 콜백.</param>
        public void ReloadActiveScene(Action<float> onProgress = null, Action onComplete = null)
        {
            EnsureSceneLoader();
            sceneLoader.ReloadActiveScene(onProgress, onComplete);
        }

        /// <summary>
        /// 로드된 씬을 언로드한다.
        /// </summary>
        /// <param name="sceneName">언로드할 씬 이름.</param>
        /// <param name="onProgress">진행률 콜백.</param>
        /// <param name="onComplete">완료 콜백.</param>
        public void UnloadScene(string sceneName, Action<float> onProgress = null, Action onComplete = null)
        {
            EnsureSceneLoader();
            sceneLoader.UnloadScene(sceneName, onProgress, onComplete);
        }

        /// <summary>
        /// 로드되어 있는 씬을 활성 씬으로 설정한다.
        /// </summary>
        /// <param name="sceneName">활성화할 씬 이름.</param>
        public void SetActiveScene(string sceneName)
        {
            EnsureSceneLoader();
            sceneLoader.SetActiveScene(sceneName);
        }

        /// <summary>
        /// 현재 진행 중인 로딩 코루틴을 취소한다.
        /// </summary>
        public void CancelCurrentLoad()
        {
            EnsureSceneLoader();
            sceneLoader.CancelCurrentLoad();
        }
        #endregion // 로드

        #region 내부
        /// <summary>
        /// 연결된 SWSceneLoader가 없으면 같은 오브젝트에서 찾거나 자동으로 추가한다.
        /// </summary>
        private void EnsureSceneLoader()
        {
            if (sceneLoader != null) return;

            sceneLoader = GetComponent<SWSceneLoader>();
            if (sceneLoader == null)
            {
                sceneLoader = gameObject.AddComponent<SWSceneLoader>();
                SWUtilsLog.Log("[SWSceneLoaderSingleton] SWSceneLoader auto-created.");
            }
        }
        #endregion // 내부
    }
}
