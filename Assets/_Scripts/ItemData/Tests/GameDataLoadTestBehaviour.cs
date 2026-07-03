using System.IO;
using System.Text;
using UnityEngine;

namespace FlatVenture.ItemData
{
    // 씬에서 아이템 CSV 로드와 데이터 무결성을 한 번에 확인하는 테스트 컴포넌트입니다.
    // 빈 GameObject에 붙이고 Play하면 콘솔에 요약, 오류, 샘플 아이템 상세 정보가 출력됩니다.
    public sealed class GameDataLoadTestBehaviour : MonoBehaviour
    {
        [SerializeField] private bool runOnStart = true;
        [SerializeField] private bool printSummary = true;
        [SerializeField] private bool printErrorsAndWarnings = true;
        [SerializeField] private bool printSampleItems = true;
        [SerializeField] private bool printAllItems;
        [SerializeField] private string[] sampleItemIds;

        public GameDataCatalog Catalog { get; private set; }
        public GameDataValidationResult LastResult { get; private set; }

        // 씬 시작 시 설정에 따라 로드 테스트를 실행합니다.
        private void Start()
        {
            if (runOnStart)
                RunTest();
        }

        // StreamingAssets의 아이템 CSV를 로드하고 검증 결과를 콘솔에 출력합니다.
        public void RunTest()
        {
            var folder = Path.Combine(Application.streamingAssetsPath, "GameData/Items");

            var preflightResult = GameDataCatalogValidator.ValidateCsvFiles(folder);
            if (!preflightResult.IsSuccess)
            {
                LastResult = preflightResult;
                PrintResult(LastResult);
                return;
            }

            Catalog = GameDataCsvLoader.LoadFromFolder(folder);
            LastResult = GameDataCatalogValidator.Validate(folder, Catalog);

            PrintResult(LastResult);

            if (printSampleItems)
                PrintSampleItemDetails(Catalog);

            if (printAllItems)
                PrintAllItemNames(Catalog);
        }

        // 검증 결과 요약과 상세 메시지를 설정에 맞춰 출력합니다.
        private void PrintResult(GameDataValidationResult result)
        {
            if (printSummary)
                Debug.Log(result.BuildSummary());

            if (printErrorsAndWarnings)
                PrintErrorsAndWarnings(result);
        }

        // 오류와 경고를 Unity Console 레벨에 맞춰 출력합니다.
        private static void PrintErrorsAndWarnings(GameDataValidationResult result)
        {
            for (var i = 0; i < result.errors.Count; i++)
                Debug.LogError("[ItemDataLoadTest] " + result.errors[i]);

            for (var i = 0; i < result.warnings.Count; i++)
                Debug.LogWarning("[ItemDataLoadTest] " + result.warnings[i]);
        }

        // 인스펙터에 지정한 샘플 아이템들의 연결 상태를 출력합니다.
        private void PrintSampleItemDetails(GameDataCatalog catalog)
        {
            if (sampleItemIds == null || sampleItemIds.Length == 0)
            {
                Debug.Log("[ItemDataLoadTest] 샘플 아이템 ID가 지정되지 않았습니다.");
                return;
            }

            for (var i = 0; i < sampleItemIds.Length; i++)
            {
                var itemId = sampleItemIds[i];
                if (string.IsNullOrEmpty(itemId))
                    continue;

                ItemRecord item;
                if (!catalog.TryGetItem(itemId, out item))
                {
                    Debug.LogWarning("[ItemDataLoadTest] 샘플 아이템을 찾을 수 없음: " + itemId);
                    continue;
                }

                Debug.Log(BuildItemDetailText(catalog, item));
            }
        }

        // 전체 아이템 ID와 표시 이름을 출력합니다.
        private static void PrintAllItemNames(GameDataCatalog catalog)
        {
            var builder = new StringBuilder();
            builder.AppendLine("[ItemDataLoadTest] 전체 아이템 목록");

            foreach (var item in catalog.items.Values)
                builder.AppendLine("- " + item.definition.itemId + " / " + item.definition.displayName + " / " + item.definition.rarityId);

            Debug.Log(builder.ToString());
        }

        // 아이템 하나의 속성, 스탯, 효과 연결 정보를 사람이 읽기 좋은 문자열로 만듭니다.
        private static string BuildItemDetailText(GameDataCatalog catalog, ItemRecord item)
        {
            var builder = new StringBuilder();
            builder.AppendLine("[ItemDataLoadTest] 샘플 아이템 상세");
            builder.AppendLine("아이템: " + item.definition.itemId + " / " + item.definition.displayName);
            builder.AppendLine("희귀도: " + item.definition.rarityId);
            builder.AppendLine("고유 여부: " + item.definition.isUnique);

            AppendElements(builder, item);
            AppendStats(builder, item);
            AppendEffects(builder, catalog, item);

            return builder.ToString();
        }

        // 아이템 속성 목록을 상세 문자열에 추가합니다.
        private static void AppendElements(StringBuilder builder, ItemRecord item)
        {
            builder.AppendLine("속성:");
            for (var i = 0; i < item.elements.Count; i++)
            {
                var element = item.elements[i];
                builder.AppendLine("- " + element.elementId + ": " + element.elementValue);
            }
        }

        // 아이템 스탯 목록을 상세 문자열에 추가합니다.
        private static void AppendStats(StringBuilder builder, ItemRecord item)
        {
            builder.AppendLine("스탯:");
            for (var i = 0; i < item.stats.Count; i++)
            {
                var stat = item.stats[i];
                builder.AppendLine("- " + stat.statId + " / " + stat.operation + " / " + stat.value + " / 조건: " + stat.conditionId);
            }
        }

        // 아이템 효과와 effect_parameter 목록을 상세 문자열에 추가합니다.
        private static void AppendEffects(StringBuilder builder, GameDataCatalog catalog, ItemRecord item)
        {
            builder.AppendLine("효과:");
            for (var i = 0; i < item.effects.Count; i++)
            {
                var effect = item.effects[i];
                builder.AppendLine("- " + effect.effectId + " / " + effect.triggerId + " -> " + effect.actionId);
                builder.AppendLine("  condition: " + effect.conditionId);
                builder.AppendLine("  projectile: " + effect.projectileId + ", status: " + effect.statusId);

                var parameters = catalog.GetEffectParameters(effect.effectId);
                for (var parameterIndex = 0; parameterIndex < parameters.Count; parameterIndex++)
                {
                    var parameter = parameters[parameterIndex];
                    builder.AppendLine("  param " + parameter.paramId + ": " + parameter.value);
                }
            }
        }
    }
}
