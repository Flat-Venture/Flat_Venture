using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FlatVenture.ItemData.Editor
{
    // 아이템 CSV를 기준으로 ItemAssetDataSO와 ItemAssetDatabaseSO를 관리하는 Editor 도구입니다.
    // 기존 SO에 직접 넣어둔 아이콘/사운드/프리팹은 덮어쓰지 않는 것을 기본 원칙으로 합니다.
    public static class ItemAssetDatabaseEditorUtility
    {
        private const string ItemDefinitionsCsvPath = "Assets/StreamingAssets/GameData/Items/item_definitions.csv";
        private const string ItemAssetFolder = "Assets/_Resource/ItemAssets";
        private const string DatabaseFolder = "Assets/_Resource/Databases";
        private const string DatabasePath = DatabaseFolder + "/ItemAssetDatabase.asset";
        private const string IconNamePrefix = "icon_";

        // CSV의 item_id 목록을 읽고, 없는 ItemAssetDataSO를 새로 생성합니다.
        [MenuItem("Tools/Flat Venture/Item Assets/Generate Missing Item Assets")]
        public static void GenerateMissingItemAssets()
        {
            EnsureProjectFolders();

            var itemIds = LoadItemIdsFromCsv();
            var existingAssets = FindAllItemAssets();
            var existingById = BuildAssetMap(existingAssets);
            var createdCount = 0;

            for (var i = 0; i < itemIds.Count; i++)
            {
                var itemId = itemIds[i];
                if (existingById.ContainsKey(itemId))
                    continue;

                var asset = ScriptableObject.CreateInstance<ItemAssetDataSO>();
                asset.SetItemIdForEditor(itemId);

                var path = AssetDatabase.GenerateUniqueAssetPath(ItemAssetFolder + "/" + SanitizeFileName(itemId) + ".asset");
                AssetDatabase.CreateAsset(asset, path);
                existingAssets.Add(asset);
                existingById[itemId] = asset;
                createdCount++;
            }

            RebuildDatabaseInternal(existingAssets);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[ItemAssetEditor] Created ItemAssetDataSO: " + createdCount);
        }

        // icon_item_id 이름 규칙에 맞는 Sprite를 찾아 비어 있는 icon에 자동 연결합니다.
        [MenuItem("Tools/Flat Venture/Item Assets/Auto Assign Icons")]
        public static void AutoAssignIcons()
        {
            var assets = FindAllItemAssets();
            var assignedCount = 0;
            var missingCount = 0;

            for (var i = 0; i < assets.Count; i++)
            {
                var asset = assets[i];
                if (asset == null || string.IsNullOrWhiteSpace(asset.ItemId))
                    continue;

                if (asset.Icon != null)
                    continue;

                var icon = FindSpriteByExactName(IconNamePrefix + asset.ItemId);
                if (icon == null)
                {
                    missingCount++;
                    continue;
                }

                asset.SetIconForEditor(icon);
                EditorUtility.SetDirty(asset);
                assignedCount++;
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[ItemAssetEditor] Assigned icons: " + assignedCount + " / Missing icons: " + missingCount);
        }

        // 폴더 안의 모든 ItemAssetDataSO를 Database에 다시 수집하고 item_id 사전순으로 정렬합니다.
        [MenuItem("Tools/Flat Venture/Item Assets/Rebuild Item Asset Database")]
        public static void RebuildDatabase()
        {
            EnsureProjectFolders();
            RebuildDatabaseInternal(FindAllItemAssets());
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[ItemAssetEditor] ItemAssetDatabase rebuilt.");
        }

        // 필수 프로젝트 폴더가 없으면 생성합니다.
        private static void EnsureProjectFolders()
        {
            EnsureFolder("Assets", "_Resource");
            EnsureFolder("Assets/_Resource", "ItemAssets");
            EnsureFolder("Assets/_Resource", "Databases");
        }

        // AssetDatabase 폴더를 생성합니다.
        private static void EnsureFolder(string parent, string child)
        {
            var path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(parent, child);
        }

        // item_definitions.csv에서 item_id 목록을 읽습니다.
        private static List<string> LoadItemIdsFromCsv()
        {
            if (!File.Exists(ItemDefinitionsCsvPath))
            {
                Debug.LogWarning("[ItemAssetEditor] item_definitions.csv not found: " + ItemDefinitionsCsvPath);
                return new List<string>();
            }

            var table = CsvTable.Parse(File.ReadAllText(ItemDefinitionsCsvPath));
            var itemIds = new List<string>();
            var seen = new HashSet<string>();

            foreach (var row in table.Rows)
            {
                var itemId = CsvValue.String(row, "item_id").Trim();
                if (string.IsNullOrEmpty(itemId))
                    continue;

                if (seen.Add(itemId))
                    itemIds.Add(itemId);
            }

            itemIds.Sort(string.CompareOrdinal);
            return itemIds;
        }

        // 프로젝트에서 ItemAssetDataSO를 모두 찾습니다.
        private static List<ItemAssetDataSO> FindAllItemAssets()
        {
            EnsureProjectFolders();

            var guids = AssetDatabase.FindAssets("t:ItemAssetDataSO", new[] { ItemAssetFolder });
            var assets = new List<ItemAssetDataSO>();

            for (var i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var asset = AssetDatabase.LoadAssetAtPath<ItemAssetDataSO>(path);
                if (asset != null)
                    assets.Add(asset);
            }

            assets.Sort(CompareAssetItemId);
            return assets;
        }

        // item_id를 키로 하는 중복 검사 맵을 만듭니다.
        private static Dictionary<string, ItemAssetDataSO> BuildAssetMap(List<ItemAssetDataSO> assets)
        {
            var map = new Dictionary<string, ItemAssetDataSO>();

            for (var i = 0; i < assets.Count; i++)
            {
                var asset = assets[i];
                if (asset == null || string.IsNullOrWhiteSpace(asset.ItemId))
                    continue;

                if (map.ContainsKey(asset.ItemId))
                {
                    Debug.LogWarning("[ItemAssetEditor] Duplicate item_id: " + asset.ItemId, asset);
                    continue;
                }

                map.Add(asset.ItemId, asset);
            }

            return map;
        }

        // Database asset을 만들거나 찾고, 현재 ItemAssetDataSO 목록을 등록합니다.
        private static void RebuildDatabaseInternal(List<ItemAssetDataSO> assets)
        {
            EnsureProjectFolders();

            var database = AssetDatabase.LoadAssetAtPath<ItemAssetDatabaseSO>(DatabasePath);
            if (database == null)
            {
                database = ScriptableObject.CreateInstance<ItemAssetDatabaseSO>();
                AssetDatabase.CreateAsset(database, DatabasePath);
            }

            assets.Sort(CompareAssetItemId);
            database.SetAssetsForEditor(assets);
            EditorUtility.SetDirty(database);
        }

        // 정확히 같은 이름의 Sprite를 프로젝트 전체에서 찾습니다.
        private static Sprite FindSpriteByExactName(string spriteName)
        {
            var guids = AssetDatabase.FindAssets(spriteName + " t:Sprite");
            for (var i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var assets = AssetDatabase.LoadAllAssetsAtPath(path);
                for (var j = 0; j < assets.Length; j++)
                {
                    var sprite = assets[j] as Sprite;
                    if (sprite != null && sprite.name == spriteName)
                        return sprite;
                }
            }

            return null;
        }

        // 파일명에 사용할 수 없는 문자를 안전한 문자로 바꿉니다.
        private static string SanitizeFileName(string value)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            for (var i = 0; i < invalidChars.Length; i++)
                value = value.Replace(invalidChars[i], '_');

            return value;
        }

        // item_id 기준으로 정렬합니다.
        private static int CompareAssetItemId(ItemAssetDataSO left, ItemAssetDataSO right)
        {
            var leftId = left != null ? left.ItemId : string.Empty;
            var rightId = right != null ? right.ItemId : string.Empty;
            return string.CompareOrdinal(leftId, rightId);
        }
    }
}
