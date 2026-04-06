using System;
using System.Globalization;
using UnityEngine;

namespace SWUtils
{
    public class SwUtilsTime
    {
        #region 필드
        /// <summary>서버-클라이언트 간 시간 차이(초). 서버 응답 수신 시 갱신.</summary>
        private static double serverOffset;
        /// <summary>오프라인 경과 시간 저장용 기본 PlayerPrefs 키</summary>
        private const string DefaultExitTimeKey = "SwUtilsTime_LastExitTime";
        /// <summary>일일 리셋 기록용 기본 PlayerPrefs 키</summary>
        private const string DefaultResetKey = "SwUtilsTime_LastResetDate";
        /// <summary>간이 프로파일링용 스톱워치.</summary>
        private static readonly System.Diagnostics.Stopwatch stopWatch = new();
        /// <summary>서버 시간 문자열 파싱에 사용되는 포맷 목록.</summary>
        private static readonly string[] ParseFormats = {
            "yyyy-MM-dd HH:mm:ss",
            "yyyy-MM-ddTHH:mm:ssZ",
            "yyyy-MM-ddTHH:mm:ss",
            "yyyyMMddHHmmss",
            "yyyy-MM-dd",
        };
        #endregion // 필드

        #region 서버 / 클라이언트 시간 보정
        /// <summary>
        /// 서버 응답 시각과 로컬 시각을 비교하여 오프셋을 갱신한다.
        /// </summary>
        /// <param name="serverUtcNow">서버에서 수신한 현재 UTC 시각</param>
        public static void SyncServerOffset(DateTime serverUtcNow)
        {
            serverOffset = (serverUtcNow - DateTime.UtcNow).TotalSeconds;
        }

        /// <summary>
        /// 보정된 현재 서버 시각(UTC)을 반환한다.
        /// </summary>
        public static DateTime ServerUtcNow
            => DateTime.UtcNow.AddSeconds(serverOffset);

        /// <summary>
        /// 보정된 현재 서버 시각을 로컬 시간대로 변환하여 반환한다.
        /// </summary>
        public static DateTime ServerLocalNow
            => ServerUtcNow.ToLocalTime();
        #endregion // 서버 / 클라이언트 시간 보정

        #region 파싱 & 변환
        /// <summary>
        /// 서버에서 내려온 시간 문자열을 <see cref="DateTime"/>(UTC)으로 안전하게 파싱한다.
        /// </summary>
        /// <param name="serverTime">서버 시간 문자열</param>
        /// <param name="result">파싱 결과 DateTime(UTC)</param>
        /// <returns>파싱 성공 여부</returns>
        public static bool TryParseServerTime(string serverTime, out DateTime result)
        {
            result = DateTime.MinValue;
            if (string.IsNullOrEmpty(serverTime) || serverTime == "null")
                return false;

            string s = serverTime.Trim();
            if (!s.Contains("T"))
            {
                s = s.Replace(" ", "T");
                if (!s.EndsWith("Z")) s += "Z";
            }

            return DateTime.TryParseExact(
                s, ParseFormats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal,
                out result);
        }

