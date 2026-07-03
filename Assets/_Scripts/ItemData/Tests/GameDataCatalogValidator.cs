using System.Collections.Generic;
using System.IO;

namespace FlatVenture.ItemData
{
    // CSV 파일 세트와 로드된 카탈로그의 무결성을 검사하는 검증기입니다.
    // 실제 게임 로직과 분리해서 테스트, 에디터 메뉴, 디버그 패널에서 재사용할 수 있게 둡니다.
    public static class GameDataCatalogValidator
    {
        private static readonly CsvRequirement[] RequiredCsvFiles =
        {
            new CsvRequirement("item_definitions.csv", new[] { "item_id", "display_name", "rarity", "is_cursed", "is_unique" }),
            new CsvRequirement("item_elements.csv", new[] { "item_id", "element_id", "element_value" }),
            new CsvRequirement("item_stats.csv", new[] { "item_id", "stat_id", "operation", "value" }),
            new CsvRequirement("item_effects.csv", new[] { "item_id", "effect_id", "trigger_id", "action_id" }),
            new CsvRequirement("effect_parameters.csv", new[] { "effect_id", "param_id", "value" }),
            new CsvRequirement("projectiles.csv", new[] { "projectile_id", "display_name", "on_hit_action_id", "next_projectile_id" }),
            new CsvRequirement("status_effects.csv", new[] { "status_id", "display_name" }),
            new CsvRequirement("elements.csv", new[] { "element_id", "display_name", "advanced_element_id" }),
            new CsvRequirement("stat_definitions.csv", new[] { "stat_id", "display_name" }),
            new CsvRequirement("condition_definitions.csv", new[] { "condition_id", "condition_type" }),
            new CsvRequirement("trigger_definitions.csv", new[] { "trigger_id", "display_name" }),
            new CsvRequirement("action_definitions.csv", new[] { "action_id", "display_name", "required_ref" }),
            new CsvRequirement("rarity_definitions.csv", new[] { "rarity_id", "display_name", "sort_order" }),
            new CsvRequirement("rarity_weight_tables.csv", new[] { "table_id", "rarity_id", "weight" })
        };

        // CSV 파일과 카탈로그 데이터를 한 번에 검증합니다.
        public static GameDataValidationResult Validate(string folder, GameDataCatalog catalog)
        {
            var result = new GameDataValidationResult();

            ValidateCsvFiles(folder, result);
            ValidateCatalogCounts(catalog, result);
            ValidateReferences(catalog, result);
            ValidateRarityCounts(catalog, result);

            return result;
        }

        // 로드 전에 CSV 파일 세트만 먼저 검증합니다.
        public static GameDataValidationResult ValidateCsvFiles(string folder)
        {
            var result = new GameDataValidationResult();
            ValidateCsvFiles(folder, result);
            return result;
        }

        // 필수 CSV 파일 존재 여부, 행 개수, 필수 컬럼을 검사합니다.
        private static void ValidateCsvFiles(string folder, GameDataValidationResult result)
        {
            for (var i = 0; i < RequiredCsvFiles.Length; i++)
            {
                var requirement = RequiredCsvFiles[i];
                var path = Path.Combine(folder, requirement.fileName);

                if (!File.Exists(path))
                {
                    result.AddError("CSV 파일 없음: " + requirement.fileName);
                    result.SetCsvRowCount(requirement.fileName, 0);
                    continue;
                }

                var table = CsvTable.Parse(File.ReadAllText(path));
                result.SetCsvRowCount(requirement.fileName, table.Rows.Count);

                if (table.Rows.Count == 0)
                    result.AddWarning("CSV 데이터 행이 비어 있음: " + requirement.fileName);

                ValidateColumns(requirement, table, result);
            }
        }

        // CSV 첫 데이터 행 기준으로 필수 컬럼 존재 여부를 검사합니다.
        private static void ValidateColumns(CsvRequirement requirement, CsvTable table, GameDataValidationResult result)
        {
            if (table.Rows.Count == 0)
                return;

            var firstRow = table.Rows[0];
            for (var i = 0; i < requirement.requiredColumns.Length; i++)
            {
                var column = requirement.requiredColumns[i];
                if (!firstRow.ContainsKey(column))
                    result.AddError(requirement.fileName + " 필수 컬럼 없음: " + column);
            }
        }

