using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SWUtils
{
    public static class SWUtilsUtility
    {
        #region 함수
        /** 정수형 랜덤숫자를 반환한다 */
        public static int RandomInt(this int valueA, int valueB)
        {
            return UnityEngine.Random.Range(valueA, valueB);
        }

        /** 실수형 랜덤숫자를 반환한다 */
        public static float RandomFloat(this float valueA, float valueB)
        {
            return UnityEngine.Random.Range(valueA, valueB);
        }

        public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source, Int32? seed = null)
        {
            List<T> buffer = source.ToList();

            System.Random random = seed.HasValue ? new System.Random(seed.Value) : new System.Random();

            Int32 count = buffer.Count;

            for (Int32 i = 0; i < count; i++)
            {
                Int32 j = random.Next(i, count);
                yield return buffer[j];
                buffer[j] = buffer[i];
            }
        }

        public static void Shuffle<T>(List<T> list, System.Random random)
        {
            for(int i = list.Count - 1; i > 0; --i)
            {
                int next = random.Next(i + 1);
                (list[i], list[next]) = (list[next], list[i]);
            }
        }

        /** 가중치 랜덤을 반환한다 */
        public static T WeightRandom<T>(T[] items, int[] weights)
        {
            int totalWeight = 0;

            foreach (var weight in weights)
            {
                totalWeight += weight;
            }

            int randomWeight = UnityEngine.Random.Range(0, totalWeight);
            int accumulatedWeight = 0;

            for (int i = 0; i < weights.Length; i++)
            {
                accumulatedWeight += weights[i];
                if (randomWeight < accumulatedWeight)
                {
                    return items[i];
                }
            }

            return items[items.Length - 1];
        }

        /** 게이지를 설정한다 */
        public static void SetGauge(TextMeshProUGUI textMesh, Image gaugeImg, int currentValue, int maxValue)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(currentValue);
            stringBuilder.Append(" / ");
            stringBuilder.Append(maxValue);

            textMesh.text = stringBuilder.ToString();
        }

        /** 부모 객체를 지정해준다 */
        public static void SetParent(this GameObject obj, Transform targetObj, bool isResetPos = true, bool isStayWorldPos = false)
        {
            obj.transform.SetParent(targetObj, isStayWorldPos);

            if (isResetPos)
                obj.transform.localPosition = Vector3.zero;
        }
        #endregion // 함수
    }
}