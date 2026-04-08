using System;
using UnityEngine;

namespace SWUtils
{
    [System.Serializable]
    public class SWUtilsRefillTimer
    {
        #region 필드
        /// <summary>PlayerPrefs 저장에 사용되는 고유 식별자.</summary>
        private readonly string id;
        /// <summary>최대 보유 수량.</summary>
        private readonly int maxCount;
        /// <summary>1개 회복에 걸리는 시간(초).</summary>
        private readonly float intervalSec;
        /// <summary>현재 보유 수량.</summary>
        private int count;
        /// <summary>마지막으로 소모한 UTC 시각.</summary>
        private DateTime lastUseUtc;
        /// <summary>PlayerPrefs 키: 수량 저장용.</summary>
        private string KeyCount => $"SwUtilsRefillTimer{id}count";
        /// <summary>PlayerPrefs 키: 마지막 사용 시각 저장용.</summary>
        private string KeyLastUse => $"SwUtilsRefillTimer{id}_LastUse";
        #endregion // 필드

        #region 프로퍼티
        /// <summary>현재 보유 수량.</summary>
        public int Count => count;
        /// <summary>최대 보유 수량.</summary>
        public int MaxCount => maxCount;
        /// <summary>수량이 가득 찼는지 여부.</summary>
        public bool IsFull => count >= maxCount;
        /// <summary>사용 가능 여부 (1개 이상 보유).</summary>
        public bool CanUse => count > 0;
        /// <summary>다음 1개 회복까지 남은 시간(초). 가득 찼으면 0.</summary>
        public float RemainSeconds
        {
            get
            {
                if (IsFull) return 0f;
                float elapsed = (float)(DateTime.UtcNow - lastUseUtc).TotalSeconds;
                float remain = intervalSec - (elapsed % intervalSec);
                return Mathf.Max(0f, remain);
            }
        }
        /// <summary>다음 1개 회복까지 남은 시간을 "MM:SS" 형식으로 반환.</summary>
        public string RemainText => SWUtilsTime.ToMinSec(RemainSeconds);
        /// <summary>다음 1개 회복까지 남은 시간을 자연어로 반환.</summary>
        public string RemainReadable => SWUtilsTime.ToReadable(RemainSeconds);
        /// <summary>전부 회복까지 남은 시간(초). 가득 찼으면 0.</summary>
        public float RemainSecondsAll
        {
            get
            {
                if (IsFull) return 0f;
                int need = maxCount - count;
                float elapsed = (float)(DateTime.UtcNow - lastUseUtc).TotalSeconds;
                float total = need * intervalSec - (elapsed % intervalSec);
                return Mathf.Max(0f, total);
            }
        }
        /// <summary>전부 회복까지 남은 시간을 "HH:MM:SS" 형식으로 반환.</summary>
        public string RemainTextAll => SWUtilsTime.ToHourMinSec(RemainSecondsAll);
        /// <summary>회복 진행률 (0 = 방금 소모, 1 = 회복 직전).</summary>
        public float Progress
        {
            get
            {
                if (IsFull) return 1f;
                float elapsed = (float)(DateTime.UtcNow - lastUseUtc).TotalSeconds;
                return Mathf.Clamp01((elapsed % intervalSec) / intervalSec);
            }
        }
        /// <summary>"현재/최대" 형식 문자열. 예: "3/5"</summary>
        public string CountText => $"{count}/{maxCount}";
        #endregion // 프로퍼티

        #region 생성자
        /// <summary>
        /// RefillTimer를 생성한다.
        /// </summary>
        /// <param name="id">고유 식별자 (PlayerPrefs 키에 사용)</param>
        /// <param name="maxCount">최대 보유 수량</param>
        /// <param name="intervalSec">1개 회복 간격(초)</param>
        public SWUtilsRefillTimer(string id, int maxCount, float intervalSec)
        {
            this.id = id;
            this.maxCount = maxCount;
            this.intervalSec = intervalSec;
            count = maxCount;
            lastUseUtc = DateTime.UtcNow;

            Load();
        }
        #endregion // 생성자

        #region 기능
        /// <summary>
        /// 경과 시간에 따라 회복량을 계산하고 수량을 갱신한다.
        /// Update 또는 앱 복귀 시 호출한다.
        /// </summary>
        /// <returns>이번 호출로 회복된 수량</returns>
        public int Tick()
        {
            if (IsFull) return 0;

            double elapsed = (DateTime.UtcNow - lastUseUtc).TotalSeconds;
            int recovered = Mathf.FloorToInt((float)(elapsed / intervalSec));

            if (recovered <= 0) return 0;

            int before = count;
            count = Mathf.Min(count + recovered, maxCount);
            int actual = count - before;

            if (IsFull)
                lastUseUtc = DateTime.UtcNow;
            else
                lastUseUtc = lastUseUtc.AddSeconds(recovered * intervalSec);

            Save();
            return actual;
        }

