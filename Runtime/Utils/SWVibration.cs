using UnityEngine;

namespace SWUtils
{
    /// <summary>
    /// 모바일 진동을 플랫폼별로 처리하는 유틸리티입니다.
    /// Android에서는 지원 가능한 경우 지속 시간, 패턴, 세기를 지정할 수 있으며
    /// 그 외 플랫폼에서는 Unity 기본 진동으로 대체합니다.
    /// </summary>
    public static class SWVibration
    {
        /// <summary>
        /// 자주 사용하는 진동 패턴 프리셋입니다.
        /// </summary>
        public enum Preset
        {
            /// <summary>짧고 약한 진동입니다.</summary>
            Light,
            /// <summary>기본 세기의 짧은 진동입니다.</summary>
            Medium,
            /// <summary>강한 단일 진동입니다.</summary>
            Heavy
        }

        #region 필드
        private const string EnabledPrefsKey = "SWVibration_Enabled";
        private const int DefaultAmplitude = -1;

        private static bool isEnabled = true;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>진동 사용 여부입니다.</summary>
        public static bool IsEnabled => isEnabled;

        /// <summary>현재 플랫폼에서 진동 요청을 처리할 수 있는지 여부입니다.</summary>
        public static bool CanVibrate
        {
            get
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                return HasAndroidVibrator();
#elif UNITY_IOS && !UNITY_EDITOR
                return true;
#else
                return false;
#endif
            }
        }

        /// <summary>현재 플랫폼에서 지속 시간 또는 패턴 기반 진동을 직접 지원하는지 여부입니다.</summary>
        public static bool SupportsCustomVibration
        {
            get
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                return HasAndroidVibrator();
#else
                return false;
#endif
            }
        }
        #endregion // 프로퍼티

        #region 설정
        /// <summary>
        /// 런타임에서 진동 사용 여부를 설정합니다.
        /// </summary>
        /// <param name="enabled">진동을 사용할지 여부입니다.</param>
        public static void SetEnabled(bool enabled)
        {
            isEnabled = enabled;
        }

        /// <summary>
        /// 진동 사용 여부를 SWUtilsPlayerPrefs에 저장합니다.
        /// </summary>
        /// <param name="key">저장에 사용할 키입니다.</param>
        public static void SaveEnabled(string key = EnabledPrefsKey)
        {
            SWUtilsPlayerPrefs.SetBool(key, isEnabled);
            SWUtilsPlayerPrefs.Save();
        }

        /// <summary>
        /// SWUtilsPlayerPrefs에 저장된 진동 사용 여부를 불러옵니다.
        /// </summary>
        /// <param name="key">불러올 때 사용할 키입니다.</param>
        /// <param name="defaultValue">저장된 값이 없을 때 사용할 기본값입니다.</param>
        public static void LoadEnabled(string key = EnabledPrefsKey, bool defaultValue = true)
        {
            isEnabled = SWUtilsPlayerPrefs.GetBool(key, defaultValue);
        }
        #endregion // 설정

        #region 실행
        /// <summary>
        /// 플랫폼 기본 진동을 실행합니다.
        /// </summary>
        public static void Vibrate()
        {
            if (!CanPlay()) return;

#if UNITY_ANDROID && !UNITY_EDITOR
            VibrateAndroid(100, DefaultAmplitude);
#else
            Handheld.Vibrate();
#endif
        }

        /// <summary>
        /// 지정한 시간만큼 진동을 실행합니다.
        /// </summary>
        /// <param name="milliseconds">진동 지속 시간입니다. 단위는 밀리초입니다.</param>
        /// <param name="amplitude">진동 세기입니다. Android API 26 이상에서 1~255 값을 사용하며, -1이면 시스템 기본 세기를 사용합니다.</param>
        public static void Vibrate(long milliseconds, int amplitude = DefaultAmplitude)
        {
            if (!CanPlay()) return;

            long safeMilliseconds = milliseconds < 1 ? 1 : milliseconds;

#if UNITY_ANDROID && !UNITY_EDITOR
            VibrateAndroid(safeMilliseconds, amplitude);
#else
            Handheld.Vibrate();
#endif
        }

        /// <summary>
        /// 지정한 프리셋에 맞는 진동을 실행합니다.
        /// </summary>
        /// <param name="preset">실행할 진동 프리셋입니다.</param>
        public static void Play(Preset preset)
        {
            switch (preset)
            {
                case Preset.Light:
                    Light();
                    break;
                case Preset.Medium:
                    Medium();
                    break;
                case Preset.Heavy:
                    Heavy();
                    break;
                default:
                    Vibrate();
                    break;
            }
        }

        /// <summary>
        /// 지정한 프리셋에 맞는 진동을 실행합니다.
        /// Play와 동일하며 호출부에서 Vibrate 이름으로 사용할 때 편리합니다.
        /// </summary>
        /// <param name="preset">실행할 진동 프리셋입니다.</param>
        public static void Vibrate(Preset preset)
        {
            Play(preset);
        }
        #endregion // 실행

        #region 프리셋
        /// <summary>
        /// 약한 단일 진동을 실행합니다.
        /// </summary>
        public static void Light()
        {
            Vibrate(35, 80);
        }

        /// <summary>
        /// 중간 세기의 단일 진동을 실행합니다.
        /// </summary>
        public static void Medium()
        {
            Vibrate(60, 150);
        }

        /// <summary>
        /// 강한 단일 진동을 실행합니다.
        /// </summary>
        public static void Heavy()
        {
            Vibrate(90, 255);
        }

        #endregion // 프리셋

        #region 패턴
        /// <summary>
        /// 진동 패턴을 실행합니다.
        /// timings 값은 대기 시간과 진동 시간을 번갈아 의미합니다.
        /// </summary>
        /// <param name="timings">대기 시간과 진동 시간 배열입니다. 단위는 밀리초입니다.</param>
        /// <param name="repeat">반복을 시작할 인덱스입니다. -1이면 반복하지 않습니다.</param>
        public static void Pattern(long[] timings, int repeat = -1)
        {
            if (!CanPlay() || !IsValidPattern(timings)) return;

#if UNITY_ANDROID && !UNITY_EDITOR
            PatternAndroid(timings, null, repeat);
#else
            Handheld.Vibrate();
#endif
        }

        /// <summary>
        /// 세기 값을 포함한 진동 패턴을 실행합니다.
        /// Android API 26 이상에서 amplitudes 값을 사용합니다.
        /// </summary>
        /// <param name="timings">대기 시간과 진동 시간 배열입니다. 단위는 밀리초입니다.</param>
        /// <param name="amplitudes">각 구간의 진동 세기 배열입니다. 0은 꺼짐, 1~255는 진동 세기입니다.</param>
        /// <param name="repeat">반복을 시작할 인덱스입니다. -1이면 반복하지 않습니다.</param>
        public static void Pattern(long[] timings, int[] amplitudes, int repeat = -1)
        {
            if (!CanPlay() || !IsValidPattern(timings)) return;

#if UNITY_ANDROID && !UNITY_EDITOR
            PatternAndroid(timings, amplitudes, repeat);
#else
            Handheld.Vibrate();
#endif
        }
        #endregion // 패턴

        #region 제어
        /// <summary>
        /// 현재 실행 중인 진동을 중지합니다.
        /// 지원하지 않는 플랫폼에서는 아무 동작도 하지 않습니다.
        /// </summary>
        public static void Cancel()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            AndroidJavaObject vibrator = GetAndroidVibrator();
            vibrator?.Call("cancel");
