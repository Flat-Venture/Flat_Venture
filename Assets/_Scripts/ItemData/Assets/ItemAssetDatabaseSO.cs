using System.Collections.Generic;
using UnityEngine;

namespace FlatVenture.ItemData
{
    // 모든 ItemAssetDataSO를 모아 item_id로 빠르게 찾기 위한 Registry입니다.
    // 런타임 UI는 이 Database를 통해 아이콘/사운드/프리팹을 조회합니다.
    [CreateAssetMenu(fileName = "ItemAssetDatabase", menuName = "Flat Venture/Item Data/Item Asset Database")]
    public sealed class ItemAssetDatabaseSO : ScriptableObject
    {
        [SerializeField] private Sprite defaultIcon;
        [SerializeField] private List<ItemAssetDataSO> itemAssets = new List<ItemAssetDataSO>();

        private readonly Dictionary<string, ItemAssetDataSO> assetsByItemId = new Dictionary<string, ItemAssetDataSO>();
        private bool lookupDirty = true;

        public Sprite DefaultIcon
        {
            get { return defaultIcon; }
        }

        public IReadOnlyList<ItemAssetDataSO> ItemAssets
        {
            get { return itemAssets; }
        }

        // ScriptableObject가 로드될 때 lookup을 다시 만들도록 표시합니다.
        private void OnEnable()
        {
            lookupDirty = true;
        }

        // Inspector에서 리스트를 수정했을 때 lookup을 다시 만들도록 표시합니다.
        private void OnValidate()
        {
            lookupDirty = true;
        }

        // item_id에 해당하는 에셋 데이터를 찾습니다.
        public bool TryGetAsset(string itemId, out ItemAssetDataSO asset)
        {
            EnsureLookup();

            if (string.IsNullOrWhiteSpace(itemId))
            {
                asset = null;
                return false;
            }

            return assetsByItemId.TryGetValue(itemId, out asset);
        }

        // item_id에 해당하는 실제 아이콘을 찾습니다. 없으면 기본 아이콘을 반환합니다.
        public Sprite GetIconOrDefault(string itemId)
        {
            ItemAssetDataSO asset;
            if (TryGetAsset(itemId, out asset) && asset != null && asset.Icon != null)
                return asset.Icon;

            return defaultIcon;
        }

        // 외부에서 리스트가 바뀌었음을 알려 lookup을 다시 만들게 합니다.
        public void MarkLookupDirty()
        {
            lookupDirty = true;
        }

        // Editor 자동화 도구에서 수집한 에셋 목록으로 교체합니다.
        public void SetAssetsForEditor(List<ItemAssetDataSO> assets)
        {
            itemAssets = assets ?? new List<ItemAssetDataSO>();
            itemAssets.Sort(CompareAssetItemId);
            MarkLookupDirty();
        }

        // ContextMenu에서 item_id 기준으로 리스트를 정렬합니다.
        [ContextMenu("Sort By Item Id")]
        public void SortByItemId()
        {
            itemAssets.Sort(CompareAssetItemId);
            MarkLookupDirty();
        }

        // lookup Dictionary를 최신 상태로 보장합니다.
        private void EnsureLookup()
        {
            if (!lookupDirty)
                return;

            assetsByItemId.Clear();

            for (var i = 0; i < itemAssets.Count; i++)
            {
                var asset = itemAssets[i];
                if (asset == null || string.IsNullOrWhiteSpace(asset.ItemId))
                    continue;

                if (assetsByItemId.ContainsKey(asset.ItemId))
                {
                    Debug.LogWarning("[ItemAssetDatabase] 중복 item_id가 있습니다: " + asset.ItemId, this);
                    continue;
                }

                assetsByItemId.Add(asset.ItemId, asset);
            }

            lookupDirty = false;
        }

        // item_id 문자열 기준 정렬을 수행합니다.
        private static int CompareAssetItemId(ItemAssetDataSO left, ItemAssetDataSO right)
        {
            var leftId = left != null ? left.ItemId : string.Empty;
            var rightId = right != null ? right.ItemId : string.Empty;
            return string.CompareOrdinal(leftId, rightId);
        }
    }
}
