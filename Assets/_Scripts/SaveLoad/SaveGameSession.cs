using UnityEngine;

namespace FlatVenture.SaveLoad
{
    // 씬 전환 중 현재 선택한 세이브 데이터를 임시로 들고 있는 런타임 세션입니다.
    // MonoBehaviour가 아니라 static 클래스라서 마을/던전 씬 전환 중에도 값이 유지됩니다.
    // 단, 게임을 완전히 종료하면 메모리는 사라지므로 중요한 변경은 SaveLoadService.Save로 파일에 저장해야 합니다.
    // 이후 게임 매니저가 생기면 이 역할을 DontDestroyOnLoad 기반 매니저나 부트스트랩 시스템으로 옮길 수 있습니다.
    public static class SaveGameSession
    {
        public enum StartReason
        {
            None = 0,
            NewGame = 1,
            Continue = 2
        }

        public static SaveData CurrentSaveData { get; private set; }
        public static int CurrentSlotIndex { get; private set; }
        public static bool HasActiveSave { get; private set; }
        public static StartReason CurrentStartReason { get; private set; }
        private static double lastPlayTimeSyncRealtime;

        // TitleMenuBehaviour가 새 게임/이어하기 슬롯 선택에 성공했을 때 호출합니다.
        // 이후 마을/던전 씬에서는 CurrentSaveData를 기준으로 진행 상황을 이어받습니다.
        public static void SetActiveSave(SaveData saveData, StartReason startReason)
        {
            CurrentSaveData = saveData;
            CurrentSlotIndex = saveData != null ? saveData.slotIndex : 0;
            HasActiveSave = saveData != null;
            CurrentStartReason = saveData != null ? startReason : StartReason.None;
            lastPlayTimeSyncRealtime = Time.realtimeSinceStartupAsDouble;
        }

        // 현재 세션의 실제 경과 시간을 SaveData.playTimeSeconds에 반영합니다.
        // 저장 직전이나 슬롯 요약 갱신 전에 호출하면 플레이 시간이 최신 상태가 됩니다.
        public static void SyncPlayTime()
        {
            if (!HasActiveSave || CurrentSaveData == null)
                return;

            var now = Time.realtimeSinceStartupAsDouble;
            var delta = now - lastPlayTimeSyncRealtime;
            if (delta <= 0d)
                return;

            CurrentSaveData.playTimeSeconds += delta;
            lastPlayTimeSyncRealtime = now;
        }

        // 전달된 SaveData가 현재 세션 데이터라면 플레이 시간을 최신화합니다.
        public static void SyncPlayTimeIfActive(SaveData saveData)
        {
            if (saveData == null || saveData != CurrentSaveData)
                return;

            SyncPlayTime();
        }

        // 타이틀로 완전히 돌아가거나 세션을 종료할 때 현재 세션 정보를 비웁니다.
        public static void Clear()
        {
            SyncPlayTime();
            CurrentSaveData = null;
            CurrentSlotIndex = 0;
            HasActiveSave = false;
            CurrentStartReason = StartReason.None;
            lastPlayTimeSyncRealtime = 0d;
        }
    }
}
