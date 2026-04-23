using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using SWUtils;
using UnityEngine;

namespace SWTools
{
    /// <summary>
    /// 엑셀에서 복사한 TSV 데이터를 ScriptableObject 필드에 매핑하는 파서.
    /// </summary>
    public static class SWExcelTableParser
    {
        #region 클래스
        public class ParseResult
        {
            public readonly List<string> Headers = new();
            public readonly List<Dictionary<string, string>> Rows = new();
            public readonly List<string> Warnings = new();
            public readonly List<string> Errors = new();
        }

        public class SheetFieldInfo
        {
            public FieldInfo Field;
            public string SheetName;
            public Type ElementType;
            public bool IsArray;
        }
        #endregion // 클래스

        #region 파싱
        /// <summary>
        /// TSV 문자열을 헤더와 행 데이터로 파싱한다.
        /// </summary>
        /// <param name="tableText">엑셀에서 복사한 TSV 문자열.</param>
        /// <returns>파싱 결과.</returns>
        public static ParseResult Parse(string tableText)
        {
            ParseResult result = new();
            if (string.IsNullOrWhiteSpace(tableText))
            {
                result.Errors.Add("입력된 TSV 데이터가 비어 있습니다.");
                return result;
            }

            string normalized = tableText.Replace("\r\n", "\n").Replace('\r', '\n').Trim();
            string[] lines = normalized.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length < 2)
            {
                result.Errors.Add("헤더와 데이터 행이 모두 필요합니다.");
                return result;
            }

            string[] headers = SplitLine(lines[0]);
            for (int index = 0; index < headers.Length; index++)
            {
                string header = headers[index].Trim();
                if (string.IsNullOrEmpty(header))
                    result.Warnings.Add($"{index + 1}번째 헤더가 비어 있습니다.");
                result.Headers.Add(header);
            }

            for (int lineIndex = 1; lineIndex < lines.Length; lineIndex++)
            {
                string[] columns = SplitLine(lines[lineIndex]);
                Dictionary<string, string> row = new(StringComparer.OrdinalIgnoreCase);

                for (int columnIndex = 0; columnIndex < result.Headers.Count; columnIndex++)
                {
                    string header = result.Headers[columnIndex];
                    if (string.IsNullOrEmpty(header)) continue;

                    string value = columnIndex < columns.Length ? columns[columnIndex].Trim() : string.Empty;
                    row[header] = value;
                }

                if (columns.Length != result.Headers.Count)
                    result.Warnings.Add($"{lineIndex + 1}번째 줄의 컬럼 수가 헤더 수와 다릅니다. Header: {result.Headers.Count}, Row: {columns.Length}");

                result.Rows.Add(row);
            }

            return result;
        }

        /// <summary>
        /// 한 줄을 컬럼 배열로 분리한다. 탭을 우선 사용하고, 탭이 없으면 연속 공백을 보조로 사용한다.
        /// </summary>
        /// <param name="line">분리할 줄.</param>
        /// <returns>컬럼 배열.</returns>
        private static string[] SplitLine(string line)
        {
            if (line.Contains("\t"))
                return line.Split('\t');

            return line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        }
        #endregion // 파싱

        #region 시트 필드
        /// <summary>
        /// ScriptableObject에서 SWTableSheetAttribute가 붙은 리스트/배열 필드를 찾는다.
        /// </summary>
        /// <param name="target">대상 ScriptableObject.</param>
        /// <returns>시트 필드 목록.</returns>
        public static List<SheetFieldInfo> GetSheetFields(ScriptableObject target)
        {
            List<SheetFieldInfo> result = new();
            if (target == null) return result;

            FieldInfo[] fields = target.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (FieldInfo field in fields)
            {
                SWTableSheetAttribute sheetAttribute = field.GetCustomAttribute<SWTableSheetAttribute>();
                if (sheetAttribute == null) continue;

                if (TryGetElementType(field.FieldType, out Type elementType, out bool isArray))
                {
                    result.Add(new SheetFieldInfo
                    {
                        Field = field,
                        SheetName = sheetAttribute.SheetName,
                        ElementType = elementType,
                        IsArray = isArray,
                    });
                }
                else
                {
                    SWUtilsLog.LogWarning($"[SWExcelTableParser] 지원하지 않는 시트 필드 타입입니다. Field: {field.Name}");
                }
            }

            return result;
        }

        /// <summary>
        /// 리스트/배열 필드의 요소 타입을 가져온다.
        /// </summary>
        private static bool TryGetElementType(Type fieldType, out Type elementType, out bool isArray)
        {
            elementType = null;
            isArray = false;

            if (fieldType.IsArray)
            {
                elementType = fieldType.GetElementType();
                isArray = true;
                return elementType != null;
            }

            if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(List<>))
            {
                elementType = fieldType.GetGenericArguments()[0];
                return true;
            }

