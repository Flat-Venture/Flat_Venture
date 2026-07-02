using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace FlatVenture.ItemData.Editor
{
    // 에디터 전용 동기화 도구입니다.
    // 개발 중에는 구글 시트를 원본으로 쓰고, 실제 게임은 로컬 CSV를 읽도록 분리합니다.
    // 이렇게 하면 웹에서 데이터를 편하게 수정하면서도 빌드와 오프라인 테스트는 안정적으로 유지할 수 있습니다.
    public static class GoogleSheetsCsvSync
    {
        private const string SpreadsheetId = "13_3HSZyWpSLCtWL8aYeayhDdFfrqf4jyULfybj-cOKE";
        private const string OutputFolder = "Assets/StreamingAssets/GameData/Items";

        // 각 gid는 구글 시트의 개별 탭을 뜻합니다.
        // 저장 파일명은 GameDataCsvLoader가 찾는 CSV 파일명과 같아야 합니다.
        private static readonly SheetExport[] Sheets =
        {
            new SheetExport("item_definitions.csv", "0"),
            new SheetExport("item_elements.csv", "1576027946"),
            new SheetExport("item_stats.csv", "801483396"),
            new SheetExport("item_effects.csv", "1961777259"),
            new SheetExport("effect_parameters.csv", "331593735"),
            new SheetExport("projectiles.csv", "874414644"),
            new SheetExport("status_effects.csv", "1203579815"),
            new SheetExport("elements.csv", "1416507896"),
            new SheetExport("rarity_definitions.csv", "1602137227"),
            new SheetExport("rarity_weight_tables.csv", "1359337548"),
            new SheetExport("stat_definitions.csv", "1651440270"),
            new SheetExport("condition_definitions.csv", "365828668"),
            new SheetExport("trigger_definitions.csv", "909840751"),
            new SheetExport("action_definitions.csv", "601539215"),
        };

        [MenuItem("Tools/Flat Venture/Sync CSV From Google Sheets")]
        // 구글 시트의 각 탭을 CSV로 내려받아 StreamingAssets 폴더에 저장합니다.
        public static void Sync()
        {
            Directory.CreateDirectory(OutputFolder);

            var successCount = 0;
            var failed = new List<string>();

            using (var client = new WebClient())
            {
                client.Encoding = Encoding.UTF8;

                foreach (var sheet in Sheets)
                {
                    var url = BuildExportUrl(sheet.Gid);
                    var outputPath = Path.Combine(OutputFolder, sheet.FileName);

                    try
                    {
                        var csv = client.DownloadString(url);
                        File.WriteAllText(outputPath, csv, new UTF8Encoding(true));
                        successCount++;
                    }
                    catch (Exception exception)
                    {
                        failed.Add(sheet.FileName + ": " + exception.Message);
                    }
                }
            }

            AssetDatabase.Refresh();

            if (failed.Count == 0)
            {
                Debug.Log("Google Sheets CSV sync complete. Files updated: " + successCount);
                EditorUtility.DisplayDialog("Flat Venture", "CSV sync complete. Files updated: " + successCount, "OK");
                return;
            }

            var message = "CSV sync partially failed\n\nSuccess: " + successCount + "\nFailed: " + failed.Count + "\n\n" +
                          string.Join("\n", failed);
            Debug.LogError(message);
            EditorUtility.DisplayDialog("Flat Venture", message, "OK");
        }

        // 구글 시트 탭 하나를 CSV로 내보내는 URL을 만듭니다.
        private static string BuildExportUrl(string gid)
        {
            return "https://docs.google.com/spreadsheets/d/" + SpreadsheetId + "/export?format=csv&gid=" + gid;
        }

        private readonly struct SheetExport
        {
            public readonly string FileName;
            public readonly string Gid;

            public SheetExport(string fileName, string gid)
            {
                FileName = fileName;
                Gid = gid;
            }
        }
    }
}
