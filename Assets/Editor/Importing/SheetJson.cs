using System;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

/// <summary>시트의 빈 숫자/불리언 셀을 정규화하고 잘못된 값은 행·열과 함께 거부한다.</summary>
public static class SheetJson
{
    public static T[] Read<T>(string json, bool requireAllColumns = false)
    {
        JArray rows;
        try { rows = JArray.Parse(json ?? ""); }
        catch (Exception ex) { throw new InvalidOperationException(typeof(T).Name + ": JSON 배열 응답이 아닙니다. 시트 이름/Apps Script 배포를 확인하세요.", ex); }
        var fields = typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public);
        var output = new T[rows.Count];
        for (int i = 0; i < rows.Count; i++)
        {
            if (!(rows[i] is JObject row)) throw new InvalidOperationException(typeof(T).Name + ": 객체가 아닌 행 " + (i + 2));
            var normalized = new JObject();
            foreach (var field in fields)
            {
                var token = row[field.Name];
                if (requireAllColumns && token == null)
                    throw new InvalidOperationException(typeof(T).Name + ": 필수 열 누락 " + field.Name + ". 헤더 행과 Apps Script 응답을 확인하세요.");
                string value = token == null || token.Type == JTokenType.Null ? "" : token.ToString();
                string context = typeof(T).Name + " " + (i + 2) + "행 " + field.Name;
                if (field.FieldType == typeof(string))
                {
                    if (token != null && token.Type != JTokenType.Null && !(token is JValue)) throw new InvalidOperationException(context + ": 문자열 필요");
                    normalized[field.Name] = value;
                }
                else if (field.FieldType == typeof(int))
                {
                    if (string.IsNullOrWhiteSpace(value)) normalized[field.Name] = 0;
                    else if (decimal.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal n) && n == decimal.Truncate(n) && n >= int.MinValue && n <= int.MaxValue) normalized[field.Name] = (int)n;
                    else throw new InvalidOperationException(context + ": 정수 필요 (입력: " + value + ")");
                }
                else if (field.FieldType == typeof(float))
                {
                    if (string.IsNullOrWhiteSpace(value)) normalized[field.Name] = 0f;
                    else if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float n) && !float.IsNaN(n) && !float.IsInfinity(n)) normalized[field.Name] = n;
                    else throw new InvalidOperationException(context + ": 유한한 숫자 필요");
                }
                else if (field.FieldType == typeof(bool))
                {
                    if (string.IsNullOrWhiteSpace(value)) normalized[field.Name] = false;
                    else if (bool.TryParse(value, out bool b)) normalized[field.Name] = b;
                    else throw new InvalidOperationException(context + ": TRUE 또는 FALSE 필요");
                }
                else throw new InvalidOperationException(context + ": 지원하지 않는 열 자료형");
            }
            output[i] = JsonConvert.DeserializeObject<T>(normalized.ToString(Formatting.None));
        }
        return output;
    }

    // 기존 셀의 여러 역슬래시+n도 실제 줄바꿈으로 읽는다. 원격 셀은 변경하지 않는다.
    public static string Multiline(string value) => Regex.Replace(value ?? "", @"\\+n", "\n").Replace("\r\n", "\n");
}