            return false;
        }
        #endregion // 시트 필드

        #region 적용
        /// <summary>
        /// 파싱된 데이터를 지정한 시트 필드에 적용한다.
        /// </summary>
        /// <param name="target">대상 ScriptableObject.</param>
        /// <param name="sheetField">적용할 시트 필드.</param>
        /// <param name="parseResult">파싱 결과.</param>
        /// <returns>성공 여부.</returns>
        public static bool ApplyToSheet(ScriptableObject target, SheetFieldInfo sheetField, ParseResult parseResult)
        {
            if (target == null || sheetField == null || parseResult == null) return false;
            if (parseResult.Errors.Count > 0) return false;

            List<object> values = new();
            FieldInfo[] dataFields = sheetField.ElementType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            List<(FieldInfo field, SWTableAttribute attribute)> mappedFields = new();

            foreach (FieldInfo field in dataFields)
            {
                SWTableAttribute attribute = field.GetCustomAttribute<SWTableAttribute>();
                if (attribute != null)
                    mappedFields.Add((field, attribute));
            }

            if (mappedFields.Count == 0)
            {
                parseResult.Errors.Add($"{sheetField.ElementType.Name} 타입에 [SWTable] 필드가 없습니다.");
                return false;
            }

            for (int rowIndex = 0; rowIndex < parseResult.Rows.Count; rowIndex++)
            {
                object rowObject = Activator.CreateInstance(sheetField.ElementType);
                Dictionary<string, string> row = parseResult.Rows[rowIndex];

                foreach (var mappedField in mappedFields)
                {
                    string columnName = mappedField.attribute.ColumnName;
                    if (!row.TryGetValue(columnName, out string rawValue))
                    {
                        parseResult.Warnings.Add($"{rowIndex + 2}번째 줄: '{columnName}' 컬럼을 찾을 수 없습니다.");
                        continue;
                    }

                    if (mappedField.attribute.Required && string.IsNullOrEmpty(rawValue))
                    {
                        parseResult.Errors.Add($"{rowIndex + 2}번째 줄: 필수 컬럼 '{columnName}' 값이 비어 있습니다.");
                        continue;
                    }

                    if (TryConvertValue(rawValue, mappedField.field.FieldType, out object convertedValue))
                    {
                        mappedField.field.SetValue(rowObject, convertedValue);
                    }
                    else
                    {
                        parseResult.Errors.Add($"{rowIndex + 2}번째 줄: '{columnName}' 값을 {mappedField.field.FieldType.Name} 타입으로 변환할 수 없습니다. Value: {rawValue}");
                    }
                }

                values.Add(rowObject);
            }

            if (parseResult.Errors.Count > 0)
                return false;

            if (sheetField.IsArray)
            {
                Array array = Array.CreateInstance(sheetField.ElementType, values.Count);
                for (int index = 0; index < values.Count; index++)
                    array.SetValue(values[index], index);
                sheetField.Field.SetValue(target, array);
            }
            else
            {
                object list = Activator.CreateInstance(typeof(List<>).MakeGenericType(sheetField.ElementType));
                System.Collections.IList typedList = (System.Collections.IList)list;
                foreach (object value in values)
                    typedList.Add(value);
                sheetField.Field.SetValue(target, list);
            }

            SWUtilsLog.Log($"[SWExcelTableParser] Apply complete. Sheet: {sheetField.SheetName}, Count: {values.Count}");
            return true;
        }

        /// <summary>
        /// 문자열 값을 대상 타입으로 변환한다.
        /// </summary>
        private static bool TryConvertValue(string rawValue, Type targetType, out object value)
        {
            value = null;
            string source = rawValue?.Trim() ?? string.Empty;

            if (targetType == typeof(string))
            {
                value = source;
                return true;
            }

            if (string.IsNullOrEmpty(source))
            {
                value = targetType.IsValueType ? Activator.CreateInstance(targetType) : null;
                return true;
            }

            Type nullableType = Nullable.GetUnderlyingType(targetType);
            if (nullableType != null)
                targetType = nullableType;

            try
            {
                if (targetType == typeof(int))
                {
                    value = int.Parse(source, NumberStyles.Integer, CultureInfo.InvariantCulture);
                    return true;
                }

                if (targetType == typeof(long))
                {
                    value = long.Parse(source, NumberStyles.Integer, CultureInfo.InvariantCulture);
                    return true;
                }

                if (targetType == typeof(float))
                {
                    value = float.Parse(source, NumberStyles.Float, CultureInfo.InvariantCulture);
                    return true;
                }

                if (targetType == typeof(double))
                {
                    value = double.Parse(source, NumberStyles.Float, CultureInfo.InvariantCulture);
                    return true;
                }

                if (targetType == typeof(bool))
                {
                    value = ParseBool(source);
                    return true;
                }

                if (targetType.IsEnum)
                {
                    value = Enum.Parse(targetType, source, true);
                    return true;
                }
            }
            catch
            {
                return false;
            }

            return false;
        }

        /// <summary>
        /// 문자열을 bool 값으로 변환한다.
        /// </summary>
        private static bool ParseBool(string source)
        {
            return source.Equals("true", StringComparison.OrdinalIgnoreCase)
                || source.Equals("yes", StringComparison.OrdinalIgnoreCase)
                || source.Equals("y", StringComparison.OrdinalIgnoreCase)
                || source == "1";
        }
        #endregion // 적용
    }
}