#endif
        }
        #endregion // 제어

        #region 내부
        /// <summary>
        /// 현재 설정과 플랫폼 상태를 기준으로 진동 실행 가능 여부를 확인합니다.
        /// </summary>
        private static bool CanPlay()
        {
            if (!isEnabled) return false;

#if UNITY_EDITOR
            return false;
#else
            return CanVibrate;
#endif
        }

        /// <summary>
        /// 진동 패턴 배열이 유효한지 확인합니다.
        /// </summary>
        /// <param name="timings">확인할 진동 패턴 배열입니다.</param>
        /// <returns>패턴을 실행할 수 있으면 true입니다.</returns>
        private static bool IsValidPattern(long[] timings)
        {
            return timings != null && timings.Length > 0;
        }
        #endregion // 내부

#if UNITY_ANDROID && !UNITY_EDITOR
        #region Android
        /// <summary>
        /// Android Vibrator 서비스를 가져옵니다.
        /// </summary>
        /// <returns>Android Vibrator 객체입니다.</returns>
        private static AndroidJavaObject GetAndroidVibrator()
        {
            try
            {
                using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    return activity?.Call<AndroidJavaObject>("getSystemService", "vibrator");
                }
            }
            catch (System.Exception exception)
            {
                SWUtilsLog.LogWarning($"[SWVibration] Failed to get Android vibrator: {exception.Message}");
                return null;
            }
        }

        /// <summary>
        /// Android 기기에서 진동 장치를 사용할 수 있는지 확인합니다.
        /// </summary>
        /// <returns>진동 장치가 있으면 true입니다.</returns>
        private static bool HasAndroidVibrator()
        {
            try
            {
                AndroidJavaObject vibrator = GetAndroidVibrator();
                return vibrator != null && vibrator.Call<bool>("hasVibrator");
            }
            catch (System.Exception exception)
            {
                SWUtilsLog.LogWarning($"[SWVibration] Failed to check Android vibrator: {exception.Message}");
                return false;
            }
        }

        /// <summary>
        /// Android에서 단일 진동을 실행합니다.
        /// </summary>
        /// <param name="milliseconds">진동 지속 시간입니다. 단위는 밀리초입니다.</param>
        /// <param name="amplitude">진동 세기입니다.</param>
        private static void VibrateAndroid(long milliseconds, int amplitude)
        {
            try
            {
                AndroidJavaObject vibrator = GetAndroidVibrator();
                if (vibrator == null) return;

                if (GetAndroidSdkVersion() >= 26)
                {
                    using (AndroidJavaClass vibrationEffect = new AndroidJavaClass("android.os.VibrationEffect"))
                    {
                        int safeAmplitude = NormalizeAmplitude(amplitude);
                        using (AndroidJavaObject effect = vibrationEffect.CallStatic<AndroidJavaObject>(
                            "createOneShot",
                            milliseconds,
                            safeAmplitude))
                        {
                            vibrator.Call("vibrate", effect);
                        }
                    }
                    return;
                }

                vibrator.Call("vibrate", milliseconds);
            }
            catch (System.Exception exception)
            {
                SWUtilsLog.LogWarning($"[SWVibration] Android vibration failed: {exception.Message}");
            }
        }

        /// <summary>
        /// Android에서 패턴 진동을 실행합니다.
        /// </summary>
        /// <param name="timings">대기 시간과 진동 시간 배열입니다.</param>
        /// <param name="amplitudes">각 구간의 진동 세기 배열입니다.</param>
        /// <param name="repeat">반복을 시작할 인덱스입니다.</param>
        private static void PatternAndroid(long[] timings, int[] amplitudes, int repeat)
        {
            try
            {
                AndroidJavaObject vibrator = GetAndroidVibrator();
                if (vibrator == null) return;

                int safeRepeat = Mathf.Clamp(repeat, -1, timings.Length - 1);

                if (GetAndroidSdkVersion() >= 26)
                {
                    using (AndroidJavaClass vibrationEffect = new AndroidJavaClass("android.os.VibrationEffect"))
                    using (AndroidJavaObject effect = amplitudes != null && amplitudes.Length == timings.Length
                        ? vibrationEffect.CallStatic<AndroidJavaObject>("createWaveform", timings, NormalizeAmplitudes(amplitudes), safeRepeat)
                        : vibrationEffect.CallStatic<AndroidJavaObject>("createWaveform", timings, safeRepeat))
                    {
                        vibrator.Call("vibrate", effect);
                    }
                    return;
                }

                vibrator.Call("vibrate", timings, safeRepeat);
            }
            catch (System.Exception exception)
            {
                SWUtilsLog.LogWarning($"[SWVibration] Android pattern failed: {exception.Message}");
            }
        }

        /// <summary>
        /// Android 진동 세기 값을 유효 범위로 보정합니다.
        /// </summary>
        /// <param name="amplitude">보정할 진동 세기입니다.</param>
        /// <returns>Android에 전달할 진동 세기입니다.</returns>
        private static int NormalizeAmplitude(int amplitude)
        {
            if (amplitude < 0) return DefaultAmplitude;
            return Mathf.Clamp(amplitude, 1, 255);
        }

        /// <summary>
        /// Android 진동 세기 배열을 유효 범위로 보정합니다.
        /// </summary>
        /// <param name="amplitudes">보정할 진동 세기 배열입니다.</param>
        /// <returns>보정된 진동 세기 배열입니다.</returns>
        private static int[] NormalizeAmplitudes(int[] amplitudes)
        {
            int[] normalized = new int[amplitudes.Length];
            for (int index = 0; index < amplitudes.Length; index++)
            {
                int amplitude = amplitudes[index];
                normalized[index] = amplitude <= 0 ? 0 : Mathf.Clamp(amplitude, 1, 255);
            }
            return normalized;
        }

        /// <summary>
        /// 현재 Android SDK 버전을 가져옵니다.
        /// </summary>
        /// <returns>Android SDK 버전입니다.</returns>
        private static int GetAndroidSdkVersion()
        {
            using (AndroidJavaClass version = new AndroidJavaClass("android.os.Build$VERSION"))
            {
                return version.GetStatic<int>("SDK_INT");
            }
        }
        #endregion // Android
#endif
    }
}
