using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SWUtils
{
    /// <summary>
    /// 랜덤, 셔플, 가중치 선택, UI 갱신 등 범용 유틸리티 함수 모음.
    /// </summary>
    public static class SWUtilsUtility
    {
        #region 함수
        /// <summary>
        /// 정수형 랜덤 숫자를 반환한다.
        /// </summary>
        /// <param name="valueA">최솟값(포함)</param>
        /// <param name="valueB">최댓값(제외)</param>
        /// <returns>랜덤 정수</returns>
        public static int RandomInt(this int valueA, int valueB)
        {
            return UnityEngine.Random.Range(valueA, valueB);
        }

        /// <summary>
        /// 실수형 랜덤 숫자를 반환한다.
        /// </summary>
        /// <param name="valueA">최솟값(포함)</param>
        /// <param name="valueB">최댓값(포함)</param>
        /// <returns>랜덤 실수</returns>
        public static float RandomFloat(this float valueA, float valueB)
        {
            return UnityEngine.Random.Range(valueA, valueB);
        }

        /// <summary>
        /// 소스 컬렉션을 섞어서 반환한다. (Fisher-Yates 셔플)
        /// </summary>
        /// <typeparam name="T">요소 타입</typeparam>
        /// <param name="source">셔플할 원본 컬렉션</param>
        /// <param name="seed">랜덤 시드. null이면 시간 기반</param>
        /// <returns>셔플된 요소 시퀀스</returns>
        public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source, Int32? seed = null)
        {
            List<T> buffer = source.ToList();

            System.Random random = seed.HasValue ? new System.Random(seed.Value) : new System.Random();

            Int32 count = buffer.Count;

            for (Int32 index = 0; index < count; index++)
            {
                Int32 swapIndex = random.Next(index, count);
                yield return buffer[swapIndex];
                buffer[swapIndex] = buffer[index];
            }
        }

        /// <summary>
        /// 리스트를 제자리에서 섞는다. (Fisher-Yates 셔플)
        /// </summary>
        /// <typeparam name="T">요소 타입</typeparam>
        /// <param name="list">셔플할 리스트</param>
        /// <param name="random">사용할 Random 인스턴스</param>
        public static void Shuffle<T>(List<T> list, System.Random random)
        {
            for (int index = list.Count - 1; index > 0; --index)
            {
                int next = random.Next(index + 1);
                (list[index], list[next]) = (list[next], list[index]);
            }
        }

        /// <summary>
        /// 가중치 기반으로 랜덤 요소를 반환한다.
        /// </summary>
        /// <typeparam name="T">요소 타입</typeparam>
        /// <param name="items">선택할 요소 배열</param>
        /// <param name="weights">각 요소의 가중치 배열</param>
        /// <returns>가중치에 따라 선택된 요소</returns>
        public static T WeightRandom<T>(T[] items, int[] weights)
        {
            int totalWeight = 0;

            foreach (var weight in weights)
            {
                totalWeight += weight;
            }

            int randomWeight = UnityEngine.Random.Range(0, totalWeight);
            int accumulatedWeight = 0;

            for (int index = 0; index < weights.Length; index++)
            {
                accumulatedWeight += weights[index];
                if (randomWeight < accumulatedWeight)
                {
                    return items[index];
                }
            }

            return items[items.Length - 1];
        }

        /// <summary>
        /// 게이지 텍스트를 설정한다. ("현재값 / 최대값" 형식)
        /// </summary>
        /// <param name="textMesh">값을 표시할 TextMeshProUGUI</param>
        /// <param name="gaugeImage">게이지 이미지 (현재 미사용, 향후 확장용)</param>
        /// <param name="currentValue">현재 값</param>
        /// <param name="maxValue">최대 값</param>
        public static void SetGauge(TextMeshProUGUI textMesh, Image gaugeImage, int currentValue, int maxValue)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(currentValue);
            stringBuilder.Append(" / ");
            stringBuilder.Append(maxValue);

            textMesh.text = stringBuilder.ToString();
        }

        /// <summary>
        /// 부모 객체를 지정한다.
        /// </summary>
        /// <param name="gameObject">자식으로 설정할 객체</param>
        /// <param name="targetObject">부모가 될 Transform</param>
        /// <param name="isResetPosition">true면 로컬 위치를 (0,0,0)으로 초기화</param>
        /// <param name="isStayWorldPosition">true면 월드 좌표 유지</param>
        public static void SetParent(this GameObject gameObject, Transform targetObject, bool isResetPosition = true, bool isStayWorldPosition = false)
        {
            gameObject.transform.SetParent(targetObject, isStayWorldPosition);

            if (isResetPosition)
                gameObject.transform.localPosition = Vector3.zero;
        }
        #endregion // 함수
    }
}