        /// <summary>
        /// <see cref="DateTime"/>을 지정한 포맷의 문자열로 변환한다.
        /// </summary>
        /// <param name="time">변환할 DateTime</param>
        /// <param name="fmt">출력 포맷 문자열</param>
        /// <returns>포맷된 시간 문자열</returns>
        public static string ToString(DateTime time, string fmt = "yyyy-MM-dd HH:mm:ss")
        {
            return time.ToString(fmt, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// 서버 시간 문자열을 로컬 시간 문자열로 변환한다.
        /// </summary>
        /// <param name="serverTime">서버 시간 문자열</param>
        /// <returns>로컬 시간 문자열. 파싱 실패 시 빈 문자열</returns>
        public static string ToLocalTimeString(string serverTime)
        {
            if (!TryParseServerTime(serverTime, out DateTime utc))
                return string.Empty;
            return ToString(utc.ToLocalTime());
        }

        /// <summary>
        /// "yyMMdd" 축약 날짜 문자열을 "yyyy-MM-dd" 형식으로 변환한다.
        /// </summary>
        /// <param name="yyMMdd">6자리 축약 날짜 문자열</param>
        /// <returns>변환된 날짜 문자열. 실패 시 빈 문자열</returns>
        public static string ShortDateToFormat(string yyMMdd)
        {
            if (yyMMdd == null || yyMMdd.Length < 6) return string.Empty;
            if (DateTime.TryParseExact("20" + yyMMdd.Substring(0, 6), "yyyyMMdd",
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dt))
                return dt.ToString("yyyy-MM-dd");
            return string.Empty;
        }
        #endregion // 파싱 & 변환

        #region Unix 타임스탬프
        /// <summary>
        /// Unix 타임스탬프(초)를 <see cref="DateTime"/>(UTC)으로 변환한다.
        /// </summary>
        /// <param name="unixSeconds">Unix 타임스탬프(초)</param>
        /// <returns>UTC DateTime</returns>
        public static DateTime UnixToDateTime(long unixSeconds)
            => DateTimeOffset.FromUnixTimeSeconds(unixSeconds).UtcDateTime;

        /// <summary>
        /// <see cref="DateTime"/>(UTC)을 Unix 타임스탬프(초)로 변환한다.
        /// </summary>
        /// <param name="utcTime">UTC DateTime</param>
        /// <returns>Unix 타임스탬프(초)</returns>
        public static long DateTimeToUnix(DateTime utcTime)
            => new DateTimeOffset(utcTime, TimeSpan.Zero).ToUnixTimeSeconds();

        /// <summary>
        /// 현재 시각의 Unix 타임스탬프(초)를 반환한다.
        /// </summary>
        /// <returns>현재 Unix 타임스탬프(초)</returns>
        public static long NowUnix() 
            => DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        #endregion // Unix 타임스탬프

        #region 경과 / 남은 시간 계산
        /// <summary>
        /// 서버 기준 경과 시간(초)을 반환한다. 양수=시간 지남, 음수=아직 안 됨.
        /// </summary>
        /// <param name="utcTarget">비교 대상 UTC 시각</param>
        /// <returns>경과 시간(초)</returns>
        public static double ElapsedServer(DateTime utcTarget)
            => (ServerUtcNow - utcTarget).TotalSeconds;

        /// <param name="serverTime">비교 대상 서버 시간 문자열</param>
        public static double ElapsedServer(string serverTime)
            => TryParseServerTime(serverTime, out DateTime dt) ? ElapsedServer(dt) : 0;

        /// <summary>
        /// 서버 기준 남은 시간(초)을 반환한다. 양수=아직 남음, 음수=이미 지남.
        /// </summary>
        /// <param name="utcTarget">목표 UTC 시각</param>
        /// <returns>남은 시간(초)</returns>
        public static double RemainServer(DateTime utcTarget)
            => (utcTarget - ServerUtcNow).TotalSeconds;

        /// <param name="serverTime">목표 서버 시간 문자열</param>
        public static double RemainServer(string serverTime)
            => TryParseServerTime(serverTime, out DateTime dt) ? RemainServer(dt) : 0;

        /// <summary>
        /// 로컬 기준 경과 시간(초)을 반환한다.
        /// </summary>
        /// <param name="localTarget">비교 대상 로컬 시각</param>
        /// <returns>경과 시간(초)</returns>
        public static double ElapsedLocal(DateTime localTarget)
            => (DateTime.Now - localTarget).TotalSeconds;

        /// <param name="time">비교 대상 시간 문자열</param>
        public static double ElapsedLocal(string time)
        {
            try { return ElapsedLocal(Convert.ToDateTime(time)); }
            catch { return 0; }
        }

        /// <summary>
        /// 로컬 기준 남은 시간(초)을 반환한다.
        /// </summary>
        /// <param name="localTarget">목표 로컬 시각</param>
        /// <returns>남은 시간(초)</returns>
        public static double RemainLocal(DateTime localTarget)
            => (localTarget - DateTime.Now).TotalSeconds;

        /// <param name="time">목표 시간 문자열</param>
        public static double RemainLocal(string time)
        {
            if (string.IsNullOrEmpty(time)) return 0;
            try { return RemainLocal(Convert.ToDateTime(time)); }
            catch { return 0; }
        }
        #endregion // 경과 / 남은 시간 계산

        #region 날짜 / 시간대 판별
        /// <summary>
        /// 주어진 시간이 오늘인지 확인한다. (로컬 기준)
        /// </summary>
        /// <param name="time">확인할 시각</param>
        /// <returns>오늘이면 true</returns>
        public static bool IsToday(DateTime time)
            => time.Date == DateTime.Today;

        /// <param name="time">확인할 시간 문자열</param>
        public static bool IsToday(string time)
        {
            try { return IsToday(Convert.ToDateTime(time)); }
            catch { return false; }
        }

        /// <summary>
        /// 현재 야간 시간대인지 확인한다.
        /// </summary>
        /// <param name="nightStart">야간 시작 시(hour)</param>
        /// <param name="nightEnd">야간 종료 시(hour)</param>
        /// <returns>야간이면 true</returns>
        public static bool IsNight(int nightStart = 19, int nightEnd = 5)
        {
            int h = DateTime.Now.Hour;
            return h >= nightStart || h <= nightEnd;
        }

        /// <summary>
        /// 현재 요일을 반환한다.
        /// </summary>
        /// <returns>현재 요일</returns>
        public static DayOfWeek GetDayOfWeek() 
            => DateTime.Today.DayOfWeek;

        /// <summary>
        /// 특정 요일인지 확인한다.
        /// </summary>
        /// <param name="day">확인할 요일</param>
        /// <param name="time">기준 시간 문자열. 비어있으면 현재 시각 사용</param>
        /// <returns>해당 요일이면 true</returns>
        public static bool IsDayOfWeek(DayOfWeek day, string time = "")
        {
            DateTime dt = string.IsNullOrEmpty(time) ? DateTime.Now : Convert.ToDateTime(time);
            return dt.DayOfWeek == day;
        }

        /// <summary>
        /// 오늘 날짜를 "yyMMdd" 형식으로 반환한다.
        /// </summary>
        /// <param name="utc">true면 UTC 기준, false면 로컬 기준</param>
        /// <returns>"yyMMdd" 형식 날짜 문자열</returns>
        public static string GetDateShort(bool utc = false)
            => (utc ? DateTime.UtcNow : DateTime.Now).ToString("yyMMdd");

        /// <summary>
        /// 현재 시각으로부터 특정 시간 후의 <see cref="DateTime"/>을 반환한다.
        /// </summary>
        /// <param name="hours">추가할 시간</param>
        /// <param name="minutes">추가할 분</param>
        /// <param name="seconds">추가할 초</param>
        /// <returns>계산된 DateTime</returns>
        public static DateTime After(int hours = 0, int minutes = 0, int seconds = 0)
            => DateTime.Now.AddHours(hours).AddMinutes(minutes).AddSeconds(seconds);

        /// <summary>
        /// 오늘 자정(00:00) 기준 특정 시각의 <see cref="DateTime"/>을 반환한다.
        /// </summary>
        /// <param name="hour">시</param>
        /// <param name="minute">분</param>
        /// <param name="second">초</param>
        /// <returns>오늘 해당 시각의 DateTime</returns>
        public static DateTime TodayAt(int hour, int minute = 0, int second = 0)
            => DateTime.Today.AddHours(hour).AddMinutes(minute).AddSeconds(second);

        /// <summary>
        /// 내일 자정(00:00)을 반환한다.
        /// </summary>
        public static DateTime TomorrowBegin
            => DateTime.Today.AddDays(1);
        #endregion // 날짜 / 시간대 판별

        #region 포맷팅
        /// <summary>
        /// 초를 "MM:SS" 형식으로 변환한다.
        /// </summary>
        /// <param name="seconds">변환할 초</param>
        /// <returns>"MM:SS" 형식 문자열</returns>
        public static string ToMinSec(float seconds)
        {
            int m = Mathf.FloorToInt(Mathf.Abs(seconds) / 60f);
            int s = Mathf.FloorToInt(Mathf.Abs(seconds) % 60f);
            return $"{m:D2}:{s:D2}";
        }

        /// <summary>
        /// 초를 "HH:MM:SS" 형식으로 변환한다.
        /// </summary>
        /// <param name="seconds">변환할 초</param>
        /// <returns>"HH:MM:SS" 형식 문자열</returns>
        public static string ToHourMinSec(float seconds)
        {
            float abs = Mathf.Abs(seconds);
            int h = Mathf.FloorToInt(abs / 3600f);
            int m = Mathf.FloorToInt((abs % 3600f) / 60f);
            int s = Mathf.FloorToInt(abs % 60f);
            return $"{h:D2}:{m:D2}:{s:D2}";
        }

        /// <summary>
        /// 초를 "MM:SS.mmm" 형식으로 변환한다.
        /// </summary>
        /// <param name="seconds">변환할 초</param>
        /// <returns>"MM:SS.mmm" 형식 문자열</returns>
        public static string ToMinSecMs(float seconds)
        {
            float abs = Mathf.Abs(seconds);
            int m = Mathf.FloorToInt(abs / 60f);
            int s = Mathf.FloorToInt(abs % 60f);
            int ms = Mathf.FloorToInt((abs * 1000f) % 1000f);
            return $"{m:D2}:{s:D2}.{ms:D3}";
        }

        /// <summary>
        /// 초를 한국어 자연어 형태로 변환한다.
        /// </summary>
        /// <param name="seconds">변환할 초</param>
        /// <returns>"N시간 N분 N초" 형식 문자열</returns>
        public static string ToReadable(float seconds)
        {
            float abs = Mathf.Abs(seconds);
            int h = Mathf.FloorToInt(abs / 3600f);
            int m = Mathf.FloorToInt((abs % 3600f) / 60f);
            int s = Mathf.FloorToInt(abs % 60f);

            if (h > 0) return $"{h}시간 {m}분 {s}초";
            if (m > 0) return $"{m}분 {s}초";
            return $"{s}초";
        }

        /// <summary>
        /// 초를 1단위 축약형으로 변환한다.
        /// </summary>
        /// <param name="seconds">변환할 초</param>
        /// <returns>"Nd", "Nh", "Nm", "Ns" 형식 문자열</returns>
        public static string ToShortForm(float seconds)
        {
            float abs = Mathf.Abs(seconds);
            if (abs >= 86400f) return $"{Mathf.FloorToInt(abs / 86400f)}d";
            if (abs >= 3600f) return $"{Mathf.FloorToInt(abs / 3600f)}h";
            if (abs >= 60f) return $"{Mathf.FloorToInt(abs / 60f)}m";
            return $"{Mathf.FloorToInt(abs)}s";
        }

        /// <summary>
        /// 초를 2단위 축약형으로 변환한다.
        /// </summary>
        /// <param name="seconds">변환할 초</param>
        /// <returns>"Nd Nh", "Nh Nm", "Nm Ns" 형식 문자열</returns>
        public static string ToShortForm2(float seconds)
        {
            float abs = Mathf.Abs(seconds);
            int d = Mathf.FloorToInt(abs / 86400f);
            int h = Mathf.FloorToInt((abs % 86400f) / 3600f);
            int m = Mathf.FloorToInt((abs % 3600f) / 60f);
            int s = Mathf.FloorToInt(abs % 60f);

            if (d > 0) return $"{d}d {h}h";
            if (h > 0) return $"{h}h {m}m";
            if (m > 0) return $"{m}m {s}s";
            return $"{s}s";
        }
        #endregion // 포맷팅

        #region 상대 시간 표현
        /// <summary>
        /// 과거 시점을 상대 시간 문자열로 변환한다.
        /// </summary>
        /// <param name="utcTime">과거 UTC 시각</param>
        /// <returns>"방금 전", "N분 전", "N시간 전", "N일 전" 또는 날짜 문자열</returns>
        public static string TimeAgo(DateTime utcTime)
        {
            double sec = (DateTime.UtcNow - utcTime).TotalSeconds;
            if (sec < 60) return "방금 전";
            if (sec < 3600) return $"{(int)(sec / 60)}분 전";
            if (sec < 86400) return $"{(int)(sec / 3600)}시간 전";
            if (sec < 604800) return $"{(int)(sec / 86400)}일 전";
            return utcTime.ToLocalTime().ToString("yyyy-MM-dd");
        }

        /// <summary>
        /// 미래 시점까지 남은 시간을 상대 시간 문자열로 변환한다.
        /// </summary>
        /// <param name="utcTarget">미래 UTC 시각</param>
        /// <returns>"지금", "N초 후", "N분 후", "N시간 후", "N일 후"</returns>
        public static string TimeUntil(DateTime utcTarget)
        {
            double sec = (utcTarget - DateTime.UtcNow).TotalSeconds;
            if (sec <= 0) return "지금";
            if (sec < 60) return $"{(int)sec}초 후";
            if (sec < 3600) return $"{(int)(sec / 60)}분 후";
            if (sec < 86400) return $"{(int)(sec / 3600)}시간 후";
            return $"{(int)(sec / 86400)}일 후";
        }
        #endregion // 상대 시간 표현

        #region 쿨다운 & 타이머
        /// <summary>
        /// 쿨다운 진행률을 반환한다.
        /// </summary>
        /// <param name="elapsed">경과 시간(초)</param>
        /// <param name="duration">전체 쿨다운 시간(초)</param>
        /// <returns>0(시작) ~ 1(완료) 사이의 값</returns>
        public static float CooldownProgress(float elapsed, float duration)
            => duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);

        /// <summary>
        /// 쿨다운이 완료되었는지 확인한다.
        /// </summary>
        /// <param name="elapsed">경과 시간(초)</param>
        /// <param name="duration">전체 쿨다운 시간(초)</param>
        /// <returns>완료되었으면 true</returns>
        public static bool IsCooldownDone(float elapsed, float duration)
            => elapsed >= duration;

        /// <summary>
        /// 남은 쿨다운 시간(초)을 반환한다.
        /// </summary>
        /// <param name="elapsed">경과 시간(초)</param>
        /// <param name="duration">전체 쿨다운 시간(초)</param>
        /// <returns>남은 시간(초). 최소 0</returns>
        public static float CooldownRemain(float elapsed, float duration)
            => Mathf.Max(0f, duration - elapsed);

        /// <summary>
        /// <see cref="DateTime"/> 기반 쿨다운 완료 여부를 확인한다.
        /// </summary>
        /// <param name="startUtc">쿨다운 시작 UTC 시각</param>
        /// <param name="durationSec">쿨다운 지속 시간(초)</param>
        /// <returns>완료되었으면 true</returns>
        public static bool IsCooldownDone(DateTime startUtc, float durationSec)
            => (DateTime.UtcNow - startUtc).TotalSeconds >= durationSec;

        /// <summary>
        /// <see cref="DateTime"/> 기반 남은 쿨다운 시간(초)을 반환한다.
        /// </summary>
        /// <param name="startUtc">쿨다운 시작 UTC 시각</param>
        /// <param name="durationSec">쿨다운 지속 시간(초)</param>
        /// <returns>남은 시간(초). 최소 0</returns>
        public static float CooldownRemain(DateTime startUtc, float durationSec)
            => Mathf.Max(0f, durationSec - (float)(DateTime.UtcNow - startUtc).TotalSeconds);
        #endregion // 쿨다운 & 타이머

        #region 보간 & 이징 헬퍼
        /// <summary>
        /// 경과 시간을 0~1로 정규화한다.
        /// </summary>
        /// <param name="elapsed">경과 시간</param>
        /// <param name="duration">전체 시간</param>
        /// <returns>0 ~ 1 사이의 정규화된 값</returns>
        public static float Normalize(float elapsed, float duration)
            => duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);

