using System.Collections.Generic;
using UnityEngine;

// 던전 안에서 임시 UI가 열려 있을 때 전투/인벤토리 같은 입력을 막기 위한 공용 도우미입니다.
public static class DungeonUiInputBlocker
{
    private static readonly HashSet<object> blockers = new HashSet<object>();
    private static bool isPauseMenuOpen;
    private static bool isTimeScaleOverridden;
    private static float previousTimeScale = 1f;

    // true이면 플레이어 이동, 인벤토리 단축키, 특수 방 상호작용 같은 게임 입력을 무시해야 합니다.
    public static bool BlocksGameplayInput
    {
        get { return isPauseMenuOpen || blockers.Count > 0; }
    }

    // ESC 메뉴는 다른 임시 UI보다 우선순위가 높기 때문에 별도 상태로 노출합니다.
    public static bool IsPauseMenuOpen
    {
        get { return isPauseMenuOpen; }
    }

    // ESC 메뉴는 항상 최상위 입력으로 취급하고, 열려 있는 동안 다른 입력을 막습니다.
    public static void SetPauseMenuOpen(bool isOpen)
    {
        isPauseMenuOpen = isOpen;
        RefreshTimeScale();
    }

    // 특정 UI 소유자가 입력 차단을 시작하거나 해제할 때 호출합니다.
    public static void SetBlocked(object owner, bool isBlocked)
    {
        if (owner == null)
        {
            return;
        }

        if (isBlocked)
        {
            // 같은 owner가 여러 번 등록되어도 HashSet이라 중복 등록되지 않습니다.
            blockers.Add(owner);
            RefreshTimeScale();
            return;
        }

        // owner가 닫힌 UI라면 목록에서 제거하고, 남은 UI가 없을 때만 시간 정지를 풉니다.
        blockers.Remove(owner);
        RefreshTimeScale();
    }

    // UI가 열려 있으면 플레이어 이동/포탈 진입 같은 시간 기반 동작을 멈춥니다.
    private static void RefreshTimeScale()
    {
        bool shouldBlock = BlocksGameplayInput;
        if (shouldBlock && !isTimeScaleOverridden)
        {
            // 이미 다른 시스템이 timeScale을 0으로 만들었다면 복구값은 기본 1로 둡니다.
            previousTimeScale = Mathf.Approximately(Time.timeScale, 0f) ? 1f : Time.timeScale;
            Time.timeScale = 0f;
            isTimeScaleOverridden = true;
            return;
        }

        if (!shouldBlock && isTimeScaleOverridden)
        {
            // 모든 UI 차단이 해제된 뒤에만 원래 시간 배율로 복구합니다.
            Time.timeScale = previousTimeScale;
            isTimeScaleOverridden = false;
        }
    }
}
