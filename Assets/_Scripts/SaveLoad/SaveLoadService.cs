using System;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace FlatVenture.SaveLoad
{
    // 세이브 파일 경로 계산, 저장, 로드, 삭제를 담당하는 서비스입니다.
    // UI나 게임 진행 로직은 파일 입출력 세부사항을 모르고 이 클래스만 호출합니다.
    public static class SaveLoadService
    {
        public const int CurrentSaveVersion = 1;
        public const int SlotCount = 3;

        private const string SaveFolderName = "SaveData";
        private const string SaveFileFormat = "save_slot_{0}.json";

        // 새 게임에 사용할 기본 SaveData를 만듭니다.
        public static SaveData CreateNewSave(int slotIndex, string profileName = "Player")
        {
            ValidateSlotIndex(slotIndex);

            var now = GetUtcNowText();
            var saveData = new SaveData
            {
                saveVersion = CurrentSaveVersion,
                slotIndex = slotIndex,
                createdAt = now,
                updatedAt = now,
                playTimeSeconds = 0d
            };

            saveData.user.profileName = profileName;

            // 현재 선택된 기본 캐릭터는 해금 목록에도 들어있어야 한다
            saveData.unlocks.unlockedCharacterIds.Add(saveData.user.selectedCharacterId);
            return saveData;
        }

        // 지정 슬롯에 세이브 데이터를 JSON 파일로 저장합니다.
        public static void Save(int slotIndex, SaveData saveData)
        {
            ValidateSlotIndex(slotIndex);

            if (saveData == null)
                throw new ArgumentNullException(nameof(saveData));

            saveData.slotIndex = slotIndex;
            saveData.saveVersion = CurrentSaveVersion;
            SaveGameSession.SyncPlayTimeIfActive(saveData);

            if (string.IsNullOrEmpty(saveData.createdAt))
                saveData.createdAt = GetUtcNowText();

            saveData.updatedAt = GetUtcNowText();

            Directory.CreateDirectory(GetSaveDirectory());

            // 파일 손상을 줄이기 위해 임시 파일을 먼저 쓰고,
            // 기존 파일이 있으면 삭제 후 임시 파일을 이동시키는 방식으로 저장합니다.
            var path = GetSlotPath(slotIndex);
            var tempPath = path + ".tmp";
            var json = JsonUtility.ToJson(saveData, true);

            File.WriteAllText(tempPath, json);

            if (File.Exists(path))
                File.Delete(path);

            File.Move(tempPath, path);
        }

        // 지정 슬롯의 세이브 데이터를 불러옵니다.
        public static bool TryLoad(int slotIndex, out SaveData saveData)
        {
            ValidateSlotIndex(slotIndex);

            saveData = null;
            var path = GetSlotPath(slotIndex);
            if (!File.Exists(path))
                return false;

            var json = File.ReadAllText(path);
            saveData = JsonUtility.FromJson<SaveData>(json);
            return saveData != null;
        }

        // 지정 슬롯의 세이브 파일이 있는지 확인합니다.
        public static bool HasSave(int slotIndex)
        {
            ValidateSlotIndex(slotIndex);
            return File.Exists(GetSlotPath(slotIndex));
        }

        // 지정 슬롯의 세이브 파일을 삭제합니다.
        public static bool Delete(int slotIndex)
        {
            ValidateSlotIndex(slotIndex);

            var path = GetSlotPath(slotIndex);
            if (!File.Exists(path))
                return false;

            File.Delete(path);
            return true;
        }

        // 슬롯 하나의 요약 정보를 가져옵니다.
        public static SaveSlotSummary GetSlotSummary(int slotIndex)
        {
            ValidateSlotIndex(slotIndex);

            if (!HasSave(slotIndex))
                return SaveSlotSummary.Empty(slotIndex);

            try
            {
                SaveData saveData;
                return TryLoad(slotIndex, out saveData)
                    ? SaveSlotSummary.FromSaveData(saveData)
                    : SaveSlotSummary.Corrupted(slotIndex, "세이브 데이터를 읽지 못했습니다.");
            }
            catch (Exception exception)
            {
                return SaveSlotSummary.Corrupted(slotIndex, exception.Message);
            }
        }

        // 모든 슬롯의 요약 정보를 가져옵니다.
        public static SaveSlotSummary[] GetAllSlotSummaries()
        {
            var summaries = new SaveSlotSummary[SlotCount];
            for (var i = 0; i < SlotCount; i++)
                summaries[i] = GetSlotSummary(i + 1);

            return summaries;
        }

        // 세이브 파일이 저장되는 폴더 경로를 반환합니다.
        public static string GetSaveDirectory()
        {
            return Path.Combine(Application.persistentDataPath, SaveFolderName);
        }

        // 지정 슬롯의 세이브 파일 경로를 반환합니다.
        public static string GetSlotPath(int slotIndex)
        {
            ValidateSlotIndex(slotIndex);
            return Path.Combine(GetSaveDirectory(), string.Format(CultureInfo.InvariantCulture, SaveFileFormat, slotIndex));
        }

        // 슬롯 번호가 허용 범위인지 검사합니다.
        private static void ValidateSlotIndex(int slotIndex)
        {
            if (slotIndex < 1 || slotIndex > SlotCount)
                throw new ArgumentOutOfRangeException(nameof(slotIndex), "세이브 슬롯은 1번부터 3번까지만 사용할 수 있습니다.");
        }

        // 저장 시각을 ISO-8601 UTC 문자열로 만듭니다.
        private static string GetUtcNowText()
        {
            // ISO-8601 형식의 UTC 문자열을 반환합니다. 예: "2024-06-01T12:34:56.789Z"
            // PC 언어 설정이 한국어든 영어든 날짜 문자열 형식이 흔들리지 않게 고정
            return DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture);
        }
    }
}