        /// <summary>
        /// <see cref="Time.time"/> 기반 핑퐁 타이머 (0→1→0 반복).
        /// </summary>
        /// <param name="duration">한 사이클 시간(초)</param>
        /// <returns>0 ~ 1 사이의 핑퐁 값</returns>
        public static float PingPong01(float duration)
            => duration <= 0f ? 0f : Mathf.PingPong(Time.time, duration) / duration;

        /// <summary>
        /// 커스텀 시작 시간 기반 핑퐁 타이머.
        /// </summary>
        /// <param name="startTime">시작 시각 (Time.time 기준)</param>
        /// <param name="duration">한 사이클 시간(초)</param>
        /// <returns>0 ~ 1 사이의 핑퐁 값</returns>
        public static float PingPong01(float startTime, float duration)
            => duration <= 0f ? 0f : Mathf.PingPong(Time.time - startTime, duration) / duration;

        /// <summary>
        /// <see cref="Time.time"/> 기반 반복 타이머 (0→1 반복).
        /// </summary>
        /// <param name="duration">한 사이클 시간(초)</param>
        /// <returns>0 ~ 1 사이의 반복 값</returns>
        public static float Repeat01(float duration)
            => duration <= 0f ? 0f : Mathf.Repeat(Time.time, duration) / duration;
        #endregion // 보간 & 이징 헬퍼

