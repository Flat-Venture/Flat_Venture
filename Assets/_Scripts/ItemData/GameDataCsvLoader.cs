using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace FlatVenture.ItemData
{
    // 아이템 관련 CSV를 모두 읽고, 행 단위 테이블을 item_id 기준 런타임 데이터로 묶습니다.
    // 구글 시트는 정규화된 구조로 관리하고, 게임에서는 빠른 딕셔너리 조회를 사용하기 위한 로더입니다.
    public static class GameDataCsvLoader
    {
        // StreamingAssets/GameData/Items 폴더에서 CSV 세트를 읽습니다.
        public static GameDataCatalog LoadFromStreamingAssets()
        {
            var folder = Path.Combine(Application.streamingAssetsPath, "GameData/Items");
            return LoadFromFolder(folder);
        }

        // 지정한 폴더에서 CSV 세트를 읽어 GameDataCatalog를 만듭니다.
        public static GameDataCatalog LoadFromFolder(string folder)
        {
            var catalog = new GameDataCatalog();

            LoadItemDefinitions(folder, catalog);
            LoadItemElements(folder, catalog);
            LoadItemStats(folder, catalog);
            LoadItemEffects(folder, catalog);
            LoadEffectParameters(folder, catalog);
            LoadProjectiles(folder, catalog);
            LoadRarityDefinitions(folder, catalog);
            LoadRarityWeightTables(folder, catalog);
            LoadElementDefinitions(folder, catalog);
            LoadStatDefinitions(folder, catalog);
            LoadStatusDefinitions(folder, catalog);
            LoadConditionDefinitions(folder, catalog);
            LoadTriggerDefinitions(folder, catalog);
            LoadActionDefinitions(folder, catalog);
            BuildRarityIndex(catalog);

            return catalog;
        }

        // 파일 하나를 읽어 CsvTable로 변환합니다.
        private static CsvTable ReadTable(string folder, string fileName)
        {
            var path = Path.Combine(folder, fileName);
            return CsvTable.Parse(File.ReadAllText(path));
        }

        // item_definitions.csv를 읽어 아이템 기본 정보를 등록합니다.
        private static void LoadItemDefinitions(string folder, GameDataCatalog catalog)
        {
            foreach (var row in ReadTable(folder, "item_definitions.csv").Rows)
            {
                var definition = new ItemDefinition
                {
                    itemId = CsvValue.String(row, "item_id"),
                    displayName = CsvValue.String(row, "display_name"),
                    rarityId = CsvValue.String(row, "rarity"),
                    isCursed = CsvValue.Bool(row, "is_cursed"),
                    isUnique = CsvValue.Bool(row, "is_unique"),
                    dropPool = CsvValue.String(row, "drop_pool"),
                    unlockId = CsvValue.String(row, "unlock_id")
                };

                catalog.items[definition.itemId] = new ItemRecord { definition = definition };
            }
        }

        // item_elements.csv를 읽어 아이템별 속성 포인트를 연결합니다.
        private static void LoadItemElements(string folder, GameDataCatalog catalog)
        {
            foreach (var row in ReadTable(folder, "item_elements.csv").Rows)
            {
                ItemRecord item;
                if (!catalog.items.TryGetValue(CsvValue.String(row, "item_id"), out item))
                    continue;

                item.elements.Add(new ItemElement
                {
                    itemId = CsvValue.String(row, "item_id"),
                    elementId = CsvValue.String(row, "element_id"),
                    elementValue = CsvValue.Int(row, "element_value"),
                });
            }
        }

        // item_stats.csv를 읽어 아이템별 스탯 수정값을 연결합니다.
        private static void LoadItemStats(string folder, GameDataCatalog catalog)
        {
            foreach (var row in ReadTable(folder, "item_stats.csv").Rows)
            {
                ItemRecord item;
                if (!catalog.items.TryGetValue(CsvValue.String(row, "item_id"), out item))
                    continue;

                item.stats.Add(new ItemStatModifier
                {
                    itemId = CsvValue.String(row, "item_id"),
                    statId = CsvValue.String(row, "stat_id"),
                    operation = CsvValue.String(row, "operation"),
                    value = CsvValue.Float(row, "value"),
                    appliesTo = CsvValue.String(row, "applies_to"),
                    conditionId = CsvValue.String(row, "condition_id"),
                    description = CsvValue.String(row, "description")
                });
            }
        }

        // item_effects.csv를 읽어 아이템별 발동 효과를 연결합니다.
        private static void LoadItemEffects(string folder, GameDataCatalog catalog)
        {
            foreach (var row in ReadTable(folder, "item_effects.csv").Rows)
            {
                ItemRecord item;
                if (!catalog.items.TryGetValue(CsvValue.String(row, "item_id"), out item))
                    continue;

                item.effects.Add(new ItemEffectDefinition
                {
                    itemId = CsvValue.String(row, "item_id"),
                    effectId = CsvValue.String(row, "effect_id"),
                    triggerId = CsvValue.String(row, "trigger_id"),
                    conditionId = CsvValue.String(row, "condition_id"),
                    actionId = CsvValue.String(row, "action_id"),
                    triggerCount = CsvValue.Int(row, "trigger_count"),
                    baseChance = CsvValue.Float(row, "base_chance"),
                    baseCooldown = CsvValue.Float(row, "base_cooldown"),
                    projectileId = CsvValue.String(row, "projectile_id"),
                    statusId = CsvValue.String(row, "status_id"),
                    buffId = CsvValue.String(row, "buff_id"),
                    target = CsvValue.String(row, "target"),
                    aimRule = CsvValue.String(row, "aim_rule"),
                });
            }
        }

        // effect_parameters.csv를 읽어 effect_id별 가변 파라미터를 묶습니다.
        private static void LoadEffectParameters(string folder, GameDataCatalog catalog)
        {
            foreach (var row in ReadTable(folder, "effect_parameters.csv").Rows)
            {
                var parameter = new EffectParameter
                {
                    effectId = CsvValue.String(row, "effect_id"),
                    paramId = CsvValue.String(row, "param_id"),
                    value = CsvValue.String(row, "value"),
                };

                List<EffectParameter> list;
                if (!catalog.effectParameters.TryGetValue(parameter.effectId, out list))
                {
                    list = new List<EffectParameter>();
                    catalog.effectParameters[parameter.effectId] = list;
                }

                list.Add(parameter);
            }
        }

        // projectiles.csv를 읽어 projectile_id별 투사체 기본 수치를 등록합니다.
        private static void LoadProjectiles(string folder, GameDataCatalog catalog)
        {
            foreach (var row in ReadTable(folder, "projectiles.csv").Rows)
            {
                var projectile = new ProjectileDefinition
                {
                    projectileId = CsvValue.String(row, "projectile_id"),
                    displayName = CsvValue.String(row, "display_name"),
                    moveType = CsvValue.String(row, "move_type"),
                    spawnPattern = CsvValue.String(row, "spawn_pattern"),
                    speed = CsvValue.Float(row, "speed"),
                    damageMultiplier = CsvValue.Float(row, "damage_multiplier"),
                    pierceCount = CsvValue.Int(row, "pierce_count"),
                    splitCount = CsvValue.Int(row, "split_count"),
                    splitAngle = CsvValue.Float(row, "split_angle"),
                    onHitActionId = CsvValue.String(row, "on_hit_action_id"),
                    nextProjectileId = CsvValue.String(row, "next_projectile_id"),
                    description = CsvValue.String(row, "description")
                };

                catalog.projectiles[projectile.projectileId] = projectile;
            }
        }

        // rarity_definitions.csv를 읽어 희귀도 표시/정렬 정보를 등록합니다.
        private static void LoadRarityDefinitions(string folder, GameDataCatalog catalog)
        {
            foreach (var row in ReadTable(folder, "rarity_definitions.csv").Rows)
            {
                var definition = new RarityDefinition
                {
                    rarityId = CsvValue.String(row, "rarity_id"),
                    displayName = CsvValue.String(row, "display_name"),
                    sortOrder = CsvValue.Int(row, "sort_order"),
                    colorHex = CsvValue.String(row, "color_hex"),
                    description = CsvValue.String(row, "description")
                };

                catalog.rarities[definition.rarityId] = definition;
            }
        }

        // rarity_weight_tables.csv를 읽어 보상 테이블별 희귀도 가중치를 등록합니다.
        private static void LoadRarityWeightTables(string folder, GameDataCatalog catalog)
        {
            foreach (var row in ReadTable(folder, "rarity_weight_tables.csv").Rows)
            {
                var weight = new RarityWeight
                {
                    tableId = CsvValue.String(row, "table_id"),
                    rarityId = CsvValue.String(row, "rarity_id"),
                    weight = CsvValue.Int(row, "weight"),
                };

                List<RarityWeight> list;
                if (!catalog.rarityWeightTables.TryGetValue(weight.tableId, out list))
                {
                    list = new List<RarityWeight>();
                    catalog.rarityWeightTables[weight.tableId] = list;
                }

                list.Add(weight);
            }
        }

        // elements.csv를 읽어 속성 정의를 등록합니다.
        private static void LoadElementDefinitions(string folder, GameDataCatalog catalog)
        {
            foreach (var row in ReadTable(folder, "elements.csv").Rows)
            {
                var definition = new ElementDefinition
                {
                    elementId = CsvValue.String(row, "element_id"),
                    displayName = CsvValue.String(row, "display_name"),
                    elementRole = CsvValue.String(row, "element_role"),
                    countsForBingo = CsvValue.Bool(row, "counts_for_bingo"),
                    advancedElementId = CsvValue.String(row, "advanced_element_id"),
                    description = CsvValue.String(row, "description")
                };

                catalog.elements[definition.elementId] = definition;
            }
        }

        // stat_definitions.csv를 읽어 스탯 정의를 등록합니다.
        private static void LoadStatDefinitions(string folder, GameDataCatalog catalog)
        {
            foreach (var row in ReadTable(folder, "stat_definitions.csv").Rows)
            {
                var definition = new StatDefinition
                {
                    statId = CsvValue.String(row, "stat_id"),
                    displayName = CsvValue.String(row, "display_name"),
                    statGroup = CsvValue.String(row, "stat_group"),
                    valueType = CsvValue.String(row, "value_type"),
                    stackRule = CsvValue.String(row, "stack_rule"),
                    minValue = CsvValue.String(row, "min_value"),
                    maxValue = CsvValue.String(row, "max_value"),
                    description = CsvValue.String(row, "description")
                };

                catalog.stats[definition.statId] = definition;
            }
        }

        // status_effects.csv를 읽어 상태이상 정의를 등록합니다.
        private static void LoadStatusDefinitions(string folder, GameDataCatalog catalog)
        {
            foreach (var row in ReadTable(folder, "status_effects.csv").Rows)
            {
                var definition = new StatusEffectDefinition
                {
                    statusId = CsvValue.String(row, "status_id"),
                    displayName = CsvValue.String(row, "display_name"),
                    stackRule = CsvValue.String(row, "stack_rule"),
                    maxStack = CsvValue.Int(row, "max_stack"),
                    duration = CsvValue.Float(row, "duration"),
                    baseTickDamage = CsvValue.Float(row, "base_tick_damage"),
                    tickInterval = CsvValue.Float(row, "tick_interval"),
                    description = CsvValue.String(row, "description")
                };

                catalog.statuses[definition.statusId] = definition;
            }
        }

        // condition_definitions.csv를 읽어 조건 정의를 등록합니다.
        private static void LoadConditionDefinitions(string folder, GameDataCatalog catalog)
        {
            foreach (var row in ReadTable(folder, "condition_definitions.csv").Rows)
            {
                var definition = new ConditionDefinition
                {
                    conditionId = CsvValue.String(row, "condition_id"),
                    conditionType = CsvValue.String(row, "condition_type"),
                    paramId = CsvValue.String(row, "param_id"),
                    value = CsvValue.String(row, "value"),
                    description = CsvValue.String(row, "description")
                };

                catalog.conditions[definition.conditionId] = definition;
            }
        }

        // trigger_definitions.csv를 읽어 효과 발동 조건 정의를 등록합니다.
        private static void LoadTriggerDefinitions(string folder, GameDataCatalog catalog)
        {
            foreach (var row in ReadTable(folder, "trigger_definitions.csv").Rows)
            {
                var definition = new TriggerDefinition
                {
                    triggerId = CsvValue.String(row, "trigger_id"),
                    displayName = CsvValue.String(row, "display_name"),
                    needsCounter = CsvValue.Bool(row, "needs_counter"),
                    usesChance = CsvValue.Bool(row, "uses_chance"),
                    usesCooldown = CsvValue.Bool(row, "uses_cooldown"),
                    resetsOnFloorEnd = CsvValue.Bool(row, "resets_on_floor_end"),
                    description = CsvValue.String(row, "description")
                };

                catalog.triggers[definition.triggerId] = definition;
            }
        }

        // action_definitions.csv를 읽어 효과 액션 정의를 등록합니다.
        private static void LoadActionDefinitions(string folder, GameDataCatalog catalog)
        {
            foreach (var row in ReadTable(folder, "action_definitions.csv").Rows)
            {
                var definition = new ActionDefinition
                {
                    actionId = CsvValue.String(row, "action_id"),
                    displayName = CsvValue.String(row, "display_name"),
                    requiredRef = CsvValue.String(row, "required_ref"),
                    description = CsvValue.String(row, "description")
                };

                catalog.actions[definition.actionId] = definition;
            }
        }

        // 보상 후보 필터링을 빠르게 하기 위해 등급별 아이템 인덱스를 만듭니다.
        private static void BuildRarityIndex(GameDataCatalog catalog)
        {
            foreach (var pair in catalog.items)
            {
                List<ItemRecord> list;
                var rarity = pair.Value.definition.rarityId;
                if (!catalog.itemsByRarity.TryGetValue(rarity, out list))
                {
                    list = new List<ItemRecord>();
                    catalog.itemsByRarity[rarity] = list;
                }

                list.Add(pair.Value);
            }
        }
    }
}
