using System.Collections.Generic;
using System.Globalization;

namespace FlatVenture.ItemData
{
    // CSV 값 변환을 한곳에 모아 로더 코드를 읽기 쉽게 유지합니다.
    // 숫자는 지역 설정에 흔들리지 않도록 invariant culture로 파싱합니다.
    public static class CsvValue
    {
        // 문자열 값을 읽고, 없으면 기본값을 반환합니다.
        public static string String(Dictionary<string, string> row, string key, string fallback = "")
        {
            string value;
            return row.TryGetValue(key, out value) ? value : fallback;
        }

        // 정수 값을 읽고, 비어 있거나 잘못된 값이면 기본값을 반환합니다.
        public static int Int(Dictionary<string, string> row, string key, int fallback = 0)
        {
            int value;
            return int.TryParse(String(row, key), NumberStyles.Integer, CultureInfo.InvariantCulture, out value)
                ? value
                : fallback;
        }

        // 실수 값을 읽고, 비어 있거나 잘못된 값이면 기본값을 반환합니다.
        public static float Float(Dictionary<string, string> row, string key, float fallback = 0f)
        {
            float value;
            return float.TryParse(String(row, key), NumberStyles.Float, CultureInfo.InvariantCulture, out value)
                ? value
                : fallback;
        }

        // TRUE/FALSE 텍스트를 bool로 변환합니다.
        public static bool Bool(Dictionary<string, string> row, string key, bool fallback = false)
        {
            var value = String(row, key);
            if (string.IsNullOrWhiteSpace(value))
                return fallback;

            return value.Trim().ToUpperInvariant() == "TRUE";
        }
    }
}