        #region 시간 단위 변환
        /// <summary>프레임 수를 초로 변환한다.</summary>
        /// <param name="frames">프레임 수</param>
        /// <param name="fps">초당 프레임 수</param>
        /// <returns>초 단위 시간</returns>
        public static float FramesToSeconds(int frames, float fps = 60f) 
            => frames / fps;

        /// <summary>초를 프레임 수로 변환한다.</summary>
        /// <param name="seconds">초</param>
        /// <param name="fps">초당 프레임 수</param>
        /// <returns>프레임 수 (반올림)</returns>
        public static int SecondsToFrames(float seconds, float fps = 60f) 
            => Mathf.RoundToInt(seconds * fps);

        /// <summary>BPM을 초 단위 비트 간격으로 변환한다.</summary>
        /// <param name="bpm">분당 비트 수</param>
        /// <returns>비트 간격(초)</returns>
        public static float BpmToInterval(float bpm) 
            => 60f / bpm;

        /// <summary>분을 초로 변환한다.</summary>
        /// <param name="min">분</param>
        /// <returns>초</returns>
        public static float MinutesToSeconds(float min) 
            => min * 60f;

        /// <summary>시간을 초로 변환한다.</summary>
        /// <param name="hours">시간</param>
        /// <returns>초</returns>
        public static float HoursToSeconds(float hours) 
            => hours * 3600f;

