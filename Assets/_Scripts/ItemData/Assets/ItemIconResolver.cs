using System.Collections.Generic;
using UnityEngine;

namespace FlatVenture.ItemData
{
    // 아이템 UI에서 사용할 아이콘을 결정합니다.
    // 실제 아이콘이 없으면 item_id 기반 색상의 임시 Sprite를 생성해서 테스트 중에도 아이템을 구분할 수 있게 합니다.
    public static class ItemIconResolver
    {
        private const int PlaceholderSize = 64;
        private static readonly Dictionary<string, Sprite> placeholderIcons = new Dictionary<string, Sprite>();

        // 아이템 아이콘을 반환합니다. SO 아이콘이 없으면 item_id 기반 임시 아이콘을 사용합니다.
        public static Sprite ResolveIcon(string itemId, ItemAssetDatabaseSO database)
        {
            if (database != null)
            {
                ItemAssetDataSO asset;
                if (database.TryGetAsset(itemId, out asset) && asset != null && asset.Icon != null)
                    return asset.Icon;
            }

            return GetOrCreatePlaceholder(itemId);
        }

        // item_id에 대응하는 임시 아이콘 색을 반환합니다.
        public static Color GetPlaceholderColor(string itemId)
        {
            var hash = GetStableHash(string.IsNullOrWhiteSpace(itemId) ? "missing_item" : itemId);
            var hue = (hash % 360) / 360f;
            return Color.HSVToRGB(hue, 0.58f, 0.86f);
        }

        // 테스트나 씬 종료 시 임시 아이콘 캐시를 정리하고 싶을 때 호출합니다.
        public static void ClearPlaceholderCache()
        {
            foreach (var pair in placeholderIcons)
            {
                if (pair.Value == null)
                    continue;

                var texture = pair.Value.texture;
                Object.Destroy(pair.Value);

                if (texture != null)
                    Object.Destroy(texture);
            }

            placeholderIcons.Clear();
        }

        // item_id별 임시 Sprite를 캐시해서 재사용합니다.
        private static Sprite GetOrCreatePlaceholder(string itemId)
        {
            var key = string.IsNullOrWhiteSpace(itemId) ? "missing_item" : itemId;

            Sprite sprite;
            if (placeholderIcons.TryGetValue(key, out sprite) && sprite != null)
                return sprite;

            sprite = CreatePlaceholderSprite(key);
            placeholderIcons[key] = sprite;
            return sprite;
        }

        // 단색 배경과 어두운 테두리를 가진 임시 Sprite를 생성합니다.
        private static Sprite CreatePlaceholderSprite(string itemId)
        {
            var color = GetPlaceholderColor(itemId);
            var borderColor = color * 0.55f;
            borderColor.a = 1f;

            var texture = new Texture2D(PlaceholderSize, PlaceholderSize, TextureFormat.RGBA32, false);
            texture.name = "placeholder_icon_" + itemId;
            texture.hideFlags = HideFlags.DontSave;
            texture.filterMode = FilterMode.Point;

            for (var y = 0; y < PlaceholderSize; y++)
            {
                for (var x = 0; x < PlaceholderSize; x++)
                {
                    var isBorder = x < 4 || y < 4 || x >= PlaceholderSize - 4 || y >= PlaceholderSize - 4;
                    texture.SetPixel(x, y, isBorder ? borderColor : color);
                }
            }

            texture.Apply(false, false);

            var sprite = Sprite.Create(texture, new Rect(0f, 0f, PlaceholderSize, PlaceholderSize), new Vector2(0.5f, 0.5f), PlaceholderSize);
            sprite.name = "placeholder_icon_" + itemId;
            sprite.hideFlags = HideFlags.DontSave;
            return sprite;
        }

        // 런타임마다 같은 item_id가 같은 색을 얻도록 안정적인 해시를 만듭니다.
        private static int GetStableHash(string value)
        {
            unchecked
            {
                var hash = 23;
                for (var i = 0; i < value.Length; i++)
                    hash = hash * 31 + value[i];

                return Mathf.Abs(hash);
            }
        }
    }
}