        // 로드 결과 딕셔너리에 데이터가 실제로 적재되었는지 검사합니다.
        private static void ValidateCatalogCounts(GameDataCatalog catalog, GameDataValidationResult result)
        {
            result.SetCatalogCount("items", catalog.items.Count);
            result.SetCatalogCount("elements", catalog.elements.Count);
            result.SetCatalogCount("stats", catalog.stats.Count);
            result.SetCatalogCount("statuses", catalog.statuses.Count);
            result.SetCatalogCount("conditions", catalog.conditions.Count);
            result.SetCatalogCount("triggers", catalog.triggers.Count);
            result.SetCatalogCount("actions", catalog.actions.Count);
            result.SetCatalogCount("projectiles", catalog.projectiles.Count);
            result.SetCatalogCount("rarities", catalog.rarities.Count);
            result.SetCatalogCount("rarityWeightTables", catalog.rarityWeightTables.Count);
            result.SetCatalogCount("effectParameters", catalog.effectParameters.Count);

            RequireNotEmpty(result, "items", catalog.items.Count);
            RequireNotEmpty(result, "rarities", catalog.rarities.Count);
            RequireNotEmpty(result, "elements", catalog.elements.Count);
            RequireNotEmpty(result, "stats", catalog.stats.Count);
            RequireNotEmpty(result, "triggers", catalog.triggers.Count);
            RequireNotEmpty(result, "actions", catalog.actions.Count);
        }

        // 반드시 있어야 하는 카탈로그가 비어 있으면 오류로 기록합니다.
        private static void RequireNotEmpty(GameDataValidationResult result, string key, int count)
        {
            if (count == 0)
                result.AddError("카탈로그 데이터가 비어 있음: " + key);
        }

        // 아이템과 정의 테이블 사이의 참조 ID 누락을 검사합니다.
        private static void ValidateReferences(GameDataCatalog catalog, GameDataValidationResult result)
        {
            foreach (var item in catalog.items.Values)
            {
                if (item == null || item.definition == null)
                {
                    result.AddError("정의가 없는 아이템 레코드가 있습니다.");
                    continue;
                }

                ValidateItemReferences(catalog, item, result);
            }

            ValidateDefinitionReferences(catalog, result);
        }

        // 아이템 하나에 연결된 희귀도, 속성, 스탯, 효과 참조를 검사합니다.
        private static void ValidateItemReferences(GameDataCatalog catalog, ItemRecord item, GameDataValidationResult result)
        {
            var itemId = item.definition.itemId;

            if (string.IsNullOrEmpty(itemId))
                result.AddError("item_id가 비어 있는 아이템이 있습니다.");

            if (!ContainsRequiredId(catalog.rarities, item.definition.rarityId))
                result.AddError(itemId + ": 존재하지 않는 rarity 참조 - " + item.definition.rarityId);

            for (var i = 0; i < item.elements.Count; i++)
            {
                var element = item.elements[i];
                if (!ContainsRequiredId(catalog.elements, element.elementId))
                    result.AddError(itemId + ": 존재하지 않는 element_id 참조 - " + element.elementId);
            }

            for (var i = 0; i < item.stats.Count; i++)
            {
                var stat = item.stats[i];
                if (!ContainsRequiredId(catalog.stats, stat.statId))
                    result.AddError(itemId + ": 존재하지 않는 stat_id 참조 - " + stat.statId);

                if (!ContainsOptionalId(catalog.conditions, stat.conditionId))
                    result.AddError(itemId + ": 존재하지 않는 condition_id 참조 - " + stat.conditionId);
            }

            for (var i = 0; i < item.effects.Count; i++)
                ValidateEffectReferences(catalog, itemId, item.effects[i], result);
        }