        /// <summary>일을 초로 변환한다.</summary>
        /// <param name="days">일</param>
        /// <returns>초</returns>
        public static float DaysToSeconds(float days) 
            => days * 86400f;
        #endregion // 시간 단위 변환

        #region 오프라인 경과 시간
        /// <summary>
        /// 현재 UTC 시간을 PlayerPrefs에 저장한다.
        /// </summary>
        /// <param name="key">PlayerPrefs 키</param>
        public static void SaveExitTime(string key = DefaultExitTimeKey)
        {
            PlayerPrefs.SetString(key, DateTime.UtcNow.ToString("o"));
            PlayerPrefs.Save();
        }

        /// <summary>
        /// 마지막 저장 시점 이후 경과 시간(초)을 반환한다.
        /// </summary>
        /// <param name="key">PlayerPrefs 키</param>
        /// <returns>경과 시간(초). 저장 기록 없으면 0</returns>
        public static double GetOfflineSeconds(string key = DefaultExitTimeKey)
        {
            string stored = PlayerPrefs.GetString(key, string.Empty);
            if (string.IsNullOrEmpty(stored)) return 0;
            if (DateTime.TryParse(stored, null, DateTimeStyles.RoundtripKind, out DateTime last))
                return (DateTime.UtcNow - last).TotalSeconds;
            return 0;
        }

