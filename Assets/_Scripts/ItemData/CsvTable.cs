using System.Collections.Generic;
using System.Text;

namespace FlatVenture.ItemData
{
    // 구글 시트에서 내보낸 CSV를 읽기 위한 최소 CSV 파서입니다.
    // Unity 프로젝트 구조가 확정되기 전까지 외부 패키지 의존성을 늘리지 않으려고 로컬 구현으로 둡니다.
    public sealed class CsvTable
    {
        private readonly List<Dictionary<string, string>> rows;

        public CsvTable(List<Dictionary<string, string>> rows)
        {
            this.rows = rows;
        }

        public IReadOnlyList<Dictionary<string, string>> Rows
        {
            get { return rows; }
        }

        // CSV 원문을 헤더 기반 행 목록으로 변환합니다.
        public static CsvTable Parse(string text)
        {
            var records = ParseRecords(text);
            var rows = new List<Dictionary<string, string>>();

            if (records.Count == 0)
                return new CsvTable(rows);

            var headers = records[0];
            if (headers.Count > 0)
                headers[0] = StripBom(headers[0]);

            for (var rowIndex = 1; rowIndex < records.Count; rowIndex++)
            {
                var record = records[rowIndex];
                if (IsEmptyRecord(record))
                    continue;

                var row = new Dictionary<string, string>();
                for (var col = 0; col < headers.Count; col++)
                {
                    var value = col < record.Count ? record[col] : string.Empty;
                    row[headers[col]] = value;
                }

                rows.Add(row);
            }

            return new CsvTable(rows);
        }

        // 따옴표와 쉼표를 고려해서 CSV 원문을 레코드 단위로 분리합니다.
        private static List<List<string>> ParseRecords(string text)
        {
            var records = new List<List<string>>();
            var record = new List<string>();
            var field = new StringBuilder();
            var inQuotes = false;

            for (var i = 0; i < text.Length; i++)
            {
                var c = text[i];

                if (inQuotes)
                {
                    if (c == '"')
                    {
                        if (i + 1 < text.Length && text[i + 1] == '"')
                        {
                            field.Append('"');
                            i++;
                        }
                        else
                        {
                            inQuotes = false;
                        }
                    }
                    else
                    {
                        field.Append(c);
                    }

                    continue;
                }

                if (c == '"')
                {
                    inQuotes = true;
                }
                else if (c == ',')
                {
                    record.Add(field.ToString());
                    field.Length = 0;
                }
                else if (c == '\n')
                {
                    record.Add(field.ToString());
                    field.Length = 0;
                    records.Add(record);
                    record = new List<string>();
                }
                else if (c != '\r')
                {
                    field.Append(c);
                }
            }

            record.Add(field.ToString());
            records.Add(record);
            return records;
        }

        // 빈 줄은 데이터 행으로 취급하지 않기 위해 검사합니다.
        private static bool IsEmptyRecord(List<string> record)
        {
            for (var i = 0; i < record.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(record[i]))
                    return false;
            }

            return true;
        }

        // UTF-8 BOM이 헤더 이름에 붙는 경우를 제거합니다.
        private static string StripBom(string value)
        {
            if (!string.IsNullOrEmpty(value) && value[0] == '\uFEFF')
                return value.Substring(1);

            return value;
        }
    }
}