        /// <summary>
        /// 아이템을 1개 소모한다.
        /// </summary>
        /// <returns>소모 성공 여부</returns>
        public bool Use()
        {
            return Use(1);
        }

        /// <summary>
        /// 아이템을 지정한 수만큼 소모한다.
        /// </summary>
        /// <param name="amount">소모할 수량</param>
        /// <returns>소모 성공 여부</returns>
        public bool Use(int amount)
        {
            Tick();

            if (count < amount) return false;

            bool wasFull = IsFull;
            count -= amount;

            if (wasFull)
                lastUseUtc = DateTime.UtcNow;

            Save();
            return true;
        }

        /// <summary>
        /// 수량을 즉시 추가한다. (보상, 광고 시청 등)
        /// </summary>
        /// <param name="amount">추가할 수량</param>
        public void Add(int amount)
        {
            count = Mathf.Min(count + amount, maxCount);
            if (IsFull)
                lastUseUtc = DateTime.UtcNow;
            Save();
        }

        /// <summary>
        /// 수량을 최대치로 즉시 채운다.
        /// </summary>
        public void Fill()
        {
            count = maxCount;
            lastUseUtc = DateTime.UtcNow;
            Save();
        }

        /// <summary>
        /// 수량을 직접 설정한다. (서버 동기화 등)
        /// </summary>
        /// <param name="count">설정할 수량</param>
        /// <param name="lastUseUtc">마지막 사용 UTC 시각. null이면 현재 시각 사용</param>
        public void Set(int count, DateTime? lastUseUtc = null)
        {
            this.count = Mathf.Clamp(count, 0, maxCount);
            lastUseUtc = lastUseUtc ?? DateTime.UtcNow;
            Save();
        }
        #endregion // 기능

        #region 오프라인 회복
        /// <summary>
        /// 오프라인 경과 시간을 반영하여 수량을 회복한다.
        /// 앱 시작 시 호출한다.
        /// </summary>
        /// <returns>회복된 수량</returns>
        public int RecoverOffline()
        {
            return Tick();
        }

        /// <summary>
        /// 오프라인 회복량을 상한 적용하여 반영한다.
        /// </summary>
        /// <param name="maxRecoverCount">최대 회복 개수</param>
        /// <returns>회복된 수량</returns>
        public int RecoverOffline(int maxRecoverCount)
        {
            if (IsFull) return 0;

            double elapsed = (DateTime.UtcNow - lastUseUtc).TotalSeconds;
            int recovered = Mathf.FloorToInt((float)(elapsed / intervalSec));
            recovered = Mathf.Min(recovered, maxRecoverCount);

            if (recovered <= 0) return 0;

            int before = count;
            count = Mathf.Min(count + recovered, maxCount);
            int actual = count - before;

            if (IsFull)
                lastUseUtc = DateTime.UtcNow;
            else
                lastUseUtc = lastUseUtc.AddSeconds(recovered * intervalSec);

            Save();
            return actual;
        }
        #endregion // 오프라인 회복

        #region 저장 / 로드
        /// <summary>
        /// 현재 상태를 PlayerPrefs에 저장한다.
        /// </summary>
        private void Save()
        {
            PlayerPrefs.SetInt(KeyCount, count);
            PlayerPrefs.SetString(KeyLastUse, lastUseUtc.ToString("o"));
            PlayerPrefs.Save();
        }

        /// <summary>
        /// PlayerPrefs에서 상태를 불러온다.
        /// </summary>
        private void Load()
        {
            if (!PlayerPrefs.HasKey(KeyCount)) return;

            count = Mathf.Clamp(PlayerPrefs.GetInt(KeyCount, maxCount), 0, maxCount);

            string stored = PlayerPrefs.GetString(KeyLastUse, "");
            if (!string.IsNullOrEmpty(stored) &&
                DateTime.TryParse(stored, null,
                    System.Globalization.DateTimeStyles.RoundtripKind, out DateTime parsed))
            {
                lastUseUtc = parsed;
            }

            Tick();
        }

        /// <summary>
        /// 저장 데이터를 초기화하고 수량을 최대치로 리셋한다.
        /// </summary>
        public void Reset()
        {
            count = maxCount;
            lastUseUtc = DateTime.UtcNow;
            PlayerPrefs.DeleteKey(KeyCount);
            PlayerPrefs.DeleteKey(KeyLastUse);
            PlayerPrefs.Save();
        }
        #endregion // 저장 / 로드
    }
}