        /// <summary>
        /// 오프라인 경과 시간을 초 단위 상한선을 적용하여 반환한다.
        /// </summary>
        /// <param name="maxSeconds">최대 인정 시간(초)</param>
        /// <param name="key">PlayerPrefs 키</param>
        /// <returns>상한 적용된 경과 시간(초)</returns>
        public static double GetOfflineSecondsClamped(double maxSeconds, string key = DefaultExitTimeKey)
            => Math.Min(GetOfflineSeconds(key), maxSeconds);

        /// <summary>
        /// 오프라인 경과 시간을 일/시/분/초 조합 상한선을 적용하여 반환한다.
        /// </summary>
        /// <param name="maxDays">최대 인정 일</param>
        /// <param name="maxHours">최대 인정 시간</param>
        /// <param name="maxMinutes">최대 인정 분</param>
        /// <param name="maxSeconds">최대 인정 초</param>
        /// <param name="key">PlayerPrefs 키</param>
        /// <returns>상한 적용된 경과 시간(초)</returns>
        public static double GetOfflineSecondsClamped(
            int maxDays, int maxHours, int maxMinutes, int maxSeconds,
            string key = DefaultExitTimeKey)
        {
            double limit = maxDays * 86400d + maxHours * 3600d + maxMinutes * 60d + maxSeconds;
            return Math.Min(GetOfflineSeconds(key), limit);
        }
        #endregion // 오프라인 경과 시간

