using System.Collections.Generic;

namespace FlatVenture.ItemData
{
    // CSV 로드 결과를 모아두는 런타임 조회 허브입니다.
    // 게임 시스템은 CSV를 반복해서 읽지 않고 이 카탈로그의 딕셔너리를 조회합니다.
    public sealed class GameDataCatalog
    {
        public readonly Dictionary<string, ItemRecord> items = new Dictionary<string, ItemRecord>();
        public readonly Dictionary<string, ElementDefinition> elements = new Dictionary<string, ElementDefinition>();
        public readonly Dictionary<string, StatDefinition> stats = new Dictionary<string, StatDefinition>();
        public readonly Dictionary<string, StatusEffectDefinition> statuses = new Dictionary<string, StatusEffectDefinition>();
        public readonly Dictionary<string, ConditionDefinition> conditions = new Dictionary<string, ConditionDefinition>();
        public readonly Dictionary<string, TriggerDefinition> triggers = new Dictionary<string, TriggerDefinition>();
        public readonly Dictionary<string, ActionDefinition> actions = new Dictionary<string, ActionDefinition>();
        public readonly Dictionary<string, ProjectileDefinition> projectiles = new Dictionary<string, ProjectileDefinition>();
        public readonly Dictionary<string, RarityDefinition> rarities = new Dictionary<string, RarityDefinition>();
        public readonly Dictionary<string, List<RarityWeight>> rarityWeightTables = new Dictionary<string, List<RarityWeight>>();
        public readonly Dictionary<string, List<EffectParameter>> effectParameters = new Dictionary<string, List<EffectParameter>>();
        public readonly Dictionary<string, List<ItemRecord>> itemsByRarity = new Dictionary<string, List<ItemRecord>>();

        // item_id로 아이템 레코드를 찾습니다.
        public bool TryGetItem(string itemId, out ItemRecord item)
        {
            return items.TryGetValue(itemId, out item);
        }

        // effect_id에 연결된 파라미터 목록을 가져옵니다.
        public List<EffectParameter> GetEffectParameters(string effectId)
        {
            List<EffectParameter> parameters;
            return effectParameters.TryGetValue(effectId, out parameters)
                ? parameters
                : new List<EffectParameter>();
        }
    }
}
