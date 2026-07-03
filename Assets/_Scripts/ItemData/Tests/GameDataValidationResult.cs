using System.Collections.Generic;
using System.Text;

namespace FlatVenture.ItemData
{
    // 게임 데이터 검증 결과를 담는 리포트 모델입니다.
    // 로더, 테스트 컴포넌트, 에디터 도구가 같은 결과 형식을 재사용할 수 있게 분리합니다.
    public sealed class GameDataValidationResult
    {
        public readonly List<string> errors = new List<string>();
        public readonly List<string> warnings = new List<string>();
        public readonly Dictionary<string, int> csvRowCounts = new Dictionary<string, int>();
        public readonly Dictionary<string, int> catalogCounts = new Dictionary<string, int>();
        public readonly Dictionary<string, int> rarityItemCounts = new Dictionary<string, int>();

        public int ErrorCount
        {
            get { return errors.Count; }
        }

        public int WarningCount
        {
            get { return warnings.Count; }
        }

        public bool IsSuccess
        {
            get { return errors.Count == 0; }
        }

        // 치명적인 데이터 문제를 추가합니다.
        public void AddError(string message)
        {
            errors.Add(message);
        }

        // 실행은 가능하지만 확인이 필요한 데이터 문제를 추가합니다.
        public void AddWarning(string message)
        {
            warnings.Add(message);
        }

        // CSV별 실제 데이터 행 개수를 기록합니다.
        public void SetCsvRowCount(string csvFileName, int rowCount)
        {
            csvRowCounts[csvFileName] = rowCount;
        }

        // 카탈로그에 적재된 데이터 개수를 기록합니다.
        public void SetCatalogCount(string key, int count)
        {
            catalogCounts[key] = count;
        }

        // 희귀도별 아이템 개수를 기록합니다.
        public void SetRarityItemCount(string rarityId, int count)
        {
            rarityItemCounts[rarityId] = count;
        }

        // 다른 검증 결과를 현재 결과에 합칩니다.
        public void Merge(GameDataValidationResult other)
        {
            errors.AddRange(other.errors);
            warnings.AddRange(other.warnings);
            CopyCounts(other.csvRowCounts, csvRowCounts);
            CopyCounts(other.catalogCounts, catalogCounts);
            CopyCounts(other.rarityItemCounts, rarityItemCounts);
        }

        // 콘솔에 출력하기 좋은 요약 문자열을 만듭니다.
        public string BuildSummary()
        {
            var builder = new StringBuilder();
            builder.AppendLine("[ItemDataLoadTest] 데이터 검증 요약");
            builder.AppendLine("성공 여부: " + (IsSuccess ? "성공" : "실패"));
            builder.AppendLine("오류: " + ErrorCount + "개 / 경고: " + WarningCount + "개");

            AppendSection(builder, "CSV 행 개수", csvRowCounts);
            AppendSection(builder, "카탈로그 적재 개수", catalogCounts);
            AppendSection(builder, "희귀도별 아이템 개수", rarityItemCounts);

            return builder.ToString();
        }

        // 딕셔너리 형태의 카운트 정보를 섹션 문자열로 붙입니다.
        private static void AppendSection(StringBuilder builder, string title, Dictionary<string, int> counts)
        {
            builder.AppendLine(title + ":");

            foreach (var pair in counts)
                builder.AppendLine("- " + pair.Key + ": " + pair.Value);
        }

        // 다른 카운트 딕셔너리의 값을 대상 딕셔너리에 복사합니다.
        private static void CopyCounts(Dictionary<string, int> source, Dictionary<string, int> target)
        {
            foreach (var pair in source)
                target[pair.Key] = pair.Value;
        }
    }
}