        #region 일일 리셋
        /// <summary>
        /// 다음 자정(UTC)까지 남은 초를 반환한다.
        /// </summary>
        /// <returns>남은 초</returns>
        public static double SecondsUntilMidnightUtc()
            => (DateTime.UtcNow.Date.AddDays(1) - DateTime.UtcNow).TotalSeconds;

        /// <summary>
        /// 다음 특정 리셋 시각(UTC)까지 남은 초를 반환한다.
        /// </summary>
        /// <param name="resetHourUtc">리셋 기준 시(hour, UTC)</param>
        /// <returns>남은 초</returns>
        public static double SecondsUntilResetHourUtc(int resetHourUtc = 0)
        {
            DateTime now = DateTime.UtcNow;
            DateTime next = now.Date.AddHours(resetHourUtc);
            if (now >= next) next = next.AddDays(1);
            return (next - now).TotalSeconds;
        }

        /// <summary>
        /// 오늘(UTC) 이미 리셋되었는지 확인한다. 자정(00:00) 기준.
        /// </summary>
        /// <param name="key">PlayerPrefs 키</param>
        /// <returns>리셋 완료이면 true</returns>
        public static bool HasResetToday(string key = DefaultResetKey)
            => PlayerPrefs.GetString(key, "") == DateTime.UtcNow.ToString("yyyy-MM-dd");

        /// <summary>
        /// 커스텀 리셋 시각(UTC) 기준으로 현재 리셋 주기에서 이미 리셋되었는지 확인한다.
        /// </summary>
        /// <param name="resetHourUtc">리셋 기준 시(hour, UTC)</param>
        /// <param name="key">PlayerPrefs 키</param>
        /// <returns>리셋 완료이면 true</returns>
        public static bool HasResetToday(int resetHourUtc, string key = DefaultResetKey)
            => PlayerPrefs.GetString(key, "") == GetResetDateKey(resetHourUtc);