        // 아이템 효과 하나에 연결된 트리거, 액션, 투사체, 상태이상 참조를 검사합니다.
        private static void ValidateEffectReferences(GameDataCatalog catalog, string itemId, ItemEffectDefinition effect, GameDataValidationResult result)
        {
            if (!ContainsRequiredId(catalog.triggers, effect.triggerId))
                result.AddError(itemId + ": 존재하지 않는 trigger_id 참조 - " + effect.triggerId);

            if (!ContainsRequiredId(catalog.actions, effect.actionId))
                result.AddError(itemId + ": 존재하지 않는 action_id 참조 - " + effect.actionId);

            if (!ContainsOptionalId(catalog.conditions, effect.conditionId))
                result.AddError(itemId + ": 존재하지 않는 effect condition_id 참조 - " + effect.conditionId);

            if (!ContainsOptionalId(catalog.projectiles, effect.projectileId))
                result.AddError(itemId + ": 존재하지 않는 projectile_id 참조 - " + effect.projectileId);

            if (!ContainsOptionalId(catalog.statuses, effect.statusId))
                result.AddError(itemId + ": 존재하지 않는 status_id 참조 - " + effect.statusId);
        }

        // 정의 테이블끼리 연결된 참조 ID를 검사합니다.
        private static void ValidateDefinitionReferences(GameDataCatalog catalog, GameDataValidationResult result)
        {
            foreach (var element in catalog.elements.Values)
            {
                if (!ContainsOptionalId(catalog.elements, element.advancedElementId))
                    result.AddError(element.elementId + ": 존재하지 않는 advanced_element_id 참조 - " + element.advancedElementId);
            }

            foreach (var projectile in catalog.projectiles.Values)
            {
                if (!ContainsOptionalId(catalog.actions, projectile.onHitActionId))
                    result.AddError(projectile.projectileId + ": 존재하지 않는 on_hit_action_id 참조 - " + projectile.onHitActionId);

                if (!ContainsOptionalId(catalog.projectiles, projectile.nextProjectileId))
                    result.AddError(projectile.projectileId + ": 존재하지 않는 next_projectile_id 참조 - " + projectile.nextProjectileId);
            }

            foreach (var table in catalog.rarityWeightTables)
            {
                var weights = table.Value;
                for (var i = 0; i < weights.Count; i++)
                {
                    var weight = weights[i];
                    if (!ContainsRequiredId(catalog.rarities, weight.rarityId))
                        result.AddError(table.Key + ": 존재하지 않는 rarity_weight rarity_id 참조 - " + weight.rarityId);
                }
            }
        }

        // 희귀도별 아이템 개수를 리포트에 기록하고 빈 희귀도를 경고합니다.
        private static void ValidateRarityCounts(GameDataCatalog catalog, GameDataValidationResult result)
        {
            foreach (var rarity in catalog.rarities.Keys)
            {
                List<ItemRecord> items;
                var count = catalog.itemsByRarity.TryGetValue(rarity, out items) ? items.Count : 0;
                result.SetRarityItemCount(rarity, count);

                if (count == 0)
                    result.AddWarning("아이템이 하나도 없는 희귀도: " + rarity);
            }
        }

        // 반드시 채워져야 하는 ID가 딕셔너리에 있는지 확인합니다.
        private static bool ContainsRequiredId<TValue>(Dictionary<string, TValue> dictionary, string id)
        {
            return !string.IsNullOrEmpty(id) && dictionary.ContainsKey(id);
        }

        // 비어 있으면 허용하고, 값이 있으면 딕셔너리에 있는지 확인합니다.
        private static bool ContainsOptionalId<TValue>(Dictionary<string, TValue> dictionary, string id)
        {
            return string.IsNullOrEmpty(id) || dictionary.ContainsKey(id);
        }

        private sealed class CsvRequirement
        {
            public readonly string fileName;
            public readonly string[] requiredColumns;

            public CsvRequirement(string fileName, string[] requiredColumns)
            {
                this.fileName = fileName;
                this.requiredColumns = requiredColumns;
            }
        }
    }
}
