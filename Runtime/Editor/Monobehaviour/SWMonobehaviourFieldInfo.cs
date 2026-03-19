using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

using Object = UnityEngine.Object;

namespace SWTools
{
    /// <summary>
    /// Unity 오브젝트의 필드 정보(fieldInfo)를 수집하고 캐싱하는 유틸리티 클래스
    /// </summary>
    public static class SWMonobehaviourFieldInfo
    {
        #region 필드
        /// <summary>
        /// Key - 타입의 Hash값
        /// Value - 해당 타입의 모든 필드 정보 리스트
        /// 오브젝트의 모든 필드 정보를 가져옵니다
        /// </summary>
        public static Dictionary<int, List<FieldInfo>> FieldInfoLDict = new();
        #endregion // 필드

        #region 프로퍼티
        #endregion // 프로퍼티

        /// <summary>
        /// 오브젝트의 모든 필드 정보를 가져옵니다
        /// </summary>
        /// <param name="target"></param>
        /// <param name="fieldInfoList"></param>
        /// <returns>필드의 총 개수</returns>
        public static int GetFieldInfo(Object target, out List<FieldInfo> fieldInfoList)
        {
            Type targetType = target.GetType();
            int targetTypeHashCode = targetType.GetHashCode();

            if (!FieldInfoLDict.TryGetValue(targetTypeHashCode, out fieldInfoList))
            {
                IList<Type> typeTree = targetType.GetBaseTypes();

                // 모든 필드 정보 수집
                // 상속 계층 순서대로 정렬
                // 부모일수록 앞으로 정렬 
                fieldInfoList = target.GetType().GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                .OrderByDescending(x => typeTree.IndexOf(x.DeclaringType))
                .ToList();

                FieldInfoLDict.Add(targetTypeHashCode, fieldInfoList);
            }

            return fieldInfoList.Count;
        }
        
        /// <summary>
        /// 지정된 타입의 모든 부모 타입을 리스트로 반환합니다
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static IList<Type> GetBaseTypes(this Type type)
        {
            var types = new List<Type>();

            while (type.BaseType != null)
            {
                types.Add(type); // 현재 타입 추가
                type = type.BaseType; // 부모 타입으로 이동
            }

            return types;
        }
    }
}