        /// <summary>
        /// 리셋 완료로 기록한다. 자정(00:00) 기준.
        /// </summary>
        /// <param name="key">PlayerPrefs 키</param>
        public static void MarkResetDone(string key = DefaultResetKey)
        {
            PlayerPrefs.SetString(key, DateTime.UtcNow.ToString("yyyy-MM-dd"));
            PlayerPrefs.Save();
        }

        /// <summary>
        /// 커스텀 리셋 시각(UTC) 기준으로 리셋 완료를 기록한다.
        /// </summary>
        /// <param name="resetHourUtc">리셋 기준 시(hour, UTC)</param>
        /// <param name="key">PlayerPrefs 키</param>
        public static void MarkResetDone(int resetHourUtc, string key = DefaultResetKey)
        {
            PlayerPrefs.SetString(key, GetResetDateKey(resetHourUtc));
            PlayerPrefs.Save();
        }

        /// <summary>
        /// 리셋이 필요하면 true를 반환하고 자동으로 기록을 갱신한다. 자정(00:00) 기준.
        /// </summary>
        /// <param name="key">PlayerPrefs 키</param>
        /// <returns>리셋이 수행되었으면 true</returns>
        public static bool TryReset(string key = DefaultResetKey)
        {
            if (HasResetToday(key)) return false;
            MarkResetDone(key);
            return true;
        }

        /// <summary>
        /// 커스텀 리셋 시각(UTC) 기준으로 리셋이 필요하면 true를 반환하고 자동으로 기록을 갱신한다.
        /// </summary>
        /// <param name="resetHourUtc">리셋 기준 시(hour, UTC)</param>
        /// <param name="key">PlayerPrefs 키</param>
        /// <returns>리셋이 수행되었으면 true</returns>
        public static bool TryReset(int resetHourUtc, string key = DefaultResetKey)
        {
            if (HasResetToday(resetHourUtc, key)) return false;
            MarkResetDone(resetHourUtc, key);
            return true;
        }

        /// <summary>
        /// 커스텀 리셋 시각 기준의 현재 리셋 주기 날짜 키를 반환한다.
        /// 예: resetHourUtc=4일 때, UTC 03:00이면 전날 날짜, UTC 05:00이면 오늘 날짜.
        /// </summary>
        /// <param name="resetHourUtc">리셋 기준 시(hour, UTC)</param>
        /// <returns>"yyyy-MM-dd" 형식의 리셋 주기 날짜</returns>
        private static string GetResetDateKey(int resetHourUtc)
        {
            DateTime now = DateTime.UtcNow;
            if (now.Hour < resetHourUtc)
                now = now.AddDays(-1);
            return now.ToString("yyyy-MM-dd");
        }
        #endregion // 일일 리셋

        #region 스톱워치
        /// <summary>
        /// 스톱워치를 시작(또는 재시작)한다.
        /// DebugMode가 아니면 무시된다.
        /// </summary>
        [System.Diagnostics.Conditional("DebugMode")]
        public static void StopwatchStart()
        {
            stopWatch.Restart();
        }

        /// <summary>
        /// 스톱워치를 정지하고 경과 시간(ms)을 반환한다.
        /// DebugMode가 아니면 항상 0을 반환한다.
        /// </summary>
        /// <returns>경과 시간(밀리초)</returns>
        public static double StopwatchStop()
        {
#if DebugMode
        stopWatch.Stop();
        return stopWatch.Elapsed.TotalMilliseconds;
#else
            return 0;
#endif
        }

        /// <summary>
        /// 스톱워치를 정지하지 않고 현재까지의 경과 시간(ms)을 반환한다.
        /// DebugMode가 아니면 항상 0을 반환한다.
        /// </summary>
        /// <returns>경과 시간(밀리초)</returns>
        public static double StopwatchLap()
        {
#if DebugMode
        return stopWatch.Elapsed.TotalMilliseconds;
#else
            return 0;
#endif
        }
        #endregion // 스톱워치
    }
}